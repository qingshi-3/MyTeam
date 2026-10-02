using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.App;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

public partial class MatrixReactionModuleContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            using var world = new World();
            var queries = new BattleMatrixQueries(world);
            var reactions = new BattleMatrixReactionScope(world, queries);
            world.Reactions = reactions;
            using var damage = new MatrixAbilityOperationSpec { Kind = MatrixOperationKind.Damage, Amount = 3 };
            using var authored = new MatrixAbilityOperationSpec
            {
                Kind = MatrixOperationKind.RegisterReaction,
                Event = MatrixEventKind.AttackHit,
                SourceRelation = MatrixRelation.Owner,
                OncePerAction = true,
                Every = 2,
                Effects = [damage]
            };
            var report = new ValidationReport();
            var rule = MatrixAbilityCompiler.Compile(authored, "module", report)
                ?? throw new InvalidOperationException("fixture compile returned no operation");
            Require(!report.HasCoreErrors, "fixture compile");
            reactions.Register("owner", "probe", rule, CombatSourceRef.System("probe"));
            var checkpoint = reactions.Capture();
            // Count unique victims within one action. Duplicate facts cannot reach Every=2.
            reactions.Observe(Fact(1, "a")); reactions.Observe(Fact(2, "a")); reactions.Drain();
            Require(world.Executions == 0, "duplicate victim advanced action counter");
            reactions.Observe(Fact(3, "b"));
            world.FailExecution = true;
            try { reactions.Drain(); throw new Exception("expected controlled rejection"); }
            catch (InvalidOperationException e) when (e.Message == "controlled effect rejection") { }
            Require(world.Executions == 0, "failed reaction published an effect");
            world.FailExecution = false;
            reactions.Observe(Fact(4, "b")); reactions.Drain();
            Require(world.Executions == 1, "rollback retained once-per-action or cooldown state");
            reactions.Observe(Fact(5, "b")); reactions.Drain();
            Require(world.Executions == 1, "completed action repeated");
            var other = new BattleMatrixReactionScope(world, queries);
            var rejected = false;
            try { other.Restore(checkpoint); } catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "cross-scope checkpoint accepted");
            reactions.Clear(); reactions.Clear();
            Require(!reactions.WantsEvent(Fact(6, "a")), "clear retained subscriptions");
            reactions.Restore(checkpoint);
            var selected = queries.Select(rule with { Target = MatrixTargetKind.NearestEnemies, MaxTargets = 2 },
                world.FindUnit("owner")!, "", null).Select(u => u.RuntimeId).ToArray();
            Require(selected.SequenceEqual(new[] { "a", "b" }), "stable target identity ordering");
            reactions.Register("owner", "periodic", rule with
            {
                Event = MatrixEventKind.Tick,
                OncePerAction = false,
                Every = 1,
                IntervalTicks = 3
            }, CombatSourceRef.System("periodic"));
            var start = world.Executions;
            reactions.AdvancePeriodic(); reactions.AdvancePeriodic();
            Require(world.Executions == start + 1, "periodic cooldown ignored");
            world.Tick = 3; reactions.AdvancePeriodic();
            Require(world.Executions == start + 2, "periodic did not resume at authored tick");
            VerifyDependencyBoundaries();
            GD.Print("MATRIX_REACTION_MODULE_CONTRACT_OK standalone read-only-targets rollback action-dedup periodic cross-scope cleanup dependency-boundaries");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("MATRIX_REACTION_MODULE_CONTRACT_FAILED " + e); GetTree().Quit(1); }
    }
    private static BattleCombatEvent Fact(int sequence, string target) => new(sequence, "module", null, "chain", 0,
        BattleCombatEventKind.AttackLanded, CombatSourceRef.System("attack"), "owner", target, 0,
        1, 1, 1, default, "", 0, 0, "", default, DamageClass: CombatDamageClass.BasicAttack, ActionId: "one-action");
    private static void VerifyDependencyBoundaries()
    {
        foreach (var type in new[] { typeof(BattleMatrixQueries), typeof(BattleMatrixReactionScope) })
        {
            Require(type.DeclaringType is null, "module remains nested in simulation");
            Require(!type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Any(f =>
                f.FieldType == typeof(BattleSimulation) || f.FieldType == typeof(BattleUnitState)), "module retains broad mutable world");
        }
        Require(!typeof(GameFlowCoordinator).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Any(f =>
            f.FieldType == typeof(BattleResult) || f.FieldType == typeof(RunBattleResolution) ||
            f.FieldType.Namespace == "TowerAutobattler.BattleLab"), "router still owns feature lifecycle state");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class World : IBattleMatrixReactionWorld, IDisposable
    {
        private readonly BattleAttributeScope _attributes = new("reaction-module");
        public IReadOnlyList<BattleUnitView> Units { get; }
        public BattleMatrixReactionScope Reactions = null!;
        public int Executions;
        public bool FailExecution;
        public int Tick { get; set; }
        public int MaximumReactions => 32;
        public World()
        {
            Units = new[] { Make("owner", 0, new(1, 1)), Make("b", 1, new(3, 1)), Make("a", 1, new(3, 1)) };
        }
        private BattleUnitView Make(string id, int team, Vector2I cell)
        {
            var definition = new UnitSnapshot(id, id, UnitRole.Fighter, true, false, 100, 10, 10, 10, 1,
                0, 0, 0, 0, [], new UnitBehaviorSnapshot());
            return BattleUnitView.For(new BattleUnitState
            {
                RuntimeId = id,
                SourceInstanceId = id,
                Definition = definition,
                Attributes = _attributes.CreateSet(id, new CompiledAttributeSetDefinition([
                    new(CombatAttribute.MaxHealth, 100, 0, 1000)], id)),
                Team = team,
                Cell = cell,
                Health = 100
            });
        }
        public BattleUnitView? FindUnit(string id) => Units.FirstOrDefault(u => u.RuntimeId == id);
        public float Counter(string ownerId, string key) => 0;
        public int Poison(string id) => 0;
        public int Chill(string id) => 0;
        public int Ember(string id) => 0;
        public bool ActionDisabled(string id) => false;
        public bool ExcludedDeath(string id) => false;
        public void WriteCounter(string ownerId, string key, float value) { }
        public void PruneBeforeFact() { }
        public void PruneBuffs() { }
        public void ResolveReaction(BattleCombatEvent? fact, Action work)
        {
            var checkpoint = Reactions.Capture(); var count = Executions;
            try { work(); } catch { Reactions.Restore(checkpoint); Executions = count; throw; }
        }
        public void Execute(CompiledMatrixOperation op, string owner, string target, CombatSourceRef origin, BattleCombatEvent? fact, string slot)
        {
            Executions++;
            Reactions.Drain(); // Reentry must not process work before the current reaction commits.
            if (FailExecution) throw new InvalidOperationException("controlled effect rejection");
        }
        public void Dispose() => _attributes.Dispose();
    }
}
