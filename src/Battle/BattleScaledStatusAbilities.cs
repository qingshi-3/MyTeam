using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private void CommitScaledStatus(CompiledScaledStatusAbilityOperation operation,
        ResolvedAbilityOperation resolved, AbilityExecutionPlan plan, ImmutableArray<string>.Builder facts)
    {
        var appliedAny = false;
        foreach (var targetId in resolved.TargetIds)
        {
            if (!_units.Any(unit => unit.RuntimeId == targetId && unit.Alive)) continue;

            // Read once, before adding this operation's stacks. Earlier committed operations
            // may contribute; this batch cannot compound itself or change its own chance.
            var current = _statusScope.SnapshotOwner(targetId);
            double Count(string id) => string.IsNullOrEmpty(id) ? 0 :
                current.Where(status => status.StableId == id).Sum(status => (double)status.Stacks);
            var amount = Math.Floor(operation.BaseStacks + Count(operation.StatusId) * operation.ExistingStackRatio);
            if (!double.IsFinite(amount) || amount > int.MaxValue)
                throw new InvalidOperationException("Scaled status application exceeds the supported stack count.");
            if (amount <= 0) continue;
            var baseChance = operation.Chance;
            if (!operation.ChanceByTraitTier.IsDefaultOrEmpty)
            {
                var owner = _units.First(unit => unit.RuntimeId == plan.OwnerId);
                var trait = CurrentTraitSnapshot.Values.FirstOrDefault(value =>
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
            if (chance <= 0 || (chance < 1 && _random.NextFloat() >= chance))
            {
                facts.Add($"StatusMiss:{operation.Status.StableId}:{targetId}");
                continue;
            }

            // Use ordinary single-stack applications to preserve source contributions,
            // refresh, overflow and lifecycle semantics; never mutate the shared definition.
            var applications = _statusScope.ApplyBatch(Enumerable.Range(0, (int)amount).Select(_ =>
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
            var anchor = _units.First(unit => unit.RuntimeId == anchorId);
            Emit("vfx", plan.SourceId, anchorId, 0, anchor.Position, "", origin: anchor.Position,
                vfx: new BattleVfxCue(BattleVfxPhase.Burst, operation.ApplicationVfx, area.Range));
        }
    }
}
