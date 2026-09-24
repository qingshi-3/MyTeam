using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Focused rendered flow checks. Prepared reward/route fixtures use isolated saves;
// there is no simulated battle, fabricated victory, or real player save access.
public partial class RewardContinueInputSmoke : Node
{
    private SubViewport _viewport = null!;
    private GameRoot? _game;
    private string _step = "boot";

    public override async void _Ready()
    {
        var code = 0;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered input check requires a window");
            _viewport = GetNode<SubViewport>("TestViewport");
            await Boot($"tests/reward-continue/bootstrap-{Guid.NewGuid():N}");
            var content = _game!.Content!;
            var compiled = GameProjectCompiler.Compile(_game.ProjectDefinition!, content.Graph);
            var project = compiled.Project ?? throw new InvalidOperationException("production project failed to compile");
            await RemoveGame();
            await Reward(content, project);
            await Recruitment(content, project);
            GD.Print("REWARD_CONTINUE_INPUT_OK reward=direct-to-tower save-failure=retained retry=once equipment=hero-view recruitment=direct-to-tower fixtures=isolated battle=not-run");
        }
        catch (Exception exception) { code = 1; GD.PrintErr($"REWARD_CONTINUE_INPUT_FAILED step={_step}: {exception}"); }
        finally { await RemoveGame(); }
        GetTree().Quit(code);
    }

    private async Task Reward(ContentRegistry content, CompiledGameProject project)
    {
        _step = "prepare pending equipment reward";
        var space = $"tests/reward-continue/reward-{Guid.NewGuid():N}";
        var save = new SaveService(space);
        var app = new RunApplication(content, save, project);
        var decisions = new RunDecisionService(content, project, new RunProgressionPersistenceService(content, save, project));
        CompiledRunChoice? equipment = null;
        for (ulong seed = 1; seed <= 64 && equipment is null; seed++)
        {
            Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], seed), "reward fixture run");
            app.ActiveRun!.PendingOffer = decisions.CreateOffer(app.ActiveRun, RunOfferKind.CombatReward);
            equipment = app.PendingOffer!.Choices.FirstOrDefault(choice => choice.Operations.Any(op =>
                op.Kind == RunOperationKind.GrantItem && content.TryGet(op.ContentId, out var entry)
                && entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Equipment }));
        }
        Require(equipment is not null && save.SaveActiveRun(app.ActiveRun!), "persist legitimate pending equipment choice");
        var floor = app.ActiveRun!.FloorIndex;
        var inventoryCount = app.ActiveRun.EquipmentInventory.Count;
        var equipmentId = equipment!.Operations.First(op => op.Kind == RunOperationKind.GrantItem
            && content.TryGet(op.ContentId, out var entry)
            && entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Equipment }).ContentId;
        await Boot(space);
        var screens = Screens;
        await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
        Require(screens.Reward.IsVisibleInTree(), "Continue resumes unclaimed reward");
        var card = Descendants<RunOfferChoiceCard>(screens.Reward).Single(value => value.StableId == equipment.StableId);
        var path = ProjectSettings.GlobalizePath($"user://{space}/active_run.json");
        var savedBefore = File.ReadAllText(path);
        _step = "save failure retains reward";
        // Windows permits the save read but rejects replacing this isolated file.
        using (File.Open(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
        {
            await Click(card.ConfirmButton);
            Require(screens.Reward.IsVisibleInTree() && !screens.Tower.IsVisibleInTree()
                && File.ReadAllText(path) == savedBefore, "failed save leaves both page and saved entitlement intact");
            Require(screens.Reward.GetNode<Label>("Center/Panel/Layout/Hint").Text.Contains("保存失败"), "failed save exposes retry feedback");
        }
        _step = "retry equipment reward";
        await Click(card.ConfirmButton);
        Require(screens.Tower.IsVisibleInTree() && !screens.Reward.IsVisibleInTree(), "successful reward immediately returns to route choices");
        var saved = save.LoadActiveRun()!;
        Require(saved.PendingOffer is null && saved.FloorIndex == floor && saved.EquipmentInventory.Count == inventoryCount + 1,
            "retry grants equipment once, consumes offer, and never advances an extra floor");
        await Capture("reward-return-to-tower");
        _step = "reward equipment in hero manager";
        var army = _game!.GetNode<ArmyOverviewController>("ArmyOverview");
        await Click(army.GetNode<Button>("%SummaryButton"));
        var view = army.GetNode<RosterLoadoutView>("%ArmyEquipmentPanel");
        var instance = saved.EquipmentInventory.Last(value => value.ContentId == equipmentId);
        Require(army.IsOpen && view.IsVisibleInTree()
            && Descendants<EquipmentSlotButton>(view).Any(value => value.InstanceId == instance.InstanceId),
            "new equipment is immediately visible in the existing hero manager");
        await Capture("reward-in-hero-manager");
        await RemoveGame();
    }

    private async Task Recruitment(ContentRegistry content, CompiledGameProject project)
    {
        _step = "prepare recruitment route";
        var space = $"tests/reward-continue/recruitment-{Guid.NewGuid():N}";
        var save = new SaveService(space);
        var app = new RunApplication(content, save, project);
        for (ulong seed = 1; seed <= 64; seed++)
        {
            Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], seed), "recruitment fixture run");
            if (app.CurrentOptions().Any(value => value.Type == TowerNodeType.Recruitment)) break;
        }
        Require(app.SelectNode(TowerNodeType.Recruitment), "select offered recruitment route");
        var floor = app.ActiveRun!.FloorIndex;
        var heroes = app.ActiveRun.Roster.Count;
        await Boot(space);
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
        Require(Screens.Recruitment.IsVisibleInTree(), "Continue resumes recruitment");
        var card = Descendants<RunOfferChoiceCard>(Screens.Recruitment).First(value => !value.ConfirmButton.Disabled);
        await Click(card.ConfirmButton);
        var saved = save.LoadActiveRun()!;
        Require(Screens.Tower.IsVisibleInTree() && !Screens.Recruitment.IsVisibleInTree() && saved.PendingOffer is null
            && saved.FloorIndex == floor + 1 && saved.Roster.Count == heroes + 1,
            "recruitment returns directly, adds one hero, and advances exactly one floor");
        await RemoveGame();
    }

    private AppScreenHost Screens => _game!.GetNode<AppScreenHost>(_game.ScreenHostPath);
    private async Task Boot(string space)
    {
        _game = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
        _game.SaveNamespace = space;
        _viewport.AddChild(_game);
        for (var i = 0; i < 300 && _game.Content is null; i++) await Frames(1);
        Require(_game.Content is not null, "production bootstrap");
        await Frames(4);
    }
    private async Task RemoveGame() { _game?.QueueFree(); _game = null; await Frames(2); }
    private async Task Click(Control control)
    {
        Require(control.IsVisibleInTree(), "visible input target: " + control.GetPath());
        var point = control.GetGlobalRect().GetCenter();
        _viewport.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        _viewport.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }, true);
        _viewport.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }, true);
        await Frames(4);
    }
    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
    private async Task Capture(string name)
    {
        await Frames(1);
        var folder = ProjectSettings.GlobalizePath("res://.godot/ui-review");
        Directory.CreateDirectory(folder);
        Require(_viewport.GetTexture().GetImage().SavePng(Path.Combine(folder, name + ".png")) == Error.Ok, "capture " + name);
    }
    private static IEnumerable<T> Descendants<T>(Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T value) yield return value;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
