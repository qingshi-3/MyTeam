using System;
using System.Collections.Generic;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    // Adapter only: targeting and reaction policies cannot reach simulation fields.
    private sealed class MatrixWorld(BattleSimulation owner) : IBattleMatrixReactionWorld
    {
        public IReadOnlyList<BattleUnitView> Units => owner.UnitViews;
        public BattleUnitView? FindUnit(string id) => owner.MatrixUnit(id) is { } unit ? BattleUnitView.For(unit) : null;
        public float Counter(string ownerId, string key) => owner._matrixRuntime.Counter($"{ownerId}:{key}");
        public void WriteCounter(string ownerId, string key, float value) => owner._matrixRuntime.Write($"{ownerId}:{key}", value);
        public int Poison(string id) => owner.MatrixReadPoison(id);
        public int Chill(string id) => owner.MatrixReadChill(id);
        public int Ember(string id) => owner.MatrixReadEmber(id);
        public bool ActionDisabled(string id) => owner._statusScope.HasTag(id, StatusDefinitionCompiler.ActionDisabledTag);
        public int Tick => owner.TickIndex;
        public int MaximumReactions => owner._combatPipeline.Limits.MaxReactions;
        public bool ExcludedDeath(string id) => owner.MatrixExcludedDeath(id);
        public void PruneBuffs() => owner.MatrixPruneBuffs();
        public void PruneBeforeFact()
        {
            var checkpoint = new BattleWorldStateCheckpoint(owner);
            try
            {
                using var resolution = owner._combatPipeline.BeginAuthoritativeResolution();
                owner.MatrixPruneBuffs();
                resolution.Commit();
                checkpoint.Commit();
            }
            catch { checkpoint.Rollback(); throw; }
        }
        public void ResolveReaction(BattleCombatEvent? fact, Action work)
        {
            using var continuation = fact is null ? null : owner._combatPipeline.ContinueReaction(fact.ChainId, fact.Depth + 1);
            using var resolution = owner._combatPipeline.BeginAuthoritativeResolution();
            var checkpoint = new BattleWorldStateCheckpoint(owner);
            try
            {
                work();
                resolution.Commit();
                checkpoint.Commit();
            }
            catch { checkpoint.Rollback(); throw; }
        }
        public void Execute(CompiledMatrixOperation operation, string ownerId, string targetId,
            CombatSourceRef origin, BattleCombatEvent? fact, string slot)
        {
            if (owner.MatrixUnit(ownerId) is { } unit) owner.MatrixExecute(operation, unit, targetId, origin, fact, null, slot);
        }
    }
}
