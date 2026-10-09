using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

internal interface IBattleTraitQueries
{
    int Tick { get; }
    IReadOnlyList<BattleUnitView> Units { get; }
    IReadOnlyDictionary<string, UnitSnapshot> SummonProfiles { get; }
    bool ManaLocked(string id);
}
internal interface IBattleTraitCommands
{
    void Damage(string source, string target, float amount, CombatSourceRef origin,
        EffectDamageType damageType = EffectDamageType.Normal, CombatDamageClass damageClass = CombatDamageClass.Other,
        bool isAreaDamage = false);
    void Heal(string source, string target, float amount, CombatSourceRef origin);
    float Shield(string source, string target, float amount, CombatSourceRef origin);
    void GainMana(string target, float amount);
    float NextRandom();
    void AddBaseHealth(string target, float ratio, CombatSourceRef origin);
    void AddAttribute(string target, CombatAttribute attribute, float amount, CombatSourceRef origin);
    void AdditionalAttack(string source, string target, float amount, string actionId,
        BattleDamageStatisticsAttribution statisticsAttribution = default);
    void Present(string source, string target, string cue);
    void RemoveShield(string target, float amount);
    void BuffSummonHealth(string target, float amount, CombatSourceRef origin);
    string? SpawnNear(UnitSnapshot profile, int team, Vector2 position, float health, float damage, string source);
    void Retire(string target, string source, string reason);
    void Publish(BattleCombatEventDraft fact);
}
internal interface IBattleTraitTransactions
{
    void Resolve(Action work);
    void Continue(string chain, int depth, Action work);
}

internal sealed record BattleTraitBinding(string Id, int Team, CompiledTraitMechanic Spec, ImmutableHashSet<string> Members);

// Published once during battle assembly. Membership never follows temporary products.
internal sealed class BattleTraitBindings(string scopeId)
{
    public ImmutableArray<BattleTraitBinding> Entries { get; private set; } = [];
    public bool ObserveBreaks { get; private set; }
    public bool NeedsEvents { get; private set; }
    private bool _initialized;
    public void Initialize(TraitSnapshot snapshot, IReadOnlyList<BattleUnitView> units)
    {
        if (_initialized) throw new InvalidOperationException("Trait bindings were already initialized.");
        Entries = snapshot.Values.Where(v => v.ActiveBreakpoint is not null)
            .SelectMany(v => (v.ActiveBreakpoint!.Mechanics.IsDefault ? [] : v.ActiveBreakpoint.Mechanics)
                .Select(m => new BattleTraitBinding(v.TraitId, v.Team, m, units.Where(u => u.Team == v.Team &&
                    v.Contributions.Any(c => c.OwnerRuntimeId == u.RuntimeId ||
                        !string.IsNullOrEmpty(u.SourceInstanceId) && c.OwnerRuntimeId == u.SourceInstanceId))
                    .Select(u => u.RuntimeId).ToImmutableHashSet(StringComparer.Ordinal))))
            .OrderBy(b => b.Team).ThenBy(b => b.Id, StringComparer.Ordinal).ToImmutableArray();
        var hasMatrix = units.Any(u => u.AbilityLoadout is { } loadout &&
            loadout.Abilities.Any(a => a.Operations.Any(o => o is CompiledMatrixOperation)));
        ObserveBreaks = hasMatrix || Entries.Any(b => b.Spec.Kind == TraitMechanicKind.BarrierRetaliation);
        NeedsEvents = hasMatrix || !Entries.IsEmpty;
        _initialized = true;
    }
    public IEnumerable<BattleTraitBinding> For(string owner, TraitMechanicKind kind) =>
        Entries.Where(b => b.Spec.Kind == kind && b.Members.Contains(owner));
    public CombatSourceRef Origin(BattleTraitBinding binding, string owner) =>
        new(CombatSourceKind.Trait, binding.Id, owner, $"{scopeId}:{binding.Team}:{binding.Id}:{owner}");
}

internal static class BattleMechanicStatus
{
    internal static CompiledStatusDefinition Create(string id, string name, int ticks,
        StatusBehaviorKind behavior = StatusBehaviorKind.None, bool harmful = false,
        ImmutableArray<CompiledAttributeModifier> modifiers = default) => new(id, "", name, name, behavior,
        harmful ? StatusDisposition.Harmful : StatusDisposition.Helpful, StatusDurationKind.TimedTicks,
        Math.Max(1, ticks), StatusAggregationPolicy.Independent, 1, StatusOverflowPolicy.RejectNewStacks,
        StatusDurationRefreshPolicy.None, StatusPeriodicResetPolicy.KeepSchedule, StatusDispelCategory.Ordinary,
        StatusDeathPolicy.Remove, behavior == StatusBehaviorKind.DisableActions ? StatusControlDurationRule.LinearResistanceCeiling : StatusControlDurationRule.None,
        behavior == StatusBehaviorKind.DisableActions ? [StatusDefinitionCompiler.ActionDisabledTag] : [],
        modifiers.IsDefault ? [] : modifiers, 1, 0, null, [], [], null,
        new CompiledStatusPresentation("status", "", "", "", "", name));

}
