using System.Collections.Immutable;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Battle;

// A cause known at the authored command site, carried only to report accumulation.
// It never changes calculation/event provenance or the gameplay damage class.
internal readonly record struct BattleDamageStatisticsAttribution(CombatSourceRef Origin, CombatDamageClass DamageClass)
{
    // Reports need the stable cause, not a new per-hit source instance string.
    public BattleDamageStatisticsAttribution(CombatSourceKind kind, string stableId, CombatDamageClass damageClass)
        : this(new(kind, stableId, string.Empty, string.Empty), damageClass) { }
    public bool IsSpecified => Origin.IsSpecified;
}

// DamageDealt/DamageTaken on the legacy report include shields. These fields separate
// post-resistance HP loss, shield absorption and damage beyond remaining HP.
public sealed record BattleDamageSourceSnapshot(
    CombatSourceKind SourceKind, string StableId, CombatDamageClass DamageClass,
    EffectDamageType DamageType, float HealthDamage, float ShieldDamage, float OverkillDamage,
    int Hits, bool IsOverflow = false);

public sealed record BattleHealingSourceSnapshot(
    CombatSourceKind SourceKind, string StableId, float EffectiveHealing, float WastedHealing,
    int Events, bool IsOverflow = false);

// Active occupancy at each authoritative owner action step. Rows can overlap;
// source credit is not a measurement of marginal extra duration or prevented attacks.
public sealed record BattleControlSourceSnapshot(
    string StableId, string DisplayName, string SourceRuntimeId, int ActiveTicks,
    bool IsOverflow = false);

public sealed record BattleUnitDetailedStatisticsSnapshot(
    float HealthDamageDealt, float HealthDamageTaken, float ShieldDamageDealt,
    float OverkillDamageDealt, float HealingWasted, float HealingReceived, float ShieldGranted,
    int LandedAttacks, int DodgedAttacks, int CriticalHits, int AbilityResolutions, int ManaCasts,
    int AliveTicks, int HardControlTicksTaken, int HardControlTicksDealt, int LegacyActionLockTicks,
    ImmutableArray<BattleDamageSourceSnapshot> DamageSources,
    ImmutableArray<BattleHealingSourceSnapshot> HealingSources,
    ImmutableArray<BattleControlSourceSnapshot> ControlSources);
