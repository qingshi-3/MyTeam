using System;
using TowerAutobattler.Attributes;
using TowerAutobattler.Domain;

namespace TowerAutobattler.Battle;

/// <summary>Battle-local mana accounting. No timers, scene lookups or shared mutable resources.</summary>
public static class BattleHeroMana
{
    public static void Initialize(BattleUnitState unit)
    {
        unit.CurrentMana = Math.Clamp(unit.Attributes.GetValue(CombatAttribute.StartingMana), 0, unit.MaxMana);
        unit.ManaLockedUntilTick = 0;
    }

    public static void Advance(BattleUnitState unit, int tick)
    {
        unit.CurrentMana = Math.Clamp(unit.CurrentMana, 0, unit.MaxMana);
        Gain(unit, unit.Attributes.GetValue(CombatAttribute.ManaPerSecond) * BattleTiming.TickSeconds, tick);
    }

    public static void OnAttack(BattleUnitState unit, int tick) =>
        Gain(unit, unit.Attributes.GetValue(CombatAttribute.ManaPerAttack), tick);

    public static void OnDamage(BattleUnitState unit, float effectiveDamage, int tick)
    {
        if (!float.IsFinite(effectiveDamage) || effectiveDamage <= 0 || unit.MaxHealth <= 0) return;
        Gain(unit, Math.Min(unit.Attributes.GetValue(CombatAttribute.ManaPerHitCap),
            effectiveDamage / unit.MaxHealth * unit.Attributes.GetValue(CombatAttribute.ManaPerDamageRatio)), tick);
    }

    public static bool IsReady(BattleUnitState unit, int tick) =>
        unit.Alive && unit.MaxMana > 0 && tick >= unit.ManaLockedUntilTick &&
        unit.CurrentMana + .0001f >= unit.MaxMana;

    // Called only inside the successful ability transaction. The enclosing checkpoint owns rollback.
    public static float Spend(BattleUnitState unit, int tick, int recoveryTicks)
    {
        var spent = unit.CurrentMana;
        unit.CurrentMana = 0;
        unit.ManaLockedUntilTick = tick + Math.Max(1, recoveryTicks);
        return spent;
    }

    private static void Gain(BattleUnitState unit, float amount, int tick)
    {
        if (!unit.Alive || unit.MaxMana <= 0 || tick < unit.ManaLockedUntilTick ||
            !float.IsFinite(amount) || amount <= 0) return;
        unit.CurrentMana = Math.Min(unit.MaxMana, unit.CurrentMana + amount);
    }
}
