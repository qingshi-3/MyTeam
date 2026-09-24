using System;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Presentation;

// An optional view of authoritative ground geometry; never participates in hit tests or movement.
public partial class BattleGeometryOverlay : Control
{
    private static readonly Color AllyColor = new("72ddf0");
    private static readonly Color EnemyColor = new("ff9a83");
    private static readonly Color ReachColor = new("ffd36a");
    public BattleSpatialSnapshot? Snapshot { get; private set; }
    public BattlefieldProjection Projection { get; private set; }
    public string SelectedId { get; private set; } = "";
    public bool ShowBodies { get; private set; }
    public bool ShowReach { get; private set; }

    public void Bind(BattleSpatialSnapshot snapshot, BattlefieldProjection projection, string selectedId,
        bool showBodies, bool showReach)
    {
        Snapshot = snapshot;
        Projection = projection;
        SelectedId = selectedId;
        ShowBodies = showBodies;
        ShowReach = showReach;
        QueueRedraw();
    }

    public void Clear()
    {
        Snapshot = null;
        SelectedId = "";
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Snapshot is null || !Projection.IsValid || (!ShowBodies && !ShowReach)) return;
        var selected = Snapshot.Units.FirstOrDefault(unit => unit.RuntimeId == SelectedId);
        var target = Snapshot.Units.FirstOrDefault(unit => unit.RuntimeId == selected?.TargetId);
        if (ShowBodies)
        {
            // Terrain occupies the full cell, including the decorative gaps in the board grid.
            foreach (var cell in Snapshot.BlockedCells)
            {
                var rect = new Rect2(Projection.CellToLocal(cell) - Projection.CellPitch * .5f, Projection.CellPitch);
                DrawRect(rect, new Color(.9f, .92f, 1f, .55f), false, 2);
                DrawLine(rect.Position, rect.End, new Color(1f, 1f, 1f, .22f), 1, true);
                DrawLine(new(rect.End.X, rect.Position.Y), new(rect.Position.X, rect.End.Y), new Color(1f, 1f, 1f, .22f), 1, true);
            }
            foreach (var reservation in Snapshot.Reservations)
            {
                Ring(reservation.Position, reservation.Radius, new Color("c6a5ff"), true, false);
                var point = Projection.LogicalToLocal(reservation.Position);
                DrawLine(point - Vector2.One * 4, point + Vector2.One * 4, new Color("c6a5ff"), 2, true);
                DrawLine(point + new Vector2(-4, 4), point + new Vector2(4, -4), new Color("c6a5ff"), 2, true);
            }
        }
        if (ShowReach && selected is not null)
            Ring(selected.Position, selected.BodyRadius + Math.Max(0, selected.AttackReach), ReachColor, true, true);
        foreach (var unit in Snapshot.Units)
        {
            if (!ShowBodies && !(ShowReach && (unit == selected || unit == target))) continue;
            var color = unit.Team == 0 ? AllyColor : EnemyColor;
            if (!unit.Grounded) color.A = .45f;
            Ring(unit.Position, unit.BodyRadius, color, !unit.Grounded, ShowBodies);
            var center = Projection.LogicalToLocal(unit.Position);
            DrawCircle(center, unit == selected ? 4 : 2, color);
        }
        if (ShowReach && selected is not null && target is not null)
            DrawSeparation(selected, target);
    }

    private void Ring(Vector2 center, float radius, Color color, bool dashed, bool fill)
    {
        const int segments = 96;
        var points = new Vector2[segments + 1];
        for (var i = 0; i <= segments; i++)
        {
            var angle = Mathf.Tau * i / segments;
            points[i] = Projection.LogicalToLocal(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        if (fill) DrawColoredPolygon(points[..segments], new Color(color, .055f));
        if (!dashed) DrawPolyline(points, color, 2, true);
        else
            for (var i = 0; i < segments; i += 4)
                DrawPolyline(points[i..(i + 3)], color, 2, true);
    }

    private void DrawSeparation(BattleSpatialUnit source, BattleSpatialUnit target)
    {
        var direction = (target.Position - source.Position).Normalized();
        var start = Projection.LogicalToLocal(source.Position + direction * source.BodyRadius);
        var end = Projection.LogicalToLocal(target.Position - direction * target.BodyRadius);
        var edgeDistance = Math.Max(0, source.Position.DistanceTo(target.Position) - source.BodyRadius - target.BodyRadius);
        var color = source.TargetWithinReach && source.TargetLineClear ? new Color("b8f28b") : new Color("ff9a83");
        DrawLine(start, end, color, 3, true);
        var normal = (end - start).Normalized().Orthogonal() * 5;
        DrawLine(start - normal, start + normal, color, 2, true);
        DrawLine(end - normal, end + normal, color, 2, true);
        var text = $"{edgeDistance:0.00} {(source.TargetWithinReach ? "≤" : "> ")} {source.AttackReach:0.##}";
        if (!source.TargetLineClear) text += " · 地形阻挡";
        var font = GetThemeDefaultFont();
        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, 16);
        var point = (start + end) * .5f - new Vector2(size.X * .5f, 12);
        point.X = Mathf.Clamp(point.X, 6, Math.Max(6, Size.X - size.X - 6));
        point.Y = Mathf.Clamp(point.Y, 22, Math.Max(22, Size.Y - 6));
        DrawRect(new Rect2(point - new Vector2(6, 18), size + new Vector2(12, 5)), new Color("16202bea"));
        DrawString(font, point, text, HorizontalAlignment.Left, -1, 16, color);
    }
}
