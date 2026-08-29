using System;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class DeploymentScreenController : Control
{
    public event Action? BackRequested;
    public event Action? StartRequested;
    public event Action<string, int>? MoveRequested;
    public event Action<string>? WithdrawRequested;

    private Label _title = null!;
    private Label _encounter = null!;
    private Label _status = null!;
    private VBoxContainer _roster = null!;
    private DeploymentBoard _board = null!;
    private Button _withdraw = null!;
    private Button _back = null!;
    private Button _start = null!;
    private PackedScene _cardScene = null!;
    private IReadOnlyList<DeploymentUnitViewModel> _units = [];
    private IReadOnlyList<string> _deployment = [];
    private BattleConfig? _config;
    private string _selectedId = string.Empty;

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _encounter = GetNode<Label>("%EncounterInfo");
        _status = GetNode<Label>("%Status");
        _roster = GetNode<VBoxContainer>("%RosterChoices");
        _board = GetNode<DeploymentBoard>("%DeploymentBoard");
        _withdraw = GetNode<Button>("%WithdrawButton");
        _back = GetNode<Button>("%BackButton");
        _start = GetNode<Button>("%StartBattleButton");
        _cardScene = GD.Load<PackedScene>("res://scenes/ui/components/DeploymentUnitCard.tscn");
        _board.SlotSelected += OnSlotSelected;
        _board.UnitDropped += OnUnitDropped;
        _withdraw.Pressed += OnWithdraw;
        _back.Pressed += OnBack;
        _start.Pressed += OnStart;
    }

    public override void _ExitTree()
    {
        _board.SlotSelected -= OnSlotSelected;
        _board.UnitDropped -= OnUnitDropped;
        _withdraw.Pressed -= OnWithdraw;
        _back.Pressed -= OnBack;
        _start.Pressed -= OnStart;
    }

    public void Bind(string title, string encounter, BattleConfig config, IReadOnlyList<DeploymentUnitViewModel> units, IReadOnlyList<string> deployment)
    {
        _title.Text = title;
        _encounter.Text = encounter;
        _config = config;
        _units = units;
        _deployment = deployment;
        if (!string.IsNullOrEmpty(_selectedId) && !ContainsUnit(_selectedId)) _selectedId = string.Empty;
        Refresh();
        ShowMessage("拖放单位，或先选单位再选部署锚点；占用锚点会替换或原子交换。", false);
    }

    public void ShowMessage(string message, bool error)
    {
        _status.Text = message;
        _status.Modulate = error ? new Color(1f, .48f, .42f) : Colors.White;
    }

    private void Refresh()
    {
        foreach (var child in _roster.GetChildren()) { _roster.RemoveChild(child); child.Free(); }
        foreach (var unit in _units)
        {
            var card = _cardScene.Instantiate<DeploymentUnitCard>();
            _roster.AddChild(card);
            card.Bind(unit, unit.InstanceId == _selectedId);
            card.UnitSelected += OnUnitSelected;
        }
        if (_config is not null) _board.Bind(_config, _units, _deployment, _selectedId);
        var reserveCount = _units.Count - CountDeployed();
        _withdraw.Disabled = string.IsNullOrEmpty(_selectedId) || IndexOfDeployment(_selectedId) < 0 || reserveCount >= RunApplication.ReserveCapacity;
        _withdraw.TooltipText = reserveCount >= RunApplication.ReserveCapacity ? "后备已满（3/3），无法撤回。" : "将已部署单位撤回后备。";
    }

    private void OnUnitSelected(string instanceId) { _selectedId = _selectedId == instanceId ? string.Empty : instanceId; Refresh(); }

    private void OnSlotSelected(int slot)
    {
        if (!string.IsNullOrEmpty(_selectedId)) { MoveRequested?.Invoke(_selectedId, slot); return; }
        if (slot >= 0 && slot < _deployment.Count && !string.IsNullOrEmpty(_deployment[slot]))
        {
            _selectedId = _deployment[slot];
            Refresh();
        }
    }

    private void OnUnitDropped(string instanceId, int slot) => MoveRequested?.Invoke(instanceId, slot);
    private void OnWithdraw() { if (!string.IsNullOrEmpty(_selectedId)) WithdrawRequested?.Invoke(_selectedId); }
    private void OnBack() => BackRequested?.Invoke();
    private void OnStart() => StartRequested?.Invoke();
    private bool ContainsUnit(string instanceId) { foreach (var unit in _units) if (unit.InstanceId == instanceId) return true; return false; }
    private int IndexOfDeployment(string instanceId) { for (var index = 0; index < _deployment.Count; index++) if (_deployment[index] == instanceId) return index; return -1; }
    private int CountDeployed() { var count = 0; foreach (var id in _deployment) if (!string.IsNullOrEmpty(id)) count++; return count; }
}
