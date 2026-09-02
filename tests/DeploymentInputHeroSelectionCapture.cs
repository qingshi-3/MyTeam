using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class DeploymentInputHeroSelectionCapture : Node
{
    private const string OutputPath = "res://.godot/qa/deployment-input-hero-selection";
    private string _saveNamespace = string.Empty;
    private Vector2I _captureSize = new(1600, 900);

    public override async void _Ready()
    {
        var code = await CaptureAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(code);
    }

    private async Task<int> CaptureAsync()
    {
        GameRoot? root = null;
        try
        {
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(OutputPath));
            ApplyRequestedCaptureSize();
            var size = (Vector2)_captureSize;
            _saveNamespace = $"tests/deployment-input-hero-capture-{(int)size.X}x{(int)size.Y}";
            var persisted = new SaveService(_saveNamespace);
            persisted.DeleteActiveRun();
            root = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
            root.SaveNamespace = _saveNamespace;
            var host = new Control { Size = size };
            AddChild(host);
            host.AddChild(root);
            for (var frame = 0; frame < 120 && root.Content is null; frame++) await RenderFrame();
            var registry = root.Content ?? throw new InvalidOperationException("content gate did not finish");

            root.Flow.ShowHeroSelection();
            await RenderFrame();
            var heroScreen = root.GetNode<HeroSelectScreen>("Screens/HeroSelectScreen");
            var tiles = heroScreen.GetNode<GridContainer>("%HeroLibrary").GetChildren().OfType<HeroLibraryTile>().ToArray();
            var initialId = heroScreen.PreviewStableId;
            await Capture("HeroDefaultSelection.png");
            var hoverTile = tiles.First(tile => tile.StableId != initialId);
            await MovePointer(hoverTile);
            if (heroScreen.PreviewStableId != initialId)
                throw new InvalidOperationException("hover changed hero selection during capture");
            await Capture("HeroHoverWithoutSelection.png");
            await Click(hoverTile);
            if (heroScreen.PreviewStableId != hoverTile.StableId)
                throw new InvalidOperationException("click did not change hero selection during capture");
            await Capture("HeroClickSelection.png");

            var app = new RunApplication(registry, persisted, TestProjectFixture.Load(registry));
            SetPrivateField(root, "_app", app);
            var heroId = app.Meta.UnlockedHeroIds.First();
            if (!app.StartNewRun(heroId, 0xC4A7UL)) throw new InvalidOperationException("capture run did not start");
            var run = app.ActiveRun!;
            var recruit = registry.Catalog.Soldiers.First(entry => run.Roster.All(unit => unit.ContentId != entry.StableId));
            if (!app.Recruit(recruit.StableId)) throw new InvalidOperationException("capture reserve recruit failed");
            var reserveId = run.Roster.Last().InstanceId;
            run.SelectedNode = TowerNodeType.Combat;
            run.PendingNode = true;
            root.Flow.SetEncounterForTesting(app.CurrentEncounter());
            root.Flow.ShowDeployment();
            await RenderFrames(2);

            var deployment = root.GetNode<DeploymentScreenController>("Screens/DeploymentScreen");
            var board = deployment.GetNode<DeploymentBoard>("%DeploymentBoard");
            var roster = deployment.GetNode<VBoxContainer>("%RosterChoices");
            var reserveCard = roster.GetChildren().OfType<DeploymentUnitCard>().Single(card => card.InstanceId == reserveId);
            await Click(reserveCard);
            await Capture("DeploymentReserveSelectedEnemyPreviews.png");

            var populationFullCell = board.GetChildren().OfType<DeploymentCell>()
                .First(cell => string.IsNullOrEmpty(cell.PieceId));
            await Click(populationFullCell);
            if (!deployment.GetNode<Label>("%Status").Text.Contains("人口", StringComparison.Ordinal))
                throw new InvalidOperationException("capture invalid feedback did not use authoritative reason");
            await Capture("DeploymentInvalidReason.png");

            root.Flow.ShowDeployment();
            await RenderFrames(2);
            reserveCard = roster.GetChildren().OfType<DeploymentUnitCard>().Single(card => card.InstanceId == reserveId);
            var hoverTargets = board.GetChildren().OfType<DeploymentCell>()
                .Where(cell => cell.IsLegalTarget && string.IsNullOrEmpty(cell.PieceId)).Take(3).ToArray();
            await BeginDragAcross(reserveCard, hoverTargets);
            if (board.CurrentDragHoverCell != hoverTargets[^1].Cell ||
                board.GetChildren().OfType<DeploymentCell>().Count(cell => cell.IsDragHovered) != 1)
                throw new InvalidOperationException("capture drag did not retain one current target");
            await Capture("DeploymentSingleDragHover.png");
            await CancelDragOutside();
            if (board.CurrentDragHoverCell is not null || board.GetChildren().OfType<DeploymentCell>().Any(cell => cell.IsDragHovered))
                throw new InvalidOperationException("capture cancelled drag left hover state");
            await Capture("DeploymentCancelledClean.png");

            var target = board.GetChildren().OfType<DeploymentCell>()
                .First(cell => cell.IsLegalTarget && string.IsNullOrEmpty(cell.PieceId));
            await Click(target);
            if (!run.Deployment.Contains(reserveId)) throw new InvalidOperationException("capture reserve click did not deploy");
            await Capture("DeploymentReserveAdded.png");

            GD.Print($"DEPLOYMENT_INPUT_HERO_SELECTION_CAPTURE_OK size={(int)size.X}x{(int)size.Y} captures=8 path={OutputPath}");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("DEPLOYMENT_INPUT_HERO_SELECTION_CAPTURE_FAILED: " + exception);
            return 1;
        }
        finally
        {
            if (root is not null && GodotObject.IsInstanceValid(root))
            {
                if (root.GetParent() is not null) root.GetParent().RemoveChild(root);
                root.Free();
            }
            if (!string.IsNullOrEmpty(_saveNamespace)) new SaveService(_saveNamespace).DeleteActiveRun();
        }
    }

    private async Task Capture(string suffix)
    {
        await RenderFrame();
        var viewportImage = GetViewport().GetTexture().GetImage();
        var image = viewportImage.GetRegion(new Rect2I(Vector2I.Zero, _captureSize));
        var name = $"UI_{image.GetWidth()}x{image.GetHeight()}_{suffix}";
        var error = image.SavePng($"{ProjectSettings.GlobalizePath(OutputPath)}/{name}");
        if (error != Error.Ok) throw new InvalidOperationException($"capture {name}: {error}");
    }

    private async Task Click(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = true, Position = point, GlobalPosition = point
        }, true);
        await RenderFrame();
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = false, Position = point, GlobalPosition = point
        }, true);
        await RenderFrames(2);
    }

    private async Task MovePointer(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        await RenderFrames(2);
    }

    private async Task BeginDragAcross(Control source, IReadOnlyList<DeploymentCell> targets)
    {
        var start = source.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = start, GlobalPosition = start }, true);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = true, Position = start, GlobalPosition = start
        }, true);
        await RenderFrame();
        var previous = start;
        foreach (var target in targets)
        {
            var end = target.GetGlobalRect().GetCenter();
            for (var step = 1; step <= 3; step++)
            {
                var point = previous.Lerp(end, step / 3f);
                GetViewport().PushInput(new InputEventMouseMotion
                {
                    Position = point, GlobalPosition = point, Relative = (end - previous) / 3f,
                    ButtonMask = MouseButtonMask.Left
                }, true);
                await RenderFrame();
            }
            previous = end;
        }
    }

    private async Task CancelDragOutside()
    {
        var point = new Vector2(2, 2);
        GetViewport().PushInput(new InputEventMouseMotion
        {
            Position = point, GlobalPosition = point, Relative = new Vector2(-200, -200),
            ButtonMask = MouseButtonMask.Left
        }, true);
        await RenderFrame();
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = false, Position = point, GlobalPosition = point
        }, true);
        await RenderFrames(2);
    }

    private async Task RenderFrames(int count)
    {
        for (var index = 0; index < count; index++) await RenderFrame();
    }

    private async Task RenderFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }

    private void ApplyRequestedCaptureSize()
    {
        const string prefix = "--qa-size=";
        var argument = OS.GetCmdlineUserArgs().FirstOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal));
        if (argument is null) return;
        var parts = argument[prefix.Length..].Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height))
            throw new InvalidOperationException("invalid QA size argument: " + argument);
        _captureSize = new Vector2I(width, height);
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(target.GetType().FullName, name);
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, methodName);
        method.Invoke(target, null);
    }

    private static IReadOnlyList<TowerRegionDefinition> Regions() =>
    [
        GD.Load<TowerRegionDefinition>("res://content/tower/region_ember_foundry.tres"),
        GD.Load<TowerRegionDefinition>("res://content/tower/region_gloam_crypt.tres"),
        GD.Load<TowerRegionDefinition>("res://content/tower/region_crown_engine.tres")
    ];
}
