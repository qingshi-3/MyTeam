using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class GenericRelicInputSmoke : Node
{
    private UiInputStage _stage = null!;
    public override async void _Ready()
    {
        GameRoot? game = null; var exit = 0;
        try
        {
            Check(DisplayServer.GetName() != "headless", "rendered test required");
            _stage = new(this, true); _stage.Resize(new(960, 540));
            game = GD.Load<PackedScene>("res://scenes/app/GrowthGameRoot.tscn").Instantiate<GameRoot>();
            game.SaveNamespace = $"tests/generic-relics/{Guid.NewGuid():N}"; _stage.AddChild(game);
            for (var i = 0; i < 300 && game.Content is null; i++) await Frames(1);
            Check(game.Content is not null, "production content ready");
            // This UI fixture installs legacy Tower offers directly; it does not test map transactions.
            var compiled = GameProjectCompiler.Compile(
                GD.Load<GameProjectDefinition>(GrowthContentPackage.LegacyProjectPath), game.Content!.Graph);
            var package = new CompiledGamePackage(game.Content, compiled.Project!, game.Content.PublicationVersion);
            var (app, save) = RunHealthFixture.New(package);
            typeof(GameRoot).GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, app);
            var run = app.ActiveRun!;
            run.Items.Add(new() { InstanceId = "item-80", ContentId = "item_rl01_card" });
            run.Items.Add(new() { InstanceId = "item-81", ContentId = "item_rl07_bowl" });
            run.Items.Add(new() { InstanceId = "item-82", ContentId = "item_rl10_recycler" });
            run.Gold = 20; run.PendingNode = true; run.SelectedNode = TowerNodeType.Shop;
            var buy = new CompiledRunChoice("new-relic", "破阵撬棍", "", [], [new(RunOperationKind.SpendGold, 16)],
                [new(RunOperationKind.GrantItem, 1, ContentId: "item_rl11_crowbar")], FailureOperations: [], ContentId: "item_rl11_crowbar");
            run.PendingOffer = new("relic-ui-shop", RunOfferKind.Shop, "遗物逻辑验证", true, true, run.FloorIndex, run.BattleNumber, [buy]);
            save.SaveActiveRun(run); game.Flow.ShowTower(); await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout); await Frames(2);
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var card = Descendants<RunOfferChoiceCard>(screens.Shop).Single();
            Check(app.PendingOffer!.Choices[0].Costs[0].Amount == 8 && !card.ConfirmButton.Disabled, "discounted real purchase enabled");
            card.ConfirmButton.GrabFocus(); await Frames(6);
            await Capture("shop-before"); await Click(card.ConfirmButton);
            Check(run.Gold == 12 && run.Items.Any(i => i.ContentId == "item_rl11_crowbar"), $"real click grants relic and pays displayed discounted amount: gold={run.Gold}, status={screens.Shop.GetNode<Label>("Margin/Layout/Status").Text}");
            // A separate valid growth reward fixture; category groups belong to growth runs.
            app = new RunApplication(package.Content, save, package.Project, GrowthContentPackage.LoadRules(package.Content));
            Check(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 99117), "growth reward fixture");
            typeof(GameRoot).GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, app);
            run = app.ActiveRun!;
            run.Items.Add(new() { InstanceId = "item-81", ContentId = "item_rl07_bowl" });
            run.Items.Add(new() { InstanceId = "item-82", ContentId = "item_rl10_recycler" });
            run.PendingNode = false; run.SelectedNode = TowerNodeType.Combat;
            var reward = buy with { StableId = "equipment", DisplayName = "精工瞄具", Costs = [], ContentId = "equipment_field_focus",
                Operations = [new(RunOperationKind.GrantItem, 1, ContentId: "equipment_field_focus")] };
            run.PendingOffer = new("relic-ui-skip", RunOfferKind.CombatReward, "装备奖励", true, false, run.FloorIndex, run.BattleNumber, [reward])
                { RewardCategory = RunRewardCategory.Equipment };
            save.SaveActiveRun(run); game.Flow.ShowTower(); await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout); await Frames(2);
            var button = screens.Reward.GetNode<Button>("Center/Panel/Layout/ContinueButton");
            Check(button.Text.Contains("最大征程生命 +2") && button.Text.Contains("回收 +5"), "skip describes actual combined reward");
            var max = run.MaximumRunHealth; var gold = run.Gold; var equipment = run.EquipmentInventory.Count;
            await Capture("skip-before"); await Click(button);
            Check(run.MaximumRunHealth == max + 2 && run.Gold == gold + 5 && run.EquipmentInventory.Count == equipment && run.PendingOffer is null,
                $"real skip input consumes entitlement once and grants only the advertised alternative: hp={run.MaximumRunHealth}, gold={run.Gold}, hint={screens.Reward.GetNode<Label>("Center/Panel/Layout/Hint").Text}");
            await Capture("skip-after");
            GD.Print("GENERIC_RELIC_INPUT_OK viewport=960x540 input=isolated-real shop=discounted-claim skip=explicit-alternative icons=not-authored");
        }
        catch (Exception error) { exit = 1; GD.PrintErr("GENERIC_RELIC_INPUT_FAILED: " + error); }
        finally { game?.QueueFree(); await Frames(3); GetTree().Quit(exit); }
    }
    private async Task Click(Control control)
    {
        Check(control.IsVisibleInTree(), "visible input target"); var point = control.GetGlobalRect().GetCenter();
        Check(new Rect2(Vector2.Zero, new(960, 540)).HasPoint(point), "input target in isolated viewport");
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(6);
    }
    private async Task Frames(int count)
    { for (var i = 0; i < count; i++) { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw); } }
    private async Task Capture(string name)
    {
        await Frames(1); var dir = ProjectSettings.GlobalizePath("res://.godot/generic-relic-review");
        System.IO.Directory.CreateDirectory(dir);
        Check(_stage.Viewport.GetTexture().GetImage().SavePng(System.IO.Path.Combine(dir, name + ".png")) == Error.Ok, "capture");
    }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    { foreach (var child in root.GetChildren()) { if (child is T match) yield return match; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
