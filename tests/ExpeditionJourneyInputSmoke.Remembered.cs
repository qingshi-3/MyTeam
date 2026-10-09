using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Run;
using TowerAutobattler.Project;
using TowerAutobattler.UI;

public partial class ExpeditionJourneyInputSmoke
{
    private async Task RememberedJourney()
    {
        var entered = false;
        for (var attempt = 0; attempt < 16 && !entered; attempt++)
        {
            await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => Screens.HeroSelection.IsVisibleInTree(), "铭记路线六选二");
            await Frames(45);
            var cards = Descendants<HeroLibraryTile>(Screens.HeroSelection).Where(card => card.IsVisibleInTree()).ToArray();
            var hunter = cards.FirstOrDefault(card => card.StableId == "prototype_u11");
            var front = cards.FirstOrDefault(card => card.StableId == "prototype_u02");
            if (hunter is null || front is null)
            {
                await Click(Screens.HeroSelection.GetNode<Button>("Margin/Layout/Footer/BackButton"));
                await Until(() => Screens.MainMenu.IsVisibleInTree(), "重新选择实际开局");
                continue;
            }
            await Click(hunter); await Click(front);
            await Capture("remembered-opening.png");
            await Click(Screens.HeroSelection.GetNode<Button>("%ConfirmOpening"));
            await Until(() => Screens.Expedition.IsVisibleInTree(), "猎手进入真实征程");
            entered = true;
        }
        Require(entered, "有界开局中获得猎手和前排，这不是抽取率统计");
        await AcquireGrowthHero("prototype_u13");
        await DeployGrowthReserve("prototype_u13");
        var elite = await ReachSite(TowerNodeType.Elite, new HashSet<string>(StringComparer.Ordinal));
        var hunterId = State().Roster.Single(hero => hero.ContentId == "prototype_u11").InstanceId;
        var growthBefore = State().Roster.Single(hero => hero.InstanceId == hunterId).Growth;
        await PreviewAndBattle(TowerNodeType.Elite, elite);
        var result = Screens.Battle.TerminalResult!;
        File.WriteAllText(Path.Combine(_output, "remembered-battle-result.json"), JsonSerializer.Serialize(result,
            new JsonSerializerOptions { WriteIndented = true }));
        Require(result.Outcome == BattleOutcome.PlayerVictory && result.RememberedGains.Length == 2,
            "自然精英战实际胜利并有完整铭记，不能伪造战果");
        var growthAfter = State().Roster.Single(hero => hero.InstanceId == hunterId).Growth;
        Require(growthAfter.AddedAttack == growthBefore.AddedAttack + 12 &&
            growthAfter.AddedMaxHealth == growthBefore.AddedMaxHealth + 120,
            "正式精英战胜利后恰好增加本场12/120铭记");
        Check("natural-recruitment-formal-battle-saves-one-remembered-pair");
        Require(State().Roster.Any(hero => hero.Growth.AscensionId.Length > 0), "此前已用真实材料完成一次升阶与发现");
        var panel = await OpenFlowerWorkbench();
        var ledger = panel.GetNode<Label>("%GrowthLedger");
        await RevealGrowthControl(panel, ledger);
        Require(ledger.Text.Contains("已保存铭记") && ledger.Text.Contains($"攻击 +{growthAfter.AddedAttack:0.##}") && ledger.Text.Contains($"生命 +{growthAfter.AddedMaxHealth:0.##}"),
            "工坊显示永久累计和其中铭记来源");
        await Capture("remembered-ledger.png");
        await Key(Godot.Key.Escape);
        await Until(() => !_game!.GetNode<ArmyOverviewController>("ArmyOverview").IsOpen, "关闭工坊");
        await VerifyResume();

        _step = "M1 / actual first Boss and stage transition";
        await Click(Screens.Expedition.GetNode<Button>("%BossButton"));
        await Until(() => Screens.Deployment.IsVisibleInTree(), "真实第一Boss整备");
        await Capture("remembered-boss-deployment.png");
        await Click(Screens.Deployment.GetNode<Button>("%StartBattleButton"));
        await Until(() => Screens.BattleReport.IsVisibleInTree(), "正常速度第一Boss实战", 180);
        var bossResult = Screens.Battle.TerminalResult!;
        Require(bossResult.Outcome == BattleOutcome.PlayerVictory && Screens.Battle.LastRuntimeFailure.Length == 0,
            "M1第一Boss必须实际胜利进入第二阶段");
        await Capture("remembered-boss-report.png");
        File.WriteAllText(Path.Combine(_output, "remembered-boss-result.json"), JsonSerializer.Serialize(bossResult,
            new JsonSerializerOptions { WriteIndented = true }));
        await Click(Screens.BattleReport.GetNode<Button>("%ReportContinue"));
        for (var group = 0; group < 4 && !Screens.Expedition.IsVisibleInTree(); group++)
        {
            await Until(() => Screens.Reward.IsVisibleInTree() || Screens.Expedition.IsVisibleInTree(), "Boss奖励返回地图");
            if (Screens.Expedition.IsVisibleInTree()) break;
            await Click(Screens.Reward.GetNode<Button>("Center/Panel/Layout/ContinueButton"));
        }
        await Until(() => Screens.Expedition.IsVisibleInTree(), "第一Boss后第二阶段");
        Require(State().Expedition is { StageIndex: 1, RemainingActions: 6 }, "完成第一段可体验征程");
        await Capture("remembered-stage-two.png");
        Check("material-ascension-discovery-real-first-boss-second-stage");
    }

    private async Task AcquireGrowthHero(string contentId)
    {
        // Take a visible early recruit, then earn the remaining material through
        // actual battles. Ascension discovery is the guaranteed M-pool source;
        // the particular hero is still conditional on the honest three choices.
        _step = "growth / actual early recruitment";
        var site = await ReachSite(TowerNodeType.Recruitment);
        await SelectCell(site.Index);
        await Click(Screens.Expedition.GetNode<Button>("%InspectButton"));
        await Until(() => Screens.Recruitment.IsVisibleInTree(), "查看正式招募库存");
        await Frames(24);
        var card = RecruitmentCards(Screens.Recruitment).First(value => !value.ConfirmButton.Disabled);
        var before = State();
        await Capture("growth-early-recruitment.png");
        await Click(card.ConfirmButton);
        await Until(() => Screens.Expedition.IsVisibleInTree(), "早期成员真实招募进入后备");
        Require(State().Roster.Count == before.Roster.Count + 1 &&
            State().Expedition!.RemainingActions == before.Expedition!.RemainingActions - 1,
            "早期招募只扣一次行动，不赠送指定中期英雄");
        while (State().Growth!.Materials < 4)
        {
            await PreviewAndBattle();
            Require(Screens.Battle.TerminalResult!.Outcome == BattleOutcome.PlayerVictory,
                "发现前的守关由正式战斗获胜");
        }
        Require(State().Expedition!.RemainingActions >= 2, "第4次有效行动后及时发现，为能力验证保留两次行动");
        var panel = await OpenFlowerWorkbench();
        var core = State().Roster.FirstOrDefault(hero => hero.ContentId == "prototype_u02") ?? State().Roster[0];
        await FlowerSelect(panel.GetNode<OptionButton>("%AscensionHero"), core.InstanceId);
        var materialsBefore = State().Growth!.Materials;
        await Click(panel.GetNode<Button>("%Ascend"));
        Require(State().Growth is { PendingDiscovery.CandidateIds.Count: 3 } && State().Growth!.Materials == materialsBefore - 4,
            "真实升阶支付四份材料并冻结三名发现候选");
        var offered = State().Growth!.PendingDiscovery!.CandidateIds.ToArray();
        File.WriteAllText(Path.Combine(_output, "growth-discovery-offer.json"), JsonSerializer.Serialize(new
        {
            Wanted = contentId, Offered = offered, Run = State()
        }, new JsonSerializerOptions { WriteIndented = true }));
        var choices = panel.GetNode<VBoxContainer>("%DiscoveryChoices").GetChildren().OfType<Button>().ToArray();
        Require(choices.Length == 3, "真实发现保留三选一");
        await Capture("growth-ascension-discovery.png");
        var selected = Array.IndexOf(offered, contentId);
        Require(selected >= 0, "本次真实发现未出现指定关键件；保留候选证据，不刷新或伪造选择。" + contentId);
        await Click(choices[selected]);
        Require(State().Growth!.PendingDiscovery is null && State().Roster.Any(hero => hero.ContentId == contentId),
            "真实领取关键件加入后备");
        await Key(Godot.Key.Escape);
        await Until(() => !_game!.GetNode<ArmyOverviewController>("ArmyOverview").IsOpen, "关闭发现工坊");
        Check("early-recruitment-four-actions-ascension-natural-three-choice-key-piece");
    }

    private async Task DeployGrowthReserve(string contentId, Vector2I? preferred = null)
    {
        var id = State().Roster.Single(hero => hero.ContentId == contentId).InstanceId;
        if (State().Deployment.Contains(id)) return;
        await Click(Screens.Expedition.GetNode<Button>("%BossButton"));
        await Until(() => Screens.Deployment.IsVisibleInTree(), "正式整备部署新后备");
        await Until(() => !Screens.IsTransitioning, "整备布局结束");
        await Frames(8);
        var card = Descendants<DeploymentUnitCard>(Screens.Deployment).Single(value => value.InstanceId == id);
        var start = card.Portrait.GetGlobalRect().GetCenter();
        var target = Descendants<DeploymentCell>(Screens.Deployment).Where(cell => BattlefieldLayout.IsPlayerDeploymentCell(cell.Cell) && cell.PieceId.Length == 0)
            .OrderBy(cell => cell.Cell.DistanceSquaredTo(preferred ?? new Vector2I(2, 3))).First();
        var end = target.GetGlobalRect().GetCenter();
        _inputs.Add(new { kind = "native-unit-drag", instanceId = id, from = start.ToString(), to = end.ToString() });
        _stage.Push(new InputEventMouseMotion { Position = start, GlobalPosition = start });
        _stage.Push(new InputEventMouseButton { Position = start, GlobalPosition = start, ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left, Pressed = true });
        for (var step = 1; step <= 20; step++)
        {
            var point = start.Lerp(end, step / 20f);
            _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point,
                Relative = (end - start) / 20, ButtonMask = MouseButtonMask.Left });
            await Frames(2);
        }
        Require(_stage.Viewport.GuiIsDragging(), "真实拖动启动Godot原生拖放");
        _stage.Push(new InputEventMouseButton { Position = end, GlobalPosition = end, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(24);
        Require(State().Deployment.Contains(id), "正式落点成功后新英雄上阵");
        if (contentId == "prototype_u13")
        {
            var links = Screens.Deployment.GetNode<Label>("%OpeningPartners");
            Require(links.IsVisibleInTree() && links.Text.Contains("忆形猎手"), "布阵显示幼龙实际开场配对");
            await Capture("remembered-opening-partners.png");
        }
        await Click(Screens.Deployment.GetNode<Button>("%BackButton"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "部署保存后返回地图");
    }

    private async Task RevealGrowthControl(GrowthWorkbenchPanel panel, Control target)
    {
        var scroll = panel.GetNode<ScrollContainer>("Scroll");
        for (var step = 0; step < 30 && !scroll.GetGlobalRect().Encloses(target.GetGlobalRect()); step++)
        {
            var point = scroll.GetGlobalRect().GetCenter();
            _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
            _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.WheelDown, Pressed = true });
            _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.WheelDown, Pressed = false });
            await Frames(4);
        }
        Require(scroll.GetGlobalRect().Encloses(target.GetGlobalRect()), "实际滚动后能看见成长账本");
    }
}
