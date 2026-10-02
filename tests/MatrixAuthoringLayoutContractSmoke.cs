using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Content;

public partial class MatrixAuthoringLayoutContractSmoke : Node
{
    private readonly List<Resource> _roots = [];
    public override void _Ready()
    {
        try
        {
            var output = ProjectSettings.GlobalizePath("res://outputs/architecture-boundaries-20261001/matrix-authoring-before.json");
            var current = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var root = ProjectSettings.GlobalizePath("res://");
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "content"), "*.tres", SearchOption.AllDirectories)
                .Where(file => File.ReadAllText(file).Contains("MatrixAbilityOperationSpec.cs", StringComparison.Ordinal)).OrderBy(file => file, StringComparer.Ordinal))
            {
                var path = "res://" + Path.GetRelativePath(root, file).Replace('\\', '/');
                var ability = GD.Load<AbilityDefinition>(path);
                _roots.Add(ability);
                current.Add(path, Fingerprint(ability));
            }
            if (OS.GetCmdlineUserArgs().Contains("--record-baseline"))
            {
                if (File.Exists(output)) throw new InvalidOperationException("Refusing to replace the pre-migration baseline.");
                File.WriteAllText(output, JsonSerializer.Serialize(current, new JsonSerializerOptions { WriteIndented = true }));
                GD.Print("MATRIX_AUTHORING_BASELINE_OK count=" + current.Count);
            }
            else
            {
                if (OS.GetCmdlineUserArgs().Contains("--compare-baseline"))
                {
                    var baseline = JsonSerializer.Deserialize<SortedDictionary<string,string>>(File.ReadAllText(output))!;
                    Require(current.Count == baseline.Count && current.All(pair => baseline.GetValueOrDefault(pair.Key) == pair.Value),
                        "authoring migration changed compiled combat operations");
                }
                // A saved asset must expose only the grouped editor schema. Flat C#
                // convenience setters are deliberately not serialization properties.
                using (var operation = new MatrixAbilityOperationSpec())
                {
                    var names = operation.GetPropertyList().Select(p => p["name"].AsString()).ToHashSet();
                    Require(names.Contains("Targeting") && names.Contains("Magnitude") && names.Contains("Execution") &&
                        names.Contains("Reaction") && !names.Contains("Amount") && !names.Contains("SourceRelation"),
                        "Inspector still exposes the flat operation schema");
                }
                var space = "user://tests/matrix-authoring-layout/" + Guid.NewGuid().ToString("N");
                DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(space));
                var index = 0;
                foreach (var pair in current)
                {
                    var original = (AbilityDefinition)_roots[index];
                    var path = $"{space}/{index++}.tres";
                    Require(ResourceSaver.Save(original, path) == Error.Ok, "save grouped authoring");
                    var reloaded = ResourceLoader.Load<AbilityDefinition>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
                    _roots.Add(reloaded);
                    Require(Fingerprint(reloaded) == pair.Value, "round-trip changed grouped operation: " + pair.Key);
                }
                GD.Print("MATRIX_AUTHORING_LAYOUT_CONTRACT_OK count=" + current.Count + " unchanged-compiled-fingerprints full-save-reload");
            }
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("MATRIX_AUTHORING_LAYOUT_CONTRACT_FAILED " + e); GetTree().Quit(1); }
    }
    private static string Fingerprint(AbilityDefinition ability)
    {
        var report = new ValidationReport();
        var operations = ability.Operations.OfType<MatrixAbilityOperationSpec>().Select(operation =>
            MatrixAbilityCompiler.Compile(operation, ability.StableId, report)).ToArray();
        Require(operations.Length > 0 && operations.All(operation => operation is not null) && !report.HasCoreErrors,
            "matrix authoring compile: " + string.Join(';', report.CoreErrors));
        return string.Join("\n", operations.Select(operation => MatrixAbilityCompiler.Fingerprint(operation!)));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
