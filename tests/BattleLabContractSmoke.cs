using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using TowerAutobattler.Content;

public partial class BattleLabContractSmoke : Node
{
    private static readonly string[] GenericRuntimeSources =
    [
        "src/Battle/BattleSimulation.cs",
        "src/Battle/BattlePreparationContracts.cs",
        "src/Presentation/BattleScreenController.cs",
        "src/UI/BattleLabScreenController.cs",
        "src/BattleLab"
    ];

    private static readonly string[] LabOwnershipSources =
    [
        "src/BattleLab",
        "src/UI/BattleLabBoardCell.cs",
        "src/UI/BattleLabLibraryCard.cs",
        "src/UI/BattleLabScreenController.cs"
    ];

    private static readonly string[] ForbiddenProductionPersistence =
    [
        "RunApplication",
        "IRunSaveService",
        "SaveActiveRun",
        "SaveMeta",
        "SaveSettings",
        "meta.json",
        "settings.json",
        "active_run.json",
        "schema-v4"
    ];

    public override async void _Ready()
    {
        var exitCode = Run();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private static int Run()
    {
        try
        {
            var simulationSource = ReadRequiredSource("src/Battle/BattleSimulation.cs");
            Require(!simulationSource.Contains("BattleLab", StringComparison.Ordinal),
                "BattleSimulation must not contain a Battle Lab branch");

            foreach (var relative in GenericRuntimeSources)
            {
                var source = ReadRequiredSource(relative);
                var issues = ProductionSourceGuard.FindIssues(source, checkDiscovery: false);
                Require(issues.Count == 0,
                    $"generic runtime source guard {relative}: {string.Join(';', issues)}");
            }

            foreach (var relative in new[]
                     {
                         "src/BattleLab/BattleLabContentIndex.cs",
                         "src/BattleLab/BattleLabPreparationAdapter.cs"
                     })
            {
                var source = ReadRequiredSource(relative);
                var inferenceIssues = ProductionSourceGuard.FindStableIdInferenceIssues(source);
                Require(inferenceIssues.Count == 0,
                    $"typed Lab classification must not infer stable-id structure: {relative}: " +
                    string.Join(';', inferenceIssues));
            }
            VerifyStableIdInferenceGuard();

            var labOwnership = string.Join('\n', LabOwnershipSources.Select(ReadRequiredSource));
            foreach (var forbidden in ForbiddenProductionPersistence)
                Require(!labOwnership.Contains(forbidden, StringComparison.Ordinal),
                    $"Lab ownership must not access production persistence: {forbidden}");

            GD.Print("BATTLE_LAB_CONTRACT_OK source-guard=production-path simulation=no-lab-branch " +
                     "content=no-concrete-id-or-prefix save=no-production-api");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("BATTLE_LAB_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }

    private static string ReadRequiredSource(string relative)
    {
        var path = ProjectSettings.GlobalizePath("res://" + relative.Replace('\\', '/'));
        if (File.Exists(path)) return File.ReadAllText(path);
        if (!Directory.Exists(path))
            throw new InvalidOperationException("Required production source path missing: " + relative);
        var files = Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();
        if (files.Length == 0)
            throw new InvalidOperationException("Required production source directory is empty: " + relative);
        return string.Join('\n', files.Select(File.ReadAllText));
    }

    private static void VerifyStableIdInferenceGuard()
    {
        var positives = new[]
        {
            "definition.Id.StartsWith(\"enemy_\", StringComparison.Ordinal)",
            "entry.StableId.Substring(0, 6)",
            "unit.Definition.ContentId[..6]",
            "candidate.ContentId.Split('_')",
            "candidate.ContentId.IndexOf(\"enemy_\", StringComparison.Ordinal) == 0",
            "0 == candidate.ContentId.IndexOf(\"enemy_\", StringComparison.Ordinal)",
            "Regex.IsMatch(definition.Id, \"^enemy_\")",
            "Regex.IsMatch(definition.Id, \"\"\"^enemy_\"\"\")",
            "new Regex(@\"^enemy_\").IsMatch(entry.StableId)",
            "EnemyPrefixRegex.IsMatch(unit.ContentId)"
        };
        foreach (var source in positives)
            Require(ProductionSourceGuard.FindStableIdInferenceIssues(source).Count > 0,
                "stable-id inference guard positive sentinel: " + source);

        var negatives = new[]
        {
            "definition.Id == expectedId",
            "displayName.StartsWith(\"enemy_\", StringComparison.Ordinal)",
            "definition.Identity.Substring(0, 2)",
            "Regex.IsMatch(displayName, \"^enemy_\")",
            "contentById.TryGetValue(definition.Id, out var content)"
        };
        foreach (var source in negatives)
            Require(ProductionSourceGuard.FindStableIdInferenceIssues(source).Count == 0,
                "stable-id inference guard negative sentinel: " + source);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }
}
