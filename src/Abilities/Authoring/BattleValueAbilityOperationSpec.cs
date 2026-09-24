using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Abilities;

public enum BattleValueAction { Damage, Heal, Shield, Mana, AddCounter, SetCounter, SetAttributeContribution, Require, ConsumeShield }
public enum BattleTargetPolicy { Any, Wounded, Temporary, Corpse, NonBossEnemy, BehindAlly, EchoableAlly }

[GlobalClass]
public partial class BattleValueAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public BattleValueAction Action { get; set; }
    [Export] public EffectTargetQuerySpec? TargetQuery { get; set; }
    [Export] public BattleTargetPolicy TargetPolicy { get; set; }
    [Export] public float Amount { get; set; }
    [Export] public Godot.Collections.Array<BattleValueTermSpec> Terms { get; set; } = [];
    [Export] public EffectDamageType DamageType { get; set; }
    [Export] public CombatAttribute Attribute { get; set; }
    [Export] public string CounterKey { get; set; } = "";
    [Export] public bool TeamShared { get; set; }
    [Export] public float Minimum { get; set; } = -1e20f;
    [Export] public float Maximum { get; set; } = 1e20f;
    [Export] public int Every { get; set; } = 1;
    [Export] public bool Broadcast { get; set; }
    [Export] public string Label { get; set; } = "";
}
