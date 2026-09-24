using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.BattleLab;

public static class BattleLabPlacementPolicy
{
    public static BattleLabPlacementResult Evaluate(
        BattleLabSession session,
        BattleLabUnitConfiguration candidate,
        Vector2I target,
        bool allowSwap,
        Func<Vector2I, bool>? canOccupy = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(candidate);
        if (!session.Content.TryGetUnit(candidate.ContentId, out var content) ||
            !content.AllowedSides.Contains(candidate.Side))
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "未找到可用单位或测试队伍无效。");
        if (!BattlefieldLayout.IsInBounds(target))
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "目标格超出 10×6 战场边界。");
        if (!session.Content.CanOccupy(session.FloorRuleId, target) || canOccupy?.Invoke(target) == false)
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "目标格被当前地形规则禁止。");
        var occupied = session.At(target);
        if (occupied?.InstanceId == candidate.InstanceId)
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "单位已位于该格，配置未改变。");
        if (occupied is null)
        {
            var proposed = session.Units.Where(unit => unit.InstanceId != candidate.InstanceId)
                .Append(candidate with { Cell = target }).ToArray();
            var error = ValidateBodies(session.Content, proposed, session.FloorRuleId, session.Mode, canOccupy);
            return error is null ? new BattleLabPlacementResult(true, string.Empty, candidate.InstanceId) :
                BattleLabPlacementResult.Reject(candidate.InstanceId, error);
        }
        if (!allowSwap)
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "目标格已被占用。");
        if (!session.TryGet(candidate.InstanceId, out var original) ||
            !session.Content.CanOccupy(session.FloorRuleId, original.Cell) ||
            canOccupy?.Invoke(original.Cell) == false)
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "交换后另一单位不能合法占据原格。");
        var swapped = session.Units.Where(unit => unit.InstanceId != candidate.InstanceId && unit.InstanceId != occupied.InstanceId)
            .Append(candidate with { Cell = target }).Append(occupied with { Cell = original.Cell }).ToArray();
        var swapError = ValidateBodies(session.Content, swapped, session.FloorRuleId, session.Mode, canOccupy);
        return swapError is null ? new BattleLabPlacementResult(true, string.Empty, candidate.InstanceId, occupied.InstanceId) :
            BattleLabPlacementResult.Reject(candidate.InstanceId, swapError);
    }

    internal static string? ValidateBodies(BattleLabContentIndex content,
        IEnumerable<BattleLabUnitConfiguration> units, string floor, BattleLabPlacementMode mode,
        Func<Vector2I, bool>? canOccupy = null)
    {
        var bodies = units.Select(unit => (Unit: unit, Radius: content.TryGetUnit(unit.ContentId, out var entry)
            ? entry.Definition.BodyRadius : BattlefieldSpace.DefaultBodyRadius)).ToArray();
        for (var i = 0; i < bodies.Length; i++)
        {
            var (unit, radius) = bodies[i];
            var point = BattlefieldSpace.CellCenter(unit.Cell);
            if (!BattlefieldSpace.IsPositionTerrainClear(point, radius, BattlefieldLayout.Width, BattlefieldLayout.Height,
                    cell => content.CanOccupy(floor, cell) && canOccupy?.Invoke(cell) != false))
                return "该单位体型较大，身体超出边界或碰到禁行地形。";
            for (var j = 0; j < i; j++)
                if (point.DistanceTo(BattlefieldSpace.CellCenter(bodies[j].Unit.Cell)) <
                    radius + bodies[j].Radius + BattlefieldSpace.BodyClearance)
                    return "单位身体相互重叠，请为大型单位留出空间。";
        }
        return null;
    }

}
