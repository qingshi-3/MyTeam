using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

public partial class AlliedBodyNavigationContractSmoke : Node
{
    public override async void _Ready()
    {
        var failures = new List<string>();
        var traceDirectory = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--trace="))?[8..];
        foreach (var fixture in new[]
        {
            new Fixture("allied-column", new(1, 2), new(7, 2), [new(3, 1), new(3, 2), new(3, 3)]),
            new Fixture("crowded-front", new(2, 2), new(5, 2), [new(4, 1), new(4, 2), new(4, 3), new(4, 4)]),
            new Fixture("edge-column", new(1, 0), new(7, 0), [new(3, 0), new(3, 1), new(3, 2)])
        })
        {
            try { RunFixture(fixture, traceDirectory); }
            catch (Exception exception) { failures.Add(fixture.Name + ": " + exception.Message); }
        }
        foreach (var failure in failures) GD.PrintErr("ALLIED_NAVIGATION_FAILED " + failure);
        if (failures.Count == 0) GD.Print("ALLIED_NAVIGATION_OK detour=column,front,edge collisions=clear attack=reached");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(failures.Count == 0 ? 0 : 1);
    }

    private static void RunFixture(Fixture fixture, string? traceDirectory)
    {
        using var battle = new BattleSimulation(CreateConfig(fixture));
        var mover = battle.Units.Single(unit => unit.RuntimeId == "mover");
        var allies = battle.Units.Where(unit => unit.RuntimeId.StartsWith("ally_")).ToArray();
        var starts = allies.ToDictionary(unit => unit.RuntimeId, unit => unit.Position);
        var trace = new StringBuilder("tick,x,y,waiting,mode\n");
        var attacked = false;
        try
        {
            for (var tick = 0; tick < 160 && !attacked; tick++)
            {
                battle.Step();
                Require(battle.Outcome == BattleOutcome.Running, "fixture battle ended before navigation completed");
                trace.AppendLine(FormattableString.Invariant($"{battle.TickIndex},{mover.Position.X},{mover.Position.Y},{mover.WaitingTicks},{mover.Mode}"));
                foreach (var first in battle.Units)
                foreach (var second in battle.Units.Where(unit => string.CompareOrdinal(first.RuntimeId, unit.RuntimeId) < 0))
                    Require(first.Position.DistanceTo(second.Position) >= first.BodyRadius + second.BodyRadius + BattlefieldSpace.BodyClearance - .0001f,
                        "living bodies overlapped");
                Require(BattlefieldSpace.IsCircleInsideArena(mover.Position, mover.BodyRadius, BattleSimulation.Width, BattleSimulation.Height),
                    "detour left arena");
                Require(allies.All(unit => unit.Position == starts[unit.RuntimeId]), "detour pushed a stationary ally");
                attacked = battle.DrainEvents().Any(fact => fact.Type == "attack" && fact.SourceRuntimeId == "mover");
            }
            Require(attacked, $"reachable target was never attacked; mover={mover.Position} waiting={mover.WaitingTicks}");
            GD.Print($"ALLIED_NAVIGATION_CASE_OK {fixture.Name} attack_tick={battle.TickIndex} position={mover.Position}");
        }
        finally
        {
            if (traceDirectory is not null)
            {
                Directory.CreateDirectory(traceDirectory);
                File.WriteAllText(Path.Combine(traceDirectory, fixture.Name + ".csv"), trace.ToString());
            }
        }
    }

    private static BattleConfig CreateConfig(Fixture fixture)
    {
        var spawns = new List<BattleSpawn>
        {
            new(Unit("mover", false), 0, fixture.Start, "mover"),
            new(Unit("target", true), 1, fixture.Target, "target")
        };
        spawns.AddRange(fixture.Allies.Select((position, index) =>
            new BattleSpawn(Unit("ally", true), 0, position, $"ally_{index}")));
        return new BattleConfig
        {
            Seed = 912,
            FloorRule = new ClearFloorRuleRuntime("clear", "常规", "绕行回归"),
            Spawns = spawns,
            HeroRule = new HeroRuleSnapshot(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, ""),
            Modifiers = new ModifierSnapshot(),
            Summons = new SummonProfiles()
        };
    }

    private static UnitSnapshot Unit(string id, bool stationary) => new(
        id, id, UnitRole.Fighter, !stationary, false, 10000, 1, .25f, 10, 3,
        0, 0, 0, 0, [], new UnitBehaviorSnapshot(Stationary: stationary, DisableBasicAttacks: stationary));

    private sealed record Fixture(string Name, Vector2I Start, Vector2I Target, Vector2I[] Allies);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
