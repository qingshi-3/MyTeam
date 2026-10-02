using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;

public partial class BattleArchitectureContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            VerifyMechanismOwnership();
            VerifyAttributeQueries();
            VerifyProjectileLifecycle();
            GD.Print("BATTLE_ARCHITECTURE_CONTRACT_OK attribute-order live-values replace remove failed-mutation checkpoint cleanup bounded-query-allocation projectile-order flight-restore identity-restore owner-cancel scope-isolation");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("BATTLE_ARCHITECTURE_CONTRACT_FAILED " + e); GetTree().Quit(1); }
    }

    private static void VerifyMechanismOwnership()
    {
        var first = new BattleMechanismScope<ImmutableArray<int>>([]);
        var other = new BattleMechanismScope<ImmutableArray<int>>([]);
        first.Replace([1, 2]);
        var checkpoint = first.Capture();
        Require(first.TryBeginDrain() && !first.TryBeginDrain(), "nested mechanism drain must be deferred");
        first.Replace(first.State.Add(3));
        first.Restore(checkpoint);
        Require(first.State.SequenceEqual([1, 2]) && !first.TryBeginDrain(), "rollback must preserve active drain guard");
        var rejected = false;
        try { other.Restore(checkpoint); } catch (InvalidOperationException) { rejected = true; }
        Require(rejected && other.State.IsEmpty, "cross-battle mechanism checkpoint accepted");
        first.EndDrain();
        Require(first.TryBeginDrain(), "drain guard did not release");
        first.EndDrain();
        first.Clear();
        first.Clear();
        Require(first.State.IsEmpty && checkpoint.State.SequenceEqual([1, 2]), "clear mutated checkpoint or retained pending work");
        first.Restore(checkpoint);
        Require(first.State.SequenceEqual([1, 2]), "world rollback cannot restore cleared mechanism state");
    }

    private static void VerifyAttributeQueries()
    {
        var definition = new CompiledAttributeSetDefinition([
            new(CombatAttribute.AttackDamage, 10, 0, 1000),
            new(CombatAttribute.SpellPower, 3, 0, 1000)], "architecture-query");
        using var scope = new BattleAttributeScope("architecture-query");
        var owner = scope.CreateSet("owner", definition);
        var source = scope.CreateSet("source", definition);
        var add = CombatSourceRef.System("add");
        var live = CombatSourceRef.System("live");
        CompiledAttributeModifier Modifier(float value, AttributeModifierOperation operation = AttributeModifierOperation.Add,
            int priority = 0, string slot = "value") => new(CombatAttribute.AttackDamage, operation, new CompiledConstantMagnitude(value), priority, slot);
        owner.ApplyModifier(Modifier(5), add);
        var multiplier = owner.ApplyModifier(Modifier(2, AttributeModifierOperation.Multiply), CombatSourceRef.System("multiply"));
        Near(owner, 30);
        owner.ApplyModifier(new(CombatAttribute.AttackDamage, AttributeModifierOperation.Add,
            new CompiledSourceAttributeMagnitude(CombatAttribute.SpellPower, AttributeCaptureMode.Live), 0, "live"),
            live, new(source, owner));
        Near(owner, 36);
        source.SetBaseValue(CombatAttribute.SpellPower, 7);
        Near(owner, 44); // Only order may be cached; cross-unit values remain live.
        owner.ApplyModifier(Modifier(2), add);
        Near(owner, 38);
        var high = owner.ApplyModifier(Modifier(99, AttributeModifierOperation.Override, 5), CombatSourceRef.System("high"));
        var lateLow = owner.ApplyModifier(Modifier(50, AttributeModifierOperation.Override, 1), CombatSourceRef.System("low"));
        Near(owner, 99);
        owner.Remove(high); Near(owner, 50);
        owner.Remove(lateLow); Near(owner, 38);

        var checkpoint = scope.CaptureState();
        source.SetBaseValue(CombatAttribute.SpellPower, 100);
        owner.RemoveSource(live);
        owner.Remove(multiplier); Near(owner, 12);
        scope.RestoreState(checkpoint); Near(owner, 38);
        var rejected = false;
        try
        {
            owner.ApplyModifier(new(CombatAttribute.AttackDamage, AttributeModifierOperation.Add,
                new CompiledTargetAttributeMagnitude(CombatAttribute.AttackDamage, AttributeCaptureMode.Live), 0, "value"), add);
        }
        catch (InvalidOperationException) { rejected = true; }
        Require(rejected, "cyclic live replacement must fail");
        Near(owner, 38); // Failed replacement must invalidate its derived order too.
        for (var i = 0; i < 500; i++) _ = owner.GetValue(CombatAttribute.AttackDamage);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10000; i++) _ = owner.GetValue(CombatAttribute.AttackDamage);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
        Require(allocated < 1024, $"stable attribute queries allocated {allocated} bytes");
        Require(owner.RemoveSource(live) == 1, "source removal count");
        Near(owner, 24);
        scope.Complete(AttributeScopeCompletionReason.BattleCompleted, 1);
        Near(owner, 24);
        Require(owner.ModifierCount == 0 && scope.LiveSetCount == 0, "completed scope retained modifiers");
    }

    private static void Near(BattleAttributeSet set, float expected) => Require(
        Math.Abs(set.GetValue(CombatAttribute.AttackDamage) - expected) < .001f, "attribute result/order changed");

    private static void VerifyProjectileLifecycle()
    {
        var world = new ProjectileWorld();
        var scope = new BattleProjectileScope(world);
        var template = new BattleProjectileState(0, "source", 0, new(2, 2), new(10, 0), .1f, 10, 3, 0, 2, []);
        var shot = scope.Spawn(template);
        var initial = scope.ReadSnapshot();
        scope.Advance(new Dictionary<string, Vector2>());
        Require(world.Hits.Count == 0 && scope.ReadSnapshot().SequenceEqual(initial), "launch tick must not move or hit");
        var checkpoint = scope.Capture();
        world.Tick = 1;
        scope.Advance(new Dictionary<string, Vector2>());
        Require(world.Hits.SequenceEqual(new[] { "a", "b" }), "simultaneous contacts require stable runtime order");
        Require(scope.ReadSnapshot().IsEmpty && initial.Length == 1 && initial[0].HitIds.IsEmpty,
            "flight completion must not mutate prior projections");
        scope.Restore(checkpoint);
        Require(scope.ReadSnapshot().SequenceEqual(initial), "restore flight state and hit history");
        var next = scope.Spawn(template with { SourceId = "other" });
        scope.Restore(checkpoint);
        Require(scope.Spawn(template with { SourceId = "other" }).Id == next.Id, "restore identity sequence");
        scope.CancelOwner("source");
        Require(scope.ReadSnapshot().Length == 1 && scope.ReadSnapshot()[0].SourceId == "other", "cancel only the changed owner");
        var other = new BattleProjectileScope(world);
        var rejected = false;
        try { other.Restore(checkpoint); } catch (InvalidOperationException) { rejected = true; }
        Require(rejected && other.ReadSnapshot().IsEmpty, "cross-battle restore must be rejected");
        scope.Clear();
        Require(scope.ReadSnapshot().IsEmpty, "teardown clears all flights");

        world.Hits.Clear();
        world.OnImpact = () => scope.CancelOwner("source");
        scope.Spawn(template with { MaximumHits = 3 });
        scope.Advance(new Dictionary<string, Vector2>());
        Require(scope.ReadSnapshot().IsEmpty && world.Hits.Count == 1,
            "impact reaction cancellation must not resurrect or continue the local flight");

        world.OnImpact = null;
        world.Hits.Clear();
        world.Targets = [new("wall", 0, new(2.5f, 2), .1f, true, true),
            new("behind", 1, new(2.75f, 2), .1f, true, false)];
        scope.Spawn(template with { MaximumHits = 1, HitIds = ["earlier"], ContinueAfterHitLimit = true });
        scope.Advance(new Dictionary<string, Vector2>());
        Require(world.Hits.SequenceEqual(new[] { "wall" }) && scope.ReadSnapshot().IsEmpty,
            "friendly wall consumes inert piercing flight without allowing another target hit");
    }

    private sealed class ProjectileWorld : IBattleProjectileWorld
    {
        public int Tick { get; set; }
        public readonly List<string> Hits = [];
        public Action? OnImpact { get; set; }
        public ProjectileTarget[] Targets { get; set; } = [
            new("b", 1, new(2.75f, 2), .1f, true, false), new("a", 1, new(2.75f, 2), .1f, true, false)];
        public IEnumerable<ProjectileTarget> ReadTargets() => Targets;
        public bool IsAlive(string runtimeId) => true;
        public bool CanOccupy(Vector2I cell) => true;
        public void ResolveImpact(BattleProjectileState projectile, string targetId, Vector2 contact, bool blockedByWall)
        {
            Hits.Add(targetId);
            OnImpact?.Invoke();
        }
        public void Publish(ProjectileFlightFact fact) { }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
