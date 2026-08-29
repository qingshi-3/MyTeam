using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Components;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

public static class BattleSetupFactory
{
    public sealed record ItemBindingSpec(CatalogEntry Entry, ItemInstanceState State);

    public static UnitSnapshot Snapshot(UnitDefinition definition, UnitBehaviorComponent? behavior = null) => new(
        definition.Id, definition.DisplayName, definition.Role, definition.IsHero, definition.Role == UnitRole.Boss,
        definition.MaxHealth, definition.AttackDamage, definition.AttackRange,
        Math.Max(1, Mathf.RoundToInt(definition.AttackCooldown / BattleSimulation.TickSeconds)),
        Math.Max(1, Mathf.RoundToInt(definition.MoveInterval / BattleSimulation.TickSeconds)),
        definition.Armor, definition.HealPower, definition.SplashRadius, definition.LifeSteal,
        definition.Tags.Select(tag => tag.ToString()).ToArray(),
        behavior is null ? new UnitBehaviorSnapshot() : new UnitBehaviorSnapshot(
            behavior.SlowOnHitTicks, behavior.AdjacentArmorAura, behavior.AdjacentDamageAura,
            behavior.ExecuteHealthThreshold, behavior.LowHealthDamageBonus, behavior.OnDeathDamage, behavior.PiercingLine,
            behavior.PeriodicShieldTicks, behavior.PeriodicShieldAmount, behavior.PeriodicSummonTicks, behavior.PeriodicSummonLimit,
            behavior.PreferBacklineTargets, behavior.SummonContentId));

    public static UnitSnapshot Snapshot(CatalogEntry entry)
    {
        var root = entry.Scene.Instantiate<UnitContentRoot>();
        try { return Snapshot((UnitDefinition)entry.Definition, root.Behavior); }
        finally { root.Free(); }
    }

    public static HeroRuleSnapshot Snapshot(HeroRuleComponent component, HeroCommandContentRoot command) => new(
        command.DisplayName, command.Description, component.MaxMana, command.ManaCost, command.GoldCost, command.CreateRuntime(), component.SoldierHealthMultiplier,
        component.SoldierDamageMultiplier, component.HeroDamageMultiplier,
        component.EmptySlotHeroBonus, component.EmptySlotHeroDefense, component.EmptySlotStartShield, component.PreferBossTargets,
        component.RequiredSoldierTag.ToString(), component.TaggedSoldierHealthMultiplier,
        component.TaggedSoldierDamageMultiplier, component.FormationArmorBonus, component.FormationDamageBonus,
        component.KillGrowth, component.HeroLifeStealBonus, component.SummonOnAllyDeath, component.AddBattleConstruct,
        component.BattleGoldBonus, component.RecruitConversionGold, component.SummonContentId);

    public static ModifierSnapshot AggregateItems(IEnumerable<ItemBindingSpec> itemBindings)
    {
        var bindings = itemBindings.ToArray();
        var registry = new ModifierAccumulatorRegistry(bindings.ToDictionary(binding => binding.State.InstanceId, binding => binding.State, StringComparer.Ordinal));
        var activeRoots = new List<ItemContentRoot>();
        try
        {
            foreach (var binding in bindings)
            {
                var root = binding.Entry.Scene.Instantiate<ItemContentRoot>();
                activeRoots.Add(root);
                root.Bind(binding.State);
                root.Activate(new ItemBindingContext(registry));
            }

            float armyHp = 1, armyDamage = 1, heroHp = 1, heroDamage = 1, armyLifeSteal = 0, heroLifeSteal = 0;
            var shield = 0; var empty = 0; var summon = false; var gold = 0; float formationArmor = 0, formationDamage = 1; var summonContentId = string.Empty;
            foreach (var registered in registry.Active)
            {
                var modifier = registered.Provider;
                var stacks = Math.Max(1, registered.Stacks);
                armyHp *= MathF.Pow(modifier.ArmyHealthMultiplier, stacks); armyDamage *= MathF.Pow(modifier.ArmyDamageMultiplier, stacks);
                heroHp *= MathF.Pow(modifier.HeroHealthMultiplier, stacks); heroDamage *= MathF.Pow(modifier.HeroDamageMultiplier, stacks);
                armyLifeSteal += modifier.ArmyLifeStealBonus * stacks; heroLifeSteal += modifier.HeroLifeStealBonus * stacks;
                shield += modifier.StartBattleShield * stacks; empty += modifier.EmptySlotPower * stacks;
                summon |= modifier.SummonToken; gold += modifier.GoldPerBattle * stacks;
                formationArmor += modifier.FormationAdjacentArmor * stacks;
                formationDamage *= MathF.Pow(modifier.FormationAdjacentDamageMultiplier, stacks);
                if (!string.IsNullOrWhiteSpace(modifier.SummonContentId)) summonContentId = modifier.SummonContentId;
            }
            return new ModifierSnapshot(armyHp, armyDamage, heroHp, heroDamage, armyLifeSteal, heroLifeSteal, shield, empty, summon, gold, formationArmor, formationDamage, summonContentId);
        }
        finally
        {
            foreach (var root in activeRoots)
            {
                root.Deactivate();
                root.Free();
            }
        }
    }

    private sealed class ModifierAccumulatorRegistry(IReadOnlyDictionary<string, ItemInstanceState> states) : IRunModifierRegistry
    {
        private readonly List<RegisteredModifier> _active = [];
        public IReadOnlyList<RegisteredModifier> Active => _active;

        public IDisposable Register(string itemInstanceId, RunModifierProviderComponent provider)
        {
            var stacks = states.TryGetValue(itemInstanceId, out var state) ? state.Stacks : 1;
            var registration = new RegisteredModifier(itemInstanceId, provider, stacks);
            _active.Add(registration);
            return new Registration(() => _active.Remove(registration));
        }
    }

    private sealed record RegisteredModifier(string InstanceId, RunModifierProviderComponent Provider, int Stacks);

    private sealed class Registration(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() { _dispose?.Invoke(); _dispose = null; }
    }
}
