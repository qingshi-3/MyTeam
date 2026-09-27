using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class UiClarityReview : Node
{
    private const string OutputDirectory = "res://.godot/ui-review/clarity";
    private UiInputStage _stage = null!;
    private readonly List<string> _captures = [];

    public override async void _Ready()
    {
        var exit = 0;
        GrowthGameRoot? game = null;
        var testNamespace = $"tests/ui-clarity/{Guid.NewGuid():N}";
        var save = new SaveService(testNamespace);
        var production = new SaveService("growth-journey");
        var productionBefore = JsonSerializer.Serialize(production.LoadActiveRun());
        try
        {
            Require(DisplayServer.GetName() != "headless", "UI clarity review requires a rendering driver");
            _stage = new UiInputStage(this, true);
            game = GD.Load<PackedScene>("res://scenes/app/GrowthGameRoot.tscn").Instantiate<GrowthGameRoot>();
            game.SaveNamespace = testNamespace;
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "GrowthGameRoot content publication");
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);

            await CaptureVisible(screens, AppScreenId.MainMenu, "main-menu");
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/SettingsButton"));
            await Until(() => screens.Settings.IsVisibleInTree(), "settings real navigation");
            await CaptureVisible(screens, AppScreenId.Settings, "settings");
            await Click(screens.Settings.GetNode<Button>("Center/Panel/Layout/SaveButton"));
            await Until(() => screens.MainMenu.IsVisibleInTree(), "settings return");

            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
            Require(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/BattleLabButton").IsVisibleInTree(),
                "tools expansion exposes battle lab entry");
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
            Require(!screens.MainMenu.GetNode<Button>("Center/Panel/Menu/BattleLabButton").IsVisibleInTree(),
                "tools collapse hides battle lab entry");
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
            Require(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/BattleLabButton").IsVisibleInTree(),
                "tools can be reopened with real input");
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/BattleLabButton"));
            await Until(() => screens.BattleLab.IsVisibleInTree() || screens.Result.IsVisibleInTree(),
                "battle lab real navigation completes");
            if (screens.BattleLab.IsVisibleInTree())
            {
                await CaptureVisible(screens, AppScreenId.BattleLab, "battle-lab");
                await Click(screens.BattleLab.GetNode<Button>("%BackButton"));
                await Until(() => screens.MainMenu.IsVisibleInTree(), "battle lab return");
            }
            else
            {
                GD.PrintErr("UI_CLARITY_REVIEW_PRODUCT_DEFECT " +
                    screens.Result.GetNode<Label>("Center/Panel/Layout/Title").Text + " · " +
                    screens.Result.GetNode<Label>("Center/Panel/Layout/Summary").Text);
                screens.Show(AppScreenId.MainMenu, null, game.Content, null);
                await Frames(3);
            }

            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "hero selection real navigation");
            await CaptureVisible(screens, AppScreenId.HeroSelection, "hero-selection");
            var candidates = Descendants<HeroLibraryTile>(screens.HeroSelection).Where(tile => tile.IsVisibleInTree()).ToArray();
            Require(candidates.Length >= 2, "opening has at least two real candidates");
            await Click(candidates[0]);
            var heroDetail = screens.HeroSelection.GetNode<HeroDetailPanel>("%HeroDetailPanel");
            var heroRule = heroDetail.GetNode<FoldableContainer>("%RulePanel");
            var heroRuleScroll = heroDetail.GetNode<ScrollContainer>("%RuleScroll");
            var heroRuleCopy = heroDetail.GetNode<RichTextLabel>("%RuleCopy");
            var unitDetails = heroDetail.GetNode<UnitDetailView>("%UnitDetails");
            var detailsScroll = unitDetails.GetNode<ScrollContainer>("%DetailsScroll");
            var firstSkill = unitDetails.GetNode<VBoxContainer>("%SkillList").GetChildren().OfType<Control>().First();
            Require(heroRule.Folded && !string.IsNullOrWhiteSpace(heroRule.Title),
                "hero rule starts folded with its authored rule title");
            Require(detailsScroll.GetGlobalRect().Grow(1).Encloses(firstSkill.GetGlobalRect()),
                "folded hero rule leaves the first complete skill summary visible");
            heroRule.GrabFocus();
            await Key(Godot.Key.Enter);
            Require(!heroRule.Folded && heroRuleScroll.IsVisibleInTree()
                    && heroRuleScroll.Size.Y <= 201,
                "keyboard expands the hero rule inside its bounded reading region");
            for (var count = 0; count < 60 && heroRuleScroll.ScrollVertical <
                 heroRuleScroll.GetVScrollBar().MaxValue - heroRuleScroll.GetVScrollBar().Page - 1; count++)
                await Wheel(heroRuleScroll, true);
            Require(heroRuleScroll.ScrollVertical >= heroRuleScroll.GetVScrollBar().MaxValue
                    - heroRuleScroll.GetVScrollBar().Page - 1
                    && heroRuleScroll.GetGlobalRect().Grow(2).Encloses(heroRuleCopy.GetGlobalRect().Intersection(heroRuleScroll.GetGlobalRect())),
                "real wheel input reaches the end of the complete hero rule copy");
            await Click(candidates[0]);
            Require(!heroRule.Folded, "selection-state update for the same hero preserves the open rule");
            await Click(candidates[0]);
            Require(!heroRule.Folded, "restoring the same hero selection also preserves the open rule");
            await Capture("hero-rule-expanded");
            heroRule.GrabFocus();
            await Key(Godot.Key.Enter);
            firstSkill = unitDetails.GetNode<VBoxContainer>("%SkillList").GetChildren().OfType<Control>().First();
            Require(heroRule.Folded && detailsScroll.GetGlobalRect().Grow(1).Encloses(firstSkill.GetGlobalRect()),
                "closing the hero rule restores the complete first skill summary");
            await Capture("skill-detail");
            await Click(candidates[1]);
            Require(heroRule.Folded, "switching heroes resets the rule disclosure");
            await Click(screens.HeroSelection.GetNode<Button>("%ConfirmOpening"));
            await Until(() => screens.Deployment.IsVisibleInTree(), "opening confirmation reaches deployment");
            await CaptureVisible(screens, AppScreenId.Deployment, "deployment");

            var app = (RunApplication?)typeof(GameRoot).GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(game)
                ?? throw new InvalidOperationException("review run application unavailable");
            var run = app.ActiveRun ?? throw new InvalidOperationException("review run unavailable");
            var deploymentBeforeBenchRoundTrip = JsonSerializer.Serialize(run.Deployment);
            var reserveBench = screens.Deployment.GetNode<Control>("%ReserveBench");
            var deployedCell = Descendants<DeploymentCell>(screens.Deployment)
                .First(cell => cell.IsVisibleInTree() && !string.IsNullOrEmpty(cell.PieceId));
            var deployedId = deployedCell.PieceId;
            var deployedPosition = deployedCell.Cell;
            await Drag(deployedCell, reserveBench.GetGlobalRect().GetCenter());
            Require(!run.Deployment.Contains(deployedId)
                    && Descendants<DeploymentUnitCard>(screens.Deployment).Any(card => card.InstanceId == deployedId),
                "real board-to-bench drag withdraws one deployed hero into the empty reserve");
            var reserveCard = Descendants<DeploymentUnitCard>(screens.Deployment).Single(card => card.InstanceId == deployedId);
            var originalCell = Descendants<DeploymentCell>(screens.Deployment).Single(cell => cell.Cell == deployedPosition);
            await Drag(reserveCard.Portrait, originalCell.GetGlobalRect().GetCenter());
            Require(run.Deployment.Contains(deployedId)
                    && originalCell.PieceId == deployedId
                    && JsonSerializer.Serialize(run.Deployment) == deploymentBeforeBenchRoundTrip,
                "real bench-to-board drag restores the original deployment");
            GD.Print("UI_CLARITY_REAL_INPUT empty-bench-round-trip=" + deployedId);

            var army = game.GetNode<ArmyOverviewController>("ArmyOverview");
            await Click(army.GetNode<Button>("%SummaryButton"));
            await Until(() => army.IsOpen, "army drawer real open");
            await Capture("equipment");
            var pageToggle = army.GetNode<Button>("%PageToggle");
            await Click(pageToggle);
            await Capture("army");
            await Click(pageToggle);
            var growthPanel = army.GrowthPanel;
            var growthRules = growthPanel.GetNode<FoldableContainer>("GrowthRules");
            var phaseHint = growthPanel.GetNode<Label>("%PhaseHint");
            var growthLedger = growthPanel.GetNode<Label>("%GrowthLedger");
            Require(!phaseHint.Visible, "normal growth preparation does not show a phase warning");
            Require(!growthLedger.Visible && string.IsNullOrEmpty(growthLedger.Text),
                "zero growth gains do not show an empty ledger");
            growthRules.GrabFocus();
            await Key(Godot.Key.Enter);
            Require(!growthRules.Folded, "keyboard expands growth rules");
            var ruleCopy = growthRules.GetNode<Label>("Rules");
            Require(ruleCopy.IsVisibleInTree() && ruleCopy.Text.Contains("生产按英雄初始属性计算", StringComparison.Ordinal)
                && ruleCopy.Text.Contains("法术在战斗正式开始时消耗", StringComparison.Ordinal),
                "expanded growth rules expose the complete authored rule text");
            await Capture("growth");
            await Key(Godot.Key.Escape);
            await Until(() => !army.IsOpen, "army drawer real close");

            _stage.Resize(new Vector2I(1280, 720));
            await Frames(3);
            await Capture("deployment-1280");
            _stage.Resize(new Vector2I(1600, 900));
            await Frames(3);

            await Click(screens.Deployment.GetNode<Button>("%StartBattleButton"));
            await Until(() => screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle, "real battle start");
            screens.Battle.SetPaused(true);
            await CaptureVisible(screens, AppScreenId.Battle, "battle");
            var visibleUnit = Descendants<UnitContentRoot>(screens.Battle)
                .FirstOrDefault(unit => unit.IsVisibleInTree());
            Require(visibleUnit is not null, "battle has a rendered unit to inspect");
            await ClickAt(visibleUnit!.GlobalPosition);
            var inspector = Descendants<BattleInspectorDock>(screens.Battle).Single();
            Require(inspector.GetNode<Control>("%InspectorPopup").IsVisibleInTree(),
                "real unit click opens populated battle detail");
            await Capture("battle-detail");
            screens.Battle.StopBattle();

            var presentation = app.Project.Presentation;
            screens.Tower.Bind(app, presentation.ChoiceCard, presentation.SemanticIcons);
            screens.Event.Bind(app.Rules);
            screens.Rest.Bind(app.Rules, app.ActiveRun?.CurrentRunHealth ?? 0, app.ActiveRun?.MaximumRunHealth ?? 0);
            screens.Result.Bind("征程结束", "本次征程已结束。");
            screens.Settings.Bind(app.Settings);

            var shopApp = OfferFixture(app, save, TowerNodeType.Shop);
            shopApp.ActiveRun!.Gold = 999;
            screens.Shop.Bind(shopApp, presentation.ChoiceCard, presentation.ItemChoiceCard, presentation.SemanticIcons);
            screens.Show(AppScreenId.Shop, shopApp.ActiveRun, shopApp.Content, shopApp.Rules);
            await Frames(3);
            Require(Descendants<RunOfferChoiceCard>(screens.Shop).Any(card => card.IsVisibleInTree()),
                "shop review contains real offer cards");
            await CaptureVisible(screens, AppScreenId.Shop, "shop");

            var reportApp = OfferFixture(app, save, TowerNodeType.Combat);
            var encounter = reportApp.CurrentEncounter();
            var report = SyntheticResult(reportApp.BuildBattleConfig(encounter), BattleOutcome.PlayerVictory);
            screens.BattleReport.Bind(report, encounter.Title, reportApp.Content, "继续");
            screens.Show(AppScreenId.BattleReport, reportApp.ActiveRun, reportApp.Content, reportApp.Rules);
            await Frames(3);
            Require(report.Units.Length > 0 && screens.BattleReport.GetNode<Control>("%PlayerRosterStrip").IsVisibleInTree(),
                "battle report review contains populated combat rows");
            await CaptureVisible(screens, AppScreenId.BattleReport, "battle-report");

            var resolution = reportApp.ResolveBattle(report, encounter);
            Require(resolution.Accepted, "reward review follows an accepted battle victory");
            Require(reportApp.PendingOffer?.Kind == TowerAutobattler.Project.RunOfferKind.CombatReward,
                "accepted battle victory creates the formal combat reward offer");
            screens.Reward.BindOffer(reportApp, presentation.ChoiceCard, presentation.ItemChoiceCard,
                presentation.SemanticIcons);
            screens.Show(AppScreenId.Reward, reportApp.ActiveRun, reportApp.Content, reportApp.Rules);
            await Frames(3);
            Require(Descendants<RunOfferChoiceCard>(screens.Reward).Any(card => card.IsVisibleInTree()),
                "reward review contains real item cards");
            await CaptureVisible(screens, AppScreenId.Reward, "reward");

            var recruitmentApp = OfferFixture(app, save, TowerNodeType.Recruitment);
            Require(recruitmentApp.PendingOffer?.Kind == TowerAutobattler.Project.RunOfferKind.Recruitment,
                "recruitment fixture creates the formal recruitment offer");
            screens.Recruitment.BindOffer(recruitmentApp, presentation.ChoiceCard, presentation.ItemChoiceCard,
                presentation.SemanticIcons);
            screens.Show(AppScreenId.Recruitment, recruitmentApp.ActiveRun, recruitmentApp.Content, recruitmentApp.Rules);
            await Frames(3);
            Require(Descendants<RunOfferChoiceCard>(screens.Recruitment).Any(card => card.IsVisibleInTree()),
                "recruitment review contains real hero cards");
            await CaptureVisible(screens, AppScreenId.Recruitment, "recruitment");

            foreach (var offerPage in new[]
                     {
                         (Type: TowerNodeType.Event, Name: "event", ExpectedChoice: "safe"),
                         (Type: TowerNodeType.Rest, Name: "rest", ExpectedChoice: "recover_run_health")
                     })
            {
                var offerApp = OfferFixture(app, save, offerPage.Type);
                screens.Reward.BindOffer(offerApp, presentation.ChoiceCard, presentation.ItemChoiceCard,
                    presentation.SemanticIcons);
                screens.Show(AppScreenId.Reward, offerApp.ActiveRun, offerApp.Content, offerApp.Rules);
                await Frames(3);
                var offer = offerApp.PendingOffer ?? throw new InvalidOperationException(
                    offerPage.Type + " review fixture did not create a pending offer");
                var cards = Descendants<RunOfferChoiceCard>(screens.Reward).Where(card => card.IsVisibleInTree()).ToArray();
                Require(offer.Choices.Length == 2 && cards.Length == 2
                        && cards.Any(card => card.StableId == offerPage.ExpectedChoice),
                    offerPage.Type + " review uses a valid generated offer with actionable choices");
                await CaptureVisible(screens, AppScreenId.Reward, offerPage.Name);
                GD.Print($"UI_CLARITY_FIXTURE page={offerPage.Name} offer={offer.OfferId} choices={string.Join(',', cards.Select(card => card.StableId))}");
            }

            // These controllers use the same live application/content as the real-input pages.
            foreach (var page in new[]
                     {
                         (AppScreenId.Tower,"tower"),
                         (AppScreenId.Result,"result")
                     })
            {
                screens.Show(page.Item1, app.ActiveRun, game.Content, app.Rules);
                await Frames(3);
                await CaptureVisible(screens, page.Item1, page.Item2);
            }

            foreach (var legacyPage in new[]
                     {
                         (Id: AppScreenId.Event, Name: "legacy-event"),
                         (Id: AppScreenId.Rest, Name: "legacy-rest")
                     })
            {
                screens.Show(legacyPage.Id, app.ActiveRun, game.Content, app.Rules);
                await Frames(3);
                await CaptureVisible(screens, legacyPage.Id, legacyPage.Name);
                GD.Print("UI_CLARITY_LEGACY_PAGE " + legacyPage.Name);
            }

            Require(_captures.Distinct(StringComparer.Ordinal).Count() == 23,
                "clarity review produced every named page/detail capture");
            Require(JsonSerializer.Serialize(production.LoadActiveRun()) == productionBefore,
                "clarity review did not alter the production growth save");
            GD.Print("UI_CLARITY_REVIEW_OK " + string.Join(',', _captures));
        }
        catch (Exception error) { exit = 1; GD.PrintErr("UI_CLARITY_REVIEW_FAILED " + error); }
        finally
        {
            game?.QueueFree();
            await Frames(3);
            save.DeleteActiveRun();
        }
        GetTree().Quit(exit);
    }

    private async Task CaptureVisible(AppScreenHost screens, AppScreenId id, string name)
    {
        var target = screens.Screen(id);
        Require(target.IsVisibleInTree() && target.GetGlobalRect().Size.X > 100 && target.GetGlobalRect().Size.Y > 100,
            "capture target is visible and laid out: " + id);
        await Capture(name);
    }

    private async Task Capture(string name)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = _stage.Viewport.GetTexture().GetImage();
        Require(image is not null && !image.IsEmpty(), "rendered image exists: " + name);
        var directory = ProjectSettings.GlobalizePath(OutputDirectory);
        DirAccess.MakeDirRecursiveAbsolute(directory);
        Require(image!.SavePng(Path.Combine(directory, name + ".png")) == Error.Ok, "save capture: " + name);
        _captures.Add(name);
    }

    private async Task Click(Control target)
    {
        Require(target.IsVisibleInTree(), "real click target visible: " + target.GetPath());
        await ClickAt(target.GetGlobalRect().GetCenter());
    }

    private async Task ClickAt(Vector2 point)
    {
        Require(_stage.Viewport.GetVisibleRect().HasPoint(point), "real click inside review viewport");
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(4);
    }

    private async Task Drag(Control source, Vector2 destination)
    {
        Require(source.IsVisibleInTree(), "real drag source visible: " + source.GetPath());
        var start = source.GetGlobalRect().GetCenter();
        Require(_stage.Viewport.GetVisibleRect().HasPoint(start) && _stage.Viewport.GetVisibleRect().HasPoint(destination),
            "real drag endpoints are inside review viewport");
        _stage.WarpPointer(start);
        await Frames(1);
        _stage.Push(new InputEventMouseMotion { Position = start, GlobalPosition = start });
        _stage.Push(new InputEventMouseButton { Position = start, GlobalPosition = start, ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left, Pressed = true });
        for (var i = 1; i <= 20; i++)
        {
            var point = start.Lerp(destination, i / 20f);
            _stage.WarpPointer(point);
            _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point,
                Relative = (destination - start) / 20, ButtonMask = MouseButtonMask.Left });
            await Frames(2);
        }
        Require(_stage.Viewport.GuiIsDragging(), "pointer movement starts a native deployment drag");
        _stage.Push(new InputEventMouseMotion { Position = destination, GlobalPosition = destination,
            ButtonMask = MouseButtonMask.Left });
        _stage.Push(new InputEventMouseButton { Position = destination, GlobalPosition = destination,
            ButtonIndex = MouseButton.Left, Pressed = false });
        var until = Time.GetTicksMsec() + 280;
        while (Time.GetTicksMsec() < until) await Frames(1);
        Require(!_stage.Viewport.GuiIsDragging(), "deployment drag completes after pointer release");
    }

    private async Task Key(Key key)
    {
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await Frames(4);
    }

    private async Task Wheel(Control target, bool down)
    {
        var point = target.GetGlobalRect().GetCenter();
        _stage.WarpPointer(point);
        await Frames(1);
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        var button = down ? MouseButton.WheelDown : MouseButton.WheelUp;
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = button, Pressed = true, Factor = 3 });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = button, Pressed = false });
        await Frames(2);
    }

    private static RunApplication OfferFixture(RunApplication source, SaveService save, TowerNodeType type)
    {
        var app = new RunApplication(source.Content, save, source.Project);
        for (ulong seed = 1; seed <= 64; seed++)
        {
            Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], seed), "review fixture starts a normal run");
            if (app.CurrentOptions().Any(option => option.Type == type)) break;
        }
        Require(app.CurrentOptions().Any(option => option.Type == type) && app.SelectNode(type),
            "review fixture selects a legitimately offered " + type + " node");
        return app;
    }

    private static BattleResult SyntheticResult(BattleConfig config, BattleOutcome outcome)
    {
        var units = config.Spawns.Select(spawn => new BattleUnitReportSnapshot(
            spawn.InstanceId, spawn.InstanceId, spawn.Unit.ContentId, spawn.Unit.DisplayName, spawn.Unit.Role,
            spawn.Team, spawn.Unit.IsHero, spawn.IsTemporary, true, spawn.Cell, spawn.Unit.MaxHealth,
            spawn.Unit.MaxHealth, 0, spawn.Unit.Damage, spawn.Team == 0 ? 100 : 30,
            spawn.Team == 0 ? 30 : 100, 0, spawn.Team == 0 ? 20 : 0, spawn.Team == 0 ? 1 : 0,
            0, null, spawn.Team == 0 ? 3 : 1, spawn.Team == 0 ? 1 : 0)).ToImmutableArray();
        RelicBattleTransitionResult? relicTransition = null;
        if (config.Relics is not null)
        {
            using var relicScope = new RelicBattleScope(config.Relics);
            relicTransition = relicScope.Complete(outcome switch
            {
                BattleOutcome.PlayerVictory => RelicBattleCompletionReason.PlayerVictory,
                BattleOutcome.PlayerDefeat => RelicBattleCompletionReason.PlayerDefeat,
                _ => RelicBattleCompletionReason.Timeout
            });
        }
        return new BattleResult(outcome, 25, new string('a', 64), units, 3, 2, relicTransition, config.Identity);
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
