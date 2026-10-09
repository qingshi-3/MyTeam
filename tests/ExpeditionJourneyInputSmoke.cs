using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Expedition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Real controls on the formal application, with no domain commands or fabricated
// battle results. Saved state is read only to assert committed outcomes. Navigation
// decisions use the same fog-redacted facts available to the player.
public partial class ExpeditionJourneyInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private GameRoot? _game;
    private SaveService _save = null!;
    private string _namespace = string.Empty;
    private string _output = string.Empty;
    private string _step = "boot";
    private readonly List<string> _checks = [];
    private readonly List<object> _inputs = [];
    private readonly List<string> _captures = [];
    private readonly List<(Image Image, ulong Time, int Tick)> _recording = [];
    private ulong _drawsAtStart;
    private ulong _drawsAtEnd;
    private int _rewardGroups;
    private string _outcome = "not-run";
    private bool _mapOnly;
    private bool _serviceOnly;
    private bool _resumeBoss;
    private bool _keyboardOnly;
    private bool _recruitmentOnly;
    private bool _recruitmentStyleOnly;
    private bool _entranceMotionOnly;
    private bool _mapLightOnly;
    private bool _recruitmentMotionOnly;
    private bool _restorationOnly;
    private bool _restorationCaptures;
    private bool _flowerOnly;
    private bool _flowerLedgerOnly;
    private bool _breedingOnly;
    private bool _rememberedOnly;
    private bool _itemsOnly;
    private bool _v1DefeatOnly;
    private int _keyboardSteps;
    private readonly List<string> _bossOutcomes = [];
    private readonly List<object> _stageRefills = [];
    private string _terminal = "not-run";

    private AppScreenHost Screens => _game!.GetNode<AppScreenHost>(_game.ScreenHostPath);
    private ActiveRunDto State() => _save.LoadActiveRun() ?? throw new InvalidOperationException("Isolated active save missing: " + _save.ActiveRunReadError);
    private ExpeditionMapView Map() => ExpeditionMapProjection.Create(State().Expedition ?? throw new InvalidOperationException("Map was not saved."));
    private ExpeditionProgressionDefinition Definition => (ExpeditionProgressionDefinition)_game!.ProjectDefinition!.Campaign!.Progression;

    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            Require(DisplayServer.GetName() != "headless", "需要实际渲染驱动，不能把无画面的运行记为输入验收。");
            _entranceMotionOnly = OS.GetCmdlineUserArgs().Contains("--entrance-motion-only");
            _mapLightOnly = OS.GetCmdlineUserArgs().Contains("--map-light-only");
            _recruitmentMotionOnly = OS.GetCmdlineUserArgs().Contains("--recruitment-motion-only");
            _flowerLedgerOnly = OS.GetCmdlineUserArgs().Contains("--flower-ledger-only");
            _breedingOnly = OS.GetCmdlineUserArgs().Contains("--breeding-only");
            _rememberedOnly = OS.GetCmdlineUserArgs().Contains("--remembered-only");
            _v1JourneyResume = OS.GetCmdlineUserArgs().Contains("--v1-journey-resume");
            Require(!(_v1JourneyResume && OS.GetCmdlineUserArgs().Contains("--v1-journey")), "新局与恢复全征程入口互斥。");
            _v1Journey = _v1JourneyResume || OS.GetCmdlineUserArgs().Contains("--v1-journey");
            _itemsOnly = OS.GetCmdlineUserArgs().Contains("--items-only");
            _v1DefeatOnly = OS.GetCmdlineUserArgs().Contains("--v1-defeat-only");
            _flowerOnly = _breedingOnly || _flowerLedgerOnly || OS.GetCmdlineUserArgs().Contains("--flower-only");
            _restorationOnly = _v1DefeatOnly || _v1Journey || _itemsOnly || _rememberedOnly || _flowerOnly || OS.GetCmdlineUserArgs().Contains("--restoration-only");
            _restorationCaptures = _flowerLedgerOnly || _restorationOnly && OS.GetCmdlineUserArgs().Contains("--restoration-captures");
            _mapOnly = OS.GetCmdlineUserArgs().Contains("--map-only");
            _serviceOnly = OS.GetCmdlineUserArgs().Contains("--service-only");
            _resumeBoss = OS.GetCmdlineUserArgs().Contains("--resume-boss");
            _keyboardOnly = OS.GetCmdlineUserArgs().Contains("--keyboard-only");
            _recruitmentStyleOnly = OS.GetCmdlineUserArgs().Contains("--recruitment-style-only");
            _recruitmentOnly = _recruitmentStyleOnly || OS.GetCmdlineUserArgs().Contains("--recruitment-only");
            Require(new[] { _mapOnly, _serviceOnly, _resumeBoss, _keyboardOnly, _recruitmentOnly, _entranceMotionOnly }.Count(value => value) <= 1, "局部验收入口不能同时指定。");
            Require(!_v1Journey || !(_mapOnly || _serviceOnly || _resumeBoss || _keyboardOnly || _recruitmentOnly || _entranceMotionOnly || _mapLightOnly || _recruitmentMotionOnly || _flowerOnly || _rememberedOnly || _itemsOnly || OS.GetCmdlineUserArgs().Contains("--restoration-only") || OS.GetCmdlineUserArgs().Contains("--restoration-captures")), "全征程入口不能混用其他局部参数");
            Require(!_v1DefeatOnly || !(_v1Journey || _itemsOnly || _flowerOnly || _rememberedOnly || _mapOnly || _serviceOnly || _resumeBoss || _keyboardOnly || _recruitmentOnly || _entranceMotionOnly || _mapLightOnly || _recruitmentMotionOnly || OS.GetCmdlineUserArgs().Contains("--restoration-only") || OS.GetCmdlineUserArgs().Contains("--restoration-captures")), "败战片段不能混用其他入口");
            var requestedNamespace = Argument("journey-namespace");
            Require(!_flowerLedgerOnly || !string.IsNullOrWhiteSpace(requestedNamespace), "培养账本补验需要既有隔离namespace。");
            Require(!_resumeBoss || !string.IsNullOrWhiteSpace(requestedNamespace), "接续 Boss 输入必须显式提供既有隔离 namespace。");
            Require(!_v1JourneyResume || !string.IsNullOrWhiteSpace(requestedNamespace) &&
                !string.IsNullOrWhiteSpace(Argument("prior-evidence")) && !string.IsNullOrWhiteSpace(Argument("output")),
                "恢复全征程必须显式指定既有 journey-namespace、prior-evidence 和独立 output。");
            _namespace = requestedNamespace ?? $"tests/expedition-journey/{Guid.NewGuid():N}";
            Require(_namespace.StartsWith("tests/expedition-journey/", StringComparison.Ordinal) && !_namespace.Contains(".."), "namespace 必须位于专用测试目录。");
            _output = ProjectSettings.GlobalizePath(Argument("output") ?? (_entranceMotionOnly
                ? "res://outputs/entrance-motion-20261004" : _recruitmentStyleOnly
                ? "res://outputs/expedition-recruitment-style-20261003" : _recruitmentOnly
                ? "res://outputs/expedition-recruitment-20261003" : _keyboardOnly
                ? "res://outputs/expedition-keyboard-20261003" : _resumeBoss
                ? "res://outputs/expedition-boss-input-20261003" : "res://outputs/expedition-journey-20261003"));
            if (_v1JourneyResume)
            {
                _v1PriorDirectory = Path.GetFullPath(ProjectSettings.GlobalizePath(Argument("prior-evidence")!)).TrimEnd(Path.DirectorySeparatorChar);
                var destination = Path.GetFullPath(_output).TrimEnd(Path.DirectorySeparatorChar);
                Require(destination != _v1PriorDirectory && !destination.StartsWith(_v1PriorDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal),
                    "恢复证据必须写入原目录之外，不能覆盖或混入前序证据。");
                Require(!File.Exists(Path.Combine(destination, "evidence.json")), "恢复输出必须尚无 evidence.json，保留既有证据。");
            }
            Directory.CreateDirectory(_output);
            _save = new SaveService(_namespace);
            var existingRun = _save.LoadActiveRun();
            if (_v1JourneyResume) V1ValidatePriorEvidence(existingRun);
            else Require(_resumeBoss || _restorationCaptures ? existingRun is { Expedition: not null, Action: null, OpeningRecruitment: null } : existingRun is null,
                _resumeBoss ? "接续 Boss 输入需要已完成普通事务的真实大陆存档。" : "本次输入验收使用全新的隔离 namespace。");
            _stage = new UiInputStage(this, true);
            // UiInputStage owns a 960x540 host and a 1600x900 internal viewport.
            // Never use Input.WarpMouse or the desktop input surface.
            _stage.Viewport.GuiEmbedSubwindows = true;
            _drawsAtStart = (ulong)Engine.GetFramesDrawn();
            await Boot();
            if (_v1DefeatOnly)
            {
                await V1DefeatJourney();
                Require(_v1DefeatObserved, "真实付费尝试未取得非终局败战样本，保留结果但不记通过");
            }
            else if (_v1JourneyResume) await V1ResumeJourney();
            else if (_v1Journey) await V1Journey();
            else if (_itemsOnly) await ItemsJourney();
            else if (_rememberedOnly) await RememberedJourney();
            else if (_flowerLedgerOnly) await FlowerLedgerReview();
            else if (_flowerOnly) await FlowerJourney();
            else if (_mapLightOnly) await MapLightJourney();
            else if (_restorationCaptures) await RestorationCaptures();
            else if (_restorationOnly) await RestorationJourney();
            else if (_entranceMotionOnly) await EntranceMotionJourney();
            else if (_resumeBoss) await ResumeBossJourney();
            else
            {
                await OpenNewJourney();
                await AssertInitialPartyVisible();
                await Capture("map-overview.png");
                Check("new-game-six-select-two-to-map");
                if (_recruitmentMotionOnly) await RecruitmentMotionJourney();
                else if (_recruitmentOnly) await RecruitmentJourney();
                else if (_keyboardOnly) await KeyboardJourney();
                else if (!_mapOnly)
                {
                    if (!_serviceOnly) await MoveAndReveal();
                    await Service();
                    if (!_serviceOnly) await PreviewAndBattle();
                    await VerifyResume();
                }
            }
            _drawsAtEnd = (ulong)Engine.GetFramesDrawn();
            Require(_drawsAtEnd > _drawsAtStart, "需要新绘制帧作为画面证据。");
            await SaveRecording();
            WriteReport(true);
            GD.Print(_v1DefeatOnly ? $"V1_NONTERMINAL_DEFEAT_INPUT_OK real-defeat health-loss report-continue exact-cold-restore output={_output}"
                : _v1JourneyResume ? $"V1_RESUMED_FULL_JOURNEY_INPUT_OK same-run={_v1ResumeRunId} prior-paid-sites=12 resumed-paid-sites=6 prior-boss-victories=2 resumed-final-victory terminal-meta prior-evidence={_v1PriorDirectory} output={_output}"
                : _v1Journey ? $"V1_FULL_JOURNEY_INPUT_OK natural-opening eighteen-paid-sites growth-discovery native-deploy equipment relic-names three-boss-victories cold-continue terminal-meta output={_output}"
                : _itemsOnly ? $"V1_ITEMS_INPUT_OK real-shop free-preview two-purchases drag-equip-replace-transfer-cancel unequip exact-continue output={_output}"
                : _rememberedOnly ? $"REMEMBERED_GROWTH_INPUT_OK natural-opening recruit deploy fixed-partners real-battle ledger ascend discover boss stage-two continue output={_output}"
                : _breedingOnly ? $"BREEDING_CULTIVATION_INPUT_OK natural-opening recruit unassigned-breeding old-points seed retarget ledger continue output={_output}"
                : _flowerLedgerOnly ? $"FLOWER_CULTIVATION_LEDGER_INPUT_OK resume actual-wheel visible-fixed-pair unchanged-save output={_output}"
                : _flowerOnly ? $"FLOWER_CULTIVATION_INPUT_OK natural-opening unassigned-zero target-keyboard service-preview real-battle fixed-pair retarget exact-continue output={_output}"
                : _recruitmentMotionOnly ? $"RECRUITMENT_MOTION_INPUT_OK real-stock inspect-only cancel-free committed-receipt repeat-blocked return output={_output}"
                : _mapLightOnly ? $"MAP_LIGHT_INPUT_OK steps={_keyboardSteps} panel-stable source-correct return-preserves-save output={_output}"
                : _restorationCaptures
                ? $"JOURNEY_RESTORATION_CAPTURES_OK settled-main-menu map deployment independent-tools output={_output}"
                : _restorationOnly
                ? $"JOURNEY_RESTORATION_INPUT_OK main-menu settings independent-experiment opening map army deployment battle={_outcome} exact-continue namespace={_namespace} output={_output}"
                : _entranceMotionOnly
                ? $"ENTRANCE_MOTION_INPUT_OK clips={_entranceClips.Count} keyboard-steps={_keyboardSteps} namespace={_namespace} output={_output}"
                : _recruitmentStyleOnly
                ? $"EXPEDITION_RECRUITMENT_STYLE_INPUT_OK natural=three hover=buttonless keyboard=tab-no-recruit details=growth-traits-skill-escape fixture=one-two-layout namespace={_namespace} output={_output}"
                : _recruitmentOnly
                ? $"EXPEDITION_RECRUITMENT_INPUT_OK natural=three-unique-unowned keyboard=enter cancel=exact-stock recruit=one-action-one-hero restore=exact fixture=one-two-layout namespace={_namespace} output={_output}"
                : _keyboardOnly
                ? $"EXPEDITION_KEYBOARD_INPUT_OK steps={_keyboardSteps} movement=wasd-and-arrows service=enter-cancel enemy=blocked-kpenter-preview overlay=blocked menu=blocked namespace={_namespace} output={_output}"
                : _resumeBoss
                ? $"EXPEDITION_BOSS_INPUT_OK outcomes={string.Join(",", _bossOutcomes)} terminal={_terminal} refill-count={_stageRefills.Count} namespace={_namespace} output={_output}"
                : _mapOnly
                ? $"EXPEDITION_MAP_SCREENSHOT_OK path={Path.Combine(_output, "map-overview.png")} namespace={_namespace}"
                : _serviceOnly
                ? $"EXPEDITION_SERVICE_INPUT_OK preview=cancel-preserves-full-save service=reopen-confirm-once restore=exact namespace={_namespace} output={_output}"
                : $"EXPEDITION_JOURNEY_INPUT_OK movement=fog-reveal service=actual-choice preview=cancel-no-charge battle={_outcome} reward-groups={_rewardGroups} restore=exact namespace={_namespace} output={_output}");
        }
        catch (Exception exception)
        {
            exit = 1;
            GD.PrintErr($"{(_recruitmentStyleOnly ? "EXPEDITION_RECRUITMENT_STYLE_INPUT_FAILED" : _recruitmentOnly ? "EXPEDITION_RECRUITMENT_INPUT_FAILED" : _keyboardOnly ? "EXPEDITION_KEYBOARD_INPUT_FAILED" : _resumeBoss ? "EXPEDITION_BOSS_INPUT_FAILED" : _serviceOnly ? "EXPEDITION_SERVICE_INPUT_FAILED" : "EXPEDITION_JOURNEY_INPUT_FAILED")} step={_step}: {exception}");
            if (_stage is not null && Directory.Exists(_output))
            {
                try { if (_v1Journey && _save.LoadActiveRun() is { } failedRun) File.WriteAllText(Path.Combine(_output, "v1-failure-save.json"), JsonSerializer.Serialize(failedRun, new JsonSerializerOptions { WriteIndented = true })); await Capture("failure.png"); await SaveRecording(); WriteReport(false, exception.ToString()); }
                catch (Exception captureError) { GD.PrintErr("EXPEDITION_INPUT_EVIDENCE_FAILED " + captureError.Message); }
            }
        }
        finally
        {
            _game?.QueueFree();
            foreach (var frame in _recording) frame.Image.Dispose();
            _recording.Clear();
        }
        GetTree().Quit(exit);
    }

    private async Task Boot()
    {
        _game = GD.Load<PackedScene>("res://scenes/app/GrowthGameRoot.tscn").Instantiate<GameRoot>();
        _game.SaveNamespace = _namespace;
        _stage.AddChild(_game);
        await Until(() => _game.Content is not null && Screens.MainMenu.IsVisibleInTree(), "正式主菜单启动", 60);
        await Frames(6);
    }

    private async Task OpenNewJourney()
    {
        _step = "new journey / choose two";
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
        await Until(() => Screens.HeroSelection.IsVisibleInTree(), "正式六选二页面");
        await Frames(45);
        var cards = Descendants<HeroLibraryTile>(Screens.HeroSelection).Where(card => card.IsVisibleInTree()).ToArray();
        Require(cards.Length == 6, "开局必须显示六名正式候选。");
        await Click(cards[0]);
        await Click(cards[1]);
        var confirm = Screens.HeroSelection.GetNode<Button>("%ConfirmOpening");
        Require(!confirm.Disabled, "选中两名后可确认。");
        await Click(confirm);
        await Until(() => Screens.Expedition.IsVisibleInTree(), "确认开局进入正式地图", 30);
        var state = State();
        Require(state.OpeningRecruitment is null && state.Roster.Count == 2 && state.Expedition is not null,
            "真实输入保存队伍和大陆，不再进入旧塔层菜单。");
        Require(!Screens.Tower.IsVisibleInTree() && Map().Cells.Any(cell => !cell.Revealed), "地图保留战争迷雾。");
    }

    private async Task MoveAndReveal()
    {
        _step = "known road / reveal fog";
        var before = State();
        var map = Map();
        var target = Frontier(map).First();
        await MoveTo(target);
        var after = State();
        Require(after.Expedition!.PositionIndex == target && after.Expedition.Cells.Count(cell => cell.Revealed) >
            before.Expedition!.Cells.Count(cell => cell.Revealed), "点击已知道路移动后揭示新格子。");
        Require(after.Expedition.RemainingActions == before.Expedition!.RemainingActions && after.Gold == before.Gold &&
            after.BattleNumber == before.BattleNumber, "移动不消耗处理行动或发放收益。");
        await Capture("map-after-exploration.png");
        await Click(Screens.Expedition.GetNode<Button>("%ArmyButton"));
        var army = _game!.GetNode<ArmyOverviewController>("ArmyOverview");
        Require(army.IsOpen, "地图打开正式军团抽屉。");
        await Key(Godot.Key.Escape);
        await Until(() => !army.IsOpen, "Escape 关闭军团抽屉");
        Check("known-path-real-click-reveals-fog-without-action-cost");
        Check("army-overlay-escape");
    }

    private async Task AssertInitialPartyVisible()
    {
        // Do not EnsureControlVisible here: that would repair the application's
        // initial camera from the test and hide a broken first map presentation.
        await Frames(6);
        var map = Map();
        var scroll = Screens.Expedition.GetNode<ScrollContainer>("%MapScroll");
        var cell = Screens.Expedition.GetNode<GridContainer>("%MapGrid")
            .GetNode<ExpeditionMapCell>($"Cell{map.PositionIndex}");
        var viewport = scroll.GetGlobalRect();
        var party = cell.GetGlobalRect();
        Require(viewport.Encloses(party), "首次地图必须自动定位到队伍，不能只显示屏幕外队伍周围的迷雾。");
        Require(cell.GetNode<Control>("Ground").IsVisibleInTree(), "初始军团脚下的已知地貌必须显示。");
        Check("initial-party-visible-without-test-scrolling");
    }

    private async Task Service()
    {
        _step = "discover / resolve service";
        var site = await ReachSite(TowerNodeType.Rest);
        await SelectCell(site.Index);
        await Capture("site-detail.png");
        var before = State();
        var savedBefore = JsonSerializer.Serialize(before);
        await Click(Screens.Expedition.GetNode<Button>("%InspectButton"));
        await Until(() => Screens.Reward.IsVisibleInTree() || Screens.Recruitment.IsVisibleInTree(), "正式服务选项页面");
        Require(State().Expedition!.RemainingActions == before.Expedition!.RemainingActions, "查看地点不会扣行动。");
        var screen = Screens.Reward.IsVisibleInTree() ? Screens.Reward : Screens.Recruitment;
        var cancel = screen.GetNode<Button>("Center/Panel/Layout/ContinueButton");
        Require(cancel.IsVisibleInTree() && !cancel.Disabled && cancel.Text == "返回地图（不消耗行动）",
            "地图服务预览始终有明确的无消耗返回入口。");
        await Capture("service-preview-before-cancel.png");
        await Click(cancel);
        await Until(() => Screens.Expedition.IsVisibleInTree(), "服务预览取消返回地图");
        Require(JsonSerializer.Serialize(State()) == savedBefore, "服务预览取消保留完整存档，不扣行动、不结算成长、不改变库存。");
        Check("service-preview-cancel-preserves-full-save");
        await SelectCell(site.Index);
        await Click(Screens.Expedition.GetNode<Button>("%InspectButton"));
        await Until(() => Screens.Reward.IsVisibleInTree() || Screens.Recruitment.IsVisibleInTree(), "重新打开同一服务地点");
        screen = Screens.Reward.IsVisibleInTree() ? Screens.Reward : Screens.Recruitment;
        var choice = Descendants<RunOfferChoiceCard>(screen).FirstOrDefault(card => card.IsVisibleInTree() && !card.ConfirmButton.Disabled);
        Require(choice is not null, "营地至少有一个合法正式选项。");
        await Capture("service-choice.png");
        await Click(choice!.ConfirmButton);
        await Until(() => Screens.Expedition.IsVisibleInTree(), "处理服务返回地图");
        var after = State();
        Require(after.Expedition!.RemainingActions == before.Expedition!.RemainingActions - 1 &&
            after.Expedition.Sites.Single(value => value.SiteId == site.SiteId).Handled, "服务真实确认恰好消耗一次行动并完成地点。");
        Check("service-preview-free-confirm-once-return-map");
    }

    private async Task PreviewAndBattle(TowerNodeType kind = TowerNodeType.Combat, ExpeditionCellView? chosenSite = null)
    {
        _step = "enemy preview / cancel";
        var site = chosenSite ?? await ReachSite(kind);
        await SelectCell(site.Index);
        var before = State();
        var savedBefore = JsonSerializer.Serialize(before);
        await Click(Screens.Expedition.GetNode<Button>("%InspectButton"));
        await Until(() => Screens.Deployment.IsVisibleInTree(), "真实敌阵整备页面");
        await Capture("deployment-preview.png");
        await Click(Screens.Deployment.GetNode<Button>("%BackButton"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "未提交整备返回地图");
        Require(JsonSerializer.Serialize(State()) == savedBefore, "预览和返回不扣行动、不冻结法术、不修改存档。");
        Check("enemy-preview-cancel-preserves-save");

        _step = "start actual battle";
        await SelectCell(site.Index);
        await Click(Screens.Expedition.GetNode<Button>("%InspectButton"));
        await Until(() => Screens.Deployment.IsVisibleInTree(), "再次进入正式整备");
        await Click(Screens.Deployment.GetNode<Button>("%StartBattleButton"));
        await Until(() => Screens.Battle.IsVisibleInTree() || Screens.BattleReport.IsVisibleInTree(), "正式开战", 15);
        Require(State().Expedition!.RemainingActions == before.Expedition!.RemainingActions - 1,
            "实际开战恰好扣一次行动。");
        Require(Mathf.IsEqualApprox(Screens.Battle.SpeedScale, 1f), "新隔离存档以正常速度战斗与录制。");
        await Capture("battle-start.png");
        await RecordBattle(_breedingOnly || _rememberedOnly ? 12 : 4);
        await Until(() => Screens.BattleReport.IsVisibleInTree(), "实际自动战斗与战报", 180);
        Require(Screens.Battle.LastRuntimeFailure.Length == 0, "战斗运行没有异常。");
        _outcome = Screens.Battle.TerminalResult?.Outcome.ToString() ?? "unknown";
        Require(_outcome != "unknown", "必须有实际战斗结果。");
        var result = State();
        Require(result.BattleNumber == before.BattleNumber + 1 && result.CurrentRunHealth > 0,
            "普通战实际结算一次，并可继续当前征程。");
        await Capture("battle-report.png");
        await Click(Screens.BattleReport.GetNode<Button>("%ReportContinue"));
        for (var group = 0; group < 4 && !Screens.Expedition.IsVisibleInTree(); group++)
        {
            await Until(() => Screens.Reward.IsVisibleInTree() || Screens.Expedition.IsVisibleInTree(), "战后奖励或地图");
            if (Screens.Expedition.IsVisibleInTree()) break;
            await Capture($"reward-{group + 1}.png");
            var choice = Descendants<RunOfferChoiceCard>(Screens.Reward).FirstOrDefault(card => card.IsVisibleInTree() && !card.ConfirmButton.Disabled);
            if (choice is not null) await Click(choice.ConfirmButton);
            else await Click(Screens.Reward.GetNode<Button>("Center/Panel/Layout/ContinueButton"));
            _rewardGroups++;
        }
        await Until(() => Screens.Expedition.IsVisibleInTree(), "战后完成结算返回地图");
        Require(State().BattleNumber == result.BattleNumber && State().Expedition!.RemainingActions == result.Expedition!.RemainingActions,
            "领奖与返回地图不重复计战或扣行动。");
        if (!_restorationOnly && Screens.Battle.TerminalResult?.Outcome == BattleOutcome.PlayerVictory)
            Require(_rewardGroups > 0, "普通战胜利真实领取至少一组战利品。");
        await Capture("map-after-battle.png");
        Check("actual-battle-result-and-return-map");
        if (_rewardGroups > 0) Check("actual-reward-confirmation");
    }

    private async Task VerifyResume()
    {
        _step = "save / continue";
        var saved = JsonSerializer.Serialize(State());
        await Click(Screens.Expedition.GetNode<Button>("%MenuButton"));
        Require(Screens.MainMenu.IsVisibleInTree(), "地图返回主菜单。");
        _game!.QueueFree();
        _game = null;
        await Frames(5);
        await Boot();
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "实际继续恢复大陆");
        Require(JsonSerializer.Serialize(State()) == saved, "继续不重生成地图、不重抽库存、不重复结算。");
        await Capture("map-restored.png");
        Check("formal-restart-continue-preserves-entire-save");
    }

    private async Task KeyboardJourney()
    {
        _step = "keyboard / discover camp";
        var camp = await KeyboardReachSite(TowerNodeType.Rest);
        Require(Map().PositionIndex == camp.Index, "通过逐格键盘移动到达营地。");
        var beforeService = JsonSerializer.Serialize(State());
        await Key(Godot.Key.Enter);
        await Until(() => Screens.Reward.IsVisibleInTree(), "地图 Enter 打开营地服务");
        await Capture("keyboard-camp-service.png");
        await Key(Godot.Key.W);
        await Key(Godot.Key.Right);
        Require(Screens.Reward.IsVisibleInTree() && JsonSerializer.Serialize(State()) == beforeService,
            "服务页面的方向键和 WASD 不能穿透到地图移动。");
        await Click(Screens.Reward.GetNode<Button>("Center/Panel/Layout/ContinueButton"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "营地取消返回地图");
        Require(JsonSerializer.Serialize(State()) == beforeService, "Enter 打开服务并取消不改变完整存档。");
        Check("keyboard-enter-camp-cancel-preserves-save-service-keys-do-not-leak");

        _step = "keyboard / army modal guard";
        var map = Map();
        var road = Neighbors(map.Size, map.PositionIndex)
            .First(index => map.Cells[index] is { Revealed: true, Walkable: true, Blocked: false });
        var army = _game!.GetNode<ArmyOverviewController>("ArmyOverview");
        await Click(Screens.Expedition.GetNode<Button>("%ArmyButton"));
        Require(army.IsOpen, "地图实际打开军团窗口。");
        await Capture("keyboard-army-modal.png");
        var beforeArmy = JsonSerializer.Serialize(State());
        await Key(MovementKey(map.Size, map.PositionIndex, road, true));
        await Key(Godot.Key.Enter);
        await Frames(18);
        Require(JsonSerializer.Serialize(State()) == beforeArmy && Screens.Expedition.IsVisibleInTree() &&
            !Screens.Reward.IsVisibleInTree(), "军团窗口打开时，合法移动方向和 Enter 都不能操作底下的大陆。");
        // Enter may legitimately activate the drawer's focused Close button.
        // Reopen through its real button so Escape has its own explicit coverage.
        if (!army.IsOpen) await Click(Screens.Expedition.GetNode<Button>("%ArmyButton"));
        await Key(Godot.Key.Escape);
        await Until(() => !army.IsOpen, "Escape 关闭军团窗口");
        await KeyboardStep(road);
        await KeyboardStep(map.PositionIndex);
        Check("keyboard-army-modal-blocks-map-escape-restores-movement");

        _step = "keyboard / terrain or map boundary";
        await KeyboardBlockedTerrain();

        _step = "keyboard / approach enemy and preview";
        var enemy = await KeyboardReachSite(TowerNodeType.Combat);
        map = Map();
        Require(Neighbors(map.Size, map.PositionIndex).Contains(enemy.Index), "守军在键盘移动可达位置的四邻格。");
        var beforeEnemy = JsonSerializer.Serialize(State());
        await Key(MovementKey(map.Size, map.PositionIndex, enemy.Index, _keyboardSteps % 2 == 0));
        Require(Map().PositionIndex == map.PositionIndex && JsonSerializer.Serialize(State()) == beforeEnemy,
            "按向邻接守军不能穿过敌人或扣除行动。");
        Require(MapCell(enemy.Index).GetNode<Control>("Selection").IsVisibleInTree(), "尝试走向守军会选中对应敌人地块。");
        await Capture("keyboard-enemy-selected.png");
        await Key(Godot.Key.KpEnter);
        await Until(() => Screens.Deployment.IsVisibleInTree(), "小键盘 Enter 进入守军正式整备");
        await Capture("keyboard-enemy-deployment.png");
        await Key(Godot.Key.A);
        Require(Screens.Deployment.IsVisibleInTree() && JsonSerializer.Serialize(State()) == beforeEnemy,
            "整备页面的地图移动键不穿透，也不会提前开战。");
        await Click(Screens.Deployment.GetNode<Button>("%BackButton"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "守军预览返回大陆");
        Require(JsonSerializer.Serialize(State()) == beforeEnemy, "键盘守军预览与返回不支付行动、不冻结法术。");
        Check("keyboard-enemy-block-select-kpenter-preview-back-no-charge");
        await Capture("keyboard-explored-map.png");

        _step = "keyboard / menu guard and continue";
        var beforeMenu = JsonSerializer.Serialize(State());
        await Click(Screens.Expedition.GetNode<Button>("%MenuButton"));
        await Until(() => Screens.MainMenu.IsVisibleInTree(), "地图返回正式主菜单");
        await Key(Godot.Key.W);
        await Key(Godot.Key.Left);
        Require(Screens.MainMenu.IsVisibleInTree() && JsonSerializer.Serialize(State()) == beforeMenu,
            "主菜单方向键与 WASD 不穿透隐藏地图。");
        var resume = Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton");
        for (var index = 0; index < 24 && _stage.Viewport.GuiGetFocusOwner() != resume; index++)
            await Key(Godot.Key.Tab);
        Require(_stage.Viewport.GuiGetFocusOwner() == resume, "真实 Tab 导航到主菜单继续按钮。");
        await Key(Godot.Key.Enter);
        await Until(() => Screens.Expedition.IsVisibleInTree(), "主菜单 Enter 激活继续");
        Require(JsonSerializer.Serialize(State()) == beforeMenu && !Screens.Deployment.IsVisibleInTree(),
            "主菜单 Enter 只继续当前征程，不同时触发底下地图的敌人交互。");
        Require(_keyboardSteps >= 2, "实际完成 WASD 和方向键两套逐格移动输入。");
        await Capture("keyboard-continued-map.png");
        Check("keyboard-menu-blocks-map-tab-enter-continues-only");
    }

    private ExpeditionMapCell MapCell(int index) =>
        Screens.Expedition.GetNode<GridContainer>("%MapGrid").GetNode<ExpeditionMapCell>($"Cell{index}");

    private async Task<ExpeditionCellView> KeyboardReachSite(TowerNodeType kind)
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var map = Map();
            var routes = KnownRoutes(map);
            var sites = map.Cells.Where(cell => cell.Revealed && !cell.Cleared && !cell.Handled && cell.SiteId.Length > 0 &&
                Definition.Sites.Any(site => site.StableId == cell.DefinitionId && site.Kind == kind));
            var reachable = sites.Select(site => (Site: site, Approach: (kind == TowerNodeType.Combat
                    ? Neighbors(map.Size, site.Index) : new[] { site.Index })
                .Where(routes.ContainsKey).OrderBy(index => routes[index].Length).Select(index => (int?)index).FirstOrDefault()))
                .Where(value => value.Approach is not null).OrderBy(value => routes[value.Approach!.Value].Length).FirstOrDefault();
            if (reachable.Site is not null)
            {
                await KeyboardRoute(reachable.Approach!.Value);
                return reachable.Site;
            }
            var frontier = Frontier(map).FirstOrDefault(-1);
            Require(frontier >= 0, $"已揭示道路没有可继续探索的边缘，尚未找到 {kind}。");
            await KeyboardRoute(frontier);
        }
        throw new InvalidOperationException("局部键盘探索未在 64 次边缘规划内找到所需地点。");
    }

    private async Task KeyboardRoute(int target)
    {
        var routes = KnownRoutes(Map());
        Require(routes.TryGetValue(target, out var steps), "键盘路径只能使用已揭示的可通行道路。");
        foreach (var next in steps!) await KeyboardStep(next);
    }

    private async Task KeyboardStep(int target)
    {
        var before = State();
        var map = Map();
        Require(Neighbors(map.Size, map.PositionIndex).Contains(target), "每次真实按键只计划四邻一步。");
        var key = MovementKey(map.Size, map.PositionIndex, target, _keyboardSteps % 2 == 0);
        await Key(key);
        var after = State();
        Require(after.Expedition!.PositionIndex == target, $"键盘 {key} 应从 {map.PositionIndex} 移动一格到 {target}。");
        Require(after.Expedition.RemainingActions == before.Expedition!.RemainingActions && after.Gold == before.Gold &&
            after.CurrentRunHealth == before.CurrentRunHealth && after.BattleNumber == before.BattleNumber,
            "键盘跑图不扣处理行动、金币或生命，也不触发战斗。");
        Require(MapCell(target).GetNode<Control>("Selection").IsVisibleInTree(), "键盘移动后自动选中当前位置。");
        _keyboardSteps++;
    }

    private async Task KeyboardBlockedTerrain()
    {
        for (var attempt = 0; attempt < 16; attempt++)
        {
            var map = Map();
            var routes = KnownRoutes(map);
            var position = routes.Keys.Where(index => BlockedDirection(map, index) is not null)
                .OrderBy(index => routes[index].Length).FirstOrDefault(-1);
            if (position >= 0)
            {
                await KeyboardRoute(position);
                var saved = JsonSerializer.Serialize(State());
                await Key(BlockedDirection(Map(), position)!.Value);
                Require(JsonSerializer.Serialize(State()) == saved, "撞山或地图边界不会移动队伍或修改完整存档。");
                Check("keyboard-terrain-or-map-boundary-cannot-move");
                return;
            }
            var frontier = Frontier(map).FirstOrDefault(-1);
            Require(frontier >= 0, "已知道路没有可用于检查地形阻挡的方向。");
            await KeyboardRoute(frontier);
        }
        throw new InvalidOperationException("局部探索未发现山地或边界，不能宣称已验阻挡。");
    }

    private static Godot.Key? BlockedDirection(ExpeditionMapView map, int index)
    {
        if (index % map.Size == 0) return Godot.Key.Left;
        if (index % map.Size == map.Size - 1) return Godot.Key.Right;
        if (index < map.Size) return Godot.Key.Up;
        if (index >= map.Size * (map.Size - 1)) return Godot.Key.Down;
        var wall = Neighbors(map.Size, index).FirstOrDefault(next => map.Cells[next] is { Revealed: true, Walkable: false }, -1);
        return wall >= 0 ? MovementKey(map.Size, index, wall, false) : null;
    }

    private static Godot.Key MovementKey(int size, int from, int to, bool wasd) => (to - from) switch
    {
        1 => wasd ? Godot.Key.D : Godot.Key.Right,
        -1 => wasd ? Godot.Key.A : Godot.Key.Left,
        var delta when delta == size => wasd ? Godot.Key.S : Godot.Key.Down,
        var delta when delta == -size => wasd ? Godot.Key.W : Godot.Key.Up,
        _ => throw new InvalidOperationException("键盘移动目标不相邻。")
    };

    private async Task ResumeBossJourney()
    {
        _step = "resume existing map for natural boss outcome";
        var before = State();
        var savedBefore = JsonSerializer.Serialize(before);
        File.WriteAllText(Path.Combine(_output, "before-boss-run.json"),
            JsonSerializer.Serialize(before, new JsonSerializerOptions { WriteIndented = true }));
        var metaBefore = _save.LoadMeta();
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "接续既有大陆存档");
        Require(JsonSerializer.Serialize(State()) == savedBefore, "继续不修改原有种子、英雄、供给或大陆进度。");
        await AssertInitialPartyVisible();
        await Capture("map-before-boss.png");
        Check("boss-continues-existing-save-without-fixture-writes");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var stageBefore = State();
            var stageIndex = stageBefore.Expedition!.StageIndex;
            var prefix = $"boss-stage-{stageIndex + 1}";
            _step = $"natural boss stage {stageIndex + 1} / deployment";
            await Click(Screens.Expedition.GetNode<Button>("%BossButton"));
            await Until(() => Screens.Deployment.IsVisibleInTree(), "阶段首领正式整备");
            Require(JsonSerializer.Serialize(State()) == JsonSerializer.Serialize(stageBefore), "首领预览不能提前支付行动或冻结阵容。");
            await Capture(prefix + "-deployment.png");
            await Click(Screens.Deployment.GetNode<Button>("%StartBattleButton"));
            await Until(() => Screens.Battle.IsVisibleInTree() || Screens.BattleReport.IsVisibleInTree(), "阶段首领实际开战", 15);
            Require(Mathf.IsEqualApprox(Screens.Battle.SpeedScale, 1f), "首领输入验收以 x1 正常速度进行。");
            await Capture(prefix + "-battle-start.png");
            if (_recording.Count == 0) await RecordBattle(4);
            _step = $"natural boss stage {stageIndex + 1} / battle report";
            await Until(() => Screens.BattleReport.IsVisibleInTree(), "首领自然战斗结果和战报", 180);
            Require(Screens.Battle.LastRuntimeFailure.Length == 0, "首领战运行没有异常。");
            var result = Screens.Battle.TerminalResult ?? throw new InvalidOperationException("首领没有实际战斗结果。");
            Require(result.Outcome is BattleOutcome.PlayerVictory or BattleOutcome.PlayerDefeat or BattleOutcome.Timeout,
                "首领必须由正式模拟产生胜利、失败或超时结果。");
            _outcome = result.Outcome.ToString();
            _bossOutcomes.Add($"stage-{stageIndex + 1}:{_outcome}");
            await Capture(prefix + "-report.png");
            Check($"boss-stage-{stageIndex + 1}-actual-{_outcome}");
            await Click(Screens.BattleReport.GetNode<Button>("%ReportContinue"));

            var expectedTerminal = result.Outcome != BattleOutcome.PlayerVictory || stageIndex == Definition.Stages.Length - 1;
            if (expectedTerminal)
            {
                await VerifyBossTerminal(stageIndex, result.Outcome, metaBefore);
                return;
            }

            _step = $"natural boss stage {stageIndex + 1} / rewards and refill";
            var groupsBefore = _rewardGroups;
            for (var group = 0; group < 8; group++)
            {
                await Until(() => Screens.Reward.IsVisibleInTree() || Screens.Expedition.IsVisibleInTree(), "首领奖励或下一阶段大陆");
                if (Screens.Expedition.IsVisibleInTree()) break;
                await Capture($"{prefix}-reward-{group + 1}.png");
                var choice = Descendants<RunOfferChoiceCard>(Screens.Reward)
                    .FirstOrDefault(card => card.IsVisibleInTree() && !card.ConfirmButton.Disabled);
                if (choice is not null) await Click(choice.ConfirmButton);
                else await Click(Screens.Reward.GetNode<Button>("Center/Panel/Layout/ContinueButton"));
                _rewardGroups++;
            }
            await Until(() => Screens.Expedition.IsVisibleInTree(), "领取首领奖励后进入下一阶段");
            var advanced = State();
            Require(advanced.Expedition!.StageIndex == stageIndex + 1 &&
                advanced.Expedition.RemainingActions == Definition.Stages[stageIndex + 1].Actions &&
                advanced.BattleNumber == stageBefore.BattleNumber + 1 && advanced.Action is null,
                "真实首领胜利和领奖恰好推进一阶段、补足行动并关闭事务。");
            _stageRefills.Add(new
            {
                FromStage = stageIndex + 1, ToStage = advanced.Expedition.StageIndex + 1,
                ActionsBefore = stageBefore.Expedition.RemainingActions,
                ActionsAfter = advanced.Expedition.RemainingActions,
                RewardGroupsClaimed = _rewardGroups - groupsBefore
            });
            await Capture(prefix + "-next-stage-map.png");
            Check($"boss-stage-{stageIndex + 1}-reward-next-stage-refill");
        }
        throw new InvalidOperationException("最多三次真实阶段首领输入后仍未到达终局；不能记为终局验收通过。");
    }

    private async Task VerifyBossTerminal(int stageIndex, BattleOutcome outcome, MetaProgressDto metaBefore)
    {
        _step = "boss natural terminal / archive / menu";
        await Until(() => Screens.Result.IsVisibleInTree(), "首领战报继续进入正式终局页");
        _terminal = outcome switch
        {
            BattleOutcome.PlayerVictory => "victory",
            BattleOutcome.Timeout => "timeout",
            _ => "defeat"
        };
        Require(_save.LoadActiveRun() is null && string.IsNullOrEmpty(_save.ActiveRunReadError) &&
            !File.Exists(ProjectSettings.GlobalizePath($"user://{_namespace}/active_run.json")),
            "自然首领终局必须删除隔离 ActiveRun，不能以读取失败冒充删除。");
        var meta = _save.LoadMeta();
        File.WriteAllText(Path.Combine(_output, "after-boss-meta.json"),
            JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
        Require(meta.HighestRegion == Math.Max(metaBefore.HighestRegion, stageIndex + 1), "终局 Meta 保留已到达阶段的高水位。");
        Require(meta.AppliedRunCompletionIds.Count == metaBefore.AppliedRunCompletionIds.Count + (outcome == BattleOutcome.PlayerVictory ? 1 : 0) &&
            meta.Victories == metaBefore.Victories + (outcome == BattleOutcome.PlayerVictory ? 1 : 0),
            "只有最终胜利增加胜局数和奖励去重身份，败局仅保留阶段高水位。");
        await Capture("boss-terminal-result.png");
        await Click(Screens.Result.GetNode<Button>("Center/Panel/Layout/MenuButton"));
        await Until(() => Screens.MainMenu.IsVisibleInTree(), "终局返回主菜单");
        Require(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton").Disabled,
            "终局后主菜单继续按钮不可用。");
        await Capture("boss-terminal-menu.png");
        Check("boss-terminal-result-active-run-deleted-meta-once-continue-disabled");
    }

    private async Task<ExpeditionCellView> ReachSite(TowerNodeType kind, ISet<string>? excluded = null)
    {
        for (var attempt = 0; attempt < 128; attempt++)
        {
            var map = Map();
            var routes = KnownRoutes(map);
            var sites = map.Cells.Where(cell => cell.Revealed && !cell.Cleared && !cell.Handled && cell.SiteId.Length > 0 &&
                (excluded is null || !excluded.Contains(cell.SiteId)) &&
                Definition.Sites.Any(site => site.StableId == cell.DefinitionId && site.Kind == kind));
            var reachable = sites.Select(site => (Site: site, Approach: Neighbors(map.Size, site.Index).Append(site.Index)
                    .Where(routes.ContainsKey).OrderBy(index => routes[index].Length).Select(index => (int?)index).FirstOrDefault()))
                .Where(candidate => candidate.Approach is not null).OrderBy(candidate => routes[candidate.Approach!.Value].Length).FirstOrDefault();
            if (reachable.Site is not null)
            {
                if (reachable.Approach!.Value != map.PositionIndex) await MoveTo(reachable.Approach.Value);
                return reachable.Site;
            }
            var frontier = Frontier(map).FirstOrDefault(-1);
            if (frontier < 0 && excluded is not null)
            {
                // Only the growth acquisition path may actively clear a known
                // roadblock. Candidate stock behind fog remains unread.
                var guard = map.Cells.Where(cell => cell.Revealed && cell.Blocked && !cell.Cleared &&
                    Definition.Sites.Any(site => site.StableId == cell.DefinitionId && site.Kind is TowerNodeType.Combat or TowerNodeType.Elite))
                    .Select(cell => (Cell: cell, Approach: Neighbors(map.Size, cell.Index).Where(routes.ContainsKey)
                        .OrderBy(index => routes[index].Length).Select(index => (int?)index).FirstOrDefault()))
                    .Where(value => value.Approach is not null)
                    .OrderBy(value => Definition.Sites.Single(site => site.StableId == value.Cell.DefinitionId).Kind == TowerNodeType.Elite)
                    .ThenBy(value => routes[value.Approach!.Value].Length).FirstOrDefault();
                Require(guard.Cell is not null && State().Expedition!.RemainingActions > 1,
                    "当前可见地图必须有能付费清理的守关；不能注入关键件或免费穿越");
                if (guard.Approach!.Value != map.PositionIndex) await MoveTo(guard.Approach.Value);
                await PreviewAndBattle(Definition.Sites.Single(site => site.StableId == guard.Cell!.DefinitionId).Kind, guard.Cell);
                Require(Screens.Battle.TerminalResult!.Outcome == BattleOutcome.PlayerVictory, "真实守关胜利后才开放道路");
                continue;
            }
            Require(frontier >= 0, $"已知道路中没有可继续揭示的边缘，尚未找到 {kind}。");
            await MoveTo(frontier);
        }
        throw new InvalidOperationException("局部探索超过 128 次，未找到需要的正式地点。");
    }

    private async Task MoveTo(int index)
    {
        // A formal route intentionally stops at its first newly revealed edge.
        // Reconfirm the same known destination after that committed partial move.
        for (var step = 0; step < 32 && Map().PositionIndex != index; step++)
        {
            var before = Map().PositionIndex;
            await SelectCell(index);
            var button = Screens.Expedition.GetNode<Button>("%MoveButton");
            Require(!button.Disabled, $"正式页面允许沿已知道路前往 {index}。");
            await Click(button);
            Require(Map().PositionIndex != before, "移动经正式输入提交至少一步，不以等待掩盖无效点击。");
        }
        Require(Map().PositionIndex == index, "逐次确认发现边缘后到达原目的地。");
    }

    private async Task SelectCell(int index) => await Click(Screens.Expedition.GetNode<GridContainer>("%MapGrid").GetNode<ExpeditionMapCell>($"Cell{index}"));

    private static IEnumerable<int> Frontier(ExpeditionMapView map)
    {
        var routes = KnownRoutes(map);
        return routes.Keys.Where(index => index != map.PositionIndex && Neighbors(map.Size, index).Any(next => !map.Cells[next].Revealed))
            .OrderBy(index => routes[index].Length).ThenBy(index => index);
    }

    private static Dictionary<int, int[]> KnownRoutes(ExpeditionMapView map)
    {
        var routes = new Dictionary<int, int[]> { [map.PositionIndex] = [] };
        var queue = new Queue<int>(); queue.Enqueue(map.PositionIndex);
        while (queue.TryDequeue(out var current))
            foreach (var next in Neighbors(map.Size, current))
                if (!routes.ContainsKey(next) && map.Cells[next] is { Revealed: true, Walkable: true, Blocked: false })
                { routes.Add(next, [.. routes[current], next]); queue.Enqueue(next); }
        return routes;
    }

    private static IEnumerable<int> Neighbors(int size, int index)
    {
        if (index >= size) yield return index - size;
        if (index % size > 0) yield return index - 1;
        if (index % size + 1 < size) yield return index + 1;
        if (index + size < size * size) yield return index + size;
    }

    private async Task Click(Control target)
    {
        if (_restorationOnly || _mapLightOnly || _recruitmentMotionOnly) await Until(() => !Screens.IsTransitioning, "页面转场结束");
        await Frames(2);
        for (var ancestor = target.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
            if (ancestor is ScrollContainer scroll) { scroll.EnsureControlVisible(target); await Frames(3); }
        Require(target.IsVisibleInTree() && (target is not BaseButton button || !button.Disabled), "输入目标可用：" + target.GetPath());
        var point = target.GetGlobalRect().GetCenter();
        Require(_stage.Viewport.GetVisibleRect().HasPoint(point), "输入目标位于内部视口：" + target.GetPath());
        _inputs.Add(new { kind = "click", target = target.GetPath().ToString(), x = point.X, y = point.Y, ticks = Time.GetTicksMsec() });
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point }); await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }); await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }); await Frames(6);
    }

    private async Task Key(Key key)
    {
        _inputs.Add(new { kind = "key", key = key.ToString(), ticks = Time.GetTicksMsec() });
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true }); await Frames(1);
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false }); await Frames(12);
    }

    private async Task Until(Func<bool> condition, string description, double seconds = 10)
    {
        var deadline = Time.GetTicksMsec() + (ulong)(seconds * 1000);
        while (!condition() && Time.GetTicksMsec() < deadline) await Frames(1);
        Require(condition(), description + "超时。");
    }

    private async Task Frames(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            RenderingServer.ForceDraw(false);
        }
    }

    private async Task Capture(string file)
    {
        if (_restorationOnly) await Until(() => !Screens.IsTransitioning, "截图等待转场结束");
        await Frames(2);
        using var image = _stage.Viewport.GetTexture().GetImage();
        Require(!image.IsEmpty() && image.SavePng(Path.Combine(_output, file)) == Error.Ok, "保存实际渲染截图：" + file);
        _captures.Add(file);
    }

    private async Task RecordBattle(double seconds)
    {
        var end = Time.GetTicksMsec() + (ulong)(seconds * 1000);
        ulong previous = 0;
        while (Time.GetTicksMsec() < end && Screens.Battle.IsVisibleInTree())
        {
            await Frames(1);
            var now = Time.GetTicksMsec();
            if (now - previous < 84) continue;
            previous = now;
            var image = _stage.Viewport.GetTexture().GetImage();
            image.Resize(960, 540, Image.Interpolation.Lanczos);
            _recording.Add((image, now, Screens.Battle.TickIndex));
        }
    }

    private Task SaveRecording()
    {
        if (_recording.Count == 0) return Task.CompletedTask;
        var folder = Path.Combine(_output, "battle-frames"); Directory.CreateDirectory(folder);
        var concat = new List<string> { "ffconcat version 1.0" };
        var fingerprints = new HashSet<string>();
        for (var index = 0; index < _recording.Count; index++)
        {
            var frame = _recording[index];
            var name = $"frame-{index:D3}.png";
            fingerprints.Add(Convert.ToHexString(SHA256.HashData(frame.Image.GetData())));
            Require(frame.Image.SavePng(Path.Combine(folder, name)) == Error.Ok, "保存正常速度录像帧。");
            var duration = index + 1 < _recording.Count ? (_recording[index + 1].Time - frame.Time) / 1000.0 : 0.084;
            concat.Add($"file '{name}'"); concat.Add("duration " + duration.ToString("0.000000", CultureInfo.InvariantCulture));
        }
        concat.Add($"file 'frame-{_recording.Count - 1:D3}.png'");
        File.WriteAllLines(Path.Combine(folder, "frames.ffconcat"), concat);
        File.WriteAllText(Path.Combine(folder, "samples.json"), JsonSerializer.Serialize(_recording.Select(frame => new { frame.Time, frame.Tick })));
        Require(_recording.Count < 3 || fingerprints.Count >= 3, "短录制必须包含新画面，不能重复空帧。");
        return Task.CompletedTask;
    }

    private void WriteReport(bool passed, string error = "") => File.WriteAllText(Path.Combine(_output, "evidence.json"), JsonSerializer.Serialize(new
    {
        Passed = passed, V1DefeatOnly = _v1DefeatOnly, V1DefeatObserved = _v1DefeatObserved, V1Journey = _v1Journey, V1JourneyResume = _v1JourneyResume,
        PriorEvidence = _v1PriorEvidence, ResumeRunId = _v1ResumeRunId,
        CombinedPaidActions = _v1JourneyResume && passed ? 12 + _v1Actions.Count : (int?)null,
        CombinedBossVictories = _v1JourneyResume && passed ? 2 + _bossOutcomes.Count : (int?)null,
        ResumeMetaBaseline = _v1JourneyResume ? "v1-resume-meta-before.json; existing isolated meta before Continue, not original new-game baseline" : "",
        V1Actions = _v1Actions, V1Clips = _v1Clips, V1Ascensions = _v1Ascensions, V1Equipment = _v1Equipment, V1RelicReads = _v1Relics, V1Assigned = _v1Assigned, V1Resumed = _v1Resumed, FlowerOnly = _flowerOnly, FlowerLedgerOnly = _flowerLedgerOnly, RecruitmentMotionOnly = _recruitmentMotionOnly, MapLightOnly = _mapLightOnly,
        EntranceMotionOnly = _entranceMotionOnly, EntranceClips = _entranceClips, MapOnly = _mapOnly, ServiceOnly = _serviceOnly, ResumeBoss = _resumeBoss, KeyboardOnly = _keyboardOnly,
        RecruitmentOnly = _recruitmentOnly, RecruitmentStyleOnly = _recruitmentStyleOnly, RecruitmentLayouts = _recruitmentLayouts,
        KeyboardSteps = _keyboardSteps, Namespace = _namespace, Step = _step, Error = error,
        Host = "960x540", InternalViewport = "1600x900", Input = "SubViewport.PushInput; no desktop cursor",
        Checks = _checks, Inputs = _inputs, Screenshots = _captures,
        EngineDrawsBefore = _drawsAtStart, EngineDrawsAfter = _drawsAtEnd,
        BattleOutcome = _outcome, RewardGroupsClaimed = _rewardGroups, RecordingFrames = _recording.Count,
        BossOutcomes = _bossOutcomes, StageRefills = _stageRefills, Terminal = _terminal,
        BeforeBossRun = _resumeBoss ? "before-boss-run.json" : "", AfterBossMeta = (_resumeBoss || _v1Journey) && _terminal != "not-run" ? "after-boss-meta.json" : "",
        Recording = _recording.Count > 0 ? "battle-frames/frames.ffconcat preserves observed frame timing; battle speed x1" : "none",
        Limitations = new[]
        {
            _v1JourneyResume ? "同一真实局经菜单继续恢复：本报告仅记录第三阶段六行动与最终首领；前十二行动、两场首领和开局/成长/装备交互引用只读前序证据。合计数不代表本次重跑十八行动，原奖励点击数不当作已提交奖励数。" :
            _v1Journey ? "当次正常开局与真实输入全征程；仅实际三首领胜利才通过，自然战败保留记录，不修改随机种子、供给、存档或模拟结果；生产分配只在自然取得生产者时执行" :
            _flowerOnly ? "仅验花灵自然开局、未分配服务零产出、真实选择培养对象、一次正常战斗、改投及精确继续；不代表自然取得概率、三阶段平衡或古树/铭记已接入" :
            _recruitmentMotionOnly ? "仅验正式招募呈现、免费退出、成功归队与重复输入阻断；不运行战斗或商店" :
            _mapLightOnly ? "仅验地图光效、行走详情框稳定、地点往返和减弱动效；不运行战斗或领奖" :
            _entranceMotionOnly ? "仅验菜单、六选二、地图移动与营地预览返回；短片保留真实帧时序和 Master 音轨，不运行战斗或全局流程" :
            _recruitmentStyleOnly ? "仅验正式三张卡样式、悬浮与键盘阅读，以及独立一/两候选布局；未重跑招募支付、取消库存或继续恢复合同" :
                _recruitmentOnly ? "自然新局三选一走真实键盘和招募输入；一、两名候选仅为独立共享界面布局夹具，不修改自然存档或声称旧库存应补货" :
                _keyboardOnly ? "局部键盘移动、服务与守军预览、窗口遮挡验收，不运行战斗或全局流程" :
                _resumeBoss ? "接续已有存档，提前迎战首领；只记录实际发生的阶段胜负与终局，不代表三阶段自然发育或平衡通过" :
                _serviceOnly ? "局部服务输入验收，未执行战斗或领奖" : _mapOnly ? "只检查首次地图定位和画面，未执行服务或战斗" : "随机正式开局，仅验一场普通战；若战败则未验胜利领奖输入",
            _v1Journey ? "每场战斗分开记录正常速度片段及真实实战结果；片段与非空名称断言不代表最终美术、动作观感、全随机平衡或用户验收" :
            _resumeBoss ? "自然失败或超时会直接结束；只有实际获胜的阶段才覆盖 GUI 领奖和行动补充，其余分支由领域检查补充" : "阶段 Boss 和终局由领域边界检查另行覆盖",
            "未作多分辨率、全部随机种子、整体平衡或用户观感验收"
        }
    }, new JsonSerializerOptions { WriteIndented = true }));

    private void Check(string contract) { _checks.Add(contract); GD.Print("EXPEDITION_INPUT_CHECK " + contract); }
    private static string? Argument(string name) => OS.GetCmdlineUserArgs().FirstOrDefault(value => value.StartsWith("--" + name + "=", StringComparison.Ordinal))?.Split('=', 2)[1];
    private static IEnumerable<T> Descendants<T>(Node node) where T : Node
    {
        foreach (var child in node.GetChildren()) { if (child is T match) yield return match; foreach (var nested in Descendants<T>(child)) yield return nested; }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
