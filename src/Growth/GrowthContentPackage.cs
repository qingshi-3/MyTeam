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
    public const string ProjectPath = "res://content/growth/growth_project.tres";
    public const string RulesPath = "res://content/growth/growth_rules.tres";

    public static Task<GamePackagePublicationResult> CreateReadyAsync(Node treeOwner)
    {
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
            FileAccess.GetFileAsString("res://content/growth/manifest.json"))
            ?? throw new InvalidOperationException("Growth manifest is missing.");
        T[] Load<T>(string key) where T : Resource => manifest[key].Select(path => GD.Load<T>(path) ??
            throw new InvalidOperationException($"Growth dependency could not be loaded: {path}")).ToArray();
        var project = GD.Load<GameProjectDefinition>(ProjectPath) ?? throw new InvalidOperationException("Growth project is missing.");
        var rules = GD.Load<GrowthRulesDefinition>(RulesPath) ?? throw new InvalidOperationException("Growth rules are missing.");
        var package = new AuthoredContentPackage(project, project.Content, Load<AbilityLoadoutDefinition>("Loadouts"),
            Load<AbilityDefinition>("Abilities"), Load<StatusDefinition>("Statuses"), Load<RelicDefinition>("Relics"))
        {
            Equipment = Load<EquipmentDefinition>("Equipment"), Traits = Load<TraitDefinition>("Traits"),
            TacticalCommands = Load<TacticalCommandDefinition>("TacticalCommands"),
            TacticalCommandScenes = Load<PackedScene>("TacticalCommandScenes"),
            AdditionalLoadoutReferences = rules.Heroes.SelectMany(hero => new[] { hero.BaseLoadout, hero.AscendedLoadout })
                .Concat(rules.Spells.Select(spell => spell.BattleLoadout)).Where(loadout => loadout is not null).ToArray()
        };
        return GamePackagePublisher.CreateAuthoredReadyAsync(treeOwner, package);
    }

    public static CompiledGrowthRules LoadRules(ContentRegistry content) => GrowthRulesCompiler.Compile(
        GD.Load<GrowthRulesDefinition>(RulesPath) ?? throw new InvalidOperationException("Growth rules are missing."), content);
}
