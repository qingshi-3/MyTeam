using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class MainMenuScreenController : Control
{
    public event Action? NewRunRequested;
    public event Action? ContinueRequested;
    public event Action? BattleLabRequested;
    public event Action? SettingsRequested;
    public event Action? QuitRequested;

    private Button _newRun = null!;
    private Button _continue = null!;
    private Button _settings = null!;
    private Button _battleLab = null!;
    private Button _quit = null!;

    public override void _Ready()
    {
        _newRun = GetNode<Button>("Center/Panel/Menu/NewRunButton");
        _continue = GetNode<Button>("Center/Panel/Menu/ContinueButton");
        _settings = GetNode<Button>("Center/Panel/Menu/SettingsButton");
        _battleLab = GetNode<Button>("Center/Panel/Menu/BattleLabButton");
        _quit = GetNode<Button>("Center/Panel/Menu/QuitButton");
        _newRun.Pressed += OnNewRun;
        _continue.Pressed += OnContinue;
        _settings.Pressed += OnSettings;
        _battleLab.Pressed += OnBattleLab;
        _quit.Pressed += OnQuit;
    }

    public override void _ExitTree()
    {
        _newRun.Pressed -= OnNewRun;
        _continue.Pressed -= OnContinue;
        _settings.Pressed -= OnSettings;
        _battleLab.Pressed -= OnBattleLab;
        _quit.Pressed -= OnQuit;
    }

    public void Bind(bool canContinue) => _continue.Disabled = !canContinue;

    private void OnNewRun() => NewRunRequested?.Invoke();
    private void OnContinue() => ContinueRequested?.Invoke();
    private void OnSettings() => SettingsRequested?.Invoke();
    private void OnBattleLab() => BattleLabRequested?.Invoke();
    private void OnQuit() => QuitRequested?.Invoke();
}
