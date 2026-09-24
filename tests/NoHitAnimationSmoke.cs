using System;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;

public partial class NoHitAnimationSmoke : Node
{
    public override void _Ready()
    {
        var unit = GD.Load<PackedScene>("res://content/heroes/hero_hc01_crossbow.tscn").Instantiate<UnitContentRoot>();
        try
        {
            AddChild(unit);
            unit.Bind("no-hit-probe", 0, 100, 100);
            var animation = unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
            var sprite = animation.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
            var health = unit.GetNode<HealthViewComponent>("HealthViewComponent");

            foreach (var cue in new[] { "idle", "move", "attack", "skill_cast" })
            {
                animation.ResetPresentation();
                animation.PlayCue(cue);
                var frame = sprite.Frame;
                for (var count = 0; count < 40; count++) unit.RefreshPresentation("hit", 61, 100);
                Require(animation.ActiveLogicalCue == cue && animation.PendingCue == "", "damage preserves " + cue);
                Require(sprite.Frame == frame && health.Bar.Value == 61, "damage updates health without restarting sprite");
            }

            animation.ResetPresentation();
            animation.BeginTimedAttack(new BattleAttackTiming(1, .8f));
            animation.StepTimedAttack(.5f);
            var shotFrame = sprite.Frame;
            animation.PlayCue("hit");
            Require(sprite.Frame == shotFrame && animation.PendingCue == "", "damage preserves timed draw progress");
            animation.CompleteTimedWindup(.8f);
            Require(sprite.Frame >= shotFrame, "release still advances the bow pose");
            animation.StepTimedAttack(.3f);
            Require(animation.ActiveLogicalCue == "idle", "no delayed flinch after arrow release");

            animation.SetDisplacementCue("hit");
            Require(animation.ActiveLogicalCue == "displaced" && animation.ActiveCue != "hit", "forced displacement keeps a neutral pose");
            var facing = animation.FacingRight;
            animation.FaceHorizontal(facing ? -10 : 10);
            Require(animation.FacingRight == facing, "forced motion still locks facing");
            animation.SetDisplacementCue("");

            var cues = BattlePresentationCueArbiter.Select(new BattleEvent[]
            {
                new(1, "move", "walker", "", 0, Vector2I.Zero, "move"),
                new(1, "damage", "enemy", "walker", 10, Vector2I.Zero, "hit"),
                new(1, "damage", "enemy", "idle-unit", 10, Vector2I.Zero, "hit")
            });
            Require(cues["walker"] == "move" && !cues.ContainsKey("idle-unit"), "damage claims no pose in battle arbitration");
            animation.PlayCue("defeated");
            animation.PlayCue("hit");
            Require(animation.IsTerminal && animation.ActiveCue != "hit", "defeat remains terminal without hit fallback");
            GD.Print("NO_HIT_ANIMATION_OK idle,move,attack,cast,timed-release,health,displacement,death");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("NO_HIT_ANIMATION_FAILED " + error); GetTree().Quit(1); }
        finally { unit.QueueFree(); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
