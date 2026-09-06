using System;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;

// Isolated render fixture: no BattleSimulation, run progression, input automation, or save access.
public partial class RangedAttackVisualPreview : Control
{
    public override async void _Ready()
    {
        try
        {
            var board = GetNode<BattleBoard>("Board");
            var layer = GetNode<RangedAttackLayer>("Board/Effects");
            layer.Bind(board);
            layer.SetClock(true, 1);
            layer.Present([
                new BattleEvent(1, "projectile_spawn", "", "", 0, new Vector2I(3, 1), "", new Vector2(3, 1), 1, new Vector2(4, 1)),
                new BattleEvent(1, "beam", "", "", 0, new Vector2I(7, 3), "", new Vector2(7, 3), 0, new Vector2(1, 3)),
                new BattleEvent(1, "projectile_impact", "", "", 0, new Vector2I(7, 1), "", new Vector2(7, 1), 2)
            ], true);
            var healer = GetNode<UnitContentRoot>("Board/Healer");
            healer.Bind("preview-healer", 0, 100, 100);
            healer.Position = board.LogicalToLocal(new Vector2(1, 1));
            healer.ApplyPresentation("skill_cast", healer.Position, 100, 100);
            var animation = healer.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
            if (animation.ActiveCue != "attack") throw new InvalidOperationException("healer cast must fall back to attack");
            healer.SetPresentationPaused(true);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var output = OS.GetCmdlineUserArgs().FirstOrDefault(argument => argument.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
            if (output is not null)
            {
                var error = GetViewport().GetTexture().GetImage().SavePng(output);
                if (error != Error.Ok) throw new InvalidOperationException($"preview capture failed: {error}");
                GD.Print("RANGED_VISUAL_PREVIEW_OK cast-fallback=attack scenes=projectile,beam,impact");
                GetTree().Quit();
            }
        }
        catch (Exception exception)
        {
            GD.PrintErr(exception);
            GetTree().Quit(1);
        }
    }
}
