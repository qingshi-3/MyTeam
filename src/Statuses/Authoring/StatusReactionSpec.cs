using Godot;
using TowerAutobattler.Battle;
namespace TowerAutobattler.Statuses;

public enum StatusReactionTarget { Owner, Counterpart, EnemiesNearCounterpart }
[GlobalClass]
public partial class StatusReactionSpec : Resource
{
    [Export] public BattleCombatEventKind EventKind { get; set; }
    [Export] public StatusReactiveOwnerRole OwnerRole { get; set; }
    [Export] public StatusReactionTarget Target { get; set; }
    [Export] public float Radius { get; set; }
    [Export] public StatusDefinition Status { get; set; } = null!;
}
