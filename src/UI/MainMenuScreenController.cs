using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class MainMenuScreenController : Control
{
    public event Action? NewRunRequested;
    public event Action? ContinueRequested;
    public event Action? BattleLabRequested;
    public event Action? ExperienceSliceRequested;
    public event Action? VfxPreviewRequested;
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
    private Button _growth = null!;
    private Button _tools = null!;

    public override void _Ready()
    {
        _newRun = GetNode<Button>("Center/Panel/Menu/NewRunButton");
        _continue = GetNode<Button>("Center/Panel/Menu/ContinueButton");
        _settings = GetNode<Button>("Center/Panel/Menu/SettingsButton");
        _battleLab = GetNode<Button>("Center/Panel/Menu/BattleLabButton");
        _experience = GetNode<Button>("Center/Panel/Menu/ExperienceSliceButton");
        _quit = GetNode<Button>("Center/Panel/Menu/QuitButton");
        _vfx = GetNode<Button>("Center/Panel/Menu/VfxPreviewButton");
        _growth = GetNode<Button>("Center/Panel/Menu/GrowthJourneyButton");
        _tools = GetNode<Button>("Center/Panel/Menu/ToolsButton");
        _tools.Toggled += SetToolsExpanded;
        _growth.Pressed += OnGrowth;
        _vfx.Pressed += OnVfx;
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
        _growth.Pressed -= OnGrowth;
        _tools.Toggled -= SetToolsExpanded;
    }

    public void Bind(bool canContinue) => _continue.Disabled = !canContinue;

    public void BindJourney(bool isGrowth)
    {
        GetNode<Label>("Center/Panel/Menu/Title").Text = isGrowth ? "成长征程" : "军团登塔";
        GetNode<Label>("Center/Panel/Menu/Subtitle").Text = isGrowth
            ? "培养伙伴，向高塔进发" : "集结军团，攻上三重高塔";
        _growth.Text = isGrowth ? "返回主菜单" : "成长征程";
    }

    private void SetToolsExpanded(bool expanded)
    {
        _battleLab.Visible = expanded;
        _experience.Visible = expanded;
        _vfx.Visible = expanded;
        _tools.Text = expanded ? "收起练习与预览 ▴" : "练习与预览 ▾";
    }

    private void OnNewRun() => NewRunRequested?.Invoke();
    private void OnContinue() => ContinueRequested?.Invoke();
    private void OnSettings() => SettingsRequested?.Invoke();
    private void OnBattleLab() => BattleLabRequested?.Invoke();
    private void OnExperience() => ExperienceSliceRequested?.Invoke();
    private void OnQuit() => QuitRequested?.Invoke();
    private void OnVfx() => VfxPreviewRequested?.Invoke();
    private void OnGrowth() => GrowthJourneyRequested?.Invoke();
}
