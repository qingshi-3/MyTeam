using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public sealed record DeploymentBodyPreview(string InstanceId, Vector2I Cell, float Radius, int Team, bool Selected = false,
    UnitPortraitDefinition? Portrait = null, string DisplayName = "");

// A view of the authored circular body, not a second placement validator or an integer tile reservation.
public partial class DeploymentFootprintLayer : Control
{
    [Export] public PackedScene BodyPortraitScene { get; set; } = null!;
    private readonly Dictionary<string, Control> _portraits = new(StringComparer.Ordinal);
    [Export] public Color AllyColor { get; set; } = new(.4f, .75f, 1f);
    [Export] public Color EnemyColor { get; set; } = new(1f, .45f, .38f);
    [Export] public Color SelectedColor { get; set; } = new(1f, .8f, .35f);
    [Export] public Color LegalColor { get; set; } = new(.4f, 1f, .7f);
    [Export] public Color IllegalColor { get; set; } = new(1f, .3f, .3f);
    private Vector2 _origin, _pitch, _cellSize;
    private DeploymentBodyPreview[] _bodies = [];
    public IReadOnlyList<DeploymentBodyPreview> Bodies => _bodies;
    public DeploymentBodyPreview? DragBody { get; private set; }
    public bool DragAllowed { get; private set; }

    public void SetGrid(Vector2 origin, Vector2 pitch, Vector2 cellSize)
    {
        _origin = origin; _pitch = pitch; _cellSize = cellSize;
        LayoutPortraits();
        QueueRedraw();
    }

    public void Bind(IEnumerable<DeploymentBodyPreview> bodies)
    {
        _bodies = bodies.Where(body => body.Radius > .5f).ToArray();
        var ids = _bodies.Where(body => body.Portrait is not null).Select(body => body.InstanceId).ToHashSet(StringComparer.Ordinal);
        foreach (var id in _portraits.Keys.Where(id => !ids.Contains(id)).ToArray())
        {
            var stale = _portraits[id];
            RemoveChild(stale); stale.QueueFree(); _portraits.Remove(id);
        }
        foreach (var body in _bodies.Where(body => body.Portrait is not null))
        {
            if (!_portraits.TryGetValue(body.InstanceId, out var view))
            {
                view = BodyPortraitScene.Instantiate<Control>();
                AddChild(view); _portraits.Add(body.InstanceId, view);
            }
            var portrait = view.GetNode<UnitPortrait>("Portrait");
            if (!ReferenceEquals(portrait.Definition, body.Portrait)) portrait.Bind(body.Portrait);
            view.GetNode<Label>("UnitName").Text = body.DisplayName;
        }
        LayoutPortraits();
        QueueRedraw();
    }

    public void ShowDrag(DeploymentBodyPreview? body, bool allowed = false)
    {
        DragBody = body; DragAllowed = allowed;
        LayoutPortraits();
        QueueRedraw();
    }

    private void LayoutPortraits()
    {
        foreach (var body in _bodies)
        {
            if (!_portraits.TryGetValue(body.InstanceId, out var view)) continue;
            view.Visible = DragBody?.InstanceId != body.InstanceId;
            view.Size = _pitch * body.Radius * 2;
            view.Position = _origin + (Vector2)body.Cell * _pitch - view.Size * .5f;
        }
    }

    public override void _Draw()
    {
        if (_pitch.X <= 0 || _pitch.Y <= 0) return;
        foreach (var body in _bodies)
        {
            if (DragBody?.InstanceId == body.InstanceId) continue;
            DrawBody(body, body.Selected ? SelectedColor : body.Team == 0 ? AllyColor : EnemyColor, false, true);
        }
        if (DragBody is { } drag) DrawBody(drag, DragAllowed ? LegalColor : IllegalColor, true, DragAllowed);
    }

    private void DrawBody(DeploymentBodyPreview body, Color tint, bool dragging, bool allowed)
    {
        var center = _origin + (Vector2)body.Cell * _pitch;
        var radius = Math.Max(BattlefieldSpace.MinimumBodyRadius, body.Radius);
        var extent = Mathf.CeilToInt(radius + .5f);
        for (var y = -extent; y <= extent; y++)
        for (var x = -extent; x <= extent; x++)
        {
            var closest = new Vector2(Math.Max(0, Math.Abs(x) - .5f), Math.Max(0, Math.Abs(y) - .5f));
            if (closest.LengthSquared() >= radius * radius) continue;
            var rect = new Rect2(center + new Vector2(x, y) * _pitch - _cellSize * .5f, _cellSize);
            DrawRect(rect, new Color(tint, dragging ? .2f : .09f));
            DrawRect(rect.Grow(-2), new Color(tint, dragging ? .9f : .5f), false, dragging ? 2 : 1);
        }
        // The outline keeps partial-cell coverage distinguishable from a solid square footprint.
        var outline = new Vector2[65];
        for (var index = 0; index < outline.Length; index++)
        {
            var angle = Mathf.Tau * index / (outline.Length - 1);
            outline[index] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _pitch * radius;
        }
        DrawPolyline(outline, new Color(tint, .9f), dragging || body.Selected ? 3 : 2, true);
        if (!dragging) return;
        if (!allowed)
        {
            DrawLine(center - new Vector2(8, 8), center + new Vector2(8, 8), tint, 3, true);
            DrawLine(center - new Vector2(8, -8), center + new Vector2(8, -8), tint, 3, true);
        }
        else
        {
            DrawLine(center + new Vector2(-8, 0), center + new Vector2(-2, 6), tint, 3, true);
            DrawLine(center + new Vector2(-2, 6), center + new Vector2(9, -7), tint, 3, true);
        }
    }
}
