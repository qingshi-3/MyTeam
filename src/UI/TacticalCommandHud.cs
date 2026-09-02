using System;
using Godot;
using TowerAutobattler.TacticalCommands;

namespace TowerAutobattler.UI;

public partial class TacticalCommandHud : PanelContainer
{
    public event Action<int>? UseRequested;

    private Label _points = null!;
    private Label _failure = null!;
    private TacticalCommandSlot[] _slots = [];
    private ColorRect[] _segments = [];

    public override void _Ready()
    {
        _points = GetNode<Label>("%TacticalPointsText");
        _failure = GetNode<Label>("%FailureReason");
        _slots =
        [
            GetNode<TacticalCommandSlot>("%TacticalCommandSlot0"),
            GetNode<TacticalCommandSlot>("%TacticalCommandSlot1")
        ];
        _segments =
        [
            GetNode<ColorRect>("%TacticalPointSegment0"),
            GetNode<ColorRect>("%TacticalPointSegment1"),
            GetNode<ColorRect>("%TacticalPointSegment2")
        ];
        foreach (var slot in _slots) slot.UseRequested += OnUseRequested;
    }

    public override void _ExitTree()
    {
        foreach (var slot in _slots) slot.UseRequested -= OnUseRequested;
    }

    public void Bind(
        BattleTacticalCommandSnapshot snapshot,
        string failureReason,
        bool failure,
        bool battleRunning)
    {
        _points.Text = $"战术点 {snapshot.CurrentPoints}/{snapshot.MaximumPoints}";
        for (var index = 0; index < _segments.Length; index++)
        {
            _segments[index].Visible = index < snapshot.MaximumPoints;
            _segments[index].Color = index < snapshot.CurrentPoints
                ? new Color("3c8dff")
                : new Color("1b2b46");
        }
        for (var index = 0; index < _slots.Length; index++)
        {
            if (index < snapshot.Slots.Length) _slots[index].Bind(snapshot.Slots[index], battleRunning);
            else _slots[index].BindUnavailable(index);
        }
        _failure.Text = failureReason;
        _failure.Visible = !string.IsNullOrWhiteSpace(failureReason);
        _failure.ThemeTypeVariation = failure ? "FeedbackFailure" : "FeedbackSuccess";
    }

    private void OnUseRequested(int slotIndex) => UseRequested?.Invoke(slotIndex);
}
