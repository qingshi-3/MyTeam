using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Growth;

public static class GrowthContentPackage
{
    public const string ProjectPath = "res://content/expedition/current/project.tres";
    public const string LegacyExpeditionProjectPath = "res://content/expedition/expedition_project.tres";
    public const string LegacyProjectPath = "res://content/growth/growth_project.tres";
    public const string RulesPath = "res://content/expedition/current/growth_rules.tres";

    public static async Task<GamePackagePublicationResult> CreateReadyAsync(Node treeOwner,
        GrowthRulesDefinition? authoredRules = null, string? defaultNewRunProfileId = null)
    {
        var result = await GamePackagePublisher.CreateAuthoredReadyAsync(treeOwner, LoadAuthored(authoredRules));
        // Focused legacy fixtures still publish/validate the complete production package.
        return defaultNewRunProfileId is not null && result.Package is { } package
            ? result with { Package = package.ForProfile(defaultNewRunProfileId) } : result;
    }

    internal static AuthoredContentPackage LoadAuthored(GrowthRulesDefinition? authoredRules = null,
        GrowthRulesDefinition? authoredLegacyRules = null)
    {
        if (!FileAccess.FileExists(ProjectPath))
            throw new InvalidOperationException("征程配置缺失，存档未被改动。");
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
            FileAccess.GetFileAsString("res://content/expedition/current/manifest.json"))
            ?? throw new InvalidOperationException("Growth manifest is missing.");
        T[] Load<T>(string key) where T : Resource => manifest[key].Select(path => GD.Load<T>(path) ??
            throw new InvalidOperationException($"Growth dependency could not be loaded: {path}")).ToArray();
        var project = GD.Load<GameProjectDefinition>(ProjectPath) ?? throw new InvalidOperationException("Growth project is missing.");
        var rules = authoredRules ?? GD.Load<GrowthRulesDefinition>(RulesPath) ?? throw new InvalidOperationException("Growth rules are missing.");
        var package = new AuthoredContentPackage(project, project.Content, Load<AbilityLoadoutDefinition>("Loadouts"),
            Load<AbilityDefinition>("Abilities"), Load<StatusDefinition>("Statuses"), Load<RelicDefinition>("Relics"))
        {
            GrowthRules = rules,
            DefaultNewRunProfileId = RunProfileIds.EightTraitExpedition,
            Profiles =
            [
                new(RunProfileIds.EightTraitExpedition, project, rules, CompatibilityVersion: 3, ProgressionProtocolVersion: 2, RewardProtocolVersion: 2)
            ],
            Equipment = Load<EquipmentDefinition>("Equipment"), Traits = Load<TraitDefinition>("Traits"),
            TacticalCommands = Load<TacticalCommandDefinition>("TacticalCommands"),
            TacticalCommandScenes = Load<PackedScene>("TacticalCommandScenes")
        };
        return package;
    }

    public static CompiledGrowthRules LoadRules(ContentRegistry content) => GrowthRulesCompiler.Compile(
        GD.Load<GrowthRulesDefinition>(RulesPath) ?? throw new InvalidOperationException("Growth rules are missing."), content);
}
