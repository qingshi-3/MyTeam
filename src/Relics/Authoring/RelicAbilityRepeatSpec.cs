using Godot;
using TowerAutobattler.Abilities;
namespace TowerAutobattler.Relics;
[GlobalClass]
public partial class RelicAbilityRepeatSpec : Resource
{
    [Export] public AbilityTriggerKind Trigger { get; set; } = AbilityTriggerKind.OwnerEnteredBattle;
    [Export] public int ExtraExecutions { get; set; } = 1;
}
