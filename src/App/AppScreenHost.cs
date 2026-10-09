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
    Expedition,
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
    [Export] public NodePath EnvironmentPath { get; set; } = "../Background";

    private readonly Dictionary<AppScreenId, Control> _screens = [];
    private ScreenRouter _router = null!;
    private ArmyOverviewController _armyOverview = null!;
    private Control _environment = null!;
    private UiJourneyTransition _journey = null!;
    public bool IsTransitioning => _journey is not null && _journey.IsPlaying
        || Recruitment is not null && Recruitment.IsRecruitmentReceiptPlaying;
    public event Action? GrowthChanged;

    public MainMenuScreenController MainMenu { get; private set; } = null!;
    public HeroSelectScreen HeroSelection { get; private set; } = null!;
    public TowerScreenController Tower { get; private set; } = null!;
    public ExpeditionScreenController Expedition { get; private set; } = null!;
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
        Expedition = GetNode<ExpeditionScreenController>("ExpeditionScreen");
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
        _screens.Add(AppScreenId.Expedition, Expedition);
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
        _environment = GetNode<Control>(EnvironmentPath);
        _journey = GetNode<UiJourneyTransition>("JourneyTransition");
        _armyOverview = armyOverview;
        _armyOverview.EquipmentChanged += RefreshEquipmentDisplay;
        _armyOverview.GrowthChanged += RefreshGrowthDisplay;
        _armyOverview.OpenChanged += Deployment.SetGlobalInspectionOpen;
        _armyOverview.OpenChanged += Expedition.SetGlobalInspectionOpen;
        Expedition.SetGlobalInspectionOpen(_armyOverview.IsOpen);
        Deployment.EquipmentOverlayChanged += _armyOverview.SetPageInspectionOpen;
        Deployment.LoadoutChanged += _armyOverview.RefreshTraits;
        armyOverview.BindModalFocusScope(this);
        _router = new ScreenRouter(
            _screens.Values.ToArray(),
            [Tower, Expedition, Deployment, Reward, Recruitment, Shop, Event, Rest],
            armyOverview,
            [Tower, Expedition, Deployment, Reward, Recruitment, Shop, Event, Rest, Battle, BattleReport]);
    }

    public Control Screen(AppScreenId id) => _screens.TryGetValue(id, out var screen)
        ? screen
        : throw new ArgumentOutOfRangeException(nameof(id));

    public override void _ExitTree()
    {
        if (_armyOverview is not null) _armyOverview.EquipmentChanged -= RefreshEquipmentDisplay;
        if (_armyOverview is not null) _armyOverview.GrowthChanged -= RefreshGrowthDisplay;
        if (_armyOverview is not null) _armyOverview.OpenChanged -= Deployment.SetGlobalInspectionOpen;
        if (_armyOverview is not null) _armyOverview.OpenChanged -= Expedition.SetGlobalInspectionOpen;
        if (Deployment is not null && _armyOverview is not null)
        {
            Deployment.EquipmentOverlayChanged -= _armyOverview.SetPageInspectionOpen;
            Deployment.LoadoutChanged -= _armyOverview.RefreshTraits;
        }
    }

    public void BindEquipmentManagement(RunApplication app) => _armyOverview.BindEquipmentManagement(app);
    public void BindJourneySound(Action<string> play)
    {
        _journey.PlaySound = play;
        Recruitment.PlaySound = play;
    }

    // The receipt shows an already saved acquisition. Refresh the persistent
    // counters now, without rebinding/removing the source card or routing again.
    public void RefreshRunOverview(RunApplication app)
    {
        BindEquipmentManagement(app);
        if (app.Run is { } run)
            _armyOverview.Bind(ArmyOverviewFactory.Build(run, app.Content, app.Rules));
    }

    public override void _Input(InputEvent input)
    {
        // This also covers the sibling global army button, outside the page's
        // own blocker. The short receipt cannot trigger a second UI operation.
        if (Recruitment is not null && Recruitment.IsRecruitmentReceiptPlaying &&
            input is InputEventKey or InputEventMouse or InputEventJoypadButton or InputEventJoypadMotion)
            GetViewport().SetInputAsHandled();
    }
    private void RefreshEquipmentDisplay() => Deployment.RefreshEquipment();
    private void RefreshGrowthDisplay() => GrowthChanged?.Invoke();
    public void OpenGrowthWorkbench() => _armyOverview.OpenGrowthWorkbench();
    public void OpenArmyOverview() => _armyOverview.OpenArmyOverview();

    public void Show(
        AppScreenId id,
        RunView? run,
        ContentRegistry? content,
        CompiledRunRules? rules)
    {
        _journey.Cancel();
        var from = _router.Current;
        var target = Screen(id);
        var entry = target == Expedition && (from is null || from == MainMenu || from == HeroSelection);
        if (entry) Expedition.PrepareForJourneyEntry();
        var animate = from != target && (from is null && target == MainMenu
            || from == MainMenu && (target == HeroSelection || target == Expedition)
            || target == MainMenu && (from == HeroSelection || from == Expedition)
            || from == HeroSelection && target == Expedition
            || from == Expedition && IsMapVisit(target)
            || target == Expedition && from is not null && IsMapVisit(from));
        _router.Show(target, run, content, rules, animate ? from : null, !animate);
        if (animate)
            _journey.Begin(from, target, Expedition, _armyOverview,
                from == HeroSelection && target == Expedition, _router.FocusCurrent);
        // The battle owns its paused/scaled scenery. Other screens use the
        // root's idle courtyard, avoiding two full-screen atmosphere draws.
        _environment.Visible = id != AppScreenId.Battle;
    }

    private bool IsMapVisit(Control screen) => screen == Recruitment || screen == Reward || screen == Shop || screen == Event
        || screen == Rest || screen == Deployment;
}
