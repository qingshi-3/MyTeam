using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private sealed class AbilityMechanisms(BattleSimulation battle) : IBattleAbilityMechanisms
    {
        public BattleAbilityOperationPreparation Prepare(CompiledAbilityOperation operation, BattleAbilityInvocation invocation)
        {
            var owner = battle._units.First(unit => unit.RuntimeId == invocation.OwnerId);
            var explicitTargetId = invocation.TargetId;
            switch (operation)
            {
                case CompiledEnemyAction enemyAction:
                    if (!battle.PrepareEnemyAction(owner,enemyAction,explicitTargetId))
                        return BattleAbilityOperationPreparation.Reject("当前没有合法的机制目标或召唤资源。");
                    return new([explicitTargetId]);
                case CompiledCombatTechniqueOperation technique:
                    if (!battle.PrepareTechnique(technique,owner,explicitTargetId,out var techniqueTargets))
                        return BattleAbilityOperationPreparation.Reject("没有合法的技能目标或积累条件。");
                    return new(techniqueTargets);
                case CompiledTrampleOperation trample:
                    if (battle.IsDisplacing(owner) || owner.ProjectileSequence is not null || !owner.ProjectileWindups.IsEmpty ||
                        !battle._units.Any(target => target.RuntimeId == explicitTargetId && battle.CanAimTrample(owner, target, trample)))
                        return BattleAbilityOperationPreparation.Reject("没有合法的冲锋方向。");
                    return new([explicitTargetId]);
                case CompiledChargedLineOperation line:
                    if (owner.ChargedLine is not null || battle.IsDisplacing(owner) || owner.ProjectileSequence is not null ||
                        !owner.ProjectileWindups.IsEmpty || !battle._units.Any(target => target.RuntimeId == explicitTargetId && battle.CanAimChargedLine(owner, target, line)))
                        return BattleAbilityOperationPreparation.Reject("当前不能蓄力或没有合法的贯穿目标。");
                    return new([explicitTargetId]);
                case CompiledDisplacementOperation displacement:
                    if (!battle.PrepareDisplacementOperation(displacement, owner, explicitTargetId, out var displacementTargets))
                        return BattleAbilityOperationPreparation.Reject("没有合法的位移目标或落点。");
                    return new(displacementTargets);
                case CompiledMatrixOperation matrix:
                    if (!battle.PrepareMatrixOperation(matrix, owner, explicitTargetId, out var matrixTargets))
                        return BattleAbilityOperationPreparation.Reject("当前不满足技能的资源、状态或目标条件。");
                    return new(matrixTargets);
                case CompiledBattleValueOperation or CompiledConsumeStatusOperation or CompiledEchoOperation or CompiledLifecycleOperation:
                    if (!battle.PrepareBattleOperation(operation,owner,explicitTargetId,out var battleTargets))
                        return BattleAbilityOperationPreparation.Reject("当前不满足技能的资源、状态或目标条件。");
                    return new(battleTargets);
                case CompiledProjectileSequenceAbilityOperation sequence:
                    if (owner.ProjectileSequence is not null || owner.AttackDelivery != Content.AttackDelivery.Projectile ||
                        !battle._units.Any(unit => unit.RuntimeId == explicitTargetId && unit.Alive && unit.Team != owner.Team))
                        return BattleAbilityOperationPreparation.Reject("当前没有合法的连射目标。");
                    return new([explicitTargetId]);
                default: return BattleAbilityOperationPreparation.Reject("能力包含不受支持的操作。");
            }
        }

        public void Execute(ResolvedAbilityOperation resolved, AbilityExecutionPlan plan, ImmutableArray<string>.Builder facts)
        {
            var owner = battle._units.First(unit => unit.RuntimeId == plan.OwnerId);
            switch (resolved.Operation)
            {
                case CompiledEnemyAction enemyAction:
                    battle.BeginEnemyAction(owner,battle._units.First(u => u.RuntimeId == resolved.TargetIds[0]),enemyAction,plan.Ability);
                    facts.Add(enemyAction.GetType().Name);
                    break;
                case CompiledCombatTechniqueOperation technique:
                    battle.ExecuteTechnique(technique,owner,resolved.TargetIds,plan.Ability);
                    facts.Add(technique.GetType().Name);
                    break;
                case CompiledTrampleOperation trample:
                    battle.BeginTrample(owner, battle._units.First(target => target.RuntimeId == resolved.TargetIds[0]), trample, plan.Ability);
                    facts.Add("Trample");
                    break;
                case CompiledChargedLineOperation line:
                    battle.BeginChargedLine(owner, battle._units.First(target => target.RuntimeId == resolved.TargetIds[0]),
                        line, plan.Ability);
                    facts.Add($"ChargedLine:{line.Delivery}");
                    break;
                case CompiledDisplacementOperation displacement:
                    battle.ExecuteDisplacementOperation(displacement, resolved.TargetIds, owner, battle.AbilityOrigin(plan.Ability, plan.OwnerId));
                    facts.Add($"Displacement:{displacement.Kind}:{string.Join(",", resolved.TargetIds)}");
                    break;
                case CompiledBattleValueOperation or CompiledConsumeStatusOperation or CompiledEchoOperation or CompiledLifecycleOperation:
                    battle.ExecuteBattleOperation(resolved,plan,owner);
                    facts.Add($"BattleOperation:{resolved.OperationIndex}");
                    break;
                case CompiledMatrixOperation:
                    battle.ExecuteMatrixOperation(resolved, plan, owner);
                    facts.Add($"MatrixOperation:{resolved.OperationIndex}");
                    break;
                case CompiledProjectileSequenceAbilityOperation sequence:
                    owner.ProjectileSequence = new ProjectileSequenceState(sequence,
                        battle.AbilityOrigin(plan.Ability, plan.OwnerId), resolved.TargetIds[0], sequence.ShotCount);
                    battle.FireSequenceShot(owner);
                    facts.Add($"Sequence:{sequence.ShotCount}");
                    break;
                default: throw new InvalidOperationException("Unsupported mechanism operation.");
            }
        }
    }
}
