using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;

namespace TowerAutobattler.Run;

// Pure configuration validator shared by persistence and non-persistent tools.
// It performs no loads, saves, migration, publication, or mutation.
public static class ActiveRunConfigurationValidator
{
    public static bool Validate(
        ActiveRunDto run,
        ContentRegistry content,
        CompiledGameProject project)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(project);
        var rules = project.RunRules;
        if (run is null || run.Roster is null || run.Deployment is null || run.Items is null ||
            run.EquippedTacticalCommandIds is null || run.PopulationCapSources is null ||
            run.Version != ActiveRunFormationSchema.CurrentVersion || run.FloorIndex < 0 ||
            run.FloorIndex >= project.Campaign.TotalFloors || run.Roster.Count == 0 ||
            run.LegacyHeroId is not null || run.LegacyHeroHealthRatio != 0 ||
            run.LegacyHeroCell is not null || run.LegacyDeploymentCells is not null ||
            run.Deployment.Any(id => id is null) || run.Roster[0] is null ||
            !content.TryGet(run.Roster[0].ContentId, out var startingHero) ||
            startingHero.Definition is not UnitDefinition { IsHero: true })
            return false;
        if (!RunPopulationPolicy.Validate(run, rules) ||
            !ActiveRunTacticalCommandPolicy.Validate(run, rules, content.Graph) ||
            run.Deployment.Count != rules.PhysicalDeploymentCeiling)
            return false;

        var instanceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in run.Roster)
            if (unit is null || string.IsNullOrWhiteSpace(unit.InstanceId) || !instanceIds.Add(unit.InstanceId) ||
                !float.IsFinite(unit.HealthRatio) || unit.HealthRatio < 0 || unit.HealthRatio > 1 || unit.Rank <= 0 ||
                unit.Equipment is null || unit.Equipment.Count > rules.EquipmentSlotCapacity ||
                !content.TryGet(unit.ContentId, out var entry) ||
                entry.Definition is not UnitDefinition { IsEnemy: false })
                return false;
        if (run.Deployment.Any(id => !string.IsNullOrEmpty(id) && !instanceIds.Contains(id))) return false;
        var deployedIds = run.Deployment.Where(id => !string.IsNullOrEmpty(id)).ToArray();
        if (deployedIds.Distinct(StringComparer.Ordinal).Count() != deployedIds.Length ||
            !RunFormationPolicy.Validate(run, rules))
            return false;

        var durableInstanceIds = new HashSet<string>(instanceIds, StringComparer.Ordinal);
        foreach (var unit in run.Roster)
        {
            var slots = new HashSet<int>();
            foreach (var equipment in unit.Equipment)
                if (equipment is null || string.IsNullOrWhiteSpace(equipment.InstanceId) ||
                    !durableInstanceIds.Add(equipment.InstanceId) || equipment.OwnerHeroInstanceId != unit.InstanceId ||
                    equipment.SlotIndex < 0 || equipment.SlotIndex >= rules.EquipmentSlotCapacity ||
                    !slots.Add(equipment.SlotIndex) || !content.TryGet(equipment.ContentId, out var entry) ||
                    entry.Definition is not ItemDefinition { ProductKind: ItemProductKind.Equipment } ||
                    !content.Graph.TryGetEquipment(equipment.ContentId, out _))
                    return false;
        }
        foreach (var item in run.Items)
        {
            if (item is null || item.Stacks <= 0 || item.Charges < 0 || item.Counters is null ||
                string.IsNullOrWhiteSpace(item.InstanceId) || !durableInstanceIds.Add(item.InstanceId) ||
                !content.TryGet(item.ContentId, out var entry) ||
                entry.Definition is not ItemDefinition { ProductKind: ItemProductKind.Relic } ||
                !content.Graph.TryGetRelic(item.ContentId, out var relic) ||
                !RelicRunScope.HasExactRunCounterSet(relic,
                    item.Counters.Select(counter => counter is null ? null! :
                        new RelicCounterStateSnapshot(counter.CounterId, counter.Value)), out _))
                return false;
        }
        return true;
    }
}
