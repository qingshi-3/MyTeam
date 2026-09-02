using System;
using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Presentation;

public partial class BattleBoard : Control
{
    public event Action<BattlefieldProjection, BattlefieldProjection>? ProjectionChanged;

    private IBattleFloorRuleRuntime? _floorRule;
    private BattlefieldProjection _projection;

    public BattlefieldProjection CurrentProjection => _projection;

    public override void _Ready() => UpdateProjection();

    public override void _Notification(int what)
    {
        if (what == NotificationResized) UpdateProjection();
    }

    public void Bind(IBattleFloorRuleRuntime floorRule)
    {
        _floorRule = floorRule;
        UpdateProjection();
        QueueRedraw();
    }

    public Vector2 CellToLocal(Vector2I cell) => _projection.CellToLocal(cell);

    public override void _Draw()
    {
        if (!_projection.IsValid) return;
        for (var y = 0; y < BattleSimulation.Height; y++)
        for (var x = 0; x < BattleSimulation.Width; x++)
        {
            var cell = new Vector2I(x, y);
            var preview = _floorRule?.GetCellPreview(cell) ?? FloorCellPreview.Normal;
            var color = preview switch
            {
                FloorCellPreview.Blocked => new Color("243043"),
                FloorCellPreview.Hazard => new Color("6e2f2f"),
                FloorCellPreview.Objective => new Color("285b57"),
                _ => (x + y) % 2 == 0 ? new Color("30384d") : new Color("293146")
            };
            var rect = _projection.CellRect(cell);
            DrawRect(rect, color, true);
            DrawRect(rect, new Color(1, 1, 1, .08f), false, Mathf.Max(1f, _projection.Scale));
        }
    }

    private void UpdateProjection()
    {
        var next = BattlefieldProjection.Fit(Size);
        if (next == _projection) return;
        var previous = _projection;
        _projection = next;
        QueueRedraw();
        if (previous.IsValid) ProjectionChanged?.Invoke(previous, next);
    }
}
