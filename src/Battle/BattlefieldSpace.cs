using System;
using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Battle;

/// <summary>
/// Pure logical battle-space geometry. One unit on either axis equals one authored deployment-cell pitch;
/// presentation pixels are deliberately absent from this contract.
/// </summary>
public static class BattlefieldSpace
{
    public const float DefaultBodyRadius = .32f;
    public const float MinimumBodyRadius = .1f;
    public const float MaximumBodyRadius = .49f;
    public const float BodyClearance = .015f;
    public const float CellHalfExtent = .5f;

    public static Vector2 CellCenter(Vector2I cell) => new(cell.X, cell.Y);

    public static Vector2I PositionToCell(Vector2 position) => new(
        Math.Clamp(Mathf.FloorToInt(position.X + CellHalfExtent), 0, BattlefieldLayout.Width - 1),
        Math.Clamp(Mathf.FloorToInt(position.Y + CellHalfExtent), 0, BattlefieldLayout.Height - 1));

    public static float EdgeDistance(BattleUnitState first, BattleUnitState second) =>
        Math.Max(0f, first.Position.DistanceTo(second.Position) - first.BodyRadius - second.BodyRadius);

    public static bool IsWithinReach(BattleUnitState source, BattleUnitState target, float reach) =>
        EdgeDistance(source, target) <= Math.Max(0f, reach) + .0001f;

    public static bool IsCircleInsideArena(Vector2 position, float radius, int width, int height)
    {
        var safeRadius = Math.Max(0f, radius);
        return position.X - safeRadius >= -CellHalfExtent &&
               position.Y - safeRadius >= -CellHalfExtent &&
               position.X + safeRadius <= width - CellHalfExtent &&
               position.Y + safeRadius <= height - CellHalfExtent;
    }

    public static bool IsPositionTerrainClear(
        Vector2 position,
        float radius,
        int width,
        int height,
        Func<Vector2I, bool> terrainAllows)
    {
        if (!IsCircleInsideArena(position, radius, width, height)) return false;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var cell = new Vector2I(x, y);
            if (terrainAllows(cell)) continue;
            var minimum = new Vector2(x - CellHalfExtent - radius, y - CellHalfExtent - radius);
            var maximum = new Vector2(x + CellHalfExtent + radius, y + CellHalfExtent + radius);
            if (position.X > minimum.X && position.X < maximum.X &&
                position.Y > minimum.Y && position.Y < maximum.Y)
                return false;
        }
        return true;
    }

    public static bool IsPositionTerrainClear(
        Vector2 position,
        float radius,
        int width,
        int height,
        IReadOnlyList<Vector2I> blockedCells)
    {
        if (!IsCircleInsideArena(position, radius, width, height)) return false;
        foreach (var cell in blockedCells)
        {
            var minimum = new Vector2(cell.X - CellHalfExtent - radius, cell.Y - CellHalfExtent - radius);
            var maximum = new Vector2(cell.X + CellHalfExtent + radius, cell.Y + CellHalfExtent + radius);
            if (position.X > minimum.X && position.X < maximum.X &&
                position.Y > minimum.Y && position.Y < maximum.Y)
                return false;
        }
        return true;
    }

    public static bool IsSegmentTerrainClear(
        Vector2 start,
        Vector2 end,
        float radius,
        int width,
        int height,
        Func<Vector2I, bool> terrainAllows) =>
        FirstTerrainHitFraction(start, end - start, radius, width, height, terrainAllows) >= 1f;

    public static float FirstTerrainHitFraction(
        Vector2 start,
        Vector2 delta,
        float radius,
        int width,
        int height,
        Func<Vector2I, bool> terrainAllows)
    {
        if (!IsPositionTerrainClear(start, radius, width, height, terrainAllows)) return 0f;
        var earliest = 1f;
        var minimumArena = new Vector2(-CellHalfExtent + radius, -CellHalfExtent + radius);
        var maximumArena = new Vector2(width - CellHalfExtent - radius, height - CellHalfExtent - radius);
        earliest = Math.Min(earliest, SegmentExitFraction(start, delta, minimumArena, maximumArena));
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var cell = new Vector2I(x, y);
            if (terrainAllows(cell)) continue;
            var minimum = new Vector2(x - CellHalfExtent - radius, y - CellHalfExtent - radius);
            var maximum = new Vector2(x + CellHalfExtent + radius, y + CellHalfExtent + radius);
            if (TrySegmentAabbEntry(start, delta, minimum, maximum, out var entry))
                earliest = Math.Min(earliest, entry);
        }
        return Mathf.Clamp(earliest, 0f, 1f);
    }

    public static float FirstTerrainHitFraction(
        Vector2 start,
        Vector2 delta,
        float radius,
        int width,
        int height,
        IReadOnlyList<Vector2I> blockedCells)
    {
        if (!IsPositionTerrainClear(start, radius, width, height, blockedCells)) return 0f;
        var earliest = SegmentExitFraction(
            start,
            delta,
            new Vector2(-CellHalfExtent + radius, -CellHalfExtent + radius),
            new Vector2(width - CellHalfExtent - radius, height - CellHalfExtent - radius));
        foreach (var cell in blockedCells)
        {
            var minimum = new Vector2(cell.X - CellHalfExtent - radius, cell.Y - CellHalfExtent - radius);
            var maximum = new Vector2(cell.X + CellHalfExtent + radius, cell.Y + CellHalfExtent + radius);
            if (TrySegmentAabbEntry(start, delta, minimum, maximum, out var entry))
                earliest = Math.Min(earliest, entry);
        }
        return Mathf.Clamp(earliest, 0f, 1f);
    }

    public static bool IsSegmentTerrainClear(
        Vector2 start,
        Vector2 end,
        float radius,
        int width,
        int height,
        IReadOnlyList<Vector2I> blockedCells) =>
        FirstTerrainHitFraction(start, end - start, radius, width, height, blockedCells) >= 1f;

    public static bool TryMovingCircleTimeOfImpact(
        Vector2 firstStart,
        Vector2 firstDelta,
        float firstRadius,
        Vector2 secondStart,
        Vector2 secondDelta,
        float secondRadius,
        out float time)
    {
        var relativePosition = firstStart - secondStart;
        var relativeDelta = firstDelta - secondDelta;
        var combinedRadius = Math.Max(0f, firstRadius) + Math.Max(0f, secondRadius) + BodyClearance;
        var c = relativePosition.LengthSquared() - combinedRadius * combinedRadius;
        if (c <= 0f)
        {
            if (relativePosition.Dot(relativeDelta) >= 0f)
            {
                time = 1f;
                return false;
            }
            time = 0f;
            return true;
        }
        var a = relativeDelta.LengthSquared();
        var b = 2f * relativePosition.Dot(relativeDelta);
        if (a <= .0000001f || b >= 0f)
        {
            time = 1f;
            return false;
        }
        var discriminant = b * b - 4f * a * c;
        if (discriminant < 0f)
        {
            time = 1f;
            return false;
        }
        var entry = (-b - MathF.Sqrt(discriminant)) / (2f * a);
        if (entry is < 0f or > 1f)
        {
            time = 1f;
            return false;
        }
        time = entry;
        return true;
    }

    public static float PointSegmentDistance(Vector2 point, Vector2 start, Vector2 end)
    {
        var segment = end - start;
        if (segment.LengthSquared() <= .0000001f) return point.DistanceTo(start);
        var t = Mathf.Clamp((point - start).Dot(segment) / segment.LengthSquared(), 0f, 1f);
        return point.DistanceTo(start + segment * t);
    }

    private static float SegmentExitFraction(Vector2 start, Vector2 delta, Vector2 minimum, Vector2 maximum)
    {
        var result = 1f;
        if (delta.X > 0f && start.X + delta.X > maximum.X)
            result = Math.Min(result, (maximum.X - start.X) / delta.X);
        else if (delta.X < 0f && start.X + delta.X < minimum.X)
            result = Math.Min(result, (minimum.X - start.X) / delta.X);
        if (delta.Y > 0f && start.Y + delta.Y > maximum.Y)
            result = Math.Min(result, (maximum.Y - start.Y) / delta.Y);
        else if (delta.Y < 0f && start.Y + delta.Y < minimum.Y)
            result = Math.Min(result, (minimum.Y - start.Y) / delta.Y);
        return Mathf.Clamp(result, 0f, 1f);
    }

    private static bool TrySegmentAabbEntry(
        Vector2 start,
        Vector2 delta,
        Vector2 minimum,
        Vector2 maximum,
        out float entry)
    {
        var near = 0f;
        var far = 1f;
        if (!ClipAxis(start.X, delta.X, minimum.X, maximum.X, ref near, ref far) ||
            !ClipAxis(start.Y, delta.Y, minimum.Y, maximum.Y, ref near, ref far))
        {
            entry = 1f;
            return false;
        }
        entry = Mathf.Clamp(near, 0f, 1f);
        return far >= 0f && near <= 1f;
    }

    private static bool ClipAxis(float origin, float delta, float minimum, float maximum, ref float near, ref float far)
    {
        if (Math.Abs(delta) <= .0000001f) return origin >= minimum && origin <= maximum;
        var first = (minimum - origin) / delta;
        var second = (maximum - origin) / delta;
        if (first > second) (first, second) = (second, first);
        near = Math.Max(near, first);
        far = Math.Min(far, second);
        return near <= far;
    }
}
