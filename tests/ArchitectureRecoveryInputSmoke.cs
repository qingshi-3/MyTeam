using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Run;

public partial class ArchitectureRecoveryInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private string _directory = "";

    public override async void _Ready()
    {
        var exit = 0;
        GameRoot? game = null;
        try
        {
            Require(DisplayServer.GetName() != "headless", "需要真实渲染驱动和隔离输入");
            var space = $"tests/architecture-recovery-input/{Guid.NewGuid():N}";
            _directory = ProjectSettings.GlobalizePath("user://" + space);
            Directory.CreateDirectory(_directory);
            var meta = Path.Combine(_directory, "meta.json");
            var active = Path.Combine(_directory, "active_run.json");
            File.WriteAllText(meta, "{broken-meta");
            File.WriteAllText(active, "{broken-active");
            _stage = new UiInputStage(this, true);
            _stage.Resize(new Vector2I(960, 540));
            game = GD.Load<PackedScene>(ProjectSettings.GetSetting("application/run/main_scene").AsString()).Instantiate<GameRoot>();
            game.SaveNamespace = space;
            _stage.AddChild(game);
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            await Until(() => screens.Result.IsVisibleInTree(), "损坏 Meta 进入可操作故障页面");
            Require(game.Content is null && !screens.MainMenu.IsVisibleInTree(), "未完成启动不能暴露主菜单");
            var retry = screens.Result.GetNode<Button>("Center/Panel/Layout/NewRunButton");
            var quit = screens.Result.GetNode<Button>("Center/Panel/Layout/MenuButton");
            Require(retry.Text == "重试启动" && quit.Text == "退出游戏", "故障按钮语义正确");
            Require(File.ReadAllText(meta) == "{broken-meta", "故障页面不重置存档");
            await Capture("startup-failure.png");
            if (OS.GetCmdlineUserArgs().Contains("--exit-only"))
            {
                GD.Print("ARCHITECTURE_RECOVERY_EXIT_REQUEST");
                await Click(quit);
                await Frames(30);
                throw new InvalidOperationException("退出按钮未结束应用");
            }
            await Click(retry);
            await Until(() => screens.Result.IsVisibleInTree(), "仍有坏档时重试仍可恢复到故障页");
            Require(File.ReadAllText(meta) == "{broken-meta" && game.Content is null, "重复失败保持原档和未发布状态");

            // Simulate the user restoring their backup; only the isolated fixture changes.
            File.WriteAllText(meta, "{}");
            await Click(retry);
            await Until(() => game.Content is not null && screens.MainMenu.IsVisibleInTree(), "恢复原档后真实点击重试进入主菜单");
            Require(!screens.Result.IsVisibleInTree(), "成功后隐藏故障页");
            await Capture("recovered-menu.png");

            // A blocked backup must not overwrite the corrupt run, even via real UI.
            File.WriteAllText(active + ".backups", "block directory creation");
            GD.Print("EXPECTED_SAVE_FAILURE_BEGIN");
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            GD.Print("EXPECTED_SAVE_FAILURE_END");
            await Until(() => screens.Result.IsVisibleInTree(), "备份失败显示可重试保存错误");
            Require(File.ReadAllText(active) == "{broken-active", "真实新局备份失败保留原件");
            Require(retry.Text == "再次登塔" && quit.Text == "返回主菜单", "结果页恢复普通征程按钮，启动重试解绑");
            await Capture("save-failure.png");
            File.Delete(active + ".backups");
            await Click(retry);
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "真实重试新局进入六选二");
            var save = new SaveService(space);
            Require(save.LoadActiveRun()?.OpeningRecruitment is not null, "新草稿真实保存");
            var backup = Directory.GetFiles(active + ".backups");
            Require(backup.Length == 1 && File.ReadAllText(backup[0]) == "{broken-active", "真实新局保留完整坏档备份");
            await Capture("recovered-opening.png");
            await Click(screens.HeroSelection.GetNode<Button>("Margin/Layout/Footer/BackButton"));
            await Until(() => screens.MainMenu.IsVisibleInTree(), "恢复后普通菜单导航正常");
            GD.Print("ARCHITECTURE_RECOVERY_INPUT_OK retry=failed,restored new-run=backup-failed,recovered viewport=960x540 desktop-pointer=unchanged");
        }
        catch (Exception exception) { GD.PrintErr("ARCHITECTURE_RECOVERY_INPUT_FAILED: " + exception); exit = 1; }
        finally { game?.QueueFree(); }
        await Frames(3);
        GetTree().Quit(exit);
    }

    public override void _ExitTree()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    private async Task Until(Func<bool> condition, string message)
    {
        for (var i = 0; i < 180 && !condition(); i++) await Frames(1);
        Require(condition(), message);
        await Frames(3);
    }

    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task Click(Control target)
    {
        Require(target.IsVisibleInTree(), "点击目标可见：" + target.Name);
        Require(_stage.Viewport.GetVisibleRect().Encloses(target.GetGlobalRect()), "完整按钮位于 960×540 视口：" + target.Name);
        var point = target.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(4);
    }

    private async Task Capture(string file)
    {
        await Frames(3);
        // Occluded macOS test hosts can stop automatic drawing indefinitely.
        // Force this test-only capture instead of waiting for a frame that may never arrive.
        RenderingServer.ForceDraw(false);
        var directory = ProjectSettings.GlobalizePath("res://outputs/architecture-hardening-20261001");
        Directory.CreateDirectory(directory);
        using var image = _stage.Viewport.GetTexture().GetImage();
        Require(image.SavePng(Path.Combine(directory, file)) == Error.Ok, "保存输入证据");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
