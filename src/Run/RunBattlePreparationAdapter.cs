using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Relics;
using TowerAutobattler.TacticalCommands;

namespace TowerAutobattler.Run;

public static class RunBattlePreparationAdapter
{
    public static BattlePreparationRequest CreateRequest(
        ContentRegistry content,
        ActiveRunDto run,
        EncounterPlan encounter,
        IBattleFloorRuleRuntime floorRule,
        ModifierSnapshot modifiers,
        RelicBattlePreparation? relics,
        TacticalCommandBattlePreparation? tacticalCommands,
        BossTimelineSnapshot? bossTimeline,
        int availableDeploymentPopulation,
        bool requireLegalFormation)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(floorRule);
        if (run.Roster.Count == 0) throw new InvalidOperationException("Player roster is empty.");

        var deployed = run.Deployment.Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var units = ImmutableArray.CreateBuilder<BattlePreparationUnitSource>();
        foreach (var hero in run.Roster)
            units.Add(new BattlePreparationUnitSource(
                hero.InstanceId,
                hero.ContentId,
                0,
                hero.HealthRatio,
                false,
                true,
                deployed.Contains(hero.InstanceId),
                hero.Equipment.Select(item => new BattlePreparationEquipmentSource(
                    item.InstanceId,
                    item.ContentId,
                    item.OwnerHeroInstanceId,
                    item.SlotIndex)).ToImmutableArray()));
        for (var index = 0; index < encounter.EnemyIds.Count; index++)
            units.Add(new BattlePreparationUnitSource(
                $"enemy-{index}",
                encounter.EnemyIds[index],
                1,
                1f,
                false,
                false,
                true,
                []));

        var placements = ImmutableArray.CreateBuilder<BattlePreparationPlacementSource>();
        for (var index = 0; index < run.Deployment.Count; index++)
        {
            var instanceId = run.Deployment[index];
            if (string.IsNullOrWhiteSpace(instanceId) ||
                !run.Roster.Any(hero => hero.InstanceId == instanceId))
                continue;
            placements.Add(new BattlePreparationPlacementSource(
                instanceId,
                BattlefieldLayout.PlayerDeploymentCells[index]));
        }
        var enemyCells = ResolveEnemyCells(content, encounter, floorRule);
        for (var index = 0; index < encounter.EnemyIds.Count; index++)
            placements.Add(new BattlePreparationPlacementSource(
                $"enemy-{index}",
                enemyCells[index]));

        return new BattlePreparationRequest(
            content,
            run.Seed ^ (ulong)(run.BattleNumber + 1) * 0xD1B54A32D192ED03UL,
            new BattleIdentity(
                encounter.EncounterId,
                encounter.NodeType,
                run.Seed,
                run.FloorIndex,
                run.BattleNumber),
            floorRule,
            units.ToImmutable(),
            placements.ToImmutable(),
            run.Roster[0].ContentId,
            modifiers,
            Math.Max(0, availableDeploymentPopulation -
                run.Deployment.Count(id => !string.IsNullOrEmpty(id))),
            run.Gold,
            relics,
            tacticalCommands,
            bossTimeline,
            requireLegalFormation
                ? BattlePlacementValidation.PlayerFormation
                : BattlePlacementValidation.None);
    }

    private static Vector2I[] ResolveEnemyCells(ContentRegistry content, EncounterPlan encounter,
        IBattleFloorRuleRuntime floor)
    {
        var cells = encounter.EnemyIds.Select((_, i) => BattlefieldLayout.EnemyCells[i % BattlefieldLayout.EnemyCells.Length]).ToArray();
        var radii = encounter.EnemyIds.Select(id => content.TryGet(id, out var entry) && entry.Definition is UnitDefinition unit
            ? unit.BodyRadius : BattlefieldSpace.DefaultBodyRadius).ToArray();
        if (!radii.Any(radius => radius > .49f)) return cells;
        // Reserve the large bodies first. The same resolved anchors drive the deployment preview and
        // battle, so a formal encounter never relies on the simulator silently repairing its giant.
        var reserved = new List<int>();
        foreach (var index in Enumerable.Range(0, cells.Length).OrderByDescending(i => radii[i]).ThenBy(i => i))
        {
            var anchor = cells[index];
            var candidates = Enumerable.Range(0, BattlefieldLayout.Height)
                .SelectMany(y => Enumerable.Range(BattlefieldLayout.Width - BattlefieldLayout.PlayerDeploymentColumns,
                    BattlefieldLayout.PlayerDeploymentColumns).Select(x => new Vector2I(x, y)))
                .OrderBy(cell => cell.DistanceSquaredTo(anchor)).ThenBy(cell => cell.Y).ThenByDescending(cell => cell.X);
            var destination = candidates.Cast<Vector2I?>().FirstOrDefault(cell =>
                BattlefieldSpace.IsPositionTerrainClear(BattlefieldSpace.CellCenter(cell!.Value), radii[index],
                    BattlefieldLayout.Width, BattlefieldLayout.Height, floor.CanOccupy) &&
                reserved.All(other => cell.Value.DistanceTo(cells[other]) >= radii[index] + radii[other] + BattlefieldSpace.BodyClearance));
            if (destination is null) throw new InvalidOperationException("敌方部署区无法容纳本场大型单位与同伴。");
            cells[index] = destination.Value;
            reserved.Add(index);
        }
        return cells;
    }
}
