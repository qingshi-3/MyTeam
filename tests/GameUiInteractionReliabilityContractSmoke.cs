using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class GameUiInteractionReliabilityContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private async Task<int> RunAsync()
    {
        var failures = new List<string>();
        try
        {
            var catalog = GD.Load<ContentCatalog>("res://content/catalogs/alpha_catalog.tres")
                ?? throw new InvalidOperationException("catalog load failed");
            var gate = await TestProjectFixture.PublishAsync(this);
            var registry = gate.Package?.Content
                ?? throw new InvalidOperationException("content gate failed: " + string.Join("; ", gate.Report.CoreErrors));
            await VerifyDeploymentRealInput(registry, failures);
            await VerifyBattleSettlementRetryRealInput(registry, failures);
            await VerifyShopRealInput(failures);
            VerifyDeploymentCellCopy(failures);
        }
        catch (Exception exception)
        {
            failures.Add(exception.GetType().Name + ": " + exception.Message);
        }

        if (failures.Count > 0)
        {
            GD.PrintErr("GAME_UI_INTERACTION_RELIABILITY_CONTRACT_FAILED: " + string.Join(" | ", failures));
            return 1;
        }

        GD.Print("GAME_UI_INTERACTION_RELIABILITY_CONTRACT_OK input=viewport mouse+focus route=chosen recruitment=chosen reward=chosen roster=reused board=selected drag=typed settlement=retry-real-input shop=reused empty-cell-copy=none engine-errors=none");
        return 0;
    }

    private async Task VerifyDeploymentRealInput(ContentRegistry registry, List<string> failures)
    {
        var save = new MemoryRunSaveService(registry);
            var app = new RunApplication(registry, save, TestProjectFixture.Load(registry));
        if (!app.StartNewRun("hero_banner_marshal", 7171))
            throw new InvalidOperationException("deployment fixture run failed to start");
        var run = app.ActiveRun!;
        run.SelectedNode = TowerNodeType.Combat;
        run.PendingNode = true;
        var encounter = app.CurrentEncounter();
        var config = app.BuildBattleConfig(encounter, false);
        var pieces = BuildPieces(registry, run);

        var host = new Control { Size = new Vector2(1280, 720) };
        AddChild(host);
        var deployment = GD.Load<PackedScene>("res://scenes/ui/DeploymentScreen.tscn")
            .Instantiate<DeploymentScreenController>();
        host.AddChild(deployment);
        deployment.Bind("真实输入契约", "敌军与楼层规则", config, pieces, null, app.Rules.ReserveCapacity);
        await ProcessFrames(2);

        try
        {
            var roster = deployment.GetNode<VBoxContainer>("%RosterChoices");
            var card = roster.GetChildren().OfType<DeploymentUnitCard>().FirstOrDefault()
                ?? throw new InvalidOperationException("deployment roster has no authored card");
            var selectedId = card.InstanceId;
            var cardObjectId = card.GetInstanceId();
            var logger = new InteractionErrorLogger();
            OS.AddLogger(logger);
            try
            {
                await Click(card);
            }
            finally
            {
                OS.RemoveLogger(logger);
            }

            if (deployment.SelectedPieceId != selectedId)
                failures.Add("real roster mouse click did not select the bound unit");
            var rebound = roster.GetChildren().OfType<DeploymentUnitCard>()
                .SingleOrDefault(candidate => candidate.InstanceId == selectedId);
            if (rebound is null || rebound.GetInstanceId() != cardObjectId)
                failures.Add("state-only roster selection replaced the active event-source card instead of rebinding it");
            if (logger.Errors.Any(error =>
                    error.Contains("locked", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("free", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("disposed", StringComparison.OrdinalIgnoreCase)))
                failures.Add("real roster click logged an active-event lifetime error: " + string.Join(" || ", logger.Errors));
            logger.Dispose();

            FormationMoveCommand? requested = null;
            var requestCount = 0;
            deployment.MoveRequested += command =>
            {
                requested = command;
                requestCount++;
            };
            var target = deployment.GetNode<DeploymentBoard>("%DeploymentBoard").GetChildren()
                .OfType<DeploymentCell>()
                .FirstOrDefault(cell => cell.IsLegalTarget && string.IsNullOrEmpty(cell.PieceId));
            if (target is null)
            {
                failures.Add("deployment fixture has no empty legal target");
            }
            else
            {
                await Click(target);
                var matchesPiece = requested is
                    { PieceKind: FormationPieceKind.RosterHero } && requested.InstanceId == selectedId;
                if (requestCount != 1 || !matchesPiece || requested!.TargetCell != target.Cell)
                    failures.Add("real roster-then-cell mouse input did not emit exactly one typed move request");

                await Click(card);
                var occupied = deployment.GetNode<DeploymentBoard>("%DeploymentBoard").GetChildren()
                    .OfType<DeploymentCell>()
                    .FirstOrDefault(cell => !string.IsNullOrEmpty(cell.PieceId));
                if (occupied is null)
                    failures.Add("deployment fixture has no occupied board cell");
                else
                {
                    await Click(occupied);
                    if (deployment.SelectedPieceId != occupied.PieceId)
                        failures.Add("real board-unit mouse click did not select the occupied unit");
                }

                requested = null;
                requestCount = 0;
                await Drag(card, target);
                if (requestCount != 1 || requested is null || requested.TargetCell != target.Cell)
                    failures.Add("real reserve/roster drag did not emit exactly one typed drop request");
            }
        }
        finally
        {
            host.QueueFree();
            await ProcessFrames(2);
        }
    }

    private async Task VerifyShopRealInput(List<string> failures)
    {
        const string saveNamespace = "tests/ui-interaction";
        new SaveService(saveNamespace).DeleteActiveRun();
        var root = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
        root.SaveNamespace = saveNamespace;
        AddChild(root);

        try
        {
            for (var frame = 0; frame < 12 && root.Content is null; frame++)
                await ProcessFrames(1);
            if (root.Content is null)
                throw new InvalidOperationException("shop fixture GameRoot content gate did not finish");

            var app = GetApplication(root);
            var heroId = app.Meta.UnlockedHeroIds.FirstOrDefault()
                ?? throw new InvalidOperationException("shop fixture has no unlocked hero");
            if (!app.StartNewRun(heroId, 8181))
                throw new InvalidOperationException("shop fixture run failed to start");
            root.Flow.ShowTower();
            await ProcessFrames(2);
            var routeChoices = root.GetNode<Container>("Screens/TowerScreen/Margin/Layout/Choices");
            var routeCard = routeChoices.GetChildren().OfType<ChoiceCard>().FirstOrDefault()
                ?? throw new InvalidOperationException("run-flow fixture has no route card");
            var routeId = routeCard.StableId;
            await Click(routeCard);
            if (!app.ActiveRun!.PendingNode || app.ActiveRun.SelectedNode.ToString() != routeId)
                failures.Add("real route-card mouse click did not choose the bound node");

            var rosterCount = app.ActiveRun.Roster.Count;
            root.Flow.ShowRecruitment();
            await ProcessFrames(2);
            var recruitCard = root.GetNode<Container>("Screens/RecruitmentScreen/Center/Panel/Layout/ChoiceScroll/Choices")
                .GetChildren().OfType<UnitChoiceCard>().FirstOrDefault()
                ?? throw new InvalidOperationException("run-flow fixture has no recruitment card");
            await ActivateFocused(recruitCard);
            if (app.ActiveRun.Roster.Count != rosterCount + 1)
                failures.Add("focused ui_accept did not recruit the bound soldier");

            var rewardItemCount = app.ActiveRun.Items.Count;
            root.Flow.ShowCombatReward();
            await ProcessFrames(2);
            var rewardCard = root.GetNode<Container>("Screens/RewardScreen/Center/Panel/Layout/ChoiceScroll/Choices")
                .GetChildren().OfType<ChoiceCard>().FirstOrDefault()
                ?? throw new InvalidOperationException("run-flow fixture has no reward card");
            await Click(rewardCard);
            if (app.ActiveRun.Items.Count != rewardItemCount + 1)
                failures.Add("real reward-card mouse click did not grant the bound item");

            app.ActiveRun!.Gold = 999;
            root.Flow.ShowShop();
            await ProcessFrames(2);

            var choices = root.GetNode<Container>("Screens/ShopScreen/Margin/Layout/Choices");
            var card = choices.GetChildren().OfType<ChoiceCard>().FirstOrDefault()
                ?? throw new InvalidOperationException("shop fixture has no authored item card");
            var stableId = card.StableId;
            var cardObjectId = card.GetInstanceId();
            var itemCount = app.ActiveRun.Items.Count;
            var logger = new InteractionErrorLogger();
            OS.AddLogger(logger);
            try
            {
                await Click(card);
            }
            finally
            {
                OS.RemoveLogger(logger);
            }

            if (app.ActiveRun.Items.Count != itemCount + 1 || app.ActiveRun.Items[^1].ContentId != stableId)
                failures.Add("real shop mouse click did not purchase the bound item exactly once");
            var rebound = choices.GetChildren().OfType<ChoiceCard>()
                .SingleOrDefault(candidate => candidate.StableId == stableId);
            if (rebound is null || rebound.GetInstanceId() != cardObjectId || rebound.Disabled || !rebound.Visible)
                failures.Add("shop purchase refresh replaced or disabled the active event-source card");
            if (logger.Errors.Any(error =>
                    error.Contains("locked", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("free", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("disposed", StringComparison.OrdinalIgnoreCase)))
                failures.Add("real shop click logged an active-event lifetime error: " + string.Join(" || ", logger.Errors));
            logger.Dispose();
        }
        finally
        {
            if (root.GetParent() is not null) root.GetParent().RemoveChild(root);
            root.Free();
            new SaveService(saveNamespace).DeleteActiveRun();
            await ProcessFrames(2);
        }
    }

    private async Task VerifyBattleSettlementRetryRealInput(
        ContentRegistry registry,
        List<string> failures)
    {
        const string saveNamespace = "tests/ui-settlement-retry";
        new SaveService(saveNamespace).DeleteActiveRun();
        var root = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
        root.SaveNamespace = saveNamespace;
        AddChild(root);

        try
        {
            for (var frame = 0; frame < 12 && root.Content is null; frame++)
                await ProcessFrames(1);
            if (root.Content is null)
                throw new InvalidOperationException("settlement retry GameRoot content gate did not finish");

            var save = new FlakySettlementSaveService(registry);
            var app = new RunApplication(registry, save, TestProjectFixture.Load(registry));
            var heroId = app.Meta.UnlockedHeroIds.FirstOrDefault()
                ?? throw new InvalidOperationException("settlement retry fixture has no unlocked hero");
            if (!app.StartNewRun(heroId, 9191))
                throw new InvalidOperationException("settlement retry fixture run failed to start");
            var authoritativeRun = app.ActiveRun
                ?? throw new InvalidOperationException("settlement retry fixture has no active run");
            authoritativeRun.SelectedNode = TowerNodeType.Combat;
            authoritativeRun.PendingNode = true;
            var encounter = app.CurrentEncounter();
            var result = SyntheticResult(app.BuildBattleConfig(encounter, false), BattleOutcome.PlayerVictory);
            var before = RunFingerprint(authoritativeRun);
            var startingFloor = authoritativeRun.FloorIndex;

            SetApplication(root, app);
            root.Flow.SetEncounterForTesting(encounter);
            root.Flow.ResetPendingBattleFlow();
            root.Flow.Show(AppScreenId.Battle);
            save.BeginSettlementProbe();
            root.Flow.AcceptBattleResult(result);
            await ProcessFrames(2);

            var firstResolution = root.Flow.PendingBattleResolution;
            var resultScreen = root.GetNode<Control>("Screens/ResultScreen");
            var resultTitle = root.GetNode<Label>("Screens/ResultScreen/Center/Panel/Layout/Title");
            if (root.Flow.BattleResolutionCommitted ||
                firstResolution is not { Accepted: false, Failure: RunBattleResolutionFailure.PersistenceFailed } ||
                !ReferenceEquals(app.ActiveRun, authoritativeRun) || RunFingerprint(authoritativeRun) != before)
                failures.Add("first failed settlement committed flow or mutated/replaced the authoritative Run");
            if (save.SettlementAttempts != 1 || save.SettlementFailures != 1 ||
                save.SettlementSuccesses != 0)
                failures.Add("first failed settlement did not perform exactly one failed publication write");
            if (resultScreen.Visible || resultTitle.Text.Contains("征程失败", StringComparison.Ordinal))
                failures.Add("victory persistence failure was presented as a committed defeat");

            root.Flow.AcceptBattleResult(result);
            await ProcessFrames(2);
            if (save.SettlementAttempts != 1)
                failures.Add("repeated battle-finished callback automatically retried settlement");

            root.Flow.ShowBattleReport();
            await ProcessFrames(2);
            var report = root.GetNode<Control>("Screens/BattleReportScreen");
            var settlementMessage = root.GetNode<Label>(
                "Screens/BattleReportScreen/Margin/Panel/Layout/SettlementMessage");
            var retry = root.GetNode<Button>(
                "Screens/BattleReportScreen/Margin/Panel/Layout/ReportContinue");
            if (!report.Visible || !settlementMessage.Visible ||
                !settlementMessage.Text.Contains("没有改变", StringComparison.Ordinal) ||
                retry.Text != "重试结算" || retry.Disabled)
                failures.Add("authored battle report did not expose the visible settlement retry state");

            await Click(retry);
            var acceptedResolution = root.Flow.PendingBattleResolution;
            var rewardScreen = root.GetNode<Control>("Screens/RewardScreen");
            if (!root.Flow.BattleResolutionCommitted ||
                acceptedResolution is not { Accepted: true, Failure: RunBattleResolutionFailure.None } ||
                !ReferenceEquals(app.ActiveRun, authoritativeRun) ||
                authoritativeRun.FloorIndex != startingFloor + 1 || authoritativeRun.PendingNode ||
                !rewardScreen.Visible || resultScreen.Visible)
                failures.Add("real retry input did not commit the same Run once and route victory to reward");
            if (save.SettlementAttempts != 2 || save.SettlementFailures != 1 ||
                save.SettlementSuccesses != 1)
                failures.Add("settlement retry did not produce exactly one failed and one successful publication write");

            root.Flow.AcceptBattleResult(result);
            InvokePrivate(root.Flow, "ContinueAfterBattleReport");
            await ProcessFrames(2);
            if (save.SettlementAttempts != 2 || authoritativeRun.FloorIndex != startingFloor + 1)
                failures.Add("repeated callback/report continue submitted the accepted settlement twice");
        }
        finally
        {
            if (root.GetParent() is not null) root.GetParent().RemoveChild(root);
            root.Free();
            new SaveService(saveNamespace).DeleteActiveRun();
            await ProcessFrames(2);
        }
    }

    private static void VerifyDeploymentCellCopy(List<string> failures)
    {
        var source = FileAccess.GetFileAsString("res://scenes/ui/components/DeploymentCell.tscn");
        if (source.Contains("可部署", StringComparison.Ordinal) || source.Contains("1-1", StringComparison.Ordinal))
            failures.Add("authored empty deployment cell retains persistent deployment/coordinate copy");

        var cell = GD.Load<PackedScene>("res://scenes/ui/components/DeploymentCell.tscn").Instantiate<DeploymentCell>();
        var host = new Control();
        host.AddChild(cell);
        cell.Bind(new Vector2I(1, 2), string.Empty, string.Empty, false, false, true, FloorCellPreview.Normal);
        if (!string.IsNullOrEmpty(cell.Text))
            failures.Add("runtime empty deployment cell renders persistent text: " + cell.Text.Replace('\n', '/'));
        host.Free();
    }

    private async Task Click(Control control)
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
        await ProcessFrames(1);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = false,
            Position = point,
            GlobalPosition = point
        }, true);
        await ProcessFrames(2);
    }

    private async Task Drag(Control source, Control target)
    {
        var start = source.GetGlobalRect().GetCenter();
        var end = target.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = start, GlobalPosition = start }, true);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = true, Position = start, GlobalPosition = start
        }, true);
        await ProcessFrames(1);
        for (var step = 1; step <= 3; step++)
        {
            var point = start.Lerp(end, step / 3f);
            GetViewport().PushInput(new InputEventMouseMotion
            {
                Position = point, GlobalPosition = point, Relative = (end - start) / 3f,
                ButtonMask = MouseButtonMask.Left
            }, true);
            await ProcessFrames(1);
        }
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left, Pressed = false, Position = end, GlobalPosition = end
        }, true);
        await ProcessFrames(3);
    }

    private async Task ActivateFocused(BaseButton button)
    {
        button.GrabFocus();
        await ProcessFrames(1);
        GetViewport().PushInput(new InputEventAction { Action = "ui_accept", Pressed = true, Strength = 1f }, true);
        await ProcessFrames(1);
        GetViewport().PushInput(new InputEventAction { Action = "ui_accept", Pressed = false, Strength = 0f }, true);
        await ProcessFrames(2);
    }

    private async Task ProcessFrames(int count)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static RunApplication GetApplication(GameRoot root)
    {
        var field = typeof(GameRoot).GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(root) as RunApplication
            ?? throw new InvalidOperationException("GameRoot application unavailable");
    }

    private static void SetApplication(GameRoot root, RunApplication application)
    {
        var field = typeof(GameRoot).GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(GameRoot).FullName, "_app");
        field.SetValue(root, application);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, methodName);
        method.Invoke(target, null);
    }

    private static IReadOnlyList<DeploymentUnitViewModel> BuildPieces(ContentRegistry registry, ActiveRunDto run)
    {
        var pieces = new List<DeploymentUnitViewModel>();
        foreach (var instance in run.Roster)
        {
            if (!registry.TryGet(instance.ContentId, out var entry) || entry.Definition is not UnitDefinition definition)
                throw new InvalidOperationException("missing roster hero: " + instance.ContentId);
            var slot = run.Deployment.IndexOf(instance.InstanceId);
            pieces.Add(new DeploymentUnitViewModel(instance.InstanceId, definition.DisplayName, definition.Description,
                instance.HealthRatio, definition.Role, definition.AttackRange, true, slot,
                slot >= 0 ? BattlefieldLayout.PlayerDeploymentCells[slot] : null, definition.Portrait));
        }
        return pieces;
    }

    private static IReadOnlyList<TowerRegionDefinition> Regions() =>
    [
        GD.Load<TowerRegionDefinition>("res://content/tower/region_ember_foundry.tres"),
        GD.Load<TowerRegionDefinition>("res://content/tower/region_gloam_crypt.tres"),
        GD.Load<TowerRegionDefinition>("res://content/tower/region_crown_engine.tres")
    ];

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
        return new BattleResult(
            outcome,
            25,
            new string('b', 64),
            units,
            0,
            0,
            relicTransition,
            config.Identity);
    }

    private static string RunFingerprint(ActiveRunDto run) => string.Join('|',
        run.Version,
        run.Seed,
        run.CurrentPopulation,
        "population-sources:" + string.Join(';', run.PopulationCapSources.Select(source =>
            $"{source.SourceId},{source.Amount}")),
        run.Gold,
        run.FloorIndex,
        run.BattleNumber,
        run.PendingNode,
        run.SelectedNode,
        "roster:" + string.Join(';', run.Roster.Select(unit =>
            $"{unit.InstanceId},{unit.ContentId},{unit.HealthRatio},{unit.Rank}")),
        "deployment:" + string.Join(',', run.Deployment),
        "items:" + string.Join(';', run.Items.Select(item =>
            $"{item.InstanceId},{item.ContentId},{item.Stacks},{item.Charges},{item.Roll}")));

    private sealed class MemoryRunSaveService(ContentRegistry registry) : IRunSaveService
    {
        private readonly MetaProgressDto _meta = new()
        {
            UnlockedHeroIds = registry.Catalog.Heroes.Select(entry => entry.StableId).ToList()
        };
        private readonly SettingsDto _settings = new();
        private ActiveRunDto? _run;

        public MetaProgressDto LoadMeta() => _meta;
        public SettingsDto LoadSettings() => _settings;
        public ActiveRunDto? LoadActiveRun() => _run;
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { _run = value; return true; }
        public void DeleteActiveRun() => _run = null;
    }

    private sealed class FlakySettlementSaveService(ContentRegistry registry) : IRunSaveService
    {
        private readonly MetaProgressDto _meta = new()
        {
            UnlockedHeroIds = registry.Catalog.Heroes.Select(entry => entry.StableId).ToList()
        };
        private readonly SettingsDto _settings = new();
        private ActiveRunDto? _run;
        private bool _settlementProbe;
        private bool _failNextSettlement;

        public int SettlementAttempts { get; private set; }
        public int SettlementFailures { get; private set; }
        public int SettlementSuccesses { get; private set; }

        public void BeginSettlementProbe()
        {
            SettlementAttempts = 0;
            SettlementFailures = 0;
            SettlementSuccesses = 0;
            _settlementProbe = true;
            _failNextSettlement = true;
        }

        public MetaProgressDto LoadMeta() => _meta;
        public SettingsDto LoadSettings() => _settings;
        public ActiveRunDto? LoadActiveRun() => _run;
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;

        public bool SaveActiveRun(ActiveRunDto value)
        {
            if (_settlementProbe)
            {
                SettlementAttempts++;
                if (_failNextSettlement)
                {
                    _failNextSettlement = false;
                    SettlementFailures++;
                    return false;
                }
                SettlementSuccesses++;
            }
            _run = value;
            return true;
        }

        public void DeleteActiveRun() => _run = null;
    }
}

internal sealed partial class InteractionErrorLogger : Logger
{
    private readonly object _gate = new();
    private readonly List<string> _errors = [];
    public IReadOnlyList<string> Errors
    {
        get { lock (_gate) return _errors.ToArray(); }
    }

    public override void _LogError(
        string function, string file, int line, string code, string rationale,
        bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (errorType == (int)ErrorType.Warning) return;
        var message = string.IsNullOrWhiteSpace(rationale) ? code : rationale;
        lock (_gate) _errors.Add($"{message} ({file}:{line} {function})");
    }
}
