using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;

public static class TestProjectFixture
{
    public static GameProjectDefinition Authored() =>
        GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres") ??
        throw new InvalidOperationException("alpha project resource load failed");

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
        GamePackagePublisher.CreateReadyAsync(
            treeOwner,
            Authored(),
            additionalValidationScenes,
            additionalStructuralValidationScenes);
}
