using System;
using Godot;
using TowerAutobattler.BattleLab;

namespace TowerAutobattler.UI;

public partial class BattleLabBoardCell : Button
{
    public event Action<string>? UnitDragRequested;
    public event Action<Vector2I>? CellRequested;
    public Vector2I Cell { get; private set; }
    public string InstanceId { get; private set; } = string.Empty;
    private string _baseText = string.Empty;

    public void BindCell(Vector2I cell)
    {
        Cell = cell;
        TooltipText = $"战场格 ({cell.X},{cell.Y})";
        Refresh(null, false);
    }

    public void Refresh(BattleLabUnitConfiguration? unit, bool selected)
    {
        InstanceId = unit?.InstanceId ?? string.Empty;
        _baseText = unit is null ? "·" : selected ? "▣" : unit.Side == BattleLabSide.Player ? "◆" : "⚔";
        Text = _baseText;
        TooltipText = unit is null
            ? $"空格 ({Cell.X},{Cell.Y})"
            : $"{(selected ? "已选中\n" : string.Empty)}{unit.ContentId}\n实例 {unit.InstanceId}\n格 ({Cell.X},{Cell.Y})";
        ThemeTypeVariation = unit is null ? "DeploymentCell" : selected ? "SelectedDeploymentCell" :
            unit.Side == BattleLabSide.Player ? "SelectedDeploymentCell" : "EnemyDeploymentCell";
    }

    public void ShowDropState(bool legal, bool swap)
    {
        Text = legal ? swap ? "⇄" : "○" : "×";
        TooltipText = legal ? swap ? "可交换" : "可放置" : "不可放置";
    }

    public void ClearDropState() => Text = _baseText;

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
        if (string.IsNullOrWhiteSpace(InstanceId)) CellRequested?.Invoke(Cell);
        else UnitDragRequested?.Invoke(InstanceId);
        AcceptEvent();
    }

    public override void _ExitTree()
    {
        UnitDragRequested = null;
        CellRequested = null;
        InstanceId = string.Empty;
    }
}
