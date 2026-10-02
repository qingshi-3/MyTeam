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

internal interface IBattleAbilityEffectWorld
{
    IReadOnlyList<BattleUnitView> Units { get; }
    EffectWorldSnapshot CaptureSnapshot(int tick);
    CombatSourceRef Origin(CompiledAbilityDefinition ability, string ownerId);
    string PassiveGrantId(string ownerId, string abilityId);
    void SetCooldowns(string targetId, int attack, int move);
    void ShowShield(string sourceId, string targetId, float amount);
    void ShowImpact(string sourceId, string targetId, float amount, string vfx);
    void ShowArea(string sourceId, string anchorId, string vfx, float range);
}

internal sealed class BattleAbilityEffectOperations(IBattleAbilityEffectWorld world,
    BattleEffectCompatibilityAdapter effects, BattleStatusScope statuses, Func<TraitSnapshot> traits, Func<float> random)
{
    public static bool Handles(CompiledAbilityOperation operation) => operation is
        CompiledEffectAbilityOperation or CompiledCooldownAbilityOperation or CompiledScaledStatusAbilityOperation or CompiledApplyStatusAbilityOperation;

    public BattleAbilityOperationPreparation Prepare(CompiledAbilityOperation operation, BattleAbilityInvocation invocation)
    {
        var query = operation switch
        {
            CompiledEffectAbilityOperation effect => effect.Binding.TargetQuery,
            CompiledCooldownAbilityOperation cooldown => cooldown.TargetQuery,
            CompiledScaledStatusAbilityOperation scaled => scaled.TargetQuery,
            CompiledApplyStatusAbilityOperation status => status.TargetQuery,
            _ => throw new InvalidOperationException("Unsupported effect operation.")
        };
        var targets = ResolveTargets(query, world.CaptureSnapshot(invocation.Tick), invocation.Snapshot,
            invocation.SourceId, invocation.OwnerId, invocation.TargetId);
        if (targets.Length == 0) return BattleAbilityOperationPreparation.Reject(
            operation is CompiledScaledStatusAbilityOperation or CompiledApplyStatusAbilityOperation ? "当前没有合法的状态目标。" : "当前没有合法的能力目标。");
        var value = 0f;
        if (operation is CompiledEffectAbilityOperation authored)
        {
            value = authored.InvocationValueSource == AbilityInvocationValueSource.OwnerMaxHealth
                ? invocation.Snapshot.Entities[invocation.OwnerId].MaxHealth * authored.InvocationValueScale : authored.InvocationValueScale;
            var preflight = effects.PreflightAuthored(authored.Binding, invocation.SourceId, invocation.OwnerId,
                targets.FirstOrDefault() ?? invocation.TargetId, invocation.Tick, value, world.Origin(invocation.Ability, invocation.OwnerId));
            if (!preflight.Succeeded) return BattleAbilityOperationPreparation.Reject(
                string.IsNullOrWhiteSpace(preflight.Message) ? "当前不满足能力效果条件。" : preflight.Message);
        }
        return new(targets, value);
    }

    internal static ImmutableArray<string> ResolveTargets(CompiledEffectTargetQuery query, EffectWorldSnapshot effects,
        AbilityWorldSnapshot abilities, string sourceId, string ownerId, string targetId) =>
        EffectTargetResolver.Resolve(query, effects, sourceId, ownerId, targetId)
            .Where(id => abilities.Entities.TryGetValue(id, out var entity) && entity.Alive).ToImmutableArray();

    public void Execute(ResolvedAbilityOperation resolved, AbilityExecutionPlan plan, ImmutableArray<string>.Builder facts)
    {
        switch (resolved.Operation)
        {
            case CompiledEffectAbilityOperation effect:
            {
                var result = effects.ExecuteAuthored(
                    effect.Binding,
                    plan.SourceId,
                    plan.OwnerId,
                    resolved.TargetIds.FirstOrDefault() ?? string.Empty,
                    plan.Tick,
                    resolved.InvocationValue,
                    world.Origin(plan.Ability, plan.OwnerId));
                if (result.Status != EffectExecutionStatus.Succeeded)
                    throw new InvalidOperationException($"Prepared ability effect '{effect.Binding.StableId}' failed during commit.");
                var steps = result.Invocations.SelectMany(invocation => invocation.Steps)
                    .Where(step => step.Status == EffectExecutionStatus.Succeeded)
                    .ToArray();
                if (plan.Ability.ActivationKind == AbilityActivationKind.Automatic)
                    foreach (var step in steps.Where(step => step.Kind == EffectKind.Shield))
                    {
                        world.ShowShield(plan.SourceId, step.TargetId, step.AppliedAmount);
                    }
                facts.AddRange(steps.Select(step => $"{step.Kind}:{step.TargetId}:{step.AppliedAmount:0.###}"));
                if (plan.Ability.Presentation?.DamageVfx is { Length: > 0 } vfx)
                    foreach (var step in steps.Where(step => step.Kind == EffectKind.Damage && step.AppliedAmount > 0))
                    {
                        var target = world.Units.First(unit => unit.RuntimeId == step.TargetId);
                        world.ShowImpact(plan.SourceId, target.RuntimeId, step.AppliedAmount, vfx);
                    }
                break;
            }
            case CompiledCooldownAbilityOperation cooldown:
                foreach (var targetId in resolved.TargetIds)
                {
                    var target = world.Units.First(unit => unit.RuntimeId == targetId);
                    if (!target.Alive) continue;
                    world.SetCooldowns(targetId,
                        AdjustCooldown(target.AttackCooldown, cooldown.AttackAdjustment, cooldown.AttackValue),
                        AdjustCooldown(target.MoveCooldown, cooldown.MoveAdjustment, cooldown.MoveValue));
                    facts.Add($"Cooldown:{targetId}");
                }
                break;
            case CompiledScaledStatusAbilityOperation scaled:
                CommitScaledStatus(scaled, resolved, plan, facts);
                break;
            case CompiledApplyStatusAbilityOperation status:
                foreach (var targetId in resolved.TargetIds)
                {
                    if (!world.Units.Any(unit => unit.RuntimeId == targetId && unit.Alive)) continue;
                    var grantId = plan.Ability.ActivationKind == AbilityActivationKind.Passive
                        ? world.PassiveGrantId(plan.OwnerId, plan.Ability.StableId) : string.Empty;
                    var applied = statuses.ApplyBatch([new StatusApplicationRequest(
                        status.Status, plan.SourceId, targetId, plan.Tick, grantId)])[0];
                    if (!applied.Applied)
                        throw new InvalidOperationException($"Prepared status '{status.Status.StableId}' failed during commit.");
                    facts.Add($"Status:{status.Status.StableId}:{targetId}:{applied.Status?.Stacks ?? 0}");
                }
                if (!string.IsNullOrWhiteSpace(status.ApplicationVfx) && status.TargetQuery is CompiledFilteredTargetQuery area)
                {
                    var anchorId = area.Anchor == EffectEntityReference.Owner ? plan.OwnerId : plan.SourceId;
                    world.ShowArea(plan.SourceId, anchorId, status.ApplicationVfx, area.Range);
                }
                break;
            default: throw new InvalidOperationException("Unsupported effect operation.");
        }
    }

    private void CommitScaledStatus(CompiledScaledStatusAbilityOperation operation,
        ResolvedAbilityOperation resolved, AbilityExecutionPlan plan, ImmutableArray<string>.Builder facts)
    {
        var appliedAny = false;
        foreach (var targetId in resolved.TargetIds)
        {
            if (!world.Units.Any(unit => unit.RuntimeId == targetId && unit.Alive)) continue;

            // Read once, before adding this operation's stacks. Earlier committed operations
            // may contribute; this batch cannot compound itself or change its own chance.
            var current = statuses.SnapshotOwner(targetId);
            double Count(string id) => string.IsNullOrEmpty(id) ? 0 :
                current.Where(status => status.StableId == id).Sum(status => (double)status.Stacks);
            var amount = Math.Floor(operation.BaseStacks + Count(operation.StatusId) * operation.ExistingStackRatio);
            if (!double.IsFinite(amount) || amount > int.MaxValue)
                throw new InvalidOperationException("Scaled status application exceeds the supported stack count.");
            if (amount <= 0) continue;
            var baseChance = operation.Chance;
            if (!operation.ChanceByTraitTier.IsDefaultOrEmpty)
            {
                var owner = world.Units.First(unit => unit.RuntimeId == plan.OwnerId);
                var trait = traits().Values.FirstOrDefault(value =>
                    value.TraitId == operation.ChanceTraitId && value.Team == owner.Team);
                // Membership and the active tier come from the existing live trait scope.
                // Re-read after team changes; a copied ability alone does not grant membership.
                if (trait?.ActiveBreakpoint is { } tier && trait.Contributions.Any(contribution =>
                        contribution.OwnerRuntimeId == owner.RuntimeId ||
                        (!string.IsNullOrEmpty(owner.SourceInstanceId) && contribution.OwnerRuntimeId == owner.SourceInstanceId)))
                {
                    if (tier.Index >= operation.ChanceByTraitTier.Length)
                        throw new InvalidOperationException("Trait chance table does not cover the active tier.");
                    baseChance = operation.ChanceByTraitTier[tier.Index];
                }
            }
            var chance = Math.Clamp(baseChance + Count(operation.ChanceStatusId) * operation.ChancePerStack,
                0d, operation.MaximumChance);

            // Preparation never consumes randomness. A failed roll is a successful no-op;
            // failures of subsequent operations still restore this roll through the world checkpoint.
            if (chance <= 0 || (chance < 1 && random() >= chance))
            {
                facts.Add($"StatusMiss:{operation.Status.StableId}:{targetId}");
                continue;
            }

            // Use ordinary single-stack applications to preserve source contributions,
            // refresh, overflow and lifecycle semantics; never mutate the shared definition.
            var applications = statuses.ApplyBatch(Enumerable.Range(0, (int)amount).Select(_ =>
                new StatusApplicationRequest(operation.Status, plan.SourceId, targetId, plan.Tick)));
            if (applications.Any(result => !result.Applied))
                throw new InvalidOperationException($"Prepared status '{operation.Status.StableId}' failed during commit.");
            appliedAny = true;
            facts.Add($"Status:{operation.Status.StableId}:{targetId}:{applications[^1].Status?.Stacks ?? 0}");
        }

        if (appliedAny && !string.IsNullOrWhiteSpace(operation.ApplicationVfx) &&
            operation.TargetQuery is CompiledFilteredTargetQuery area)
        {
            var anchorId = area.Anchor == EffectEntityReference.Owner ? plan.OwnerId : plan.SourceId;
            world.ShowArea(plan.SourceId, anchorId, operation.ApplicationVfx, area.Range);
        }
    }

    private static int AdjustCooldown(int current, CooldownAdjustmentKind kind, int value) => kind switch
    {
        CooldownAdjustmentKind.None => current,
        CooldownAdjustmentKind.Reset => 0,
        CooldownAdjustmentKind.Add => current + value,
        CooldownAdjustmentKind.Cap => Math.Min(current, value),
        CooldownAdjustmentKind.Divide => current / value,
        _ => current
    };

}
