using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

namespace TowerAutobattler.Equipment;

public interface IRunEquipmentPersistence
{
    bool ValidateRun(ActiveRunDto run);
    ActiveRunDto CloneRun(ActiveRunDto source);
    bool TryPublish(ActiveRunDto working, ActiveRunDto authoritative);
}

public sealed class RunEquipmentService
{
    private readonly CompiledContentGraph _graph;
    private readonly CompiledRunRules _rules;
    private readonly IRunEquipmentPersistence _persistence;

    public RunEquipmentService(
        CompiledContentGraph graph,
        CompiledRunRules rules,
        IRunEquipmentPersistence persistence)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
    }

    public bool Equip(
        ActiveRunDto? run,
        string ownerHeroInstanceId,
        int slotIndex,
        string equipmentContentId)
    {
        if (run is null || !_persistence.ValidateRun(run) || string.IsNullOrWhiteSpace(ownerHeroInstanceId) ||
            slotIndex < 0 || slotIndex >= _rules.EquipmentSlotCapacity ||
            !_graph.TryGetEquipment(equipmentContentId, out _))
            return false;
        var working = _persistence.CloneRun(run);
        var owner = working.Roster.SingleOrDefault(hero => hero.InstanceId == ownerHeroInstanceId);
        if (owner?.Equipment is null || !TryNextInstanceSequence(working, out var sequence)) return false;
        owner.Equipment.RemoveAll(item => item.SlotIndex == slotIndex);
        owner.Equipment.Add(new EquipmentInstanceState
        {
            InstanceId = $"equipment-{sequence}",
            ContentId = equipmentContentId,
            OwnerHeroInstanceId = ownerHeroInstanceId,
            SlotIndex = slotIndex
        });
        owner.Equipment = owner.Equipment.OrderBy(item => item.SlotIndex).ToList();
        return _persistence.ValidateRun(working) && _persistence.TryPublish(working, run);
    }

    public bool Remove(ActiveRunDto? run, string ownerHeroInstanceId, int slotIndex)
    {
        if (run is null || !_persistence.ValidateRun(run) || string.IsNullOrWhiteSpace(ownerHeroInstanceId) ||
            slotIndex < 0 || slotIndex >= _rules.EquipmentSlotCapacity)
            return false;
        var working = _persistence.CloneRun(run);
        var owner = working.Roster.SingleOrDefault(hero => hero.InstanceId == ownerHeroInstanceId);
        if (owner?.Equipment is null || owner.Equipment.RemoveAll(item => item.SlotIndex == slotIndex) != 1)
            return false;
        return _persistence.ValidateRun(working) && _persistence.TryPublish(working, run);
    }

    private static bool TryNextInstanceSequence(ActiveRunDto run, out int sequence)
    {
        sequence = 0;
        if (run.Roster is null || run.Items is null || run.Roster.Any(hero => hero?.Equipment is null) ||
            run.Items.Any(item => item is null) || run.Roster.SelectMany(hero => hero.Equipment).Any(item => item is null))
            return false;
        var maximum = run.Roster.Select(hero => hero.InstanceId)
            .Concat(run.Items.Select(item => item.InstanceId))
            .Concat(run.Roster.SelectMany(hero => hero.Equipment).Select(item => item.InstanceId))
            .Select(ParseInstanceSuffix)
            .DefaultIfEmpty(0)
            .Max();
        if (maximum == int.MaxValue) return false;
        sequence = maximum + 1;
        return true;
    }

    private static int ParseInstanceSuffix(string? instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return 0;
        var separator = instanceId.LastIndexOf('-');
        return separator >= 0 && int.TryParse(instanceId[(separator + 1)..], out var value) ? value : 0;
    }
}
