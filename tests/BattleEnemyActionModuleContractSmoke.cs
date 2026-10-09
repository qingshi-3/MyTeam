using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;

public partial class BattleEnemyActionModuleContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            using (var world = new World())
            {
                var cone = new CompiledConeBreath(4, 90, 2, 1, 3, 1, 1, "fire");
                Require(world.Scope.SelectTarget("owner", cone) == "target" && world.Scope.Prepare("owner", cone, "target"), "independent target preparation");
                Require(world.Events.Count == 0 && world.Hits == 0, "preparation mutated world");
                world.Scope.Begin("owner", "target", cone, "喷火", CombatSourceRef.System("cone"));
                var checkpoint = world.Scope.Capture();
                world.Tick = 1; world.Scope.Advance(); Require(world.Hits == 0, "windup delivered damage");
                world.Tick = 2; world.FailHit = true;
                try { world.Scope.Advance(); throw new Exception("expected injected failure"); }
                catch (InvalidOperationException e) when (e.Message == "injected hit failure") { }
                Require(world.Hits == 0 && world.Events.All(e => e.EnemyAction?.Stage != "release"), "failed pulse retained damage or release fact");
                world.FailHit = false; world.Scope.Advance();
                Require(world.Hits == 1 && world.Events.Count(e => e.EnemyAction?.Stage == "release") == 1, "pulse eligibility was consumed by failure");
                world.ControlledOwner = true; world.Tick = 3; world.Scope.Advance();
                Require(!world.Scope.IsBusy("owner") && world.Hits == 1 && world.Events.Any(e => e.EnemyAction?.Stage == "end"), "control did not cancel remaining pulses");
                world.Scope.Clear(); world.Scope.Restore(checkpoint);
                Require(world.Scope.IsBusy("owner") && world.Scope.IsExecuting("owner", "cone"), "checkpoint lost action state");
                using var other = new World();
                var rejected = false;
                try { other.Scope.Restore(checkpoint); } catch (InvalidOperationException) { rejected = true; }
                Require(rejected && !other.Scope.IsBusy("owner"), "cross-battle enemy checkpoint accepted");
            }
            using (var world = new World())
            {
                var blade = new CompiledReturningBlade(4, .1f, 12, 1, "blade");
                world.Scope.Begin("owner", "target", blade, "回旋刃", CombatSourceRef.System("blade"));
                var previous = world.Units.ToDictionary(unit => unit.RuntimeId, unit => unit.Position);
                world.Tick = 1; world.Scope.Advance();
                Require(world.Events.Any(e => e.Type == "blade_release"), "blade did not launch");
                world.Owner.Health = 0;
                for (var tick = 2; tick < 100 && !world.Events.Any(e => e.Type == "blade_end"); tick++)
                { world.Tick = tick; world.Scope.Advance(); world.Scope.AdvanceBlades(previous); }
                Require(world.Hits == 2 && world.Events.Any(e => e.Type == "blade_end" && e.Position == new Vector2(2, 2)),
                    "released blade must survive its owner and hit once per leg before returning to its original point");
            }
            GD.Print("BATTLE_ENEMY_ACTION_MODULE_CONTRACT_OK root-free target-query windup rollback retry cancel blade-legs owner-death checkpoint-isolation");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("BATTLE_ENEMY_ACTION_MODULE_CONTRACT_FAILED " + e); GetTree().Quit(1); }
    }
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }

    private sealed class World : IBattleEnemyQueries, IBattleEnemyCommands, IBattleEnemyTransactions, IDisposable
    {
        private readonly BattleAttributeScope _attributes = new("enemy-module");
        public readonly BattleEnemyActionScope Scope;
        public readonly BattleUnitState Owner;
        public readonly List<BattleEvent> Events = [];
        public int Hits;
        public bool FailHit;
        public bool ControlledOwner;
        public World()
        {
            Owner = Make("owner", 0, new(2, 2));
            Units = new[] { BattleUnitView.For(Owner), BattleUnitView.For(Make("target", 1, new(4, 2))) };
            Scope = new(this, this, this);
        }
        private BattleUnitState Make(string id, int team, Vector2 position) => new()
        {
            RuntimeId = id, SourceInstanceId = id, Team = team, Cell = Vector2I.Zero, Position = position, Health = 100,
            Definition = new(id, id, UnitRole.Fighter, false, false, 100, 10, 1, 10, 1, 0, 0, 0, 0, [], new UnitBehaviorSnapshot()),
            Attributes = _attributes.CreateSet(id, new CompiledAttributeSetDefinition([
                new(CombatAttribute.MaxHealth, 100, 0, 1000), new(CombatAttribute.AttackDamage, 10, 0, 1000)], id))
        };
        public int Tick { get; set; }
        public IReadOnlyList<BattleUnitView> Units { get; }
        public IReadOnlyDictionary<string, UnitSnapshot> Profiles { get; } = new Dictionary<string, UnitSnapshot>();
        public IReadOnlyList<DisplacementReservation> Reservations => [];
        public bool CanOccupyTerrain(Vector2I cell) => true;
        public bool Controlled(string id) => id == "owner" && ControlledOwner;
        public bool Displacing(string id) => false;
        public bool Airborne(string id) => false;
        public bool HasAttackInFlight(string id) => false;
        public CompiledBroodPhase? Brood(string id) => null;
        public bool HasLineAccess(Vector2 from, Vector2 to) => true;
        public bool CanOccupy(Vector2 position, float radius) => true;
        public void BeginCast(string ownerId, string targetId, string displayName) { }
        public void ContinueCast(string ownerId) { }
        public void Hit(string sourceId, string targetId, float damage, CombatSourceRef origin, EffectDamageType type, bool isAreaDamage = false)
        { Hits++; if (FailHit) throw new InvalidOperationException("injected hit failure"); }
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
            var checkpoint = Scope.Capture(); var hits = Hits; var count = Events.Count;
            try { work(); }
            catch { Scope.Restore(checkpoint); Hits = hits; Events.RemoveRange(count, Events.Count - count); throw; }
        }
        public void Dispose() => _attributes.Dispose();
    }
}
