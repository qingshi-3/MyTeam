using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

public partial class BattleDetailedStatisticsContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            VerifyVitality();
            VerifyControlsAndCheckpoint();
            VerifyStatusClock();
            VerifyBoundsAndOptOut();
            GD.Print("BATTLE_DETAILED_STATISTICS_OK hp-shield-overkill useful-wasted-heal control-union contributor-overlap legacy-self-lock revive frozen-report nested-cow-rollback bounded-sources opt-out causal-report-only");
            GetTree().Quit();
        }
        catch (Exception exception) { GD.PrintErr("BATTLE_DETAILED_STATISTICS_FAILED " + exception); GetTree().Quit(1); }
    }
    private static void VerifyVitality()
    {
        using var world = new World();
        var stats = new BattleStatistics(true);
        foreach (var unit in world.Units) stats.Join(unit.RuntimeId, 0);
        using var pipeline = new BattleCombatEventPipeline("stats-vitality");
        using var statuses = new BattleStatusScope("stats-statuses", (_, _) => { }, id => world.Unit(id).Attributes, _ => true, null, null);
        var resolver = new BattleVitalityScope(world, new BattleConfig
        { FloorRule = new ClearFloorRuleRuntime("clear", "常规", ""), HeroRule = HeroRuleSnapshot.Neutral }, pipeline, stats, statuses);
        var target = world.Unit("target");
        target.Health = 70;
        var origin = new CombatSourceRef(CombatSourceKind.Ability, "damage", "source", "cast:1");
        resolver.Shield("source", target, 20, origin);
        resolver.Damage("source", world.Unit("source"), target, 40, origin, EffectDamageType.True, CombatDamageClass.ActiveSkill);
        resolver.Heal("source", target, 100, origin);
        Require(target.Health == 100 && target.Shield == 0, "vitality preparation state");
        resolver.Damage("source", world.Unit("source"), target, 130, origin, EffectDamageType.True, CombatDamageClass.ActiveSkill);
        var report = stats.ReadReports(world.Views, _ => 0);
        var source = report[0].DetailedStatistics!;
        var victim = report[1].DetailedStatistics!;
        Require(report[0].DamageDealt == 140 && source.HealthDamageDealt == 120 && source.ShieldDamageDealt == 20 && source.OverkillDamageDealt == 30, "HP/shield/overkill decomposition at actual resolver");
        Require(victim.HealthDamageTaken == 120 && report[1].ShieldAbsorbed == 20, "received HP and shield totals");
        Require(report[0].HealingDone == 50 && source.HealingWasted == 50 && victim.HealingReceived == 50 && source.ShieldGranted == 20, "useful healing versus overheal and shield grant");
        Require(source.DamageSources.Single().Hits == 2 && source.DamageSources.Single().DamageType == EffectDamageType.True, "stable source and type breakdown");
        stats.Heal("source", "target", 0, 10, origin);
        Require(stats.ReadReports(world.Views, _ => 0)[0].DetailedStatistics!.HealingSources.Single().WastedHealing == 60, "zero useful heal still records waste");
        var cause = new CombatSourceRef(CombatSourceKind.Trait, "prototype_020", "source", "authored-trait");
        resolver.Damage("source", world.Unit("source"), world.Unit("other"), 10, origin,
            EffectDamageType.True, CombatDamageClass.BasicAttack, "unchanged-action",
            new(cause, CombatDamageClass.Derived));
        var classified = stats.ReadReports(world.Views, _ => 0)[0].DetailedStatistics!.DamageSources
            .Single(row => row.SourceKind == CombatSourceKind.Trait);
        var fact = world.Facts.Last(row => row.Kind == BattleCombatEventKind.DamageResolved);
        Require(classified.StableId == "prototype_020" && classified.DamageClass == CombatDamageClass.Derived &&
            classified.HealthDamage == 10 && fact.Source == origin && fact.DamageClass == CombatDamageClass.BasicAttack &&
            fact.ActionId == "unchanged-action", "statistics causal override cannot modify resolved combat fact provenance");
    }
    private static void VerifyControlsAndCheckpoint()
    {
        using var world = new World();
        var stats = new BattleStatistics(true);
        foreach (var unit in world.Units) stats.Join(unit.RuntimeId, 0);
        var status = Control("stun", "source", [new("source", 1, 0, 1), new("other", 2, 0, 1)]);
        var duplicate = status with { InstanceId = "another-stack" };
        var second = Control("freeze", "source", []);
        stats.AdvanceTick(1);
        stats.ControlTick("target", [status, duplicate, second], true, true, 1);
        stats.AdvanceTick(2);
        stats.ControlTick("target", [status], true, false, 2);
        var before = stats.ReadReports(world.Views, _ => 2);
        var target = before[1].DetailedStatistics!;
        Require(target.HardControlTicksTaken == 2 && target.LegacyActionLockTicks == 1, "hard control union excludes legacy lock");
        Require(target.ControlSources.Where(row => row.StableId == "stun").All(row => row.ActiveTicks == 2) && target.ControlSources.Single(row => row.StableId == "freeze").ActiveTicks == 1, "duplicates merge while different controls overlap");
        Require(before[0].DetailedStatistics!.HardControlTicksDealt == 2 && before[2].DetailedStatistics!.HardControlTicksDealt == 2, "aggregated contributors get overlapping occupancy credit once per target/tick");
        var outer = stats.Capture();
        stats.Damage("source", "target", 10, 0, false, 10, 0, CombatSourceRef.System("first"));
        var inner = stats.Capture();
        stats.Damage("source", "target", 20, 0, false, 20, 0, CombatSourceRef.System("second"));
        stats.ControlTick("target", [status], true, false, 3);
        stats.Restore(inner);
        Require(stats.ReadReports(world.Views, _ => 2)[0].DetailedStatistics!.DamageSources.Single().HealthDamage == 10, "inner checkpoint copied source maps on write");
        stats.Restore(outer);
        Require(stats.ReadReports(world.Views, _ => 2)[0].DetailedStatistics!.DamageSources.IsEmpty && stats.ReadReports(world.Views, _ => 2)[1].DetailedStatistics!.HardControlTicksTaken == 2, "outer checkpoint retained pristine metrics");
        stats.Defeat("target", 4);
        stats.AdvanceTick(7);
        stats.Observe(new(BattleCombatEventKind.UnitRevived, default, "target", "target", 7));
        stats.AdvanceTick(10);
        stats.Join("summon", 10);
        Require(stats.ReadReports(world.Views, _ => 10)[1].DetailedStatistics!.AliveTicks == 7, "revive excludes three dead simulated ticks");
        foreach (var kind in new[] { BattleCombatEventKind.AttackLanded, BattleCombatEventKind.AttackDodged, BattleCombatEventKind.CriticalHit, BattleCombatEventKind.AbilityResolved, BattleCombatEventKind.ManaSkillResolved })
            stats.Observe(new(kind, default, "source", "source", 10));
        var actions = stats.ReadReports(world.Views, _ => 10)[0].DetailedStatistics!;
        Require(actions.LandedAttacks == 1 && actions.DodgedAttacks == 1 && actions.CriticalHits == 1 && actions.AbilityResolutions == 1 && actions.ManaCasts == 1, "resolved action counters");
        stats.Freeze(world.Views, _ => 10);
        var frozen = stats.ReadReports(world.Views, _ => 10);
        stats.ControlTick("target", [status], true, true, 11);
        stats.Damage("source", "target", 10, 0, false, 10);
        stats.AdvanceTick(12);
        Require(stats.ReadReports(world.Views, _ => 12).Equals(frozen), "frozen immutable report survives cleanup and subsequent writes");
        stats.Restore(outer);
        var failed = false;
        try { new BattleStatistics(true).Restore(outer); } catch (InvalidOperationException) { failed = true; }
        Require(failed, "foreign checkpoint rejected");
    }
    private static void VerifyBoundsAndOptOut()
    {
        using var world = new World();
        var stats = new BattleStatistics(true);
        var compact = new BattleStatistics();
        foreach (var unit in world.Units) { stats.Join(unit.RuntimeId, 0); compact.Join(unit.RuntimeId, 0); }
        for (var index = 0; index < 200; index++)
        {
            var origin = new CombatSourceRef(CombatSourceKind.Ability, "source-" + index, "source", "cast-" + index);
            stats.Damage("source", "target", 1, 0, false, 1, 0, origin);
            stats.Heal("source", "target", 1, 2, origin);
            stats.ControlTick("target", [Control("control-" + index, "source", [])], true, false, index + 1);
            compact.Damage("source", "target", 1, 0, false, 1, 0, origin);
        }
        var details = stats.ReadReports(world.Views, _ => 0)[0].DetailedStatistics!;
        Require(details.DamageSources.Length == 65 && details.HealingSources.Length == 65 && details.DamageSources.Sum(row => row.HealthDamage) == 200 && details.HealingSources.Sum(row => row.WastedHealing) == 200, "bounded sources preserve numerical totals in overflow");
        Require(stats.ReadReports(world.Views, _ => 0)[1].DetailedStatistics!.ControlSources.Length == 65, "bounded control source rows");
        Require(compact.ReadReports(world.Views, _ => 0).All(row => row.DetailedStatistics is null) && compact.ReadReports(world.Views, _ => 0)[0].DamageDealt == 200, "opt-out retains compact aggregate");
        var justJoined = new BattleStatistics(true);
        justJoined.Join("source", 0); justJoined.Join("target", 10); justJoined.Join("other", 0);
        justJoined.AdvanceTick(10);
        justJoined.ControlTick("target", [Control("same-tick", "source", [])], true, false, 10);
        var joinDetail = justJoined.ReadReports(world.Views, _ => 0)[1].DetailedStatistics!;
        Require(joinDetail.AliveTicks == 0 && joinDetail.HardControlTicksTaken == 1,
            "same-tick join can participate in one controlled action step before an elapsed lifetime tick; these metrics must not form a percentage");
    }
    private static void VerifyStatusClock()
    {
        using var world = new World();
        var stats = new BattleStatistics(true);
        foreach (var unit in world.Units) stats.Join(unit.RuntimeId, 0);
        using var statuses = new BattleStatusScope("stats-clock", (id, snapshot) => world.Unit(id).Statuses = snapshot,
            id => world.Unit(id).Attributes, _ => true, null, null);
        var definition = new CompiledStatusDefinition("clock-stun", "", "眩晕", "", StatusBehaviorKind.DisableActions,
            StatusDisposition.Harmful, StatusDurationKind.TimedTicks, 3, StatusAggregationPolicy.BySource, 1,
            StatusOverflowPolicy.RejectNewStacks, StatusDurationRefreshPolicy.KeepLonger,
            StatusPeriodicResetPolicy.KeepSchedule, StatusDispelCategory.Ordinary, StatusDeathPolicy.Remove,
            StatusControlDurationRule.None, [StatusDefinitionCompiler.ActionDisabledTag], [], 1, 0, null, [], [], null, null);
        Require(statuses.Apply(definition, "source", "target", 0).Applied, "clock status applied");
        for (var tick = 1; tick <= 6; tick++)
        {
            var blocked = statuses.HasTag("target", StatusDefinitionCompiler.ActionDisabledTag);
            stats.AdvanceTick(tick);
            stats.ControlTick("target", world.Unit("target").Statuses, blocked, false, tick);
            statuses.AdvanceOwner("target", tick);
            if (tick == 2) statuses.Apply(definition, "source", "target", tick);
        }
        Require(stats.ReadReports(world.Views, _ => 6)[1].DetailedStatistics!.HardControlTicksTaken == 5,
            "refresh extends occupancy to expiration step without counting declared duration twice");
        Require(!statuses.HasTag("target", StatusDefinitionCompiler.ActionDisabledTag), "expired control removed");
        statuses.Apply(definition, "source", "target", 6);
        Require(statuses.Dispel("target", definition.StableId, "source"), "clock dispel");
        stats.ControlTick("target", world.Unit("target").Statuses,
            statuses.HasTag("target", StatusDefinitionCompiler.ActionDisabledTag), false, 7);
        Require(stats.ReadReports(world.Views, _ => 7)[1].DetailedStatistics!.HardControlTicksTaken == 5,
            "dispelled future duration adds no occupancy");
    }
    private static StatusRuntimeSnapshot Control(string id, string source, ImmutableArray<StatusSourceContributionSnapshot> contributions) =>
        new(id, "眩晕", "", source, "target", id, 1, 0, 0, 1, 10, false, true,
            StatusDurationKind.TimedTicks, StatusAggregationPolicy.ByTarget, StatusDispelCategory.Ordinary,
            StatusDeathPolicy.Remove, StatusBehaviorKind.DisableActions, StatusDisposition.Harmful, 1,
            [StatusDefinitionCompiler.ActionDisabledTag], contributions, [], "", "控制");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class World : IBattleVitalityWorld, IDisposable
    {
        private readonly BattleAttributeScope _attributes = new("stats-unit-attributes");
        private readonly List<BattleUnitState> _units = [];
        public readonly List<BattleCombatEventDraft> Facts = [];
        public IReadOnlyList<BattleUnitState> Units => _units;
        public IReadOnlyList<BattleUnitView> Views => _units.Select(BattleUnitView.For).ToArray();
        public int Tick => 1;
        public World()
        {
            foreach (var (id, team) in new[] { ("source", 0), ("target", 1), ("other", 0) })
            {
                var profile = new UnitSnapshot(id, id, UnitRole.Fighter, true, false, 100, 10, 1, 10, 1, 0, 0, 0, 0, [], new());
                _units.Add(new() { RuntimeId = id, SourceInstanceId = id, Definition = profile, Team = team, Cell = new(team + 1, 2), Health = 100,
                    Attributes = _attributes.CreateSet(id, BattleUnitFactory.CreateAttributes(profile, 100, 10, 0)) });
            }
        }
        public BattleUnitState Unit(string id) => _units.Single(unit => unit.RuntimeId == id);
        public float ModifyIncomingDamage(BattleUnitState target, float amount) => amount;
        public float Armor(BattleUnitState target) => 0;
        public CombatSourceRef Source(string id, BattleUnitState? explicitUnit = null) => CombatSourceRef.System("test");
        public CombatDamageClass DamageClass(CombatSourceRef source) => CombatDamageClass.Other;
        public void ShieldConsumed(BattleUnitState target, float amount, bool enemyDamage, CombatSourceRef origin) { }
        public void HealthChanged(BattleUnitState target) { }
        public void BeforeDeath(BattleUnitState target) { }
        public void CancelProjectiles(BattleUnitState target) { }
        public void CancelActionsAndGrants(BattleUnitState target) { }
        public void ReleaseMovement(string id) { }
        public void ReleaseBindings(string id) { }
        public bool SpawnNear(UnitSnapshot? profile, int team, Vector2 position, float health, float damage, string source) => false;
        public void Publish(BattleCombatEventDraft fact) => Facts.Add(fact);
        public void Present(string type, string source, string target, float value, Vector2 position, string cue, BattleVfxCue? vfx = null) { }
        public void Dispose() => _attributes.Dispose();
    }
}
