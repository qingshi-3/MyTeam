using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

namespace TowerAutobattler.App;

public partial class GameRoot : Control
{
    [Export] public string SaveNamespace { get; set; } = string.Empty;
    private RunApplication? _app;
    private PackedScene _choiceCard = null!;
    private PackedScene _unitChoiceCard = null!;
    private readonly List<Control> _screens = [];
    private Control _main = null!;
    private HeroSelectScreen _heroes = null!;
    private Control _tower = null!;
    private DeploymentScreenController _deployment = null!;
    private BattleScreenController _battle = null!;
    private BattleReportScreen _battleReport = null!;
    private Control _reward = null!;
    private Control _recruitment = null!;
    private Control _shop = null!;
    private Control _event = null!;
    private Control _rest = null!;
    private Control _result = null!;
    private Control _settings = null!;
    private ArmyOverviewController _armyOverview = null!;
    private EncounterPlan? _encounter;
    private BattleResult? _pendingBattleResult;
    private PostBattleRoute _postBattleRoute;
    private string _pendingResultTitle = string.Empty;
    private string _pendingResultSummary = string.Empty;
    private string _pendingEncounterTitle = string.Empty;
    private bool _battleResolutionCommitted;
    private bool _battleReportShown;
    private bool _battleReportContinued;
    private RewardMode _rewardMode;
    private SemanticIconCatalog _semanticIcons = null!;
    private bool _buttonsWired;
    public ContentRegistry? Content => _app?.Content;

    public override async void _Ready()
    {
        _choiceCard = GD.Load<PackedScene>("res://scenes/ui/components/ChoiceCard.tscn");
        _unitChoiceCard = GD.Load<PackedScene>("res://scenes/ui/components/UnitChoiceCard.tscn");
        _semanticIcons = SemanticIcons.Catalog;
        CacheScreens();
        WireButtons();
        var semanticReport = _semanticIcons.Validate();
        if (semanticReport.HasCoreErrors)
        {
            ShowResult("界面资源校验失败", string.Join("\n", semanticReport.CoreErrors));
            return;
        }
        var catalog = GD.Load<ContentCatalog>("res://content/catalogs/alpha_catalog.tres");
        var gate = await ContentRegistry.CreateReadyAsync(this, catalog);
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        if (gate.Registry is not { } registry)
        {
            ShowResult("内容校验失败", string.Join("\n", gate.Report.CoreErrors));
            return;
        }
        var regions = new[]
        {
            GD.Load<TowerRegionDefinition>("res://content/tower/region_ember_foundry.tres"),
            GD.Load<TowerRegionDefinition>("res://content/tower/region_gloam_crypt.tres"),
            GD.Load<TowerRegionDefinition>("res://content/tower/region_crown_engine.tres")
        };
        _app = new RunApplication(registry, new SaveService(SaveNamespace), regions);
        ShowMainMenu();
    }

    public override void _ExitTree()
    {
        UnwireButtons();
    }

    private void CacheScreens()
    {
        _main = GetNode<Control>("Screens/MainMenuScreen");
        _heroes = GetNode<HeroSelectScreen>("Screens/HeroSelectScreen");
        _tower = GetNode<Control>("Screens/TowerScreen");
        _deployment = GetNode<DeploymentScreenController>("Screens/DeploymentScreen");
        _battle = GetNode<BattleScreenController>("Screens/BattleScreen");
        _battleReport = GetNode<BattleReportScreen>("Screens/BattleReportScreen");
        _reward = GetNode<Control>("Screens/RewardScreen");
        _recruitment = GetNode<Control>("Screens/RecruitmentScreen");
        _shop = GetNode<Control>("Screens/ShopScreen");
        _event = GetNode<Control>("Screens/EventScreen");
        _rest = GetNode<Control>("Screens/RestScreen");
        _result = GetNode<Control>("Screens/ResultScreen");
        _settings = GetNode<Control>("Screens/SettingsScreen");
        _armyOverview = GetNode<ArmyOverviewController>("ArmyOverview");
        _armyOverview.BindModalFocusScope(GetNode<Control>("Screens"));
        _screens.AddRange([_main, _heroes, _tower, _deployment, _battle, _battleReport, _reward, _recruitment, _shop, _event, _rest, _result, _settings]);
    }

    private void WireButtons()
    {
        if (_buttonsWired) return;
        Button(_main, "Center/Panel/Menu/NewRunButton").Pressed += ShowHeroSelection;
        _heroes.HeroChosen += StartNewRun;
        Button(_main, "Center/Panel/Menu/ContinueButton").Pressed += ContinueRun;
        Button(_main, "Center/Panel/Menu/SettingsButton").Pressed += ShowSettings;
        Button(_main, "Center/Panel/Menu/QuitButton").Pressed += QuitGame;
        Button(_heroes, "Margin/Layout/BackButton").Pressed += ShowMainMenu;
        Button(_tower, "Margin/Layout/AbandonButton").Pressed += AbandonRun;
        _deployment.BackRequested += ShowTower;
        _deployment.StartRequested += StartBattle;
        _deployment.MoveRequested += MoveDeploymentUnit;
        _deployment.WithdrawRequested += WithdrawDeploymentUnit;
        Button(_reward, "Center/Panel/Layout/ConvertButton").Pressed += ConvertRecruit;
        Button(_reward, "Center/Panel/Layout/ContinueButton").Pressed += SkipReward;
        Button(_recruitment, "Center/Panel/Layout/ConvertButton").Pressed += ConvertRecruit;
        Button(_recruitment, "Center/Panel/Layout/ContinueButton").Pressed += SkipReward;
        Button(_shop, "Margin/Layout/LeaveButton").Pressed += LeaveShop;
        Button(_event, "Center/Panel/Layout/RiskButton").Pressed += ResolveRiskyEvent;
        Button(_event, "Center/Panel/Layout/SafeButton").Pressed += ResolveSafeEvent;
        Button(_rest, "Center/Panel/Layout/RecoverButton").Pressed += RecoverAtRest;
        Button(_rest, "Center/Panel/Layout/GoldButton").Pressed += TakeRestGold;
        Button(_result, "Center/Panel/Layout/NewRunButton").Pressed += ShowHeroSelection;
        Button(_result, "Center/Panel/Layout/MenuButton").Pressed += ShowMainMenu;
        Button(_settings, "Center/Panel/Layout/SaveButton").Pressed += SaveSettings;
        _battle.Finished += OnBattleFinished;
        _battle.EndTransitionFinished += OnBattleEndTransitionFinished;
        _battleReport.ContinueRequested += OnBattleReportContinue;
        _buttonsWired = true;
    }

    private void UnwireButtons()
    {
        if (!_buttonsWired) return;
        Button(_main, "Center/Panel/Menu/NewRunButton").Pressed -= ShowHeroSelection;
        _heroes.HeroChosen -= StartNewRun;
        Button(_main, "Center/Panel/Menu/ContinueButton").Pressed -= ContinueRun;
        Button(_main, "Center/Panel/Menu/SettingsButton").Pressed -= ShowSettings;
        Button(_main, "Center/Panel/Menu/QuitButton").Pressed -= QuitGame;
        Button(_heroes, "Margin/Layout/BackButton").Pressed -= ShowMainMenu;
        Button(_tower, "Margin/Layout/AbandonButton").Pressed -= AbandonRun;
        _deployment.BackRequested -= ShowTower;
        _deployment.StartRequested -= StartBattle;
        _deployment.MoveRequested -= MoveDeploymentUnit;
        _deployment.WithdrawRequested -= WithdrawDeploymentUnit;
        Button(_reward, "Center/Panel/Layout/ConvertButton").Pressed -= ConvertRecruit;
        Button(_reward, "Center/Panel/Layout/ContinueButton").Pressed -= SkipReward;
        Button(_recruitment, "Center/Panel/Layout/ConvertButton").Pressed -= ConvertRecruit;
        Button(_recruitment, "Center/Panel/Layout/ContinueButton").Pressed -= SkipReward;
        Button(_shop, "Margin/Layout/LeaveButton").Pressed -= LeaveShop;
        Button(_event, "Center/Panel/Layout/RiskButton").Pressed -= ResolveRiskyEvent;
        Button(_event, "Center/Panel/Layout/SafeButton").Pressed -= ResolveSafeEvent;
        Button(_rest, "Center/Panel/Layout/RecoverButton").Pressed -= RecoverAtRest;
        Button(_rest, "Center/Panel/Layout/GoldButton").Pressed -= TakeRestGold;
        Button(_result, "Center/Panel/Layout/NewRunButton").Pressed -= ShowHeroSelection;
        Button(_result, "Center/Panel/Layout/MenuButton").Pressed -= ShowMainMenu;
        Button(_settings, "Center/Panel/Layout/SaveButton").Pressed -= SaveSettings;
        _battle.Finished -= OnBattleFinished;
        _battle.EndTransitionFinished -= OnBattleEndTransitionFinished;
        _battleReport.ContinueRequested -= OnBattleReportContinue;
        _buttonsWired = false;
    }

    private void ShowMainMenu()
    {
        if (_app is not null) Button(_main, "Center/Panel/Menu/ContinueButton").Disabled = _app.ActiveRun is null;
        Show(_main);
    }

    private void ShowHeroSelection()
    {
        if (_app is null) return;
        var heroes = new List<HeroSelectionViewModel>();
        foreach (var entry in _app.Content.Catalog.Heroes)
        {
            var definition = (UnitDefinition)entry.Definition;
            var unlocked = _app.Meta.UnlockedHeroIds.Contains(entry.StableId);
            var root = entry.Scene.Instantiate<UnitContentRoot>();
            try
            {
                var rule = root.HeroRule;
                var command = root.HeroCommand;
                heroes.Add(new HeroSelectionViewModel(
                    entry.StableId,
                    definition,
                    unlocked,
                    rule?.RuleTitle ?? "军团规则",
                    rule?.RuleDescription ?? definition.Description,
                    command?.DisplayName ?? "无战场指令",
                    command?.Description ?? "该英雄没有可发动的战场指令。",
                    command?.ManaCost ?? 0,
                    command?.GoldCost ?? 0));
            }
            finally { root.Free(); }
        }
        _heroes.Bind(heroes);
        Show(_heroes);
    }

    private void StartNewRun(string heroId)
    {
        if (_app?.StartNewRun(heroId, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) == true) ShowTower();
    }

    private void ContinueRun()
    {
        if (_app?.ActiveRun is null) { ShowHeroSelection(); return; }
        if (_app.ActiveRun.PendingNode) OpenSelectedNode(); else ShowTower();
    }

    private void ShowTower()
    {
        if (_app?.ActiveRun is not { } run) { ShowMainMenu(); return; }
        if (run.PendingNode) { OpenSelectedNode(); return; }
        var region = _app.Tower.RegionFor(run.FloorIndex);
        Label(_tower, "Margin/Layout/Title").Text = region.DisplayName;
        Label(_tower, "Margin/Layout/RunInfo").Text = $"第 {run.FloorIndex + 1}/15 层　金币 {run.Gold}　英雄生命 {run.HeroHealthRatio:P0}";
        Label(_tower, "Margin/Layout/RosterInfo").Text = BuildRosterSummary(run);
        var choices = Container(_tower, "Margin/Layout/Choices");
        ClearChoices(choices);
        foreach (var option in _app.CurrentOptions())
            AddChoice(choices, option.Type.ToString(), option.Title, option.Description, $"风险 {option.Risk}",
                id => SelectNode(Enum.Parse<TowerNodeType>(id)),
                icon: _semanticIcons.ResolveIcon(SemanticIconKeys.TowerNodeSemantic(option.Type)),
                footerVariation: "WarningLabel", footerSemanticKey: SemanticIconKeys.Risk);
        Show(_tower);
    }

    private void SelectNode(TowerNodeType type)
    {
        if (_app?.SelectNode(type) == true) OpenSelectedNode();
    }

    private void OpenSelectedNode()
    {
        if (_app?.ActiveRun is not { } run) { ShowMainMenu(); return; }
        switch (run.SelectedNode)
        {
            case TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss:
                _encounter = _app.CurrentEncounter();
                ShowDeployment();
                break;
            case TowerNodeType.Recruitment: ShowRecruitment(); break;
            case TowerNodeType.Shop: ShowShop(); break;
            case TowerNodeType.Event: Show(_event); break;
            case TowerNodeType.Rest: Show(_rest); break;
        }
    }

    private void ShowDeployment()
    {
        if (_app?.ActiveRun is not { } run || _encounter is null) return;
        var units = run.Roster.Select(instance =>
        {
            var definition = (UnitDefinition)Required(instance.ContentId).Definition;
            return new DeploymentUnitViewModel(instance.InstanceId, definition.DisplayName, definition.Description,
                instance.HealthRatio, definition.Role, definition.AttackRange, run.Deployment.IndexOf(instance.InstanceId), definition.Portrait);
        }).ToArray();
        _deployment.Bind(_encounter.Title, DescribeEncounter(_encounter), _app.BuildBattleConfig(_encounter), units, run.Deployment);
        Show(_deployment);
    }

    private void MoveDeploymentUnit(string instanceId, int slot)
    {
        if (_app?.MoveDeploymentUnit(instanceId, slot) == true) ShowDeployment();
        else _deployment.ShowMessage("部署操作无效，阵型未改变。", true);
    }

    private void WithdrawDeploymentUnit(string instanceId)
    {
        if (_app?.WithdrawDeploymentUnit(instanceId) == true) ShowDeployment();
        else _deployment.ShowMessage("只有已部署单位可以撤回，且后备区不能超过 3 人。", true);
    }

    private void StartBattle()
    {
        if (_app is null || _encounter is null) return;
        ResetPendingBattleFlow();
        var config = _app.BuildBattleConfig(_encounter);
        Show(_battle);
        _battle.StartBattle(_app.Content, config, _encounter.Title, _app.Settings.DefaultBattleSpeed);
    }

    private void OnBattleFinished(BattleResult result)
    {
        if (_battleResolutionCommitted || _app is null || _encounter is null) return;
        var finalBoss = _app.ActiveRun?.FloorIndex == 14 && _encounter.IsBoss;
        var victory = _app.CompleteBattle(result, _encounter);
        _pendingBattleResult = result;
        _pendingEncounterTitle = _encounter.Title;
        _battleResolutionCommitted = true;
        if (!victory)
        {
            _postBattleRoute = PostBattleRoute.Failure;
            _pendingResultTitle = "征程失败";
            _pendingResultSummary = $"军团止步于第 {Math.Max(1, _app.Meta.HighestRegion)} 区。\n战斗摘要：{PlayerFacingText.DescribeBattleOutcome(result.Outcome)}，耗时 {result.Ticks * BattleSimulation.TickSeconds:0.0} 秒。";
            return;
        }
        if (finalBoss)
        {
            _postBattleRoute = PostBattleRoute.Success;
            _pendingResultTitle = "登塔成功";
            _pendingResultSummary = "塔顶主宰已被击败。新的英雄与更高难度正在等待下一次征程。\n本局战斗摘要：" + result.Digest[..12];
            return;
        }
        _postBattleRoute = PostBattleRoute.Reward;
    }

    private void OnBattleEndTransitionFinished()
    {
        if (!_battleResolutionCommitted || _battleReportShown || _pendingBattleResult is null || _app is null) return;
        _battleReportShown = true;
        Show(_battleReport);
        _battleReport.Bind(_pendingBattleResult, _pendingEncounterTitle, _app.Content);
    }

    private void OnBattleReportContinue()
    {
        if (!_battleReportShown || _battleReportContinued) return;
        _battleReportContinued = true;
        switch (_postBattleRoute)
        {
            case PostBattleRoute.Reward:
                ShowCombatReward();
                break;
            case PostBattleRoute.Success:
            case PostBattleRoute.Failure:
                ShowResult(_pendingResultTitle, _pendingResultSummary);
                break;
        }
    }

    private void ShowRecruitment()
    {
        if (_app is null) return;
        _rewardMode = RewardMode.Recruitment;
        Label(_recruitment, "Center/Panel/Layout/Title").Text = "征募新兵";
        Label(_recruitment, "Center/Panel/Layout/Hint").Text = "士兵是可替换的构筑部件：提供身体、阵型、标签和功能。选择一名加入军团。";
        var choices = Container(_recruitment, "Center/Panel/Layout/ChoiceScroll/Choices");
        ClearChoices(choices);
        foreach (var entry in _app.RecruitmentChoices())
        {
            var definition = (UnitDefinition)entry.Definition;
            AddUnitChoice(
                choices,
                entry.StableId,
                definition,
                definition.Description,
                "加入军团",
                ClaimRecruit,
                metaVariation: "PlayerLabel");
        }
        Button(_recruitment, "Center/Panel/Layout/ConvertButton").Visible = CurrentHeroConversion() > 0;
        Button(_recruitment, "Center/Panel/Layout/ContinueButton").Text = "跳过征募，继续登塔";
        Show(_recruitment);
    }

    private void ShowCombatReward()
    {
        if (_app is null) return;
        _rewardMode = RewardMode.Combat;
        Label(_reward, "Center/Panel/Layout/Title").Text = "战斗胜利"
            ;
        Label(_reward, "Center/Panel/Layout/Hint").Text = "选择一件战利品。物品以独立场景提供经济、阵型、召唤或英雄强化。";
        var choices = Container(_reward, "Center/Panel/Layout/ChoiceScroll/Choices");
        ClearChoices(choices);
        foreach (var entry in _app.ItemChoices(37))
        {
            var definition = (ItemDefinition)entry.Definition;
            AddChoice(choices, entry.StableId, definition.DisplayName, definition.Description, PlayerFacingText.DescribeItemRarity(definition.Rarity),
                ClaimItem, icon: definition.Icon ?? _semanticIcons.ResolveIcon(SemanticIconKeys.Loot),
                titleVariation: definition.Rarity == ItemRarity.Legendary ? new StringName("HeroLabel") : null,
                footerVariation: definition.Rarity == ItemRarity.Legendary ? "HeroLabel" : "PlayerLabel");
        }
        Button(_reward, "Center/Panel/Layout/ConvertButton").Visible = false;
        Button(_reward, "Center/Panel/Layout/ContinueButton").Text = "放弃战利品，继续登塔";
        Show(_reward);
    }

    private void ClaimRecruit(string id)
    {
        if (_app?.Recruit(id) == true) { _app.FinishNonCombatNode(); ShowTower(); }
    }

    private void ClaimItem(string id)
    {
        if (_app?.GrantItem(id) == true) ShowTower();
    }

    private void ConvertRecruit()
    {
        if (_rewardMode != RewardMode.Recruitment || _app is null) return;
        _app.ConvertRecruitToGold();
        _app.FinishNonCombatNode();
        ShowTower();
    }

    private void SkipReward()
    {
        if (_rewardMode == RewardMode.Recruitment) _app?.FinishNonCombatNode();
        ShowTower();
    }

    private void ShowShop()
    {
        if (_app?.ActiveRun is not { } run) return;
        Label(_shop, "Margin/Layout/Gold").Text = $"金币：{run.Gold}";
        var choices = Container(_shop, "Margin/Layout/Choices");
        ClearChoices(choices);
        foreach (var entry in _app.ItemChoices(91))
        {
            var definition = (ItemDefinition)entry.Definition;
            AddChoice(choices, entry.StableId, definition.DisplayName, definition.Description, $"售价 {definition.Price}", BuyItem,
                icon: definition.Icon ?? _semanticIcons.ResolveIcon(SemanticIconKeys.Loot), footerVariation: "HeroLabel");
        }
        Show(_shop);
    }

    private void BuyItem(string id)
    {
        Label(_shop, "Margin/Layout/Status").Text = _app?.BuyItem(id) == true ? "购买成功。" : "金币不足。";
        ShowShop();
    }

    private void LeaveShop() { _app?.FinishNonCombatNode(); ShowTower(); }
    private void ResolveEvent(bool risky) { _app?.ResolveEvent(risky); _app?.FinishNonCombatNode(); ShowTower(); }
    private void ResolveRest(bool gold) { _app?.Rest(gold); _app?.FinishNonCombatNode(); ShowTower(); }
    private void ResolveRiskyEvent() => ResolveEvent(true);
    private void ResolveSafeEvent() => ResolveEvent(false);
    private void RecoverAtRest() => ResolveRest(false);
    private void TakeRestGold() => ResolveRest(true);
    private void QuitGame() => GetTree().Quit();

    private void AbandonRun()
    {
        _app?.AbandonRun();
        ShowResult("征程已放弃", "本次军团已解散，已解锁的英雄与历史进度仍会保留。可随时再次登塔。");
    }

    private void ShowSettings()
    {
        if (_app is null) return;
        _settings.GetNode<HSlider>("Center/Panel/Layout/VolumeSlider").Value = _app.Settings.MasterVolume;
        _settings.GetNode<OptionButton>("Center/Panel/Layout/SpeedOption").Selected = _app.Settings.DefaultBattleSpeed switch { >= 4 => 2, >= 2 => 1, _ => 0 };
        Show(_settings);
    }

    private void SaveSettings()
    {
        if (_app is null) return;
        _app.Settings.MasterVolume = (float)_settings.GetNode<HSlider>("Center/Panel/Layout/VolumeSlider").Value;
        _app.Settings.DefaultBattleSpeed = _settings.GetNode<OptionButton>("Center/Panel/Layout/SpeedOption").Selected switch { 2 => 4f, 1 => 2f, _ => 1f };
        _app.SaveSettings();
        ShowMainMenu();
    }

    private void ShowResult(string title, string summary)
    {
        Label(_result, "Center/Panel/Layout/Title").Text = title;
        Label(_result, "Center/Panel/Layout/Summary").Text = summary;
        Show(_result);
    }

    private void Show(Control target)
    {
        foreach (var screen in _screens) screen.Visible = screen == target;
        var showArmy = _app?.ActiveRun is not null &&
            (target == _tower || target == _deployment || target == _reward || target == _recruitment || target == _shop || target == _event || target == _rest);
        _armyOverview.Visible = showArmy;
        if (showArmy && _app?.ActiveRun is { } run)
            _armyOverview.Bind(ArmyOverviewFactory.Build(run, _app.Content));
        else
            _armyOverview.Close();
        foreach (var node in target.FindChildren("*", "Button", true, false))
            if (node is Button { Disabled: false, Visible: true } button) { button.GrabFocus(); break; }
    }

    private ChoiceCard AddChoice(
        Container parent,
        string id,
        string title,
        string description,
        string footer,
        Action<string> action,
        bool disabled = false,
        Texture2D? icon = null,
        StringName? titleVariation = null,
        StringName? footerVariation = null,
        StringName? footerSemanticKey = null)
    {
        var card = _choiceCard.Instantiate<ChoiceCard>();
        parent.AddChild(card);
        card.Bind(id, title, description, footer, icon, titleVariation, footerVariation, footerSemanticKey);
        card.Disabled = disabled;
        card.ConnectChosen(action);
        return card;
    }

    private UnitChoiceCard AddUnitChoice(
        Container parent,
        string id,
        UnitDefinition definition,
        string description,
        string meta,
        Action<string> action,
        bool disabled = false,
        StringName? nameVariation = null,
        StringName? metaVariation = null,
        PackedScene? template = null)
    {
        var card = (template ?? _unitChoiceCard).Instantiate<UnitChoiceCard>();
        parent.AddChild(card);
        card.Bind(
            id,
            definition,
            definition.Icon ?? _semanticIcons.ResolveIcon(definition.IsHero
                ? SemanticIconKeys.Hero
                : definition.AttackRange > 3 ? SemanticIconKeys.Ranged : SemanticIconKeys.Melee),
            description,
            meta,
            nameVariation,
            metaVariation);
        card.Disabled = disabled;
        card.ConnectChosen(action);
        return card;
    }

    private void ResetPendingBattleFlow()
    {
        _pendingBattleResult = null;
        _postBattleRoute = PostBattleRoute.None;
        _pendingResultTitle = string.Empty;
        _pendingResultSummary = string.Empty;
        _pendingEncounterTitle = string.Empty;
        _battleResolutionCommitted = false;
        _battleReportShown = false;
        _battleReportContinued = false;
    }

    private static void ClearChoices(Container parent)
    {
        foreach (var child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.Free();
        }
    }

    private string BuildRosterSummary(ActiveRunDto run)
    {
        var hero = ((UnitDefinition)Required(run.HeroId).Definition).DisplayName;
        var soldiers = run.Roster.Select(unit => ((UnitDefinition)Required(unit.ContentId).Definition).DisplayName);
        var items = run.Items.Select(item => ((ItemDefinition)Required(item.ContentId).Definition).DisplayName);
        return $"英雄：{hero}\n军团：{string.Join("、", soldiers)}\n物品：{(run.Items.Count == 0 ? "无" : string.Join("、", items))}";
    }

    private string DescribeEncounter(EncounterPlan encounter)
    {
        var enemies = encounter.EnemyIds.Select(id => ((UnitDefinition)Required(id).Definition).DisplayName).GroupBy(name => name)
            .Select(group => group.Count() > 1 ? $"{group.Key}×{group.Count()}" : group.Key);
        var floorName = encounter.FloorRuleId;
        foreach (var scene in _app!.Content.Catalog.FloorRules)
        {
            var root = scene.Instantiate<FloorRuleContentRoot>();
            try
            {
                if (root.Id == encounter.FloorRuleId) floorName = $"{root.DisplayName}：{root.PreviewText}";
            }
            finally { root.Free(); }
        }
        return $"敌军：{string.Join("、", enemies)}\n楼层规则：{floorName}";
    }

    private int CurrentHeroConversion()
    {
        if (_app?.ActiveRun is not { } run) return 0;
        var root = Required(run.HeroId).Scene.Instantiate<UnitContentRoot>();
        try { return root.HeroRule?.RecruitConversionGold ?? 0; }
        finally { root.Free(); }
    }

    private CatalogEntry Required(string id) => _app?.Content.TryGet(id, out var entry) == true ? entry : throw new InvalidOperationException("Missing content: " + id);
    private static Button Button(Control root, string path) => root.GetNode<Button>(path);
    private static Label Label(Control root, string path) => root.GetNode<Label>(path);
    private static Container Container(Control root, string path) => root.GetNode<Container>(path);
    private enum RewardMode { Recruitment, Combat }
    private enum PostBattleRoute { None, Reward, Success, Failure }
}
