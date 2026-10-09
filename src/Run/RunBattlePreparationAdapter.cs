using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
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
        bool requireLegalFormation,
        CompiledGrowthRules? growthRules = null)
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
                // Run battles start fresh; old saves may still contain a wound ratio.
                1f,
                false,
                true,
                deployed.Contains(hero.InstanceId),
                hero.Equipment.Select(item => new BattlePreparationEquipmentSource(
                    item.InstanceId,
                    item.ContentId,
                    item.OwnerHeroInstanceId,
                    item.SlotIndex)).ToImmutableArray(),
                Growth: ProjectGrowth(hero, growthRules, run.Growth, GrowthRunService.GetPending(run))));
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
                string.IsNullOrEmpty(encounter.ExpeditionProfileId) ? run.FloorIndex : -1,
                run.BattleNumber,
                encounter.CompositionId) { OriginKey = OriginKey(run, encounter) },
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

    public static string OriginKey(ActiveRunDto run, EncounterPlan encounter)
    {
        if (string.IsNullOrEmpty(encounter.ExpeditionProfileId)) return string.Empty;
        if (run.Action?.Source is { LegacyFloorIndex: null } source && !string.IsNullOrWhiteSpace(source.ActionId))
            return source.ActionId;
        // Read-only preparation can exist before an action is committed. Its
        // namespace can never be accepted as a paid battle's receipt origin.
        return string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"preview-v1:{run.Seed:x16}:{run.BattleNumber}:{encounter.ExpeditionSiteId}:{encounter.ExpeditionProfileId}:{encounter.FloorRuleId}");
    }

    internal static BattleUnitGrowth? ProjectGrowth(RosterHeroInstanceDto hero,
        CompiledGrowthRules? rules, GrowthRunDto? runGrowth = null, GrowthNodeSnapshotDto? pending = null) =>
        ProjectGrowth(RosterHeroView.For(hero), rules, runGrowth is null ? null : GrowthRunView.For(runGrowth),
            pending is null ? null : GrowthNodeSnapshotView.For(pending));

    public static BattleUnitGrowth? ProjectGrowth(RosterHeroView hero,
        CompiledGrowthRules? rules, GrowthRunView? runGrowth = null, GrowthNodeSnapshotView? pending = null)
    {
        if (rules is null) return null;
        rules.Heroes.TryGetValue(hero.ContentId, out var definition);
        var loadout = definition?.ResolveBattleLoadout(!string.IsNullOrEmpty(hero.Growth.AscensionId));
        // Preview reads the selection; an already-started battle reads its paid snapshot.
        var started = (pending ?? runGrowth?.PendingNode) is { IsBattle: true } node ? node : null;
        var spellId = started?.ConsumedSpellId ?? runGrowth?.EquippedSpellId ?? "";
        var target = started?.SpellTargetInstanceId ?? runGrowth?.SpellTargetInstanceId ?? "";
        var canUse = started is not null || runGrowth?.SpellInventory.GetValueOrDefault(spellId) > 0;
        var spell = canUse && target == hero.InstanceId && rules.Spells.TryGetValue(spellId, out var selected)
            ? selected.BattleLoadout : null;
        return new BattleUnitGrowth(hero.Growth.AddedAttack, hero.Growth.AddedMaxHealth, loadout, spell);
    }

    private static Vector2I[] ResolveEnemyCells(ContentRegistry content, EncounterPlan encounter,
        IBattleFloorRuleRuntime floor)
    {
        if (!encounter.EnemyCells.IsDefaultOrEmpty && encounter.EnemyCells.Length != encounter.EnemyIds.Count)
            throw new InvalidOperationException("Encounter placement count does not match its enemies.");
        var cells = encounter.EnemyCells.IsDefaultOrEmpty
            ? encounter.EnemyIds.Select((_, i) => BattlefieldLayout.EnemyCells[i % BattlefieldLayout.EnemyCells.Length]).ToArray()
            : encounter.EnemyCells.ToArray();
        var radii = encounter.EnemyIds.Select(id => content.TryGet(id, out var entry) && entry.Definition is UnitDefinition unit
            ? unit.BodyRadius : BattlefieldSpace.DefaultBodyRadius).ToArray();
        // A complete, legal authored formation is authoritative. Validate all bodies
        // before reserving any anchor; partial preservation can consume space needed
        // by a larger ally. Legacy invalid formations keep the deterministic fallback.
        if (!encounter.EnemyCells.IsDefaultOrEmpty && AuthoredEnemyFormationFits(cells, radii, floor))
            return cells;
        // Reserve large bodies first, then resolve even small anchors against terrain and prior bodies.
        // Preview and battle share this result instead of relying on silent simulator relocation.
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

    private static bool AuthoredEnemyFormationFits(Vector2I[] cells, float[] radii,
        IBattleFloorRuleRuntime floor)
    {
        var enemyBoundary = BattlefieldLayout.Width / 2f - BattlefieldSpace.CellHalfExtent;
        var playerBoundary = BattlefieldLayout.PlayerDeploymentColumns - BattlefieldSpace.CellHalfExtent;
        for (var index = 0; index < cells.Length; index++)
        {
            var position = BattlefieldSpace.CellCenter(cells[index]);
            if (!BattlefieldLayout.IsInBounds(cells[index]) ||
                position.X - radii[index] < Math.Max(enemyBoundary, playerBoundary) ||
                !BattlefieldSpace.IsPositionTerrainClear(position, radii[index],
                    BattlefieldLayout.Width, BattlefieldLayout.Height, floor.CanOccupy))
                return false;
            for (var other = 0; other < index; other++)
                if (position.DistanceTo(BattlefieldSpace.CellCenter(cells[other])) <
                    radii[index] + radii[other] + BattlefieldSpace.BodyClearance)
                    return false;
        }
        return true;
    }
}
