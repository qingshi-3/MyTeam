using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.TacticalCommands;

namespace TowerAutobattler.Run;

public sealed class RunBattlePreparationService
{
    private readonly ContentRegistry _content;
    private readonly CompiledGameProject _project;
    private readonly CompiledRunRules _rules;
    private readonly RunRelicService _relics;
    private readonly CompiledGrowthRules? _growthRules;

    public RunBattlePreparationService(
        ContentRegistry content,
        CompiledGameProject project,
        RunRelicService relics,
        CompiledGrowthRules? growthRules = null)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _rules = project.RunRules;
        _relics = relics ?? throw new ArgumentNullException(nameof(relics));
        _growthRules = growthRules;
    }

    public BattleConfig Build(ActiveRunDto run, EncounterPlan encounter, bool requireLegalFormation)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(encounter);
        if (run.Roster.Count == 0) throw new InvalidOperationException("Player roster is empty.");
        var compiledEncounter = ResolveEncounter(encounter);
        var expeditionProfile = ResolveExpeditionProfile(encounter);
        if (expeditionProfile is not null && run.Action is { Phase: RunActionPhase.Battle } action &&
            (action.Source.LegacyFloorIndex is not null || string.IsNullOrWhiteSpace(action.Source.ActionId) ||
             action.Encounter is not { } frozen || !SameExpeditionPlan(frozen, encounter)))
            throw new InvalidOperationException("Committed expedition battle cannot change its frozen encounter.");
        var bossTimeline = ToBattleTimeline(compiledEncounter.BossTimeline);
        if (!_project.FloorRules.TryGetValue(encounter.FloorRuleId, out var ruleScene))
            throw new InvalidOperationException($"Missing compiled floor rule: {encounter.FloorRuleId}");
        var rule = ruleScene.Instantiate<FloorRuleContentRoot>();
        try
        {
            var floorRuntime = rule.CreateRuntime();
            if (requireLegalFormation && !RunFormationPolicy.Validate(run, _rules, floorRuntime.CanOccupy))
                throw new InvalidOperationException("Player formation is invalid for the selected floor rule.");

            var relics = _relics.PrepareBattle(Key(run, encounter), Bindings(run));
            // Growth battles execute the prepared roster and one-shot spell automatically.
            // Historical loadout ids stay in Run saves but confer no combat intervention.
            var tacticalCommands = _growthRules is null
                ? TacticalCommandBattlePreparationBuilder.Build(run, _content.Graph)
                : null;
            var request = RunBattlePreparationAdapter.CreateRequest(
                _content,
                run,
                encounter,
                floorRuntime,
                relics.Modifiers,
                relics.BattlePreparation,
                tacticalCommands,
                bossTimeline,
                RunPopulationPolicy.Evaluate(run, _rules).AvailableDeploymentPopulation,
                requireLegalFormation, _growthRules);
            return BattlePreparationAssembler.Assemble(request with
            {
                Units = encounter.IsElite ? request.Units.Select(unit => unit.Team == 1 && !unit.IsTemporary
                    ? unit with { HealthRatio = unit.HealthRatio * new RunRelicRules(_content).EliteHealthRatio(run) } : unit).ToImmutableArray() : request.Units,
                MatrixStatusRules = _project.MatrixStatusRules,
                EnemyHealthMultiplier = expeditionProfile?.FinalHealthMultiplier ??
                    compiledEncounter.EnemyHealthMultiplier * LocalMultiplier(compiledEncounter.LocalHealthMultipliers, run),
                EnemyDamageMultiplier = expeditionProfile?.FinalDamageMultiplier ??
                    compiledEncounter.EnemyDamageMultiplier * LocalMultiplier(compiledEncounter.LocalDamageMultipliers, run)
            });
        }
        finally { rule.Free(); }
    }

    private float LocalMultiplier(ImmutableArray<float> curve, ActiveRunDto run) =>
        curve.IsDefaultOrEmpty ? 1 : curve[run.FloorIndex % _project.Campaign.FloorsPerRegion];

    public RelicRunApplyResult ValidateTransition(
        ActiveRunDto run,
        RelicBattleTransitionResult transition,
        RelicBattleCompletionReason expectedReason) =>
        _relics.ValidateTransition(Key(run), Bindings(run), transition, expectedReason);

    public RelicRunApplyResult ApplyTransition(
        ActiveRunDto run,
        RelicBattleTransitionResult transition,
        RelicBattleCompletionReason expectedReason = RelicBattleCompletionReason.PlayerVictory) =>
        _relics.ApplyTransition(Key(run), Bindings(run), transition, expectedReason);

    private RelicRunKey Key(ActiveRunDto run, EncounterPlan? encounter = null)
    {
        if (_project.Campaign.Progression is not CompiledExpedition)
            return new(run.Seed, run.Roster[0].ContentId, run.FloorIndex, run.BattleNumber);
        var origin = encounter is not null ? RunBattlePreparationAdapter.OriginKey(run, encounter)
            : run.Action is { Phase: RunActionPhase.Battle, Source: { LegacyFloorIndex: null } source }
                ? source.ActionId : string.Empty;
        if (string.IsNullOrWhiteSpace(origin))
            throw new InvalidOperationException("Expedition battle transition has no committed action origin.");
        return new(run.Seed, run.Roster[0].ContentId, -1, run.BattleNumber) { OriginKey = origin };
    }

    private RunItemBinding[] Bindings(ActiveRunDto run) => run.Items.Select(item => new RunItemBinding(
        Required(item.ContentId),
        new ItemInstanceState
        {
            InstanceId = item.InstanceId,
            ContentId = item.ContentId,
            Stacks = item.Stacks,
            Charges = item.Charges,
            Roll = item.Roll,
            Counters = item.Counters.Select(counter => new RelicCounterStateSnapshot(
                counter.CounterId,
                counter.Value)).ToList()
        },
        _content.Graph.ResolveRelic(item.ContentId))).ToArray();

    private CatalogEntry Required(string id) => _content.TryGet(id, out var entry)
        ? entry
        : throw new InvalidOperationException($"Missing content: {id}");

    private CompiledEncounter ResolveEncounter(EncounterPlan encounter)
    {
        var matches = _project.Campaign.Encounters
            .Where(candidate => candidate.StableId == encounter.EncounterId)
            .ToArray();
        if (matches.Length != 1 || matches[0].NodeType != encounter.NodeType)
            throw new InvalidOperationException(
                $"Encounter '{encounter.EncounterId}' is not an unambiguous compiled {encounter.NodeType} encounter.");
        return matches[0];
    }

    private CompiledExpeditionEncounter? ResolveExpeditionProfile(EncounterPlan plan)
    {
        if (_project.Campaign.Progression is not CompiledExpedition expedition)
        {
            if (!string.IsNullOrEmpty(plan.ExpeditionProfileId))
                throw new InvalidOperationException("An expedition encounter cannot use tower progression.");
            return null;
        }
        if (!expedition.Encounters.TryGetValue(plan.ExpeditionProfileId, out var profile) ||
            profile.Encounter.StableId != plan.EncounterId || profile.Encounter.NodeType != plan.NodeType ||
            profile.Composition.StableId != plan.CompositionId ||
            !profile.Composition.EnemyIds.SequenceEqual(plan.EnemyIds, StringComparer.Ordinal) ||
            !profile.Composition.Cells.SequenceEqual(plan.EnemyCells) ||
            !profile.Encounter.FloorRulePool.ContentIds.Contains(plan.FloorRuleId, StringComparer.Ordinal) ||
            plan.IsBoss != (profile.Encounter.NodeType == TowerNodeType.Boss) ||
            plan.IsElite != (profile.Encounter.NodeType == TowerNodeType.Elite))
            throw new InvalidOperationException("The expedition encounter differs from its frozen authored profile.");
        return profile;
    }

    private static bool SameExpeditionPlan(EncounterPlan first, EncounterPlan second) =>
        first.ExpeditionProfileId == second.ExpeditionProfileId && first.ExpeditionSiteId == second.ExpeditionSiteId &&
        first.ExpeditionBoss == second.ExpeditionBoss && first.EncounterId == second.EncounterId &&
        first.CompositionId == second.CompositionId && first.FloorRuleId == second.FloorRuleId &&
        first.NodeType == second.NodeType && first.IsBoss == second.IsBoss && first.IsElite == second.IsElite &&
        first.EnemyIds.SequenceEqual(second.EnemyIds, StringComparer.Ordinal) && first.EnemyCells.SequenceEqual(second.EnemyCells);

    private static BossTimelineSnapshot? ToBattleTimeline(CompiledBossTimeline? timeline) => timeline is null
        ? null
        : new BossTimelineSnapshot(
            timeline.StableId,
            timeline.BossContentId,
            timeline.Phases.Select(phase => new BossPhaseSnapshot(
                phase.StableId,
                phase.DisplayName,
                phase.StartHealthRatio,
                phase.AbilityLoadout)).ToImmutableArray());
}
