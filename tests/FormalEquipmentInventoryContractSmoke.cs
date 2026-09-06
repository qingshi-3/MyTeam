using System;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Run;

// In-memory Run commands only: no combat stepping, GUI automation or player save path.
public partial class FormalEquipmentInventoryContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = 0;
        try
        {
            var gate = await TestProjectFixture.PublishAsync(this);
            var package = gate.Package ?? throw new InvalidOperationException(string.Join("; ", gate.Report.CoreErrors));
            VerifyInventory(package);
            VerifyVersionMigration(package);
            GD.Print("FORMAL_EQUIPMENT_INVENTORY_OK identity=preserved replacement=returned transfer=atomic save-failure=unchanged schema=v5 battle=not-run");
        }
        catch (Exception exception) { exitCode = 1; GD.PrintErr("FORMAL_EQUIPMENT_INVENTORY_FAILED: " + exception); }
        GetTree().Quit(exitCode);
    }

    private static void VerifyInventory(CompiledGamePackage package)
    {
        var save = new MemorySave();
        var app = new RunApplication(package.Content, save, package.Project);
        Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 3821), "create memory run");
        var run = app.ActiveRun!;
        var ownerA = run.Roster[0].InstanceId;
        var ownerB = run.Roster[1].InstanceId;
        Require(app.GrantItem("equipment_rimebrand") && app.GrantItem("equipment_vanguard_insignia"), "equipment rewards enter inventory");
        var bladeId = run.EquipmentInventory[0].InstanceId;
        var armorId = run.EquipmentInventory[1].InstanceId;
        Require(app.EquipOwnedItem(bladeId, ownerA, 0), "equip owned blade");
        Require(app.EquipOwnedItem(armorId, ownerA, 0), "replace blade with armor");
        Require(run.EquipmentInventory.Single().InstanceId == bladeId &&
                run.EquipmentInventory.Single().OwnerHeroInstanceId == "" &&
                run.EquipmentInventory.Single().SlotIndex == -1, "replaced item retains identity in bag");
        Require(app.EquipOwnedItem(armorId, ownerB, 2), "transfer worn armor to another hero's third slot");
        Require(run.Roster[0].Equipment.Count == 0 && run.Roster[1].Equipment.Single().InstanceId == armorId, "one owner after transfer");
        Require(app.RemoveEquipment(ownerB, 2) && run.EquipmentInventory.Count == 2, "unequip loses nothing");
        Require(!app.EquipOwnedItem("missing", ownerA, 0) && !app.EquipOwnedItem(bladeId, ownerA, 3), "invalid ids and fourth slot reject");

        var before = JsonSerializer.Serialize(run);
        var beforeStored = save.RunJson;
        save.FailPublication = true;
        Require(!app.EquipOwnedItem(bladeId, ownerA, 0) && JsonSerializer.Serialize(run) == before && save.RunJson == beforeStored,
            "failed equip publishes neither memory nor saved state");
        Require(!app.GrantItem("equipment_field_focus") && JsonSerializer.Serialize(run) == before, "failed reward creates no orphan");
        save.FailPublication = false;
        run.Gold = 100;
        save.SaveActiveRun(run);
        before = JsonSerializer.Serialize(run);
        save.FailPublication = true;
        Require(!app.BuyItem("equipment_field_focus") && JsonSerializer.Serialize(run) == before, "failed purchase refunds gold and inventory atomically");
        save.FailPublication = false;

        app.SetEquipmentEditingLocked(true);
        Require(!app.EquipOwnedItem(bladeId, ownerA, 0) && !app.GrantItem("equipment_field_focus") &&
                !app.BuyItem("equipment_field_focus") && !app.EquipItem(ownerA, 0, "equipment_field_focus"), "battle lock covers equipment mutations");
        app.SetEquipmentEditingLocked(false);
        Require(app.EquipOwnedItem(bladeId, ownerA, 0), "unlock permits preparation");
        Require(app.EquipOwnedItem(armorId, ownerB, 0), "replacement rollback setup");
        before = JsonSerializer.Serialize(run);
        beforeStored = save.RunJson;
        save.FailPublication = true;
        Require(!app.EquipOwnedItem(bladeId, ownerB, 0) && !app.RemoveEquipment(ownerA, 0) &&
                JsonSerializer.Serialize(run) == before && save.RunJson == beforeStored,
            "failed cross-owner replacement and unequip retain both original owners");
        save.FailPublication = false;
        Require(app.RemoveEquipment(ownerB, 0), "return armor before reload");
        var reloaded = new RunApplication(package.Content, save, package.Project).ActiveRun!;
        Require(reloaded.Roster[0].Equipment.Single().InstanceId == bladeId &&
                reloaded.EquipmentInventory.Single().InstanceId == armorId, "save reload keeps both locations");
        var all = reloaded.EquipmentInventory.Concat(reloaded.Roster.SelectMany(hero => hero.Equipment)).ToArray();
        Require(all.Select(item => item.InstanceId).Distinct().Count() == 2, "no duplicate durable identity");
        var invalid = JsonSerializer.Deserialize<ActiveRunDto>(JsonSerializer.Serialize(reloaded))!;
        invalid.EquipmentInventory.Add(invalid.Roster[0].Equipment[0]);
        Require(!ActiveRunConfigurationValidator.Validate(invalid, package.Content, package.Project), "duplicate equipped/backpack ownership rejected");
    }

    private static void VerifyVersionMigration(CompiledGamePackage package)
    {
        var source = new MemorySave();
        var app = new RunApplication(package.Content, source, package.Project);
        Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 923), "migration fixture");
        var legacy = app.ActiveRun!;
        legacy.Version = 4;
        source.SaveActiveRun(legacy);
        var oldJson = source.RunJson;
        source.FailPublication = true;
        var rejected = new RunApplication(package.Content, source, package.Project);
        Require(rejected.ActiveRun is null && rejected.ActiveRunLoadDiagnostic?.Kind == ActiveRunLoadFailureKind.MigrationPublicationFailed &&
                source.RunJson == oldJson, "failed v4 migration retains original");
        source.FailPublication = false;
        var migrated = new RunApplication(package.Content, source, package.Project).ActiveRun;
        Require(migrated is { Version: ActiveRunFormationSchema.CurrentVersion, EquipmentInventory.Count: 0 } &&
                migrated.Roster[0].InstanceId == legacy.Roster[0].InstanceId, "v4 migrates without inventing items");
        var future = JsonSerializer.Deserialize<ActiveRunDto>(source.RunJson!)!;
        future.Version = 99;
        source.SaveActiveRun(future);
        oldJson = source.RunJson;
        Require(new RunApplication(package.Content, source, package.Project).ActiveRun is null && source.RunJson == oldJson,
            "future schema rejected without overwriting source");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }

    private sealed class MemorySave : IRunSaveService
    {
        public string? RunJson { get; private set; }
        public bool FailPublication { get; set; }
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => RunJson is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(RunJson);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value)
        {
            if (FailPublication) return false;
            RunJson = JsonSerializer.Serialize(value);
            return true;
        }
        public void DeleteActiveRun() => RunJson = null;
    }
}
