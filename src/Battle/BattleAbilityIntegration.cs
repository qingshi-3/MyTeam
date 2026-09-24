using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private readonly List<IDisposable> _abilityCombatSubscriptions = [];
    private readonly List<PendingAbilityReaction> _pendingAbilityReactions = [];
    private bool _drainingAbilityReactions;
    private bool _projectingInitialGrants = true;
    private bool _discardStatusEffects;
    private readonly List<PendingSetupEffect> _pendingSetupEffects = [];

    private bool TryApproachAutomaticHealingTarget(BattleUnitState owner, ImmutableArray<AbilityActivationResult> results)
    {
        if (owner.Definition.Behavior.Stationary || results.Any(result => result.Succeeded)) return false;
        foreach (var result in results)
        {
            // Only an otherwise ready, single-operation heal can attribute failure to
            // its empty range. Resource/cooldown/compound conditions must not cause pursuit.
            if (result.Failure != AbilityActivationFailure.ConditionsUnmet ||
                _abilityScope?.Find(owner.RuntimeId, result.AbilityId) is not
                { Trigger: AbilityTriggerKind.ManaFull, AutomaticTarget: AbilityAutomaticTargetKind.WoundedAlly,
                    Operations: [CompiledBattleValueOperation { Action: BattleValueAction.Heal,
                        TargetPolicy: BattleTargetPolicy.Wounded, TargetQuery: CompiledFilteredTargetQuery
                        { Anchor: EffectEntityReference.Owner, Team: EffectRelativeTeam.Allies, Range: > 0 } query }] }) continue;
            if (!ResolveBattleTargets(query, owner, "", BattleTargetPolicy.Wounded).IsEmpty) continue;
            var candidates = ResolveBattleTargets(query with { Range = -1, MaxTargets = 0 }, owner, "", BattleTargetPolicy.Wounded)
                .Select(id => _units.First(unit => unit.RuntimeId == id)).ToArray();
            var target = _movement!.SelectTarget(owner, candidates, query.Range);
            if (target is null) continue;
            SetActionTarget(owner, target);
            owner.LastActionKind = BattleActionKind.Ability;
            owner.Mode = BattleUnitMode.Seeking;
            if (owner.MoveCooldown == 0) _movement.QueueMove(owner);
            return true;
        }
        return false;
    }

    private sealed record PendingSetupEffect(StatusEffectInvocation? Status, Action Execute);

    private void QueueSetupEffect(Action execute, StatusEffectInvocation? status = null)
    {
        if (_pendingSetupEffects.Count >= _combatPipeline.Limits.MaxReactions)
            throw new InvalidOperationException("Initial grant effect budget exceeded.");
        _pendingSetupEffects.Add(new PendingSetupEffect(status, execute));
    }

    private void ExecuteInitialGrantEffects()
    {
        _projectingInitialGrants = false;
        var pending = _pendingSetupEffects.ToArray();
        _pendingSetupEffects.Clear();
        foreach (var effect in pending)
        {
            // Revoked grants cannot retain a queued Applied/reactive effect. Removed effects
            // deliberately keep their captured source after the runtime instance has gone.
            if (effect.Status is { Kind: not StatusEffectInvocationKind.Removed } status &&
                !_units.Any(unit => unit.RuntimeId == status.OwnerId && unit.Alive &&
                    unit.Statuses.Any(instance => instance.InstanceId == status.InstanceId)))
                continue;
            effect.Execute();
            DrainAbilityReactions();
        }
    }

    private float TeamCount(AttributeTeamCountKind kind, int team) => kind switch
    {
        AttributeTeamCountKind.Persistent => _config.Spawns.Count(unit => unit.Team == team && !unit.IsTemporary),
        AttributeTeamCountKind.Deployed => _config.Spawns.Count(unit => unit.Team == team),
        AttributeTeamCountKind.Alive => _units.Count(unit => unit.Team == team && unit.Alive),
        _ => throw new InvalidOperationException("Unsupported team count basis.")
    };

    private TraitSnapshot CurrentTraitSnapshot => _traitScope?.MagnitudeSnapshot ??
        TraitSnapshotBuilder.Build(_config.Traits.Definitions, _config.Traits.Contributions);

    private StatusGrantRuntimeContext CreateStatusGrantContext() => new()
    {
        Tick = () => TickIndex,
        CanReceive = id => _units.Any(unit => unit.RuntimeId == id && unit.Alive),
        Replace = (revoke, grants) => _statusScope.ReplaceGrants(revoke, grants)
    };

    private sealed record PendingAbilityReaction(string OwnerId, AbilityTriggerKind Trigger,
        string TargetId, int Tick, string ChainId, int Depth, bool PassiveGrant = false, BattleCombatEvent? Event = null, CompiledAbilityDefinition? Echo = null);

    private static CombatSourceRef AbilityOrigin(CompiledAbilityDefinition ability, string ownerId) =>
        new(CombatSourceKind.Ability, ability.StableId, ownerId, $"{ownerId}:{ability.StableId}");

    private string PassiveGrantId(string ownerId, string abilityId) =>
        $"ability:{_abilityScope!.ScopeId}:{ownerId}:{abilityId}";

    private void ActivatePassiveGrants(string ownerId)
    {
        if (_abilityScope is null) return;
        foreach (var result in _abilityScope.ActivatePassives(ownerId, TickIndex))
            if (!result.Succeeded)
                throw new InvalidOperationException($"Passive ability '{result.AbilityId}' could not be granted: {result.FailureReason}");
    }

    private void RevokePassiveGrants(string ownerId)
    {
        if (_abilityScope is null || _statusScope.IsCompleted) return;
        foreach (var ability in _abilityScope.Passives(ownerId))
            _statusScope.RevokeGrant(PassiveGrantId(ownerId, ability.StableId));
    }

    private void BindAbilityCombatEvents()
    {
        var bindingSource = CombatSourceRef.System("ability-event-bridge");
        var kinds = new[] { BattleCombatEventKind.AttackLanded, BattleCombatEventKind.SkillHitLanded, BattleCombatEventKind.UnitDefeated,
            BattleCombatEventKind.CriticalHit, BattleCombatEventKind.AttackDodged, BattleCombatEventKind.HealthLost,
            BattleCombatEventKind.ShieldResolved, BattleCombatEventKind.HealingResolved, BattleCombatEventKind.ManaSkillResolved,
            BattleCombatEventKind.ControlApplied, BattleCombatEventKind.UnitRevived };
        foreach (var kind in kinds)
            _abilityCombatSubscriptions.Add(_combatPipeline.Subscribe(kind,bindingSource,0,(combatEvent,sink) =>
            {
                var source = _units.FirstOrDefault(u => u.RuntimeId == combatEvent.SourceRuntimeId);
                var target = _units.FirstOrDefault(u => u.RuntimeId == combatEvent.TargetRuntimeId);
                foreach (var owner in _units.OrderBy(u => u.RuntimeId,StringComparer.Ordinal).ToArray())
                foreach (var trigger in Enum.GetValues<AbilityTriggerKind>())
                {
                    if (_abilityScope?.HasTrigger(owner.RuntimeId,trigger) != true) continue;
                    bool matches = trigger switch {
                        AbilityTriggerKind.AttackHit => kind == BattleCombatEventKind.AttackLanded && source == owner,
                        AbilityTriggerKind.OwnerAttackOrSkillHit => kind is (BattleCombatEventKind.AttackLanded or BattleCombatEventKind.SkillHitLanded) && source == owner,
                        AbilityTriggerKind.AllyTemporaryDefeated => kind == BattleCombatEventKind.UnitDefeated &&
                            target is { IsTemporary: true } && target != owner && target.Team == owner.Team &&
                            combatEvent.Reason is ("" or "Consumed"),
                        AbilityTriggerKind.OwnerDefeated => kind == BattleCombatEventKind.UnitDefeated && target == owner,
                        AbilityTriggerKind.CriticalHit => kind == BattleCombatEventKind.CriticalHit && source == owner,
                        AbilityTriggerKind.DodgedAttack => kind == BattleCombatEventKind.AttackDodged && target == owner,
                        AbilityTriggerKind.ReceivedAttack => kind == BattleCombatEventKind.AttackLanded && target == owner,
                        AbilityTriggerKind.HealthDamaged => kind == BattleCombatEventKind.HealthLost && target == owner && combatEvent.EffectiveValue > 0,
                        AbilityTriggerKind.ShieldReceived => kind == BattleCombatEventKind.ShieldResolved && target == owner && combatEvent.EffectiveValue > 0,
                        AbilityTriggerKind.HealingDone => kind == BattleCombatEventKind.HealingResolved && source == owner && combatEvent.EffectiveValue > 0,
                        AbilityTriggerKind.OverhealReceived => kind == BattleCombatEventKind.HealingResolved && target == owner && combatEvent.AppliedValue > combatEvent.EffectiveValue,
                        AbilityTriggerKind.AllyManaCast => kind == BattleCombatEventKind.ManaSkillResolved && source?.Team == owner.Team,
                        AbilityTriggerKind.OwnerManaCast => kind == BattleCombatEventKind.ManaSkillResolved && source == owner,
                        AbilityTriggerKind.AllyAttackHit => kind is (BattleCombatEventKind.AttackLanded or BattleCombatEventKind.SkillHitLanded) && source?.Team == owner.Team,
                        AbilityTriggerKind.AllyDefeated => kind == BattleCombatEventKind.UnitDefeated && target != owner && target?.Team == owner.Team,
                        AbilityTriggerKind.EnemyDefeated => kind == BattleCombatEventKind.UnitDefeated && target is not null && target.Team != owner.Team,
                        AbilityTriggerKind.ControlApplied => kind == BattleCombatEventKind.ControlApplied && source == owner,
                        AbilityTriggerKind.SummonAttackHit => kind == BattleCombatEventKind.AttackLanded && source?.SummonerRuntimeId == owner.RuntimeId && source.Team == owner.Team,
                        AbilityTriggerKind.OwnerRevived => kind == BattleCombatEventKind.UnitRevived && target == owner,
                        _ => false };
                    if (!matches || !owner.Alive && trigger != AbilityTriggerKind.OwnerDefeated) continue;
                    string counterpart = target == owner ? source?.RuntimeId ?? "" : target?.RuntimeId ?? "";
                    if (!sink.Enqueue(bindingSource,0,context => {
                        if (_pendingAbilityReactions.Count >= _combatPipeline.Limits.MaxReactions)
                            throw new InvalidOperationException("Pending ability reaction budget exceeded.");
                        _pendingAbilityReactions.Add(new PendingAbilityReaction(owner.RuntimeId,trigger,counterpart,
                            combatEvent.Tick,context.ChainId,context.Depth,Event:combatEvent));
                    })) throw new InvalidOperationException("Ability reaction exceeded combat chain budget.");
                }
            }));
    }

    // The combat subscriber only records intent. Execute after the enclosing world transaction
    // has completed, never inside a Status transaction or Effect drain. Each failed reaction
    // rolls back its own invocation while retaining the committed triggering combat fact.
    private void DrainAbilityReactions()
    {
        if (_drainingAbilityReactions || _abilityScope is null) return;
        _drainingAbilityReactions = true;
        try
        {
            var processed = 0;
            while (_pendingAbilityReactions.Count > 0)
            {
                if (++processed > _combatPipeline.Limits.MaxReactions)
                    throw new InvalidOperationException("Ability reaction drain budget exceeded.");
                var pending = _pendingAbilityReactions[0];
                _pendingAbilityReactions.RemoveAt(0);
                using var continuation = _combatPipeline.ContinueReaction(pending.ChainId, pending.Depth);
                if (pending.PassiveGrant)
                {
                    if (_units.Any(unit => unit.RuntimeId == pending.OwnerId && unit.Alive))
                        ActivatePassiveGrants(pending.OwnerId);
                }
                else
                {
                    var previousEvent = _abilityTriggerEvent;
                    var previousEcho = _executingEcho;
                    _abilityTriggerEvent = pending.Event;
                    _executingEcho = pending.Echo is not null;
                    try
                    {
                        if (pending.Echo is { } echo)
                        {
                            var prepared = PrepareAbility(echo,pending.OwnerId,pending.OwnerId,pending.TargetId,TickIndex);
                            if (prepared.Succeeded && prepared.Plan is { } plan) CommitAbility(plan);
                        }
                        else _abilityScope.ActivateTriggered(pending.OwnerId,pending.Trigger,TickIndex,pending.TargetId);
                    }
                    finally { _abilityTriggerEvent = previousEvent; _executingEcho = previousEcho; }
                }
            }
        }
        finally { _drainingAbilityReactions = false; }
    }
}
