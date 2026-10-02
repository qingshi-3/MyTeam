using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Battle;

public enum SkillResourceKind { Mana, Grit, Timer, Condition, Passive, Counter }
public enum SkillProgressState { Building, Ready, Casting, Queued, Recovering, Waiting, Active, Spent, Defeated, Ended }

public sealed record BattleSkillProgress(string AbilityId, string DisplayName, SkillResourceKind Kind,
    float Current, float Maximum, string ResourceName, SkillProgressState State, string Detail = "", int Priority = 2);

public sealed record BattleUnitSkillsSnapshot(ImmutableArray<CompiledAbilityDefinition> Abilities,
    ImmutableArray<BattleSkillProgress> Progress)
{
    public static BattleUnitSkillsSnapshot Empty { get; } = new([], []);
    public BattleSkillProgress? Primary => Progress.IsDefaultOrEmpty ? null : Progress[0];
}

public sealed partial class BattleSimulation
{
    private readonly BattleSkillProgressProjection _skillProgress;
    public BattleUnitSkillsSnapshot ReadUnitSkills(string id) => _skillProgress.ReadUnitSkills(id);
    private void CaptureTerminalSkillProgress() => _skillProgress.CaptureTerminalSkillProgress();
    private bool IsSkillExecuting(BattleUnitState owner, CompiledAbilityDefinition skill) =>
        owner.Trample?.Origin.StableId == skill.StableId || owner.ChargedLine?.Origin.StableId == skill.StableId ||
        owner.ProjectileSequence?.Origin.StableId == skill.StableId ||
        _techniqueRuntime.IsExecuting(owner.RuntimeId, skill.StableId) ||
        _enemyRuntime.IsExecuting(owner.RuntimeId, skill.StableId);

    private sealed class SkillProgressWorld(BattleSimulation owner) : IBattleSkillProgressWorld
    {
        private BattleUnitState Unit(string id) => owner._units.First(unit => unit.RuntimeId == id);
        public int Tick => owner.TickIndex;
        public IReadOnlyList<BattleUnitView> Units => owner.UnitViews;
        public ImmutableArray<AbilityRuntimeStatus> ReadStates(string id) => owner._abilityScope?.ReadStates(id) ?? [];
        public float Grit(string id, string key) => owner.CurrentGrit(Unit(id), key);
        public bool CriticalUsed(string id, string key) => owner._techniqueRuntime.CriticalUsed(Unit(id), key);
        public bool Recovering(string id) => owner.UnitActionRecovering(Unit(id));
        public bool Queued(string id, string ability) => owner.HasQueuedUnitAction(Unit(id), ability);
        public bool Executing(string id, CompiledAbilityDefinition skill) => owner.IsSkillExecuting(Unit(id), skill);
        public BattleBroodProgress? Brood(string id) => owner._enemyRuntime.ReadBrood(id);
        public float Counter(string id, string key) => owner.ReadCounter(Unit(id), key);
    }
}
