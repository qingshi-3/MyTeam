using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Abilities;
using TowerAutobattler.Statuses;
using TowerAutobattler.Relics;
using TowerAutobattler.Equipment;
using TowerAutobattler.Traits;
using TowerAutobattler.TacticalCommands;

public static class TestProjectFixture
{
    public static GameProjectDefinition Authored() =>
        throw new InvalidOperationException("旧内容夹具已归档，依赖它的测试需随新内容迁移；见 docs/testcases/content-reset.md。不得把此状态记为测试通过。");

    public static CompiledGameProject Load(ContentRegistry registry)
    {
        var result = GameProjectCompiler.Compile(Authored(), registry.Graph);
        return result.Project ?? throw new InvalidOperationException(
            "alpha project compile: " + string.Join("; ", result.Report.CoreErrors));
    }

    public static Task<GamePackagePublicationResult> PublishAsync(
        Node treeOwner,
        IEnumerable<PackedScene>? additionalValidationScenes = null,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes = null) =>
        GamePackagePublisher.CreateAuthoredReadyAsync(treeOwner, FrozenPackage(),
            additionalValidationScenes, additionalStructuralValidationScenes);

    // Frozen compatibility coverage only. New hero/dummy tests publish the current project directly.
    private static AuthoredContentPackage FrozenPackage()
    {
        var project = Authored();
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
            FileAccess.GetFileAsString("res://tests/fixtures/legacy-roster/manifest.json"))!;
        T[] Load<T>(string key) where T : Resource => manifest[key].Select(GD.Load<T>).ToArray();
        return new AuthoredContentPackage(project, project.Content,
            Load<AbilityLoadoutDefinition>("Loadouts"), Load<AbilityDefinition>("Abilities"),
            Load<StatusDefinition>("Statuses"), Load<RelicDefinition>("Relics"))
        {
            Equipment = Load<EquipmentDefinition>("Equipment"), Traits = Load<TraitDefinition>("Traits"),
            TacticalCommands = Load<TacticalCommandDefinition>("TacticalCommands"),
            TacticalCommandScenes = Load<PackedScene>("TacticalCommandScenes")
        };
    }
}
