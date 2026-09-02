using System.Collections.Generic;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Composition;

/// <summary>
/// Path-independent complete game-authoring batch used by focused content packages.
/// Production publication adds strict directory and bidirectional disk-completeness gates.
/// </summary>
internal sealed record AuthoredContentPackage(
    GameProjectDefinition? Project,
    ContentCatalog? Catalog,
    IReadOnlyList<AbilityLoadoutDefinition?> Loadouts,
    IReadOnlyList<AbilityDefinition?> Abilities,
    IReadOnlyList<StatusDefinition?> Statuses,
    IReadOnlyList<RelicDefinition?> Relics)
{
    public IReadOnlyList<EquipmentDefinition?> Equipment { get; init; } = [];
    public IReadOnlyList<TacticalCommandDefinition?> TacticalCommands { get; init; } = [];
    public IReadOnlyList<PackedScene?> TacticalCommandScenes { get; init; } = [];
    public IReadOnlyList<TraitDefinition?> Traits { get; init; } = [];
}
