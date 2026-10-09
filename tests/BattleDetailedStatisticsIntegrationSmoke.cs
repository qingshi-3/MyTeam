using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

public partial class BattleDetailedStatisticsIntegrationSmoke : Node
{
    private const int BenchmarkTicks = 300;
    public override async void _Ready()
    {
        try
        {
            VerifyActualActionClock();
            var content = await SkillSampleContent.LoadAsync(this);
            var samples = new List<Sample>();
            var observedProvenance = new HashSet<string>(StringComparer.Ordinal);
            for (var preset = 0; preset < AbilityPrototypeContent.Presets.Length; preset++)
            {
                // Warm each matched configuration before measurement; alternate measured order.
                var warmOff = Measure(content, preset, false);
                var warmOn = Measure(content, preset, true);
                Compare(warmOff, warmOn, preset);
                CollectProvenance(warmOn);
                for (var iteration = 0; iteration < 5; iteration++)
                {
                    var first = Measure(content, preset, iteration % 2 == 0);
                    var second = Measure(content, preset, iteration % 2 != 0);
                    Compare(first, second, preset);
                    CollectProvenance(first.Enabled ? first : second);
                    samples.Add(first); samples.Add(second);
                    GD.Print($"DETAILED_STATS_SAMPLE preset={preset} iteration={iteration} enabled={first.Enabled} ms={first.Milliseconds:F3} bytes={first.Bytes} steps={first.Result.Ticks}");
                    GD.Print($"DETAILED_STATS_SAMPLE preset={preset} iteration={iteration} enabled={second.Enabled} ms={second.Milliseconds:F3} bytes={second.Bytes} steps={second.Result.Ticks}");
                }
            }
            foreach (var expected in new[] { "Trait:prototype_020:Derived", "Ability:prototype_u02_a:Derived", "Ability:prototype_u17_a:ActiveSkill" })
                Require(observedProvenance.Contains(expected), "real authored extra-hit provenance was not observed: " + expected);
            foreach (var group in samples.GroupBy(sample => new { sample.Preset, sample.Enabled }))
                GD.Print($"DETAILED_STATS_MEAN preset={group.Key.Preset} enabled={group.Key.Enabled} n={group.Count()} ms={group.Average(sample => sample.Milliseconds):F3} bytes={group.Average(sample => sample.Bytes):F0}");
            GD.Print("BATTLE_DETAILED_STATISTICS_INTEGRATION_OK actual-act expiry refresh resistance dispel periodic-provenance four-presets same-seed same-digest same-legacy-result same-combat-facts exact-trait020-ability02-ability17-source paired-benchmark");
            GetTree().Quit();
            void CollectProvenance(Sample sample)
            {
                foreach (var row in sample.Result.Units.SelectMany(unit => unit.DetailedStatistics!.DamageSources))
                    if (row.Hits > 0) observedProvenance.Add($"{row.SourceKind}:{row.StableId}:{row.DamageClass}");
            }
        }
        catch (Exception exception) { GD.PrintErr("BATTLE_DETAILED_STATISTICS_INTEGRATION_FAILED " + exception); GetTree().Quit(1); }
    }
    private static void VerifyActualActionClock()
    {
        using var battle = new BattleSimulation(ClockConfig());
        var statuses = (BattleStatusScope)typeof(BattleSimulation).GetField("_statusScope", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(battle)!;
        var source = battle.UnitViews.Single(unit => unit.Team == 0).RuntimeId;
        var target = battle.UnitViews.Single(unit => unit.Team == 1).RuntimeId;
        var control = Status("integration-control", StatusBehaviorKind.DisableActions, 6) with
        { ControlDurationRule = StatusControlDurationRule.LinearResistanceCeiling };
        Require(statuses.Apply(control, source, target, 0).Status!.RemainingTicks == 3, "50% resistance resolves six authored ticks into three");
        battle.Step();
        Require(battle.Outcome == BattleOutcome.Running, "clock fixture retains a persistent player hero so status scope stays active");
        Require(Report(target).HardControlTicksTaken == 1, "actual Act counts first control step");
        Require(statuses.Apply(control, source, target, 1).Status!.RemainingTicks == 3, "refresh uses resolved duration");
        statuses.Apply(control with { StableId = "integration-overlap", DurationTicks = 2,
            ControlDurationRule = StatusControlDurationRule.None }, source, target, 1);
        battle.Step(); battle.Step(); battle.Step();
        Require(Report(target).HardControlTicksTaken == 4 && !statuses.HasTag(target, StatusDefinitionCompiler.ActionDisabledTag), "actual expiration step blocks once; refresh adds occupancy rather than declared duration");
        Require(Report(target).ControlSources.Sum(row => row.ActiveTicks) == 6 && Report(source).HardControlTicksDealt == 4,
            "actual overlapping status rows sum six while target union and source target-tick credit remain four");
        battle.Step();
        Require(Report(target).HardControlTicksTaken == 4, "first uncontrolled Act adds no control");
        statuses.Apply(control, source, target, 5);
        Require(statuses.Dispel(target, control.StableId, source), "actual dispel succeeds");
        battle.Step();
        Require(Report(target).HardControlTicksTaken == 4, "dispel cuts off future occupancy");
        var periodic = new CompiledEffectBinding("integration-periodic-binding", 0,
            new(EffectTriggerKind.Manual, EffectDomainEventKind.None), [], new CompiledOwnerTargetQuery(),
            [new CompiledEffectStep(EffectKind.Damage, EffectAmountSource.Fixed, 10, DamageType: EffectDamageType.True)], new(0, 0, 0, 0), null);
        statuses.Apply(Status("integration-periodic", StatusBehaviorKind.None, 2) with
        { PeriodicEffect = periodic, PeriodicIntervalTicks = 1 }, source, target, 6);
        battle.Step(); battle.Step();
        var row = Report(source).DamageSources.Single(bucket => bucket.StableId == "integration-periodic");
        Require(row.SourceKind == CombatSourceKind.Status && row.DamageClass == CombatDamageClass.Periodic && row.HealthDamage == 20 && row.Hits == 2, "actual status periodic provenance and damage amount");
        var final = battle.ReadUnitReports();
        battle.Abort();
        Require(battle.ReadUnitReports().Zip(final).All(pair => SameReport(pair.First, pair.Second)),
            "terminal freeze preserves live detailed metrics before status cleanup");
        BattleUnitDetailedStatisticsSnapshot Report(string id) => battle.ReadUnitReports().Single(unit => unit.RuntimeId == id).DetailedStatistics!;
    }
    private static BattleConfig ClockConfig()
    {
        UnitSnapshot Profile(string id, float resistance) => new(id, id, UnitRole.Fighter, true, false,
            1000, 0, 1, 100, 100, 0, 0, 0, 0, [], new(Stationary: true, DisableBasicAttacks: true),
            AttributeDefinition: AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute, float>
            { [CombatAttribute.MaxHealth] = 1000, [CombatAttribute.AttackDamage] = 0,
                [CombatAttribute.AttackSpeed] = 1, [CombatAttribute.MoveSpeed] = 1,
                [CombatAttribute.ControlResistance] = resistance }));
        return new() { Seed = 65432, CaptureDetailedStatistics = true,
            FloorRule = new ClearFloorRuleRuntime("clear", "常规", ""), HeroRule = HeroRuleSnapshot.Neutral,
            Spawns = [new(Profile("clock-source", 0), 0, new(2, 2), "source"),
                new(Profile("clock-target", .5f), 1, new(7, 2), "target")] };
    }
    private static CompiledStatusDefinition Status(string id, StatusBehaviorKind behavior, int ticks) => new(
        id, "", "测试状态", "", behavior, StatusDisposition.Harmful, StatusDurationKind.TimedTicks, ticks,
        StatusAggregationPolicy.BySource, 1, StatusOverflowPolicy.RejectNewStacks,
        StatusDurationRefreshPolicy.KeepLonger, StatusPeriodicResetPolicy.KeepSchedule,
        StatusDispelCategory.Ordinary, StatusDeathPolicy.Remove, StatusControlDurationRule.None,
        behavior == StatusBehaviorKind.DisableActions ? [StatusDefinitionCompiler.ActionDisabledTag] : [],
        [], 1, 0, null, [], [], null, null);
    private static Sample Measure(ContentRegistry content, int preset, bool enabled)
    {
        var config = AbilityPrototypeContent.CreateBattle(content, AbilityPrototypeContent.Presets[preset],
            AbilityPrototypeContent.Presets[(preset + 1) % AbilityPrototypeContent.Presets.Length],
            seed: 20261006, captureDetailedStatistics: enabled);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        using var battle = new BattleSimulation(config);
        for (var tick = 0; tick < BenchmarkTicks && battle.Outcome == BattleOutcome.Running; tick++) battle.Step();
        var result = battle.CreateResult();
        watch.Stop();
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(result.Units.All(unit => (unit.DetailedStatistics is not null) == enabled), "configured detail gate");
        var facts = battle.CombatEvents.ToImmutableArray();
        foreach (var fact in facts.Where(fact => fact.Kind == BattleCombatEventKind.DamageResolved &&
                     fact.ActionId.StartsWith("derived:", StringComparison.Ordinal)))
            Require(fact.Source.Kind == CombatSourceKind.Unit && fact.DamageClass == CombatDamageClass.BasicAttack,
                "report attribution must not replace existing extra-hit gameplay Unit/BasicAttack facts");
        return new(preset, enabled, watch.Elapsed.TotalMilliseconds, bytes, result, facts);
    }
    private static void Compare(Sample first, Sample second, int preset)
    {
        Require(first.Result.Digest == second.Result.Digest && first.Result.Outcome == second.Result.Outcome &&
            first.Result.Ticks == second.Result.Ticks && first.Result.GoldSpent == second.Result.GoldSpent &&
            first.Result.SuccessfulTacticalCommandUses == second.Result.SuccessfulTacticalCommandUses &&
            first.Result.PermanentGains.SequenceEqual(second.Result.PermanentGains), "detail gate changed simulation result preset " + preset);
        Require(first.Result.Units.Select(unit => unit with { DetailedStatistics = null })
            .SequenceEqual(second.Result.Units.Select(unit => unit with { DetailedStatistics = null })), "detail gate changed unit outcome/legacy aggregates preset " + preset);
        Require(first.Facts.SequenceEqual(second.Facts), "detail gate changed authoritative combat facts preset " + preset);
    }
    private sealed record Sample(int Preset, bool Enabled, double Milliseconds, long Bytes, BattleResult Result,
        ImmutableArray<BattleCombatEvent> Facts);
    private static bool SameReport(BattleUnitReportSnapshot first, BattleUnitReportSnapshot second)
    {
        var a = first.DetailedStatistics!;
        var b = second.DetailedStatistics!;
        return (first with { DetailedStatistics = null }) == (second with { DetailedStatistics = null }) &&
            (a with { DamageSources = default, HealingSources = default, ControlSources = default }) ==
                (b with { DamageSources = default, HealingSources = default, ControlSources = default }) &&
            a.DamageSources.SequenceEqual(b.DamageSources) && a.HealingSources.SequenceEqual(b.HealingSources) &&
            a.ControlSources.SequenceEqual(b.ControlSources);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
