using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.UI;

public partial class UiSmoke : Node
{
    public override async void _Ready()
    {
        var code = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GetTree().Quit(code);
    }

    private async Task<int> RunAsync()
    {
        GameRoot? root = null;
        try
        {
            new SaveService("tests/ui-smoke").DeleteActiveRun();
            root = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
            root.SaveNamespace = "tests/ui-smoke";
            AddChild(root);
            for (var frame = 0; frame < 10 && root.Content is null; frame++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (root.Content is null) throw new InvalidOperationException("game root content gate did not finish");
            var screens = root.GetNode<Control>("Screens");
            if (screens.GetChildCount() != 14 || !root.GetNode<Control>("Screens/MainMenuScreen").Visible ||
                root.GetNodeOrNull<Control>("Screens/BattleReportScreen") is null ||
                root.GetNodeOrNull<Control>("Screens/BattleLabScreen") is null)
                throw new InvalidOperationException("main menu composition");
            foreach (var child in screens.GetChildren().OfType<Control>())
                if (child.Size.X < 1200 || child.Size.Y < 650) throw new InvalidOperationException("screen layout: " + child.Name);

            Press(root, "Screens/MainMenuScreen/Center/Panel/Menu/NewRunButton");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var heroScreen = root.GetNode<HeroSelectScreen>("Screens/HeroSelectScreen");
            if (!heroScreen.Visible) throw new InvalidOperationException("hero navigation");
            var heroTiles = root.GetNode<GridContainer>("Screens/HeroSelectScreen/Margin/Layout/Content/LibraryPanel/LibraryLayout/LibraryScroll/HeroLibrary")
                .GetChildren().OfType<HeroLibraryTile>().ToArray();
            var expectedHeroIds = root.Content.Catalog.Heroes.Select(entry => entry.StableId).Order(StringComparer.Ordinal).ToArray();
            var presentedHeroIds = heroTiles.Select(tile => tile.StableId).Order(StringComparer.Ordinal).ToArray();
            if (!presentedHeroIds.SequenceEqual(expectedHeroIds) ||
                heroTiles.Any(tile => tile.GetNode<UnitPortrait>("%HeroPortrait").Definition is null))
                throw new InvalidOperationException("hero library did not author one compact animated portrait tile per cataloged hero");
            var application = (RunApplication?)typeof(GameRoot).GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(root)
                ?? throw new InvalidOperationException("run application unavailable");
            var commanderTile = heroTiles.Single(tile => tile.StableId == "hero_banner_marshal");
            commanderTile.EmitSignal(BaseButton.SignalName.Pressed);
            if (application.ActiveRun is not null)
                throw new InvalidOperationException("hero library preview incorrectly started a run");
            VerifyHeroDetail(root, "hero_banner_marshal");
            VerifyReportAndSemanticResources();
            VerifyHeroDetail(root, "hero_bone_regent");
            VerifyHeroDetail(root, "hero_brood_matriarch");
            VerifyHeroDetail(root, "hero_gear_architect");
            VerifyHeroDetail(root, "hero_crimson_count");
            VerifyHeroDetail(root, "hero_edge_ascetic");
            VerifyHeroDetail(root, "hero_hour_arbiter");
            VerifyHeroDetail(root, "hero_gilded_factor");
            heroScreen.Preview("hero_banner_marshal");
            Press(root, "Screens/HeroSelectScreen/Margin/Layout/Content/HeroDetailPanel/Layout/DeployButton");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!root.GetNode<Control>("Screens/TowerScreen").Visible) throw new InvalidOperationException("tower navigation");
            var equipmentEntry = root.Content.Catalog.Items.Single(entry =>
                entry.StableId == "equipment_vanguard_insignia" &&
                entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Equipment });
            var equipmentOwner = application.ActiveRun?.Roster[0].InstanceId ??
                throw new InvalidOperationException("Equipment visibility Run owner unavailable");
            if (!application.EquipItem(equipmentOwner, 0, equipmentEntry.StableId))
                throw new InvalidOperationException("Equipment visibility fixture could not equip the starting hero");
            root.Flow.ShowTower();
            var overview = root.GetNode<ArmyOverviewController>("ArmyOverview");
            var summaryButton = root.GetNode<Button>("ArmyOverview/SummaryButton");
            var deployedSummary = root.GetNode<SemanticChip>("ArmyOverview/SummaryButton/ResourceStrip/Deployed");
            if (!overview.Visible || !string.IsNullOrEmpty(summaryButton.Text) || deployedSummary.ResolvedIcon is null ||
                string.IsNullOrWhiteSpace(deployedSummary.DisplayText))
                throw new InvalidOperationException("army overview missing on tower");
            summaryButton.GrabFocus();
            Press(root, "ArmyOverview/SummaryButton");
            if (!root.GetNode<Control>("ArmyOverview/Drawer").Visible) throw new InvalidOperationException("army drawer did not open");
            var armyUnitRow = root.GetNode<VBoxContainer>("ArmyOverview/Drawer/Layout/Scroll/Rows").GetChildren()
                .OfType<ArmyDrawerRow>().FirstOrDefault(row => row.GetNode<UnitPortrait>("%UnitPortrait").Visible);
            if (armyUnitRow?.GetNode<UnitPortrait>("%UnitPortrait").Definition is null)
                throw new InvalidOperationException("Army details did not reuse the authored unit portrait component");
            var equipmentName = ((ItemDefinition)equipmentEntry.Definition).DisplayName;
            if (!armyUnitRow.GetNode<Label>("%RowDetails").Text.Contains($"装备：{equipmentName}", StringComparison.Ordinal))
                throw new InvalidOperationException("Army hero details did not expose authored Equipment ownership");
            var tacticalRow = root.GetNode<VBoxContainer>("ArmyOverview/Drawer/Layout/Scroll/Rows").GetChildren()
                .OfType<ArmyDrawerRow>().FirstOrDefault(row => row.GetNode<Label>("%RowTitle").Text.StartsWith("槽位 1", StringComparison.Ordinal))
                ?? throw new InvalidOperationException("Army overview lacks the first tactical-command row");
            var tacticalCost = tacticalRow.GetNode<ResourceCostBadge>("%TacticalPointCostBadge");
            if (!tacticalCost.Visible || tacticalCost.DisplayText != "1 战术点")
                throw new InvalidOperationException("Army tactical command cost is not a structured tactical-point badge");
            var closeButton = root.GetNode<Button>("ArmyOverview/Drawer/Layout/Header/CloseButton");
            if (!overview.IsOpen || screens.FocusBehaviorRecursive != Control.FocusBehaviorRecursiveEnum.Disabled ||
                summaryButton.FocusMode != Control.FocusModeEnum.None || GetViewport().GuiGetFocusOwner() != closeButton)
                throw new InvalidOperationException("army drawer did not establish a modal focus scope");
            var underlyingButton = root.GetNode<Button>("Screens/TowerScreen/Margin/Layout/AbandonButton");
            underlyingButton.GrabFocus();
            if (GetViewport().GuiGetFocusOwner() == underlyingButton)
                throw new InvalidOperationException("army drawer allowed focus to escape to the underlying screen");
            Press(root, "ArmyOverview/Drawer/Layout/Header/CloseButton");
            if (overview.IsOpen || screens.FocusBehaviorRecursive == Control.FocusBehaviorRecursiveEnum.Disabled ||
                GetViewport().GuiGetFocusOwner() != summaryButton)
                throw new InvalidOperationException("army drawer did not restore its prior focus and scope");

            VerifyRecruitmentCard(root);
            await VerifyRewardLayoutIsolationAsync(root);
            await VerifyMerchantHudAsync(root.Content);
            root.Flow.ShowTower();

            var battleRoute = root.GetNode<Container>("Screens/TowerScreen/Margin/Layout/Choices").GetChildren().OfType<ChoiceCard>()
                .FirstOrDefault(card => card.StableId is "Combat" or "Elite" or "Boss");
            if (battleRoute is not null)
            {
                VerifyStructuredCard(battleRoute);
                battleRoute.EmitSignal(BaseButton.SignalName.Pressed);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var board = root.GetNode<DeploymentBoard>("Screens/DeploymentScreen/Margin/Layout/Columns/BoardPanel/DeploymentBoard");
                var cells = board.GetChildren().OfType<DeploymentCell>().ToArray();
                if (cells.Length != 18 || cells.Any(cell => !BattlefieldLayout.IsPlayerDeploymentCell(cell.Cell)))
                    throw new InvalidOperationException("deployment board did not expose all 18 first-three-column cells");
                var reserveSummary = root.GetNode<SemanticChip>("ArmyOverview/SummaryButton/ResourceStrip/Reserve");
                if (!overview.Visible || reserveSummary.ResolvedIcon is null || string.IsNullOrWhiteSpace(reserveSummary.DisplayText))
                    throw new InvalidOperationException("army overview missing on deployment");
                var deploymentCards = root.GetNode<VBoxContainer>("Screens/DeploymentScreen/Margin/Layout/Columns/RosterPanel/RosterScroll/RosterChoices")
                    .GetChildren().OfType<DeploymentUnitCard>().ToArray();
                if (deploymentCards.Length == 0 || deploymentCards.Any(card => card.Portrait.Definition is null))
                    throw new InvalidOperationException("deployment list did not reuse authored unit portraits");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                VerifyDeploymentFactsFit(
                    root.GetNode<ScrollContainer>("Screens/DeploymentScreen/Margin/Layout/Columns/RosterPanel/RosterScroll"),
                    deploymentCards);
                Press(root, "Screens/DeploymentScreen/Margin/Layout/Actions/BackButton");
            }

            Press(root, "Screens/TowerScreen/Margin/Layout/AbandonButton");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!root.GetNode<Control>("Screens/ResultScreen").Visible) throw new InvalidOperationException("result navigation");
            Press(root, "Screens/ResultScreen/Center/Panel/Layout/MenuButton");
            Press(root, "Screens/MainMenuScreen/Center/Panel/Menu/SettingsButton");
            if (!root.GetNode<Control>("Screens/SettingsScreen").Visible) throw new InvalidOperationException("settings navigation");
            if (root.HasNode("Screens/SettingsScreen/Center/Panel/Layout/DamageCheck"))
                throw new InvalidOperationException("settings exposes unsupported damage-number control");
            Press(root, "Screens/SettingsScreen/Center/Panel/Layout/SaveButton");
            if (!root.GetNode<Control>("Screens/MainMenuScreen").Visible) throw new InvalidOperationException("settings return");

            VerifyBattleReportRouting(root, root.Content);

            GD.Print("UI_SMOKE_OK screens=14 navigation=menu-lab-hero-tower-report-reward-recruitment-result-settings hero=command-details recruitment=localized-traits,single-column,fixed-actions,isolated-screen merchant=economy-hud hierarchy=structured-cards,semantic-theme,icons portraits=hero,recruitment,deployment,army,report report=authored-responsive,active-tabs,seconds-only,explicit-zero,theme-tints interaction=modal-focus,battle-selection,tactical-command-mouse,zero-tactical-points");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("UI_SMOKE_FAILED: " + exception);
            return 1;
        }
        finally
        {
            if (root is not null)
            {
                if (root.GetParent() is not null) root.GetParent().RemoveChild(root);
                root.Free();
            }
        }
    }

    private static void Press(Node root, string path) => root.GetNode<Button>(path).EmitSignal(BaseButton.SignalName.Pressed);

    private async Task ClickAsync(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion
        {
            Position = point,
            GlobalPosition = point
        }, true);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = point,
            GlobalPosition = point
        }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = false,
            Position = point,
            GlobalPosition = point
        }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void VerifyHeroDetail(GameRoot root, string stableId, params string[] values)
    {
        var screen = root.GetNode<HeroSelectScreen>("Screens/HeroSelectScreen");
        screen.Preview(stableId);
        var detail = root.GetNode<HeroDetailPanel>("Screens/HeroSelectScreen/Margin/Layout/Content/HeroDetailPanel");
        if (detail.GetNodeOrNull<Control>("Layout/DetailScroll/Content/HeroAbilityPanel") is not null)
            throw new InvalidOperationException("hero detail still presents a hero-owned tactical command");
        var text = string.Join("\n", detail.FindChildren("*", "Label", true, false).OfType<Label>().Select(label => label.Text));
        foreach (var value in values)
            if (!text.Contains(value, StringComparison.Ordinal))
                throw new InvalidOperationException($"hero detail {stableId} omitted generated value {value}");
        var stats = detail.FindChildren("*", "", true, false).OfType<StatBlock>().ToArray();
        if (stats.Length != 3 || stats.Select(stat => stat.SemanticKey.ToString()).Order().SequenceEqual(new[] { "damage", "health", "reach" }) is false)
            throw new InvalidOperationException("hero detail lacks three typed core stat blocks");
    }

    private static void VerifyStructuredUnitCard(UnitChoiceCard card)
    {
        if (card.GetNodeOrNull<UnitPortrait>("%UnitPortrait") is null ||
            card.GetNodeOrNull<Label>("%UnitName") is null ||
            card.GetNodeOrNull<HFlowContainer>("%IdentityFacts") is not { } identityFacts ||
            card.GetNodeOrNull<HFlowContainer>("%AttributeFacts") is not { } attributeFacts ||
            card.GetNodeOrNull<Label>("%UnitDescription") is null ||
            card.GetNodeOrNull<Label>("%UnitMeta") is null ||
            card.Portrait.Definition is null ||
            identityFacts.GetChildren().OfType<TraitBadge>().Count() < 2 ||
            attributeFacts.GetChildren().OfType<StatBlock>().Select(stat => stat.SemanticKey.ToString()).Order().SequenceEqual(new[] { "damage", "health", "reach" }) is false)
            throw new InvalidOperationException("UnitChoiceCard omitted its authored portrait/fact hierarchy");
    }

    private static void VerifyDeploymentFactsFit(ScrollContainer scroll, DeploymentUnitCard[] cards)
    {
        var scrollRect = scroll.GetGlobalRect();
        foreach (var card in cards)
        {
            var facts = card.GetNode<HBoxContainer>("%UnitFacts");
            var chips = facts.GetChildren().OfType<SemanticChip>().ToArray();
            var requiredWidth = facts.GetCombinedMinimumSize().X;
            var cardRect = card.GetGlobalRect();
            var factsRect = facts.GetGlobalRect();
            var visibleRight = Mathf.Min(
                cardRect.Position.X + cardRect.Size.X,
                scrollRect.Position.X + scrollRect.Size.X);
            var visibleWidth = Mathf.Max(0f, visibleRight - factsRect.Position.X);
            if (chips.Length != 3 || chips.Any(chip => chip.ResolvedIcon is null) || requiredWidth > visibleWidth + .5f)
                throw new InvalidOperationException(
                    $"deployment semantic facts are clipped or incomplete: needs {requiredWidth:0.#}px, authored visible rect is {visibleWidth:0.#}px");
        }
    }

    private static void VerifyStructuredCard(ChoiceCard card)
    {
        if (card.GetNodeOrNull<TextureRect>("%ChoiceIcon") is null ||
            card.GetNodeOrNull<Label>("%ChoiceTitle") is null ||
            card.GetNodeOrNull<Label>("%ChoiceBody") is null ||
            card.GetNodeOrNull<Label>("%ChoiceFooter") is null)
            throw new InvalidOperationException("ChoiceCard is still a concatenated text button instead of an authored structured card");
    }

    private static void VerifyReportAndSemanticResources()
    {
        foreach (var path in new[]
                 {
                     "res://scenes/ui/BattleReportScreen.tscn",
                     "res://scenes/ui/components/BattleReportTeamSummary.tscn",
                     "res://scenes/ui/components/BattleReportComparison.tscn",
                     "res://scenes/ui/components/BattleReportCoreMatchupRow.tscn",
                     "res://scenes/ui/components/BattleReportRosterStrip.tscn",
                     "res://scenes/ui/components/BattleReportLeaderboardHeader.tscn",
                     "res://scenes/ui/components/BattleReportLeaderboardRow.tscn",
                     "res://scenes/ui/components/BattleReportUnitDetail.tscn",
                     "res://scenes/ui/components/IconText.tscn"
                 })
            if (!ResourceLoader.Exists(path)) throw new InvalidOperationException("missing authored report/UI scene: " + path);
        foreach (var name in new[] { "health", "damage", "shield", "healing", "mana", "gold", "time", "kills", "deaths", "hero", "melee", "ranged", "risk", "loot" })
            if (!ResourceLoader.Exists($"res://assets/ui/icons/{name}.svg"))
                throw new InvalidOperationException("missing semantic icon: " + name);
        var themeSource = FileAccess.GetFileAsString("res://content/ui/RealmTheme.tres");
        foreach (var variation in new[]
                 {
                     "HeroLabel", "PlayerLabel", "HealingLabel", "EnemyLabel", "WarningLabel", "SecondaryLabel",
                     "HealingTitleLabel", "EnemyTitleLabel", "WarningTitleLabel", "ModalPanel",
                     "NotificationPanel", "PrimaryButton", "SecondaryButton", "CompactButton",
                     "ReportPlayerSummarySurface", "ReportEnemySummarySurface", "ReportUnitCardSurface",
                     "ReportHeroCardSurface", "ReportDefeatedCardSurface"
                 })
            if (!themeSource.Contains(variation + "/base_type", StringComparison.Ordinal))
                throw new InvalidOperationException("shared Theme omitted semantic variation " + variation);
    }

    private static void VerifyBattleReportRouting(GameRoot root, ContentRegistry content)
    {
        VerifyBattleReportRoute(root, content, BattleOutcome.PlayerVictory, TowerNodeType.Combat, 0,
            "Screens/RewardScreen", "ordinary-victory", 2101);
        VerifyBattleReportRoute(root, content, BattleOutcome.PlayerVictory, TowerNodeType.Boss, 14,
            "Screens/ResultScreen", "final-victory", 2102);
        VerifyBattleReportRoute(root, content, BattleOutcome.PlayerDefeat, TowerNodeType.Combat, 0,
            "Screens/ResultScreen", "defeat", 2103);
        VerifyBattleReportRoute(root, content, BattleOutcome.Timeout, TowerNodeType.Combat, 0,
            "Screens/ResultScreen", "timeout", 2104);
    }

    private static void VerifyBattleReportRoute(
        GameRoot root,
        ContentRegistry content,
        BattleOutcome outcome,
        TowerNodeType nodeType,
        int floor,
        string expectedRoute,
        string label,
        ulong seed)
    {
        var save = new SaveService("tests/ui-report-" + label);
        save.DeleteActiveRun();
        save.SaveMeta(new MetaProgressDto { UnlockedHeroIds = content.Catalog.Heroes.Select(entry => entry.StableId).ToList() });
        var app = new RunApplication(content, save, TestProjectFixture.Load(content));
        if (!app.StartNewRun(content.Catalog.Heroes[0].StableId, seed))
            throw new InvalidOperationException("report route run start: " + label);
        app.ActiveRun!.FloorIndex = floor;
        app.ActiveRun.SelectedNode = nodeType;
        app.ActiveRun.PendingNode = true;
        var encounter = app.CurrentEncounter();
        var result = SyntheticResult(app.BuildBattleConfig(encounter), outcome);
        var floorBefore = app.ActiveRun.FloorIndex;

        SetPrivate(root, "_app", app);
        root.Flow.SetEncounterForTesting(encounter);
        root.Flow.ResetPendingBattleFlow();
        root.Flow.Show(AppScreenId.Battle);
        root.Flow.AcceptBattleResult(result);
        root.Flow.AcceptBattleResult(result);
        if (!root.GetNode<Control>("Screens/BattleScreen").Visible)
            throw new InvalidOperationException("authoritative resolution navigated before fade/report: " + label);
        if (outcome == BattleOutcome.PlayerVictory && floor < 14 && app.ActiveRun?.FloorIndex != floorBefore + 1)
            throw new InvalidOperationException("ordinary victory did not resolve exactly once before report");
        if ((outcome != BattleOutcome.PlayerVictory || floor == 14) && app.ActiveRun is not null)
            throw new InvalidOperationException("terminal run resolution did not commit before report: " + label);

        root.Flow.ShowBattleReport();
        root.Flow.ShowBattleReport();
        var report = root.GetNode<BattleReportScreen>("Screens/BattleReportScreen");
        var playerSummary = root.GetNode<BattleReportTeamSummary>("Screens/BattleReportScreen/Margin/Panel/Layout/TeamComparison/PlayerSummary");
        var enemySummary = root.GetNode<BattleReportTeamSummary>("Screens/BattleReportScreen/Margin/Panel/Layout/TeamComparison/EnemySummary");
        var playerTab = root.GetNode<Button>("Screens/BattleReportScreen/Margin/Panel/Layout/Controls/AllegianceTabs/PlayerTab");
        var enemyTab = root.GetNode<Button>("Screens/BattleReportScreen/Margin/Panel/Layout/Controls/AllegianceTabs/EnemyTab");
        var overviewTab = root.GetNode<Button>("Screens/BattleReportScreen/Margin/Panel/Layout/Controls/DimensionTabs/OverviewTab");
        var offenseTab = root.GetNode<Button>("Screens/BattleReportScreen/Margin/Panel/Layout/Controls/DimensionTabs/OffenseTab");
        var survivalTab = root.GetNode<Button>("Screens/BattleReportScreen/Margin/Panel/Layout/Controls/DimensionTabs/SurvivalTab");
        var healingTab = root.GetNode<Button>("Screens/BattleReportScreen/Margin/Panel/Layout/Controls/DimensionTabs/HealingTab");
        var duration = root.GetNode<Label>("Screens/BattleReportScreen/Margin/Panel/Layout/OutcomeBanner/Header/Meta/ReportDuration/Text").Text;
        var outcomeTitle = root.GetNode<Label>("Screens/BattleReportScreen/Margin/Panel/Layout/OutcomeBanner/Header/Headline/ReportOutcome");
        var overviewPage = report.GetNode<Control>("%OverviewPage");
        var leaderboardPage = report.GetNode<Control>("%LeaderboardPage");
        var leaderboardList = report.GetNode<VBoxContainer>("%LeaderboardList");
        var empty = report.GetNode<Control>("%EmptyState");
        if (!report.Visible || playerSummary.Team != 0 || enemySummary.Team != 1 ||
            !overviewPage.Visible || leaderboardPage.Visible ||
            report.GetNode<BattleReportComparison>("%OverviewComparison").GetNode<VBoxContainer>("%CoreMatchups").GetChildCount() != 3)
            throw new InvalidOperationException("battle report did not bind authored statistical overview: " + label);
        var rosterPortraits = report.GetNode<BattleReportRosterStrip>("%PlayerRosterStrip")
            .GetNode<HFlowContainer>("%RosterPortraits").GetChildren().OfType<BattleReportRosterPortrait>().ToArray();
        if (rosterPortraits.Length == 0 || rosterPortraits.Any(entry => entry.GetNode<UnitPortrait>("%UnitPortrait").Definition is null))
            throw new InvalidOperationException("battle report roster strip did not reuse authored portraits: " + label);
        if (!playerTab.ButtonPressed || enemyTab.ButtonPressed || playerTab.Disabled || enemyTab.Disabled ||
            !overviewTab.ButtonPressed || report.SelectedTeam != 0 || report.SelectedDimension != BattleReportDimension.Overview ||
            playerTab.ThemeTypeVariation != "ReportTabButton" || enemyTab.ThemeTypeVariation != "ReportTabButton")
            throw new InvalidOperationException("player report tab did not expose an enabled active state: " + label);
        if (!duration.StartsWith("模拟时长 ", StringComparison.Ordinal) ||
            duration.Contains("tick", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("battle report duration exposed internal tick text: " + label);
        var expectedOutcomeVariation = outcome switch
        {
            BattleOutcome.PlayerVictory => new StringName("HealingTitleLabel"),
            BattleOutcome.Timeout => new StringName("WarningTitleLabel"),
            _ => new StringName("EnemyTitleLabel")
        };
        if (outcomeTitle.ThemeTypeVariation != expectedOutcomeVariation || outcomeTitle.GetThemeFontSize("font_size") != 36)
            throw new InvalidOperationException("battle report outcome lost its title-sized semantic variation: " + label);

        Press(root, "Screens/BattleReportScreen/Margin/Panel/Layout/Controls/DimensionTabs/OffenseTab");
        var rows = leaderboardList.GetChildren().OfType<BattleReportLeaderboardRow>().ToArray();
        var leaderboardHeader = report.GetNode<BattleReportLeaderboardHeader>("%LeaderboardHeader");
        if (report.SelectedDimension != BattleReportDimension.Offense || !offenseTab.ButtonPressed ||
            rows.Length == 0 || leaderboardHeader.GetNode<Label>("%PrimaryHeader").Text != "有效伤害" ||
            rows.Any(row => row.GetNode<ProgressBar>("%ContributionBar").Value < 0) ||
            string.IsNullOrWhiteSpace(report.SelectedRuntimeId) ||
            root.GetViewport().GuiGetFocusOwner() != offenseTab)
            throw new InvalidOperationException("output dimension did not bind fixed columns/order/selection/focus: " + label);
        Press(root, "Screens/BattleReportScreen/Margin/Panel/Layout/Controls/DimensionTabs/SurvivalTab");
        if (report.SelectedDimension != BattleReportDimension.Survival || !survivalTab.ButtonPressed ||
            leaderboardHeader.GetNode<Label>("%PrimaryHeader").Text != "有效承伤" || leaderboardList.GetChildCount() == 0)
            throw new InvalidOperationException("survival dimension did not replace fixed columns: " + label);
        Press(root, "Screens/BattleReportScreen/Margin/Panel/Layout/Controls/DimensionTabs/HealingTab");
        if (report.SelectedDimension != BattleReportDimension.Healing || !healingTab.ButtonPressed ||
            (!empty.Visible && (leaderboardHeader.GetNode<Label>("%PrimaryHeader").Text != "有效治疗" || leaderboardList.GetChildCount() == 0)))
            throw new InvalidOperationException("healing dimension did not bind fixed columns or deliberate zero state: " + label);

        Press(root, "Screens/BattleReportScreen/Margin/Panel/Layout/Controls/AllegianceTabs/EnemyTab");
        if (!empty.Visible || playerTab.ButtonPressed || !enemyTab.ButtonPressed || report.SelectedTeam != 1 ||
            root.GetViewport().GuiGetFocusOwner() != enemyTab)
            throw new InvalidOperationException("enemy zero-healing state or allegiance focus did not bind: " + label);
        Press(root, "Screens/BattleReportScreen/Margin/Panel/Layout/Controls/DimensionTabs/OverviewTab");
        if (!overviewPage.Visible || leaderboardPage.Visible || empty.Visible || report.SelectedDimension != BattleReportDimension.Overview)
            throw new InvalidOperationException("enemy report tab did not bind: " + label);
        Press(root, "Screens/BattleReportScreen/Margin/Panel/Layout/ReportContinue");
        Press(root, "Screens/BattleReportScreen/Margin/Panel/Layout/ReportContinue");
        if (!root.GetNode<Control>(expectedRoute).Visible)
            throw new InvalidOperationException("post-report route mismatch: " + label);
        save.DeleteActiveRun();
    }

    private static BattleResult SyntheticResult(BattleConfig config, BattleOutcome outcome)
    {
        var units = config.Spawns.Select(spawn => new BattleUnitReportSnapshot(
            spawn.InstanceId,
            spawn.InstanceId,
            spawn.Unit.ContentId,
            spawn.Unit.DisplayName,
            spawn.Unit.Role,
            spawn.Team,
            spawn.Unit.IsHero,
            spawn.IsTemporary,
            outcome != BattleOutcome.PlayerDefeat || spawn.Team == 1,
            spawn.Cell,
            outcome == BattleOutcome.PlayerDefeat && spawn.Team == 0 ? 0 : spawn.Unit.MaxHealth,
            spawn.Unit.MaxHealth,
            0,
            spawn.Unit.Damage,
            spawn.Team == 0 ? (outcome == BattleOutcome.PlayerDefeat ? 0 : 100) : 30,
            spawn.Team == 0 ? 30 : 100,
            0,
            spawn.Team == 0 ? 20 : 0,
            spawn.Team == 0 ? 1 : 0,
            0,
            outcome == BattleOutcome.PlayerDefeat && spawn.Team == 0 ? 12 : null,
            spawn.Team == 0 ? 3 : 1,
            spawn.Team == 0 ? 1 : 0)).ToImmutableArray();
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
        return new BattleResult(outcome, 25, new string('a', 64), units, 3, 2, relicTransition,
            config.Identity);
    }

    private static void VerifyRecruitmentCard(GameRoot root)
    {
        var application = (RunApplication?)typeof(GameRoot).GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(root)
            ?? throw new InvalidOperationException("run application unavailable");
        var heroId = application.ActiveRun is { Roster.Count: > 0 } run
            ? run.Roster[0].ContentId
            : throw new InvalidOperationException("active run unavailable");
        var found = false;
        for (ulong seed = 1; seed <= 2000; seed++)
        {
            application.AbandonRun();
            if (!application.StartNewRun(heroId, seed)) throw new InvalidOperationException("seeded UI run start failed");
            if (application.RecruitmentChoices().Any(entry => entry.StableId == "soldier_abyss_crawler"))
            {
                found = true;
                break;
            }
        }
        if (!found) throw new InvalidOperationException("no deterministic recruitment seed for abyss crawler");

        root.Flow.ShowRecruitment();
        var scroll = root.GetNode<ScrollContainer>("Screens/RecruitmentScreen/Center/Panel/Layout/ChoiceScroll");
        var choices = root.GetNode<Container>("Screens/RecruitmentScreen/Center/Panel/Layout/ChoiceScroll/Choices");
        var card = choices.GetChildren().OfType<UnitChoiceCard>().Single(choice => choice.StableId == "soldier_abyss_crawler");
        if (!card.SearchText.Contains("亡灵", StringComparison.Ordinal) || !card.SearchText.Contains("野兽", StringComparison.Ordinal) ||
            card.SearchText.Contains("soldier", StringComparison.Ordinal) || card.SearchText.Contains("undead", StringComparison.Ordinal) ||
            card.SearchText.Contains("beast", StringComparison.Ordinal))
            throw new InvalidOperationException("recruitment card did not localize multi-trait gameplay tags");
        if (choices.GetChildCount() != 3 || choices.GetChildren().OfType<UnitChoiceCard>().Any(choice =>
                choice.CustomMinimumSize.Y is < 170 or > 174 || choice.Portrait.CustomMinimumSize.X is < 104 or > 108))
            throw new InvalidOperationException("recruitment did not author three compact unit rows with 106px portraits");
        if (!scroll.IsAncestorOf(root.GetNode("Screens/RecruitmentScreen/Center/Panel/Layout/ChoiceScroll/Choices")) ||
            scroll.IsAncestorOf(root.GetNode("Screens/RecruitmentScreen/Center/Panel/Layout/ContinueButton")) ||
            scroll.IsAncestorOf(root.GetNode("Screens/RecruitmentScreen/Center/Panel/Layout/ConvertButton")))
            throw new InvalidOperationException("recruitment scroll ownership moved the fixed bottom actions");
        VerifyStructuredUnitCard(card);
    }

    private async Task VerifyRewardLayoutIsolationAsync(GameRoot root)
    {
        root.Flow.ShowCombatReward();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var ordinary = root.GetNode<Control>("Screens/RewardScreen");
        var ordinaryPanel = ordinary.GetNode<PanelContainer>("Center/Panel");
        var ordinaryChoices = ordinary.GetNode<Container>("Center/Panel/Layout/ChoiceScroll/Choices");
        var ordinaryCards = ordinaryChoices.GetChildren().OfType<ChoiceCard>().ToArray();
        if (!ordinary.Visible || ordinaryCards.Length != 3 ||
            ordinaryPanel.CustomMinimumSize != new Vector2(900, 650) || ordinaryPanel.Size != new Vector2(900, 650) ||
            ordinaryCards.Any(card => card.CustomMinimumSize != new Vector2(250, 112) || card.Size.X > 865f))
            throw new InvalidOperationException(
                $"ordinary reward layout drifted from 900x650 / <=864px cards: panel={ordinaryPanel.Size}, cards={string.Join(',', ordinaryCards.Select(card => card.Size.X.ToString("0.#")))}");

        root.Flow.ShowRecruitment();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var recruitment = root.GetNodeOrNull<Control>("Screens/RecruitmentScreen")
            ?? throw new InvalidOperationException("recruitment has no independent authored screen");
        var recruitmentPanel = recruitment.GetNode<PanelContainer>("Center/Panel");
        var recruitmentScroll = recruitment.GetNode<ScrollContainer>("Center/Panel/Layout/ChoiceScroll");
        var recruitmentChoices = recruitment.GetNode<Container>("Center/Panel/Layout/ChoiceScroll/Choices");
        if (!recruitment.Visible || ordinary.Visible ||
            recruitmentPanel.CustomMinimumSize != new Vector2(980, 760) || recruitmentPanel.Size != new Vector2(980, 760) ||
            recruitmentChoices.GetChildren().OfType<UnitChoiceCard>().Count() != 3 ||
            recruitmentScroll.IsAncestorOf(recruitment.GetNode("Center/Panel/Layout/ContinueButton")) ||
            recruitmentScroll.IsAncestorOf(recruitment.GetNode("Center/Panel/Layout/ConvertButton")))
            throw new InvalidOperationException("recruitment did not retain its independent 980x760 single-column/fixed-action layout");
    }

    private async Task VerifyMerchantHudAsync(ContentRegistry content)
    {
        var merchantEntry = content.Catalog.Heroes.Single(entry => entry.StableId == "hero_gilded_factor");
        var enemyEntry = content.Catalog.Enemies[0];
        var merchantRoot = merchantEntry.Scene.Instantiate<UnitContentRoot>();
        var enemyRoot = enemyEntry.Scene.Instantiate<UnitContentRoot>();
        var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
        AddChild(screen);
        try
        {
            var hero = BattleSetupFactory.Snapshot((UnitDefinition)merchantEntry.Definition, merchantRoot.Behavior);
            var enemy = BattleSetupFactory.Snapshot((UnitDefinition)enemyEntry.Definition, enemyRoot.Behavior);
            var rule = BattleSetupFactory.Snapshot(merchantRoot.HeroRule!);
            var summonEntry = content.Catalog.Soldiers.Single(entry => entry.StableId == merchantRoot.HeroRule!.SummonContentId);
            var summon = BattleSetupFactory.Snapshot(summonEntry);

            screen.StartBattle(content, MerchantConfig(content, hero, enemy, rule, summon, 5), "商人指令测试");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var status = screen.GetNode<BattleStatusStrip>("%BattleStatus");
            var command = screen.GetNode<TacticalCommandSlot>(
                "Margin/Layout/Hud/ControlRow/TacticalCommandHud/Layout/Slots/TacticalCommandSlot0");
            var secondCommand = screen.GetNode<TacticalCommandSlot>(
                "Margin/Layout/Hud/ControlRow/TacticalCommandHud/Layout/Slots/TacticalCommandSlot1");
            var commandName = command.GetNode<Label>("%CommandName");
            var secondCommandName = secondCommand.GetNode<Label>("%CommandName");
            var pointCost = command.GetNode<ResourceCostBadge>("%TacticalPointCostBadge");
            var goldCost = command.GetNode<ResourceCostBadge>("%GoldCostBadge");
            var points = screen.GetNode<Label>(
                "Margin/Layout/Hud/ControlRow/TacticalCommandHud/Layout/Resource/TacticalPointsText");
            if (!commandName.Text.Contains("加急雇佣", StringComparison.Ordinal) ||
                !secondCommandName.Text.Contains("全军集结", StringComparison.Ordinal) ||
                command.CommandId == secondCommand.CommandId ||
                pointCost.DisplayText != "1 战术点" || goldCost.DisplayText != "5 金币" ||
                !points.Text.Contains("3/3", StringComparison.Ordinal) || status.DisplayedGold != 5)
                throw new InvalidOperationException("two-slot merchant command loadout, cost, or balance is not visible");
            await ClickAsync(command);
            if (screen.TacticalPoints != 2 || screen.RemainingGold != 0 || screen.TemporaryUnitCount != 1 || !points.Text.Contains("2/3", StringComparison.Ordinal) ||
                status.DisplayedGold != 0)
                throw new InvalidOperationException("merchant command success did not refresh HUD economy");

            var board = screen.GetNode<BattleBoard>("%BattleBoard");
            var clickPosition = board.GlobalPosition + board.CellToLocal(new Vector2I(0, 2));
            var boardReceivedInput = false;
            var receivedPosition = Vector2.Zero;
            board.GuiInput += inputEvent =>
            {
                boardReceivedInput = true;
                if (inputEvent is InputEventMouseButton receivedMouse) receivedPosition = receivedMouse.Position;
            };
            GetViewport().PushInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                Position = clickPosition,
                GlobalPosition = clickPosition
            }, true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var selectedPanel = screen.GetNode<Control>("%SelectedUnitPanel");
            var selectedTitle = selectedPanel.GetNode<Label>("Layout/UnitTitle");
            var selectedAction = selectedPanel.GetNode<Label>("Layout/UnitAction");
            if (!selectedPanel.Visible || !selectedTitle.Text.Contains(((UnitDefinition)merchantEntry.Definition).DisplayName, StringComparison.Ordinal) ||
                !selectedAction.Text.Contains("行动：", StringComparison.Ordinal))
                throw new InvalidOperationException($"production BattleBoard mouse input did not open selected-unit details (received={boardReceivedInput}, click={clickPosition}, local={receivedPosition}, board={board.GlobalPosition}/{board.Size})");

            screen.StartBattle(content, MerchantConfig(content, hero, enemy, rule, summon, 4), "商人余额不足测试");
            command.EmitSignal(BaseButton.SignalName.Pressed);
            if (screen.TacticalPoints != 3 || screen.RemainingGold != 4 || screen.TemporaryUnitCount != 0 ||
                !screen.CommandFeedback.Contains("金币不足", StringComparison.Ordinal) || !status.FeedbackText.Contains("金币不足", StringComparison.Ordinal))
                throw new InvalidOperationException("merchant insufficient-gold feedback or invariants failed");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!status.FeedbackText.Contains("金币不足", StringComparison.Ordinal))
                throw new InvalidOperationException("merchant insufficient-gold feedback was overwritten by process HUD refresh");

            screen.StartBattle(content, MerchantConfig(content, hero, enemy, rule, summon, 20), "商人战术点耗尽测试");
            command.EmitSignal(BaseButton.SignalName.Pressed);
            command.EmitSignal(BaseButton.SignalName.Pressed);
            command.EmitSignal(BaseButton.SignalName.Pressed);
            if (screen.TacticalPoints != 0 || command.Disabled)
                throw new InvalidOperationException("zero-point command became unclickable before it could report failure");
            var goldAtZero = screen.RemainingGold;
            var summonsAtZero = screen.TemporaryUnitCount;
            command.EmitSignal(BaseButton.SignalName.Pressed);
            if (screen.TacticalPoints != 0 || screen.RemainingGold != goldAtZero || screen.TemporaryUnitCount != summonsAtZero ||
                !screen.CommandFeedback.Contains("战术点不足", StringComparison.Ordinal) || !status.FeedbackText.Contains("战术点不足", StringComparison.Ordinal))
                throw new InvalidOperationException("zero-point attempt swallowed feedback or changed command resources");
        }
        finally
        {
            merchantRoot.Free();
            enemyRoot.Free();
            RemoveChild(screen);
            screen.Free();
        }
    }

    private static BattleConfig MerchantConfig(
        ContentRegistry content,
        UnitSnapshot hero,
        UnitSnapshot enemy,
        HeroRuleSnapshot rule,
        UnitSnapshot summon,
        int gold)
    {
        var commands = ImmutableArray.Create(
            content.Graph.ResolveTacticalCommand("tactical_paid_reinforcement"),
            content.Graph.ResolveTacticalCommand("tactical_rally"));
        return new BattleConfig
        {
            Seed = 91,
            FloorRule = new ClearFloorRuleRuntime("ui-merchant", "常规", "测试"),
            HeroRule = rule,
            StartingGold = gold,
            TacticalCommands = new TacticalCommandBattlePreparation(
                TacticalCommandBattlePreparationBuilder.Fingerprint(commands), commands),
            TacticalSummons = ImmutableDictionary<string, UnitSnapshot>.Empty
                .Add("soldier_aegis_guard", summon),
            Spawns =
            [
                new BattleSpawn(hero, 0, new Vector2I(0, 2), "merchant"),
                new BattleSpawn(enemy, 1, new Vector2I(9, 2), "enemy")
            ]
        };
    }

    private static void InvokePrivate(object target, string name, params object?[] arguments)
    {
        var method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length == arguments.Length)
            ?? throw new InvalidOperationException("private method unavailable: " + name);
        method.Invoke(target, arguments);
    }

    private static void SetPrivate(object target, string name, object? value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("private field unavailable: " + name);
        field.SetValue(target, value);
    }
}
