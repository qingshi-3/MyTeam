using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Abilities;

public enum BattleValueMetric { Attribute, Health, MissingHealth, Shield, HealthRatio, Counter, EventEffective, EventOverheal, StatusStacks, HarmfulKinds, BattleSeconds, AttackActions, OwnedSummons, IntrinsicArmor, EventOrdinal = 15, IsTemporary = 16 }
public enum BattleValueSubject { Owner, Target, EventSource, EventTarget }

// A bounded sum of typed battle reads; never contains mutable runtime state.
[GlobalClass]
public partial class BattleValueTermSpec : Resource
{
    [Export] public BattleValueMetric Metric { get; set; }
    [Export] public BattleValueSubject Subject { get; set; }
    [Export] public CombatAttribute Attribute { get; set; }
    [Export] public string Key { get; set; } = "";
    [Export] public float Scale { get; set; } = 1;
    [Export] public bool TeamShared { get; set; }
    [Export] public bool OwnStatusOnly { get; set; }
}
