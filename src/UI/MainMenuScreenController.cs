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
    public event Action? SettingsRequested;
    public event Action? QuitRequested;

    private Button _newRun = null!;
    private Button _continue = null!;
    private Button _settings = null!;
    private Button _battleLab = null!;
    private Button _experience = null!;
    private Button _quit = null!;
    private Button _vfx = null!;

    public override void _Ready()
    {
        _newRun = GetNode<Button>("Center/Panel/Menu/NewRunButton");
        _continue = GetNode<Button>("Center/Panel/Menu/ContinueButton");
        _settings = GetNode<Button>("Center/Panel/Menu/SettingsButton");
        _battleLab = GetNode<Button>("Center/Panel/Menu/BattleLabButton");
        _experience = GetNode<Button>("Center/Panel/Menu/ExperienceSliceButton");
        _quit = GetNode<Button>("Center/Panel/Menu/QuitButton");
        _vfx = GetNode<Button>("Center/Panel/Menu/VfxPreviewButton");
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
    }

    public void Bind(bool canContinue) => _continue.Disabled = !canContinue;

    private void OnNewRun() => NewRunRequested?.Invoke();
    private void OnContinue() => ContinueRequested?.Invoke();
    private void OnSettings() => SettingsRequested?.Invoke();
    private void OnBattleLab() => BattleLabRequested?.Invoke();
    private void OnExperience() => ExperienceSliceRequested?.Invoke();
    private void OnQuit() => QuitRequested?.Invoke();
    private void OnVfx() => VfxPreviewRequested?.Invoke();
}
