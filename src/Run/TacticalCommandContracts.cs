using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Content;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

public static class ActiveRunTacticalCommandPolicy
{
    public const int SlotCount = 2;

    public static bool Validate(
        ActiveRunDto run,
        CompiledRunRules rules,
        CompiledContentGraph graph)
    {
        if (run?.EquippedTacticalCommandIds is null || rules is null || graph is null ||
            rules.TacticalCommandSlotCount != SlotCount ||
            run.EquippedTacticalCommandIds.Count != SlotCount ||
            run.EquippedTacticalCommandIds.Any(string.IsNullOrWhiteSpace) ||
            run.EquippedTacticalCommandIds.Distinct(StringComparer.Ordinal).Count() != SlotCount)
            return false;
        return run.EquippedTacticalCommandIds.All(id => graph.TryGetTacticalCommand(id, out _));
    }

    public static List<string> StarterLoadout(CompiledRunRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.TacticalCommandSlotCount != SlotCount ||
            rules.StarterTacticalCommandIds.Length != SlotCount ||
            rules.StarterTacticalCommandIds.Distinct(StringComparer.Ordinal).Count() != SlotCount)
            throw new ArgumentException("Compiled Run rules do not contain a valid two-command starter loadout.",
                nameof(rules));
        return rules.StarterTacticalCommandIds.ToList();
    }

    public static bool TryLegacyLoadout(
        string legacyHeroId,
        CompiledRunRules rules,
        out List<string> commandIds)
    {
        commandIds = [];
        if (string.IsNullOrWhiteSpace(legacyHeroId) || rules is null ||
            rules.TacticalCommandSlotCount != SlotCount ||
            rules.StarterTacticalCommandIds.Length != SlotCount ||
            !rules.LegacyTacticalCommandByHeroId.TryGetValue(legacyHeroId, out var legacyCommandId))
            return false;
        var second = rules.StarterTacticalCommandIds.FirstOrDefault(id => id != legacyCommandId);
        if (string.IsNullOrWhiteSpace(second)) return false;
        commandIds = [legacyCommandId, second];
        return commandIds.Distinct(StringComparer.Ordinal).Count() == SlotCount;
    }
}
