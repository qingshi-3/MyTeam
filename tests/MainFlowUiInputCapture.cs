using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// The main pass follows production navigation and a real battle. Independent offer
// fixtures are labelled explicitly and never stand in for a successful normal run.
public partial class MainFlowUiInputCapture : Node
{
    private const string Output = "res://.godot/ui-review";
    private string _step = "boot";
    private readonly HashSet<string> _seen = [];
    private int _captures;

    public override async void _Ready()
    {
        var code = 0;
        GameRoot? game = null;
        try
        {
            Require(DisplayServer.GetName() != "headless", "This input capture needs a rendered window.");
            GetWindow().Size = new Vector2I(1600, 900);
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(Output));
            game = GD.Load<PackedScene>(ProjectSettings.GetSetting("application/run/main_scene").AsString()).Instantiate<GameRoot>();
            game.SaveNamespace = $"tests/main-flow-ui/{Guid.NewGuid():N}";
            AddChild(game);
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            await Until(() => game.Content is not null && screens.MainMenu.IsVisibleInTree(), "production ready", 240);
            var content = game.Content!;
            var compiled = GameProjectCompiler.Compile(game.ProjectDefinition ?? GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"), content.Graph);
            var project = compiled.Project ?? throw new InvalidOperationException(string.Join(';', compiled.Report.CoreErrors));
            var army = game.GetNode<ArmyOverviewController>("ArmyOverview");
            var offersOnly = OS.GetCmdlineUserArgs().Contains("--offers-only");

            if (!offersOnly)
            {
            Step("normal new run: hero, route and army drawer");
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "hero selection");
            var initial = Descendants<HeroLibraryTile>(screens.HeroSelection).Single(tile => tile.StableId == screens.HeroSelection.PreviewStableId);
            await Click(initial);
            await Click(screens.HeroSelection.GetNode<HeroDetailPanel>("%HeroDetailPanel").GetNode<Button>("%DeployButton"));
            await Until(() => screens.Tower.IsVisibleInTree(), "new run reached route");
            await Capture("main-route");
            await Click(army.GetNode<Button>("%SummaryButton"));
            Require(army.IsOpen, "army opens by summary button");
            Bounds(army.GetNode<Control>("%Drawer"), "army drawer");
            Bounds(army.GetNode<Control>("%CloseButton"), "army close");
            await Capture("main-army-top");
            var armyScroll = army.GetNode<Control>("%HeroDetails").GetNode<ScrollContainer>("%DetailsScroll");
            await ScrollEnd(armyScroll);
            await Capture("main-army-bottom");
            var armyPages = army.GetNode<TabContainer>("%Pages");
            await Tab(armyPages, 1);
            await Capture("main-army-equipment");
            await Tab(armyPages, 2);
            await Capture("main-army-overview");
            await Tab(armyPages, 0);
            await Click(army.GetNode<Button>("%CloseButton"));
            Require(!army.IsOpen, "army close restores main route");

            for (var route = 0; route < 4 && !screens.Deployment.IsVisibleInTree(); route++)
            {
                Step("normal route choice " + (route + 1));
                var options = Descendants<ChoiceCard>(screens.Tower).Where(card => card.IsVisibleInTree() && !card.Disabled).ToArray();
                var target = !_seen.Contains("recruitment") ? options.FirstOrDefault(card => card.StableId == "Recruitment") : null;
                target ??= options.FirstOrDefault(card => card.StableId == "Combat") ?? options.FirstOrDefault(card => card.StableId == "Elite") ?? options.FirstOrDefault(card => card.StableId == "Boss") ?? options.FirstOrDefault();
                Require(target is not null, "normal route has an actionable option");
                GD.Print("MAIN_FLOW_ROUTE " + target!.StableId);
                await Click(target);
                await Frames(4);
                if (screens.Deployment.IsVisibleInTree()) break;
                if (screens.Recruitment.IsVisibleInTree())
                {
                    await InspectOffer(screens.Recruitment, "main-recruitment");
                    _seen.Add("recruitment");
                    await ResolveNormalOffer(screens.Recruitment);
                }
                else if (screens.Shop.IsVisibleInTree())
                {
                    await InspectShop(screens.Shop, "main-shop");
                    _seen.Add("shop");
                    await Click(screens.Shop.GetNode<Button>("Margin/Layout/LeaveButton"));
                }
                else if (screens.Reward.IsVisibleInTree())
                {
                    await InspectOffer(screens.Reward, "main-node-offer");
                    await ResolveNormalOffer(screens.Reward);
                }
                else throw new InvalidOperationException("Route did not open a supported production screen.");
                await Until(() => screens.Tower.IsVisibleInTree(), "resolved normal route returns to tower");
            }

            Step("normal deployment: select roster unit and inspect");
            Require(screens.Deployment.IsVisibleInTree(), "short natural route reaches a battle");
            var deployment = screens.Deployment;
            var tabs = deployment.GetNode<TabContainer>("%SidebarTabs");
            await Capture("main-deployment-default");
            await ScrollEnd(deployment.GetNode<Control>("%PreparedUnitDetailPanel").GetNode<UnitDetailView>("%UnitDetails").GetNode<ScrollContainer>("%DetailsScroll"));
            await Capture("main-deployment-details-bottom");
            var enemy = Descendants<EnemyDeploymentPreview>(deployment).FirstOrDefault(unit => unit.IsVisibleInTree());
            if (enemy is not null)
            {
                await Click(enemy);
                await Capture("main-deployment-enemy");
            }
            await Tab(tabs, 1);
            var deployedIds = Descendants<DeploymentCell>(deployment).Select(cell => cell.PieceId).ToHashSet();
            var rosterCard = Descendants<DeploymentUnitCard>(deployment).FirstOrDefault(card => card.IsVisibleInTree() && !deployedIds.Contains(card.InstanceId))
                ?? Descendants<DeploymentUnitCard>(deployment).First(card => card.IsVisibleInTree());
            await Click(rosterCard);
            Require(deployment.SelectedPieceId == rosterCard.InstanceId, "roster selection is driven by the real card click");
            Bounds(deployment.GetNode<Control>("%StartBattleButton"), "deployment start");
            Bounds(deployment.GetNode<Control>("%BackButton"), "deployment back");
            Bounds(deployment.GetNode<Control>("%DeploymentBoard"), "deployment board");
            await Capture("main-deployment-selected");
            if (!deployedIds.Contains(rosterCard.InstanceId))
            {
                var identity = rosterCard.InstanceId;
                var destination = Descendants<DeploymentCell>(deployment).First(cell => string.IsNullOrEmpty(cell.PieceId) && cell.IsLegalTarget);
                await Click(destination);
                Require(destination.PieceId == identity, "recruited reserve deploys through the real cell input");
                await Capture("main-deployment-recruit-placed");
            }
            await Tab(tabs, 2);
            await Capture("main-deployment-equipment");
            await Click(deployment.GetNode<Button>("%StartBattleButton"));
            await Until(() => screens.Battle.IsVisibleInTree(), "normal battle starts");
            Step("normal battle: real unit click and actual outcome");
            await Until(() => screens.Battle.ReadRuntimeUnits().Length > 0, "battle has runtime units");
            await Click(screens.Battle.GetNode<Button>("%PauseButton"));
            var runtime = screens.Battle.ReadRuntimeUnits().First(unit => unit.Team == 0);
            var presenter = Descendants<UnitContentRoot>(screens.Battle).Single(unit => unit.RuntimeId == runtime.RuntimeId);
            await ClickPoint(presenter.GlobalPosition);
            await Capture("main-battle-unit");
            await Click(screens.Battle.GetNode<Button>("%PauseButton"));
            // Use the shipped speed control, not direct simulation stepping or a fabricated result.
            for (var press = 0; press < 2 && screens.Battle.IsVisibleInTree(); press++)
                await Click(screens.Battle.GetNode<Button>("%SpeedButton"));
            await Until(() => screens.BattleReport.IsVisibleInTree(), "actual battle report", 4200);
            Bounds(screens.BattleReport.GetNode<Control>("%ReportContinue"), "report continue");
            await Capture("main-battle-report");
            await Click(screens.BattleReport.GetNode<Button>("%OffenseTab"));
            await Capture("main-battle-report-offense");
            await Click(screens.BattleReport.GetNode<Button>("%ReportContinue"));
            await Frames(4);
            if (screens.Reward.IsVisibleInTree() && screens.Reward.GetNode<Control>("Center/Panel/Layout/OfferBody").Visible)
            {
                await InspectOffer(screens.Reward, "main-combat-reward");
                _seen.Add("reward");
                await ResolveNormalOffer(screens.Reward);
                await Until(() => screens.Tower.IsVisibleInTree(), "claim combat reward and continue to route");
                await Capture("main-route-after-reward");
            }
            GD.Print("MAIN_FLOW_NATURAL_DONE outcome-not-forced screens=" + string.Join(',', _seen));
            }

            // Supplement random-route gaps using independent, explicitly named UI fixtures.
            // All source data is the same compiled production project; no test hero skills.
            // Keep the production background. Only replace the routed content with the explicit fixture.
            screens.Visible = false;
            army.Visible = false;
            if (!_seen.Contains("recruitment")) await OfferFixture(content, project, TowerNodeType.Recruitment);
            if (!_seen.Contains("shop")) await OfferFixture(content, project, TowerNodeType.Shop);
            if (!_seen.Contains("reward")) await RewardPreviewFixture(content, project);
            GD.Print($"MAIN_FLOW_UI_INPUT_CAPTURE_OK captures={_captures} size=1600x900 normal-battle={(offersOnly ? "skipped-focused-offer-check" : "actual")} offer-fixtures=explicit save=unique-test-namespace path={Output}");
        }
        catch (Exception exception)
        {
            code = 1;
            GD.PrintErr($"MAIN_FLOW_UI_INPUT_CAPTURE_FAILED step={_step}: {exception}");
            if (DisplayServer.GetName() != "headless") await Capture("main-failure");
        }
        finally { game?.QueueFree(); }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(code);
    }

    private async Task ResolveNormalOffer(RewardScreenController screen)
    {
        var choices = screen.GetNode<Container>("Center/Panel/Layout/OfferBody/ChoiceScroll/Choices").GetChildren().OfType<RunOfferChoiceCard>().Where(card => !card.ConfirmButton.Disabled && !card.IsQueuedForDeletion()).ToArray();
        if (choices.Length > 0)
        {
            await Click(choices[0].ConfirmButton);
            await Frames(3);
            Require(!screen.IsVisibleInTree(),
                "explicit candidate confirm must resolve the offer: " + screen.GetNode<Label>("Center/Panel/Layout/Hint").Text);
        }
        var next = screen.GetNode<Button>("Center/Panel/Layout/ContinueButton");
        if (screen.IsVisibleInTree() && next.IsVisibleInTree()) await Click(next);
    }

    private async Task InspectOffer(RewardScreenController screen, string name, RunApplication? app = null)
    {
        var root = "Center/Panel/Layout/";
        Bounds(screen.GetNode<Control>(root + "OfferBody"), name + " body");
        var next = screen.GetNode<Control>(root + "ContinueButton");
        if (next.IsVisibleInTree()) Bounds(next, name + " continue");
        await InspectCards(screen.GetNode<Container>(root + "OfferBody/ChoiceScroll/Choices"), name, app);
    }

    private async Task InspectShop(ShopScreenController screen, string name, RunApplication? app = null)
    {
        var root = "Margin/Layout/";
        Bounds(screen.GetNode<Control>(root + "OfferBody"), name + " body");
        Bounds(screen.GetNode<Control>(root + "LeaveButton"), name + " leave");
        await InspectCards(screen.GetNode<Container>(root + "OfferBody/ChoiceScroll/Choices"), name, app);
    }

    private async Task InspectCards(Container cards, string name, RunApplication? app)
    {
        var choices = cards.GetChildren().OfType<RunOfferChoiceCard>().Where(card => !card.IsQueuedForDeletion()).ToArray();
        Require(choices.Length > 0, "offer has visible production choices");
        foreach (var card in choices.Take(3))
        {
            Bounds(card, name + " candidate " + card.TitleText);
            Bounds(card.ConfirmButton, name + " explicit confirm");
        }
        if (choices.Length >= 3)
            Require(choices[0].GetGlobalRect().End.X <= choices[1].GlobalPosition.X + 2 &&
                choices[1].GetGlobalRect().End.X <= choices[2].GlobalPosition.X + 2 &&
                Math.Abs(choices[0].GlobalPosition.Y - choices[2].GlobalPosition.Y) <= 2,
                "first three offer candidates are side by side without overlap");
        var unchanged = app is null ? null : JsonSerializer.Serialize(app.ActiveRun);
        await Capture(name);
        var first = choices.FirstOrDefault(card => card.DetailsButton.IsVisibleInTree()) ?? choices[0];
        var stats = first.GetNode<Control>("Layout/Stats");
        var inspectionSurface = stats.IsVisibleInTree() ? stats : first.GetNode<Control>("Layout/Artwork");
        await MovePointer(inspectionSurface.GetGlobalRect().GetCenter());
        await Frames(3);
        await Click(inspectionSurface);
        if (app is not null) Require(JsonSerializer.Serialize(app.ActiveRun) == unchanged, "clicking attributes/artwork does not claim the candidate");
        if (!first.DetailsButton.IsVisibleInTree())
        {
            GD.Print("MAIN_FLOW_DETAILS_NOT_APPLICABLE " + name + " (candidate has no additional rules)");
            return;
        }
        await RevealByWheel(first.BodyScroll, first.DetailsButton);
        await Click(first.DetailsButton);
        Require(first.DetailsExpanded, "details button opens local explanation");
        if (app is not null) Require(JsonSerializer.Serialize(app.ActiveRun) == unchanged, "opening skill/attribute details does not alter the run");
        await KeyPress(Key.Space);
        Require(!first.DetailsExpanded, "keyboard accept on focused detail button closes the same explanation");
        await KeyPress(Key.Space);
        Require(first.DetailsExpanded, "keyboard accept reopens the same explanation");
        await ScrollEnd(first.BodyScroll);
        Bounds(first.ConfirmButton, name + " confirm remains visible below scrolled details");
        if (app is not null) Require(JsonSerializer.Serialize(app.ActiveRun) == unchanged, "keyboard expansion and scrolling are read-only");
        await Capture(name + "-expanded");
    }

    private RunApplication FixtureApp(ContentRegistry content, CompiledGameProject project) =>
        new(content, new SaveService($"tests/main-flow-offer/{Guid.NewGuid():N}"), project);

    private async Task OfferFixture(ContentRegistry content, CompiledGameProject project, TowerNodeType type)
    {
        Step("independent typed RunApplication UI fixture: " + type);
        var app = FixtureApp(content, project);
        for (ulong seed = 1; seed <= 24; seed++)
        {
            Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], seed), "fixture starts using a normally unlocked hero");
            if (app.CurrentOptions().Any(option => option.Type == type)) break;
        }
        Require(app.SelectNode(type), "fixture selects a legitimately offered node");
        var host = FixtureHost();
        try
        {
            if (type == TowerNodeType.Shop)
            {
                var screen = GD.Load<PackedScene>("res://scenes/ui/ShopScreen.tscn").Instantiate<ShopScreenController>();
                host.AddChild(screen);
                screen.Bind(app, project.Presentation.ChoiceCard, project.Presentation.ItemChoiceCard, project.Presentation.SemanticIcons);
                // The independent screen fixture has no ScreenRouter; reproduce its initial focus handoff.
                Descendants<BaseButton>(screen).First(button => button.IsVisibleInTree() && !button.Disabled).GrabFocus();
                var left = false;
                screen.LeaveRequested += () => left = true;
                var purchased = false;
                screen.PurchaseRequested += id =>
                {
                    var result = app.ResolveOffer(app.PendingOffer!.OfferId, id);
                    purchased = result.Succeeded;
                    if (app.PendingOffer is not null)
                        screen.Bind(app, project.Presentation.ChoiceCard, project.Presentation.ItemChoiceCard, project.Presentation.SemanticIcons);
                    screen.ShowDecisionResult(result);
                };
                await Frames(4);
                await InspectShop(screen, "fixture-shop", app);
                var affordable = Descendants<RunOfferChoiceCard>(screen).FirstOrDefault(card => !card.ConfirmButton.Disabled && card.IsVisibleInTree());
                if (affordable is not null)
                {
                    var before = JsonSerializer.Serialize(app.ActiveRun);
                    await Click(affordable.ConfirmButton);
                    Require(purchased && JsonSerializer.Serialize(app.ActiveRun) != before, "explicit shop confirm commits an affordable isolated purchase");
                    await Capture("fixture-shop-purchased");
                }
                await Click(screen.GetNode<Button>("Margin/Layout/LeaveButton"));
                Require(left, "fixture shop real leave click");
            }
            else
            {
                var screen = GD.Load<PackedScene>("res://scenes/ui/RecruitmentScreen.tscn").Instantiate<RewardScreenController>();
                host.AddChild(screen);
                screen.BindOffer(app, project.Presentation.ChoiceCard, project.Presentation.ItemChoiceCard, project.Presentation.SemanticIcons);
                Descendants<BaseButton>(screen).First(button => button.IsVisibleInTree() && !button.Disabled).GrabFocus();
                screen.ChoiceRequested += choice =>
                {
                    var offer = app.PendingOffer!;
                    var result = app.ResolveOffer(offer.OfferId, choice);
                    if (result.Succeeded) screen.Hide();
                    else screen.ShowDecisionMessage(result.Message);
                };
                await Frames(4);
                await InspectOffer(screen, "fixture-recruitment", app);
                if (OS.GetCmdlineUserArgs().Contains("--offers-only"))
                    await NarrowOfferFixture(screen, host, app);
                var rosterBefore = app.ActiveRun!.Roster.Count;
                await ResolveNormalOffer(screen);
                Require(app.ActiveRun.Roster.Count > rosterBefore, "only explicit recruitment confirm adds a hero");
                screen.Visible = false;
                await ArmyFixture(app, host);
                await DeploymentFixture(app, host);
            }
        }
        finally { host.QueueFree(); app.AbandonRun(); await Frames(2); }
    }

    private async Task RewardPreviewFixture(ContentRegistry content, CompiledGameProject project)
    {
        Step("independent reward preview fixture: no battle victory or reward receipt claimed");
        var app = FixtureApp(content, project);
        Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 0x513UL), "reward preview run created");
        var host = FixtureHost();
        try
        {
            var screen = GD.Load<PackedScene>("res://scenes/ui/RewardScreen.tscn").Instantiate<RewardScreenController>();
            host.AddChild(screen);
            screen.BindCombatReward(app, project.Presentation.ChoiceCard, project.Presentation.ItemChoiceCard, project.Presentation.SemanticIcons);
            Descendants<BaseButton>(screen).First(button => button.IsVisibleInTree() && !button.Disabled).GrabFocus();
            await Frames(4);
            await InspectOffer(screen, "fixture-reward-preview-only", app);
            var continued = false;
            screen.ContinueRequested += () => continued = true;
            await Click(screen.GetNode<Button>("Center/Panel/Layout/ContinueButton"));
            Require(continued, "preview continue button responds to input");
        }
        finally { host.QueueFree(); app.AbandonRun(); await Frames(2); }
    }

    private async Task NarrowOfferFixture(RewardScreenController screen, Control host, RunApplication app)
    {
        Step("single narrow candidate layout: 900x900");
        var window = GetWindow();
        var originalWindow = window.Size;
        var originalScale = window.ContentScaleSize;
        var originalHost = host.Size;
        var before = JsonSerializer.Serialize(app.ActiveRun);
        var outer = screen.GetNode<ScrollContainer>("Center/Panel/Layout/OfferBody/ChoiceScroll");
        var grid = outer.GetNode<GridContainer>("Choices");
        try
        {
            // Resize the real logical viewport as well as the native window; the
            // project's 1600-wide canvas stretch alone would only shrink the image.
            window.ContentScaleSize = new Vector2I(900, 900);
            window.Size = new Vector2I(900, 900);
            host.Size = new Vector2(900, 900);
            await Frames(6);
            Require(grid.Columns is 1 or 2, "narrow production grid adapts to one or two columns");
            var cards = grid.GetChildren().OfType<RunOfferChoiceCard>().Where(card => !card.IsQueuedForDeletion()).ToArray();
            Require(cards.Length >= 3, "narrow recruitment fixture has at least three real candidates");
            var available = outer.GetGlobalRect().Grow(3);
            foreach (var card in cards.Take(grid.Columns))
            {
                Require(available.Encloses(card.GetGlobalRect()), "first-row narrow candidate fits its scrolling viewport");
                Require(available.Encloses(card.ConfirmButton.GetGlobalRect()), "first-row fixed confirm remains in the scrolling viewport");
            }
            GD.Print($"MAIN_FLOW_NARROW size={GetViewport().GetVisibleRect().Size} columns={grid.Columns} candidates={cards.Length}");
            await Capture("fixture-recruitment-narrow-grid");
            // Target the outer scrollbar itself so wheel input cannot be consumed
            // by a card's independent skill-text scroll area.
            var bar = outer.GetVScrollBar();
            Require(bar.IsVisibleInTree(), "additional narrow rows have a usable outer scrollbar");
            for (var turn = 0; turn < 60 && bar.Value < bar.MaxValue - bar.Page - 1; turn++)
            {
                var point = bar.GetGlobalRect().GetCenter();
                await MovePointer(point);
                Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.WheelDown, Pressed = true, Factor = 3 });
                Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.WheelDown, Pressed = false });
                await Frames(2);
            }
            var last = cards[^1];
            Require(outer.GetGlobalRect().Grow(3).Encloses(last.GetGlobalRect()), "last narrow candidate can be reached by outer wheel input");
            Require(last.DetailsButton.IsVisibleInTree(), "last recruitment candidate has details to inspect");
            await RevealByWheel(last.BodyScroll, last.DetailsButton);
            var expanded = last.DetailsExpanded;
            await Click(last.DetailsButton);
            Require(last.DetailsExpanded != expanded, "last narrow candidate detail button receives the real click");
            Require(outer.GetGlobalRect().Grow(3).Encloses(last.ConfirmButton.GetGlobalRect()), "last fixed confirm stays reachable after expanding details");
            Require(JsonSerializer.Serialize(app.ActiveRun) == before, "narrow scrolling and detail inspection do not recruit");
            await Capture("fixture-recruitment-narrow-last-details");
        }
        finally
        {
            window.ContentScaleSize = originalScale;
            window.Size = originalWindow;
            host.Size = originalHost;
            await Frames(6);
        }
        Require(grid.Columns == 3, "candidate comparison restores three columns at 1600 width");
    }

    private async Task DeploymentFixture(RunApplication app, Control host)
    {
        Step("independent recruited reserve deployment fixture");
        var option = app.CurrentOptions().FirstOrDefault(candidate => candidate.Type is TowerNodeType.Combat or TowerNodeType.Elite);
        Require(option is not null && app.SelectNode(option.Type), "fixture reaches a legal combat preparation after recruitment");
        var encounter = app.CurrentEncounter();
        var screen = GD.Load<PackedScene>("res://scenes/ui/DeploymentScreen.tscn").Instantiate<DeploymentScreenController>();
        host.AddChild(screen);
        screen.Bind(app, encounter);
        screen.MoveRequested += command =>
        {
            Require(app.ApplyFormationCommand(command, screen.FloorRule!), "fixture commits real formation command");
            screen.Bind(app, encounter);
        };
        await Frames(4);
        await InspectPrepared(screen.GetNode<Control>("%PreparedUnitDetailPanel"), "fixture-deployment-compact", app);
        Bounds(screen.GetNode<Control>("%StartBattleButton"), "compact deployment start");
        var reserveId = app.ActiveRun!.Roster.First(hero => !app.ActiveRun.Deployment.Contains(hero.InstanceId)).InstanceId;
        await Tab(screen.GetNode<TabContainer>("%SidebarTabs"), 1);
        var card = Descendants<DeploymentUnitCard>(screen).Single(candidate => candidate.InstanceId == reserveId);
        await Click(card);
        Require(screen.SelectedPieceId == reserveId, "reserve card center selects the recruited hero");
        var destination = Descendants<DeploymentCell>(screen).First(cell => cell.IsLegalTarget && string.IsNullOrEmpty(cell.PieceId));
        await Click(destination);
        Require(destination.PieceId == reserveId && app.ActiveRun.Deployment.Contains(reserveId), "reserve placement reaches saved formation");
        await Capture("fixture-recruited-hero-deployed");
    }

    private async Task ArmyFixture(RunApplication app, Control host)
    {
        Step("independent army drawer compact detail fixture");
        var army = GD.Load<PackedScene>("res://scenes/ui/components/ArmyOverview.tscn").Instantiate<ArmyOverviewController>();
        host.AddChild(army);
        army.BindEquipmentManagement(app);
        army.Bind(ArmyOverviewFactory.Build(app.ActiveRun!, app.Content, app.Rules));
        await Frames(4);
        await Click(army.GetNode<Button>("%SummaryButton"));
        Require(army.IsOpen, "fixture army opens by real summary click");
        await InspectPrepared(army.GetNode<Control>("%HeroDetails"), "fixture-army-compact", app);
        await Tab(army.GetNode<TabContainer>("%Pages"), 1);
        await Capture("fixture-army-equipment");
        await KeyPress(Key.Escape);
        Require(!army.IsOpen, "Escape closes army drawer without swallowing the input");
        army.QueueFree();
        await Frames(2);
    }

    private async Task InspectPrepared(Control host, string name, RunApplication app)
    {
        var panel = host.GetNode<UnitDetailView>("%UnitDetails");
        var before = JsonSerializer.Serialize(app.ActiveRun);
        var scroll = panel.GetNode<ScrollContainer>("%DetailsScroll");
        var damage = panel.GetNode<UnitCoreStats>("%CoreStats").GetNode<Button>("%DamageFact");
        Bounds(damage, name + " primary damage fact");
        await Capture(name);
        await MovePointer(damage.GetGlobalRect().GetCenter());
        await ToSignal(GetTree().CreateTimer(.7), SceneTreeTimer.SignalName.Timeout);
        await Frames(2);
        var hover = Descendants<RichTextLabel>(GetTree().Root).FirstOrDefault(label => label.Name == "TooltipCopy" && label.IsVisibleInTree());
        Require(hover is not null, "attribute hover produces its authored explanation tooltip");
        var hoverCopy = hover!.GetParsedText();
        var tooltipRoot = hover.GetParent().GetParent().GetParent<Control>();
        Require(tooltipRoot.Size.Y < 450, "single attribute tooltip fits its content rather than retaining an oversized initial wrap");
        await Capture(name + "-attribute-hover");
        await Click(damage);
        var explanation = panel.GetNode<Control>("%Explanation");
        Require(explanation.IsVisibleInTree(), "attribute click opens the local explanation");
        var copy = panel.GetNode<RichTextLabel>("%ExplanationText").Text;
        Require(copy.Length > 0, "local attribute explanation has readable text");
        Require(panel.GetNode<RichTextLabel>("%ExplanationText").GetParsedText() == hoverCopy, "hover and clicked attribute explanation say the same thing");
        await KeyPress(Key.Space);
        Require(panel.GetNode<RichTextLabel>("%ExplanationText").Text == copy, "keyboard accept explains the same attribute");
        var secondary = panel.GetNode<FoldableContainer>("%SecondaryAttributes");
        await RevealByWheel(scroll, secondary);
        await ClickPoint(secondary.GlobalPosition + new Vector2(Math.Min(100, secondary.Size.X / 2), 14));
        Require(!secondary.Folded, "secondary attributes expand through their visible title");
        await Capture(name + "-attributes");
        var skills = panel.GetNode<Control>("%SkillList");
        var firstSkill = skills.GetChildren().OfType<Control>().First(node => !node.IsQueuedForDeletion());
        var header = firstSkill.GetNode<Button>("%SkillHeader");
        await RevealByWheel(scroll, header);
        await Click(header);
        Require(panel.GetNode<Control>("%Explanation").IsVisibleInTree(), "skill header opens local skill explanation");
        Require(firstSkill.GetNode<RichTextLabel>("%SkillBody").Text.Length > 0, "skill body stays independently readable");
        await RevealByWheel(scroll, panel.GetNode<Control>("%Explanation"));
        Require(JsonSerializer.Serialize(app.ActiveRun) == before, "unit attribute/skill inspection is read-only");
        await Capture(name + "-skill-explanation");
    }

    private Control FixtureHost()
    {
        var host = new Control { Size = new Vector2(1600, 900), Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres") };
        AddChild(host);
        return host;
    }

    private async Task Tab(TabContainer tabs, int index)
    {
        var bar = tabs.GetTabBar();
        await ClickPoint(bar.GlobalPosition + bar.GetTabRect(index).GetCenter());
        Require(tabs.CurrentTab == index, $"real tab click selects {index} on {tabs.GetPath()}");
    }

    private void Bounds(Control control, string label) => Require(control.IsVisibleInTree() && GetViewport().GetVisibleRect().Grow(2).Encloses(control.GetGlobalRect()), $"{label} outside viewport: {control.GetGlobalRect()}");
    private async Task RevealByWheel(ScrollContainer scroll, Control target)
    {
        for (var count = 0; count < 60; count++)
        {
            var view = scroll.GetGlobalRect().Grow(-3);
            var point = target.GetGlobalRect().GetCenter();
            if (view.HasPoint(point)) return;
            var direction = point.Y > view.End.Y ? MouseButton.WheelDown : MouseButton.WheelUp;
            var position = scroll.GetGlobalRect().GetCenter();
            await MovePointer(position);
            Input.ParseInputEvent(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = direction, Pressed = true, Factor = 2 });
            Input.ParseInputEvent(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = direction, Pressed = false });
            await Frames(2);
        }
        throw new InvalidOperationException("Input cannot reveal " + target.GetPath());
    }
    private async Task ScrollEnd(ScrollContainer scroll)
    {
        for (var index = 0; index < 60; index++)
        {
            var bar = scroll.GetVScrollBar();
            if (bar.Value >= bar.MaxValue - bar.Page - 1) return;
            await MovePointer(scroll.GetGlobalRect().GetCenter());
            var point = scroll.GetGlobalRect().GetCenter();
            Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.WheelDown, Pressed = true, Factor = 3 });
            Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.WheelDown, Pressed = false });
            await Frames(2);
        }
        throw new InvalidOperationException("Scroll end not reachable: " + scroll.GetPath());
    }
    private async Task Click(Control control)
    {
        Require(control.IsVisibleInTree() && GetViewport().GetVisibleRect().HasPoint(control.GetGlobalRect().GetCenter()), "click target hidden/outside: " + control.GetPath());
        GD.Print("MAIN_FLOW_CLICK " + control.GetPath());
        await ClickPoint(control.GetGlobalRect().GetCenter());
    }
    private async Task ClickPoint(Vector2 point)
    {
        await MovePointer(point);
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }
    private async Task MovePointer(Vector2 point)
    {
        Input.WarpMouse(point);
        await Frames(1);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(1);
    }
    private async Task KeyPress(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await Frames(2);
    }
    private async Task Capture(string name)
    {
        await Frames(2);
        var path = ProjectSettings.GlobalizePath($"{Output}/after-{name}.png");
        Require(GetViewport().GetTexture().GetImage().SavePng(path) == Error.Ok, "save screenshot: " + path);
        _captures++;
        GD.Print("MAIN_FLOW_CAPTURE " + path);
    }
    private async Task Until(Func<bool> ready, string label, int limit = 90)
    {
        for (var index = 0; index < limit; index++)
        {
            if (ready()) return;
            if (index > 0 && index % 600 == 0) GD.Print($"MAIN_FLOW_WAIT {label} rendered-frames={index}");
            await Frames(1);
        }
        throw new InvalidOperationException("Timed out: " + label);
    }
    private async Task Frames(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
    private void Step(string message) { _step = message; GD.Print("MAIN_FLOW_STEP " + message); }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
