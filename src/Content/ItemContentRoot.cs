using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Components;

namespace TowerAutobattler.Content;

[GlobalClass]
public partial class ItemContentRoot : Node
{
    [Export] public ItemDefinition Definition { get; set; } = null!;
    public IReadOnlyList<RunModifierProviderComponent> ModifierProviders => GetChildren().OfType<RunModifierProviderComponent>().ToArray();
    public RunModifierProviderComponent? ModifierProvider => ModifierProviders.FirstOrDefault();
    public ItemInstanceState? InstanceState { get; private set; }
    public ContentLifecycleState LifecycleState { get; private set; }

    private readonly List<IDisposable> _registrations = [];

    public ValidationReport ValidateAuthoring()
    {
        var report = new ValidationReport();
        if (Definition is null) report.Error($"{SceneFilePath}: missing ItemDefinition");
        else if (string.IsNullOrWhiteSpace(Definition.Id)) report.Error($"{SceneFilePath}: empty stable id");
        if (ModifierProviders.Count == 0) report.Error($"{SceneFilePath}: missing modifier provider");
        return report;
    }

    public void Bind(ItemInstanceState state)
    {
        if (LifecycleState == ContentLifecycleState.Active) throw new InvalidOperationException("Active item cannot be rebound.");
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrWhiteSpace(state.InstanceId)) throw new ArgumentException("Item instance id is required.", nameof(state));
        InstanceState = state;
        LifecycleState = ContentLifecycleState.Bound;
    }

    public void Activate(ItemBindingContext context)
    {
        if (LifecycleState != ContentLifecycleState.Bound || InstanceState is null)
            throw new InvalidOperationException("Item must be bound before activation.");
        try
        {
            foreach (var provider in ModifierProviders)
                _registrations.Add(context.Modifiers.Register(InstanceState.InstanceId, provider));
            LifecycleState = ContentLifecycleState.Active;
        }
        catch
        {
            Deactivate();
            throw;
        }
    }

    public void Deactivate()
    {
        if (LifecycleState == ContentLifecycleState.Active) LifecycleState = ContentLifecycleState.Bound;
        var registrations = _registrations.ToArray();
        _registrations.Clear();
        foreach (var registration in registrations)
            try { registration.Dispose(); }
            catch (Exception exception) { GD.PushError($"Item unregister failed: {exception.Message}"); }
    }

    public override void _ExitTree()
    {
        Deactivate();
        InstanceState = null;
        LifecycleState = ContentLifecycleState.Unbound;
    }
}
