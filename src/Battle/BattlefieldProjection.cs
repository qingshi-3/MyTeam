using System;
using Godot;

namespace TowerAutobattler.Battle;

public readonly record struct BattlefieldProjection(Vector2 Origin, Vector2 CellPitch, float Scale)
{
    public const float DefaultPadding = 12f;
    private const float CellFill = .92f;

    public bool IsValid => CellPitch.X > 0f && CellPitch.Y > 0f;
    public float UnitScale => Mathf.Clamp(Scale, .9f, 1.4f);
    public float SelectionRadius => Mathf.Clamp(44f * Scale, 40f, 62f);

    public Vector2 CellToLocal(Vector2I cell) =>
        Origin + new Vector2(cell.X * CellPitch.X, cell.Y * CellPitch.Y);

    public Rect2 CellRect(Vector2I cell)
    {
        var size = CellPitch * CellFill;
        return new Rect2(CellToLocal(cell) - size * .5f, size);
    }

    public Vector2 RemapLocalTo(BattlefieldProjection next, Vector2 localPosition)
    {
        if (!IsValid || !next.IsValid) return localPosition;
        var logical = new Vector2(
            (localPosition.X - Origin.X) / CellPitch.X,
            (localPosition.Y - Origin.Y) / CellPitch.Y);
        return next.Origin + new Vector2(logical.X * next.CellPitch.X, logical.Y * next.CellPitch.Y);
    }

    public static BattlefieldProjection Fit(Vector2 allocatedSize, float padding = DefaultPadding)
    {
        var safePadding = Math.Max(0f, padding);
        var usable = new Vector2(
            Math.Max(1f, allocatedSize.X - safePadding * 2f),
            Math.Max(1f, allocatedSize.Y - safePadding * 2f));
        var baseExtent = new Vector2(
            (BattlefieldLayout.Width - 1 + CellFill) * BattlefieldLayout.BaseCellPitch.X,
            (BattlefieldLayout.Height - 1 + CellFill) * BattlefieldLayout.BaseCellPitch.Y);
        var scale = Math.Max(.01f, Math.Min(usable.X / baseExtent.X, usable.Y / baseExtent.Y));
        var pitch = BattlefieldLayout.BaseCellPitch * scale;
        var extent = new Vector2(
            (BattlefieldLayout.Width - 1 + CellFill) * pitch.X,
            (BattlefieldLayout.Height - 1 + CellFill) * pitch.Y);
        var topLeft = (allocatedSize - extent) * .5f;
        var origin = topLeft + pitch * (CellFill * .5f);
        return new BattlefieldProjection(origin, pitch, scale);
    }
}
