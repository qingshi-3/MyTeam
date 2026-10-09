using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Expedition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class ExpeditionJourneyInputSmoke
{
    private bool _v1Journey, _v1JourneyResume;
    private string _v1PriorDirectory = "", _v1ResumeRunId = "", _v1ResumeSave = "";
    private object? _v1PriorEvidence;
    private readonly List<object> _v1Actions = [];
    private readonly List<string> _v1Clips = [];
    private int _v1Ascensions, _v1Equipment, _v1Relics;
    private bool _v1Resumed, _v1Assigned;
    private string _v1ViewedSite = "";
    private readonly HashSet<string> _v1UnavailableSites = new(StringComparer.Ordinal);
    private static readonly string[] V1CoreIds = ["prototype_u02", "prototype_u07", "prototype_u19", "prototype_u11", "prototype_u21", "prototype_u17", "prototype_u18", "prototype_u22"];
    private UnitDefinition V1Unit(string id) => _game!.Content!.TryGet(id, out var entry) && entry.Definition is UnitDefinition unit
        ? unit : throw new InvalidOperationException("Published hero missing: " + id);
    private static bool V1Front(UnitDefinition unit) => unit.Role is UnitRole.Vanguard or UnitRole.Fighter;

    private async Task V1Journey()
    {
        var metaBefore = _save.LoadMeta();
        await V1Opening();
        await AssertInitialPartyVisible();
        await V1PlayStages(0, metaBefore);
        Require(_terminal == "victory" && _v1Resumed && _v1Ascensions > 0 && _v1Equipment > 0 && _v1Relics > 0,
            "三首领胜利、真实升阶发现、拖穿装备、可读遗物与冷启动继续全部完成");
        Check("v1-three-stages-eighteen-paid-sites-three-boss-victories-final-meta-save-deletion");
    }

    private async Task V1PlayStages(int firstStage, MetaProgressDto metaBefore)
    {
        for (var stage = firstStage; stage < 3; stage++)
        {
            Require(State().Expedition is { RemainingActions: 6 } && State().Expedition!.StageIndex == stage,
                "正式阶段开始补足六次行动");
            var kinds = new List<TowerNodeType>();
            while (State().Expedition!.RemainingActions > 0)
            {
                await V1ManageArmy();
                var site = await V1ChooseSite(stage, kinds);
                var kind = Definition.Sites.Single(d => d.StableId == site.DefinitionId).Kind;
                var before = State();
                _step = $"v1 stage {stage + 1} action {7-before.Expedition!.RemainingActions} {kind}";
                if (kind is TowerNodeType.Combat or TowerNodeType.Elite) await V1Battle(site, false, stage, metaBefore);
                else if (!await V1Service(site, kind)) continue;
                var after = State();
                Require(after.Expedition!.StageIndex == stage && after.Expedition.RemainingActions == before.Expedition.RemainingActions - 1,
                    "真实地点处理恰扣一次行动，不以免费预览凑满六次");
                _v1UnavailableSites.Clear();
                kinds.Add(kind);
                _v1Actions.Add(new { Stage=stage+1, Kind=kind.ToString(), site.SiteId, SiteBiome=site.BiomeIndex, Before=before.Expedition.RemainingActions,
                    After=after.Expedition.RemainingActions, after.Gold, after.CurrentRunHealth, after.BattleNumber });
                V1SaveEvidence($"stage-{stage+1}-action-{kinds.Count}");
                if (!_v1Resumed && (stage > 0 || kinds.Count >= 4))
                { await VerifyResume(); _v1Resumed = true; }
            }
            Require(kinds.Count == 6, "每阶段六次真实有效行动");
            await V1ManageArmy();
            await V1Battle(null, true, stage, metaBefore);
            Require(_bossOutcomes.Count == stage-firstStage+1 && _bossOutcomes[^1].EndsWith(":PlayerVictory", StringComparison.Ordinal),
                "本阶段首领必须实际胜利才能声称全征程完成");
        }
    }

    // Prior files and saved domain state are read only. Every resumed mutation goes
    // through the same visible Continue/reward/map controls as a new journey.
    private void V1ValidatePriorEvidence(ActiveRunDto? run)
    {
        _step = "v1 resume / verify prior evidence and unchanged active save";
        using var evidence = JsonDocument.Parse(File.ReadAllText(Path.Combine(_v1PriorDirectory, "evidence.json")));
        var prior = evidence.RootElement;
        Require(prior.GetProperty("Namespace").GetString() == _namespace && prior.GetProperty("V1Journey").GetBoolean() &&
            !prior.GetProperty("Passed").GetBoolean(), "前序证据必须是指定 namespace 的中断全征程。");
        var priorRun = JsonSerializer.Deserialize<ActiveRunDto>(File.ReadAllText(Path.Combine(_v1PriorDirectory, "v1-failure-save.json")))
            ?? throw new InvalidOperationException("前序失败快照为空。");
        Require(run is { Expedition: { StageIndex: 1, RemainingActions: 0 }, OpeningRecruitment: null,
            Action: { Phase: RunActionPhase.Rewards, IsStageBoss: true, RewardCursor: 1 } } && !run.TerminalVictory,
            "只接续第二首领胜利后剩余奖励，不能以其他存档替代该真实局。");
        var active = run!;
        _v1ResumeSave = JsonSerializer.Serialize(active);
        Require(_v1ResumeSave == JsonSerializer.Serialize(priorRun), "现存完整存档必须与原 failure snapshot 一致，不能改写状态以恢复。");
        _v1ResumeRunId = active.RunId;
        var actions = prior.GetProperty("V1Actions").EnumerateArray().ToArray();
        Require(actions.Length == 12, "前序必须有十二次真实付费地点记录。");
        for (var i = 0; i < actions.Length; i++)
        {
            var stage = i / 6 + 1; var ordinal = i % 6 + 1; var action = actions[i];
            Require(action.GetProperty("Stage").GetInt32() == stage && action.GetProperty("Before").GetInt32() == 7 - ordinal &&
                action.GetProperty("After").GetInt32() == 6 - ordinal, "前序行动链必须逐次扣费，不计预览或重复点击。");
            using var snapshot = JsonDocument.Parse(File.ReadAllText(Path.Combine(_v1PriorDirectory, $"v1-stage-{stage}-action-{ordinal}-save.json")));
            Require(snapshot.RootElement.GetProperty("RunId").GetString() == active.RunId &&
                snapshot.RootElement.GetProperty("ContentFingerprint").GetString() == active.ContentFingerprint &&
                snapshot.RootElement.GetProperty("Expedition").GetProperty("RemainingActions").GetInt32() == 6 - ordinal,
                "前序逐行动存档需归属同一 Run 与内容版本。");
        }
        Require(prior.GetProperty("BossOutcomes").EnumerateArray().Select(x => x.GetString())
            .SequenceEqual(new[] { "stage-1:PlayerVictory", "stage-2:PlayerVictory" }), "前序两场首领必须真实胜利。");
        var files = new[] { "evidence.json", "v1-failure-save.json", "v1-stage-1-boss-result.json", "v1-stage-2-boss-result.json" };
        for (var stage = 1; stage <= 2; stage++)
        {
            using var result = JsonDocument.Parse(File.ReadAllText(Path.Combine(_v1PriorDirectory, $"v1-stage-{stage}-boss-result.json")));
            var identity = result.RootElement.GetProperty("Identity");
            var origin = identity.GetProperty("OriginKey").GetString() ?? "";
            Require(result.RootElement.GetProperty("Outcome").GetInt32() == (int)BattleOutcome.PlayerVictory &&
                origin.StartsWith(active.RunId + ":action:", StringComparison.Ordinal), "前序真实战果必须来自同一 Run。");
            if (stage == 2) Require(origin == active.Action!.Source.ActionId &&
                identity.GetProperty("BattleNumber").GetInt32() + 1 == active.BattleNumber,
                "待领奖必须属于已提交一次的第二首领战果。");
        }
        Require(prior.GetProperty("V1Ascensions").GetInt32() > 0 && prior.GetProperty("V1Equipment").GetInt32() > 0 &&
            prior.GetProperty("V1RelicReads").GetInt32() > 0 && prior.GetProperty("V1Resumed").GetBoolean(),
            "前序已留存真实升阶、装备、遗物阅读和冷启动交互；本次不虚构重复覆盖。");
        _v1PriorEvidence = new { Directory = _v1PriorDirectory, active.RunId, active.ContentFingerprint, PaidActions = 12, BossVictories = 2,
            Files = files.Select(file => new { File = file, Sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(_v1PriorDirectory, file)))) }).ToArray(),
            Interactions = "Original evidence.json only; old reward click count is not a committed-reward count" };
        Check("v1-resume-prior-twelve-actions-two-bosses-same-run-exact-save-verified");
    }

    private async Task V1ResumeJourney()
    {
        _step = "v1 resume / Continue pending second-boss reward";
        var before = State();
        var metaBefore = _save.LoadMeta();
        File.WriteAllText(Path.Combine(_output, "v1-resume-meta-before.json"), JsonSerializer.Serialize(metaBefore, new JsonSerializerOptions { WriteIndented = true }));
        V1SaveEvidence("resume-start");
        Require(JsonSerializer.Serialize(before) == _v1ResumeSave, "启动菜单不改写原待领奖存档。");
        var button = Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton");
        Require(!button.Disabled, "正常主菜单必须可继续既有存档。");
        await Click(button);
        await Until(() => Screens.Reward.IsVisibleInTree(), "Continue 恢复第二首领剩余奖励");
        Require(JsonSerializer.Serialize(State()) == _v1ResumeSave, "Continue 不重抽、不重付、不重复结算原首领。");
        await Capture("v1-resume-pending-reward.png");
        await V1Rewards("v1-resume-stage-2-boss");
        await Until(() => Screens.Expedition.IsVisibleInTree(), "剩余奖励完成后进入第三阶段");
        Require(State() is { Action: null, Expedition: { StageIndex: 2, RemainingActions: 6 } } &&
            State().RunId == _v1ResumeRunId && State().BattleNumber == before.BattleNumber,
            "只完成原奖励事务补六行动，不增加战斗结算次数或替换 Run。");
        _stageRefills.Add(new { FromStage = 2, ToStage = 3, ActionsBefore = 0, ActionsAfter = 6, ResumedReward = true });
        _v1Resumed = true;
        V1SaveEvidence("resume-stage-3-start");
        await V1PlayStages(2, metaBefore);
        Require(_v1Actions.Count == 6 && _bossOutcomes.SequenceEqual(new[] { "stage-3:PlayerVictory" }) && _terminal == "victory",
            "恢复段必须真实完成第三阶段六行动和最终胜利，不能合并成当次十八行动。");
        Check("v1-resumed-same-run-prior-12-plus-current-6-prior-2-plus-final-victory-meta-once");
    }

    private async Task V1Opening()
    {
        _step = "v1 / current natural six choices";
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
        await Until(() => Screens.HeroSelection.IsVisibleInTree(), "正式六选二"); await Frames(45);
        var cards = Descendants<HeroLibraryTile>(Screens.HeroSelection).Where(c => c.IsVisibleInTree()).ToArray();
        Require(cards.Length == 6, "当次自然六人候选，不重开筛种子");
        var front = cards.Where(c => V1Front(V1Unit(c.StableId)))
            .OrderByDescending(c => V1Unit(c.StableId).MaxHealth + V1Unit(c.StableId).Armor * 30).FirstOrDefault()
            ?? cards.OrderByDescending(c => V1Unit(c.StableId).MaxHealth).First();
        var second = cards.Where(c => c != front).OrderByDescending(c => c.StableId == "prototype_u16")
            .ThenByDescending(c => V1Unit(c.StableId).AttackRange >= 2)
            .ThenByDescending(c => V1Unit(c.StableId).AttackDamage).First();
        await Click(front); await Click(second); await Capture("v1-opening.png");
        await Click(Screens.HeroSelection.GetNode<Button>("%ConfirmOpening"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "真实开局地图");
        V1SaveEvidence("opening");
    }

    private async Task<ExpeditionCellView> V1ChooseSite(int stage, List<TowerNodeType> handled)
    {
        // Route and site priorities use only fog-redacted, currently visible map facts.
        for (var attempt = 0; attempt < 128; attempt++)
        {
            var map = Map(); var routes = KnownRoutes(map); var run = State();
            bool Eligible(ExpeditionCellView cell) => cell.BiomeIndex == stage || cell.BiomeIndex < stage && Definition.Sites.Single(s => s.StableId == cell.DefinitionId).Kind == TowerNodeType.Combat;
            int Priority(ExpeditionCellView cell)
            {
                var kind = Definition.Sites.Single(s => s.StableId == cell.DefinitionId).Kind;
                if(cell.BiomeIndex < stage) return 2; // An earlier visible roadblock can still need a paid clear.
                return kind switch
                {
                    TowerNodeType.Recruitment => run.Roster.Count < Math.Min(run.CurrentPopulation, 4 + stage * 2) ? 100 : 15,
                    TowerNodeType.Shop => !handled.Contains(kind) && run.Gold >= 8 ? 90 : -1,
                    TowerNodeType.Event => !handled.Contains(kind) ? 85 : 20,
                    TowerNodeType.Rest => run.CurrentRunHealth < run.MaximumRunHealth * .6f ? 110 : !handled.Contains(kind) ? 45 : 20,
                    TowerNodeType.Combat => 70,
                    TowerNodeType.Elite => run.Deployment.Count(id => id.Length > 0) >= 5 ? 50 : 5,
                    _ => -1
                };
            }
            var reachable = map.Cells.Where(c => c.Revealed && c.SiteId.Length > 0 && Eligible(c) && !_v1UnavailableSites.Contains(c.SiteId) && !c.Handled && !c.Cleared)
                .Select(c => (Cell:c, Approach:Neighbors(map.Size,c.Index).Append(c.Index).Where(routes.ContainsKey)
                    .OrderBy(i => routes[i].Length).Select(i => (int?)i).FirstOrDefault(), Rank:Priority(c)))
                .Where(c => c.Approach is not null && c.Rank >= 0).OrderByDescending(c => c.Rank)
                .ThenBy(c => routes[c.Approach!.Value].Length).ThenBy(c => c.Cell.Index).FirstOrDefault();
            var localFrontier = Frontier(map).Where(i => map.Cells[i].BiomeIndex == stage).FirstOrDefault(-1);
            var exploration = localFrontier >= 0 ? localFrontier : reachable.Cell is { BiomeIndex: var region } && region < stage ? Frontier(map).FirstOrDefault(-1) : -1;
            if (reachable.Cell is not null && reachable.Rank < 80 && exploration >= 0)
            { await MoveTo(exploration); continue; }
            if (reachable.Cell is not null)
            {
                await MoveTo(reachable.Approach!.Value);
                return reachable.Cell;
            }
            var frontier = Frontier(map).OrderBy(i => map.Cells[i].BiomeIndex == stage ? 0 : 1).FirstOrDefault(-1);
            Require(frontier >= 0, "当前已知道路无边缘或可处理地点；保留天然路阻，不读取迷雾后的库存");
            await MoveTo(frontier);
        }
        throw new InvalidOperationException("全征程有界探索未找到当前地域可达地点");
    }

    private async Task<bool> V1Service(ExpeditionCellView site, TowerNodeType kind)
    {
        _v1ViewedSite = site.SiteId;
        await SelectCell(site.Index); var saved = JsonSerializer.Serialize(State());
        await Click(Screens.Expedition.GetNode<Button>("%InspectButton"));
        await Until(() => Screens.Recruitment.IsVisibleInTree() || Screens.Shop.IsVisibleInTree() || Screens.Reward.IsVisibleInTree(), "真实地点选项");
        Control screen = Screens.Recruitment.IsVisibleInTree() ? Screens.Recruitment : Screens.Shop.IsVisibleInTree() ? Screens.Shop : Screens.Reward;
        await Frames(24); await Capture($"v1-{State().Expedition!.StageIndex+1}-{site.SiteId}-choices.png");
        Require(JsonSerializer.Serialize(State()) == saved, "预览不扣费或修改既有冻结库存");
        var cards = Descendants<RunOfferChoiceCard>(screen).Where(c => c.IsVisibleInTree() && !c.IsQueuedForDeletion() && !c.ConfirmButton.Disabled).ToArray();
        if(cards.Length == 0)
        {
            var cancel = kind == TowerNodeType.Shop ? Screens.Shop.GetNode<Button>("Margin/Layout/LeaveButton") : screen.GetNode<Button>("Center/Panel/Layout/ContinueButton");
            await Click(cancel); await Until(() => Screens.Expedition.IsVisibleInTree(), "不可选库存真实免费返回");
            Require(JsonSerializer.Serialize(State()) == saved, "无合法商品/候选时真实取消零扣费");
            _v1UnavailableSites.Add(site.SiteId); Check("v1-visible-site-unavailable-cancel-free-choose-another"); return false;
        }
        var choice = cards.OrderByDescending(c => V1OfferScore(c.StableId)).First();
        await Click(choice.ConfirmButton); await Frames(20);
        if (kind == TowerNodeType.Shop)
        {
            // Same-store additional purchases are optional; never require a specific item to appear.
            var extra = Descendants<RunOfferChoiceCard>(Screens.Shop).FirstOrDefault(c => c.IsVisibleInTree() && !c.IsQueuedForDeletion() && !c.ConfirmButton.Disabled);
            if (extra is not null && State().Gold >= 8) { await Click(extra.ConfirmButton); await Frames(20); }
            await Click(Screens.Shop.GetNode<Button>("Margin/Layout/LeaveButton"));
        }
        else if (Screens.Reward.IsVisibleInTree()) await V1Rewards("service");
        await Until(() => Screens.Expedition.IsVisibleInTree(), "服务真实结算返回地图"); return true;
    }

    private float V1OfferScore(string choiceId)
    {
        var state = State();
        var offer = state.Action?.CurrentOffer ?? state.Expedition?.Sites.SingleOrDefault(s => s.SiteId == _v1ViewedSite)?.ServiceOffer;
        var choice = offer?.Choices.FirstOrDefault(c => c.StableId == choiceId);
        if (choice is null) return 0;
        var safe = choice.SuccessChance >= 1 ? 1000 : 0;
        if (!_game!.Content!.TryGet(choice.ContentId, out var entry)) return safe +
            (state.CurrentRunHealth < state.MaximumRunHealth * .7f && choice.Operations.Any(o => o.Kind == RunOperationKind.RecoverRunHealth) ? 2000 : 0);
        if (entry.Definition is UnitDefinition unit)
        {
            var needFront = State().Roster.Count(h => V1Front(V1Unit(h.ContentId))) < 2;
            return (needFront && V1Front(unit) ? 10000 : 0) + (unit.Id == "prototype_u16" ? 5000 : 0)
                + unit.MaxHealth + unit.AttackDamage * 10;
        }
        return safe;
    }

    private async Task V1DeployArmy()
    {
        // Deployment is edited through the ordinary boss preview, without committing battle.
        await Click(Screens.Expedition.GetNode<Button>("%BossButton"));
        await Until(() => Screens.Deployment.IsVisibleInTree() && !Screens.IsTransitioning, "正式后备布阵"); await Frames(8);
        foreach (var hero in State().Roster.OrderByDescending(h => V1Front(V1Unit(h.ContentId))).ToArray())
        {
            var cells = Descendants<DeploymentCell>(Screens.Deployment).ToArray();
            var deployed = State().Deployment.Contains(hero.InstanceId);
            var ownCell = cells.FirstOrDefault(c => c.PieceId == hero.InstanceId);
            if (deployed && (!V1Front(V1Unit(hero.ContentId)) || ownCell is null || ownCell.Cell.X == BattlefieldLayout.PlayerDeploymentColumns - 1)) continue;
            if (!deployed && State().Deployment.Count(id => id.Length > 0) >= State().CurrentPopulation) continue;
            Control source = deployed ? ownCell! : Descendants<DeploymentUnitCard>(Screens.Deployment)
                .Single(c => c.InstanceId == hero.InstanceId).Portrait;
            var preferred = V1Front(V1Unit(hero.ContentId)) ? new Vector2I(BattlefieldLayout.PlayerDeploymentColumns - 1,3) : new Vector2I(1,2);
            var cell = Descendants<DeploymentCell>(Screens.Deployment).Where(c => BattlefieldLayout.IsPlayerDeploymentCell(c.Cell) && c.PieceId.Length == 0)
                .OrderBy(c => (c.Cell.Y%2 == preferred.Y%2 ? 0 : 20) + c.Cell.DistanceSquaredTo(preferred)).First();
            await ItemDrag(source, cell);
            Require(State().Deployment.Contains(hero.InstanceId) && Descendants<DeploymentCell>(Screens.Deployment).Any(c => c.Cell == cell.Cell && c.PieceId == hero.InstanceId), "真实拖放在所选合法格成功；已有部署身份不能掩盖被拒绝的移动");
        }
        await Click(Screens.Deployment.GetNode<Button>("%BackButton"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "保存布阵回地图");
    }

    private async Task V1ManageArmy()
    {
        await V1DeployArmy();
        var rosterBefore = State().Roster.Count;
        var panel = await OpenFlowerWorkbench();
        var producer = panel.GetNode<OptionButton>("%Producer");
        if (producer.ItemCount > 0)
        {
            var id = Enumerable.Range(0, producer.ItemCount).Select(i => producer.GetItemMetadata(i).AsString()).FirstOrDefault(id => State().Deployment.Contains(id) && State().Roster.Single(h => h.InstanceId == id).ContentId == "prototype_u16");
            if (id is not null)
            {
                await V1RevealGrowthControl(panel,producer); await FlowerSelect(producer,id); var mode = panel.GetNode<OptionButton>("%ProductionMode");
                var target = State().Roster.Where(h => State().Deployment.Contains(h.InstanceId)).OrderByDescending(h => V1Front(V1Unit(h.ContentId))).First();
                Require(Enumerable.Range(0,mode.ItemCount).Any(i => mode.GetItemMetadata(i).AsString() == "Cultivation"), "自然花灵正式作者提供树种培养；古树不生产");
                var preferredMode = "Cultivation";
                await V1RevealGrowthControl(panel,mode); await FlowerSelect(mode,preferredMode);
                var targetMenu=panel.GetNode<OptionButton>("%ProductionTarget");
                await V1RevealGrowthControl(panel,targetMenu); await FlowerSelect(targetMenu,target.InstanceId);
                var producerState=State().Roster.Single(h=>h.InstanceId==id).Growth;
                if(producerState.ProductionMode.ToString()!=preferredMode || producerState.ProductionTargetInstanceId!=target.InstanceId)
                {
                    var apply=panel.GetNode<Button>("%ApplyAssignment");await V1RevealGrowthControl(panel,apply);await Click(apply);
                }
                _v1Assigned = true;
                Require(State().Roster.Single(h => h.InstanceId == id).Growth.ProductionTargetInstanceId.Length > 0, "工坊正式保存生产分配");
            }
        }
        if (State().Growth!.Materials >= 4)
        {
            var core = State().Roster.Where(h => h.Growth.AscensionId.Length == 0 && V1CoreIds.Contains(h.ContentId))
                .OrderBy(h => Array.IndexOf(V1CoreIds,h.ContentId)).FirstOrDefault();
            if (core is not null)
            {
                var ascensionMenu=panel.GetNode<OptionButton>("%AscensionHero");
                await V1RevealGrowthControl(panel,ascensionMenu);await FlowerSelect(ascensionMenu,core.InstanceId);
                var button = panel.GetNode<Button>("%Ascend"); await V1RevealGrowthControl(panel,button);
                if (!button.Disabled)
                {
                    var beforeState = State(); var before = JsonSerializer.Serialize(beforeState); await Click(button); await Frames(8);
                    if (State().Growth!.PendingDiscovery is { } discovery)
                    {
                        Require(discovery.CandidateIds.Count == 3, "真实升阶冻结完整三名高阶候选");
                        Require(State().Roster.Single(h => h.InstanceId == core.InstanceId).Growth.AscensionId.Length > 0 &&
                            beforeState.Growth!.Materials + beforeState.Growth.CategoryMaterials.Values.Sum() - State().Growth!.Materials - State().Growth!.CategoryMaterials.Values.Sum() == 4, "升阶恰付四份真实材料并保存核心升阶");
                        V1SaveEvidence($"discovery-{_v1Ascensions+1}");
                        var choices = panel.GetNode<VBoxContainer>("%DiscoveryChoices").GetChildren().OfType<Button>().Where(b => !b.IsQueuedForDeletion()).ToArray();
                        Require(choices.Length == 3, "真实三选一按钮");
                        var selected = discovery.CandidateIds.Select((id,i) => (id,i)).OrderByDescending(x => V1Unit(x.id).MaxHealth + V1Unit(x.id).AttackDamage*10).First();
                        await V1RevealGrowthControl(panel,choices[selected.i]); await Capture($"v1-discovery-{_v1Ascensions+1}.png"); await Click(choices[selected.i]);
                        Require(State().Growth!.PendingDiscovery is null && State().Roster.Any(h => h.ContentId == selected.id), "正式发现归队"); _v1Ascensions++;
                    }
                    else Require(JsonSerializer.Serialize(State()) == before, "候选不足时完整回滚，不能扣材料后伪造发现");
                }
            }
        }
        await Key(Godot.Key.Escape); await Until(() => !_game!.GetNode<ArmyOverviewController>("ArmyOverview").IsOpen,"关闭工坊");
        if(State().Roster.Count > rosterBefore) await V1DeployArmy();
        if (State().EquipmentInventory.Count > 0)
        {
            await Click(Screens.Expedition.GetNode<Button>("%ArmyButton")); var army = _game!.GetNode<ArmyOverviewController>("ArmyOverview");
            await Until(() => army.IsOpen,"装备管理"); var view=army.GetNode<RosterLoadoutView>("%ArmyEquipmentPanel");
            for (var i=0;i<3&&!view.IsVisibleInTree();i++) await Click(army.GetNode<Button>("%PageToggle"));
            foreach (var item in State().EquipmentInventory.ToArray())
            {
                var hero=State().Roster.Where(h=>State().Deployment.Contains(h.InstanceId)).OrderBy(h=>h.Equipment.Count).First();
                await Click(ItemSelector(view,hero.InstanceId));
                var slot=Enumerable.Range(0,3).FirstOrDefault(i=>hero.Equipment.All(e=>e.SlotIndex!=i),-1); if(slot<0)break;
                var tile=InventoryTile(view,item.InstanceId);var label=tile.GetNode<Label>("NoIconLabel");
                Require(tile.Icon is not null || label.IsVisibleInTree() && label.Text.Length>0,"实际背包装备有图或可见中文名字");
                await ItemDrag(tile,HeroSlot(view,slot));Require(HeroHasItem(hero.InstanceId,item.InstanceId),"实际拖穿获得的装备");_v1Equipment++;
            }
            await Capture("v1-army-equipment.png"); await Key(Godot.Key.Escape); await Until(()=>!army.IsOpen,"关闭装备");
        }
        foreach(var icon in Descendants<RunRelicIcon>(_game!).Where(i=>i.IsVisibleInTree()))
        {
            var label=icon.GetNode<Label>("NoIconLabel");
            Require(icon.GetNode<TextureRect>("Artwork").Texture is not null || label.IsVisibleInTree() && !string.IsNullOrWhiteSpace(label.Text),"自然所得无图标遗物有可见中文名称");
            _v1Relics++;
        }
        await Capture("v1-map-growth-items.png");
    }

    private async Task V1Battle(ExpeditionCellView? site, bool boss, int stage, MetaProgressDto metaBefore)
    {
        _step=$"v1 stage {stage+1} / {(boss?"boss":"guard")} battle"; var before=State();
        if(boss)await Click(Screens.Expedition.GetNode<Button>("%BossButton"));
        else {await SelectCell(site!.Index);await Click(Screens.Expedition.GetNode<Button>("%InspectButton"));}
        await Until(()=>Screens.Deployment.IsVisibleInTree(),"正式整备");
        await Click(Screens.Deployment.GetNode<Button>("%StartBattleButton"));
        await Until(()=>Screens.Battle.IsVisibleInTree()||Screens.BattleReport.IsVisibleInTree(),"正式战斗",15);
        Require(Mathf.IsEqualApprox(Screens.Battle.SpeedScale,1),"新动作以x1正式时钟播放");
        _v1ViewedSite="";
        var prefix=$"v1-stage-{stage+1}-{(boss?"boss":"guard-"+before.BattleNumber)}";
        await V1Record(prefix,12);
        await Until(()=>Screens.BattleReport.IsVisibleInTree(),"正式战报",180);
        Require(Screens.Battle.LastRuntimeFailure.Length==0,"战斗无运行异常");
        var result=Screens.Battle.TerminalResult??throw new InvalidOperationException("无正式战果"); _outcome=result.Outcome.ToString();
        File.WriteAllText(Path.Combine(_output,prefix+"-result.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
        await Capture(prefix+"-report.png");
        if(boss)_bossOutcomes.Add($"stage-{stage+1}:{_outcome}");
        await Click(Screens.BattleReport.GetNode<Button>("%ReportContinue"));
        if(Screens.Result.IsVisibleInTree() || boss && (result.Outcome!=BattleOutcome.PlayerVictory || stage==2))
        {
            await VerifyBossTerminal(stage,result.Outcome,metaBefore);
            Require(boss && stage==2 && result.Outcome==BattleOutcome.PlayerVictory,"自然失败/超时终局已留证，不能记成全征程通关");return;
        }
        await V1Rewards(prefix);
        await Until(()=>Screens.Expedition.IsVisibleInTree(),"真实领奖回地图");
        Require(State().BattleNumber==before.BattleNumber+1,"真实战果仅结算一次");
        if(boss)
        {
            Require(result.Outcome==BattleOutcome.PlayerVictory && State().Expedition is {RemainingActions:6} && State().Expedition!.StageIndex==stage+1,"首领胜利推进补行动");
            _stageRefills.Add(new{FromStage=stage+1,ToStage=stage+2,ActionsBefore=before.Expedition!.RemainingActions,ActionsAfter=6});
        }
    }

    private async Task V1Rewards(string prefix)
    {
        for(var group=0;group<12&&!Screens.Expedition.IsVisibleInTree();group++)
        {
            await Until(()=>Screens.Reward.IsVisibleInTree()||Screens.Expedition.IsVisibleInTree()||Screens.Result.IsVisibleInTree(),"真实奖励或地图");
            Require(!Screens.Result.IsVisibleInTree(),"普通行动已自然终局，停止且保留实际失败证据");if(Screens.Expedition.IsVisibleInTree())return;
            await Capture($"{prefix}-reward-{group+1}.png");
            var card=Descendants<RunOfferChoiceCard>(Screens.Reward).Where(c=>c.IsVisibleInTree()&&!c.IsQueuedForDeletion()&&!c.ConfirmButton.Disabled).OrderByDescending(c=>V1OfferScore(c.StableId)).FirstOrDefault();
            if(card is not null)await Click(card.ConfirmButton);else await Click(Screens.Reward.GetNode<Button>("Center/Panel/Layout/ContinueButton"));_rewardGroups++;
        }
    }

    private async Task V1RevealGrowthControl(GrowthWorkbenchPanel panel,Control target)
    {
        var scroll=panel.GetNode<ScrollContainer>("Scroll");
        for(var attempt=0;attempt<40&&!scroll.GetGlobalRect().Encloses(target.GetGlobalRect());attempt++)
        {
            var point=scroll.GetGlobalRect().GetCenter();
            var direction=target.GetGlobalRect().Position.Y<scroll.GetGlobalRect().Position.Y?MouseButton.WheelUp:MouseButton.WheelDown;
            _inputs.Add(new{kind="growth-wheel",target=target.GetPath().ToString(),direction=direction.ToString()});
            _stage.Push(new InputEventMouseMotion{Position=point,GlobalPosition=point});
            _stage.Push(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=direction,Pressed=true});
            _stage.Push(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=direction,Pressed=false});await Frames(4);
        }
        Require(target.IsVisibleInTree()&&scroll.GetGlobalRect().Encloses(target.GetGlobalRect()),"双向真实滚动使工坊操作控件在可见区");
    }

    private void V1SaveEvidence(string name)=>File.WriteAllText(Path.Combine(_output,"v1-"+name+"-save.json"),JsonSerializer.Serialize(State(),new JsonSerializerOptions{WriteIndented=true}));
    private async Task V1Record(string prefix,double seconds)
    {
        var folder=Path.Combine(_output,prefix+"-frames");Directory.CreateDirectory(folder);var times=new List<ulong>();var ticks=new List<int>();
        var deadline=Time.GetTicksMsec()+(ulong)(seconds*1000);ulong previous=0;
        while(Time.GetTicksMsec()<deadline&&Screens.Battle.IsVisibleInTree())
        {
            await Frames(1);var now=Time.GetTicksMsec();if(now-previous<84)continue;previous=now;
            using var image=_stage.Viewport.GetTexture().GetImage();image.Resize(960,540,Image.Interpolation.Lanczos);
            Require(image.SavePng(Path.Combine(folder,$"frame-{times.Count:D3}.png"))==Error.Ok,"保存正式正常速度帧");times.Add(now);ticks.Add(Screens.Battle.TickIndex);
        }
        Require(times.Count>0,"实际战斗正常速度录像非空");var concat=new List<string>{"ffconcat version 1.0"};
        for(var i=0;i<times.Count;i++){concat.Add($"file 'frame-{i:D3}.png'");concat.Add("duration "+(i+1<times.Count?(times[i+1]-times[i])/1000d:.084).ToString("0.000000",System.Globalization.CultureInfo.InvariantCulture));}
        concat.Add($"file 'frame-{times.Count-1:D3}.png'");File.WriteAllLines(Path.Combine(folder,"frames.ffconcat"),concat);
        File.WriteAllText(Path.Combine(folder,"samples.json"),JsonSerializer.Serialize(times.Select((time,i)=>new{Time=time,Tick=ticks[i]})));_v1Clips.Add(prefix+"-frames/frames.ffconcat");
    }
}
