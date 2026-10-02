using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Growth;
using TowerAutobattler.Presentation;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class GrowthEntryInputSmoke : Node
{
    private UiInputStage _stage = null!;
    public override async void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Contains("--portraits-only"))
        {
            await VerifyPortraitEntry();
            return;
        }
        var exit = 0;
        GameRoot? game = null;
        var testNamespace = $"tests/growth-entry/{Guid.NewGuid():N}";
        var testSave = new SaveService(testNamespace);
        var productionSave = new SaveService("growth-journey");
        var productionBefore = JsonSerializer.Serialize(productionSave.LoadActiveRun());
        try
        {
            Require(DisplayServer.GetName() != "headless", "真实入口输入烟测需要渲染驱动");
            _stage = new UiInputStage(this, true);
            _stage.Resize(new Vector2I(960, 540));
            _stage.Viewport.GuiEmbedSubwindows = true;
            var mainScene = ProjectSettings.GetSetting("application/run/main_scene").AsString();
            Require(mainScene == "res://scenes/app/GrowthGameRoot.tscn", "默认主场景接入成长玩法");
            game = GD.Load<PackedScene>(mainScene).Instantiate<GameRoot>();
            game.SaveNamespace = testNamespace;
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "默认主场景发布内容");
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var menu = screens.MainMenu;
            Require(menu.IsVisibleInTree(), "从默认真实主菜单开始");
            var journey = menu.GetNode<Button>("Center/Panel/Menu/GrowthJourneyButton");
            Require(!journey.IsVisibleInTree() && journey.Text.Contains("旧版征程"), "旧版只在开发工具中提供兼容入口");
            foreach (var button in Descendants<Button>(menu).Where(button => button.IsVisibleInTree()))
                Require(_stage.Viewport.GetVisibleRect().Encloses(button.GetGlobalRect()), "默认菜单按钮完整可见：" + button.Text);
            await Capture("main-menu.png");
            await Click(menu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
            Require(journey.IsVisibleInTree(), "旧版存档的兼容入口可以展开");
            foreach (var button in Descendants<Button>(menu).Where(button => button.IsVisibleInTree()))
                Require(_stage.Viewport.GetVisibleRect().Encloses(button.GetGlobalRect()), "工具菜单按钮完整可见：" + button.Text);
            await Click(menu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
            await Click(menu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "默认新征程打开真实六选二");
            Require(testSave.LoadActiveRun()!.Growth is not null, "默认新局拥有成长状态");
            Require(Descendants<HeroLibraryTile>(screens.HeroSelection).Count(tile => tile.IsVisibleInTree()) == 6,
                "默认新局提供六名真实候选");

            // A fixed seed controls only the legitimate opening draw, not resources,
            // growth, battle results or spell stock. Continue then uses the real input path.
            game.QueueFree();
            await Frames(5);
            var publication = await GrowthContentPackage.CreateReadyAsync(this);
            var package = publication.Package ?? throw new InvalidOperationException(string.Join(';', publication.Report.CoreErrors));
            var fixture = new RunApplication(package.Content, testSave, package.Project,
                growthRules: GrowthContentPackage.LoadRules(package.Content));
            Require(fixture.BeginOpeningRecruitment(2), "合法seed2开局存档");
            game = GD.Load<PackedScene>(mainScene).Instantiate<GameRoot>();
            game.SaveNamespace = testNamespace;
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "确定种子开局继续");
            screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "真实继续恢复六选二");
            var candidates = Descendants<HeroLibraryTile>(screens.HeroSelection).Where(tile => tile.IsVisibleInTree()).ToArray();
            Require(candidates.Length == 6, "固定种子仍为六选二");
            await Click(candidates.Single(tile => tile.StableId == "hero_mx25"));
            await Click(candidates.Single(tile => tile.StableId == "hero_mx21"));
            var confirm = screens.HeroSelection.GetNode<Button>("%ConfirmOpening");
            Require(!confirm.Disabled, "两次真实候选点击允许确认");
            await Click(confirm);
            await Until(() => screens.Tower.IsVisibleInTree(), "成长开局进入当前路线");
            await Click(Route(screens, "Combat"));
            await Until(() => screens.Deployment.IsVisibleInTree(), "普通战直接进入整备");
            Require(!screens.Tower.GetNode<OpponentChoicePanel>("OpponentChoicePanel").IsVisibleInTree()
                && testSave.LoadActiveRun()!.SelectedOpponentOptionId.Length == 0, "普通战不提供具体选敌");
            Require(testSave.LoadActiveRun()!.Growth!.Research == 0, "自然开局没有夹具研究");
            await Click(screens.Deployment.GetNode<Button>("%StartBattleButton"));
            await Until(() => screens.BattleReport.IsVisibleInTree(), "第一场真实自动战斗完成", 18000);
            Require(testSave.LoadActiveRun()!.BattleNumber == 1 && testSave.LoadActiveRun()!.Growth!.Research == 1,
                "首战结算一次并自然生产1研究");
            Require(testSave.LoadActiveRun()!.PendingOffer is { RewardCategory: RunRewardCategory.Equipment },
                "首战胜利进入独立装备奖励组");
            await Click(screens.BattleReport.GetNode<Button>("%ReportContinue"));
            await Until(() => screens.Reward.IsVisibleInTree(), "战报继续打开装备奖励");
            Require(RewardTitle(screens).Contains("装备"), "装备奖励标题可辨认");
            await Frames(3);
            var firstReward = Descendants<RunOfferChoiceCard>(screens.Reward).First(card => card.IsVisibleInTree());
            var rewardScroll = screens.Reward.GetNode<ScrollContainer>("Center/Panel/Layout/OfferBody/ChoiceScroll");
            Require(rewardScroll.GetGlobalRect().Encloses(firstReward.ConfirmButton.GetGlobalRect()),
                "首行奖励领取按钮无需滚动即完整可见");
            await Capture("equipment-reward.png");
            await Click(Descendants<RunOfferChoiceCard>(screens.Reward).First(card => card.IsVisibleInTree()).ConfirmButton);
            Require(testSave.LoadActiveRun()!.EquipmentInventory.Count == 1
                && testSave.LoadActiveRun()!.PendingOffer is { RewardCategory: RunRewardCategory.Relic },
                "领装备不占遗物名额");
            Require(RewardTitle(screens).Contains("遗物"), "领取后直接进入独立遗物组");
            var savedRelicOffer = JsonSerializer.Serialize(testSave.LoadActiveRun()!.PendingOffer);
            var savedGold = testSave.LoadActiveRun()!.Gold;
            game.QueueFree();
            await Frames(5);
            game = GD.Load<PackedScene>(mainScene).Instantiate<GameRoot>();
            game.SaveNamespace = testNamespace;
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "奖励中途重新启动默认主场景");
            screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
            await Until(() => screens.Reward.IsVisibleInTree(), "真实继续恢复剩余遗物组");
            Require(JsonSerializer.Serialize(testSave.LoadActiveRun()!.PendingOffer) == savedRelicOffer
                && testSave.LoadActiveRun()!.EquipmentInventory.Count == 1 && testSave.LoadActiveRun()!.Gold == savedGold,
                "读档不重发装备、不重抽遗物、不重复基础收益");
            await Capture("relic-reward.png");
            await Click(Descendants<RunOfferChoiceCard>(screens.Reward).First(card => card.IsVisibleInTree()).ConfirmButton);
            await Until(() => screens.Tower.IsVisibleInTree(), "完成独立奖励返回路线");
            Require(testSave.LoadActiveRun()!.Items.Count == 1 && testSave.LoadActiveRun()!.Growth!.Research == 1,
                "遗物独立领取且领奖不重复生产");
            await Click(Route(screens, "Rest"));
            await Until(() => screens.Reward.IsVisibleInTree() && RewardTitle(screens).Contains("休整"), "第二节点休整");
            await Click(Descendants<RunOfferChoiceCard>(screens.Reward).Single(card => card.StableId == "gold").ConfirmButton);
            await Until(() => screens.Tower.IsVisibleInTree(), "休整结算返回路线");
            Require(testSave.LoadActiveRun()!.Growth!.Research == 2, "休整自然生产第2研究");
            await Click(Route(screens, "Event"));
            await Until(() => screens.Reward.IsVisibleInTree() && RewardTitle(screens).Contains("旅途"), "第三节点事件");
            await Click(Descendants<RunOfferChoiceCard>(screens.Reward).Single(card => card.StableId == "safe").ConfirmButton);
            await Until(() => screens.Tower.IsVisibleInTree(), "事件结算返回路线");
            Require(testSave.LoadActiveRun()!.Growth!.Research == 3, "事件自然生产第3研究");
            var combat = Route(screens, "Elite");
            await Click(combat);
            var opponents = screens.Tower.GetNode<OpponentChoicePanel>("OpponentChoicePanel");
            await Until(() => opponents.IsVisibleInTree(), "精英战真实点击打开具体对手");
            var cards = opponents.GetNode<Container>("%OpponentCards").GetChildren().OfType<ChoiceCard>().ToArray();
            Require(_stage.Viewport.GetVisibleRect().Encloses(opponents.GetNode<Button>("%ConfirmOpponent").GetGlobalRect()),
                "确认按钮完整位于视口内");
            Require(cards.Length >= 2 && cards.Select(card => card.StableId).Distinct().Count() == cards.Length, "至少两个不同且稳定的具体敌阵");
            var candidatesBefore = cards.Select(card => card.StableId).ToArray();
            var beforeBrowse = JsonSerializer.Serialize(testSave.LoadActiveRun());
            await Click(cards[1]);
            Require(opponents.SelectedOptionId == candidatesBefore[1]
                && !string.IsNullOrEmpty(opponents.GetNode<Label>("%OpponentPressure").Text)
                && opponents.GetNode<Label>("%OpponentReward").Text.Contains("金币"), "真实候选点击更新压力和收益");
            var enemyPicker = opponents.GetNode<OptionButton>("%EnemyPicker");
            if (enemyPicker.ItemCount > 1)
            {
                await SelectIndexThroughKeys(enemyPicker, 1);
                Require(enemyPicker.Selected == 1, "真实键盘切换敌人详情");
            }
            await Capture("opponent-choice.png");
            await Click(opponents.GetNode<Button>("%BackToRoute"));
            Require(JsonSerializer.Serialize(testSave.LoadActiveRun()) == beforeBrowse, "浏览与返回不写存档或消费资源");
            await Click(combat);
            cards = opponents.GetNode<Container>("%OpponentCards").GetChildren().OfType<ChoiceCard>().Where(card => !card.IsQueuedForDeletion()).ToArray();
            Require(cards.Select(card => card.StableId).SequenceEqual(candidatesBefore), "退出重进不会刷新对手");
            await Click(cards[1]);
            await Click(opponents.GetNode<Button>("%ConfirmOpponent"));
            await Until(() => screens.Deployment.IsVisibleInTree(), "确认具体对手后进入真实整备");
            var selectedRun = testSave.LoadActiveRun()!;
            Require(selectedRun.SelectedOpponentOptionId == candidatesBefore[1] && selectedRun.PendingNode, "选定对手持久化成功");
            GD.Print("GROWTH_ENTRY_PHASE opponent-confirmed");

            // Resume the chosen elite with naturally earned research.
            game.QueueFree();
            await Frames(5);
            Require(selectedRun.Growth!.Research == 3, "第二战法术研究来自真实节点生产");
            game = GD.Load<PackedScene>(mainScene).Instantiate<GameRoot>();
            game.SaveNamespace = testNamespace;
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "恢复成长征程发布内容");
            screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
            await Until(() => screens.Deployment.IsVisibleInTree(), "真实继续按钮恢复已经选定的敌阵");
            Require(testSave.LoadActiveRun()!.SelectedOpponentOptionId == candidatesBefore[1], "读档保持具体敌阵 ID");
            GD.Print("GROWTH_ENTRY_PHASE selected-opponent-resumed");
            Require(_stage.Viewport.GuiGetFocusOwner() is not null, "进入备战后保留键盘焦点");

            var army = game.GetNode<ArmyOverviewController>("ArmyOverview");
            await Click(screens.Deployment.GetNode<Button>("%GrowthSpellButton"));
            Require(army.IsOpen, "战前法术入口真实点击打开工坊");
            await Frames(24);
            var pages = army.GetNode<TabContainer>("%Pages");
            Require(pages.CurrentTab == 2 && army.GrowthPanel.IsVisibleInTree(), "直达入口打开成长工坊，实际页=" + pages.CurrentTab);
            GD.Print("GROWTH_ENTRY_PHASE spell-workbench-open");
            var growthPanel = army.GrowthPanel;
            await SelectThroughKeys(growthPanel.GetNode<OptionButton>("%Spell"), "suppress_front");
            await Click(growthPanel.GetNode<Button>("%CraftSpell"));
            Require(testSave.LoadActiveRun()!.Growth!.SpellInventory.GetValueOrDefault("suppress_front") == 1, "真实输入兑换压制法术");
            await Click(growthPanel.GetNode<Button>("%EquipSpell"));
            Require(testSave.LoadActiveRun()!.Growth!.EquippedSpellId == "suppress_front", "真实输入预设法术与友方目标");
            await Click(growthPanel.GetNode<Button>("%UnequipSpell"));
            Require(testSave.LoadActiveRun()!.Growth!.EquippedSpellId.Length == 0
                && testSave.LoadActiveRun()!.Growth!.SpellInventory["suppress_front"] == 1, "取消预设不损失库存");
            await Click(growthPanel.GetNode<Button>("%EquipSpell"));
            var presetCopy = growthPanel.GetNode<Label>("%EquippedSpell");
            for (var ancestor = presetCopy.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
                if (ancestor is ScrollContainer scroll) { scroll.EnsureControlVisible(presetCopy); await Frames(3); }
            await Capture("spell-preset.png");
            await Key(Godot.Key.Escape);
            await Until(() => !army.IsOpen, "Escape 完成军团抽屉关闭");
            Require(!army.IsOpen && _stage.Viewport.GuiGetFocusOwner() is not null, "Escape 关闭工坊并恢复焦点");

            var start = screens.Deployment.GetNode<Button>("%StartBattleButton");
            Require(start.IsVisibleInTree() && !start.Disabled, "真实备战开始按钮可用");
            await Click(start);
            await Until(() => screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle, "第二场真实战斗接入成长开战事务");
            Require(screens.Battle.TacticalPoints == 0 && !screens.Battle.GetNode<Control>("%TacticalCommandHud").IsVisibleInTree(),
                "成长模式纯自动战斗不暴露战中指令");
            var started = testSave.LoadActiveRun()!.Growth!;
            Require(started.SpellInventory["suppress_front"] == 0 && started.PendingNode!.ConsumedSpellId == "suppress_front",
                "正式开战只消费一次并冻结本场法术");
            Require(testSave.LoadActiveRun()!.BattleNumber == 1, "第二战开战前只有一场已结算战斗");
            await Frames(90);
            await Capture("battle-start.png");
            screens.Battle.SetPaused(true);
            screens.Battle.StopBattle();
            Require(JsonSerializer.Serialize(productionSave.LoadActiveRun()) == productionBefore,
                "唯一测试命名空间没有读取或覆盖 growth-journey 正式存档");
            GD.Print("GROWTH_ENTRY_INPUT_OK default-menu opening ordinary-direct real-first-battle independent-rewards resume natural-research elite-choice spell-craft second-battle-consume isolated-save viewport=960x540");
        }
        catch (Exception error)
        {
            exit = 1;
            GD.PrintErr("GROWTH_ENTRY_INPUT_FAILED " + error);
            if (_stage is not null) await Capture("input-failure.png");
        }
        finally
        {
            game?.QueueFree();
            await Frames(3);
            testSave.DeleteActiveRun();
        }
        GetTree().Quit(exit);
    }

    private async Task VerifyPortraitEntry()
    {
        var exit = 0;
        GameRoot? game = null;
        var testNamespace = $"tests/portrait-entry/{Guid.NewGuid():N}";
        var save = new SaveService(testNamespace);
        var production = new SaveService("growth-journey");
        var before = JsonSerializer.Serialize(production.LoadActiveRun());
        try
        {
            Require(DisplayServer.GetName() != "headless", "portrait entry requires rendered input");
            _stage = new UiInputStage(this, true);
            var scene = ProjectSettings.GetSetting("application/run/main_scene").AsString();
            Require(scene == "res://scenes/app/GrowthGameRoot.tscn", "check the actual default game");
            game = GD.Load<PackedScene>(scene).Instantiate<GameRoot>();
            game.SaveNamespace = testNamespace;
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "default game content published");
            foreach (var entry in game.Content!.Catalog.AllEntries())
                if (entry.Definition is TowerAutobattler.Content.UnitDefinition { Portrait.Illustration: not null } unit)
                    Require(unit.Portrait!.CardIllustration is null,
                        entry.StableId + ": default-game units must load optimized full artwork");
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            // Bootstrap reveals the menu and collapses its developer entries.
            // Let the authored containers settle before sampling click geometry.
            await Frames(3);
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "actual new-run opening");
            var tiles = Descendants<HeroLibraryTile>(screens.HeroSelection).Where(tile => tile.IsVisibleInTree()).ToArray();
            Require(tiles.Length == 6, "six actual default-game candidates");
            var openingCount = CheckVisiblePortraits(screens.HeroSelection);
            await CapturePortraitEntry("opening.png");
            await Click(tiles[0]);
            await Click(tiles[1]);
            await Click(screens.HeroSelection.GetNode<Button>("%ConfirmOpening"));
            await Until(() => screens.Tower.IsVisibleInTree(), "actual opening confirmation");
            var army = game.GetNode<ArmyOverviewController>("ArmyOverview");
            await Click(army.GetNode<Button>("%SummaryButton"));
            await Until(() => army.IsOpen, "actual heroes and equipment entry");
            await Frames(24);
            var rosterCount = CheckVisiblePortraits(army);
            await CapturePortraitEntry("heroes-equipment.png");
            await Click(army.GetNode<Button>("%CloseButton"));
            Require(!army.IsOpen, "close actual roster");
            Require(JsonSerializer.Serialize(production.LoadActiveRun()) == before, "formal save unchanged");
            GD.Print($"GROWTH_PORTRAIT_ENTRY_OK opening={openingCount} roster={rosterCount} actual-input full-art no-old-cutouts isolated-save native-mouse-unchanged");
        }
        catch (Exception error)
        {
            exit = 1; GD.PrintErr("GROWTH_PORTRAIT_ENTRY_FAILED " + error);
            if (_stage is not null) await CapturePortraitEntry("failure.png");
        }
        finally
        {
            game?.QueueFree();
            await Frames(3);
            save.DeleteActiveRun();
        }
        GetTree().Quit(exit);
    }

    private static int CheckVisiblePortraits(Node screen)
    {
        var portraits = Descendants<UnitPortrait>(screen)
            .Where(portrait => portrait.IsVisibleInTree() && portrait.IsShowingIllustration).ToArray();
        Require(portraits.Length > 0, "actual screen has visible illustrations");
        foreach (var portrait in portraits)
        {
            var source = portrait.Definition!;
            Require(source.CardIllustration is null &&
                portrait.GetNode<TextureRect>("%PortraitIllustration").Texture == source.Illustration,
                source.StableId + ": actual screen selects optimized original PNG");
            GD.Print($"GROWTH_PORTRAIT_TEXTURE {source.StableId} {source.Illustration!.ResourcePath}");
        }
        return portraits.Length;
    }

    private async Task CapturePortraitEntry(string file)
    {
        const string folder = "res://.godot/ui-review/portrait-entry-20261001";
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(folder));
        await Frames(2);
        RenderingServer.ForceDraw(false);
        using var image = _stage.Viewport.GetTexture().GetImage();
        Require(image.SavePng(ProjectSettings.GlobalizePath(folder + "/" + file)) == Error.Ok, "capture portrait entry");
    }

    private static ChoiceCard Route(AppScreenHost screens, string id) =>
        Descendants<ChoiceCard>(screens.Tower).Single(card => card.IsVisibleInTree() && card.StableId == id);
    private static string RewardTitle(AppScreenHost screens) =>
        screens.Reward.GetNode<Label>("Center/Panel/Layout/Title").Text;

    private async Task Click(Control target)
    {
        for (var ancestor = target.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
            if (ancestor is ScrollContainer scroll) { scroll.EnsureControlVisible(target); await Frames(3); }
        Require(target.IsVisibleInTree(), "点击目标不可见：" + target.GetPath());
        var point = target.GetGlobalRect().GetCenter();
        Require(_stage.Viewport.GetVisibleRect().HasPoint(point), "点击目标超出视口：" + target.GetPath());
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(4);
    }
    private async Task SelectThroughKeys(OptionButton target, string id)
    {
        var index = Enumerable.Range(0, target.ItemCount).Single(value => target.GetItemMetadata(value).AsString() == id);
        await SelectIndexThroughKeys(target, index);
        Require(target.GetSelectedMetadata().AsString() == id, "真实键盘选择法术：" + id);
    }
    private async Task SelectIndexThroughKeys(OptionButton target, int index)
    {
        for (var ancestor = target.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
            if (ancestor is ScrollContainer scroll) { scroll.EnsureControlVisible(target); await Frames(3); }
        target.GrabFocus();
        await Key(Godot.Key.Space);
        await Key(Godot.Key.Home);
        for (var i = 0; i < index; i++) await Key(Godot.Key.Down);
        await Key(Godot.Key.Enter);
    }
    private async Task Capture(string file)
    {
        const string folder = "res://design-discussion/03-run-mechanisms/journey-and-pressure/threat-preview/artifacts/runtime";
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(folder));
        await Frames(2);
        // An off-desktop macOS host may stop automatic swap draws; render the
        // real isolated viewport explicitly without moving or focusing the desktop.
        RenderingServer.ForceDraw(false);
        Require(_stage.Viewport.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath(folder + "/" + file)) == Error.Ok,
            "保存真实输入画面：" + file);
    }
    private async Task Key(Key key)
    {
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await Frames(4);
    }
    private async Task Until(Func<bool> predicate, string message, int maxFrames = 600)
    {
        for (var frame = 0; frame < maxFrames && !predicate(); frame++) await Frames(1);
        Require(predicate(), message);
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren()) { if (child is T match) yield return match; foreach (var nested in Descendants<T>(child)) yield return nested; }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
