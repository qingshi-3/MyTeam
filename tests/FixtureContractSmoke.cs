using System;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Components;

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

            var registry = new FixtureModifierRegistry();
            item.Bind(new ItemInstanceState { InstanceId = "fixture-item-instance", Charges = 2 });
            item.Activate(new ItemBindingContext(registry));
            if (registry.Active != 2) throw new InvalidOperationException("multi-provider registration");
            try { item.Bind(new ItemInstanceState { InstanceId = "illegal-rebind" }); throw new InvalidOperationException("active rebind accepted"); }
            catch (InvalidOperationException exception) when (exception.Message != "active rebind accepted") { }
            item.Deactivate();
            if (registry.Active != 0) throw new InvalidOperationException("item unregistration");
            item.Bind(new ItemInstanceState { InstanceId = "rollback-instance" });
            var throwing = new ThrowingModifierRegistry();
            try { item.Activate(new ItemBindingContext(throwing)); throw new InvalidOperationException("partial registration accepted"); }
            catch (InvalidOperationException exception) when (exception.Message != "partial registration accepted") { }
            if (throwing.Active != 0 || item.LifecycleState != ContentLifecycleState.Bound)
                throw new InvalidOperationException("partial registration rollback");
            GD.Print("FIXTURE_CONTRACT_OK");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr($"FIXTURE_CONTRACT_FAILED: {exception}");
            return 1;
        }
    }

    private sealed class FixtureModifierRegistry : IRunModifierRegistry
    {
        public int Active { get; private set; }
        public IDisposable Register(string itemInstanceId, RunModifierProviderComponent provider)
        {
            Active++;
            return new Registration(() => Active--);
        }
    }

    private sealed class Registration(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() { _dispose?.Invoke(); _dispose = null; }
    }

    private sealed class ThrowingModifierRegistry : IRunModifierRegistry
    {
        public int Active { get; private set; }
        private int _calls;
        public IDisposable Register(string itemInstanceId, RunModifierProviderComponent provider)
        {
            if (++_calls == 2) throw new InvalidOperationException("intentional second-provider failure");
            Active++;
            return new Registration(() => Active--);
        }
    }
}
