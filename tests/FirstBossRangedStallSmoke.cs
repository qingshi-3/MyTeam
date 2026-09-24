using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class FirstBossRangedStallSmoke : Node
{
    public override async void _Ready()
    {
        var code = 0;
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join('\n', gate.Report.CoreErrors));
            var json = System.IO.File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/fixtures/first-boss-ranged-stall.json"));
            var app = new RunApplication(package.Content, new MemorySave(json), package.Project);
            using var battle = new BattleSimulation(app.BuildBattleConfig(app.CurrentEncounter()));
            var maxIdle = 0;
            for (var i = 0; i < 1800 && battle.Outcome == BattleOutcome.Running; i++)
            {
                battle.Step();
                foreach (var unit in battle.Units.Where(unit => unit.Alive && unit.Team == 0 && unit.AttackRange > 3))
                    if (unit.Mode == BattleUnitMode.Waiting && string.IsNullOrEmpty(unit.ActionTargetRuntimeId))
                        maxIdle = Math.Max(maxIdle, unit.WaitingTicks);
            }
            Require(maxIdle < 40, $"reachable first Boss was discarded for {maxIdle} ticks; ranged units must replan around central terrain");
            GD.Print($"BOSS_REPLAY_OK tick={battle.TickIndex} outcome={battle.Outcome} maximum-targetless-wait={maxIdle}");
            Require(NarrowLaneProbe(false) == NarrowLaneProbe(false), "same seed must replay the same detour and launch tick");
            NarrowLaneProbe(true);
            GD.Print("FIRST_BOSS_RANGED_STALL_OK saved-formation,replan,terrain-sweep,legal-fire,unreachable,determinism");
        }
        catch (Exception e) { GD.PrintErr(e); code = 1; }
        GetTree().Quit(code);
    }

    private static string NarrowLaneProbe(bool sealedWall)
    {
        var floor = sealedWall ? (IBattleFloorRuleRuntime)new SealedWall() : new NarrowLanesRuntime("narrow", "狭路", "");
        var shooter = new UnitSnapshot("shooter", "远程", UnitRole.Ranged, true, false,
            1000, 10, 5.2f, 10, 3, 0, 0, 0, 0, [], new UnitBehaviorSnapshot())
            { AttackDelivery = AttackDelivery.Projectile, ProjectileSpeed = 8, ProjectileLifetime = 5 };
        var target = new UnitSnapshot("target", "固定目标", UnitRole.Fighter, false, false,
            1000, 0, .25f, 10, 3, 0, 0, 0, 0, [], new UnitBehaviorSnapshot(Stationary: true, DisableBasicAttacks: true));
        using var battle = new BattleSimulation(new BattleConfig
        {
            Seed = 912, FloorRule = floor,
            HeroRule = new HeroRuleSnapshot(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, ""),
            Spawns = [new(shooter, 0, new Vector2I(2, 2), "shooter"), new(target, 1, new Vector2I(6, 2), "target")]
        });
        var mover = battle.Units.Single(unit => unit.SourceInstanceId == "shooter");
        var enemy = battle.Units.Single(unit => unit.SourceInstanceId == "target");
        var trace = new StringBuilder();
        var launched = false;
        for (var tick = 0; tick < 120; tick++)
        {
            var previous = mover.Position;
            battle.Step();
            Require(BattlefieldSpace.IsSegmentTerrainClear(previous, mover.Position, mover.BodyRadius,
                BattleSimulation.Width, BattleSimulation.Height, floor.CanOccupy), "detour cannot cross blocked terrain");
            Require(mover.Position.DistanceTo(enemy.Position) >= mover.BodyRadius + enemy.BodyRadius,
                "detour cannot overlap the target body");
            trace.Append(FormattableString.Invariant($"{mover.Position.X:R},{mover.Position.Y:R};"));
            var shots = battle.DrainEvents().Where(fact => fact.Type == "attack" && fact.SourceRuntimeId == mover.RuntimeId).ToArray();
            if (shots.Length > 0)
            {
                launched = true;
                Require(BattlefieldSpace.IsWithinReach(mover, enemy, mover.AttackRange) &&
                    BattlefieldSpace.IsSegmentTerrainClear(mover.Position, enemy.Position, mover.Definition.ProjectileRadius,
                        BattleSimulation.Width, BattleSimulation.Height, floor.CanOccupy), "only fire with range and clear line");
            }
            if (!sealedWall && enemy.Health < enemy.MaxHealth) break;
        }
        Require(sealedWall ? !launched && enemy.Health == enemy.MaxHealth : launched && enemy.Health < enemy.MaxHealth,
            sealedWall ? "an unreachable enemy must not enable shooting through walls" : $"reachable enemy behind central wall must be attacked; launched={launched}, hp={enemy.Health}, position={mover.Position}, mode={mover.Mode}, target={mover.ActionTargetRuntimeId}");
        GD.Print($"NARROW_LANE_PROBE_OK sealed={sealedWall} tick={battle.TickIndex} position={mover.Position}");
        return trace.ToString();
    }

    private sealed class SealedWall() : ClearFloorRuleRuntime("sealed", "封闭墙", "")
    {
        public override bool CanOccupy(Vector2I cell) => cell.X != 4;
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }

    private sealed class MemorySave(string json) : IRunSaveService
    {
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => JsonSerializer.Deserialize<ActiveRunDto>(json);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) => throw new InvalidOperationException("Battle diagnosis must not save a run.");
        public void DeleteActiveRun() => throw new InvalidOperationException("Battle diagnosis must not delete a run.");
    }
}
