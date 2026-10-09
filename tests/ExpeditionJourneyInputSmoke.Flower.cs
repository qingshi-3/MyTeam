using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.UI;

public partial class ExpeditionJourneyInputSmoke
{
    private async Task FlowerLedgerReview()
    {
        _step = "flower / committed ledger visibility";
        var saved = JsonSerializer.Serialize(State());
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "接续已完成花灵战斗的隔离征程");
        var panel = await OpenFlowerWorkbench();
        var ledger = panel.GetNode<Label>("%GrowthLedger");
        var scroll = panel.GetNode<ScrollContainer>("Scroll");
        bool VisibleLedger() => ledger.IsVisibleInTree() && scroll.GetGlobalRect().Encloses(ledger.GetGlobalRect());
        for (var step = 0; step < 24 && !VisibleLedger(); step++)
        {
            var point = scroll.GetGlobalRect().GetCenter();
            _inputs.Add(new { kind = "wheel-down", target = scroll.GetPath().ToString(), x = point.X, y = point.Y });
            _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
            _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.WheelDown, Pressed = true });
            _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.WheelDown, Pressed = false });
            await Frames(4);
        }
        Require(VisibleLedger() && ledger.Text.Contains("攻击 +4") && ledger.Text.Contains("生命 +40"), "真实滚动可见已结算培养账本");
        await Capture("flower-ledger-visible.png");
        await Click(_game!.GetNode<ArmyOverviewController>("ArmyOverview").GetNode<Button>("%CloseButton"));
        Require(JsonSerializer.Serialize(State()) == saved, "补验账本不重复培养或修改存档");
        Check("actual-wheel-reveals-ledger-without-state-change");
    }

    private async Task FlowerJourney()
    {
        // Only real new-run/cancel/card inputs select a naturally offered flower.
        // No fixed pool, edited hero or manufactured battle result is injected.
        var entered = false;
        for (var attempt = 0; attempt < 16 && !entered; attempt++)
        {
            _step = "flower / natural opening";
            await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => Screens.HeroSelection.IsVisibleInTree(), "六选二");
            await Frames(45);
            var cards = Descendants<HeroLibraryTile>(Screens.HeroSelection).Where(card => card.IsVisibleInTree()).ToArray();
            Require(cards.Length == 6, "真实六名候选");
            var flower = cards.FirstOrDefault(card => card.StableId == "prototype_u16");
            var priority = new[] { "prototype_u02", "prototype_u04", "prototype_u07", "prototype_u11" };
            var partner = priority.Select(id => cards.FirstOrDefault(card => card.StableId == id)).FirstOrDefault(card => card is not null);
            if (flower is null || partner is null)
            {
                await Click(Screens.HeroSelection.GetNode<Button>("Margin/Layout/Footer/BackButton"));
                await Until(() => Screens.MainMenu.IsVisibleInTree(), "返回菜单重新开局");
                continue;
            }
            await Click(partner);
            await Click(flower);
            await Capture("flower-opening.png");
            await Click(Screens.HeroSelection.GetNode<Button>("%ConfirmOpening"));
            await Until(() => Screens.Expedition.IsVisibleInTree(), "花灵进入真实大陆");
            entered = true;
        }
        Require(entered, "有界真实开局中出现花灵和前排；这不是供给率评估");
        if (_breedingOnly)
        {
            await AcquireGrowthHero("prototype_u25");
            await DeployGrowthReserve("prototype_u25");
        }
        var flowerId = State().Roster.Single(hero => hero.ContentId == "prototype_u16").InstanceId;
        var partnerId = State().Roster.Single(hero => _breedingOnly ? hero.ContentId == "prototype_u25" : hero.ContentId != "prototype_u16").InstanceId;
        var panel = await OpenFlowerWorkbench();
        Require(panel.GetNode<Label>("%ProductionPreview").Text.Contains("未"), "未保存分配应明确显示未指定");
        await FlowerSelect(panel.GetNode<OptionButton>("%ProductionTarget"), partnerId);
        Require(State().Roster.Single(hero => hero.InstanceId == flowerId).Growth.ProductionTargetInstanceId.Length == 0,
            "选择菜单只是草稿，不自动保存生产对象");
        await Capture("flower-unassigned-preview.png");
        await Key(Godot.Key.Escape);
        await Until(() => !_game!.GetNode<ArmyOverviewController>("ArmyOverview").IsOpen, "关闭工坊");
        await Service();
        Require(State().Roster.All(hero => hero.Growth.AddedAttack == 0 && hero.Growth.AddedMaxHealth == 0),
            "未应用分配的有效服务也不会凭默认下拉框发放成长");
        Check("unassigned-service-zero-gain");
        if (_breedingOnly)
            Require(State().Roster.Single(hero => hero.InstanceId == partnerId).Growth.BreedingPoints == 1,
                "花灵未分配时，古树仍在真实付费服务结算一次育种");

        panel = await OpenFlowerWorkbench();
        await FlowerSelect(panel.GetNode<OptionButton>("%ProductionMode"), "Cultivation");
        await FlowerSelect(panel.GetNode<OptionButton>("%ProductionTarget"), partnerId);
        await Click(panel.GetNode<Button>("%ApplyAssignment"));
        Require(State().Roster.Single(hero => hero.InstanceId == flowerId).Growth.ProductionTargetInstanceId == partnerId,
            "真实点击保存树种对象");
        Require(panel.GetNode<Label>("%ProductionPreview").Text.Contains("已分配"), "展示实际已保存分配");
        if (_breedingOnly)
            Require(panel.GetNode<Label>("%ProductionPreview").Text.Contains("攻击永久 +5") &&
                panel.GetNode<Label>("%GrowthLedger").Text.Contains("育种 1"), "工坊显示旧育种和下一份5/50树种");
        await Capture("flower-assigned.png");
        await Key(Godot.Key.Escape);
        await Until(() => !_game!.GetNode<ArmyOverviewController>("ArmyOverview").IsOpen, "关闭已分配工坊");
        await PreviewAndBattle();
        var grown = State().Roster.Single(hero => hero.InstanceId == partnerId).Growth;
        var expectedSeed = _breedingOnly ? 5 : 4;
        Require(grown.AddedAttack == expectedSeed && grown.AddedMaxHealth == expectedSeed * 10,
            "真实战果按提交时旧育种给指定对象一次树种");
        if (_breedingOnly) Require(grown.BreedingPoints == 2, "本次5/50树种结算后古树才增加第二点育种");
        Require(State().Roster.Single(hero => hero.InstanceId == flowerId).Growth.AddedAttack == 0,
            "非受益者不误领树种");
        var flowerReport = Screens.Battle.TerminalResult!.Units.Single(unit => unit.ContentId == "prototype_u16" && !unit.IsTemporary);
        Require(flowerReport.HealingDone > 0 && flowerReport.EffectiveHealingEvents > 0, "正式正常速度战斗实际产生花灵有效治疗");
        File.WriteAllText(Path.Combine(_output, "flower-battle-result.json"), JsonSerializer.Serialize(Screens.Battle.TerminalResult,
            new JsonSerializerOptions { WriteIndented = true }));
        Check("real-battle-healing-and-fixed-pair-growth");

        panel = await OpenFlowerWorkbench();
        var ledger = panel.GetNode<Label>("%GrowthLedger").Text;
        Require(ledger.Contains($"攻击 +{expectedSeed}") && ledger.Contains($"生命 +{expectedSeed * 10}"), "真实培养账本显示两个实际量");
        if (_breedingOnly)
        {
            Require(ledger.Contains("育种 2") && panel.GetNode<Label>("%ProductionPreview").Text.Contains("攻击永久 +6"),
                "旧对象获得5/50，下一份树种6/60分开显示");
            var ledgerNode = panel.GetNode<Label>("%GrowthLedger");
            var ledgerScroll = panel.GetNode<ScrollContainer>("Scroll");
            for (var attempt = 0; attempt < 20 && !ledgerScroll.GetGlobalRect().Encloses(ledgerNode.GetGlobalRect()); attempt++)
            {
                var wheelPosition = ledgerScroll.GetGlobalRect().GetCenter();
                _inputs.Add(new { kind = "wheel-down", target = ledgerScroll.GetPath().ToString(), x = wheelPosition.X, y = wheelPosition.Y });
                _stage.Push(new InputEventMouseMotion { Position = wheelPosition, GlobalPosition = wheelPosition });
                _stage.Push(new InputEventMouseButton { Position = wheelPosition, GlobalPosition = wheelPosition, ButtonIndex = MouseButton.WheelDown, Pressed = true });
                _stage.Push(new InputEventMouseButton { Position = wheelPosition, GlobalPosition = wheelPosition, ButtonIndex = MouseButton.WheelDown, Pressed = false });
                await Frames(4);
            }
            Require(ledgerScroll.GetGlobalRect().Encloses(ledgerNode.GetGlobalRect()), "真实滚动后育种和培养账本在可见窗口内");
        }
        await Capture("flower-ledger.png");
        await FlowerSelect(panel.GetNode<OptionButton>("%ProductionTarget"), flowerId);
        await Click(panel.GetNode<Button>("%ApplyAssignment"));
        Require(State().Roster.Single(hero => hero.InstanceId == flowerId).Growth.ProductionTargetInstanceId == flowerId,
            "可以真实改投自己，旧核心已获属性保留");
        await Capture("flower-retarget.png");
        await Key(Godot.Key.Escape);
        await Until(() => !_game!.GetNode<ArmyOverviewController>("ArmyOverview").IsOpen, "关闭工坊后恢复地图");
        await VerifyResume();
        panel = await OpenFlowerWorkbench();
        Require(panel.GetNode<OptionButton>("%ProductionTarget").GetItemMetadata(
            panel.GetNode<OptionButton>("%ProductionTarget").Selected).AsString() == flowerId,
            "继续后工坊恢复已保存的新对象");
        Require(State().Roster.Single(hero => hero.InstanceId == partnerId).Growth.AddedMaxHealth == expectedSeed * 10,
            "恢复不重复发放旧目标收益");
        if (_breedingOnly)
            Require(State().Roster.Single(hero => hero.InstanceId == partnerId).Growth.BreedingPoints == 2 &&
                panel.GetNode<Label>("%GrowthLedger").Text.Contains("育种 2"), "继续保留育种2，未重复结算");
        await Capture("flower-continued.png");
        Check("retarget-ledger-and-continue-preserve-committed-facts");
    }

    private async Task<GrowthWorkbenchPanel> OpenFlowerWorkbench()
    {
        await Click(Screens.Expedition.GetNode<Button>("%ArmyButton"));
        var army = _game!.GetNode<ArmyOverviewController>("ArmyOverview");
        await Until(() => army.IsOpen, "军团窗口打开");
        var panel = army.GrowthPanel;
        for (var attempt = 0; attempt < 3 && !panel.IsVisibleInTree(); attempt++)
            await Click(army.GetNode<Button>("%PageToggle"));
        Require(panel.IsVisibleInTree(), "真实页面按钮打开成长工坊");
        await Frames(4);
        return panel;
    }

    private async Task FlowerSelect(OptionButton menu, string id)
    {
        var index = Enumerable.Range(0, menu.ItemCount).FirstOrDefault(i => menu.GetItemMetadata(i).AsString() == id, -1);
        Require(index >= 0, "实际下拉选项包含目标：" + id);
        // Reuse the native keyboard opening path from AbilityPrototypeInputSmoke
        // and verify that the real popup both opens and confirms its selection.
        menu.GrabFocus();
        await Frames(2);
        var current = menu.Selected;
        await Key(Godot.Key.Space);
        Require(menu.GetPopup().Visible, "键盘展开实际选项菜单");
        for (var step = current; step > index; step--) await Key(Godot.Key.Up);
        for (var step = current; step < index; step++) await Key(Godot.Key.Down);
        await Key(Godot.Key.Enter);
        await Until(() => !menu.GetPopup().Visible, "确认选项后菜单收起");
        Require(menu.Selected == index, "键盘导航选中实际选项：" + id);
    }
}
