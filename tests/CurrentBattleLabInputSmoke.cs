using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Exercises the authored application and GUI. All setup writes use a unique
// test namespace; production saves and personal laboratory presets are read-only.
public partial class CurrentBattleLabInputSmoke : Node
{
    private const string Output = "res://outputs/battle-lab-restore-20261006/input";
    private UiInputStage _stage = null!;
    private AppScreenHost? _screens;
    private string _step = "boot";

    public override async void _Ready()
    {
        GameRoot? game = null;
        var exit = 0;
        var isolatedNamespace = $"tests/current-battle-lab/{Guid.NewGuid():N}";
        var isolatedDirectory = ProjectSettings.GlobalizePath("user://" + isolatedNamespace);
        Dictionary<string, string>? productionBefore = null;
        try
        {
            Require(DisplayServer.GetName() != "headless", "input smoke requires a rendering driver");
            _stage = new UiInputStage(this, isolated: true);
            _stage.Viewport.GuiEmbedSubwindows = true;
            var mainScene = ProjectSettings.GetSetting("application/run/main_scene").AsString();
            Require(mainScene == "res://scenes/app/GrowthGameRoot.tscn", "production main scene");
            game = GD.Load<PackedScene>(mainScene).Instantiate<GameRoot>();
            var productionNamespace = game.SaveNamespace;
            productionBefore = FingerprintProduction(productionNamespace);
            game.SaveNamespace = isolatedNamespace;
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "production content publication");
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            _screens = screens;
            await Until(() => screens.MainMenu.IsVisibleInTree(), "production main menu");

            // Create a supported current run through normal input, then preserve it
            // throughout laboratory work. No fixture can migrate a real player save.
            _step = "isolated run setup";
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "opening recruitment");
            await Settle();
            var candidates = Descendants<HeroLibraryTile>(screens.HeroSelection)
                .Where(card => card.IsVisibleInTree()).ToArray();
            Require(candidates.Length == 6, "current opening has six candidates");
            await Click(candidates[0]);
            await Click(candidates[1]);
            await Click(screens.HeroSelection.GetNode<Button>("%ConfirmOpening"));
            await Until(() => screens.Expedition.IsVisibleInTree(), "isolated current run starts");
            await Settle();
            await Click(screens.Expedition.GetNode<Button>("%MenuButton"));
            await Until(() => screens.MainMenu.IsVisibleInTree(), "run remains available at menu");
            await Settle();
            var isolatedSave = new SaveService(isolatedNamespace);
            var runBefore = JsonSerializer.Serialize(isolatedSave.LoadActiveRun());
            Require(isolatedSave.LoadActiveRun() is not null, "existing supported run fixture");
            var isolatedBefore = FingerprintDirectory(isolatedDirectory);

            _step = "main menu laboratory entry";
            await EnterLab(screens);
            var lab = screens.BattleLab;
            var initial = lab.CurrentSnapshot ?? throw new InvalidOperationException("lab session missing");
            Require(initial.Mode == BattleLabPlacementMode.FreeExperiment && initial.Units.Length == 4,
                "current default preset has two units on each side");
            Require(initial.Units.Any(unit => unit.ContentId == "prototype_u02") &&
                    initial.Units.Any(unit => unit.ContentId == "enemy_ee03_trample_brute"),
                "default preset uses current hero and retained enemy");
            Require(initial.Relics.Length == 0 && initial.Units.All(unit => unit.Equipment.Length == 0),
                "removed equipment and relics are absent");
            Require(lab.CellCount == 60 && lab.GetNode<Control>("%Battlefield").IsVisibleInTree(),
                "full editable board is visible");
            await Capture("01-configuration");

            _step = "current library and pointer placement";
            await Click(lab.GetNode<Button>("%ToggleLibraryButton"));
            await Settle();
            var library = Descendants<BattleLabLibraryCard>(lab).ToArray();
            var publishedIds = game.Content!.Catalog.AllEntries()
                .Where(entry => entry.Definition is UnitDefinition).Select(entry => entry.StableId)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
            Require(library.Select(card => card.ContentId).OrderBy(id => id, StringComparer.Ordinal)
                    .SequenceEqual(publishedIds), "library exposes all current published units");
            Require(library.Any(card => card.ContentId == "prototype_u02") &&
                    library.Any(card => card.ContentId == "enemy_ee07_furnace_lizard"),
                "current published units are selectable");
            await Capture("02-current-library");
            await Click(lab.GetNode<LineEdit>("%UnitSearch"));
            foreach (var character in "prototype_u02")
                await Key((Key)0, character);
            await Settle();
            var source = Descendants<BattleLabLibraryCard>(lab)
                .Single(card => card.IsVisibleInTree() && card.ContentId == "prototype_u02");
            var target = Descendants<BattleLabBoardCell>(lab)
                .Single(cell => cell.Cell == new Vector2I(1, 1));
            await Drag(source, target);
            var configured = lab.CurrentSnapshot ?? throw new InvalidOperationException("edited session missing");
            Require(configured.Units.Length == initial.Units.Length + 1 &&
                    configured.Units.Any(unit => unit.Cell == new Vector2I(1, 1) &&
                        unit.ContentId == "prototype_u02" && unit.Side == BattleLabSide.Player),
                "real library drag places exactly one current unit");
            var configurationDigest = configured.CanonicalDigest;
            await Capture("03-pointer-placement");

            _step = "normal laboratory battle";
            await Click(lab.GetNode<Button>("%StartButton"));
            await Until(() => screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle,
                "production laboratory battle starts");
            var battle = screens.Battle;
            await Until(() => battle.TickIndex >= 45, "normal speed battle progression");
            Require(battle.LastRuntimeFailure.Length == 0 && battle.SpeedScale == 1,
                "normal production playback has no runtime failure");
            await Capture("04-normal-battle");
            await Click(battle.GetNode<Button>("%PauseButton"));
            Require(battle.IsPaused, "pause through actual button");
            var pausedTick = battle.TickIndex;
            await ToSignal(GetTree().CreateTimer(.2), SceneTreeTimer.SignalName.Timeout);
            Require(battle.TickIndex == pausedTick, "paused clock is stable");
            await Click(battle.GetNode<Button>("%StepButton"));
            Require(battle.IsPaused && battle.TickIndex == pausedTick + 1,
                "actual step button advances exactly one fixed tick");

            _step = "battle statistics";
            var inspector = battle.GetNode<BattleInspectorDock>("%BattleInspectorDock");
            await Click(inspector.GetNode<Button>("%StatisticsToggle"));
            await Settle();
            Require(inspector.WantsStatistics &&
                    inspector.GetNode<Control>("%StatisticsDetail").IsVisibleInTree(),
                "lab opts into detailed statistics");
            var row = inspector.GetNode<VBoxContainer>("%StatRows").GetChildren()
                .OfType<BattleStatRow>().First(item => item.IsVisibleInTree());
            await Click(row);
            var detail = inspector.GetNode<RichTextLabel>("%StatisticsDetailText");
            Require(detail.Text.Contains("伤害组成") && detail.Text.Contains("受到硬控"),
                "actual statistics row shows detailed battle facts");
            Require(battle.IsPaused && battle.TickIndex == pausedTick + 1,
                "statistics inspection preserves playback state");
            await Capture("05-paused-statistics");
            await Click(inspector.GetNode<Button>("%CloseInspector"));
            await Settle();

            _step = "reset and return";
            await Click(battle.GetNode<Button>("%ResetBattleButton"));
            Require(battle.HasActiveBattle && !battle.IsPaused && battle.TickIndex < pausedTick,
                "actual reset starts a fresh runtime");
            await Until(() => battle.TickIndex >= 15, "reset battle progresses");
            await Capture("06-reset-battle");
            await Click(battle.GetNode<Button>("%ReturnConfigurationButton"));
            await Until(() => lab.IsVisibleInTree(), "return to configuration");
            await Settle();
            Require(!battle.HasActiveBattle && lab.CurrentSnapshot?.CanonicalDigest == configurationDigest,
                "return cleans runtime and preserves configuration");

            _step = "exit and re-entry";
            await Click(lab.GetNode<Button>("%BackButton"));
            await Until(() => screens.MainMenu.IsVisibleInTree(), "lab exit returns to main menu");
            await EnterLab(screens);
            Require(lab.CurrentSnapshot?.CanonicalDigest == configurationDigest && !battle.HasActiveBattle,
                "re-entry binds the same clean editable session");
            await Click(lab.GetNode<Button>("%StartButton"));
            await Until(() => battle.HasActiveBattle && battle.IsVisibleInTree(), "re-entered lab can start");
            await Click(battle.GetNode<Button>("%ReturnConfigurationButton"));
            await Until(() => lab.IsVisibleInTree(), "re-entered battle returns");
            Require(!battle.HasActiveBattle && lab.CurrentSnapshot?.CanonicalDigest == configurationDigest,
                "re-entry has no duplicate transition or configuration mutation");
            await Capture("07-reentered-configuration");
            Require(JsonSerializer.Serialize(isolatedSave.LoadActiveRun()) == runBefore,
                "existing supported run semantic state is unchanged");
            Require(SameFingerprint(isolatedBefore, FingerprintDirectory(isolatedDirectory)),
                "isolated run files preserve bytes, size and modification timestamps");
            Require(SameFingerprint(productionBefore, FingerprintProduction(productionNamespace)),
                "production saves and personal presets preserve bytes and metadata");
            GD.Print($"CURRENT_BATTLE_LAB_INPUT_OK main=GrowthGameRoot library={publishedIds.Length} pointer-place " +
                     "normal-speed pause one-tick statistics reset return exit reentry " +
                     "host=960x540 viewport=1600x900 save=isolated-byte-and-metadata-unchanged");
        }
        catch (Exception error)
        {
            GD.PrintErr("CURRENT_BATTLE_LAB_INPUT_FAILED " + _step + ": " + error);
            exit = 1;
        }
        finally
        {
            game?.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (Directory.Exists(isolatedDirectory)) Directory.Delete(isolatedDirectory, recursive: true);
        }
        GetTree().Quit(exit);
    }

    private async Task EnterLab(AppScreenHost screens)
    {
        await Settle();
        var button = screens.MainMenu.GetNode<Button>("Center/Panel/Menu/BattleLabButton");
        if (!button.IsVisibleInTree())
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
        await Click(button);
        await Until(() => screens.BattleLab.IsVisibleInTree() && screens.BattleLab.CurrentSnapshot is not null,
            "real main-menu battle laboratory entry");
        await Settle();
    }

    private async Task Click(Control control)
    {
        if (_screens is not null)
            await Until(() => !_screens.IsTransitioning, "page transition completes before input");
        await Draw();
        Require(control.IsVisibleInTree() && control is not Button { Disabled: true },
            "clickable control: " + control.Name);
        var position = control.GetGlobalRect().GetCenter();
        Require(_stage.Viewport.GetVisibleRect().HasPoint(position), "control within isolated viewport: " + control.Name);
        _stage.Push(new InputEventMouseMotion { Position = position, GlobalPosition = position });
        _stage.Push(new InputEventMouseButton { Position = position, GlobalPosition = position,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        await Draw();
        _stage.Push(new InputEventMouseButton { Position = position, GlobalPosition = position,
            ButtonIndex = MouseButton.Left, Pressed = false });
        await Draw();
    }

    private async Task Key(Key key, uint unicode = 0)
    {
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = unicode, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = unicode, Pressed = false });
        await Draw();
    }

    private async Task Drag(Control source, Control target)
    {
        await Draw();
        var start = source.GetGlobalRect().GetCenter();
        var end = target.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = start, GlobalPosition = start });
        _stage.Push(new InputEventMouseButton { Position = start, GlobalPosition = start,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        await Draw();
        var previous = start;
        for (var step = 1; step <= 12; step++)
        {
            var point = start.Lerp(end, step / 12f);
            _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point,
                Relative = point - previous, ButtonMask = MouseButtonMask.Left });
            previous = point;
            await Draw();
        }
        _stage.Push(new InputEventMouseButton { Position = end, GlobalPosition = end,
            ButtonIndex = MouseButton.Left, Pressed = false });
        await Settle();
    }

    private async Task Draw()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw(false);
    }
    private async Task Settle()
    {
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        await Draw();
    }
    private async Task Until(Func<bool> condition, string label, ulong timeout = 30000)
    {
        var deadline = Time.GetTicksMsec() + timeout;
        while (!condition())
        {
            if (Time.GetTicksMsec() > deadline) throw new InvalidOperationException("Timeout: " + label);
            await Draw();
        }
    }
    private async Task Capture(string name)
    {
        await Draw();
        Directory.CreateDirectory(ProjectSettings.GlobalizePath(Output));
        Require(_stage.Viewport.GetTexture().GetImage().SavePng(Output + "/" + name + ".png") == Error.Ok,
            "capture " + name);
    }
    private static Dictionary<string, string> FingerprintDirectory(string directory)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!Directory.Exists(directory)) return result;
        foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            AddFingerprint(result, path);
        return result;
    }
    private static Dictionary<string, string> FingerprintProduction(string sceneNamespace)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in new[] { "meta.json", "settings.json", "active_run.json" })
            AddFingerprint(result, ProjectSettings.GlobalizePath("user://" + name));
        foreach (var space in new[] { sceneNamespace, "growth-journey", "eight-trait-journey", "battle_lab" }
                     .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct())
            foreach (var pair in FingerprintDirectory(ProjectSettings.GlobalizePath("user://" + space)))
                result[pair.Key] = pair.Value;
        return result;
    }
    private static void AddFingerprint(Dictionary<string, string> result, string path)
    {
        if (!File.Exists(path)) return;
        var info = new FileInfo(path);
        result[path] = info.Length + ":" + info.LastWriteTimeUtc.Ticks + ":" +
                       Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    }
    private static bool SameFingerprint(Dictionary<string, string> before, Dictionary<string, string> after) =>
        before.Count == after.Count && before.All(pair => after.TryGetValue(pair.Key, out var value) && value == pair.Value);
    private static IEnumerable<T> Descendants<T>(Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T match) yield return match;
            foreach (var match2 in Descendants<T>(child)) yield return match2;
        }
    }
    private static void Require(bool condition, string label)
    { if (!condition) throw new InvalidOperationException(label); }
}
