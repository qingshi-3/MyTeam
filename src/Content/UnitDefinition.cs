using Godot;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Content;

public enum UnitRole { Vanguard, Fighter, Ranged, Support, Assassin, Summoner, Artillery, Boss }
public enum UnitFaction { Order, Desert, Undead, Beast, Machine, Frost, Neutral, Enemy }
public enum AttackDelivery { Melee, Projectile, Beam }

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
    [Export] public bool IsTestDummy { get; set; }
    [Export] public int RecruitCost { get; set; } = 5;
    [Export] public float MaxHealth { get; set; } = 100;
    [Export] public float AttackDamage { get; set; } = 15;
    // Extra reach outside both bodies; the short default is suitable for melee contact.
    [Export] public float AttackRange { get; set; } = .25f;
    [Export] public AttackDelivery AttackDelivery { get; set; } = AttackDelivery.Melee;
    // Logical battlefield units per second; independent of rendering and playback speed.
    [Export] public float ProjectileSpeed { get; set; } = 8f;
    [Export] public float ProjectileRadius { get; set; } = .07f;
    [Export] public float ProjectileLifetime { get; set; } = 3f;
    // Base-speed windup and the matching normalized release point in the attack clip.
    [Export] public float ProjectileWindupSeconds { get; set; }
    [Export(PropertyHint.Range, "0.05,1,0.01")] public float AttackReleaseProgress { get; set; } = .8f;
    [Export] public float AttackCooldown { get; set; } = 1.2f;
    [Export] public float MoveInterval { get; set; } = 0.45f;
    [Export(PropertyHint.Range, "0.1,1.5,0.01")] public float BodyRadius { get; set; } = .32f;
    [Export] public float Armor { get; set; }
    [Export] public float SpellPower { get; set; }
    [Export] public float HealPower { get; set; }
    [Export] public float SplashRadius { get; set; }
    [Export] public float LifeSteal { get; set; }
    [Export(PropertyHint.Range, "0,1,0.01")] public float BaseControlResistance { get; set; }
    [ExportGroup("法力与自动施法")]
    [Export] public float MaxMana { get; set; }
    [Export] public float StartingMana { get; set; }
    [Export] public float ManaPerSecond { get; set; } = 5;
    [Export] public float ManaPerAttack { get; set; } = 10;
    // Effective health + shield damage divided by maximum health, capped per damage event.
    [Export] public float ManaPerDamageRatio { get; set; } = 50;
    [Export] public float ManaPerHitCap { get; set; } = 20;
    [ExportGroup("")]
    [Export] public TraitContributionSpec[] TraitContributions { get; set; } = [];
    [Export] public Godot.Collections.Array<StringName> Tags { get; set; } = [];
}
