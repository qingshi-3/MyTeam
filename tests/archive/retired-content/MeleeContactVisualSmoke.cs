using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;

// Production bodies, motion, sprites and basic attacks; private loadouts isolate melee contact.
public partial class MeleeContactVisualSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            GetWindow().Size = new(1600, 900);
            var args = OS.GetCmdlineUserArgs();
            bool before = args.Contains("--before"), diagonal = args.Contains("--diagonal");
            bool alliedBlockers = args.Contains("--allied-blockers");
            var capture = args.FirstOrDefault(arg => arg.StartsWith("--capture="))?[10..];
            if (capture is not null) Directory.CreateDirectory(capture);
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var session = new BattleLabSession(index, 6, 912, BattleLabPlacementMode.FreeExperiment, "rule_clear");
            Require(session.AddAndPlace("hero_hc03_iron_guard", BattleLabSide.Player, new(alliedBlockers ? 1 : 3, 2)).Succeeded, "hero placement");
            Require(session.AddAndPlace(alliedBlockers ? "soldier_dummy_static" : "soldier_dummy_melee", BattleLabSide.Enemy,
                new(alliedBlockers ? 7 : 5, diagonal ? 4 : 2)).Succeeded, "opponent placement");
            if (alliedBlockers)
                for (var y = 1; y <= 3; y++)
                    Require(session.AddAndPlace("soldier_dummy_static", BattleLabSide.Player, new(3, y)).Succeeded, "allied blocker placement");
            var config = new BattleLabPreparationAdapter(index).Build(session.Freeze());
            for (int i=0; i<config.Spawns.Count; i++)
            {
                var unit = config.Spawns[i].Unit;
                var attributes = unit.AttributeDefinition!;
                config.Spawns[i] = config.Spawns[i] with { Unit = unit with {
                    AbilityLoadout = null, Range = before ? 1.2f : unit.Range,
                    AttributeDefinition = attributes with { Attributes = attributes.Attributes.Select(value =>
                        before && value.Attribute == CombatAttribute.AttackRange ? value with { BaseValue = 1.2f } : value).ToImmutableArray() } } };
            }
            var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
            AddChild(screen);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            screen.StartBattle(package.Content, config, alliedBlockers ? "绕过己方单位 · 接敌" : "近战接敌 · 靠近后交手");
            var trace = new StringBuilder("frame,tick,center_distance,first_action,second_action,hero_x,hero_y\n");
            for (int frame=0; frame<360; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var units = screen.ReadRuntimeUnits();
                var hero = units.Single(unit => unit.IsHero);
                var opponent = units.Single(unit => unit.Team == 1);
                float distance = hero.Position.DistanceTo(opponent.Position);
                trace.AppendLine(FormattableString.Invariant($"{frame},{screen.TickIndex},{distance},{hero.LastActionKind},{opponent.LastActionKind},{hero.Position.X},{hero.Position.Y}"));
                Require(screen.LastRuntimeFailure.Length == 0, screen.LastRuntimeFailure);
                if (capture is not null && DisplayServer.GetName() != "headless")
                {
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    Require(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(capture, $"{frame:D4}.png")) == Error.Ok, "capture");
                }
            }
            var final = screen.ReadRuntimeUnits();
            Require(alliedBlockers ? final.Single(unit => unit.Team == 1).Health < final.Single(unit => unit.Team == 1).MaxHealth :
                final.All(unit => unit.Health < unit.MaxHealth), "real basic hits after engagement");
            if (alliedBlockers)
                Require(final.Where(unit => unit.Team == 0 && !unit.IsHero).All(unit => unit.Position.X == 3), "stationary allies were not pushed");
            var finalDistance = final.Single(unit => unit.IsHero).Position.DistanceTo(final.Single(unit => unit.Team == 1).Position);
            Require(before || finalDistance <= 1, "melee settles within one cell pitch");
            if (capture is not null) File.WriteAllText(Path.Combine(capture, "trace.csv"), trace.ToString());
            screen.StopBattle(); screen.QueueFree();
            GD.Print($"MELEE_CONTACT_VISUAL_OK before={before} diagonal={diagonal} allied_blockers={alliedBlockers} center_distance={finalDistance:0.000}");
            GetTree().Quit();
        }
        catch (Exception exception) { GD.PrintErr("MELEE_CONTACT_VISUAL_FAILED: " + exception); GetTree().Quit(1); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
