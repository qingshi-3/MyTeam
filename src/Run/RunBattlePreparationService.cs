using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
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

    public RunBattlePreparationService(
        ContentRegistry content,
        CompiledGameProject project,
        RunRelicService relics)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _rules = project.RunRules;
        _relics = relics ?? throw new ArgumentNullException(nameof(relics));
    }

    public BattleConfig Build(ActiveRunDto run, EncounterPlan encounter, bool requireLegalFormation)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(encounter);
        if (run.Roster.Count == 0) throw new InvalidOperationException("Player roster is empty.");
        var compiledEncounter = ResolveEncounter(encounter);
        var bossTimeline = ToBattleTimeline(compiledEncounter.BossTimeline);
        if (!_project.FloorRules.TryGetValue(encounter.FloorRuleId, out var ruleScene))
            throw new InvalidOperationException($"Missing compiled floor rule: {encounter.FloorRuleId}");
        var rule = ruleScene.Instantiate<FloorRuleContentRoot>();
        try
        {
            var floorRuntime = rule.CreateRuntime();
            if (requireLegalFormation && !RunFormationPolicy.Validate(run, _rules, floorRuntime.CanOccupy))
                throw new InvalidOperationException("Player formation is invalid for the selected floor rule.");

            var relics = _relics.PrepareBattle(Key(run), Bindings(run));
            var tacticalCommands = TacticalCommandBattlePreparationBuilder.Build(run, _content.Graph);
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
                requireLegalFormation);
            return BattlePreparationAssembler.Assemble(request);
        }
        finally { rule.Free(); }
    }

    public RelicRunApplyResult ValidateTransition(
        ActiveRunDto run,
        RelicBattleTransitionResult transition,
        RelicBattleCompletionReason expectedReason) =>
        _relics.ValidateTransition(Key(run), Bindings(run), transition, expectedReason);

    public RelicRunApplyResult ApplyTransition(
        ActiveRunDto run,
        RelicBattleTransitionResult transition) =>
        _relics.ApplyTransition(Key(run), Bindings(run), transition);

    private RelicRunKey Key(ActiveRunDto run) =>
        new(run.Seed, run.Roster[0].ContentId, run.FloorIndex, run.BattleNumber);

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
        var matches = _project.Campaign.Regions
            .SelectMany(region => region.Encounters.Values)
            .Where(candidate => candidate.StableId == encounter.EncounterId)
            .ToArray();
        if (matches.Length != 1 || matches[0].NodeType != encounter.NodeType)
            throw new InvalidOperationException(
                $"Encounter '{encounter.EncounterId}' is not an unambiguous compiled {encounter.NodeType} encounter.");
        return matches[0];
    }

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
