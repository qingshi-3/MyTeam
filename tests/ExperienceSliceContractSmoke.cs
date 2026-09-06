using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Experience;

public partial class ExperienceSliceContractSmoke : Node
{
    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            var saves = SaveFingerprint();
            var gate = await TestProjectFixture.PublishAsync(this);
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(";", gate.Report.CoreErrors));
            var definition = GD.Load<ExperienceSliceDefinition>("res://content/experience/reward_loop.tres");
            var resourceBefore = ResourceGraphFingerprint.Compute([definition, package.Content.Catalog]);
            VerifyLoop(package, definition);
            VerifyFailure(package, definition);
            Require(ResourceGraphFingerprint.Compute([definition, package.Content.Catalog]) == resourceBefore, "shared definitions unchanged");
            Require(SaveFingerprint() == saves, "production save files unchanged");
            GD.Print("EXPERIENCE_SLICE_CONTRACT_OK isolated=memory reward=exactly-once gear=owned-transfer-displacement checkpoint=deep actual-battle=two failure=no-reward resources=unchanged");
        }
        catch (Exception exception) { exit = 1; GD.PrintErr("EXPERIENCE_SLICE_CONTRACT_FAILED: " + exception); }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exit);
    }

    private static void VerifyLoop(CompiledGamePackage package, ExperienceSliceDefinition definition)
    {
        var session = new ExperienceSliceSession(package, definition);
        var starting = JsonSerializer.Serialize(session.Run);
        var preview = session.PreviewRelic(definition.RelicIds[0]);
        Require(preview.Contains("→"), "relic preview has real beneficiaries");
        Require(JsonSerializer.Serialize(session.Run) == starting, "preview is read-only");
        Require(!session.ChooseRelic("unknown") && session.ChooseRelic(definition.RelicIds[0]), "offer membership");
        Require(!session.ChooseRelic(definition.RelicIds[1]) && session.Run.Items.Count == 1, "repeated relic claim rejected");
        Require(session.ChooseRecruit(definition.RecruitIds[1]), "recruit");
        Require(!session.ChooseRecruit(definition.RecruitIds[0]) && session.Run.Roster.Count == 5, "repeated recruit rejected");
        var reserve = session.Run.Roster.Last();
        Require(session.Application.MoveDeploymentUnit(reserve.InstanceId, session.Run.Deployment.FindIndex(string.IsNullOrEmpty)), "deploy recruited hero");
        using var battle = new BattleSimulation(session.StartBattle());
        Require(!session.MoveEquipment(session.Inventory[0].InstanceId, reserve.InstanceId, 0), "equipment locked during battle");
        var result = battle.RunToEnd();
        GD.Print($"EXPERIENCE_FIRST outcome={result.Outcome} ticks={result.Ticks}");
        Require(result.Outcome == BattleOutcome.PlayerVictory, "baseline first encounter has a viable path");
        Require(session.AcceptResult(result) && !session.AcceptResult(result), "battle settlement exactly once");
        Require(session.ContinueReport() && !session.ContinueReport(), "report route exactly once");
        var afterFirst = JsonSerializer.Serialize(session.Run);
        Require(session.ChooseEquipment(definition.EquipmentIds[2]) && !session.ChooseEquipment(definition.EquipmentIds[0]), "equipment reward once");
        var item = session.Inventory.Last();
        var firstOwner = session.Run.Roster[0].InstanceId;
        var nextOwner = session.Run.Roster.Single(h => h.ContentId == "soldier_longbow").InstanceId;
        var beforeEquip = session.PreparedStats();
        Require(!session.MoveEquipment("unknown", firstOwner, 0) && !session.MoveEquipment(item.InstanceId, "absent", 0) &&
                !session.MoveEquipment(item.InstanceId, firstOwner, 3), "invalid gear moves reject");
        Require(session.MoveEquipment(item.InstanceId, nextOwner, 0), "assign earned equipment");
        var afterEquip = session.PreparedStats();
        Require(afterEquip.Single(s => s.InstanceId == nextOwner).Damage > beforeEquip.Single(s => s.InstanceId == nextOwner).Damage,
            "earned equipment changes actual prepared attack");
        Require(session.MoveEquipment(item.InstanceId, firstOwner, 0), "transfer and displace initial armor");
        Require(session.Inventory.Count == 2 && session.Run.Roster.SelectMany(h => h.Equipment).Count() == 1,
            "displaced armor retained in bag without equipped duplicate");
        Require(session.Run.Roster.Single(h => h.InstanceId == firstOwner).Equipment.Single().InstanceId == item.InstanceId, "instance preserved");
        Require(session.MoveEquipment(session.Inventory[0].InstanceId, firstOwner, 1), "reequip displaced original");
        using var second = new BattleSimulation(session.StartBattle());
        var secondResult = second.RunToEnd();
        GD.Print($"EXPERIENCE_SECOND outcome={secondResult.Outcome} ticks={secondResult.Ticks}");
        Require(session.AcceptResult(secondResult) && session.ContinueReport() && session.Stage == ExperienceStage.Complete,
            "real second battle reaches experiment review");
        Require(session.Retry(true) && session.Stage == ExperienceStage.Equipment, "post-battle checkpoint available");
        Require(JsonSerializer.Serialize(session.Run) == afterFirst && session.Inventory.Count == 1, "checkpoint restores health identity gear and opportunity");
        Require(session.History.Count == 1 && session.History[0].Results.Length == 2, "prior branch retained");
        Require(session.ChooseEquipment(definition.EquipmentIds[1]), "alternative reward after checkpoint");
        Require(session.Retry(false) && JsonSerializer.Serialize(session.Run) == starting, "full restart is identical");
        Require(session.Run.Items.Count == 0 && session.History.Count == 2, "new branch doesn't erase previous trials");
    }

    private static void VerifyFailure(CompiledGamePackage package, ExperienceSliceDefinition definition)
    {
        using var harder = (ExperienceSliceDefinition)definition.Duplicate();
        harder.FirstEnemyCount = 8;
        var session = new ExperienceSliceSession(package, harder);
        Require(session.ChooseRelic(harder.RelicIds[0]) && session.ChooseRecruit(null), "failure setup");
        // Use real formation commands; retain one fragile ranged hero and withdraw the rest.
        foreach (var hero in session.Run.Roster.Where(h => h.ContentId != "soldier_longbow").ToArray())
            Require(session.Application.WithdrawDeploymentUnit(hero.InstanceId), "withdraw failure fixture");
        using var battle = new BattleSimulation(session.StartBattle());
        var result = battle.RunToEnd();
        Require(result.Outcome != BattleOutcome.PlayerVictory, "actual losing fixture");
        Require(session.AcceptResult(result) && session.ContinueReport(), "failure report completes");
        Require(!session.ChooseEquipment(harder.EquipmentIds[0]) && !session.CanRetryAfterFirst, "failure does not grant rewards");
        Require(session.Retry(false) && session.Run.Roster.Count == 4, "failure can restore original state");
    }

    private static string SaveFingerprint() => string.Join("|", new[] { "meta.json", "settings.json", "active_run.json" }.Select(name =>
    {
        var path = ProjectSettings.GlobalizePath("user://" + name);
        return File.Exists(path) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) : "absent";
    }));
    private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
}
