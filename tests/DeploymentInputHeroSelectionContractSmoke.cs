using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class DeploymentInputHeroSelectionContractSmoke : Node
{
    private const string SaveNamespace = "tests/deployment-input-hero-correction";

    public override async void _Ready()
    {
        var exitCode = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private async Task<int> RunAsync()
    {
        var failures = new List<string>();
        var persisted = new SaveService(SaveNamespace);
        persisted.DeleteActiveRun();
        var root = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
        root.SaveNamespace = SaveNamespace;
        AddChild(root);

        try
        {
            for (var frame = 0; frame < 120 && root.Content is null; frame++) await ProcessFrames(1);
            var registry = root.Content ?? throw new InvalidOperationException("GameRoot content gate did not finish");
            var save = new CountingPersistedSaveService(persisted);
            var app = new RunApplication(registry, save, TestProjectFixture.Load(registry));
            SetPrivateField(root, "_app", app);

            var heroId = app.Meta.UnlockedHeroIds.FirstOrDefault()
                ?? throw new InvalidOperationException("no unlocked hero for isolated fixture");
            if (!app.StartNewRun(heroId, 0xD310UL))
                throw new InvalidOperationException("isolated fixture run did not start");
            var run = app.ActiveRun!;
            var recruit = registry.Catalog.Soldiers.First(entry => run.Roster.All(unit => unit.ContentId != entry.StableId));
            if (!app.Recruit(recruit.StableId))
                throw new InvalidOperationException("isolated reserve fixture recruit failed");
            var reserveId = run.Roster.Last().InstanceId;
            if (!app.GrantPopulation(1)) throw new InvalidOperationException("isolated population grant failed");
            if (run.Roster.Count != 5 || run.Deployment.Count(id => !string.IsNullOrEmpty(id)) != 4 ||
                run.Deployment.Contains(reserveId))
                throw new InvalidOperationException("fixture is not five roster heroes / four deployed / one reserve");

            run.SelectedNode = TowerNodeType.Combat;
            run.PendingNode = true;
            var encounter = app.CurrentEncounter();
            root.Flow.SetEncounterForTesting(encounter);
            root.Flow.ShowDeployment();
            await ProcessFrames(3);

            await VerifyFormationEvaluation(app, reserveId, failures);
            await VerifyEnemyPortraits(root, registry, failures);
            await VerifyReserveClickAndDrag(root, app, save, persisted, reserveId, failures);
            await VerifyTenAndEighteenDeploymentInput(root, registry, app, save, failures);
            await VerifyHeroSelection(root, app, failures);
        }
        catch (Exception exception)
        {
            failures.Add(exception.ToString());
        }
        finally
        {
            if (root.GetParent() is not null) root.GetParent().RemoveChild(root);
            root.Free();
            persisted.DeleteActiveRun();
            await ProcessFrames(2);
        }

        if (failures.Count > 0)
        {
            GD.PrintErr("DEPLOYMENT_INPUT_HERO_SELECTION_CONTRACT_FAILED: " + string.Join(" | ", failures));
            return 1;
        }

        GD.Print("DEPLOYMENT_INPUT_HERO_SELECTION_CONTRACT_OK formation=evaluate+commit save=isolated-once input=reserve-click+drag+10+18 drag-hover=single+cleanup enemies=animated-portraits readability=board+army-scroll+redundant-facts hero=click-accept-only");
        return 0;
    }

    private static Task VerifyFormationEvaluation(RunApplication app, string reserveId, List<string> failures)
    {
        var method = typeof(RunApplication).GetMethod("EvaluateFormationCommand", BindingFlags.Instance | BindingFlags.Public);
        if (method is null)
        {
            failures.Add("RunApplication has no shared non-mutating formation evaluation boundary");
            return Task.CompletedTask;
        }

        var run = app.ActiveRun!;
        var floorRule = app.BuildBattleConfig(app.CurrentEncounter(), false).FloorRule;
        var occupied = run.Deployment.Where(id => !string.IsNullOrEmpty(id))
            .Select(id => BattlefieldLayout.PlayerDeploymentCells[run.Deployment.IndexOf(id)]).ToHashSet();
        var empty = CandidateCells().First(cell => floorRule.CanOccupy(cell) && !occupied.Contains(cell));
        var valid = method.Invoke(app, [FormationMoveCommand.RosterHero(reserveId, empty), floorRule]);
        if (!EvaluationIsValid(valid) || string.IsNullOrWhiteSpace(EvaluationOperation(valid)))
            failures.Add($"shared formation evaluation did not accept reserve-to-empty with an operation kind " +
                         $"(valid={EvaluationIsValid(valid)}, operation={EvaluationOperation(valid)}, reason={EvaluationReason(valid)})");

        var invalid = method.Invoke(app, [FormationMoveCommand.RosterHero(reserveId, new Vector2I(3, 0)), floorRule]);
        if (EvaluationIsValid(invalid) || string.IsNullOrWhiteSpace(EvaluationReason(invalid)))
            failures.Add("shared formation evaluation did not reject an out-of-zone destination with a concise reason");
        return Task.CompletedTask;
    }

    private async Task VerifyEnemyPortraits(GameRoot root, ContentRegistry registry, List<string> failures)
    {
        var board = root.GetNode<DeploymentScreenController>("Screens/DeploymentScreen")
            .GetNode<DeploymentBoard>("%DeploymentBoard");
        var app = GetApplication(root);
        var enemySpawns = app.BuildBattleConfig(app.CurrentEncounter(), false).Spawns.Where(spawn => spawn.Team == 1).ToArray();
        var previews = board.GetChildren().OfType<Control>()
            .Where(child => child.GetType().Name == "EnemyDeploymentPreview").ToArray();
        if (previews.Length != enemySpawns.Length)
        {
            failures.Add($"deployment enemy preview remains icon-only: expected {enemySpawns.Length} animated previews, found {previews.Length}");
            return;
        }

        foreach (var spawn in enemySpawns)
        {
            var preview = previews.FirstOrDefault(node => ReadStringProperty(node, "InstanceId") == spawn.InstanceId);
            if (preview is null)
            {
                failures.Add("enemy preview missing stable runtime identity " + spawn.InstanceId);
                continue;
            }
            var portrait = preview.GetNodeOrNull<UnitPortrait>("%EnemyPortrait");
            var expected = ((UnitDefinition)registry.Catalog.Enemies.Single(entry => entry.StableId == spawn.Unit.ContentId).Definition).Portrait;
            if (portrait?.Definition != expected)
                failures.Add("enemy preview did not bind the composition-resolved portrait for " + spawn.Unit.ContentId);
            var contextMirror = ReadBoolProperty(portrait, "ContextMirrorHorizontal");
            if (contextMirror is not true)
                failures.Add("enemy preview did not author its deployment-only portrait mirror for " + spawn.Unit.ContentId);
            var sprite = portrait?.GetNodeOrNull<AnimatedSprite2D>("%PortraitSprite");
            if (expected is not null && sprite?.FlipH != (expected.FlipHorizontal ^ true))
                failures.Add("enemy preview did not invert the definition flip for " + spawn.Unit.ContentId);
            if (portrait?.IsPortraitPlaying != true)
                failures.Add("enemy preview did not start independent idle playback for " + spawn.Unit.ContentId);
            if (preview.Scale.X < 0 || portrait?.Scale.X < 0 ||
                preview.GetNode<TextureRect>("%EnemyBadge").Scale.X < 0 ||
                preview.GetNode<TextureRect>("%RoleBadge").Scale.X < 0 ||
                preview.GetNode<TextureRect>("%ReachBadge").Scale.X < 0)
                failures.Add("enemy deployment mirror escaped the portrait image leaves for " + spawn.Unit.ContentId);
        }
        await VerifyPortraitMirrorComposition(failures);
        await ProcessFrames(2);
    }

    private async Task VerifyPortraitMirrorComposition(List<string> failures)
    {
        var mirrorProperty = typeof(UnitPortrait).GetProperty("ContextMirrorHorizontal", BindingFlags.Instance | BindingFlags.Public);
        if (mirrorProperty is null || mirrorProperty.PropertyType != typeof(bool) || !mirrorProperty.CanWrite)
        {
            failures.Add("UnitPortrait has no consumer-authored horizontal-mirror option");
            return;
        }

        var host = new Control { Size = new Vector2(96, 96) };
        var portrait = GD.Load<PackedScene>("res://scenes/ui/components/UnitPortrait.tscn").Instantiate<UnitPortrait>();
        host.AddChild(portrait);
        AddChild(host);
        await ProcessFrames(1);
        var fallbackTexture = SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Reach);
        var fallback = portrait.GetNode<TextureRect>("%PortraitFallback");
        try
        {
            mirrorProperty.SetValue(portrait, true);
            portrait.Bind(new UnitPortraitDefinition { FlipHorizontal = false }, fallbackTexture);
            if (!fallback.Visible || !fallback.FlipH)
                failures.Add("fallback portrait did not apply false XOR context mirror");

            portrait.Bind(new UnitPortraitDefinition { FlipHorizontal = true }, fallbackTexture);
            if (!fallback.Visible || fallback.FlipH)
                failures.Add("fallback portrait did not apply true XOR context mirror");

            mirrorProperty.SetValue(portrait, false);
            portrait.Bind(new UnitPortraitDefinition { FlipHorizontal = true }, fallbackTexture);
            if (!fallback.FlipH)
                failures.Add("default portrait consumer no longer preserves the authored definition flip");
        }
        finally
        {
            if (host.GetParent() is not null) host.GetParent().RemoveChild(host);
            host.Free();
        }
    }

    private async Task VerifyReserveClickAndDrag(
        GameRoot root,
        RunApplication app,
        CountingPersistedSaveService save,
        SaveService persisted,
        string reserveId,
        List<string> failures)
    {
        var deployment = root.GetNode<DeploymentScreenController>("Screens/DeploymentScreen");
        var board = deployment.GetNode<DeploymentBoard>("%DeploymentBoard");
        var roster = deployment.GetNode<VBoxContainer>("%RosterChoices");
        var reserveCard = roster.GetChildren().OfType<DeploymentUnitCard>().Single(card => card.InstanceId == reserveId);
        var cardObjectId = reserveCard.GetInstanceId();
        await Click(reserveCard);

        var target = board.GetChildren().OfType<DeploymentCell>()
            .First(cell => cell.IsLegalTarget && string.IsNullOrEmpty(cell.PieceId));
        var targetObjectId = target.GetInstanceId();
        var savesBefore = save.ActiveRunSaveCalls;
        await Click(target);
        var run = app.ActiveRun!;
        var slot = run.Deployment.IndexOf(reserveId);
        if (slot < 0 || BattlefieldLayout.PlayerDeploymentCells[slot] != target.Cell)
            failures.Add("real GameRoot reserve-to-empty click did not mutate application formation");
        if (save.ActiveRunSaveCalls != savesBefore + 1)
            failures.Add("reserve-to-empty click did not persist exactly once");
        var saved = persisted.LoadActiveRun();
        var savedSlot = saved?.Deployment.IndexOf(reserveId) ?? -1;
        if (savedSlot < 0 || BattlefieldLayout.PlayerDeploymentCells[savedSlot] != target.Cell)
            failures.Add("reserve-to-empty click did not persist the expected identity/cell");
        var reboundCard = roster.GetChildren().OfType<DeploymentUnitCard>().Single(card => card.InstanceId == reserveId);
        var reboundTarget = board.GetChildren().OfType<DeploymentCell>().Single(cell => cell.Cell == target.Cell);
        if (reboundCard.GetInstanceId() != cardObjectId || reboundTarget.GetInstanceId() != targetObjectId ||
            reboundTarget.PieceId != reserveId)
            failures.Add("reserve-to-empty click did not safely rebind the same stable controls");
        if (!deployment.GetNode<Label>("%Status").Text.Contains("更新", StringComparison.Ordinal))
            failures.Add("valid reserve-to-empty click did not show success feedback");

        savesBefore = save.ActiveRunSaveCalls;
        await Drag(reboundCard, reboundTarget);
        if (save.ActiveRunSaveCalls != savesBefore ||
            !deployment.GetNode<Label>("%Status").Text.Contains("已在此格", StringComparison.Ordinal))
            failures.Add("same-cell drag did not expose the authoritative rejection without saving");

        if (!app.WithdrawDeploymentUnit(reserveId))
            throw new InvalidOperationException("drag fixture withdrawal failed");
        root.Flow.ShowDeployment();
        await ProcessFrames(2);
        reserveCard = roster.GetChildren().OfType<DeploymentUnitCard>().Single(card => card.InstanceId == reserveId);
        var dragTarget = board.GetChildren().OfType<DeploymentCell>()
            .First(cell => cell.IsLegalTarget && string.IsNullOrEmpty(cell.PieceId) && cell.Cell != target.Cell);
        savesBefore = save.ActiveRunSaveCalls;
        await Drag(reserveCard, dragTarget);
        slot = run.Deployment.IndexOf(reserveId);
        if (slot < 0 || BattlefieldLayout.PlayerDeploymentCells[slot] != dragTarget.Cell)
            failures.Add("real GameRoot reserve-to-empty drag did not mutate application formation");
        if (save.ActiveRunSaveCalls != savesBefore + 1)
            failures.Add("reserve-to-empty drag did not persist exactly once");
        if (board.GetChildren().OfType<DeploymentCell>().Single(cell => cell.Cell == dragTarget.Cell).PieceId != reserveId)
            failures.Add("reserve-to-empty drag did not rebind the destination cell");

        if (!app.WithdrawDeploymentUnit(reserveId))
            throw new InvalidOperationException("drag-trail fixture withdrawal failed");
        root.Flow.ShowDeployment();
        await ProcessFrames(2);
        reserveCard = roster.GetChildren().OfType<DeploymentUnitCard>().Single(card => card.InstanceId == reserveId);
        var hoverTargets = board.GetChildren().OfType<DeploymentCell>()
            .Where(cell => cell.IsLegalTarget && string.IsNullOrEmpty(cell.PieceId)).Take(3).ToArray();
        savesBefore = save.ActiveRunSaveCalls;
        await BeginDragAcross(reserveCard, hoverTargets);
        var hovered = board.GetChildren().OfType<DeploymentCell>().Count(IsDragHovered);
        if (hovered != 1)
            failures.Add($"drag traversal left {hovered} active hover cells instead of one");
        var currentHover = ReadNullableCellProperty(board, "CurrentDragHoverCell");
        if (currentHover is null || currentHover.Value != hoverTargets[^1].Cell)
            failures.Add("board does not own the latest drag-hover destination");
        await CancelDragOutside();
        if (board.GetChildren().OfType<DeploymentCell>().Any(IsDragHovered) ||
            ReadNullableCellProperty(board, "CurrentDragHoverCell") is not null)
            failures.Add("cancelled/outside drag did not clear every transient hover state");
        if (save.ActiveRunSaveCalls != savesBefore || run.Deployment.Contains(reserveId))
            failures.Add("cancelled drag mutated or saved formation state");
    }

    private async Task VerifyHeroSelection(GameRoot root, RunApplication app, List<string> failures)
    {
        root.Flow.ShowHeroSelection();
        await ProcessFrames(3);
        var screen = root.GetNode<HeroSelectScreen>("Screens/HeroSelectScreen");
        var tiles = screen.GetNode<GridContainer>("%HeroLibrary").GetChildren().OfType<HeroLibraryTile>().ToArray();
        if (tiles.Length < 3) throw new InvalidOperationException("hero library fixture needs three tiles");
        var initial = screen.PreviewStableId;
        var hoverTile = tiles.First(tile => tile.StableId != initial);
        await MovePointer(hoverTile);
        if (screen.PreviewStableId != initial)
            failures.Add("mouse hover changed the explicitly selected hero");

        screen.Preview(initial);
        var focusTile = tiles.First(tile => tile.StableId != initial && tile.StableId != hoverTile.StableId);
        focusTile.GrabFocus();
        await ProcessFrames(2);
        if (screen.PreviewStableId != initial)
            failures.Add("focus movement changed the explicitly selected hero");

        var chosenCount = 0;
        var chosenId = string.Empty;
        screen.HeroChosen += id => { chosenCount++; chosenId = id; };
        await ActivateFocused(focusTile);
        if (screen.PreviewStableId != focusTile.StableId)
            failures.Add("ui_accept did not select the focused hero tile");
        if (chosenCount != 0)
            failures.Add("tile activation started a run instead of only selecting the hero");

        var selectedBeforeRun = focusTile.StableId;
        await Click(screen.GetNode<HeroDetailPanel>("%HeroDetailPanel").GetNode<Button>("%DeployButton"));
        if (chosenCount != 1 || chosenId != selectedBeforeRun ||
            app.ActiveRun?.Roster.FirstOrDefault()?.ContentId != selectedBeforeRun)
            failures.Add("detail primary action did not start exactly one run for the explicit selected hero");
    }

    private async Task VerifyTenAndEighteenDeploymentInput(
        GameRoot root,
        ContentRegistry registry,
        RunApplication app,
        CountingPersistedSaveService save,
        List<string> failures)
    {
        var run = app.ActiveRun ?? throw new InvalidOperationException("population input fixture has no active Run");
        var ordinaryGrant = 10 - run.CurrentPopulation;
        if (ordinaryGrant > 0 && !app.GrantPopulation(ordinaryGrant))
            throw new InvalidOperationException("ordinary population input fixture grant failed");
        RecruitUntil(registry, app, 10);
        await DeployReserveThroughViewport(root, registry, app, save, 10, failures);
        await VerifyDeploymentReadability(root, registry, app, 10, failures);

        if (!app.GrantPopulationFromSource("phase5_physical_input", 8, 8))
            throw new InvalidOperationException("physical population input fixture grant failed");
        RecruitUntil(registry, app, 18);
        await DeployReserveThroughViewport(root, registry, app, save, 18, failures);
        await VerifyDeploymentReadability(root, registry, app, 18, failures);
    }

    private static void RecruitUntil(ContentRegistry registry, RunApplication app, int target)
    {
        var run = app.ActiveRun ?? throw new InvalidOperationException("population recruit fixture has no active Run");
        while (run.Roster.Count < target)
        {
            var contentId = registry.Catalog.Soldiers[run.Roster.Count % registry.Catalog.Soldiers.Count].StableId;
            if (!app.Recruit(contentId))
                throw new InvalidOperationException($"population recruit fixture stopped at {run.Roster.Count}/{target}");
        }
    }

    private async Task DeployReserveThroughViewport(
        GameRoot root,
        ContentRegistry registry,
        RunApplication app,
        CountingPersistedSaveService save,
        int target,
        List<string> failures)
    {
        var run = app.ActiveRun ?? throw new InvalidOperationException("population deployment fixture has no active Run");
        while (run.Deployment.Count(id => !string.IsNullOrEmpty(id)) < target)
        {
            BindClearDeployment(root, registry, app);
            await ProcessFrames(2);
            var deployment = root.GetNode<DeploymentScreenController>("Screens/DeploymentScreen");
            var board = deployment.GetNode<DeploymentBoard>("%DeploymentBoard");
            var rosterScroll = deployment.GetNode<ScrollContainer>("Margin/Layout/Columns/RosterPanel/RosterScroll");
            var roster = deployment.GetNode<VBoxContainer>("%RosterChoices");
            var reserve = run.Roster.FirstOrDefault(hero => !run.Deployment.Contains(hero.InstanceId)) ??
                          throw new InvalidOperationException($"no reserve remained before viewport deployment {target}");
            var card = roster.GetChildren().OfType<DeploymentUnitCard>()
                .Single(candidate => candidate.InstanceId == reserve.InstanceId);
            rosterScroll.EnsureControlVisible(card);
            await ProcessFrames(2);
            if (!VisibleWithin(card, rosterScroll))
                failures.Add($"{target}-unit reserve card was not reachable inside its authored scroll owner");
            if (deployment.SelectedPieceId != reserve.InstanceId)
                await ClickWithin(card, rosterScroll);
            if (deployment.SelectedPieceId != reserve.InstanceId)
                throw new InvalidOperationException(
                    $"real viewport click did not select reserve hero toward {target}; " +
                    $"card={card.GetGlobalRect()} scroll={rosterScroll.GetGlobalRect()}");
            var targetCell = board.GetChildren().OfType<DeploymentCell>()
                .First(cell => string.IsNullOrEmpty(cell.PieceId) && cell.IsLegalTarget);
            var savesBefore = save.ActiveRunSaveCalls;
            await Click(targetCell);
            if (!run.Deployment.Contains(reserve.InstanceId))
                failures.Add($"real viewport input did not deploy reserve hero toward {target}");
            if (save.ActiveRunSaveCalls != savesBefore + 1)
                failures.Add($"real viewport deployment toward {target} did not persist exactly once");
        }
    }

    private async Task VerifyDeploymentReadability(
        GameRoot root,
        ContentRegistry registry,
        RunApplication app,
        int expected,
        List<string> failures)
    {
        BindClearDeployment(root, registry, app);
        await ProcessFrames(3);
        var run = app.ActiveRun!;
        var deployment = root.GetNode<DeploymentScreenController>("Screens/DeploymentScreen");
        var board = deployment.GetNode<DeploymentBoard>("%DeploymentBoard");
        var cells = board.GetChildren().OfType<DeploymentCell>().OrderBy(cell => cell.Cell.Y)
            .ThenBy(cell => cell.Cell.X).ToArray();
        var occupied = cells.Where(cell => !string.IsNullOrEmpty(cell.PieceId)).ToArray();
        var viewport = new Rect2(Vector2.Zero, GetViewport().GetVisibleRect().Size);
        var boardRect = board.GetGlobalRect();
        if (run.Deployment.Count(id => !string.IsNullOrEmpty(id)) != expected || occupied.Length != expected ||
            run.Deployment.Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal).Count() != expected)
            failures.Add($"{expected}-unit viewport formation did not retain unique physical occupancy");
        if (cells.Length != 18 || board.CandidateCellCount != 18 ||
            cells.Any(cell => !BattlefieldLayout.IsPlayerDeploymentCell(cell.Cell)))
            failures.Add($"{expected}-unit viewport did not expose exactly all 18 legal candidate cells");
        if (!Contains(viewport, boardRect) || !board.CurrentProjection.IsValid ||
            cells.Any(cell => !Contains(boardRect, cell.GetGlobalRect()) || !Contains(viewport, cell.GetGlobalRect())))
            failures.Add($"{expected}-unit board or candidate cells clipped outside the authored viewport region");
        if (GetViewport().GetVisibleRect().Size.X >= 1600 &&
            (board.CurrentProjection.CellPitch.X < 100 || board.CurrentProjection.CellPitch.Y < 74))
            failures.Add($"{expected}-unit desktop board pitch became unreadably small: {board.CurrentProjection.CellPitch}");
        if (occupied.Any(cell =>
                !cell.GetNode<UnitPortrait>("%UnitPortrait").Visible ||
                !cell.GetNode<TextureRect>("%HeroBadge").Visible ||
                !cell.GetNode<TextureRect>("%RoleBadge").Visible ||
                !cell.GetNode<TextureRect>("%ReachBadge").Visible ||
                string.IsNullOrWhiteSpace(cell.TooltipText) ||
                (!cell.TooltipText.Contains("点击选择", StringComparison.Ordinal) &&
                 !cell.TooltipText.Contains("已在此格", StringComparison.Ordinal))))
            failures.Add($"{expected}-unit occupied cells lost portrait/icon/text redundancy");

        var selectedCell = occupied.First(cell => cell.PieceId != deployment.SelectedPieceId);
        await Click(selectedCell);
        if (deployment.SelectedPieceId != selectedCell.PieceId)
            failures.Add($"real viewport click did not select an occupied cell at density {expected}");
        var rosterScroll = deployment.GetNode<ScrollContainer>("Margin/Layout/Columns/RosterPanel/RosterScroll");
        var selectedCard = deployment.GetNode<VBoxContainer>("%RosterChoices").GetChildren()
            .OfType<DeploymentUnitCard>().Single(card => card.InstanceId == selectedCell.PieceId);
        rosterScroll.EnsureControlVisible(selectedCard);
        await ProcessFrames(2);
        if (!VisibleWithin(selectedCard, rosterScroll) ||
            selectedCard.ThemeTypeVariation != "SelectedButton" ||
            string.IsNullOrWhiteSpace(selectedCard.GetNode<Label>("%UnitName").Text) ||
            string.IsNullOrWhiteSpace(selectedCard.GetNode<Label>("%UnitState").Text))
            failures.Add($"selected hero facts were not readable through the authored roster scroll at density {expected}");
        if (!Contains(viewport, deployment.GetNode<Button>("%StartBattleButton").GetGlobalRect()) ||
            !Contains(viewport, deployment.GetNode<Label>("%Status").GetGlobalRect()))
            failures.Add($"critical deployment action/status surfaces clipped at density {expected}");

        var army = root.GetNode<ArmyOverviewController>("ArmyOverview");
        await Click(army.GetNode<Button>("%SummaryButton"));
        var drawer = army.GetNode<PanelContainer>("%Drawer");
        var armyScroll = army.GetNode<ScrollContainer>("Drawer/Layout/Scroll");
        var rows = army.GetNode<VBoxContainer>("%Rows").GetChildren().OfType<ArmyDrawerRow>().ToArray();
        if (!army.IsOpen || !drawer.Visible || !Contains(viewport, drawer.GetGlobalRect()) ||
            !Contains(drawer.GetGlobalRect(), armyScroll.GetGlobalRect()))
            failures.Add($"real viewport input did not open an unclipped Army detail drawer at density {expected}");
        if (rows.Length < expected || rows.Take(expected).Any(row =>
                string.IsNullOrWhiteSpace(row.GetNode<Label>("%RowTitle").Text) ||
                string.IsNullOrWhiteSpace(row.GetNode<Label>("%RowDetails").Text) ||
                !row.GetNode<HFlowContainer>("%RowFacts").Visible))
            failures.Add($"Army detail drawer lost roster identity/facts at density {expected}");
        if (expected == 18 && armyScroll.GetVScrollBar().MaxValue <= armyScroll.GetVScrollBar().Page)
            failures.Add("18-unit Army detail overflow did not remain reachable through the authored scroll owner");
        await Click(army.GetNode<Button>("%CloseButton"));
        if (army.IsOpen) failures.Add($"real viewport input did not close Army details at density {expected}");
    }

    private static void BindClearDeployment(GameRoot root, ContentRegistry registry, RunApplication app)
    {
        var encounter = app.CurrentEncounter();
        var source = app.BuildBattleConfig(encounter, false);
        var floor = new ClearFloorRuleRuntime("phase5_density_clear", "密度验收", "全部十八格可部署");
        var config = new BattleConfig
        {
            Seed = source.Seed,
            Identity = source.Identity,
            FloorRule = floor,
            Spawns = source.Spawns,
            HeroRule = source.HeroRule
        };
        var run = app.ActiveRun!;
        var pieces = run.Roster.Select(instance =>
        {
            var definition = (UnitDefinition)registry.Catalog.AllEntries()
                .Single(entry => entry.StableId == instance.ContentId).Definition;
            var slot = run.Deployment.IndexOf(instance.InstanceId);
            var evaluations = CandidateCells().ToDictionary(
                cell => cell,
                cell => app.EvaluateFormationCommand(FormationMoveCommand.RosterHero(instance.InstanceId, cell), floor));
            return new DeploymentUnitViewModel(
                instance.InstanceId,
                definition.DisplayName,
                definition.Description,
                instance.HealthRatio,
                definition.Role,
                definition.AttackRange,
                true,
                slot,
                slot >= 0 ? BattlefieldLayout.PlayerDeploymentCells[slot] : null,
                definition.Portrait,
                evaluations);
        }).ToArray();
        var enemies = source.Spawns.Where(spawn => spawn.Team == 1).Select(spawn =>
        {
            var definition = (UnitDefinition)registry.Catalog.AllEntries()
                .Single(entry => entry.StableId == spawn.Unit.ContentId).Definition;
            return new EnemyDeploymentViewModel(spawn.InstanceId, definition.DisplayName, spawn.Cell,
                definition.Role, definition.AttackRange, spawn.Unit.IsBoss, definition.Portrait);
        }).ToArray();
        root.Flow.ShowDeployment();
        root.GetNode<DeploymentScreenController>("Screens/DeploymentScreen")
            .Bind("战前部署", $"密度验收：{run.Deployment.Count(id => !string.IsNullOrEmpty(id))}/18", config,
                pieces, enemies, app.Rules.ReserveCapacity);
    }

    private static bool Contains(Rect2 outer, Rect2 inner) =>
        inner.Position.X >= outer.Position.X - .5f && inner.Position.Y >= outer.Position.Y - .5f &&
        inner.End.X <= outer.End.X + .5f && inner.End.Y <= outer.End.Y + .5f;

    private static bool VisibleWithin(Control control, Control clipOwner)
    {
        var visible = control.GetGlobalRect().Intersection(clipOwner.GetGlobalRect());
        return visible.Size.X >= Math.Min(control.Size.X, clipOwner.Size.X) - 1 && visible.Size.Y >= 24;
    }

    private async Task Click(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = true, Position = point, GlobalPosition = point
        }, true);
        await ProcessFrames(1);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = false, Position = point, GlobalPosition = point
        }, true);
        await ProcessFrames(2);
    }

    private async Task ClickWithin(Control control, Control clipOwner)
    {
        var visible = control.GetGlobalRect().Intersection(clipOwner.GetGlobalRect());
        if (visible.Size.X <= 1 || visible.Size.Y <= 1)
            throw new InvalidOperationException($"control has no clickable visible rect: {control.Name}");
        var point = visible.GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = true, Position = point, GlobalPosition = point
        }, true);
        await ProcessFrames(1);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = false, Position = point, GlobalPosition = point
        }, true);
        await ProcessFrames(2);
    }

    private async Task MovePointer(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        await ProcessFrames(2);
    }

    private async Task Drag(Control source, Control target)
    {
        await BeginDragAcross(source, [target]);
        var point = target.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = false, Position = point, GlobalPosition = point
        }, true);
        await ProcessFrames(3);
    }

    private async Task BeginDragAcross(Control source, IReadOnlyList<Control> targets)
    {
        var start = source.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = start, GlobalPosition = start }, true);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = true, Position = start, GlobalPosition = start
        }, true);
        await ProcessFrames(1);
        var previous = start;
        foreach (var target in targets)
        {
            var end = target.GetGlobalRect().GetCenter();
            for (var step = 1; step <= 3; step++)
            {
                var point = previous.Lerp(end, step / 3f);
                GetViewport().PushInput(new InputEventMouseMotion
                {
                    Position = point, GlobalPosition = point, Relative = (end - previous) / 3f,
                    ButtonMask = MouseButtonMask.Left
                }, true);
                await ProcessFrames(1);
            }
            previous = end;
        }
    }

    private async Task CancelDragOutside()
    {
        var point = new Vector2(2, 2);
        GetViewport().PushInput(new InputEventMouseMotion
        {
            Position = point, GlobalPosition = point, Relative = new Vector2(-200, -200), ButtonMask = MouseButtonMask.Left
        }, true);
        await ProcessFrames(1);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = false, Position = point, GlobalPosition = point
        }, true);
        await ProcessFrames(3);
    }

    private async Task ActivateFocused(BaseButton button)
    {
        button.GrabFocus();
        await ProcessFrames(1);
        GetViewport().PushInput(new InputEventAction { Action = "ui_accept", Pressed = true, Strength = 1f }, true);
        await ProcessFrames(1);
        GetViewport().PushInput(new InputEventAction { Action = "ui_accept", Pressed = false }, true);
        await ProcessFrames(2);
    }

    private async Task ProcessFrames(int count)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static bool IsDragHovered(DeploymentCell cell)
    {
        var property = typeof(DeploymentCell).GetProperty("IsDragHovered", BindingFlags.Instance | BindingFlags.Public);
        if (property?.GetValue(cell) is bool value) return value;
        var field = typeof(DeploymentCell).GetField("_dropHover", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(cell) is true;
    }

    private static Vector2I? ReadNullableCellProperty(object target, string name)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(target) switch
        {
            Vector2I cell => cell,
            _ => null
        };
    }

    private static bool EvaluationIsValid(object? evaluation) =>
        evaluation?.GetType().GetProperty("IsValid")?.GetValue(evaluation) is true;

    private static string EvaluationOperation(object? evaluation) =>
        evaluation?.GetType().GetProperty("Operation")?.GetValue(evaluation)?.ToString() ?? string.Empty;

    private static string EvaluationReason(object? evaluation) =>
        evaluation?.GetType().GetProperty("RejectionReason")?.GetValue(evaluation)?.ToString() ?? string.Empty;

    private static string ReadStringProperty(object target, string name) =>
        target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target)?.ToString() ?? string.Empty;

    private static bool? ReadBoolProperty(object? target, string name) =>
        target?.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target) as bool?;

    private static IEnumerable<Vector2I> CandidateCells()
    {
        for (var y = 0; y < BattlefieldLayout.Height; y++)
        for (var x = 0; x < BattlefieldLayout.PlayerDeploymentColumns; x++)
            yield return new Vector2I(x, y);
    }

    private static RunApplication GetApplication(GameRoot root)
    {
        var field = typeof(GameRoot).GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(root) as RunApplication
            ?? throw new InvalidOperationException("GameRoot application unavailable");
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(target.GetType().FullName, name);
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, methodName);
        method.Invoke(target, null);
    }

    private static IReadOnlyList<TowerRegionDefinition> Regions() =>
    [
        GD.Load<TowerRegionDefinition>("res://content/tower/region_ember_foundry.tres"),
        GD.Load<TowerRegionDefinition>("res://content/tower/region_gloam_crypt.tres"),
        GD.Load<TowerRegionDefinition>("res://content/tower/region_crown_engine.tres")
    ];

    private sealed class CountingPersistedSaveService(SaveService inner) : IRunSaveService
    {
        public int ActiveRunSaveCalls { get; private set; }
        public MetaProgressDto LoadMeta() => inner.LoadMeta();
        public SettingsDto LoadSettings() => inner.LoadSettings();
        public ActiveRunDto? LoadActiveRun() => inner.LoadActiveRun();
        public bool SaveMeta(MetaProgressDto value) => inner.SaveMeta(value);
        public bool SaveSettings(SettingsDto value) => inner.SaveSettings(value);
        public bool SaveActiveRun(ActiveRunDto value)
        {
            ActiveRunSaveCalls++;
            return inner.SaveActiveRun(value);
        }
        public void DeleteActiveRun() => inner.DeleteActiveRun();
    }
}
