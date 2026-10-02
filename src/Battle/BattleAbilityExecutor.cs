using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Battle;

internal sealed record BattleAbilityCaster(bool Alive, bool AutomaticBlocked, bool Displacing, bool ManaReady, bool Controlled);
internal sealed record BattleAbilityCast(float ManaSpent, string ActionId);
internal sealed record BattleAbilityInvocation(CompiledAbilityDefinition Ability, string SourceId, string OwnerId,
    string TargetId, int Tick, AbilityWorldSnapshot Snapshot);
internal sealed record BattleAbilityOperationPreparation(ImmutableArray<string> Targets, float Value = 0, string Error = "")
{
    public static BattleAbilityOperationPreparation Reject(string reason) => new([], Error: reason);
}

internal interface IBattleAbilityHost
{
    int Tick { get; }
    int RemainingGold { get; }
    BattleAbilityCaster? ReadCaster(string ownerId, int tick);
    bool IsAlive(string id);
    AbilityWorldSnapshot CaptureSnapshot(int tick);
    string SelectAutomaticTarget(string ownerId, CompiledAbilityDefinition ability);
    AbilityCommitResult ExecuteAtomically(Func<AbilityCommitResult> work);
    BattleAbilityCast BeginCast(AbilityExecutionPlan plan);
    void CompleteCast(AbilityExecutionPlan plan, BattleAbilityCast cast);
}

internal interface IBattleAbilityMechanisms
{
    BattleAbilityOperationPreparation Prepare(CompiledAbilityOperation operation, BattleAbilityInvocation invocation);
    void Execute(ResolvedAbilityOperation operation, AbilityExecutionPlan plan, ImmutableArray<string>.Builder facts);
}

// Owns the ability protocol, not the mutable world. Preparation never enters a transaction.
internal sealed class BattleAbilityExecutor(IBattleAbilityHost host, BattleAbilityEffectOperations effects,
    BattleAbilitySummons summons, IBattleAbilityMechanisms mechanisms) : IAbilityRuntimeWorld
{
    public AbilityWorldSnapshot CaptureSnapshot(int tick) => host.CaptureSnapshot(tick);

    public AbilityPreparationResult Prepare(CompiledAbilityDefinition ability, string sourceId, string ownerId,
        string explicitTargetId, int tick)
    {
        var caster = host.ReadCaster(ownerId, tick);
        if (caster is null || (!caster.Alive && ability.Trigger != AbilityTriggerKind.OwnerDefeated))
            return Failed(AbilityActivationFailure.SourceUnavailable, "英雄或能力拥有者已无法行动。");
        if (ability.GoldCost > host.RemainingGold)
            return Failed(AbilityActivationFailure.InsufficientGold, $"金币不足：需要 {ability.GoldCost} 金币。");
        if (ability.ActivationKind == AbilityActivationKind.Automatic && caster.AutomaticBlocked)
            return Failed(AbilityActivationFailure.ConditionsUnmet, "受控期间不能施法。");
        if (ability.Trigger == AbilityTriggerKind.ManaFull)
        {
            if (caster.Displacing)
                return Failed(AbilityActivationFailure.ConditionsUnmet, "位移过程中不能开始新的主动技能。");
            if (!caster.ManaReady)
                return Failed(AbilityActivationFailure.InsufficientMana, "法力尚未充满或正在施法。");
        }
        if (ability.ActivationKind == AbilityActivationKind.Automatic)
            explicitTargetId = host.SelectAutomaticTarget(ownerId, ability);
        var invocation = new BattleAbilityInvocation(ability, sourceId, ownerId, explicitTargetId, tick, CaptureSnapshot(tick));
        var operations = ImmutableArray.CreateBuilder<ResolvedAbilityOperation>();
        var reservations = new BattleAbilitySummons.Reservations();
        for (var index = 0; index < ability.Operations.Length; index++)
        {
            var operation = ability.Operations[index];
            var prepared = operation is CompiledSummonAbilityOperation summon
                ? summons.Prepare(summon, invocation, index, reservations)
                : BattleAbilityEffectOperations.Handles(operation)
                    ? effects.Prepare(operation, invocation)
                    : mechanisms.Prepare(operation, invocation);
            if (prepared.Error.Length > 0) return Failed(AbilityActivationFailure.ConditionsUnmet, prepared.Error);
            operations.Add(new(index, operation, prepared.Targets, prepared.Value));
        }
        return new(true, AbilityActivationFailure.None, "", new(ability, sourceId, ownerId, tick,
            operations.ToImmutable(), reservations.Items.ToImmutableArray(), ability.GoldCost));
    }

    public AbilityCommitResult Commit(AbilityExecutionPlan plan) => host.ExecuteAtomically(() => CommitCore(plan));

    private AbilityCommitResult CommitCore(AbilityExecutionPlan plan)
    {
        if (plan.Tick != host.Tick) return CommitFailed(AbilityActivationFailure.CommitFailed, "能力计划已经过期。");
        var caster = host.ReadCaster(plan.OwnerId, plan.Tick);
        if (caster is null || (!caster.Alive && plan.Ability.Trigger != AbilityTriggerKind.OwnerDefeated))
            return CommitFailed(AbilityActivationFailure.SourceUnavailable, "英雄或能力拥有者已无法行动。");
        if (plan.Ability.Trigger == AbilityTriggerKind.ManaFull &&
            (caster.Displacing || !caster.ManaReady || caster.Controlled))
            return CommitFailed(AbilityActivationFailure.ConditionsUnmet, "施法条件在提交前失效。");
        if (plan.GoldCost > host.RemainingGold)
            return CommitFailed(AbilityActivationFailure.InsufficientGold, $"金币不足：需要 {plan.GoldCost} 金币。");
        foreach (var operation in plan.Operations)
            if (operation.Operation is not CompiledLifecycleOperation && operation.TargetIds.Any(id => !host.IsAlive(id)))
                return CommitFailed(AbilityActivationFailure.CommitFailed, "能力目标在提交前失效。");
        var reservationError = summons.Validate(plan);
        if (reservationError.Length > 0) return CommitFailed(AbilityActivationFailure.CommitFailed, reservationError);

        // Lock mana/recovery before any effect can react to this action.
        var cast = host.BeginCast(plan);
        var facts = ImmutableArray.CreateBuilder<string>();
        var successfulSummons = new HashSet<int>();
        foreach (var resolved in plan.Operations.OrderBy(operation => operation.OperationIndex))
        {
            // A death caused by an earlier committed operation is retained, not a preflight failure.
            if (resolved.Operation is not CompiledLifecycleOperation && resolved.TargetIds.Length > 0 &&
                resolved.TargetIds.All(id => !host.IsAlive(id))) continue;
            if (resolved.Operation is CompiledSummonAbilityOperation summon)
            {
                if (summons.Execute(summon, resolved, plan, facts)) successfulSummons.Add(resolved.OperationIndex);
            }
            else if (BattleAbilityEffectOperations.Handles(resolved.Operation)) effects.Execute(resolved, plan, facts);
            else mechanisms.Execute(resolved, plan, facts);
        }
        summons.CompleteBonuses(plan, successfulSummons);
        host.CompleteCast(plan, cast);
        return new(true, AbilityActivationFailure.None, "", facts.ToImmutable(), cast.ManaSpent);
    }

    private static AbilityPreparationResult Failed(AbilityActivationFailure failure, string reason) => new(false, failure, reason, null);
    internal static AbilityCommitResult CommitFailed(AbilityActivationFailure failure, string reason) => new(false, failure, reason, []);
}
