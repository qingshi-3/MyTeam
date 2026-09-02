using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Equipment;

public enum EquipmentReactiveStatusTarget
{
    Owner,
    EventTarget
}

public enum EquipmentReactiveStatusSource
{
    EquipmentInstance,
    Owner
}

[GlobalClass]
public partial class EquipmentReactiveStatusBindingSpec : Resource
{
    [Export] public BattleCombatEventKind EventKind { get; set; }
    [Export] public EquipmentReactiveStatusTarget Target { get; set; }
    [Export] public EquipmentReactiveStatusSource Source { get; set; }
    [Export] public int Priority { get; set; }
    [Export] public StatusDefinition? Status { get; set; }
}
