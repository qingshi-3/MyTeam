using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Growth;
using TowerAutobattler.Presentation;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class GrowthEntryInputSmoke : Node
{
    private UiInputStage _stage = null!;
    public override async void _Ready()
    {
        var exit = 0;
        GrowthGameRoot? game = null;
        var testNamespace = $"tests/growth-entry/{Guid.NewGuid():N}";
        var testSave = new SaveService(testNamespace);
        var productionSave = new SaveService("growth-journey");
        var productionBefore = JsonSerializer.Serialize(productionSave.LoadActiveRun());
        try
        {
            Require(DisplayServer.GetName() != "headless", "真实入口输入烟测需要渲染驱动");
            _stage = new UiInputStage(this, true);
            game = GD.Load<PackedScene>("res://scenes/app/GrowthGameRoot.tscn").Instantiate<GrowthGameRoot>();
            game.SaveNamespace = testNamespace;
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "GrowthGameRoot 发布内容");
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var menu = screens.MainMenu;
            Require(menu.IsVisibleInTree(), "成长征程从真实主菜单开始");
            var journey = menu.GetNode<Button>("Center/Panel/Menu/GrowthJourneyButton");
            Require(journey.IsVisibleInTree() && journey.Text.Contains("返回主菜单"), "成长入口已识别当前独立玩法，未套用普通菜单身份");

            await Click(menu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "新征程打开真实六选二");
            var candidates = Descendants<HeroLibraryTile>(screens.HeroSelection).Where(tile => tile.IsVisibleInTree()).ToArray();
            Require(candidates.Length == 6, "成长开局提供六名真实候选");
            await Click(candidates[0]);
            await Click(candidates[1]);
            var confirm = screens.HeroSelection.GetNode<Button>("%ConfirmOpening");
            Require(!confirm.Disabled, "两次真实候选点击允许确认");
            await Click(confirm);
            await Until(() => screens.Deployment.IsVisibleInTree(), "确认后恢复首战 PendingNode 并进入真实备战");
            Require(!screens.Tower.IsVisibleInTree(), "首战恢复直接进入备战，不错误停留在路线页");
            Require(_stage.Viewport.GuiGetFocusOwner() is not null, "进入备战后保留键盘焦点");

            var army = game.GetNode<ArmyOverviewController>("ArmyOverview");
            await Click(army.GetNode<Button>("%SummaryButton"));
            Require(army.IsOpen, "军团入口真实点击打开");
            var pages = army.GetNode<TabContainer>("%Pages");
            var toggle = army.GetNode<Button>("%PageToggle");
            await Click(toggle);
            await Click(toggle);
            Require(pages.CurrentTab == 2 && army.GrowthPanel.IsVisibleInTree(), "军团页真实切换到成长工坊");
            const string workbenchCapture = "res://design-discussion/04-content-validation/artifacts/growth-route/runtime/growth-workbench-entry.png";
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://design-discussion/04-content-validation/artifacts/growth-route/runtime"));
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Require(_stage.Viewport.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath(workbenchCapture)) == Error.Ok,
                "保存真实入口成长工坊主题截图");
            await Click(toggle);
            Require(pages.CurrentTab == 0, "成长工坊可返回英雄页");
            await Key(Godot.Key.Escape);
            await Until(() => !army.IsOpen, "Escape 完成军团抽屉关闭");
            Require(!army.IsOpen && _stage.Viewport.GuiGetFocusOwner() is not null, "Escape 关闭工坊并恢复焦点");

            var start = screens.Deployment.GetNode<Button>("%StartBattleButton");
            Require(start.IsVisibleInTree() && !start.Disabled, "真实备战开始按钮可用");
            await Click(start);
            await Until(() => screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle, "真实开始接入成长开战事务");
            screens.Battle.SetPaused(true);
            screens.Battle.StopBattle();
            Require(JsonSerializer.Serialize(productionSave.LoadActiveRun()) == productionBefore,
                "唯一测试命名空间没有读取或覆盖 growth-journey 正式存档");
            GD.Print("GROWTH_ENTRY_INPUT_OK menu opening deployment army-growth battle-start isolated-save");
        }
        catch (Exception error) { exit = 1; GD.PrintErr("GROWTH_ENTRY_INPUT_FAILED " + error); }
        finally
        {
            game?.QueueFree();
            await Frames(3);
            testSave.DeleteActiveRun();
        }
        GetTree().Quit(exit);
    }

    private async Task Click(Control target)
    {
        Require(target.IsVisibleInTree(), "点击目标不可见：" + target.GetPath());
        var point = target.GetGlobalRect().GetCenter();
        Require(_stage.Viewport.GetVisibleRect().HasPoint(point), "点击目标超出视口：" + target.GetPath());
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(4);
    }
    private async Task Key(Key key)
    {
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await Frames(4);
    }
    private async Task Until(Func<bool> predicate, string message)
    {
        for (var frame = 0; frame < 600 && !predicate(); frame++) await Frames(1);
        Require(predicate(), message);
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren()) { if (child is T match) yield return match; foreach (var nested in Descendants<T>(child)) yield return nested; }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

