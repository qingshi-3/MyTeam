using System;
using Godot;
using TowerAutobattler.App;

// Exercise the configured game entry point and current production content, so
// frozen compatibility fixtures cannot hide a broken newly authored resource.
public partial class ProductionStartupContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = 0;
        GameRoot? game = null;
        try
        {
            var mainScene = ProjectSettings.GetSetting("application/run/main_scene").AsString();
            game = GD.Load<PackedScene>(mainScene).Instantiate<GameRoot>();
            game.SaveNamespace = $"tests/production-startup/{Guid.NewGuid():N}";
            AddChild(game);
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            for (var frame = 0; frame < 120 && game.Content is null && !screens.Result.Visible; frame++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (game.Content is not { } content)
                throw new InvalidOperationException("Production startup did not publish content: " +
                    screens.Result.GetNode<Label>("Center/Panel/Layout/Summary").Text);
            if (!screens.MainMenu.IsVisibleInTree() || screens.Result.Visible)
                throw new InvalidOperationException("Production startup did not reach the main menu.");

            GD.Print($"PRODUCTION_STARTUP_OK heroes={content.Catalog.Heroes.Count} " +
                $"abilities={content.Graph.Abilities.Length} statuses={content.Graph.Statuses.Length} main-menu=visible");
        }
        catch (Exception exception)
        {
            GD.PrintErr("PRODUCTION_STARTUP_FAILED: " + exception);
            exitCode = 1;
        }
        finally
        {
            game?.QueueFree();
        }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GetTree().Quit(exitCode);
    }
}
