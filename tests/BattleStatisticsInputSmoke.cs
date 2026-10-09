using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Presentation;
using TowerAutobattler.UI;

public partial class BattleStatisticsInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private const string Output = "res://outputs/battle-statistics-20261006/input";
    public override async void _Ready()
    {
        try
        {
            _stage = new UiInputStage(this, isolated: true);
            _stage.Viewport.GuiEmbedSubwindows = true;
            var root = GD.Load<PackedScene>("res://scenes/app/ContentRebuildRoot.tscn").Instantiate<ContentRebuildRoot>();
            _stage.AddChild(root);
            await Until(() => root.ContentReady, "published content");
            await Click(root.GetNode<Button>("%Prototype"));
            var panel = root.GetNode<Control>("PrototypePanel");
            await Click(panel.GetNode<Button>("%PrototypeStart"));
            var battle = root.GetNode<BattleScreenController>("Battle");
            var inspector = battle.GetNode<BattleInspectorDock>("%BattleInspectorDock");
            await Until(() => battle.TickIndex >= 80, "natural normal-speed fight");
            Require(battle.StatisticsRefreshCount == 0, "closed statistics builds no UI snapshots");
            await Click(inspector.GetNode<Button>("%StatisticsToggle"));
            await Settle();
            Require(inspector.WantsStatistics, "actual statistics button opens panel");
            Require(inspector.GetNode<Control>("%StatisticsDetail").IsVisibleInTree(), "opt-in detail visible");
            Require(_stage.Viewport.GetVisibleRect().Encloses(inspector.GetNode<Control>("%ExpandedInspector").GetGlobalRect()), "statistics panel inside viewport");
            var refreshStart = battle.StatisticsRefreshCount;
            await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
            var refreshes = battle.StatisticsRefreshCount - refreshStart;
            Require(refreshes is >= 2 and <= 5, "open statistics refresh throttled to 4Hz");
            await Capture("live-statistics");
            await Click(inspector.GetNode<Button>("%CloseInspector"));
            await Settle();
            await Click(battle.GetNode<Button>("%PauseButton"));
            Require(battle.IsPaused, "real pause button");
            await Click(inspector.GetNode<Button>("%StatisticsToggle"));
            await Settle();
            var pausedTick = battle.TickIndex;
            var pausedRefresh = battle.StatisticsRefreshCount;
            await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
            Require(battle.TickIndex == pausedTick && battle.StatisticsRefreshCount == pausedRefresh, "pause freezes totals and builds no repeated snapshot");
            await Click(inspector.GetNode<Button>("%StatEnemies"));
            await SelectThroughKeys(inspector.GetNode<OptionButton>("%StatMetric"), 4);
            var firstRow = inspector.GetNode<VBoxContainer>("%StatRows").GetChildren().OfType<BattleStatRow>().First(row => row.IsVisibleInTree());
            await Click(firstRow);
            var detail = inspector.GetNode<RichTextLabel>("%StatisticsDetailText");
            Require(detail.Text.Contains("受到硬控") && detail.Text.Contains("伤害组成") && detail.Text.Contains("来源"), "selected row shows breakdown");
            await Click(inspector.GetNode<Button>("%ExportStatistics"));
            Require(File.Exists(battle.LastStatisticsExportPath), "real export button writes file");
            using (var json = JsonDocument.Parse(File.ReadAllText(battle.LastStatisticsExportPath)))
            {
                Require(json.RootElement.GetProperty("Ticks").GetInt32() == pausedTick, "export exact simulation tick");
                var units = json.RootElement.GetProperty("Units").EnumerateArray().ToArray();
                Require(units.Any(unit => unit.GetProperty("DetailedStatistics").GetProperty("HardControlTicksTaken").GetInt32() > 0), "real prototype controls produce positive duration");
                Require(units.Any(unit => unit.GetProperty("DetailedStatistics").GetProperty("DamageSources").GetArrayLength() > 1), "damage composition contains multiple real sources");
            }
            Directory.CreateDirectory(ProjectSettings.GlobalizePath(Output));
            File.Copy(battle.LastStatisticsExportPath, ProjectSettings.GlobalizePath(Output + "/paused-battle.json"), overwrite: true);
            await Capture("control-detail");
            var scrollPoint = detail.GetGlobalRect().GetCenter();
            for (var i = 0; i < 8; i++)
            {
                _stage.Push(new InputEventMouseMotion { Position = scrollPoint, GlobalPosition = scrollPoint });
                _stage.Push(new InputEventMouseButton { Position = scrollPoint, GlobalPosition = scrollPoint, ButtonIndex = MouseButton.WheelDown, Pressed = true });
                _stage.Push(new InputEventMouseButton { Position = scrollPoint, GlobalPosition = scrollPoint, ButtonIndex = MouseButton.WheelDown, Pressed = false });
                await Draw();
            }
            Require(detail.GetVScrollBar().Value > 0, "actual wheel scroll reaches full statistics");
            await Capture("damage-breakdown");
            await Click(inspector.GetNode<Button>("%InspectStatisticsUnit"));
            Require(inspector.GetNode<Label>("%InspectorHeading").Text == "单位详情", "unit information remains available");
            await Click(inspector.GetNode<Button>("%CloseInspector"));
            await Settle();
            await Click(battle.GetNode<Button>("%PauseButton"));
            await Click(battle.GetNode<Button>("%SpeedButton"));
            await Click(battle.GetNode<Button>("%SpeedButton"));
            Require(battle.SpeedScale == 4, "real speed controls");
            await Until(() => battle.TerminalResult is not null, "natural terminal result", 120000);
            await Settle();
            Require(inspector.WantsStatistics, "terminal experiment opens frozen statistics");
            Require(!battle.GetNode<Control>("%EndFadeOverlay").Visible, "terminal analysis is not hidden by black fade");
            Require(inspector.GetNode<Label>("%StatisticsClock").Text.Contains("统计冻结"), "terminal report labelled frozen");
            var terminalTick = battle.TickIndex;
            await Click(inspector.GetNode<Button>("%StatAllies"));
            await SelectThroughKeys(inspector.GetNode<OptionButton>("%StatMetric"), 0);
            await Click(inspector.GetNode<VBoxContainer>("%StatRows").GetChildren().OfType<BattleStatRow>().First(row => row.IsVisibleInTree()));
            await Click(inspector.GetNode<Button>("%ExportStatistics"));
            using (var json = JsonDocument.Parse(File.ReadAllText(battle.LastStatisticsExportPath)))
                Require(json.RootElement.GetProperty("Terminal").GetBoolean() && json.RootElement.GetProperty("Ticks").GetInt32() == terminalTick, "terminal export frozen");
            File.Copy(battle.LastStatisticsExportPath, ProjectSettings.GlobalizePath(Output + "/terminal-battle.json"), overwrite: true);
            await Capture("terminal-statistics");
            await Click(inspector.GetNode<Button>("%CloseInspector"));
            await Settle();
            await Click(battle.GetNode<Button>("%ResetBattleButton"));
            Require(battle.TickIndex < terminalTick && !battle.IsEnding && battle.StatisticsRefreshCount == 0 && battle.LastStatisticsExportPath.Length == 0, "reset clears statistics/export state");
            await Click(battle.GetNode<Button>("%ReturnConfigurationButton"));
            Require(panel.IsVisibleInTree() && !battle.HasActiveBattle, "return to retained configuration");
            Require(battle.LastRuntimeFailure.Length == 0, "no runtime failure");
            root.QueueFree(); await Draw();
            GD.Print($"BATTLE_STATISTICS_INPUT_OK actual GUI open closed=0 refreshes/second={refreshes} pause team metric row export inspect natural-end reset return 960x540 isolated");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("BATTLE_STATISTICS_INPUT_FAILED " + error); GetTree().Quit(1); }
    }

    private async Task SelectThroughKeys(OptionButton pick, int index)
    {
        pick.GrabFocus(); await Draw(); var current = pick.Selected;
        await PressKey(Key.Space);
        for (var i = current; i > index; i--) await PressKey(Key.Up);
        for (var i = current; i < index; i++) await PressKey(Key.Down);
        await PressKey(Key.Enter);
        Require(pick.Selected == index, "metric changed by actual keys");
    }
    private async Task PressKey(Key key)
    {
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await Draw(); await Draw();
    }
    private async Task Click(Button button)
    {
        Require(button.IsVisibleInTree() && !button.Disabled, "clickable " + button.Name);
        await Draw(); var point = button.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }); await Draw();
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }); await Draw();
    }
    private async Task Draw() { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw); }
    private async Task Settle() { await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout); await Draw(); }
    private async Task Until(Func<bool> condition, string label, ulong timeout = 30000)
    {
        var end = Time.GetTicksMsec() + timeout;
        while (!condition()) { if (Time.GetTicksMsec() > end) throw new InvalidOperationException("Timeout: " + label); await Draw(); }
    }
    private async Task Capture(string name)
    {
        await Draw(); Directory.CreateDirectory(ProjectSettings.GlobalizePath(Output));
        Require(_stage.Viewport.GetTexture().GetImage().SavePng(Output + "/" + name + ".png") == Error.Ok, "capture");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
