using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

namespace TowerAutobattler.App;

public enum AppScreenId
{
    MainMenu,
    HeroSelection,
    Tower,
    Deployment,
    Battle,
    BattleReport,
    BattleLab,
    Reward,
    Recruitment,
    Shop,
    Event,
    Rest,
    Result,
    Settings
}

// Authored screen composition root. It owns direct-child discovery and exposes
// typed controllers; GameRoot never reaches into screen-local node paths.
public partial class AppScreenHost : Control
{
    [Export] public NodePath ArmyOverviewPath { get; set; } = "../ArmyOverview";

    private readonly Dictionary<AppScreenId, Control> _screens = [];
    private ScreenRouter _router = null!;

    public MainMenuScreenController MainMenu { get; private set; } = null!;
    public HeroSelectScreen HeroSelection { get; private set; } = null!;
    public TowerScreenController Tower { get; private set; } = null!;
    public DeploymentScreenController Deployment { get; private set; } = null!;
    public BattleScreenController Battle { get; private set; } = null!;
    public BattleReportScreen BattleReport { get; private set; } = null!;
    public BattleLabScreenController BattleLab { get; private set; } = null!;
    public RewardScreenController Reward { get; private set; } = null!;
    public RewardScreenController Recruitment { get; private set; } = null!;
    public ShopScreenController Shop { get; private set; } = null!;
    public EventScreenController Event { get; private set; } = null!;
    public RestScreenController Rest { get; private set; } = null!;
    public ResultScreenController Result { get; private set; } = null!;
    public SettingsScreenController Settings { get; private set; } = null!;

    public override void _Ready()
    {
        MainMenu = GetNode<MainMenuScreenController>("MainMenuScreen");
        HeroSelection = GetNode<HeroSelectScreen>("HeroSelectScreen");
        Tower = GetNode<TowerScreenController>("TowerScreen");
        Deployment = GetNode<DeploymentScreenController>("DeploymentScreen");
        Battle = GetNode<BattleScreenController>("BattleScreen");
        BattleReport = GetNode<BattleReportScreen>("BattleReportScreen");
        BattleLab = GetNode<BattleLabScreenController>("BattleLabScreen");
        Reward = GetNode<RewardScreenController>("RewardScreen");
        Recruitment = GetNode<RewardScreenController>("RecruitmentScreen");
        Shop = GetNode<ShopScreenController>("ShopScreen");
        Event = GetNode<EventScreenController>("EventScreen");
        Rest = GetNode<RestScreenController>("RestScreen");
        Result = GetNode<ResultScreenController>("ResultScreen");
        Settings = GetNode<SettingsScreenController>("SettingsScreen");
        _screens.Add(AppScreenId.MainMenu, MainMenu);
        _screens.Add(AppScreenId.HeroSelection, HeroSelection);
        _screens.Add(AppScreenId.Tower, Tower);
        _screens.Add(AppScreenId.Deployment, Deployment);
        _screens.Add(AppScreenId.Battle, Battle);
        _screens.Add(AppScreenId.BattleReport, BattleReport);
        _screens.Add(AppScreenId.BattleLab, BattleLab);
        _screens.Add(AppScreenId.Reward, Reward);
        _screens.Add(AppScreenId.Recruitment, Recruitment);
        _screens.Add(AppScreenId.Shop, Shop);
        _screens.Add(AppScreenId.Event, Event);
        _screens.Add(AppScreenId.Rest, Rest);
        _screens.Add(AppScreenId.Result, Result);
        _screens.Add(AppScreenId.Settings, Settings);

        var armyOverview = GetNode<ArmyOverviewController>(ArmyOverviewPath);
        armyOverview.BindModalFocusScope(this);
        _router = new ScreenRouter(
            _screens.Values.ToArray(),
            [Tower, Deployment, Reward, Recruitment, Shop, Event, Rest],
            armyOverview);
    }

    public Control Screen(AppScreenId id) => _screens.TryGetValue(id, out var screen)
        ? screen
        : throw new ArgumentOutOfRangeException(nameof(id));

    public void Show(
        AppScreenId id,
        ActiveRunDto? run,
        ContentRegistry? content,
        CompiledRunRules? rules) =>
        _router.Show(Screen(id), run, content, rules);
}
