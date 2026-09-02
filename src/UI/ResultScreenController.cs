using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class ResultScreenController : Control
{
    public event Action? NewRunRequested;
    public event Action? MenuRequested;

    private Label _title = null!;
    private Label _summary = null!;
    private Button _newRun = null!;
    private Button _menu = null!;

    public override void _Ready()
    {
        _title = GetNode<Label>("Center/Panel/Layout/Title");
        _summary = GetNode<Label>("Center/Panel/Layout/Summary");
        _newRun = GetNode<Button>("Center/Panel/Layout/NewRunButton");
        _menu = GetNode<Button>("Center/Panel/Layout/MenuButton");
        _newRun.Pressed += OnNewRun;
        _menu.Pressed += OnMenu;
    }

    public override void _ExitTree()
    {
        _newRun.Pressed -= OnNewRun;
        _menu.Pressed -= OnMenu;
    }

    public void Bind(string title, string summary)
    {
        _title.Text = title;
        _summary.Text = summary;
    }

    private void OnNewRun() => NewRunRequested?.Invoke();
    private void OnMenu() => MenuRequested?.Invoke();
}
