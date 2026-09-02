using System;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;

namespace TowerAutobattler.Content;

[GlobalClass]
public partial class ItemContentRoot : Node
{
    [Export] public ItemDefinition Definition { get; set; } = null!;
    [Export] public RelicDefinition? Relic { get; set; }
    [Export] public EquipmentDefinition? Equipment { get; set; }
    public ItemInstanceState? InstanceState { get; private set; }
    public ContentLifecycleState LifecycleState { get; private set; }

    private readonly List<IDisposable> _registrations = [];

    public ValidationReport ValidateAuthoring()
    {
        var report = new ValidationReport();
        if (Definition is null) report.Error($"{SceneFilePath}: missing ItemDefinition");
        else if (string.IsNullOrWhiteSpace(Definition.Id)) report.Error($"{SceneFilePath}: empty stable id");
        if (Definition is not null && !Enum.IsDefined(Definition.ProductKind))
            report.Error($"{SceneFilePath}: invalid item product classification");
        if (Definition?.ProductKind == ItemProductKind.Relic)
        {
            if (Relic is null) report.Error($"{SceneFilePath}: missing relic definition");
            if (Equipment is not null) report.Error($"{SceneFilePath}: relic item cannot reference Equipment");
            if (Relic is not null && Relic.StableId != Definition.Id)
                report.Error($"{SceneFilePath}: relic stable id does not match item definition id");
        }
        else if (Definition?.ProductKind == ItemProductKind.Equipment)
        {
            if (Equipment is null) report.Error($"{SceneFilePath}: missing Equipment definition");
            if (Relic is not null) report.Error($"{SceneFilePath}: Equipment item cannot reference a Relic");
            if (Equipment is not null && Equipment.StableId != Definition.Id)
                report.Error($"{SceneFilePath}: Equipment stable id does not match item definition id");
        }
        return report;
    }

    public void Bind(ItemInstanceState state)
    {
        if (LifecycleState == ContentLifecycleState.Active) throw new InvalidOperationException("Active item cannot be rebound.");
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrWhiteSpace(state.InstanceId)) throw new ArgumentException("Item instance id is required.", nameof(state));
        if (string.IsNullOrWhiteSpace(state.ContentId) && Definition is not null) state.ContentId = Definition.Id;
        InstanceState = state;
        LifecycleState = ContentLifecycleState.Bound;
    }

    public void Activate(ItemBindingContext context)
    {
        if (LifecycleState != ContentLifecycleState.Bound || InstanceState is null)
            throw new InvalidOperationException("Item must be bound before activation.");
        try
        {
            if (Definition is null || Definition.ProductKind != ItemProductKind.Relic || Relic is null ||
                context.Definition.StableId != Definition.Id ||
                context.Definition.StableId != Relic.StableId)
                throw new InvalidOperationException("Published relic definition does not match the item scene identity.");
            _registrations.Add(context.Relics.Activate(context.Definition, InstanceState));
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
