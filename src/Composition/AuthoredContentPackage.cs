using System.Collections.Generic;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Growth;
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
    public GrowthRulesDefinition? GrowthRules { get; init; }
    // Empty preserves the single-project authoring entry point. An explicit set
    // owns every profile root, including compatibility rules, for the package lifetime.
    public IReadOnlyList<AuthoredRunProfile> Profiles { get; init; } = [];
    public string DefaultNewRunProfileId { get; init; } = string.Empty;
    public IReadOnlyList<EquipmentDefinition?> Equipment { get; init; } = [];
    public IReadOnlyList<TacticalCommandDefinition?> TacticalCommands { get; init; } = [];
    public IReadOnlyList<PackedScene?> TacticalCommandScenes { get; init; } = [];
    public IReadOnlyList<TraitDefinition?> Traits { get; init; } = [];
    // Explicit non-scene owners (for example, an authored ascension or spell recipe).
    // This is a reference list, not an exemption from dependency/orphan validation.
    public IReadOnlyList<AbilityLoadoutDefinition?> AdditionalLoadoutReferences { get; init; } = [];
}

internal sealed record AuthoredRunProfile(
    string ProfileId,
    GameProjectDefinition Project,
    GrowthRulesDefinition? GrowthRules = null,
    int CompatibilityVersion = 1,
    int ProgressionProtocolVersion = 1,
    int RewardProtocolVersion = 1,
    int IdentityProtocolVersion = 1);
