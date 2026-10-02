using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private readonly BattleAbilityExecutor _abilityExecutor;

    private sealed class AbilityHost(BattleSimulation battle) : IBattleAbilityHost
    {
        public int Tick => battle.TickIndex;
        public int RemainingGold => battle.RemainingGold;
        public bool IsAlive(string id) => battle._units.Any(unit => unit.RuntimeId == id && unit.Alive);
        public AbilityWorldSnapshot CaptureSnapshot(int tick) => battle.CaptureAbilitySnapshot(tick);
        public string SelectAutomaticTarget(string ownerId, CompiledAbilityDefinition ability) =>
            battle.SelectAutomaticTarget(battle._units.First(unit => unit.RuntimeId == ownerId), ability);
        public BattleAbilityCaster? ReadCaster(string id, int tick)
        {
            var unit = battle._units.FirstOrDefault(unit => unit.RuntimeId == id);
            if (unit is null) return null;
            var controlled = unit.DisabledTicks > 0 || battle._statusScope.HasTag(id, StatusDefinitionCompiler.ActionDisabledTag);
            return new(unit.Alive, battle.EnemyActionBusy(unit) || battle.TechniqueBusy(unit) ||
                battle.UnitActionRecovering(unit) || battle.DuelOpponent(unit) is not null ||
                unit.Trample is not null || unit.ChargedLine is not null || controlled,
                battle.IsDisplacing(unit), BattleHeroMana.IsReady(unit, tick), controlled);
        }

        public AbilityCommitResult ExecuteAtomically(Func<AbilityCommitResult> work)
        {
            var checkpoint = new BattleWorldStateCheckpoint(battle);
            try
            {
                AbilityCommitResult result;
                using (var resolution = battle._combatPipeline.BeginAuthoritativeResolution())
                {
                    result = work();
                    if (result.Succeeded) resolution.Commit();
                }
                if (result.Succeeded) checkpoint.Commit();
                else checkpoint.Rollback();
                return result;
            }
            catch (Exception commitFailure)
            {
                try { checkpoint.Rollback(); }
                catch (Exception rollbackFailure)
                { throw new AggregateException("Ability world rollback failed.", commitFailure, rollbackFailure); }
                return BattleAbilityExecutor.CommitFailed(AbilityActivationFailure.CommitFailed, commitFailure.Message);
            }
        }

        public BattleAbilityCast BeginCast(AbilityExecutionPlan plan)
        {
            var owner = battle._units.First(unit => unit.RuntimeId == plan.OwnerId);
            var manaSkill = plan.Ability.Trigger == AbilityTriggerKind.ManaFull;
            var manaSpent = manaSkill ? BattleHeroMana.Spend(owner, plan.Tick, plan.Ability.CooldownTicks) : 0;
            var actionId = battle.BeginAbilityAction(owner.RuntimeId, plan.Ability);
            battle.TraitMechanicsBeginAbility(owner.RuntimeId, actionId, manaSkill && manaSpent > 0);
            return new(manaSpent, actionId);
        }

        public void CompleteCast(AbilityExecutionPlan plan, BattleAbilityCast cast)
        {
            var owner = battle._units.First(unit => unit.RuntimeId == plan.OwnerId);
            var manaSkill = plan.Ability.Trigger == AbilityTriggerKind.ManaFull;
            var manaSpent = cast.ManaSpent;
            var actionId = cast.ActionId;
            battle.GoldSpent += plan.GoldCost;
            if (plan.GoldCost > 0)
                foreach (var unit in battle._units.Where(unit => unit.Team == 0 && unit.Alive)) unit.Health = Math.Min(unit.Health, unit.MaxHealth);
            if (plan.Ability.IsActiveSkill)
            {
                owner.LastAbilityName = plan.Ability.DisplayName;
                var primaryTarget = plan.Operations.SelectMany(operation => operation.TargetIds).FirstOrDefault() ?? owner.RuntimeId;
                if (owner.Alive)
                {
                    owner.Mode = BattleUnitMode.Casting;
                    owner.LastActionKind = BattleActionKind.Ability;
                    var actionTarget = battle._units.FirstOrDefault(unit => unit.RuntimeId == primaryTarget);
                    if (actionTarget is not null) SetActionTarget(owner, actionTarget);
                    else ClearActionTarget(owner);
                    battle._movement?.ReleaseGoal(owner.RuntimeId);
                    battle.Emit("ability", owner.RuntimeId, primaryTarget, manaSpent, owner.Position,
                        string.IsNullOrWhiteSpace(plan.Ability.Presentation?.Cue) ? "skill_cast" : plan.Ability.Presentation.Cue,
                        sourceVfx: plan.Ability.Presentation?.CastVfx ?? "");
                }
            }
            if (manaSkill && manaSpent > 0)
                battle.PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.ManaSkillResolved,
                    battle.AbilityOrigin(plan.Ability,plan.OwnerId),plan.SourceId,plan.OwnerId,plan.Tick,
                    RequestedValue:manaSpent,AppliedValue:manaSpent,EffectiveValue:manaSpent,
                    SubjectStableId:plan.Ability.StableId,ActionId:actionId));
            battle.PublishCombat(new BattleCombatEventDraft(
                BattleCombatEventKind.AbilityResolved,
                battle.AbilityOrigin(plan.Ability, plan.OwnerId),
                plan.SourceId,
                plan.OwnerId,
                plan.Tick,
                Cell: ToCombatCell(owner.Cell),
                Position: ToCombatPoint(owner.Position),
                SubjectStableId: plan.Ability.StableId, ActionId: actionId));
        }
    }

    private sealed class AbilityEffectWorld(BattleSimulation battle) : IBattleAbilityEffectWorld
    {
        public IReadOnlyList<BattleUnitView> Units => battle.UnitViews;
        public EffectWorldSnapshot CaptureSnapshot(int tick) => battle.CaptureEffectSnapshot(tick);
        public CombatSourceRef Origin(CompiledAbilityDefinition ability, string ownerId) => battle.AbilityOrigin(ability, ownerId);
        public string PassiveGrantId(string ownerId, string abilityId) => battle.PassiveGrantId(ownerId, abilityId);
        public void SetCooldowns(string targetId, int attack, int move)
        {
            var target = battle._units.First(unit => unit.RuntimeId == targetId);
            target.AttackCooldown = attack;
            target.MoveCooldown = move;
        }
        public void ShowShield(string sourceId, string targetId, float amount) =>
            battle.Emit("shield", sourceId, targetId, amount, Position(targetId), "skill_cast");
        public void ShowImpact(string sourceId, string targetId, float amount, string vfx) =>
            battle.Emit("vfx", sourceId, targetId, amount, Position(targetId), "", vfx: new BattleVfxCue(BattleVfxPhase.Burst, vfx));
        public void ShowArea(string sourceId, string anchorId, string vfx, float range) =>
            battle.Emit("vfx", sourceId, anchorId, 0, Position(anchorId), "", origin: Position(anchorId),
                vfx: new BattleVfxCue(BattleVfxPhase.Burst, vfx, range));
        private Vector2 Position(string id) => battle._units.First(unit => unit.RuntimeId == id).Position;
    }

    private sealed class AbilitySummonWorld(BattleSimulation battle) : IBattleAbilitySummonWorld
    {
        public IReadOnlyList<BattleUnitView> Units => battle.UnitViews;
        public bool TryFindOpenNear(Vector2 origin, int team, float radius, IReadOnlyList<(Vector2 Position, float Radius)> reserved, out Vector2 position) =>
            battle.TryFindOpenNear(origin, team, radius, reserved, out position);
        public bool CanOccupy(Vector2 position, float radius, IReadOnlyList<(Vector2 Position, float Radius)> reserved) =>
            battle.CanOccupyPosition(position, radius, reserved);
        public bool Spawn(UnitSnapshot profile, int team, Vector2 position, float health, float damage, string sourceId) =>
            battle.SpawnTemporary(profile, team, position, health, damage, sourceId);
        public void SpawnNear(UnitSnapshot profile, int team, Vector2 origin, float health, float damage, string sourceId) =>
            battle.SpawnTemporaryNear(profile, team, origin, health, damage, sourceId);
        public bool ConsumeExtraSummon(string ownerId) => battle.TraitMechanicsExtraSummon(ownerId);
        public UnitSnapshot? ResolveProfile(CompiledSummonAbilityOperation summon, string ownerId)
        {
            var owner = battle._units.First(unit => unit.RuntimeId == ownerId);
            if (!string.IsNullOrWhiteSpace(summon.SummonContentId) &&
                battle._config.TacticalSummons.TryGetValue(summon.SummonContentId, out var authoredSummon))
                return authoredSummon;
            var profile = summon.Profile switch
            {
                AbilitySummonProfile.DeathSummon => battle._config.Summons.DeathSummon,
                AbilitySummonProfile.HeroConstruct => battle._config.Summons.HeroConstruct,
                AbilitySummonProfile.Mercenary => battle._config.Summons.Mercenary,
                AbilitySummonProfile.ItemToken => battle._config.Summons.ItemToken,
                AbilitySummonProfile.BehaviorSummon => owner.BehaviorSummon,
                _ => null
            };
            return profile is not null &&
                   (string.IsNullOrWhiteSpace(summon.SummonContentId) || profile.ContentId == summon.SummonContentId)
                ? profile
                : null;
        }
    }
}
