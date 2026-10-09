using System;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Domain;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

public sealed record ChargedLineState(CompiledChargedLineOperation Operation, CombatSourceRef Origin,
    Vector2 Start, Vector2 End, int StartTick, int Team);
public sealed record BattleLineCue(string ChargeVfx, string ReleaseVfx, int StartTick, int ChargeTicks,
    float Radius, ChargedLineDelivery Delivery, float ReleaseProgress = .8f);

public sealed partial class BattleSimulation
{
    private readonly BattleChargedLineScope _chargedLines;
    private bool CanAimChargedLine(BattleUnitState owner, BattleUnitState target, CompiledChargedLineOperation op) => _chargedLines.CanAimChargedLine(owner, target, op);
    private void BeginChargedLine(BattleUnitState owner, BattleUnitState target, CompiledChargedLineOperation op, CompiledAbilityDefinition ability) => _chargedLines.BeginChargedLine(owner, target, op, ability);
    private bool AdvanceChargedLine(BattleUnitState owner) => _chargedLines.AdvanceChargedLine(owner);
    private void CancelChargedLine(BattleUnitState owner) => _chargedLines.CancelChargedLine(owner);
    private bool ShouldHoldForChargedLine(BattleUnitState owner) => _chargedLines.ShouldHoldForChargedLine(owner);
    private static float? LineHitTime(Vector2 start, Vector2 delta, float radius, BattleUnitState target) => BattleCollisionQueries.LineHitTime(start, delta, radius, target);
    private void ResolveLineSkillHit(BattleUnitState source, BattleUnitState target, float damage,
        CombatSourceRef origin, Effects.EffectDamageType damageType, bool isAreaDamage = false)
    {
        var applied = ApplyDamage(source.RuntimeId, source, target, damage, origin, damageType, isAreaDamage:isAreaDamage);
        RegisterGrowingHit(source, target);
        PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.SkillHitLanded, origin,
            source.RuntimeId, target.RuntimeId, TickIndex, RequestedValue: damage, AppliedValue: applied,
            EffectiveValue: applied, DamageType: damageType, DamageClass:DamageClassFor(origin), ActionId:origin.InstanceId, IsAreaDamage:isAreaDamage));
    }

}
