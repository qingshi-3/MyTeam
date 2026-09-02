using System;
using System.Collections.Immutable;
using System.Linq;
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
        for (var index = 0; index < encounter.EnemyIds.Count; index++)
            placements.Add(new BattlePreparationPlacementSource(
                $"enemy-{index}",
                BattlefieldLayout.EnemyCells[index % BattlefieldLayout.EnemyCells.Length]));

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
}
