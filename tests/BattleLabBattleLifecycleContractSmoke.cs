using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Presentation;
using TowerAutobattler.UI;

public partial class BattleLabBattleLifecycleContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private async Task<int> RunAsync()
    {
        Control? host = null;
        BattleScreenController? battle = null;
        try
        {
            GetWindow().Size = new Vector2I(1600, 900);
            var gate = await TestProjectFixture.PublishAsync(this);
            var package = gate.Package ?? throw new InvalidOperationException(
                "Battle Lab lifecycle package: " + string.Join(';', gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var session = new BattleLabSession(index, Math.Max(2, package.Project.RunRules.InitialPopulation), 5501);
            var traitHero = index.PlayerHeroes.FirstOrDefault(hero =>
                package.Content.Graph.ResolveUnitTraitContributions(hero.StableId).Length > 0) ?? index.PlayerHeroes[0];
            var playerPlacement = session.AddAndPlace(traitHero.StableId, BattleLabSide.Player,
                new Vector2I(0, 2));
            Require(playerPlacement.Succeeded, "player placement");
            if (index.Equipment.Length > 0)
                Require(session.Equip(playerPlacement.InstanceId, 0, index.Equipment[0].StableId),
                    "player Equipment fixture");
            Require(session.AddAndPlace(index.PveUnits.First(unit => unit.Definition.IsEnemy).StableId,
                BattleLabSide.Enemy, new Vector2I(9, 2)).Succeeded, "enemy placement");
            var frozen = session.Freeze();
            var config = new BattleLabPreparationAdapter(index).Build(frozen);

            host = new Control { Size = new Vector2(1600, 900) };
            AddChild(host);
            battle = (GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn") ??
                      throw new InvalidOperationException("BattleScreen missing")).Instantiate<BattleScreenController>();
            host.AddChild(battle);
            await Frame(3);
            var finished = 0;
            var resets = 0;
            var returns = 0;
            battle.Finished += _ => finished++;
            battle.ResetRequested += () => resets++;
            battle.ReturnToConfigurationRequested += () => returns++;
            battle.StartBattle(package.Content, config, "战斗实验室", 1f);
            battle.SetLabControlsVisible(true);
            await Frame(2);
            var initial = battle.ReadRuntimeUnits();
            Require(initial.Length == 2 && initial.All(unit => unit.Health == unit.MaxHealth), "fresh runtime read model");
            var initialPlayer = initial.Single(unit => unit.Team == 0);
            Require(initialPlayer.ContentId == traitHero.StableId &&
                    initialPlayer.SourceInstanceId == playerPlacement.InstanceId &&
                    initialPlayer.RuntimeId == playerPlacement.InstanceId &&
                    initialPlayer.Damage > 0 && initialPlayer.AttackSpeed > 0 &&
                    initialPlayer.Reach > 0 && initialPlayer.ControlResistance >= 0,
                "runtime inspection identities and final attributes");
            Require(initialPlayer.TraitContributions.Length > 0 && initialPlayer.TeamTraits.Length > 0,
                "runtime inspection unit Trait contributions and team tiers");
            if (index.Equipment.Length > 0)
                Require(initialPlayer.Equipment.Length == 1 &&
                        initialPlayer.Equipment[0].InstanceId == session.Units.Single(unit =>
                            unit.InstanceId == playerPlacement.InstanceId).Equipment[0].InstanceId,
                    "runtime inspection Equipment instance identity");
            battle.SetPaused(true);
            await VerifyResponsiveLayout(battle, host);
            battle.SetPaused(false);

            await Click(battle.GetNode<Control>("%PauseButton"));
            Require(battle.IsPaused, "real pause input");
            var tick = battle.TickIndex;
            await Click(battle.GetNode<Control>("%StepButton"));
            Require(battle.TickIndex == tick + 1, "real single fixed tick");
            await Click(battle.GetNode<Control>("%PauseButton"));
            Require(!battle.IsPaused, "real continue input");
            tick = battle.TickIndex;
            battle._Process(1.0);
            Require(battle.TickIndex > tick, "continued battle resumes simulation advancement");
            await Click(battle.GetNode<Control>("%PauseButton"));
            Require(battle.IsPaused, "real pause input after continue");
            await Click(battle.GetNode<Control>("%SpeedButton"));
            Require(battle.SpeedScale == 2f, "real x2 input");
            await Click(battle.GetNode<Control>("%SpeedButton"));
            Require(battle.SpeedScale == 4f, "real x4 input");
            await Click(battle.GetNode<Control>("%SpeedButton"));
            Require(battle.SpeedScale == 1f, "real x1 input");

            var guard = 0;
            while (battle.Outcome == BattleOutcome.Running && guard++ < 2500)
                Require(battle.StepOneTick(), "paused fixed-step progression");
            Require(battle.Outcome != BattleOutcome.Running && battle.TerminalResult is not null && finished == 1,
                "terminal fixed-step handling exactly once");
            var firstResult = Projection(battle.TerminalResult);

            await Click(battle.GetNode<Control>("%ResetBattleButton"));
            Require(resets == 1, "isolated BattleScreen reset button emits its owner intent once");
            battle.StopBattle();
            Require(!battle.HasActiveBattle && battle.ActiveFloatingCueCount == 0 &&
                    battle.ActiveFloatingTweenCount == 0, "replacement cleanup");

            battle.StartBattle(package.Content, new BattleLabPreparationAdapter(index).Build(frozen), "战斗实验室", 1f);
            battle.SetLabControlsVisible(true);
            battle.SetPaused(true);
            var secondInitial = battle.ReadRuntimeUnits();
            Require(secondInitial.All(unit => unit.Health == unit.MaxHealth && unit.Statuses.Length == 0),
                "independent replacement start builds clean runtime state");
            guard = 0;
            while (battle.Outcome == BattleOutcome.Running && guard++ < 2500) battle.StepOneTick();
            Equal(Projection(battle.TerminalResult), firstResult, "same snapshot and seed determinism");
            battle.StopBattle();
            battle.StartBattle(package.Content, new BattleLabPreparationAdapter(index).Build(frozen), "战斗实验室", 1f);
            battle.SetLabControlsVisible(true);
            await Click(battle.GetNode<Control>("%ReturnConfigurationButton"));
            Require(returns == 1, "real return-to-configuration intent");
            battle.StopBattle();
            Equal(session.Freeze().CanonicalDigest, frozen.CanonicalDigest, "editable configuration preserved");
            VerifyFailureTransactions(battle, package.Content, index, frozen);
            host.QueueFree();
            host = null;
            battle = null;
            await Frame(3);
            await VerifyCoordinatorResetPaths();
            GD.Print("BATTLE_LAB_BATTLE_LIFECYCLE_CONTRACT_OK controls=pause-continue-step-x1-x2-x4-reset-return " +
                     "reset=running-paused-terminal-production-path " +
                     "inspection=immutable-runtime terminal=once deterministic=same-seed config=preserved " +
                     "cleanup=simulation-cues-tweens-nodes");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("BATTLE_LAB_BATTLE_LIFECYCLE_CONTRACT_FAILED: " + exception);
            battle?.StopBattle();
            host?.QueueFree();
            return 1;
        }
    }

    private static void VerifyFailureTransactions(
        BattleScreenController battle,
        ContentRegistry content,
        BattleLabContentIndex index,
        BattleLabStartSnapshot snapshot)
    {
        battle.PresenterFactory = entry =>
        {
            var presenter = entry.Scene.Instantiate<UnitContentRoot>();
            presenter.Bind("prebound-fixture", 0, 1, 1);
            presenter.Activate(new UnitBindingContext(
                new DeterministicRandom(1), SilentEvents.Instance, RejectCommands.Instance));
            return presenter;
        };
        RequireThrows(() => battle.StartBattle(
                content,
                new BattleLabPreparationAdapter(index).Build(snapshot),
                "presenter failure"),
            "tree-attached presenter bind failure propagates");
        Require(!battle.HasActiveBattle && battle.GetNode<Node2D>("%UnitsRoot").GetChildCount() == 0 &&
                !string.IsNullOrWhiteSpace(battle.LastRuntimeFailure),
            "presenter failure transaction removes simulation and unregistered tree child");
        battle.PresenterFactory = entry => entry.Scene.Instantiate<UnitContentRoot>();

        var startBase = new BattleLabPreparationAdapter(index).Build(snapshot);
        var startFailure = CloneWithFloor(startBase, new ThrowingFloorRule(startBase.FloorRule, throwDisplayName: true));
        RequireThrows(() => battle.StartBattle(content, startFailure, "start failure"),
            "failure after simulation construction propagates");
        Require(!battle.HasActiveBattle && battle.GetNode<Node2D>("%UnitsRoot").GetChildCount() == 0,
            "start transaction cleans simulation and presenters");

        var stepBase = new BattleLabPreparationAdapter(index).Build(snapshot);
        battle.StartBattle(content, CloneWithFloor(stepBase,
            new ThrowingFloorRule(stepBase.FloorRule, throwOnTick: true)), "step failure");
        battle.SetLabControlsVisible(true);
        battle.SetPaused(true);
        RequireThrows(() => battle.StepOneTick(), "fixed-step failure propagates");
        Require(!battle.HasActiveBattle && !string.IsNullOrWhiteSpace(battle.LastRuntimeFailure),
            "fixed-step failure reliably cleans runtime");

        var processBase = new BattleLabPreparationAdapter(index).Build(snapshot);
        battle.StartBattle(content, CloneWithFloor(processBase,
            new ThrowingFloorRule(processBase.FloorRule, throwOnTick: true)), "process failure");
        battle._Process(1.0);
        Require(!battle.HasActiveBattle && !string.IsNullOrWhiteSpace(battle.LastRuntimeFailure),
            "process failure reliably cleans runtime without repeated routing");
    }

    private static BattleConfig CloneWithFloor(BattleConfig source, IBattleFloorRuleRuntime floorRule) => new()
    {
        Seed = source.Seed,
        Identity = source.Identity,
        FloorRule = floorRule,
        Spawns = source.Spawns,
        HeroRule = source.HeroRule,
        Modifiers = source.Modifiers,
        Summons = source.Summons,
        EmptyDeploymentSlots = source.EmptyDeploymentSlots,
        StartingGold = source.StartingGold,
        Relics = source.Relics,
        RelicSummons = source.RelicSummons,
        Equipment = source.Equipment,
        Traits = source.Traits,
        TacticalCommands = source.TacticalCommands,
        TacticalSummons = source.TacticalSummons,
        BossTimeline = source.BossTimeline,
        ConfigureCombatBindings = source.ConfigureCombatBindings
    };

    private async Task VerifyCoordinatorResetPaths()
    {
        GameRoot? root = null;
        try
        {
            var scene = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn") ??
                        throw new InvalidOperationException("GameRoot scene missing for coordinator reset fixture");
            root = scene.Instantiate<GameRoot>();
            root.SaveNamespace = "tests/battle_lab_lifecycle_resets";
            AddChild(root);
            for (var frame = 0; frame < 600 && root.Content is null; frame++) await Frame();
            Require(root.Content is not null, "coordinator reset fixture bootstrap");

            var screens = root.GetNode<AppScreenHost>("Screens");
            await Click(screens.MainMenu.GetNode<Control>("Center/Panel/Menu/BattleLabButton"));
            var digest = screens.BattleLab.CurrentSnapshot?.CanonicalDigest ??
                         throw new InvalidOperationException("coordinator reset Lab snapshot missing");
            await Click(screens.BattleLab.GetNode<Control>("%StartButton"));
            var battle = screens.Battle;
            Require(battle.HasActiveBattle && battle.Outcome == BattleOutcome.Running,
                "coordinator running reset fixture started through real UI");
            var expectedUnits = battle.ReadRuntimeUnits().Length;

            await Click(battle.GetNode<Control>("%ResetBattleButton"));
            RequireFreshCoordinatorReset(screens, digest, expectedUnits, "running");

            await Click(battle.GetNode<Control>("%PauseButton"));
            Require(battle.IsPaused, "coordinator paused reset fixture paused through real UI");
            await Click(battle.GetNode<Control>("%StepButton"));
            Require(battle.TickIndex == 1, "coordinator paused reset fixture mutated one fixed tick");
            await Click(battle.GetNode<Control>("%ResetBattleButton"));
            RequireFreshCoordinatorReset(screens, digest, expectedUnits, "paused");

            await Click(battle.GetNode<Control>("%PauseButton"));
            Require(battle.IsPaused, "coordinator terminal reset fixture paused through real UI");
            var guard = 0;
            while (battle.Outcome == BattleOutcome.Running && guard++ < 4000)
                Require(battle.StepOneTick(), "coordinator terminal reset fixture progression");
            Require(battle.TerminalResult is not null, "coordinator terminal reset fixture reached terminal state");
            await Click(battle.GetNode<Control>("%ResetBattleButton"));
            RequireFreshCoordinatorReset(screens, digest, expectedUnits, "terminal");

            await Click(battle.GetNode<Control>("%ReturnConfigurationButton"));
            Require(screens.BattleLab.Visible && !battle.HasActiveBattle &&
                    screens.BattleLab.CurrentSnapshot?.CanonicalDigest == digest,
                "coordinator reset fixture returns to unchanged configuration with runtime cleanup");
        }
        finally
        {
            root?.QueueFree();
            await Frame(3);
        }
    }

    private static void RequireFreshCoordinatorReset(
        AppScreenHost screens,
        string expectedDigest,
        int expectedUnits,
        string phase)
    {
        var battle = screens.Battle;
        var units = battle.ReadRuntimeUnits();
        Require(screens.Battle.Visible && battle.HasActiveBattle && battle.Outcome == BattleOutcome.Running &&
                battle.TerminalResult is null && !battle.IsPaused,
            $"{phase} reset starts a fresh running production battle");
        Require(units.Length == expectedUnits && units.All(unit =>
                    unit.Health == unit.MaxHealth && unit.Statuses.Length == 0),
            $"{phase} reset rebuilds clean runtime unit state");
        Require(battle.ActiveFloatingCueCount == 0 && battle.ActiveFloatingTweenCount == 0 &&
                battle.GetNode<Node2D>("%UnitsRoot").GetChildCount() == expectedUnits,
            $"{phase} reset cleans replaced presenters, cues, and Tweens");
        Require(screens.BattleLab.CurrentSnapshot?.CanonicalDigest == expectedDigest,
            $"{phase} reset preserves editable configuration");
    }

    private static string Projection(BattleResult? result) => result is null ? string.Empty :
        $"{result.Outcome}|{result.Ticks}|{result.Digest}|" + string.Join(';', result.Units
            .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal)
            .Select(unit => $"{unit.RuntimeId}:{unit.FinalHealth}:{unit.FinalCell.X},{unit.FinalCell.Y}"));

    private async Task Click(Control control)
    {
        var position = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(Mouse(position, true));
        await Frame();
        GetViewport().PushInput(Mouse(position, false));
        await Frame(2);
    }

    private async Task VerifyResponsiveLayout(BattleScreenController battle, Control host)
    {
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1600, 900) })
        {
            GetWindow().Size = size;
            host.Size = size;
            await Frame(4);
            var screenRect = battle.GetGlobalRect();
            foreach (var path in new[]
                     {
                         "%BattleStatus", "%PauseButton", "%SpeedButton", "%StepButton",
                         "%ResetBattleButton", "%ReturnConfigurationButton", "%TacticalCommandHud"
                     })
                Require(Inside(screenRect, battle.GetNode<Control>(path).GetGlobalRect()),
                    $"Battle HUD {path} reachable at {size.X}x{size.Y}");
            Require(Inside(screenRect, battle.GetNode<Control>("%BattleBoard").GetGlobalRect()),
                $"Battle board reachable at {size.X}x{size.Y}");
        }
    }

    private static bool Inside(Rect2 outer, Rect2 inner) =>
        outer.HasPoint(inner.Position + Vector2.One) &&
        outer.HasPoint(inner.End - Vector2.One);

    private static InputEventMouseButton Mouse(Vector2 position, bool pressed) => new()
    {
        Position = position,
        GlobalPosition = position,
        ButtonIndex = MouseButton.Left,
        ButtonMask = pressed ? MouseButtonMask.Left : 0,
        Pressed = pressed
    };

    private async Task Frame(int count = 1)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }

    private static void Equal<T>(T actual, T expected, string label)
    {
        if (!Equals(actual, expected)) throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }

    private static void RequireThrows(Action action, string label)
    {
        try { action(); }
        catch (Exception) { return; }
        throw new InvalidOperationException(label);
    }

    private sealed class ThrowingFloorRule(
        IBattleFloorRuleRuntime inner,
        bool throwDisplayName = false,
        bool throwOnTick = false) : IBattleFloorRuleRuntime
    {
        public string Id => inner.Id;
        public string DisplayName => throwDisplayName
            ? throw new InvalidOperationException("fixture display failure")
            : inner.DisplayName;
        public string PreviewText => inner.PreviewText;
        public bool CanOccupy(Vector2I cell) => inner.CanOccupy(cell);
        public FloorCellPreview GetCellPreview(Vector2I cell) => inner.GetCellPreview(cell);
        public void OnBattleStarted(BattleRuleContext context) => inner.OnBattleStarted(context);
        public void OnTick(BattleRuleContext context)
        {
            if (throwOnTick) throw new InvalidOperationException("fixture tick failure");
            inner.OnTick(context);
        }
        public void OnBattleEnded(BattleRuleContext context, BattleOutcome outcome) =>
            inner.OnBattleEnded(context, outcome);
        public float ModifyIncomingDamage(BattleRuleContext context, BattleUnitState target, float rawDamage) =>
            inner.ModifyIncomingDamage(context, target, rawDamage);
    }

    private sealed class SilentEvents : ISemanticBattleEventSink
    {
        public static readonly SilentEvents Instance = new();
        public void Publish(SemanticBattleEvent battleEvent) { }
    }

    private sealed class RejectCommands : IBattleCommandGateway
    {
        public static readonly RejectCommands Instance = new();
        public bool Submit(BattleCommandRequest command) => false;
    }
}
