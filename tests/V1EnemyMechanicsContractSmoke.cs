using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;

public partial class V1EnemyMechanicsContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            AuthoringAndClock(); AttackCycle(); SeatProtection(); Quake(); Afterglow(); RealGuardAndArmor();
            GD.Print("V1_ENEMY_MECHANICS_OK authored first-tick fixed-clock miss-not-queued cycle-cancel checkpoint guard-initial-identity-death quake-three-center-pulses armor-completion afterglow-third-pulse-fixed-area-periodic-owner-death rollback-retry");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("V1_ENEMY_MECHANICS_FAILED " + e); GetTree().Quit(1); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Near(float actual, float expected, string message) => Require(Math.Abs(actual - expected) < .001f, $"{message}: {actual} != {expected}");
    private static CompiledAbilityDefinition Compile(AbilityOperationSpec op, string id, bool entry = false, int first = 0, int interval = 0)
    {
        var result = AbilityDefinitionCompiler.Compile(new AbilityDefinition
        {
            StableId = id, DisplayName = id, ActivationKind = entry ? AbilityActivationKind.Triggered : AbilityActivationKind.Automatic,
            Trigger = entry ? AbilityTriggerKind.OwnerEnteredBattle : AbilityTriggerKind.PeriodicTick,
            MaxUses = entry ? 1 : 0, FirstTriggerTick = first, IntervalTicks = interval, Operations = [op]
        });
        Require(!result.Report.HasCoreErrors && result.Ability is not null, "compile: " + string.Join(";", result.Report.CoreErrors));
        return result.Ability!;
    }
    private static void AuthoringAndClock()
    {
        var quake = Compile(new EnemyStoneQuakeSpec(), "test_quake", first: 50, interval: 80);
        Require(quake.FirstTriggerTick == 50 && quake.Operations[0] is CompiledEnemyStoneQuake, "typed quake compiler");
        Require(!Compile(new EnemySeatGuardSpec(), "test_guard", true).IsDisplayedActiveSkill, "guard is passive in UI");
        Compile(new EnemyOriginalAttackCycleSpec(), "test_cycle", true);
        Compile(new ConeBreathAbilityOperationSpec { AfterglowDurationTicks = 15, AfterglowAttackMultiplier = .2f }, "test_cone", interval: 90);
        var invalid = AbilityDefinitionCompiler.Compile(new AbilityDefinition { StableId = "bad_first", DisplayName = "错误",
            ActivationKind = AbilityActivationKind.Triggered, Trigger = AbilityTriggerKind.OwnerEnteredBattle,
            FirstTriggerTick = 30, MaxUses = 1, Operations = [new EnemyOriginalAttackCycleSpec()] });
        Require(invalid.Report.HasCoreErrors, "first tick is automatic periodic only");
        var probe = new AbilityProbe();
        using var scope = new BattleAbilityScope("fixed-clock", probe, 0);
        scope.RegisterLoadout("owner", new([quake with { FirstTriggerTick = 30, IntervalTicks = 90 }]));
        for (var tick = 0; tick <= 210; tick++)
        {
            probe.Blocked = tick == 120;
            scope.ActivateAutomatic("owner", tick);
        }
        Require(probe.Attempts.SequenceEqual(new[] { 30, 120, 210 }) && probe.Commits.SequenceEqual(new[] { 30, 210 }),
            "periodic requests must keep authored phase and never catch up a blocked request");
        var legacy = new AbilityProbe(); using var legacyScope = new BattleAbilityScope("old-clock", legacy, 0);
        legacyScope.RegisterLoadout("owner", new([quake with { FirstTriggerTick = 0, IntervalTicks = 10 }]));
        for (var tick = 0; tick <= 20; tick++) legacyScope.ActivateAutomatic("owner", tick);
        Require(legacy.Commits.SequenceEqual(new[] { 10, 20 }), "default first tick preserves original schedule");
    }
    private static void AttackCycle()
    {
        using var w = new World(); var cycle = new CompiledEnemyOriginalAttackCycle(4, 4, 1.8f);
        w.Scope.Begin("owner", "owner", cycle, "cycle", CombatSourceRef.System("cycle"));
        for (var i = 0; i < 3; i++) Require(w.Scope.PrepareOriginalAttack("owner").WindupTicks == 0, "first three are normal");
        var checkpoint = w.Scope.Capture();
        var fourth = w.Scope.PrepareOriginalAttack("owner"); Require(fourth.WindupTicks == 4 && fourth.DamageMultiplier == 1.8f, "fourth preparation");
        w.Scope.Cancel("owner"); Require(w.Scope.PrepareOriginalAttack("owner").WindupTicks == 0, "cancellation cannot refund cycle");
        w.Scope.Restore(checkpoint); Require(w.Scope.PrepareOriginalAttack("owner") == fourth, "world rollback restores cycle");
    }
    private static void SeatProtection()
    {
        using var w = new World(); w.Owner.Position = new(8, 2);
        var a = w.Add("guard", 0, new(6, 0)); w.Add("temporary", 0, new(6, 2), temporary: true);
        w.Add("late", 0, new(6, 4), initial: false);
        var rule = new CompiledEnemySeatGuard([new(6, 0), new(6, 2), new(6, 4)], .8f);
        w.Scope.Begin("owner", "owner", rule, "guard", CombatSourceRef.System("guard"));
        Near(w.Incoming("guard"), 80, "initial guard protected"); Near(w.Incoming("temporary"), 100, "temporary excluded"); Near(w.Incoming("late"), 100, "later native excluded");
        a.Position = new(4, 4); w.Scope.Cancel("owner"); Near(w.Incoming("guard"), 80, "movement/control retains identity");
        var checkpoint = w.Scope.Capture(); w.Scope.ObserveDefeated("guard"); Near(w.Incoming("guard"), 100, "defeat permanently unbinds even if revived");
        w.Scope.Restore(checkpoint); Near(w.Incoming("guard"), 80, "binding checkpoint restores");
        w.Owner.Health = 0; Near(w.Incoming("guard"), 100, "source death invalidates before callback");
        w.Scope.ObserveDefeated("owner"); w.Owner.Health = 100; Near(w.Incoming("guard"), 100, "source revive never rebinds");
    }
    private static CompiledEnemyStoneQuake QuakeRule => new(2, 7, 5, 3, 5, .7f, 8, 4, 60);
    private static void Quake()
    {
        using (var w = new World())
        {
            w.Add("outside", 1, new(4.01f, 2));
            w.Scope.Begin("owner", "target", QuakeRule, "quake", CombatSourceRef.System("quake"));
            w.At(6); Require(w.Hits.Count == 0, "quake windup"); w.At(7);
            Require(w.Hits.Count == 1 && w.Hits[0].Target == "target" && w.Hits[0].Area, "quake uses centers not body expansion");
            Near(w.Hits[0].Damage, 31, "quake reads .7A+8D"); w.Owner.Attributes.SetBaseValue(CombatAttribute.Armor, 5);
            w.At(12); Near(w.Hits[1].Damage, 47, "each pulse reads current armor"); w.At(17); w.At(21);
            Require(w.Hits.Count == 3 && w.ArmorAwards == 0 && w.Scope.IsBusy("owner"), "hold through recovery"); w.At(22);
            Require(w.ArmorAwards == 1 && !w.Scope.IsBusy("owner"), "completed channel awards armor once");
        }
        using (var w = new World())
        {
            w.Scope.Begin("owner", "target", QuakeRule, "quake", CombatSourceRef.System("quake"));
            w.At(7); w.ControlledOwner = true; w.At(8); w.ControlledOwner = false; w.At(22);
            Require(w.Hits.Count == 1 && w.ArmorAwards == 0, "interrupt cancels remaining pulses and completion armor");
        }
        using (var w = new World())
        {
            w.Scope.Begin("owner", "target", QuakeRule, "quake", CombatSourceRef.System("quake"));
            w.FailHit = true; ExpectFailure(() => w.At(7)); Require(w.Hits.Count == 0, "failed pulse rolls back");
            w.FailHit = false; w.At(7); Require(w.Hits.Count == 1, "failed pulse can retry once");
        }
    }
    private static CompiledConeBreath Cone => new(4, 70, 2, 2, 3, 2, 1, "cone_breath", 15, 5, .2f);
    private static void Afterglow()
    {
        using (var w = new World())
        {
            w.Scope.Begin("owner", "target", Cone, "breath", CombatSourceRef.System("breath"));
            w.At(2); w.At(4); w.ControlledOwner = true; w.At(6); w.ControlledOwner = false; w.At(30);
            Require(w.Hits.Count == 2 && w.Events.All(f => f.Type != "cone_afterglow_start"), "interrupted third pulse produces no afterglow");
        }
        using (var w = new World())
        {
            w.Scope.Begin("owner", "target", Cone, "breath", CombatSourceRef.System("breath")); w.At(2); w.At(4);
            w.FailHit = true; ExpectFailure(() => w.At(6)); Require(w.Hits.Count == 2 && w.Events.All(f => f.Type != "cone_afterglow_start"), "third-pulse fault has no released field");
            w.FailHit = false; w.At(6); Require(w.Events.Count(f => f.Type == "cone_afterglow_start") == 1, "successful third pulse releases field once");
            w.Owner.Position = new(8, 4); w.Owner.Health = 0; w.Owner.Damage = 999; w.ControlledOwner = true;
            w.At(10); Require(w.Hits.Count == 3, "field waits five ticks");
            w.FailHit = true; ExpectFailure(() => w.At(11)); Require(w.Hits.Count == 3, "afterglow fault rollback");
            w.FailHit = false; w.At(11); w.At(16); w.At(21); w.At(26);
            var periodic = w.Hits.Where(h => h.Periodic).ToArray();
            Require(periodic.Length == 3 && periodic.All(h => h.Target == "target" && h.Area), "released field survives death, keeps original cone and ends after three ticks");
            Require(periodic.All(h => Math.Abs(h.Damage - 2) < .001f), "released afterglow snapshots attack damage");
            Require(w.Events.Count(f => f.Type == "enemy_action" && f.EnemyAction?.Stage == "release") == 1, "afterglow never restarts breath release");
        }
    }
    private static void RealGuardAndArmor()
    {
        var guard = Compile(new EnemySeatGuardSpec(), "real_guard", true);
        var quake = Compile(new EnemyStoneQuakeSpec(), "real_quake", first: 1, interval: 80);
        var idle = new UnitSnapshot("probe", "验证单位", UnitRole.Fighter, false, false, 10000, 10, .35f, 10000, 10000, 3, 0, 0, 0, [],
            new(Stationary: true, DisableBasicAttacks: true));
        using var battle = new BattleSimulation(new BattleConfig { Seed = 7, HeroRule = HeroRuleSnapshot.Neutral,
            FloorRule = new ClearFloorRuleRuntime("probe", "验证", ""), Spawns = [
                new(idle with { AbilityLoadout = new([guard, quake]) }, 1, new(8, 2), "boss"),
                new(idle, 1, new(6, 0), "guard"), new(idle with { IsHero = true }, 0, new(7, 3), "target", IsPersistentRosterHero: true)] });
        Require(battle.Units.Single(u => u.RuntimeId == "guard").Statuses.Any(s => s.StableId == "enemy_seat_guard" && s.Permanent), "real owner entry grants permanent readable marker");
        for (var step = 0; step < 24 && battle.Outcome == BattleOutcome.Running; step++) battle.Step();
        Require(battle.Outcome == BattleOutcome.Running && battle.TickIndex == 24,
            $"real quake fixture must remain active through completion: tick={battle.TickIndex}, outcome={battle.Outcome}");
        Near(battle.Units.Single(u => u.RuntimeId == "boss").Armor, 7, "real complete quake grants +4 armor through Status");
        Require(battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.DamageResolved && e.SourceRuntimeId == "boss") == 3, "real quake has exactly three damage resolutions");
    }
    private static void ExpectFailure(Action action) { try { action(); throw new Exception("fault absent"); } catch (InvalidOperationException e) when (e.Message == "injected") { } }
    private sealed record Hit(string Target, float Damage, bool Area, bool Periodic);
    private sealed class World : IBattleEnemyQueries, IBattleEnemyCommands, IBattleEnemyTransactions, IDisposable
    {
        private readonly BattleAttributeScope _attributes = new("v1-enemy");
        private readonly List<BattleUnitState> _units = [];
        private readonly HashSet<string> _initial = [];
        public readonly BattleEnemyActionScope Scope;
        public readonly BattleUnitState Owner;
        public readonly List<Hit> Hits = [];
        public readonly List<BattleEvent> Events = [];
        public int ArmorAwards; public bool FailHit; public bool ControlledOwner;
        public World() { Owner = Add("owner", 0, new(2, 2)); Add("target", 1, new(4, 2)); Scope = new(this, this, this); }
        public BattleUnitState Add(string id, int team, Vector2 at, bool temporary = false, bool initial = true)
        {
            var unit = new BattleUnitState { RuntimeId = id, SourceInstanceId = id, Team = team, InitialTeam = team,
                Cell = Vector2I.Zero, Position = at, Health = 100, IsTemporary = temporary,
                Definition = new(id, id, UnitRole.Fighter, false, false, 100, 10, .35f, 10, 10, 3, 0, 0, 0, [], new()),
                Attributes = _attributes.CreateSet(id, new([new(CombatAttribute.MaxHealth, 100, 0, 1000),
                    new(CombatAttribute.AttackDamage, 10, 0, 1000), new(CombatAttribute.Armor, 3, 0, 1000)], id)) };
            _units.Add(unit); if (initial) _initial.Add(id); return unit;
        }
        public float Incoming(string id) => Scope.ModifyIncomingDamage(new(BattleCombatCalculationKind.Damage, CombatSourceRef.System("probe"), "target", id, Tick, 100), 100);
        public void At(int tick) { Tick = tick; Scope.Advance(); }
        public int Tick { get; private set; }
        public IReadOnlyList<BattleUnitView> Units => _units.Select(BattleUnitView.For).ToArray();
        public IReadOnlyDictionary<string, UnitSnapshot> Profiles { get; } = new Dictionary<string, UnitSnapshot>();
        public IReadOnlyList<DisplacementReservation> Reservations => [];
        public bool CanOccupyTerrain(Vector2I cell) => true;
        public bool Controlled(string id) => id == "owner" && ControlledOwner;
        public bool Displacing(string id) => false;
        public bool Airborne(string id) => false;
        public bool HasAttackInFlight(string id) => false;
        public CompiledBroodPhase? Brood(string id) => null;
        public bool IsInitialNative(string id) => _initial.Contains(id);
        public bool HasLineAccess(Vector2 from, Vector2 to) => true;
        public bool CanOccupy(Vector2 position, float radius) => true;
        public void BeginCast(string ownerId, string targetId, string displayName) { }
        public void ContinueCast(string ownerId) { }
        public void Hit(string sourceId, string targetId, float damage, CombatSourceRef origin, EffectDamageType type, bool isAreaDamage = false)
        { Hits.Add(new(targetId, damage, isAreaDamage, false)); if (FailHit) throw new InvalidOperationException("injected"); }
        public void PeriodicHit(string sourceId, string targetId, float damage, CombatSourceRef origin)
        { Hits.Add(new(targetId, damage, true, true)); if (FailHit) throw new InvalidOperationException("injected"); }
        public void SeatGuardMarker(string sourceId, string targetId, bool active) { }
        public void CompletionArmor(string ownerId, float amount, int ticks) { Require(amount == 4 && ticks == 60, "completion armor authored values"); ArmorAwards++; }
        public string? Spawn(UnitSnapshot profile, int team, Vector2 position, string ownerId) => throw new NotSupportedException();
        public void Swap(string firstId, string secondId) => throw new NotSupportedException();
        public void InterruptForForm(string ownerId) => throw new NotSupportedException();
        public void TransformBrood(string ownerId, float radius, float reach) => throw new NotSupportedException();
        public void Retreat(string ownerId, string targetId, Vector2 start, Vector2 end, float distance) => throw new NotSupportedException();
        public void Retire(string unitId, string ownerId, string reason) => throw new NotSupportedException();
        public void Present(BattleEvent fact) => Events.Add(fact);
        public void Resolve(Action work) => CheckpointOnly(work);
        public void ResolveHit(Action work) => work();
        public void CheckpointOnly(Action work)
        {
            var checkpoint = Scope.Capture(); var hits = Hits.Count; var facts = Events.Count; var armor = ArmorAwards;
            try { work(); } catch { Scope.Restore(checkpoint); Hits.RemoveRange(hits, Hits.Count - hits); Events.RemoveRange(facts, Events.Count - facts); ArmorAwards = armor; throw; }
        }
        public void Dispose() => _attributes.Dispose();
    }
    private sealed class AbilityProbe : IAbilityRuntimeWorld
    {
        public bool Blocked; public readonly List<int> Attempts = []; public readonly List<int> Commits = [];
        public AbilityWorldSnapshot CaptureSnapshot(int tick) => new(tick,
            new[] { new AbilityEntitySnapshot("owner", 0, true, 100, []) }.ToImmutableDictionary(e => e.RuntimeId));
        public AbilityPreparationResult Prepare(CompiledAbilityDefinition ability, string sourceId, string ownerId, string explicitTargetId, int tick)
        {
            Attempts.Add(tick);
            if (Blocked) return new(false, AbilityActivationFailure.ConditionsUnmet, "blocked", null);
            return new(true, AbilityActivationFailure.None, "", new(ability, sourceId, ownerId, tick,
                ability.Operations.Select((op, i) => new ResolvedAbilityOperation(i, op, [ownerId], 0)).ToImmutableArray(), [], 0));
        }
        public AbilityCommitResult Commit(AbilityExecutionPlan plan) { Commits.Add(plan.Tick); return new(true, AbilityActivationFailure.None, "", []); }
    }
}
