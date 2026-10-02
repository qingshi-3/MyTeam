using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Battle;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Content;

internal sealed record AbilityStatusGraphCompilationResult(
    AbilityBatchCompilationResult Abilities,
    StatusBatchCompilationResult Statuses,
    ValidationReport Report);

internal sealed record RelicGraphCompilationResult(
    RelicBatchCompilationResult Relics,
    ValidationReport Report);

internal sealed record EquipmentGraphCompilationResult(
    EquipmentBatchCompilationResult Equipment,
    ValidationReport Report);

internal sealed record TraitGraphCompilationResult(
    System.Collections.Immutable.ImmutableArray<CompiledTraitDefinition> Definitions,
    IReadOnlyDictionary<string, System.Collections.Immutable.ImmutableArray<CompiledTraitContribution>> UnitContributions,
    ValidationReport Report);

internal sealed record TacticalCommandAuthoredGraph(
    IReadOnlyList<TacticalCommandDefinition?> Definitions,
    IReadOnlyList<PackedScene?> Scenes);

internal sealed record AbilityStatusAuthoredGraph(
    IReadOnlyList<AbilityLoadoutDefinition?> Loadouts,
    IReadOnlyList<AbilityDefinition?> Abilities,
    IReadOnlyList<StatusDefinition?> Statuses,
    IReadOnlyList<AbilityLoadoutDefinition?> ContentLoadoutReferences,
    IReadOnlySet<string> ValidContentIds)
{
    public IReadOnlyList<StatusDefinition?> AdditionalStatusReferences { get; init; } = [];
    public IReadOnlySet<string>? ValidUnitContentIds { get; init; }
}

