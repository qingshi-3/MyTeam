using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Content;

// Immutable compiled content published once for a complete game package. Resource
// references are lookup keys only; mutable runtime state never enters this graph.
public sealed class CompiledContentGraph
{
    private readonly ImmutableDictionary<AbilityLoadoutDefinition, CompiledAbilityLoadout> _loadoutsByResource;
    private readonly ImmutableDictionary<string, CompiledAbilityLoadout> _loadoutsByPath;
    private readonly ImmutableDictionary<string, CompiledRelicDefinition> _relicsById;
    private readonly ImmutableDictionary<string, CompiledEquipmentDefinition> _equipmentById;
    private readonly ImmutableDictionary<string, CompiledTraitDefinition> _traitsById;
    private readonly ImmutableDictionary<string, ImmutableArray<CompiledTraitContribution>> _unitTraitContributions;
    private readonly ImmutableDictionary<string, CompiledStatusDefinition> _statusesById;
    private readonly ImmutableDictionary<string, CompiledStatusDefinition> _statusesByPath;
    private readonly ImmutableDictionary<string, CompiledTacticalCommandDefinition> _tacticalCommandsById;
    private readonly ImmutableDictionary<TacticalCommandDefinition, CompiledTacticalCommandDefinition>
        _tacticalCommandsByResource;
    private readonly ImmutableDictionary<string, CompiledTacticalCommandDefinition> _tacticalCommandsByPath;

    internal CompiledContentGraph(
        IEnumerable<CompiledAbilityLoadoutPublication> loadouts,
        ImmutableArray<CompiledAbilityDefinition> abilities,
        ImmutableArray<CompiledStatusDefinition> statuses,
        ImmutableArray<CompiledRelicDefinition> relics,
        ImmutableArray<CompiledEquipmentDefinition> equipment,
        ImmutableArray<CompiledTacticalCommandDefinition> tacticalCommands = default,
        IEnumerable<TacticalCommandDefinition>? tacticalAuthored = null,
        ImmutableArray<CompiledTraitDefinition> traits = default,
        IReadOnlyDictionary<string, ImmutableArray<CompiledTraitContribution>>? unitTraitContributions = null)
    {
        if (tacticalCommands.IsDefault) tacticalCommands = [];
        if (traits.IsDefault) traits = [];
        var byResource = ImmutableDictionary.CreateBuilder<AbilityLoadoutDefinition, CompiledAbilityLoadout>(
            ReferenceEqualityComparer.Instance);
        var byPath = ImmutableDictionary.CreateBuilder<string, CompiledAbilityLoadout>(StringComparer.Ordinal);
        var compiledLoadouts = ImmutableArray.CreateBuilder<CompiledAbilityLoadout>();
        foreach (var publication in loadouts)
        {
            byResource.Add(publication.Authored, publication.Compiled);
            if (!string.IsNullOrWhiteSpace(publication.Authored.ResourcePath))
                byPath.Add(publication.Authored.ResourcePath, publication.Compiled);
            compiledLoadouts.Add(publication.Compiled);
        }

        _loadoutsByResource = byResource.ToImmutable();
        _loadoutsByPath = byPath.ToImmutable();
        _relicsById = relics.ToImmutableDictionary(relic => relic.StableId, StringComparer.Ordinal);
        _equipmentById = equipment.ToImmutableDictionary(definition => definition.StableId, StringComparer.Ordinal);
        _traitsById = traits.ToImmutableDictionary(definition => definition.StableId, StringComparer.Ordinal);
        _unitTraitContributions = (unitTraitContributions ??
                ImmutableDictionary<string, ImmutableArray<CompiledTraitContribution>>.Empty)
            .ToImmutableDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        _statusesById = statuses.ToImmutableDictionary(status => status.StableId, StringComparer.Ordinal);
        _statusesByPath = statuses.Where(status => !string.IsNullOrWhiteSpace(status.ResourcePath))
            .ToImmutableDictionary(status => status.ResourcePath, StringComparer.Ordinal);
        _tacticalCommandsById = tacticalCommands.ToImmutableDictionary(
            definition => definition.StableId,
            StringComparer.Ordinal);
        var tacticalByResource = ImmutableDictionary.CreateBuilder<
            TacticalCommandDefinition,
            CompiledTacticalCommandDefinition>(ReferenceEqualityComparer.Instance);
        var tacticalByPath = ImmutableDictionary.CreateBuilder<string, CompiledTacticalCommandDefinition>(
            StringComparer.Ordinal);
        var tacticalById = tacticalCommands.ToDictionary(
            definition => definition.StableId,
            StringComparer.Ordinal);
        foreach (var authored in tacticalAuthored ?? [])
        {
            if (authored is null || !tacticalById.TryGetValue(authored.StableId, out var compiled)) continue;
            tacticalByResource.Add(authored, compiled);
        }
        foreach (var definition in tacticalCommands)
        {
            if (string.IsNullOrWhiteSpace(definition.ResourcePath)) continue;
            tacticalByPath.Add(definition.ResourcePath, definition);
        }
        _tacticalCommandsByResource = tacticalByResource.ToImmutable();
        _tacticalCommandsByPath = tacticalByPath.ToImmutable();
        AbilityLoadouts = compiledLoadouts.ToImmutable();
        Abilities = abilities;
        Statuses = statuses;
        Relics = relics;
        Equipment = equipment;
        TacticalCommands = tacticalCommands;
        Traits = traits;
    }

    public ImmutableArray<CompiledAbilityLoadout> AbilityLoadouts { get; }
    public ImmutableArray<CompiledAbilityDefinition> Abilities { get; }
    public ImmutableArray<CompiledStatusDefinition> Statuses { get; }
    public ImmutableArray<CompiledRelicDefinition> Relics { get; }
    public ImmutableArray<CompiledEquipmentDefinition> Equipment { get; }
    public ImmutableArray<CompiledTacticalCommandDefinition> TacticalCommands { get; }
    public ImmutableArray<CompiledTraitDefinition> Traits { get; }

    public bool TryResolveLoadout(
        AbilityLoadoutDefinition? authored,
        out CompiledAbilityLoadout loadout)
    {
        if (authored is not null && _loadoutsByResource.TryGetValue(authored, out loadout!)) return true;
        if (authored is not null && !string.IsNullOrWhiteSpace(authored.ResourcePath) &&
            _loadoutsByPath.TryGetValue(authored.ResourcePath, out loadout!))
            return true;
        loadout = null!;
        return false;
    }

    public CompiledAbilityLoadout ResolveLoadout(AbilityLoadoutDefinition authored) =>
        TryResolveLoadout(authored, out var loadout)
            ? loadout
            : throw new InvalidOperationException(
                $"Ability loadout is not part of the published graph: {ResourceLabel(authored)}");

    public bool TryGetRelic(string stableId, out CompiledRelicDefinition definition) =>
        _relicsById.TryGetValue(stableId, out definition!);

    public CompiledRelicDefinition ResolveRelic(string stableId) =>
        TryGetRelic(stableId, out var definition)
            ? definition
            : throw new InvalidOperationException($"Relic is not part of the published graph: {stableId}");

    public bool TryGetEquipment(string stableId, out CompiledEquipmentDefinition definition) =>
        _equipmentById.TryGetValue(stableId, out definition!);

    public CompiledEquipmentDefinition ResolveEquipment(string stableId) =>
        TryGetEquipment(stableId, out var definition)
            ? definition
            : throw new InvalidOperationException($"Equipment is not part of the published graph: {stableId}");

    public bool TryGetTrait(string stableId, out CompiledTraitDefinition definition) =>
        _traitsById.TryGetValue(stableId, out definition!);

    public CompiledTraitDefinition ResolveTrait(string stableId) =>
        TryGetTrait(stableId, out var definition)
            ? definition
            : throw new InvalidOperationException($"Trait is not part of the published graph: {stableId}");

    public ImmutableArray<CompiledTraitContribution> ResolveUnitTraitContributions(string contentId) =>
        _unitTraitContributions.TryGetValue(contentId, out var contributions) ? contributions : [];

    public bool TryGetStatus(string stableId, out CompiledStatusDefinition definition) =>
        _statusesById.TryGetValue(stableId, out definition!);

    public CompiledStatusDefinition ResolveStatus(string stableId) =>
        TryGetStatus(stableId, out var definition)
            ? definition
            : throw new InvalidOperationException($"Status is not part of the published graph: {stableId}");

    public bool TryGetTacticalCommand(
        string stableId,
        out CompiledTacticalCommandDefinition definition) =>
        _tacticalCommandsById.TryGetValue(stableId, out definition!);

    public CompiledTacticalCommandDefinition ResolveTacticalCommand(string stableId) =>
        TryGetTacticalCommand(stableId, out var definition)
            ? definition
            : throw new InvalidOperationException(
                $"Tactical command is not part of the published graph: {stableId}");

    public bool TryResolveTacticalCommand(
        TacticalCommandDefinition? authored,
        out CompiledTacticalCommandDefinition definition)
    {
        if (authored is not null && _tacticalCommandsByResource.TryGetValue(authored, out definition!))
            return true;
        if (authored is not null && !string.IsNullOrWhiteSpace(authored.ResourcePath) &&
            _tacticalCommandsByPath.TryGetValue(authored.ResourcePath, out definition!))
            return true;
        definition = null!;
        return false;
    }

    public CompiledTacticalCommandDefinition ResolveTacticalCommand(
        TacticalCommandDefinition authored) =>
        TryResolveTacticalCommand(authored, out var definition)
            ? definition
            : throw new InvalidOperationException(
                $"Tactical command is not part of the published graph: {ResourceLabel(authored)}");

    public bool TryResolveStatusPath(string resourcePath, out CompiledStatusDefinition definition) =>
        _statusesByPath.TryGetValue(resourcePath, out definition!);

    private static string ResourceLabel(Godot.Resource resource) =>
        string.IsNullOrWhiteSpace(resource.ResourcePath) ? resource.GetType().Name : resource.ResourcePath;
}

internal sealed record ContentGraphCompilationResult(
    CompiledContentGraph? Graph,
    ValidationReport Report);
