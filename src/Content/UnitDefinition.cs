using Godot;

namespace TowerAutobattler.Content;

public enum UnitRole { Vanguard, Fighter, Ranged, Support, Assassin, Summoner, Artillery, Boss }
public enum UnitFaction { Order, Desert, Undead, Beast, Machine, Frost, Neutral, Enemy }

[GlobalClass]
public partial class UnitDefinition : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public Texture2D? Icon { get; set; }
    [Export] public UnitPortraitDefinition? Portrait { get; set; }
    [Export] public UnitRole Role { get; set; } = UnitRole.Fighter;
    [Export] public UnitFaction Faction { get; set; } = UnitFaction.Neutral;
    [Export] public bool IsHero { get; set; }
    [Export] public bool IsEnemy { get; set; }
    [Export] public int RecruitCost { get; set; } = 5;
    [Export] public float MaxHealth { get; set; } = 100;
    [Export] public float AttackDamage { get; set; } = 15;
    [Export] public float AttackRange { get; set; } = 1.2f;
    [Export] public float AttackCooldown { get; set; } = 1.2f;
    [Export] public float MoveInterval { get; set; } = 0.45f;
    [Export] public float Armor { get; set; }
    [Export] public float HealPower { get; set; }
    [Export] public float SplashRadius { get; set; }
    [Export] public float LifeSteal { get; set; }
    [Export] public Godot.Collections.Array<StringName> Tags { get; set; } = [];
}
