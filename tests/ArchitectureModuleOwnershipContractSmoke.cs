using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Abilities;
using TowerAutobattler.Battle;
using TowerAutobattler.Effects;
using TowerAutobattler.Content;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

public partial class ArchitectureModuleOwnershipContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            foreach (var type in new[] { typeof(BattleTraitBindings), typeof(BattleTraitReactionScope), typeof(BattleElementStatusScope),
                         typeof(BattleSupportEffectScope), typeof(BattleVitalityScope), typeof(BattleUnitFactory),
                         typeof(BattleAttackScope), typeof(BattleTargetingPolicy), typeof(BattleTechniqueScope),
                         typeof(BattleUnitActionQueueScope), typeof(BattleDisplacementScope), typeof(BattleChargedLineScope),
                         typeof(BattleTrampleScope), typeof(BattleLifecycleScope), typeof(BattleValueLedger),
                         typeof(BattleValueOperations), typeof(BattleMatrixAbilityScope), typeof(BattlePermanentGrowthScope),
                         typeof(BattleSkillProgressProjection),
                         typeof(StatusModifierProjectionScope), typeof(StatusReactiveSubscriptionScope), typeof(StatusPeriodicReceiptLedger) })
                Require(type.DeclaringType is null && !type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Any(f =>
                    f.FieldType == typeof(BattleSimulation) || f.FieldType == typeof(BattleStatusScope) && type.Namespace == typeof(BattleStatusScope).Namespace),
                    type.Name + " retained its broad root");
            VerifyElementAndSupportOwners();
            VerifyReceiptOwnership();
            VerifyActionsAndValueLedger();
            GD.Print("ARCHITECTURE_MODULE_OWNERSHIP_OK standalone-elements-support frozen-death poison-rollback shield-expiry deferred-mana receipt-exactly-once action-reason-merge recovery-boundary value-projection-rollback foreign-checkpoint boundary-guards");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("ARCHITECTURE_MODULE_OWNERSHIP_FAILED " + e); GetTree().Quit(1); }
    }
    private static void VerifyElementAndSupportOwners()
    {
        using var world = new World();
        var bindings = new BattleTraitBindings("ownership");
        bindings.Initialize(new("", [], []), world.Units);
        Throws(() => bindings.Initialize(new("", [], []), world.Units), "bindings republished");
        using var statuses = new BattleStatusScope("elements", (id, snapshot) => world.Unit(id).Statuses = snapshot,
            id => world.Unit(id).Attributes, _ => true, null, null);
        var elements = new BattleElementStatusScope(world, world, bindings, statuses, CompiledMatrixStatusRules.Default);
        elements.ApplyPoison("source", "target", 2);
        Require(elements.ReadPoison("target") == 2, "standalone poison stack ledger");
        var checkpoint = elements.Capture();
        var statusCheckpoint = statuses.BeginWorldTransaction();
        Require(elements.ConsumePoison("target", 1) == 1 && elements.ReadPoison("target") == 1, "consume visible layer");
        statusCheckpoint.Rollback(); elements.Restore(checkpoint);
        Require(elements.ReadPoison("target") == 2, "status and elemental rollback agree");
        Throws(() => new BattleElementStatusScope(world, world, bindings, statuses, CompiledMatrixStatusRules.Default).Restore(checkpoint), "foreign elemental checkpoint");
        elements.FreezeDeath("target");
        world.Unit("target").Health = 0;
        elements.RemoveLive("target");
        Require(elements.ReadPoison("target") == 2, "death retains immutable poison evidence after live cleanup");
        elements.Clear();
        Require(elements.ReadPoison("target") == 0, "element completion cleared frozen evidence");
        world.Unit("target").Health = 100;
        var support = new BattleSupportEffectScope(world, world, bindings);
        Require(support.ApplyShield("source", "target", 40, 2) == 40, "timed shield grant");
        world.Unit("target").Shield -= 15;
        support.ShieldConsumed(BattleUnitView.For(world.Unit("target")), 15, true, CombatSourceRef.System("hit"));
        world.Locked = true;
        support.GrantMana("source", "target", 12, true);
        support.Advance();
        Require(world.Unit("target").CurrentMana == 0 && world.Unit("target").Shield == 25, "locked mana and live shield");
        var supportCheckpoint = support.Capture();
        world.Tick = 2; world.Locked = false; support.Advance();
        Require(world.Unit("target").Shield == 0 && world.Unit("target").CurrentMana == 12 && !support.HasPending,
            "expiry subtracts only remaining shield and unlock grants mana once");
        Throws(() => new BattleSupportEffectScope(world, world, bindings).Restore(supportCheckpoint), "foreign support checkpoint");
        support.Clear(); support.Clear();
    }
    private static void VerifyReceiptOwnership()
    {
        var ledger = new StatusPeriodicReceiptLedger();
        var definition = BattleMechanicStatus.Create("receipt", "凭据", 10);
        var binding = new CompiledEffectBinding("receipt", 0, new(default, default), [], new CompiledOwnerTargetQuery(), [], new(0, 0, 1, 1), null);
        var invocation = new StatusEffectInvocation(StatusEffectInvocationKind.Periodic, definition, "source", "owner", "instance", "owner", 3,
            binding, StatusRemovalReason.None, PeriodicValue: 7, PeriodicReceiptId: "r");
        ledger.Schedule("r", "instance", "owner", "receipt", 3, 7);
        var before = ledger.Capture();
        Throws(() => ledger.TryClaim(invocation with { Tick = 4 }), "mismatched receipt accepted");
        Require(ledger.PendingValue("owner", "receipt") == 7 && ledger.TryClaim(invocation) && !ledger.TryClaim(invocation), "claim exactly once");
        ledger.Restore(before);
        Require(ledger.TryClaim(invocation), "claim restored by rollback");
        Throws(() => new StatusPeriodicReceiptLedger().Restore(before), "foreign receipt checkpoint");
    }
    private static void VerifyActionsAndValueLedger()
    {
        using var world = new World();
        var source = world.Unit("source"); var target = world.Unit("target");
        var actions = new BattleUnitActionQueueScope(() => world.Tick, () => null);
        actions.QueueUnitAction(source, "punch", UnitActionReason.FullResource);
        actions.QueueUnitAction(source, "punch", UnitActionReason.CriticalHealth);
        var before = actions.Capture();
        actions.RemoveQueuedUnitActionReason(source, "punch", UnitActionReason.FullResource);
        Require(actions.TakeQueuedUnitAction(source, "punch") == UnitActionReason.CriticalHealth &&
            !actions.HasQueuedUnitAction(source, "punch"), "merged request retained its other reason without duplicate casts");
        actions.Restore(before);
        Require(actions.TakeQueuedUnitAction(source, "punch") == (UnitActionReason.FullResource | UnitActionReason.CriticalHealth), "action rollback restores reasons");
        actions.RecoverUnitAction(source, 3);
        Require(actions.UnitActionRecovering(source), "recovery begins");
        world.Tick = 3;
        Require(!actions.UnitActionRecovering(source), "recovery ends at exact tick");
        Throws(() => new BattleUnitActionQueueScope(() => 0, () => null).Restore(before), "foreign action checkpoint");
        var units = new[] { source, target };
        var ledger = new BattleValueLedger(() => world.Tick, () => units, new BattleStatistics());
        ledger.WriteCounter(source, "owned", 2);
        ledger.WriteCounter(source, "shared", 4, shared: true);
        ledger.SetValueContribution("aura", source, target, CombatAttribute.MaxHealth, 20, true, CombatSourceRef.System("aura"));
        Require(target.MaxHealth == 120, "broadcast modifier applied");
        var valueCheckpoint = ledger.Capture();
        var attributeCheckpoint = target.Attributes.CaptureModifierState();
        ledger.WriteCounter(source, "owned", 7);
        ledger.ReleaseSource(source.RuntimeId);
        Require(target.MaxHealth == 100, "source release removed its projection");
        target.Attributes.RestoreModifierState(attributeCheckpoint);
        ledger.Restore(valueCheckpoint);
        Require(target.MaxHealth == 120 && ledger.ReadCounter(source, "owned") == 2 && ledger.ReadCounter(source, "shared", true) == 4,
            "world rollback restores values and projection handle index together");
        source.Health = 0;
        Require(ledger.HasDeadBroadcasts, "dead broadcast pending");
        ledger.CleanDeadBroadcasts();
        Require(target.MaxHealth == 100 && !ledger.HasDeadBroadcasts, "dead source cleanup uses restored handle");
        Throws(() => new BattleValueLedger(() => 0, () => units, new BattleStatistics()).Restore(valueCheckpoint), "foreign value checkpoint");
    }
    private static void Throws(Action action, string label)
    { var thrown = false; try { action(); } catch (InvalidOperationException) { thrown = true; } Require(thrown, label); }
    private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    private sealed class World : IBattleTraitQueries, IBattleTraitCommands, IDisposable
    {
        private readonly BattleAttributeScope _attributes = new("trait-module");
        private readonly Dictionary<string, BattleUnitState> _units = new(StringComparer.Ordinal);
        public int Tick { get; set; }
        public bool Locked { get; set; }
        public IReadOnlyList<BattleUnitView> Units { get; }
        public IReadOnlyDictionary<string, UnitSnapshot> SummonProfiles { get; } = new Dictionary<string, UnitSnapshot>();
        public World()
        {
            foreach (var (id, team) in new[] { ("source", 0), ("target", 1) })
            {
                var profile = new UnitSnapshot(id, id, UnitRole.Fighter, true, false, 100, 10, 10, 10, 1,
                    0, 0, 0, 0, [], new UnitBehaviorSnapshot())
                { AbilityLoadout = new CompiledAbilityLoadout([new("mana", "法力", "", AbilityActivationKind.Automatic,
                    AbilityTriggerKind.ManaFull, 100, 0, 0, 0, 0, [], null)]) };
                _units.Add(id, new BattleUnitState { RuntimeId = id, SourceInstanceId = id, Cell = new(team + 1, 2), Definition = profile, Team = team, Health = 100,
                    Attributes = _attributes.CreateSet(id, new CompiledAttributeSetDefinition([
                        new(CombatAttribute.MaxHealth, 100, 0, 1000), new(CombatAttribute.MaxMana, 100, 0, 1000)], id)) });
            }
            Units = _units.Values.Select(BattleUnitView.For).ToArray();
        }
        public BattleUnitState Unit(string id) => _units[id];
        public bool ManaLocked(string id) => Locked;
        public void Damage(string source, string target, float amount, CombatSourceRef origin, EffectDamageType damageType = EffectDamageType.Normal, CombatDamageClass damageClass = CombatDamageClass.Other) => Unit(target).Health -= amount;
        public void Heal(string source, string target, float amount, CombatSourceRef origin) => Unit(target).Health += amount;
        public float Shield(string source, string target, float amount, CombatSourceRef origin) { Unit(target).Shield += amount; return amount; }
        public void GainMana(string target, float amount) => Unit(target).CurrentMana += amount;
        public void RemoveShield(string target, float amount) => Unit(target).Shield -= amount;
        public void BuffSummonHealth(string target, float amount, CombatSourceRef origin) => throw new NotSupportedException();
        public string? SpawnNear(UnitSnapshot profile, int team, Vector2 position, float health, float damage, string source) => throw new NotSupportedException();
        public void Retire(string target, string source, string reason) => throw new NotSupportedException();
        public void Publish(BattleCombatEventDraft fact) { }
        public void Dispose() => _attributes.Dispose();
    }
}
