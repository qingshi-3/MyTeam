using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Presentation;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class GameUiChangedScreensCapture : Node
{
    private const string OutputPath = "res://.godot/qa";

    public override async void _Ready()
    {
        var code = await CaptureAsync();
        GetTree().Quit(code);
    }

    private async Task<int> CaptureAsync()
    {
        GameRoot? root = null;
        try
        {
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(OutputPath));
            const string saveNamespace = "tests/ui-changed-capture";
            new SaveService(saveNamespace).DeleteActiveRun();
            root = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
            root.SaveNamespace = saveNamespace;
            AddChild(root);
            for (var frame = 0; frame < 12 && root.Content is null; frame++) await RenderFrame();
            if (root.Content is null) throw new InvalidOperationException("GameRoot content gate did not finish");
            var app = GetApplication(root);

            root.Flow.ShowHeroSelection();
            await Capture("HeroSelectLayered.png");

            var heroId = app.Meta.UnlockedHeroIds.Contains("hero_banner_marshal")
                ? "hero_banner_marshal"
                : app.Meta.UnlockedHeroIds.First();
            if (!app.StartNewRun(heroId, 9191)) throw new InvalidOperationException("capture run did not start");
            var run = app.ActiveRun!;
            run.Gold = 99;
            root.Flow.ShowTower();
            await Capture("TowerResourceHierarchy.png");
            root.GetNode<Button>("ArmyOverview/SummaryButton").EmitSignal(BaseButton.SignalName.Pressed);
            await Capture("ArmyResourceDetail.png");
            root.GetNode<Button>("ArmyOverview/Drawer/Layout/Header/CloseButton").EmitSignal(BaseButton.SignalName.Pressed);

            run.SelectedNode = TowerNodeType.Combat;
            run.PendingNode = true;
            root.Flow.OpenSelectedNode();
            await Capture("DeploymentDefault.png");
            var board = root.GetNode<DeploymentBoard>("Screens/DeploymentScreen/Margin/Layout/Columns/BoardPanel/DeploymentBoard");
            var startingInstanceId = run.Roster[0].InstanceId;
            var heroCell = board.GetChildren().OfType<DeploymentCell>().Single(cell => cell.PieceId == startingInstanceId);
            heroCell.EmitSignal(BaseButton.SignalName.Pressed);
            await Capture("DeploymentSelectedLegalSwap.png");
            var focus = board.GetChildren().OfType<DeploymentCell>().First(cell => string.IsNullOrEmpty(cell.PieceId) && cell.IsLegalTarget);
            focus.GrabFocus();
            await Capture("DeploymentFocus.png");
            focus.Bind(focus.Cell, string.Empty, string.Empty, false, false, false, FloorCellPreview.Blocked,
                hasSelection: true);
            await Capture("DeploymentIllegal.png");
            root.Flow.ShowDeployment();
            board = root.GetNode<DeploymentBoard>("Screens/DeploymentScreen/Margin/Layout/Columns/BoardPanel/DeploymentBoard");
            heroCell = board.GetChildren().OfType<DeploymentCell>().Single(cell => cell.PieceId == startingInstanceId);
            var swap = board.GetChildren().OfType<DeploymentCell>().First(cell =>
                !string.IsNullOrEmpty(cell.PieceId) && cell.PieceId != startingInstanceId);
            await BeginDrag(heroCell, swap);
            await Capture("DeploymentDragSwap.png");
            await EndDrag(swap);
            focus.FlashResult(false);
            await Capture("DeploymentFailure.png");

            root.Flow.ShowRecruitment();
            await Capture("RecruitmentLayered.png");
            root.Flow.ShowCombatReward();
            await Capture("RewardItemIdentity.png");
            root.Flow.ShowShop();
            await Capture("ShopItemIdentity.png");

            run.SelectedNode = TowerNodeType.Event;
            run.PendingNode = true;
            root.Flow.OpenSelectedNode();
            await Capture("EventOutcomeClusters.png");
            run.SelectedNode = TowerNodeType.Rest;
            run.PendingNode = true;
            root.Flow.OpenSelectedNode();
            await Capture("RestOutcomeClusters.png");

            root.Flow.ShowSettings();
            await Capture("SettingsHierarchy.png");
            root.Flow.ShowResult("征程结算", "关键结果保留在按需详情层；主行动保持清晰可见。");
            await Capture("ResultHierarchy.png");

            var encounter = app.Tower.Encounter(run, TowerNodeType.Combat);
            var battle = root.GetNode<BattleScreenController>("Screens/BattleScreen");
            root.Flow.Show(AppScreenId.Battle);
            battle.StartBattle(app.Content, app.BuildBattleConfig(encounter), encounter.Title);
            battle._Process(1.0);
            var battleBoard = root.GetNode<BattleBoard>("Screens/BattleScreen/Margin/Layout/BattleArea/BattleBoard");
            var heroPoint = battleBoard.GlobalPosition + battleBoard.CellToLocal(
                BattlefieldLayout.PlayerDeploymentCells[run.Deployment.IndexOf(startingInstanceId)]);
            GetViewport().PushInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left, Pressed = true, Position = heroPoint, GlobalPosition = heroPoint
            }, true);
            await Capture("BattleSemanticStatus.png");

            GD.Print($"GAME_UI_CHANGED_SCREENS_CAPTURE_OK size={GetViewport().GetVisibleRect().Size} captures=16 path={OutputPath}");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("GAME_UI_CHANGED_SCREENS_CAPTURE_FAILED: " + exception);
            return 1;
        }
        finally
        {
            if (root is not null && GodotObject.IsInstanceValid(root))
            {
                if (root.GetParent() is not null) root.GetParent().RemoveChild(root);
                root.Free();
            }
            new SaveService("tests/ui-changed-capture").DeleteActiveRun();
        }
    }

    private async Task Capture(string fileName)
    {
        await RenderFrame();
        var image = GetViewport().GetTexture().GetImage();
        var name = $"UI_{image.GetWidth()}x{image.GetHeight()}_{fileName}";
        var error = image.SavePng($"{ProjectSettings.GlobalizePath(OutputPath)}/{name}");
        if (error != Error.Ok) throw new InvalidOperationException($"capture {name}: {error}");
    }

    private async Task RenderFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }

    private async Task BeginDrag(Control source, Control target)
    {
        var start = source.GetGlobalRect().GetCenter();
        var end = target.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = start, GlobalPosition = start }, true);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = true, Position = start, GlobalPosition = start
        }, true);
        await RenderFrame();
        for (var step = 1; step <= 3; step++)
        {
            var point = start.Lerp(end, step / 3f);
            GetViewport().PushInput(new InputEventMouseMotion
            {
                Position = point, GlobalPosition = point, Relative = (end - start) / 3f,
                ButtonMask = MouseButtonMask.Left
            }, true);
            await RenderFrame();
        }
    }

    private async Task EndDrag(Control target)
    {
        var point = target.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = false, Position = point, GlobalPosition = point
        }, true);
        await RenderFrame();
    }

    private static RunApplication GetApplication(GameRoot root)
    {
        var field = typeof(GameRoot).GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(root) as RunApplication ?? throw new InvalidOperationException("GameRoot application unavailable");
    }

    private static void InvokePrivate(object target, string methodName, params object[] arguments)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, methodName);
        method.Invoke(target, arguments);
    }
}
