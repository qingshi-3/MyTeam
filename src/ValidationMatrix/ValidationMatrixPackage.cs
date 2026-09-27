using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Composition;
using TowerAutobattler.Equipment;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.ValidationMatrix;

// A separately authored validation publication. The alpha catalog and recruitment supply are never mutated.
public static class ValidationMatrixPackage
{
    public const string ProjectPath="res://content/validation_matrix/matrix_project.tres";
    public static Task<GamePackagePublicationResult> CreateReadyAsync(Node treeOwner)
    {
        var manifest=JsonSerializer.Deserialize<Dictionary<string,string[]>>(
            FileAccess.GetFileAsString("res://content/validation_matrix/manifest.json"))
            ??throw new InvalidOperationException("Matrix manifest is missing.");
        T[] Load<T>(string key) where T:Resource => manifest[key].Select(path=>GD.Load<T>(path)??
            throw new InvalidOperationException($"Matrix dependency could not be loaded: {path}")).ToArray();
        var project=GD.Load<GameProjectDefinition>(ProjectPath)??throw new InvalidOperationException("Matrix project is missing.");
        var package=new AuthoredContentPackage(project,project.Content,Load<AbilityLoadoutDefinition>("Loadouts"),
            Load<AbilityDefinition>("Abilities"),Load<StatusDefinition>("Statuses"),Load<RelicDefinition>("Relics"))
        {
            Equipment=Load<EquipmentDefinition>("Equipment"),Traits=Load<TraitDefinition>("Traits"),
            TacticalCommands=Load<TacticalCommandDefinition>("TacticalCommands"),
            TacticalCommandScenes=Load<PackedScene>("TacticalCommandScenes")
        };
        return GamePackagePublisher.CreateAuthoredReadyAsync(treeOwner,package);
    }
}
