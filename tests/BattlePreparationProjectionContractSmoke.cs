using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.ValidationMatrix;
using static TraitMatrixValidationSupport;

public partial class BattlePreparationProjectionContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var gate = await ValidationMatrixPackage.CreateReadyAsync(this);
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var heroes = index.PlayerHeroes.Where(h => h.StableId.StartsWith("hero_mx", StringComparison.Ordinal)).ToArray();
            foreach (var hero in heroes)
            {
                BattleConfig Config() => Laboratory(index, [hero.StableId, hero.StableId == "hero_mx04" ? "hero_mx25" : "hero_mx04"],
                    ["soldier_dummy_static"], 20261001);
                var projected = BattlePreparationProjection.Build(Config());
                var frozen = Fingerprint(projected);
                using (var battle = new BattleSimulation(Config()))
                {
                    Require(frozen == Fingerprint(BattleSimulation.CapturePreparation(battle)),
                        "prepared state differs from authoritative initialization: " + hero.StableId);
                    battle.Step();
                }
                Require(frozen == Fingerprint(projected), "frozen preparation changed after combat/disposal");
            }
            var config = Laboratory(index, ["hero_mx01", "hero_mx04", "hero_mx20", "hero_mx25"],
                ["soldier_dummy_static", "soldier_dummy_melee"], 20261001);
            BattlePreparationSnapshot Full()
            {
                using var battle = new BattleSimulation(config);
                return BattleSimulation.CapturePreparation(battle);
            }
            for (var i = 0; i < 3; i++) { _ = Full(); _ = BattlePreparationProjection.Build(config); }
            (long Allocated, double Milliseconds) Measure(Func<BattlePreparationSnapshot> factory)
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread();
                var watch = Stopwatch.StartNew();
                for (var i = 0; i < 20; i++) _ = factory();
                return (GC.GetAllocatedBytesForCurrentThread() - bytes, watch.Elapsed.TotalMilliseconds);
            }
            var full = Measure(Full);
            var preparation = Measure(() => BattlePreparationProjection.Build(config));
            var folder = ProjectSettings.GlobalizePath("res://outputs/architecture-boundaries-20261001");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "preparation-profile.json"), JsonSerializer.Serialize(new
            {
                Cases = heroes.Length, Repetitions = 20, Configuration = "Debug fixed actual six-unit initialization",
                FullAllocated = full.Allocated, PreparationAllocated = preparation.Allocated,
                FullMilliseconds = full.Milliseconds, PreparationMilliseconds = preparation.Milliseconds,
                Note = "Timing is diagnostic only, not a shipping performance claim. Both paths capture identical immutable projections."
            }, new JsonSerializerOptions { WriteIndented = true }));
            foreach (var file in new[] { "src/UI/DeploymentScreenController.cs", "src/UI/OpponentChoicePanel.cs",
                "src/UI/RosterEquipmentPreview.cs", "src/BattleLab/BattleLabDerivedProjection.cs" })
                Require(!File.ReadAllText(ProjectSettings.GlobalizePath("res://" + file)).Contains("new BattleSimulation"),
                    "presentation bypasses shared preparation port: " + file);
            GD.Print($"BATTLE_PREPARATION_PROJECTION_CONTRACT_OK heroes={heroes.Length} initialization-parity frozen-state shared-query-port full-bytes={full.Allocated} preparation-bytes={preparation.Allocated}");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("BATTLE_PREPARATION_PROJECTION_CONTRACT_FAILED " + e); GetTree().Quit(1); }
    }
    private static string Fingerprint(BattlePreparationSnapshot snapshot) => JsonSerializer.Serialize(new
    {
        Traits = snapshot.Traits.Fingerprint,
        Units = snapshot.Units.Select(u => new { u.RuntimeId, u.SourceInstanceId, u.Team, u.Definition.ContentId,
            X = u.Position.X, Y = u.Position.Y, u.BodyRadius, u.IsTemporary, u.Health, u.Shield, u.CurrentMana, u.MaxMana,
            Attributes = u.Attributes.Values.OrderBy(p => p.Key).Select(p => (Attribute: p.Key.ToString(), Value: p.Value)).Select(p => new { p.Attribute, p.Value }),
            u.Statuses })
    });
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
