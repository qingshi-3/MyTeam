using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Content;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

// Stable, live view of one owned unit. No mutable unit or AttributeSet can be
// recovered through this API, and reading it does not clone per-frame state.
public sealed class BattleUnitView
{
    private static readonly ConditionalWeakTable<BattleUnitState, BattleUnitView> Views = new();
    private readonly BattleUnitState _state;
    private BattleUnitView(BattleUnitState state) { _state = state; Attributes = new(state.Attributes); }
    internal static BattleUnitView For(BattleUnitState state) => Views.GetValue(state, static value => new(value));
    public string RuntimeId => _state.RuntimeId;
    public string SourceInstanceId => _state.SourceInstanceId;
    public string SummonerRuntimeId => _state.SummonerRuntimeId;
    public bool HasManaSkill => _state.HasManaSkill;
    public UnitSnapshot Definition => _state.Definition;
    public BattleAttributeView Attributes { get; }
    public int Team => _state.Team;
    public Vector2I Cell => _state.Cell;
    public Vector2 Position => _state.Position;
    public AttackDelivery AttackDelivery => _state.AttackDelivery;
    public string ProjectileVisualId => _state.ProjectileVisualId;
    public float BodyRadius => _state.BodyRadius;
    public float Health => _state.Health;
    public float MaxHealth => _state.MaxHealth;
    public float CurrentMana => _state.CurrentMana;
    public float MaxMana => _state.MaxMana;
    public float Damage => _state.Damage;
    public float Armor => _state.Armor;
    public float Shield => _state.Shield;
    public float LifeSteal => _state.LifeSteal;
    public float HealingPower => _state.HealingPower;
    public int EffectiveAttackTicks => _state.EffectiveAttackTicks;
    public int EffectiveMoveTicks => _state.EffectiveMoveTicks;
    public int AttackCooldown => _state.AttackCooldown;
    public int MoveCooldown => _state.MoveCooldown;
    public int InitialTeam => _state.InitialTeam;
    public int DisabledTicks => _state.DisabledTicks;
    public long AttackHitStacks => _state.AttackHitStacks;
    public bool IsTemporary => _state.IsTemporary;
    public bool IsPersistentRosterHero => _state.IsPersistentRosterHero;
    public bool Alive => _state.Alive;
    public BattleUnitMode Mode => _state.Mode;
    public BattleActionKind LastActionKind => _state.LastActionKind;
    public string LastAbilityName => _state.LastAbilityName;
    public string ActionTargetRuntimeId => _state.ActionTargetRuntimeId;
    public string ActionTargetName => _state.ActionTargetName;
    public ImmutableArray<StatusRuntimeSnapshot> Statuses => _state.Statuses;
}

public sealed class BattleAttributeView
{
    private readonly BattleAttributeSet _attributes;
    internal BattleAttributeView(BattleAttributeSet attributes) => _attributes = attributes;
    public float GetValue(CombatAttribute attribute) => _attributes.GetValue(attribute);
    public float GetBaseValue(CombatAttribute attribute) => _attributes.GetBaseValue(attribute);
}
