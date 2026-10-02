using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using TowerAutobattler.ValidationMatrix;

public partial class MatrixResourceLifetimeContractSmoke : Node
{
    // Keep authored graphs alive for the whole load/save/publication operation,
    // just as the application composition root does during startup and retries.
    private readonly List<AbilityDefinition> _authored = [];

    public override async void _Ready()
    {
        var exit = 0;
        var space = $"user://tests/matrix-resource-lifetime/{Guid.NewGuid():N}";
        var directory = ProjectSettings.GlobalizePath(space);
        try
        {
            Directory.CreateDirectory(directory);
            VerifyDefaultAndCycle();
            foreach (var id in new[] { "hero_mx20_p", "hero_mx23_p" })
                VerifyRoundTrip(id, space);
            await VerifyRuntimeRollback();
            GD.Print("MATRIX_RESOURCE_LIFETIME_CONTRACT_OK default inspector-type nested-roundtrip fingerprint cycle-rejection rollback");
        }
        catch (Exception exception) { GD.PrintErr("MATRIX_RESOURCE_LIFETIME_CONTRACT_FAILED: " + exception); exit = 1; }
        finally
        {
            _authored.Clear();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        // The original minimal reproduction still leaked after these collections.
        // The external runner also checks engine output after Quit, not just this marker.
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        GetTree().Quit(exit);
    }

    private static void VerifyDefaultAndCycle()
    {
        using var operation = new MatrixAbilityOperationSpec();
        Require(operation.Effects.Length == 0, "默认子操作为空");
        var property = operation.GetPropertyList().Single(item => item["name"].AsString() == "Effects");
        Require(property["hint_string"].AsString().Contains(nameof(MatrixAbilityOperationSpec), StringComparison.Ordinal),
            "编辑器仍有明确的矩阵操作类型提示");
        var report = new ValidationReport();
        operation.Kind = MatrixOperationKind.Sequence;
        operation.Effects = [operation];
        MatrixAbilityCompiler.Compile(operation, "cyclic-test", report);
        Require(report.CoreErrors.Any(error => error.Contains("cyclic", StringComparison.Ordinal)), "自引用数据仍由编译器拒绝");
        operation.Effects = [];
    }

    private static string Fingerprint(AbilityDefinition ability)
    {
        var report = new ValidationReport();
        var compiled = ability.Operations.OfType<MatrixAbilityOperationSpec>()
            .Select(operation => MatrixAbilityCompiler.Compile(operation, ability.StableId, report)).ToArray();
        Require(!report.HasCoreErrors && compiled.Length > 0 && compiled.All(operation => operation is not null),
            "原始或重新载入的实际技能必须可编译");
        Require(compiled.Any(operation => operation!.Effects.Length > 0), "夹具必须包含嵌套操作");
        return string.Join("\n", compiled.Select(operation => MatrixAbilityCompiler.Fingerprint(operation!)));
    }

    private void VerifyRoundTrip(string id, string space)
    {
        var original = GD.Load<AbilityDefinition>($"res://content/validation_matrix/abilities/{id}.tres");
        _authored.Add(original);
        var before = Fingerprint(original);
        var file = $"{space}/{id}.tres";
        Require(ResourceSaver.Save(original, file) == Error.Ok, "现有类型化 .tres 能再次保存");
        var reloaded = ResourceLoader.Load<AbilityDefinition>(file, cacheMode: ResourceLoader.CacheMode.Ignore);
        if (reloaded is not null) _authored.Add(reloaded);
        Require(reloaded is not null && Fingerprint(reloaded) == before, "嵌套次序、数值和编译指纹在保存重载后不变");
        Require(Fingerprint(original) == before, "保存过程不改变共享定义");
    }

    private async Task VerifyRuntimeRollback()
    {
        var gate = await ValidationMatrixPackage.CreateReadyAsync(this);
        var package = gate.Package ?? throw new InvalidOperationException(string.Join("; ", gate.Report.CoreErrors));
        TraitMatrixRollbackChecks.Run(new BattleLabContentIndex(package));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
