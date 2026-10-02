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
    public Vector2 LogicalToLocal(Vector2 position) => _projection.LogicalToLocal(position);

    public override void _Draw()
    {
        if (!_projection.IsValid) return;
        for (var y = 0; y < BattleSimulation.Height; y++)
        for (var x = 0; x < BattleSimulation.Width; x++)
        {
            var cell = new Vector2I(x, y);
            var preview = _floorRule?.GetCellPreview(cell) ?? FloorCellPreview.Normal;
            if (preview == FloorCellPreview.Normal) continue;
            var color = preview switch
            {
                FloorCellPreview.Blocked => new Color(.15f, .19f, .18f, .62f),
                FloorCellPreview.Hazard => new Color(.55f, .15f, .10f, .32f),
                _ => new Color(.10f, .42f, .32f, .3f)
            };
            var rect = _projection.CellRect(cell);
            DrawRect(rect, color, true);
            // Rule information remains visible with a shape cue as well as color.
            var center = rect.GetCenter();
            var width = Mathf.Max(1.5f, _projection.Scale * 2f);
            if (preview == FloorCellPreview.Blocked)
            {
                DrawLine(rect.Position + rect.Size * .2f, rect.End - rect.Size * .2f, new Color(.77f, .75f, .66f), width);
                DrawLine(new Vector2(rect.End.X, rect.Position.Y) + new Vector2(-rect.Size.X, rect.Size.Y) * .2f,
                    new Vector2(rect.Position.X, rect.End.Y) + new Vector2(rect.Size.X, -rect.Size.Y) * .2f,
                    new Color(.77f, .75f, .66f), width);
            }
            else if (preview == FloorCellPreview.Hazard)
            {
                DrawLine(center - new Vector2(0, rect.Size.Y * .22f), center + new Vector2(0, rect.Size.Y * .05f),
                    new Color(1f, .76f, .4f), width * 2f);
                DrawCircle(center + new Vector2(0, rect.Size.Y * .22f), width, new Color(1f, .76f, .4f));
            }
            else DrawArc(center, rect.Size.Y * .3f, 0, Mathf.Tau, 24, new Color(.5f, 1f, .83f), width);
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
