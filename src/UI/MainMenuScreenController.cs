using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class MainMenuScreenController : Control
{
    public Control JourneySurface => GetNode<Control>("Center/Panel");
    public event Action? NewRunRequested;
    public event Action? ContinueRequested;
    public event Action? BattleLabRequested;
    public event Action? ExperienceSliceRequested;
    public event Action? VfxPreviewRequested;
    public event Action? EnvironmentPreviewRequested;
    public event Action? AbilityPrototypeRequested;
    public event Action? GrowthJourneyRequested;
    public event Action? SettingsRequested;
    public event Action? QuitRequested;

    private Button _newRun = null!;
    private Button _continue = null!;
    private Button _settings = null!;
    private Button _battleLab = null!;
    private Button _experience = null!;
    private Button _quit = null!;
    private Button _vfx = null!;
    private Button _environment = null!;
    private Button _growth = null!;
    private Button _tools = null!;
    private Button _prototype = null!;

    public override void _Ready()
    {
        _newRun = GetNode<Button>("Center/Panel/Menu/NewRunButton");
        _continue = GetNode<Button>("Center/Panel/Menu/ContinueButton");
        _settings = GetNode<Button>("Center/Panel/Menu/SettingsButton");
        _battleLab = GetNode<Button>("Center/Panel/Menu/BattleLabButton");
        _experience = GetNode<Button>("Center/Panel/Menu/ExperienceSliceButton");
        _quit = GetNode<Button>("Center/Panel/Menu/QuitButton");
        _vfx = GetNode<Button>("Center/Panel/Menu/VfxPreviewButton");
        _environment = GetNode<Button>("Center/Panel/Menu/EnvironmentPreviewButton");
        _growth = GetNode<Button>("Center/Panel/Menu/GrowthJourneyButton");
        _tools = GetNode<Button>("Center/Panel/Menu/ToolsButton");
        _prototype = GetNode<Button>("Center/Panel/Menu/AbilityPrototypeButton");
        _prototype.Pressed += OnPrototype;
        _tools.Toggled += SetToolsExpanded;
        _growth.Pressed += OnGrowth;
        _vfx.Pressed += OnVfx;
        _environment.Pressed += OnEnvironment;
        _newRun.Pressed += OnNewRun;
        _continue.Pressed += OnContinue;
        _settings.Pressed += OnSettings;
        _battleLab.Pressed += OnBattleLab;
        _experience.Pressed += OnExperience;
        _quit.Pressed += OnQuit;
    }

    public override void _ExitTree()
    {
        _newRun.Pressed -= OnNewRun;
        _continue.Pressed -= OnContinue;
        _settings.Pressed -= OnSettings;
        _battleLab.Pressed -= OnBattleLab;
        _experience.Pressed -= OnExperience;
        _quit.Pressed -= OnQuit;
        _vfx.Pressed -= OnVfx;
        _environment.Pressed -= OnEnvironment;
        _growth.Pressed -= OnGrowth;
        _tools.Toggled -= SetToolsExpanded;
        _prototype.Pressed -= OnPrototype;
    }

    public void Bind(bool canContinue)
    {
        _continue.Disabled = !canContinue;
        _tools.SetPressedNoSignal(false);
        SetToolsExpanded(false);
    }

    public void BindJourney(bool isGrowth)
    {
        GetNode<Label>("Center/Panel/Menu/Title").Text = isGrowth ? "军团登塔" : "旧版征程";
        GetNode<Label>("Center/Panel/Menu/Subtitle").Text = isGrowth
            ? "穿越迷雾，结伴远征" : "集结军团，攻上三重高塔";
        _growth.Text = isGrowth ? "旧版征程（开发）" : "返回正式征程";
        _growth.TooltipText = isGrowth ? "保留旧版玩法与旧存档入口。" : "进入当前成长玩法。";
    }

    private void SetToolsExpanded(bool expanded)
    {
        _newRun.Visible = !expanded;
        _continue.Visible = !expanded;
        _settings.Visible = !expanded;
        // Retired content is not a second playable journey or practice slice.
        _growth.Visible = false;
        _battleLab.Visible = expanded;
        _experience.Visible = false;
        _prototype.Visible = expanded;
        _vfx.Visible = expanded;
        _environment.Visible = expanded;
        _tools.Text = expanded ? "收起练习与预览 ▴" : "练习与预览 ▾";
    }

    private void OnNewRun() => NewRunRequested?.Invoke();
    private void OnContinue() => ContinueRequested?.Invoke();
    private void OnSettings() => SettingsRequested?.Invoke();
    private void OnBattleLab() => BattleLabRequested?.Invoke();
    private void OnExperience() => ExperienceSliceRequested?.Invoke();
    private void OnQuit() => QuitRequested?.Invoke();
    private void OnVfx() => VfxPreviewRequested?.Invoke();
    private void OnEnvironment() => EnvironmentPreviewRequested?.Invoke();
    private void OnGrowth() => GrowthJourneyRequested?.Invoke();
    private void OnPrototype() => AbilityPrototypeRequested?.Invoke();
}
