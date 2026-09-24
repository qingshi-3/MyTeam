using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;

// Runs the shipped NE presets unchanged through publication and lab preparation.
// No frozen roster fixtures, UI commands, save service, or battle-state injection.
public partial class FirstContentPresetsContractSmoke : Node
{
    private const int TickLimit = 600;
    private const string Poison = "status_ne01_poison";
    private const string Skeleton = "soldier_bc_skeleton";
    private const string DeathBlast = "ability_ne02_temporary_death_blast";
    private const string FrostVolley = "ability_ne10_frost_volley";
    private const string Freeze = "status_ne10_freeze";
    private const string ControlShelter = "status_ne10_control_shelter";

    public override async void _Ready()
    {
        var exitCode = 0;
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(
                "Production publication failed: " + string.Join(" | ", gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var catalog = GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres")
                ?? throw new InvalidOperationException("Production battle-lab preset catalog did not load.");
            var store = new BattleLabPresetStore(catalog);
            var failures = new List<string>();

            var codes = OS.GetCmdlineUserArgs().Contains("--frost-only") ? new[] { "NE10" } : new[] { "NE01", "NE02", "NE10" };
            foreach (var code in codes)
            {
                try
                {
                    var matches = store.BuiltIns.Where(pair =>
                        pair.Key.StartsWith(code + " ·", StringComparison.Ordinal)).ToArray();
                    Require(matches.Length == 1, $"{code}: expected one shipped preset; found {matches.Length}.");
                    var snapshot = BattleLabPresetStore.ToSnapshot(matches[0].Value);
                    var config = new BattleLabPreparationAdapter(index).Build(snapshot);
                    using var battle = new BattleSimulation(config);
                    for (var tick = 0; tick < TickLimit && battle.Outcome == BattleOutcome.Running; tick++)
                        battle.Step();

                    var events = battle.CombatEvents.ToArray();
                    var context = $"{code} seed={snapshot.Seed} ticks={battle.TickIndex} outcome={battle.Outcome}" +
                        $" stop={(battle.Outcome == BattleOutcome.Running ? "tick-limit" : "battle-ended")}" +
                        $" events={events.Length}";
                    switch (code)
                    {
                        case "NE01": CheckPoison(events, context, failures); break;
                        case "NE02": CheckSummonDeaths(battle, events, context, failures); break;
                        case "NE10": CheckFrost(events, context, failures); break;
                    }
                }
                catch (Exception exception)
                {
                    var failure = $"{code}: preset preparation or simulation failed: {exception}";
                    GD.PrintErr("FIRST_CONTENT_PRESET_FAILED " + failure);
                    failures.Add(failure);
                }
            }

            Require(failures.Count == 0, string.Join("\n", failures));
            GD.Print("FIRST_CONTENT_PRESETS_CONTRACT_OK " + string.Join(" ", codes));
        }
        catch (Exception exception)
        {
            GD.PrintErr("FIRST_CONTENT_PRESETS_CONTRACT_FAILED: " + exception);
            exitCode = 1;
        }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private static void CheckPoison(BattleCombatEvent[] events, string context, List<string> failures)
    {
        var applied = StatusChanges(events, Poison);
        // This status has only a periodic damage binding; attributed DamageResolved
        // establishes actual settlement instead of merely checking authored operations.
        var ticks = PositiveEffects(events, BattleCombatEventKind.DamageResolved, Poison);
        var healthLoss = PositiveEffects(events, BattleCombatEventKind.HealthLost, Poison);
        var settledTargets = ticks.Where(tick => applied.Any(status =>
                status.TargetRuntimeId == tick.TargetRuntimeId && status.Sequence < tick.Sequence))
            .Select(tick => tick.TargetRuntimeId).Distinct(StringComparer.Ordinal).Count();
        GD.Print($"FIRST_CONTENT_PRESET {context} poison-applications={applied.Length}" +
            $" poison-damage-events={ticks.Length} poison-damage={ticks.Sum(tick => tick.EffectiveValue):0.##}" +
            $" poison-health-loss={healthLoss.Sum(tick => tick.EffectiveValue):0.##}" +
            $" poison-settled-targets={settledTargets}");
        Observe(applied.Length > 0, context, "棘毒施加或叠层", failures);
        Observe(ticks.Length > 0 && settledTargets > 0, context, "棘毒施加后实际周期伤害", failures);
    }

    private static void CheckSummonDeaths(BattleSimulation battle, BattleCombatEvent[] events,
        string context, List<string> failures)
    {
        var temporaryIds = battle.Units.Where(unit => unit.IsTemporary &&
                unit.Definition.ContentId == Skeleton && unit.InitialTeam == 0)
            .Select(unit => unit.RuntimeId).ToHashSet(StringComparer.Ordinal);
        var summons = events.Where(fact => fact.Kind == BattleCombatEventKind.UnitSummoned &&
            fact.SubjectStableId == Skeleton && temporaryIds.Contains(fact.TargetRuntimeId)).ToArray();
        var deaths = events.Where(fact => fact.Kind == BattleCombatEventKind.UnitDefeated &&
            temporaryIds.Contains(fact.TargetRuntimeId)).ToArray();
        var blasts = PositiveEffects(events, BattleCombatEventKind.DamageResolved, DeathBlast);
        var blastsAfterDeath = blasts.Count(blast => deaths.Any(death => death.Sequence < blast.Sequence));
        GD.Print($"FIRST_CONTENT_PRESET {context} temporary-skeletons={temporaryIds.Count}" +
            $" skeleton-summon-events={summons.Length} skeleton-deaths={deaths.Length}" +
            $" death-blast-damage-events={blasts.Length} death-blast-damage={blasts.Sum(blast => blast.EffectiveValue):0.##}" +
            $" death-blasts-after-skeleton-death={blastsAfterDeath}");
        Observe(summons.Length > 0, context, "实际生成己方临时砂骨仆", failures);
        Observe(deaths.Length > 0, context, "砂骨仆实际死亡", failures);
        Observe(blasts.Length > 0 && blastsAfterDeath > 0, context, "临时砂骨仆死亡后的殉爆伤害", failures);
    }

    private static void CheckFrost(BattleCombatEvent[] events, string context, List<string> failures)
    {
        var casts = events.Where(fact => fact.Kind == BattleCombatEventKind.AbilityResolved &&
            fact.SubjectStableId == FrostVolley).ToArray();
        var hits = events.Where(fact => fact.Kind == BattleCombatEventKind.SkillHitLanded &&
            fact.Source.StableId == FrostVolley).ToArray();
        var hitTargets = hits.Select(fact => fact.TargetRuntimeId).Distinct(StringComparer.Ordinal).Count();
        var targetsPerCast = casts.Select(cast =>
        {
            var nextCast = casts.Where(next => next.SourceRuntimeId == cast.SourceRuntimeId &&
                    next.Sequence > cast.Sequence).Select(next => next.Sequence).DefaultIfEmpty(long.MaxValue).Min();
            return hits.Where(hit => hit.SourceRuntimeId == cast.SourceRuntimeId &&
                    hit.Sequence > cast.Sequence && hit.Sequence < nextCast)
                .Select(hit => hit.TargetRuntimeId).Distinct(StringComparer.Ordinal).Count();
        }).DefaultIfEmpty(0).Max();
        // This fixed encounter previously exposed replacement targeting: a single
        // three-target volley silently acquired a fourth enemy after a death.
        Observe(targetsPerCast <= 3, context, "单次连射遵守三目标上限", failures);
        var freezeChanges = StatusChanges(events, Freeze);
        Observe(!events.Any(fact => fact.SubjectStableId == "status_ne10_chill"), context, "无寒意状态事件", failures);
        var controls = events.Where(fact => fact.Kind == BattleCombatEventKind.ControlApplied &&
            fact.SubjectStableId == Freeze && fact.EffectiveValue > 0).ToArray();
        var shields = PositiveEffects(events, BattleCombatEventKind.ShieldResolved, ControlShelter);
        var shieldsAfterFreeze = shields.Count(shield => controls.Any(control =>
            control.SourceRuntimeId == shield.TargetRuntimeId && control.Sequence < shield.Sequence));
        GD.Print($"FIRST_CONTENT_PRESET {context} frost-volley-casts={casts.Length}" +
            $" frost-skill-hits={hits.Length} frost-skill-targets={hitTargets}" +
            $" max-targets-in-one-cast={targetsPerCast}" +
            $" freeze-applications={freezeChanges.Length} freeze-controls={controls.Length}" +
            $" control-shield-events={shields.Length} control-shield={shields.Sum(shield => shield.EffectiveValue):0.##}" +
            $" self-shields-after-freeze={shieldsAfterFreeze}");
        Observe(casts.Length > 0 && hits.Length > 0, context, "霜羽主动施放后真实技能箭命中", failures);
        Observe(targetsPerCast > 1, context, "同次霜羽连射的技能箭实际覆盖多个敌人", failures);
        Observe(freezeChanges.Length > 0 && controls.Length > 0, context, "冰冻状态及成功控制事件", failures);
        Observe(shields.Length > 0 && shieldsAfterFreeze > 0, context, "冰冻成功后给控制者的新护盾", failures);
    }

    private static BattleCombatEvent[] StatusChanges(IEnumerable<BattleCombatEvent> events, string statusId) =>
        events.Where(fact => fact.SubjectStableId == statusId &&
            fact.Kind is BattleCombatEventKind.StatusApplied or BattleCombatEventKind.StatusStackChanged).ToArray();

    private static BattleCombatEvent[] PositiveEffects(IEnumerable<BattleCombatEvent> events,
        BattleCombatEventKind kind, string sourceId) => events.Where(fact =>
            fact.Kind == kind && fact.Source.StableId == sourceId && fact.EffectiveValue > 0).ToArray();

    private static void Observe(bool observed, string context, string behavior, List<string> failures)
    {
        if (observed) return;
        var message = $"{context}: 未观察到{behavior}。保留预设的原始阵容、属性、技能和种子；" +
            "可能是接线问题，或本次战斗在触发前结束，需按上述事件统计判断。";
        GD.PrintErr("FIRST_CONTENT_PRESET_MISSING " + message);
        failures.Add(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
