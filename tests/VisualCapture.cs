using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class VisualCapture : Node
{
    private const string OutputPath = "res://.godot/qa";
    private const string CommanderId = "hero_banner_marshal";
    private bool _blockNextProcessFrame;

    public override void _Process(double delta)
    {
        if (!_blockNextProcessFrame) return;
        _blockNextProcessFrame = false;
        System.Threading.Thread.Sleep(320);
    }

    public override async void _Ready()
    {
        var code = await CaptureAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GetTree().Quit(code);
    }

    private async Task<int> CaptureAsync()
    {
        try
        {
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(OutputPath));
            await CaptureCoreFlow();
            await CaptureShop();
            await CaptureEvent();
            await CaptureRest();
            await CaptureRecruitment();
            await CaptureSettings();
            await CaptureHazardBattle();
            await CaptureAnimationStates();
            await CaptureMovementSequences();
            await CaptureRealProductionHitch();
            await CaptureVictory();
            await CaptureDefeat();
            GD.Print("VISUAL_CAPTURE_OK screens=12 extras=15 movement_frames=29 flows=report,reward,recruitment,shop,victory,defeat states=attack,defeated,hitch,grid-march information=selected-unit,zero-mana,semantic-icons path=res://.godot/qa");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("VISUAL_CAPTURE_FAILED: " + exception);
            return 1;
        }
    }

    private async Task CaptureCoreFlow()
    {
        var root = await CreateRoot("core");
        try
        {
            await CaptureScreen(root, "MainMenuScreen");
            Press(root, "Screens/MainMenuScreen/Center/Panel/Menu/NewRunButton");
            await RenderFrame();
            await CaptureScreen(root, "HeroSelectScreen");
            var heroScreen = root.GetNode<HeroSelectScreen>("Screens/HeroSelectScreen");
            heroScreen.Preview("hero_gilded_factor");
            await CaptureCurrent("HeroSelectMerchant.png");
            var lockedTile = root.GetNode<GridContainer>("Screens/HeroSelectScreen/Margin/Layout/Content/LibraryPanel/LibraryLayout/LibraryScroll/HeroLibrary")
                .GetChildren().OfType<HeroLibraryTile>().FirstOrDefault(tile => !tile.Unlocked);
            if (lockedTile is not null)
            {
                heroScreen.Preview(lockedTile.StableId);
                await CaptureCurrent("HeroSelectLocked.png");
            }
            heroScreen.Preview(CommanderId);
            StartCommanderRun(root, 1100, floor: 0, strong: true);
            await RenderFrame();
            await CaptureScreen(root, "TowerScreen");
            Press(root, "ArmyOverview/SummaryButton");
            await CaptureCurrent("ArmyDrawerTower.png");
            Press(root, "ArmyOverview/Drawer/Layout/Header/CloseButton");
            PressChoice(root, "Screens/TowerScreen/Margin/Layout/Choices", TowerNodeType.Combat.ToString());
            await RenderFrame();
            await CaptureScreen(root, "DeploymentScreen");
            Press(root, "Screens/DeploymentScreen/Margin/Layout/Actions/StartBattleButton");
            await RenderFrame();
            var battle = root.GetNode<BattleScreenController>("Screens/BattleScreen");
            var board = root.GetNode<BattleBoard>("Screens/BattleScreen/Margin/Layout/BattleBoard");
            var heroClick = board.GlobalPosition + board.CellToLocal(BattlefieldLayout.HeroCell);
            GetViewport().PushInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                Position = heroClick,
                GlobalPosition = heroClick
            }, true);
            await RenderFrame();
            var selectedPanel = root.GetNode<Control>("Screens/BattleScreen/Margin/Layout/BattleBoard/SelectedUnitPanel");
            if (!selectedPanel.Visible)
                throw new InvalidOperationException("production BattleBoard mouse input did not open selected-unit details");
            await CaptureCurrent("SelectedUnitDetails.png");
            battle._Process(1.0);
            await CaptureScreen(root, "BattleScreen");
            const string commandPath = "Screens/BattleScreen/Margin/Layout/Hud/HeroCommandHud/Layout/Mana/CommandButton";
            Press(root, commandPath);
            if (battle.CurrentMana != 2)
                throw new InvalidOperationException("first command did not leave the battle at 2/3 mana");
            await CaptureCurrent("BattleCommandMana.png");
            Press(root, commandPath);
            Press(root, commandPath);
            if (battle.CurrentMana != 0)
                throw new InvalidOperationException("three successful commands did not exhaust battle mana");
            Press(root, commandPath);
            if (!battle.CommandFeedback.Contains("法力不足", StringComparison.Ordinal))
                throw new InvalidOperationException("zero-mana command attempt did not expose the authored failure reason");
            await CaptureCurrent("BattleCommandManaEmpty.png");
            ResolveBattle(root, useCommands: true);
            await RenderFrame();
            RequireVisible(root, "BattleReportScreen");
            await CaptureCurrent("BattleReportPlayer.png");
            Press(root, "Screens/BattleReportScreen/Margin/Panel/Layout/Tabs/EnemyTab");
            await CaptureCurrent("BattleReportEnemy.png");
            Press(root, "Screens/BattleReportScreen/Margin/Panel/Layout/ReportContinue");
            RequireVisible(root, "RewardScreen");
            if (root.GetNode<Container>("Screens/RewardScreen/Center/Panel/Layout/ChoiceScroll/Choices").GetChildCount() == 0)
                throw new InvalidOperationException("normal combat reward did not populate choices");
            RejectInternalEnumText(root.GetNode<Container>("Screens/RewardScreen/Center/Panel/Layout/ChoiceScroll/Choices"));
            await CaptureScreen(root, "RewardScreen");
        }
        finally { await DisposeRoot(root); }
    }

    private async Task CaptureShop()
    {
        var root = await CreateRoot("shop");
        try
        {
            StartNewRunFromMenu(root);
            StartCommanderRun(root, 1100, floor: 0, strong: true);
            await RenderFrame();
            PressChoice(root, "Screens/TowerScreen/Margin/Layout/Choices", TowerNodeType.Shop.ToString());
            await RenderFrame();
            RequireVisible(root, "ShopScreen");
            if (root.GetNode<Container>("Screens/ShopScreen/Margin/Layout/Choices").GetChildCount() == 0)
                throw new InvalidOperationException("shop did not populate choices");
            await CaptureScreen(root, "ShopScreen");
            Press(root, "ArmyOverview/SummaryButton");
            await CaptureCurrent("ArmyDrawerShop.png");
            Press(root, "ArmyOverview/Drawer/Layout/Header/CloseButton");
        }
        finally { await DisposeRoot(root); }
    }

    private async Task CaptureEvent()
    {
        var root = await CreateRoot("event");
        try
        {
            StartNewRunFromMenu(root);
            StartCommanderRun(root, 1100, floor: 0, strong: false);
            await RenderFrame();
            PressChoice(root, "Screens/TowerScreen/Margin/Layout/Choices", TowerNodeType.Event.ToString());
            await RenderFrame();
            await CaptureScreen(root, "EventScreen");
        }
        finally { await DisposeRoot(root); }
    }

    private async Task CaptureRest()
    {
        var root = await CreateRoot("rest");
        try
        {
            StartNewRunFromMenu(root);
            StartCommanderRun(root, 1101, floor: 0, strong: false);
            await RenderFrame();
            PressChoice(root, "Screens/TowerScreen/Margin/Layout/Choices", TowerNodeType.Rest.ToString());
            await RenderFrame();
            await CaptureScreen(root, "RestScreen");
        }
        finally { await DisposeRoot(root); }
    }

    private async Task CaptureSettings()
    {
        var root = await CreateRoot("settings");
        try
        {
            Press(root, "Screens/MainMenuScreen/Center/Panel/Menu/SettingsButton");
            await RenderFrame();
            await CaptureScreen(root, "SettingsScreen");
        }
        finally { await DisposeRoot(root); }
    }

    private async Task CaptureRecruitment()
    {
        var root = await CreateRoot("recruitment");
        try
        {
            StartNewRunFromMenu(root);
            StartCommanderRun(root, 1101, floor: 0, strong: false);
            await RenderFrame();
            PressChoice(root, "Screens/TowerScreen/Margin/Layout/Choices", TowerNodeType.Recruitment.ToString());
            await RenderFrame();
            RequireVisible(root, "RecruitmentScreen");
            if (root.GetNode<Control>("Screens/RewardScreen").Visible)
                throw new InvalidOperationException("recruitment flow leaked into the ordinary reward screen");
            var title = root.GetNode<Label>("Screens/RecruitmentScreen/Center/Panel/Layout/Title").Text;
            if (title != "征募新兵") throw new InvalidOperationException("recruitment flow did not populate its authored screen");
            RejectInternalEnumText(root.GetNode<Container>("Screens/RecruitmentScreen/Center/Panel/Layout/ChoiceScroll/Choices"));
            await CaptureCurrent("RecruitmentScreen.png");
        }
        finally { await DisposeRoot(root); }
    }

    private async Task CaptureHazardBattle()
    {
        var root = await CreateRoot("hazard");
        try
        {
            StartNewRunFromMenu(root);
            var app = StartCommanderRun(root, 4400, floor: 5, strong: true);
            var encounter = new EncounterPlan(
                "熔炉脉冲实战",
                "rule_hazard_pulse",
                ["enemy_rust_guard", "enemy_crossbow", "enemy_cutpurse", "enemy_rust_guard"],
                false,
                false);
            var battle = root.GetNode<BattleScreenController>("Screens/BattleScreen");
            InvokePrivate(root, "Show", battle);
            battle.StartBattle(app.Content, app.BuildBattleConfig(encounter), encounter.Title);
            battle._Process(1.0);
            await RenderFrame();
            await CaptureCurrent("BattleHazard.png");
        }
        finally { await DisposeRoot(root); }
    }

    private async Task CaptureAnimationStates()
    {
        var root = await CreateRoot("animation-states");
        try
        {
            StartNewRunFromMenu(root);
            var app = StartCommanderRun(root, 1100, floor: 0, strong: true);
            var run = app.ActiveRun ?? throw new InvalidOperationException("animation capture run missing");
            var encounter = app.Tower.Encounter(run, TowerNodeType.Combat);
            var battle = root.GetNode<BattleScreenController>("Screens/BattleScreen");
            InvokePrivate(root, "Show", battle);
            battle.StartBattle(app.Content, app.BuildBattleConfig(encounter), "攻击与败退状态");
            await RenderFrame();

            var presenters = typeof(BattleScreenController)
                .GetField("_presenters", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(battle) as System.Collections.Generic.Dictionary<string, UnitContentRoot>
                ?? throw new InvalidOperationException("battle presenters unavailable");
            var hero = presenters.Values.First(unit => unit.Team == 0 && unit.Definition.IsHero);
            hero.ApplyPresentation("attack", hero.Position, 100f, 100f);
            await CaptureCurrent("BattleAttackState.png");

            var enemy = presenters.Values.First(unit => unit.Team == 1);
            enemy.ApplyPresentation("defeated", enemy.Position, 0f, 100f);
            await CaptureCurrent("BattleDefeatState.png");
        }
        finally { await DisposeRoot(root); }
    }

    private async Task CaptureMovementSequences()
    {
        var root = await CreateRoot("movement-sequences");
        try
        {
            StartNewRunFromMenu(root);
            var app = StartCommanderRun(root, 1100, floor: 0, strong: true);
            var run = app.ActiveRun ?? throw new InvalidOperationException("movement capture run missing");
            var encounter = app.Tower.Encounter(run, TowerNodeType.Combat);
            var battle = root.GetNode<BattleScreenController>("Screens/BattleScreen");
            InvokePrivate(root, "Show", battle);

            foreach (var speed in new[] { 1f, 2f, 4f })
            {
                var probe = PrepareMovementProbe(app, encounter, battle, speed, $"{speed:0}x 单格移动");
                await CaptureMovementFrame($"{speed:0}x_OneCell", 0);
                probe.Unit.QueueMovement(probe.Right);
                probe.Motion._Process(.30);
                probe.Motion._Process(speed switch { 1f => .05, 2f => .045, _ => .03 });
                await CaptureMovementFrame($"{speed:0}x_OneCell", 1);
                AdvanceMotionFrames(probe.Motion, 4, .05);
                await CaptureMovementFrame($"{speed:0}x_OneCell", 2);
            }

            {
                var probe = PrepareMovementProbe(app, encounter, battle, 1f, "格点行军转向");
                await CaptureMovementFrame("1x_GridMarchTurns", 0);
                probe.Unit.QueueMovement(probe.Right);
                probe.Unit.QueueMovement(probe.Corner);
                probe.Unit.QueueMovement(probe.LeftCorner);
                probe.Motion._Process(.30);
                probe.Motion._Process(.05);
                await CaptureMovementFrame("1x_GridMarchTurns", 1);
                probe.Motion._Process(.05);
                await CaptureMovementFrame("1x_GridMarchTurns", 2);
                probe.Motion._Process(.05);
                await CaptureMovementFrame("1x_GridMarchTurns", 3);
                probe.Motion._Process(.05);
                await CaptureMovementFrame("1x_GridMarchTurns", 4);
                probe.Motion._Process(.05);
                await CaptureMovementFrame("1x_GridMarchTurns", 5);
            }

            {
                var probe = PrepareMovementProbe(app, encounter, battle, 2f, "动作与移动并行");
                probe.Unit.QueueMovement(probe.Right);
                probe.Unit.RefreshPresentation("attack", 100, 100);
                probe.Motion._Process(.30);
                probe.Motion._Process(.04);
                await CaptureMovementFrame("2x_ActionOverlap", 0);
                AdvanceMotionFrames(probe.Motion, 3, .05);
                await CaptureMovementFrame("2x_ActionOverlap", 1);
                probe.Animation._Process(probe.Animation.ActivePlaybackSeconds + .01);
                await CaptureMovementFrame("2x_ActionOverlap", 2);
            }

            {
                var probe = PrepareMovementProbe(app, encounter, battle, 1f, "暂停与继续");
                probe.Unit.QueueMovement(probe.Right);
                probe.Motion._Process(.30);
                probe.Motion._Process(.04);
                probe.Unit.SetPresentationPaused(true);
                var pausedAt = probe.Unit.Position;
                await CaptureMovementFrame("1x_PauseResume", 0);
                probe.Motion._Process(.5);
                await CaptureMovementFrame("1x_PauseResume", 1);
                if (!probe.Unit.Position.IsEqualApprox(pausedAt))
                    throw new InvalidOperationException("rendered pause sequence drifted while paused");
                probe.Unit.SetPresentationPaused(false);
                probe.Motion._Process(.30);
                AdvanceMotionFrames(probe.Motion, 3, .05);
                await CaptureMovementFrame("1x_PauseResume", 2);
            }

            {
                var probe = PrepareMovementProbe(app, encounter, battle, 4f, "败退中断移动");
                probe.Unit.QueueMovement(probe.Right);
                probe.Motion._Process(.30);
                probe.Motion._Process(.03);
                await CaptureMovementFrame("4x_DefeatInterrupt", 0);
                var defeatedAt = probe.Unit.Position;
                probe.Unit.RefreshPresentation("defeated", 0, 100);
                await CaptureMovementFrame("4x_DefeatInterrupt", 1);
                probe.Motion._Process(1);
                if (!probe.Unit.Position.IsEqualApprox(defeatedAt))
                    throw new InvalidOperationException("rendered defeat sequence moved after terminal interruption");
                await CaptureMovementFrame("4x_DefeatInterrupt", 2);
            }
        }
        finally { await DisposeRoot(root); }
    }

    private async Task CaptureRealProductionHitch()
    {
        var root = await CreateRoot("real-production-hitch");
        try
        {
            StartNewRunFromMenu(root);
            var app = StartCommanderRun(root, 7711, floor: 0, strong: false);
            var battle = root.GetNode<BattleScreenController>("Screens/BattleScreen");
            InvokePrivate(root, "Show", battle);
            var heroEntry = app.Content.Catalog.Heroes.Single(entry => entry.StableId == CommanderId);
            var enemyEntry = app.Content.Catalog.Enemies[0];
            var heroAuthoring = heroEntry.Scene.Instantiate<UnitContentRoot>();
            var enemyAuthoring = enemyEntry.Scene.Instantiate<UnitContentRoot>();
            UnitSnapshot heroSnapshot;
            UnitSnapshot enemySnapshot;
            HeroRuleSnapshot heroRule;
            try
            {
                heroSnapshot = BattleSetupFactory.Snapshot((UnitDefinition)heroEntry.Definition, heroAuthoring.Behavior) with
                {
                    Damage = 0,
                    Range = 1f,
                    MoveTicks = 1
                };
                enemySnapshot = BattleSetupFactory.Snapshot((UnitDefinition)enemyEntry.Definition, enemyAuthoring.Behavior) with
                {
                    Damage = 1,
                    Range = 20,
                    AttackTicks = 1000,
                    MoveTicks = 1000
                };
                heroRule = BattleSetupFactory.Snapshot(heroAuthoring.HeroRule!, heroAuthoring.HeroCommand!);
            }
            finally
            {
                heroAuthoring.Free();
                enemyAuthoring.Free();
            }
            battle.StartBattle(app.Content, new BattleConfig
            {
                Seed = 7711,
                FloorRule = new ClearFloorRuleRuntime("visual-real-hitch", "常规", "320ms 卡顿恢复"),
                HeroRule = heroRule,
                Spawns =
                [
                    new BattleSpawn(heroSnapshot, 0, new Vector2I(0, 2), "visual-hitch-hero"),
                    new BattleSpawn(enemySnapshot, 1, new Vector2I(9, 2), "visual-hitch-enemy")
                ]
            }, "真实 320ms 生产卡顿帧");
            await RenderFrame();

            var presenters = typeof(BattleScreenController)
                .GetField("_presenters", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(battle) as System.Collections.Generic.Dictionary<string, UnitContentRoot>
                ?? throw new InvalidOperationException("real hitch presenters unavailable");
            var hero = presenters.Values.First(candidate => candidate.Team == 0 && candidate.Definition.IsHero);
            var motion = hero.GetNode<UnitMotionPresentationComponent>("UnitMotionPresentationComponent");
            var source = hero.Position;
            await CaptureMovementFrame("1x_RealProductionHitch", 0);

            _blockNextProcessFrame = true;
            await CaptureMovementFrame("1x_RealProductionHitch", 1);
            await CaptureMovementFrame("1x_RealProductionHitch", 2);
            battle.SetProcess(false);
            if (!motion.IsMoving)
                throw new InvalidOperationException("real rendered hitch did not enqueue production movement");
            if (!hero.Position.IsEqualApprox(source))
                throw new InvalidOperationException($"real rendered hitch consumed fresh movement before draw: {source} -> {hero.Position}");

            motion._Process(1.0 / 60.0);
            await CaptureMovementFrame("1x_RealProductionHitch", 3);
            for (var frame = 0; frame < 18 && motion.IsMoving; frame++)
            {
                motion._Process(1.0 / 60.0);
                await RenderFrame();
            }
            await CaptureMovementFrame("1x_RealProductionHitch", 4);
            if (motion.IsMoving) throw new InvalidOperationException("real rendered hitch did not settle within supported-frame budget");
        }
        finally { await DisposeRoot(root); }
    }

    private static MovementProbe PrepareMovementProbe(
        RunApplication app,
        EncounterPlan encounter,
        BattleScreenController battle,
        float speed,
        string title)
    {
        battle.StartBattle(app.Content, app.BuildBattleConfig(encounter), title, speed);
        battle.SetProcess(false);
        var presenters = typeof(BattleScreenController)
            .GetField("_presenters", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(battle) as System.Collections.Generic.Dictionary<string, UnitContentRoot>
            ?? throw new InvalidOperationException("movement capture presenters unavailable");
        var unit = presenters.Values.First(candidate => candidate.Team == 0 && candidate.Definition.IsHero);
        var board = battle.GetNode<BattleBoard>("%BattleBoard");
        var source = board.CellToLocal(new Vector2I(1, 2));
        unit.SnapPresentation(source, 100, 100);
        unit.SetPresentationSpeed(speed);
        return new MovementProbe(
            unit,
            unit.GetNode<UnitMotionPresentationComponent>("UnitMotionPresentationComponent"),
            unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent"),
            board.CellToLocal(new Vector2I(2, 2)),
            board.CellToLocal(new Vector2I(2, 3)),
            board.CellToLocal(new Vector2I(1, 3)));
    }

    private async Task CaptureMovementFrame(string sequence, int frame)
    {
        await RenderFrame();
        var image = GetViewport().GetTexture().GetImage();
        var fileName = $"Motion_{image.GetWidth()}x{image.GetHeight()}_{sequence}_{frame:00}.png";
        var error = image.SavePng($"{ProjectSettings.GlobalizePath(OutputPath)}/{fileName}");
        if (error != Error.Ok) throw new InvalidOperationException($"capture {fileName}: {error}");
    }

    private async Task CaptureVictory()
    {
        var root = await CreateRoot("victory");
        try
        {
            StartNewRunFromMenu(root);
            StartCommanderRun(root, 1101, floor: 14, strong: true);
            await RenderFrame();
            PressChoice(root, "Screens/TowerScreen/Margin/Layout/Choices", TowerNodeType.Boss.ToString());
            await RenderFrame();
            Press(root, "Screens/DeploymentScreen/Margin/Layout/Actions/StartBattleButton");
            await RenderFrame();
            var battle = root.GetNode<BattleScreenController>("Screens/BattleScreen");
            battle._Process(1.0);
            await CaptureCurrent("BattleBossWard.png");
            ResolveBattle(root, useCommands: true);
            await RenderFrame();
            RequireVisible(root, "BattleReportScreen");
            await CaptureCurrent("BattleReportFinalVictory.png");
            Press(root, "Screens/BattleReportScreen/Margin/Panel/Layout/ReportContinue");
            RequireVisible(root, "ResultScreen");
            var title = root.GetNode<Label>("Screens/ResultScreen/Center/Panel/Layout/Title").Text;
            if (title != "登塔成功") throw new InvalidOperationException("final boss did not reach victory result: " + title);
            await CaptureScreen(root, "ResultScreen");
            await CaptureCurrent("ResultVictory.png");
        }
        finally { await DisposeRoot(root); }
    }

    private async Task CaptureDefeat()
    {
        var root = await CreateRoot("defeat");
        try
        {
            StartNewRunFromMenu(root);
            var app = StartCommanderRun(root, 1100, floor: 0, strong: false);
            app.ActiveRun!.HeroHealthRatio = .01f;
            foreach (var unit in app.ActiveRun.Roster) unit.HealthRatio = .01f;
            for (var slot = 0; slot < 6; slot++) app.ClearDeploymentSlot(slot);
            InvokePrivate(root, "ShowTower");
            await RenderFrame();
            PressChoice(root, "Screens/TowerScreen/Margin/Layout/Choices", TowerNodeType.Combat.ToString());
            Press(root, "Screens/DeploymentScreen/Margin/Layout/Actions/StartBattleButton");
            ResolveBattle(root, useCommands: false);
            await RenderFrame();
            RequireVisible(root, "BattleReportScreen");
            await CaptureCurrent("BattleReportDefeat.png");
            Press(root, "Screens/BattleReportScreen/Margin/Panel/Layout/ReportContinue");
            RequireVisible(root, "ResultScreen");
            var title = root.GetNode<Label>("Screens/ResultScreen/Center/Panel/Layout/Title").Text;
            if (title != "征程失败") throw new InvalidOperationException("weak run did not reach defeat result: " + title);
            var summary = root.GetNode<Label>("Screens/ResultScreen/Center/Panel/Layout/Summary").Text;
            if (summary.Contains("PlayerDefeat", StringComparison.Ordinal))
                throw new InvalidOperationException("defeat result leaked internal enum text");
            await CaptureCurrent("ResultDefeat.png");
        }
        finally { await DisposeRoot(root); }
    }

    private async Task<GameRoot> CreateRoot(string suffix)
    {
        var root = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
        root.SaveNamespace = "tests/visual-capture-" + suffix;
        AddChild(root);
        for (var frame = 0; frame < 10 && root.Content is null; frame++) await RenderFrame();
        if (root.Content is null) throw new InvalidOperationException("GameRoot content gate did not finish");
        return root;
    }

    private static void StartNewRunFromMenu(GameRoot root)
    {
        Press(root, "Screens/MainMenuScreen/Center/Panel/Menu/NewRunButton");
    }

    private static RunApplication StartCommanderRun(GameRoot root, ulong seed, int floor, bool strong)
    {
        var app = GetApplication(root);
        if (!app.Meta.UnlockedHeroIds.Contains(CommanderId)) app.Meta.UnlockedHeroIds.Add(CommanderId);
        InvokePrivate(root, "ShowHeroSelection");
        var heroScreen = root.GetNode<HeroSelectScreen>("Screens/HeroSelectScreen");
        heroScreen.Preview(CommanderId);
        Press(root, "Screens/HeroSelectScreen/Margin/Layout/Content/HeroDetailPanel/Layout/DeployButton");
        var run = app.ActiveRun ?? throw new InvalidOperationException("commander run did not start");
        run.Seed = seed;
        run.FloorIndex = floor;
        run.PendingNode = false;
        run.HeroHealthRatio = 1f;
        if (strong)
        {
            foreach (var soldier in app.Content.Catalog.Soldiers.Select(entry => entry.StableId))
            {
                if (run.Roster.Count >= 9) break;
                app.Recruit(soldier);
            }
            foreach (var item in new[] { "item_commander_map", "item_aegis_standard", "item_last_banner", "item_field_rations" })
                app.GrantItem(item);
            foreach (var item in run.Items) item.Stacks = 4;
            run.Gold = 100;
            for (var slot = 0; slot < 6; slot++) app.ClearDeploymentSlot(slot);
            var deployed = run.Roster.OrderByDescending(unit => unit.HealthRatio).Take(6).ToArray();
            for (var slot = 0; slot < deployed.Length; slot++) app.EquipDeployment(deployed[slot].InstanceId, slot);
        }
        InvokePrivate(root, "ShowTower");
        return app;
    }

    private static void ResolveBattle(GameRoot root, bool useCommands)
    {
        var battle = root.GetNode<BattleScreenController>("Screens/BattleScreen");
        var command = root.GetNode<Button>("Screens/BattleScreen/Margin/Layout/Hud/HeroCommandHud/Layout/Mana/CommandButton");
        for (var iteration = 0; !battle.IsEnding && iteration < 2000; iteration++)
        {
            if (useCommands && iteration is 0 or 21 or 42) command.EmitSignal(BaseButton.SignalName.Pressed);
            battle._Process(1.0);
            var presenters = typeof(BattleScreenController)
                .GetField("_presenters", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(battle) as System.Collections.Generic.Dictionary<string, UnitContentRoot>;
            if (presenters is not null)
                foreach (var presenter in presenters.Values)
                    presenter.GetNode<UnitMotionPresentationComponent>("UnitMotionPresentationComponent")._Process(1.0);
        }
        if (!battle.IsEnding) throw new InvalidOperationException("battle did not settle within visual-capture budget");
        InvokePrivate(battle, "CompleteEndTransition");
    }

    private async Task CaptureScreen(GameRoot root, string screenName)
    {
        RequireVisible(root, screenName);
        await RenderFrame();
        await CaptureCurrent(screenName + ".png");
    }

    private async Task CaptureCurrent(string fileName)
    {
        await RenderFrame();
        var image = GetViewport().GetTexture().GetImage();
        var resolvedName = $"UI_{image.GetWidth()}x{image.GetHeight()}_{fileName}";
        var error = image.SavePng($"{ProjectSettings.GlobalizePath(OutputPath)}/{resolvedName}");
        if (error != Error.Ok) throw new InvalidOperationException($"capture {resolvedName}: {error}");
    }

    private async Task RenderFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }

    private async Task DisposeRoot(GameRoot root)
    {
        if (root.GetParent() is not null) root.GetParent().RemoveChild(root);
        root.Free();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static RunApplication GetApplication(GameRoot root)
    {
        var field = typeof(GameRoot).GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(root) as RunApplication ?? throw new InvalidOperationException("GameRoot application unavailable");
    }

    private static void InvokePrivate(object root, string methodName, params object[] arguments)
    {
        var method = root.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(root.GetType().FullName, methodName);
        method.Invoke(root, arguments);
    }

    private static void RequireVisible(GameRoot root, string screenName)
    {
        if (!root.GetNode<Control>("Screens/" + screenName).Visible)
            throw new InvalidOperationException(screenName + " is not the active flow screen");
    }

    private static void RejectInternalEnumText(Container choices)
    {
        string[] internalNames =
        [
            "Common", "Uncommon", "Rare", "Legendary", "Vanguard", "Fighter", "Ranged", "Support",
            "Assassin", "Summoner", "Artillery", "Boss", "PlayerVictory", "PlayerDefeat", "Timeout"
        ];
        foreach (var card in choices.GetChildren().OfType<ChoiceCard>())
        foreach (var internalName in internalNames)
            if (card.SearchText.Contains(internalName, StringComparison.Ordinal))
                throw new InvalidOperationException($"player-facing card leaked internal enum {internalName}");
        foreach (var card in choices.GetChildren().OfType<UnitChoiceCard>())
        foreach (var internalName in internalNames)
            if (card.SearchText.Contains(internalName, StringComparison.Ordinal))
                throw new InvalidOperationException($"player-facing unit card leaked internal enum {internalName}");
    }

    private static void PressChoice(Node root, string path, string stableId)
    {
        var children = root.GetNode<Container>(path).GetChildren();
        BaseButton? card = children.OfType<ChoiceCard>()
            .FirstOrDefault(candidate => candidate.StableId == stableId && !candidate.Disabled);
        card ??= children.OfType<UnitChoiceCard>()
            .FirstOrDefault(candidate => candidate.StableId == stableId && !candidate.Disabled);
        (card ?? throw new InvalidOperationException($"choice {stableId} missing at {path}"))
            .EmitSignal(BaseButton.SignalName.Pressed);
    }

    private static void Press(Node root, string path) => root.GetNode<Button>(path).EmitSignal(BaseButton.SignalName.Pressed);

    private static void AdvanceMotionFrames(UnitMotionPresentationComponent motion, int count, double delta)
    {
        for (var frame = 0; frame < count; frame++) motion._Process(delta);
    }

    private sealed record MovementProbe(
        UnitContentRoot Unit,
        UnitMotionPresentationComponent Motion,
        UnitAnimationComponent Animation,
        Vector2 Right,
        Vector2 Corner,
        Vector2 LeftCorner);
}
