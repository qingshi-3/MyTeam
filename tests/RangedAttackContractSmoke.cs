using System;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

public partial class RangedAttackContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            DelayedImpactAndImmediateBeam();
            InterceptionAndPiercing();
            TerrainExpiryAndDeadTarget();
            DeterminismAndCleanup();
            GD.Print("RANGED_ATTACK_CONTRACT_OK delayed,instant,swept,interception,piercing,terrain,expiry,dead-target,dead-source,determinism,cleanup");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PrintErr(exception);
            GetTree().Quit(1);
        }
    }

    private static void DelayedImpactAndImmediateBeam()
    {
        using var projectile = Create(AttackDelivery.Projectile, speed: 40);
        projectile.Step();
        Check(Target(projectile).Health == 100 && projectile.Projectiles.Length == 1,
            "launch must create one entity without damage");
        Check(projectile.CombatEvents.Count(fact => fact.Kind == BattleCombatEventKind.AttackDeclared) == 1,
            "one declaration per launch");
        projectile.Step();
        Check(Target(projectile).Health < 100 && projectile.Projectiles.IsEmpty,
            "high-speed sweep must hit instead of tunnelling");
        Check(projectile.CombatEvents.Count(fact => fact.Kind == BattleCombatEventKind.AttackLanded) == 1,
            "one landed attack per primary impact");

        foreach (var delivery in new[] { AttackDelivery.Beam, AttackDelivery.Melee })
        {
            using var instant = Create(delivery);
            instant.Step();
            Check(Target(instant).Health < 100 && instant.Projectiles.IsEmpty, "instant attack timing");
            Check(instant.PendingEvents.Any(fact => fact.Type == "beam") == (delivery == AttackDelivery.Beam),
                "only beam delivery creates a beam cue");
        }
    }

    private static void InterceptionAndPiercing()
    {
        using var intercepted = Create(AttackDelivery.Projectile, speed: 40, extra: true);
        intercepted.Step();
        var blocker = intercepted.Units.Single(unit => unit.SourceInstanceId == "blocker");
        blocker.Position = new Vector2(2, 2);
        intercepted.Step();
        Check(blocker.Health < 100 && Target(intercepted).Health == 100, "first enemy intercepts shot");

        using var piercing = Create(AttackDelivery.Projectile, speed: 40, extra: true, piercing: true);
        piercing.Step();
        piercing.Units.Single(unit => unit.SourceInstanceId == "blocker").Position = new Vector2(2, 2);
        piercing.Step();
        Check(piercing.Units.Single(unit => unit.SourceInstanceId == "blocker").Health < 100 &&
              Target(piercing).Health < 100 && piercing.Projectiles.IsEmpty,
            "piercing follows the sweep and hits two distinct enemies");
    }

    private static void TerrainExpiryAndDeadTarget()
    {
        var floor = new SwitchableWall();
        using var blocked = Create(AttackDelivery.Projectile, speed: 40, floor: floor);
        blocked.Step();
        floor.Closed = true;
        blocked.Step();
        Check(Target(blocked).Health == 100 && blocked.Projectiles.IsEmpty, "terrain intercepts in-flight shot");

        using var expired = Create(AttackDelivery.Projectile, speed: .1f, lifetime: .1f);
        expired.Step();
        expired.Step();
        Check(Target(expired).Health == 100 && expired.Projectiles.IsEmpty, "finite lifetime clears missed shots");

        using var dead = Create(AttackDelivery.Projectile, speed: 40, extra: true);
        dead.Step();
        Target(dead).Health = 0;
        var other = dead.Units.Single(unit => unit.SourceInstanceId == "blocker");
        other.Position = new Vector2(4, 2);
        dead.Step();
        Check(other.Health < 100, "dead intended target does not cancel a shot or stop collision with another enemy");

        using var moving = Create(AttackDelivery.Projectile, speed: 40);
        moving.Step();
        Target(moving).Position = new Vector2(4, 4);
        moving.Step();
        Check(Target(moving).Health == 100, "projectile does not home toward a moved target");
    }

    private static void DeterminismAndCleanup()
    {
        using var first = Create(AttackDelivery.Projectile);
        using var second = Create(AttackDelivery.Projectile);
        for (var tick = 0; tick < 8; tick++) { first.Step(); second.Step(); }
        Check(first.PendingEvents.SequenceEqual(second.PendingEvents), "same-seed projectile events repeat exactly");
        first.Replace();
        Check(first.Projectiles.IsEmpty, "replacement clears projectile state");

        using var deadSource = Create(AttackDelivery.Projectile, speed: 40, extra: true, backupHero: true);
        deadSource.Step();
        deadSource.Units.Single(unit => unit.SourceInstanceId == "shooter").Health = 0;
        deadSource.Step();
        Check(Target(deadSource).Health < 100, "in-flight shot survives its source while battle is running");
        deadSource.Dispose();
        Check(deadSource.Projectiles.IsEmpty, "disposal clears projectile state");
    }

    private static BattleSimulation Create(AttackDelivery delivery, float speed = 8, float lifetime = 3,
        bool extra = false, bool piercing = false, IBattleFloorRuleRuntime? floor = null, bool backupHero = false)
    {
        var shooter = Unit("shooter", true) with
        {
            AttackDelivery = delivery, ProjectileSpeed = speed, ProjectileLifetime = lifetime,
            Behavior = new UnitBehaviorSnapshot(PiercingLine: piercing)
        };
        var config = new BattleConfig
        {
            Seed = 17, FloorRule = floor ?? new ClearFloorRuleRuntime("clear", "常规", "test"),
            HeroRule = new HeroRuleSnapshot(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, ""),
            Spawns = [new(shooter, 0, new Vector2I(1, 2), "shooter"),
                new(Unit("target", false), 1, new Vector2I(4, 2), "target")]
        };
        if (extra) config.Spawns.Add(new(Unit("blocker", false), 1, new Vector2I(8, 5), "blocker"));
        if (backupHero) config.Spawns.Add(new(Unit("backup", true), 0, new Vector2I(0, 5), "backup"));
        var simulation = new BattleSimulation(config);
        foreach (var unit in simulation.Units)
        {
            unit.MoveCooldown = 1000;
            if (unit.SourceInstanceId != "shooter") unit.AttackCooldown = 1000;
        }
        simulation.DrainEvents();
        return simulation;
    }

    private static UnitSnapshot Unit(string id, bool hero) =>
        new(id, id, UnitRole.Ranged, hero, false, 100, 20, 6, 100, 100,
            0, 0, 0, 0, [], new UnitBehaviorSnapshot());
    private static BattleUnitState Target(BattleSimulation simulation) =>
        simulation.Units.Single(unit => unit.SourceInstanceId == "target");
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private sealed class SwitchableWall() : ClearFloorRuleRuntime("wall", "障碍", "test")
    {
        public bool Closed { get; set; }
        public override bool CanOccupy(Vector2I cell) => !Closed || cell.X != 3;
    }
}
