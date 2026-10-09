using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Growth;
using TowerAutobattler.Run;

public partial class ArchitectureRecoveryContractSmoke : Node
{
    private readonly List<string> _directories = [];

    public override async void _Ready()
    {
        var failures = new List<string>();
        try
        {
            var gate = await GrowthContentPackage.CreateReadyAsync(this,
                defaultNewRunProfileId: RunProfileIds.GrowthTowerLegacy);
            var package = gate.Package ?? throw new InvalidOperationException(string.Join("; ", gate.Report.CoreErrors));
            Require(package.GrowthRules is not null, "成长发布必须携带编译规则");
            async Task Check(string name, Func<Task> run)
            {
                try { await run(); GD.Print("ARCHITECTURE_RECOVERY_CHECK_OK " + name); }
                catch (Exception exception) { failures.Add(name + ": " + exception); }
            }
            await Check("corrupt-save", () => { VerifyCorruptSave(package); return Task.CompletedTask; });
            await Check("backup-failure", () => { VerifyBackupFailure(package); return Task.CompletedTask; });
            await Check("version-backup", () => { VerifyVersionBackup(); return Task.CompletedTask; });
            await Check("growth-publication", () => VerifyGrowthPublication(package));
        }
        catch (Exception exception) { failures.Add(exception.ToString()); }
        finally
        {
            foreach (var directory in _directories)
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        if (failures.Count == 0) GD.Print("ARCHITECTURE_RECOVERY_CONTRACT_OK");
        else GD.PrintErr("ARCHITECTURE_RECOVERY_CONTRACT_FAILED: " + string.Join("\n", failures));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(failures.Count == 0 ? 0 : 1);
    }

    private (SaveService Save, string RunPath) CreateSave()
    {
        var space = $"tests/architecture-recovery/{Guid.NewGuid():N}";
        var directory = ProjectSettings.GlobalizePath("user://" + space);
        Directory.CreateDirectory(directory);
        _directories.Add(directory);
        return (new SaveService(space), Path.Combine(directory, "active_run.json"));
    }

    private void VerifyCorruptSave(CompiledGamePackage package)
    {
        var (save, path) = CreateSave();
        foreach (var damaged in new[] { "{broken", "null", "[1,2]" })
        {
            File.WriteAllText(path, damaged);
            var app = new RunApplication(package.Content, save, package.Project, package.GrowthRules);
            Require(app.ActiveRun is null && app.ActiveRunLoadDiagnostic is not null, "坏档拒绝载入");
            Require(File.ReadAllText(path) == damaged, "读取不能重置原档");
            var before = Directory.Exists(path + ".backups")
                ? Directory.GetFiles(path + ".backups").ToHashSet() : [];
            Require(app.BeginOpeningRecruitment(20261001), "明确新局可保存并发布");
            var backups = Directory.GetFiles(path + ".backups").Where(file => !before.Contains(file)).ToArray();
            Require(backups.Length == 1 && File.ReadAllText(backups[0]) == damaged, "备份必须完整且不能覆盖旧备份");
            Require(save.LoadActiveRun()?.Seed == 20261001 && app.ActiveRun?.OpeningRecruitment is not null,
                "新局可重载且开局草稿已发布");
        }
        Require(Directory.GetFiles(path + ".backups").Length == 3, "多次恢复保留各自原件");
        Require(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp").Length == 0, "成功写盘清理临时文件");
    }

    private void VerifyBackupFailure(CompiledGamePackage package)
    {
        var (save, path) = CreateSave();
        var app = new RunApplication(package.Content, save, package.Project, package.GrowthRules);
        Require(app.BeginOpeningRecruitment(77), "先建立有效活动草稿");
        var active = app.ActiveRun;
        var snapshot = save.Serialize(active);
        File.WriteAllText(path, "{damaged-after-load");
        File.WriteAllText(path + ".backups", "block directory creation");
        GD.Print("EXPECTED_SAVE_FAILURE_BEGIN");
        var saved = app.BeginOpeningRecruitment(88);
        GD.Print("EXPECTED_SAVE_FAILURE_END");
        Require(!saved && ReferenceEquals(active, app.ActiveRun) && save.Serialize(app.ActiveRun) == snapshot,
            "备份失败不能发布新局或改变内存草稿");
        Require(File.ReadAllText(path) == "{damaged-after-load", "备份失败不能覆盖损坏原件");
        Require(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp").Length == 0, "失败也清理自己的临时文件");
        File.Delete(path + ".backups");
        Require(app.BeginOpeningRecruitment(88), "存储故障排除后可重试");
    }

    private void VerifyVersionBackup()
    {
        var (save, path) = CreateSave();
        foreach (var version in new[] { 9, ActiveRunFormationSchema.CurrentVersion + 1 })
        {
            var original = save.Serialize(new ActiveRunDto { Version = version, Seed = 123 });
            File.WriteAllText(path, original);
            Require(save.SaveActiveRun(new ActiveRunDto { Version = ActiveRunFormationSchema.CurrentVersion, Seed = 456 }),
                "不同版本写回成功");
            var backup = Directory.GetFiles(Path.GetDirectoryName(path)!, $"active_run.json.v{version}.*.bak");
            Require(backup.Length == 1 && File.ReadAllText(backup[0]) == original, "旧版本与未来版本原件均保留");
        }
    }

    private async Task VerifyGrowthPublication(CompiledGamePackage published)
    {
        var rules = GD.Load<GrowthRulesDefinition>(GrowthContentPackage.RulesPath);
        var cost = rules.AscensionCost;
        var pool = rules.FirstDiscoveryPool;
        var heroId = rules.Heroes[0].ContentId;
        async Task Rejected(string diagnostic)
        {
            var gate = await GrowthContentPackage.CreateReadyAsync(this);
            Require(gate.Package is null && gate.PublishedVersion == 0 && gate.Report.HasCoreErrors,
                "非法成长配置拒绝整包且无已发布版本");
            Require(gate.Report.CoreErrors.Any(error => error.Contains(diagnostic, StringComparison.Ordinal)),
                "保留具体配置诊断：" + diagnostic);
        }
        try
        {
            rules.AscensionCost = 0;
            await Rejected("成长材料配置无效");
            Require(published.GrowthRules!.AscensionCost == cost, "已发布编译规则不随作者资源改变");
            rules.AscensionCost = cost;
            rules.FirstDiscoveryPool = ["missing-a", "missing-b", "missing-c"];
            await Rejected("discovery hero");
            rules.FirstDiscoveryPool = pool;
            rules.Heroes[0].ContentId = "missing-growth-hero";
            await Rejected("Growth hero");
        }
        finally
        {
            rules.AscensionCost = cost;
            rules.FirstDiscoveryPool = pool;
            rules.Heroes[0].ContentId = heroId;
        }
        var repaired = await GrowthContentPackage.CreateReadyAsync(this);
        Require(repaired.Package?.GrowthRules is not null && !repaired.Report.HasCoreErrors, "修复后可完整重新发布");
    }

    private static void Require(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException(message);
    }
}
