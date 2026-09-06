using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
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
        string TargetId, int Tick, string ChainId, int Depth, bool PassiveGrant = false);

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
        var bindingSource = CombatSourceRef.System("ability_event_bridge");
        foreach (var kind in new[] { BattleCombatEventKind.AttackLanded, BattleCombatEventKind.UnitDefeated })
            _abilityCombatSubscriptions.Add(_combatPipeline.Subscribe(kind, bindingSource, 0, (combatEvent, sink) =>
            {
                var defeated = combatEvent.Kind == BattleCombatEventKind.UnitDefeated;
                var ownerId = defeated ? combatEvent.TargetRuntimeId : combatEvent.SourceRuntimeId;
                var targetId = defeated ? combatEvent.SourceRuntimeId : combatEvent.TargetRuntimeId;
                var trigger = defeated ? AbilityTriggerKind.OwnerDefeated : AbilityTriggerKind.AttackHit;
                if (_abilityScope?.HasTrigger(ownerId, trigger) != true)
                    return;
                if (!sink.Enqueue(bindingSource, 0, context =>
                    {
                        if (_pendingAbilityReactions.Count >= _combatPipeline.Limits.MaxReactions)
                            throw new InvalidOperationException("Pending ability reaction budget exceeded.");
                        _pendingAbilityReactions.Add(new PendingAbilityReaction(ownerId, trigger, targetId,
                            combatEvent.Tick, context.ChainId, context.Depth));
                    }))
                    throw new InvalidOperationException("Ability reaction was rejected by the combat chain budget.");
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
                else _abilityScope.ActivateTriggered(pending.OwnerId, pending.Trigger, pending.Tick, pending.TargetId);
            }
        }
        finally { _drainingAbilityReactions = false; }
    }
}
