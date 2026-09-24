using TowerAutobattler.Effects;

namespace TowerAutobattler.Abilities;

public sealed record CompiledChargedLineOperation(ChargedLineDelivery Delivery, EffectDamageType DamageType,
    float Range, float Radius, int ChargeTicks, float AttackMultiplier, int MaximumHits,
    float SubsequentHitMultiplier, float ProjectileSpeed, bool HoldPosition, string ChargeVfx,
    string ReleaseVfx) : CompiledAbilityOperation;
