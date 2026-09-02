using System;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Relics;

public partial class FixtureContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private async Task<int> RunAsync()
    {
        try
        {
            var unitEntry = GD.Load<CatalogEntry>("res://tests/fixtures/fixture_unit_entry.tres");
            var itemEntry = GD.Load<CatalogEntry>("res://tests/fixtures/fixture_item_entry.tres");
            if (unitEntry?.Scene.Instantiate() is not UnitContentRoot unit) throw new InvalidOperationException("unit fixture root");
            if (itemEntry?.Scene.Instantiate() is not ItemContentRoot item) throw new InvalidOperationException("item fixture root");
            AddChild(unit);
            AddChild(item);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (unit.ValidateAuthoring().HasCoreErrors || item.ValidateAuthoring().HasCoreErrors) throw new InvalidOperationException("authoring validation");
            if (!ReferenceEquals(unit.Definition, unitEntry.Definition) || !ReferenceEquals(item.Definition, itemEntry.Definition)) throw new InvalidOperationException("definition identity");
            var relicCompilation = RelicDefinitionCompiler.Compile(item.Relic);
            var relicDefinition = relicCompilation.Definition ?? throw new InvalidOperationException(
                "fixture relic compile: " + string.Join("; ", relicCompilation.Report.CoreErrors));

            using var relics = new RelicRunScope(new RelicRunKey(7UL, "fixture_hero", 0, 0));
            item.Bind(new ItemInstanceState
            {
                InstanceId = "fixture-item-instance",
                ContentId = "fixture_item",
                Stacks = 2,
                Charges = 2
            });
            item.Activate(new ItemBindingContext(relics, relicDefinition));
            if (relics.LiveRunInstanceCount != 1) throw new InvalidOperationException("relic registration");
            var preparation = relics.PrepareBattle();
            if (Math.Abs(preparation.Modifiers.ArmyDamageMultiplier - 1.21f) > .001f ||
                preparation.Modifiers.StartBattleShield != 10)
                throw new InvalidOperationException("stacked authored relic projection");
            try { item.Bind(new ItemInstanceState { InstanceId = "illegal-rebind" }); throw new InvalidOperationException("active rebind accepted"); }
            catch (InvalidOperationException exception) when (exception.Message != "active rebind accepted") { }
            item.Deactivate();
            if (relics.LiveRunInstanceCount != 0) throw new InvalidOperationException("item unregistration");
            using var rollbackRelics = new RelicRunScope(new RelicRunKey(8UL, "fixture_hero", 0, 0));
            item.Bind(new ItemInstanceState
            {
                InstanceId = "rollback-instance",
                ContentId = "wrong_content"
            });
            try { item.Activate(new ItemBindingContext(rollbackRelics, relicDefinition)); throw new InvalidOperationException("invalid relic state accepted"); }
            catch (ArgumentException exception) when (exception.Message != "invalid relic state accepted") { }
            if (rollbackRelics.LiveRunInstanceCount != 0 || item.LifecycleState != ContentLifecycleState.Bound)
                throw new InvalidOperationException("relic activation rollback");
            GD.Print("FIXTURE_CONTRACT_OK relic=typed,lifecycle-zero");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr($"FIXTURE_CONTRACT_FAILED: {exception}");
            return 1;
        }
    }

}
