using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

[GlobalClass]
public partial class HeroCommandContentRoot : Node
{
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public int ManaCost { get; set; } = 1;
    [Export] public int GoldCost { get; set; }
    public string Description => Describe();

    public virtual string Describe() => string.Empty;
    public virtual IHeroCommandRuntime CreateRuntime() => throw new InvalidOperationException("Hero command content must create a concrete runtime.");

    public virtual ValidationReport ValidateAuthoring()
    {
        var report = new ValidationReport();
        if (string.IsNullOrWhiteSpace(DisplayName)) report.Error($"{SceneFilePath}: hero command missing display name");
        if (string.IsNullOrWhiteSpace(Description)) report.Error($"{SceneFilePath}: hero command missing generated description");
        if (ManaCost <= 0) report.Error($"{SceneFilePath}: hero command mana cost must be positive");
        if (GoldCost < 0) report.Error($"{SceneFilePath}: hero command has negative gold cost");
        return report;
    }

    protected static string Percent(float ratio) => $"{ratio * 100f:0.#}%";
}

public interface IHeroCommandRuntime
{
    bool TryExecute(BattleCommandContext context);
}

public sealed class BattleCommandContext
{
    private readonly Func<BattleUnitState, BattleUnitState, float, float> _heal;
    private readonly Func<UnitSnapshot?, Vector2I, float, float, bool> _spawn;
    private readonly Func<int, bool> _spendGold;
    private readonly Action<string> _emit;
    public BattleUnitState Hero { get; }
    public IReadOnlyList<BattleUnitState> Allies { get; }
    public IReadOnlyList<BattleUnitState> Enemies { get; }
    public SummonProfiles Summons { get; }
    public string FailureReason { get; private set; } = string.Empty;

    public BattleCommandContext(
        BattleUnitState hero, IReadOnlyList<BattleUnitState> allies, IReadOnlyList<BattleUnitState> enemies,
        SummonProfiles summons, Func<BattleUnitState, BattleUnitState, float, float> heal,
        Func<UnitSnapshot?, Vector2I, float, float, bool> spawn, Func<int, bool> spendGold, Action<string> emit)
    {
        Hero = hero; Allies = allies; Enemies = enemies; Summons = summons;
        _heal = heal; _spawn = spawn; _spendGold = spendGold; _emit = emit;
    }

    public float Heal(BattleUnitState source, BattleUnitState target, float amount) => _heal(source, target, amount);

    public bool Spawn(UnitSnapshot? profile, Vector2I near, float healthScale = 1f, float damageScale = 1f)
    {
        if (_spawn(profile, near, healthScale, damageScale)) return true;
        FailureReason = "没有可用的召唤单位或合法落点。";
        return false;
    }

    public bool SpendGold(int amount)
    {
        if (_spendGold(amount)) return true;
        FailureReason = $"金币不足：需要 {amount} 金币。";
        return false;
    }
    public void Emit(string cue) => _emit(cue);
}

public sealed class RallyCommandRuntime(float shieldAmount, int attackCooldownCapTicks) : IHeroCommandRuntime
{
    public bool TryExecute(BattleCommandContext context)
    {
        foreach (var ally in context.Allies)
        {
            ally.Shield += shieldAmount;
            ally.AttackCooldown = Math.Min(ally.AttackCooldown, attackCooldownCapTicks);
        }
        return true;
    }
}

public sealed class RaiseDeadCommandRuntime(int summonCount, float healthMultiplier, float damageMultiplier) : IHeroCommandRuntime
{
    public bool TryExecute(BattleCommandContext context)
    {
        var spawned = false;
        for (var index = 0; index < summonCount; index++)
            spawned |= context.Spawn(context.Summons.DeathSummon, context.Hero.Cell, healthMultiplier, damageMultiplier);
        return spawned;
    }
}

public sealed class BeastRoarCommandRuntime(string synergyTag, float damageMultiplier) : IHeroCommandRuntime
{
    public bool TryExecute(BattleCommandContext context)
    {
        foreach (var ally in context.Allies)
            if (ally.Definition.Tags.Contains(synergyTag)) ally.Damage *= damageMultiplier;
        return true;
    }
}

public sealed class OverclockCommandRuntime : IHeroCommandRuntime
{
    public bool TryExecute(BattleCommandContext context)
    {
        foreach (var ally in context.Allies) { ally.AttackCooldown = 0; ally.MoveCooldown = 0; }
        return true;
    }
}

public sealed class BloodRushCommandRuntime(float healRatio, float damageMultiplier) : IHeroCommandRuntime
{
    public bool TryExecute(BattleCommandContext context)
    {
        if (!context.Hero.Alive) return false;
        context.Heal(context.Hero, context.Hero, context.Hero.MaxHealth * healRatio);
        context.Hero.AttackCooldown = 0;
        context.Hero.Damage *= damageMultiplier;
        return true;
    }
}

public sealed class DuelFocusCommandRuntime(float shieldRatio) : IHeroCommandRuntime
{
    public bool TryExecute(BattleCommandContext context)
    {
        context.Hero.Shield += context.Hero.MaxHealth * shieldRatio;
        context.Hero.AttackCooldown = 0;
        return true;
    }
}

public sealed class TimeStopCommandRuntime(int disableTicks, int allyCooldownDivisor) : IHeroCommandRuntime
{
    public bool TryExecute(BattleCommandContext context)
    {
        foreach (var enemy in context.Enemies) enemy.DisabledTicks = Math.Max(enemy.DisabledTicks, disableTicks);
        foreach (var ally in context.Allies) ally.AttackCooldown /= allyCooldownDivisor;
        return true;
    }
}

public sealed class PaidReinforcementCommandRuntime(int goldCost, float healthMultiplier, float damageMultiplier) : IHeroCommandRuntime
{
    public bool TryExecute(BattleCommandContext context) =>
        context.SpendGold(goldCost) && context.Spawn(context.Summons.Mercenary, context.Hero.Cell, healthMultiplier, damageMultiplier);
}
