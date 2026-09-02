using System;
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
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "该发布内容不能放入所选阵营。");
        if (!BattlefieldLayout.IsInBounds(target))
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "目标格超出 10×6 战场边界。");
        if (!session.Content.CanOccupy(session.FloorRuleId, target) || canOccupy?.Invoke(target) == false)
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "目标格被当前地形规则禁止。");
        if (session.Mode == BattleLabPlacementMode.Formal)
        {
            if (candidate.Side == BattleLabSide.Player && !BattlefieldLayout.IsPlayerDeploymentCell(target))
                return BattleLabPlacementResult.Reject(candidate.InstanceId, "正式规则下我方只能部署在左侧 3×6 区域。");
            if (candidate.Side == BattleLabSide.Enemy && target.X < BattlefieldLayout.Width - BattlefieldLayout.PlayerDeploymentColumns)
                return BattleLabPlacementResult.Reject(candidate.InstanceId, "正式规则下敌方只能部署在右侧 3×6 区域。");
            var playerCount = session.Units.Count(unit => unit.Side == BattleLabSide.Player &&
                unit.InstanceId != candidate.InstanceId) + (candidate.Side == BattleLabSide.Player ? 1 : 0);
            if (playerCount > session.CurrentPopulation)
                return BattleLabPlacementResult.Reject(candidate.InstanceId, "我方部署数量超过当前人口。");
        }

        var occupied = session.At(target);
        if (occupied?.InstanceId == candidate.InstanceId)
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "单位已位于该格，配置未改变。");
        if (occupied is null)
            return new BattleLabPlacementResult(true, string.Empty, candidate.InstanceId);
        if (!allowSwap)
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "目标格已被占用。");
        if (occupied.Side != candidate.Side && session.Mode == BattleLabPlacementMode.Formal)
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "正式规则下不能跨阵营交换单位。");
        if (!session.TryGet(candidate.InstanceId, out var original) ||
            !CellAllowedForSide(session, occupied.Side, original.Cell) ||
            !session.Content.CanOccupy(session.FloorRuleId, original.Cell) ||
            canOccupy?.Invoke(original.Cell) == false)
            return BattleLabPlacementResult.Reject(candidate.InstanceId, "交换后另一单位不能合法占据原格。");
        return new BattleLabPlacementResult(true, string.Empty, candidate.InstanceId, occupied.InstanceId);
    }

    private static bool CellAllowedForSide(
        BattleLabSession session,
        BattleLabSide side,
        Vector2I cell) => session.Mode != BattleLabPlacementMode.Formal ||
        (side == BattleLabSide.Player
            ? BattlefieldLayout.IsPlayerDeploymentCell(cell)
            : cell.X >= BattlefieldLayout.Width - BattlefieldLayout.PlayerDeploymentColumns);
}
