using System;
using System.Linq;
using System.Collections.Immutable;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Statuses;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private bool PrepareLifecycleOperation(CompiledLifecycleOperation operation, BattleUnitState owner, string explicitTarget,
        out ImmutableArray<string> targets)
    {
        targets = [];
        switch (operation.Kind)
        {
            case LifecycleAbilityKind.ReviveOwner:
                if (_executingEcho || owner.Alive || _mechanics.SpentCorpses.Contains(owner.RuntimeId) ||
                    _mechanics.Revivals.Any(r => r.OwnerId == owner.RuntimeId)) return false;
                targets = [owner.RuntimeId]; return true;
            case LifecycleAbilityKind.ConsumeAlly:
                targets = ResolveBattleTargets(operation.TargetQuery,owner,explicitTarget,BattleTargetPolicy.Temporary)
                    .Where(id => id != owner.RuntimeId).Take(1).ToImmutableArray();
                return targets.Length > 0;
            case LifecycleAbilityKind.RaiseCorpse:
                if (!_config.TacticalSummons.TryGetValue(operation.SummonContentId,out var product) ||
                    _units.Count(u => u.Alive && u.SummonerRuntimeId == owner.RuntimeId) >= operation.MaximumLivingSummons ||
                    FindOpenNear(owner.Position,owner.Team,product.BodyRadius) is null) return false;
                targets = ResolveBattleTargets(operation.TargetQuery,owner,explicitTarget,BattleTargetPolicy.Corpse).Take(1).ToImmutableArray();
                return targets.Length > 0;
            case LifecycleAbilityKind.Charm:
                if (_mechanics.Allegiances.Any(a => a.OwnerId == owner.RuntimeId)) return false;
                var selection = operation.TargetQuery is CompiledFilteredTargetQuery filter
                    ? filter with { MaxTargets = 0 }
                    : operation.TargetQuery;
                targets = ResolveBattleTargets(selection,owner,explicitTarget,BattleTargetPolicy.NonBossEnemy)
                    .Where(id => { var u = _units.First(x => x.RuntimeId == id); return u.Health/u.MaxHealth <= operation.HealthRatio && HasLineAccess(owner,u); })
                    .Take(1).ToImmutableArray();
                return targets.Length > 0;
            default: return false;
        }
    }

    private void ExecuteLifecycleOperation(CompiledLifecycleOperation operation, ImmutableArray<string> targets,
        BattleUnitState owner, CombatSourceRef origin)
    {
        switch (operation.Kind)
        {
            case LifecycleAbilityKind.ReviveOwner:
                if (owner.Alive || _executingEcho || _mechanics.SpentCorpses.Contains(owner.RuntimeId) ||
                    _mechanics.Revivals.Any(r => r.OwnerId == owner.RuntimeId)) return;
                _mechanics = _mechanics with { Revivals = _mechanics.Revivals.Add(new(owner.RuntimeId,TickIndex+operation.DelayTicks,operation.HealthRatio)) };
                break;
            case LifecycleAbilityKind.ConsumeAlly:
            {
                var donor = _units.First(u => u.RuntimeId == targets[0]);
                if (!donor.Alive || donor.Team != owner.Team || !donor.IsTemporary) return;
                // Only named numeric attributes transfer. No equipment, ability, status or roster identity is copied.
                var attack = donor.Damage * operation.AttackTransferRatio;
                var health = donor.MaxHealth * operation.HealthRatio;
                ConsumeTemporaryAlly(owner, donor, origin);
                WriteCounter(owner,"eaten_attack",ReadCounter(owner,"eaten_attack")+attack);
                WriteCounter(owner,"eaten_health",ReadCounter(owner,"eaten_health")+health);
                SetValueContribution($"consume:attack:{owner.RuntimeId}",owner,owner,CombatAttribute.AttackDamage,ReadCounter(owner,"eaten_attack"),false,origin);
                SetValueContribution($"consume:health:{owner.RuntimeId}",owner,owner,CombatAttribute.MaxHealth,ReadCounter(owner,"eaten_health"),false,origin);
                HealLiving(owner.RuntimeId,owner,health,origin);
                break;
            }
            case LifecycleAbilityKind.RaiseCorpse:
            {
                var corpse = _units.First(u => u.RuntimeId == targets[0]);
                if (corpse.Alive || corpse.Team == owner.Team || corpse.Definition.IsBoss ||
                    _mechanics.SpentCorpses.Contains(corpse.RuntimeId) ||
                    _mechanics.Revivals.Any(r => r.OwnerId == corpse.RuntimeId)) return;
                if (!_config.TacticalSummons.TryGetValue(operation.SummonContentId,out var product) ||
                    _units.Count(u => u.Alive && u.SummonerRuntimeId == owner.RuntimeId) >= operation.MaximumLivingSummons ||
                    !SpawnTemporaryNear(product,owner.Team,owner.Position,1,1,owner.RuntimeId))
                    throw new InvalidOperationException("Corpse product has no legal position.");
                _mechanics = _mechanics with { SpentCorpses = _mechanics.SpentCorpses.Add(corpse.RuntimeId) };
                WriteCounter(owner,"raised_corpses",ReadCounter(owner,"raised_corpses")+1);
                break;
            }
            case LifecycleAbilityKind.Charm:
            {
                var target = _units.First(u => u.RuntimeId == targets[0]);
                if (!TargetMatches(BattleTargetPolicy.NonBossEnemy,owner,target) ||
                    target.Health/target.MaxHealth > operation.HealthRatio ||
                    _mechanics.Allegiances.Any(a => a.OwnerId == owner.RuntimeId)) return;
                _mechanics = _mechanics with { Allegiances = _mechanics.Allegiances.Add(new(owner.RuntimeId,target.RuntimeId,
                    target.Team,TickIndex+operation.DelayTicks+operation.DurationTicks,TickIndex+operation.DelayTicks,false,operation.TargetQuery)) };
                CancelProjectileWindups(owner);
                owner.ProjectileSequence = null;
                owner.ManaLockedUntilTick = Math.Max(owner.ManaLockedUntilTick,TickIndex+operation.DelayTicks);
                SetActionTarget(owner,target);
                break;
            }
        }
    }

    private void ConsumeTemporaryAlly(BattleUnitState owner, BattleUnitState donor, CombatSourceRef origin)
    {
        // Consumption is an explicit sacrifice, not an arbitrarily huge damage hit.
        // It supplies a death event but grants no ordinary-hit, damage or enemy-kill credit.
        donor.Health = 0;
        donor.Shield = 0;
        donor.Mode = BattleUnitMode.Defeated;
        donor.LastActionKind = BattleActionKind.None;
        donor.WaitingTicks = 0;
        _statistics[donor.RuntimeId].DefeatTick ??= TickIndex;
        ClearActionTarget(donor);
        CancelProjectileWindups(donor);
        donor.ProjectileSequence = null;
        PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.UnitDefeated,origin,
            owner.RuntimeId,donor.RuntimeId,TickIndex,Cell:ToCombatCell(donor.Cell),Reason:"Consumed",
            Position:ToCombatPoint(donor.Position)));
        Emit("defeated",owner.RuntimeId,donor.RuntimeId,0,donor.Position,"defeated");
        HandleDeath(null,donor);
    }

    private void AdvanceBattleLifecycles()
    {
        // Changes are in the same world transaction as status grants, navigation and presentation facts.
        if (_mechanics.Revivals.IsEmpty && _mechanics.Allegiances.IsEmpty &&
            !_mechanics.Contributions.Values.Any(c => c.Broadcast && !_units.Any(u => u.RuntimeId == c.SourceId && u.Alive))) return;
        var checkpoint = new BattleWorldStateCheckpoint(this);
        try
        {
            using var resolution = _combatPipeline.BeginAuthoritativeResolution();
            foreach (var pair in _mechanics.Contributions.Where(p => p.Value.Broadcast &&
                         !_units.Any(u => u.RuntimeId == p.Value.SourceId && u.Alive)).ToArray())
            {
                _units.First(u => u.RuntimeId == pair.Value.TargetId).Attributes.Remove(pair.Value.Handle);
                _mechanics = _mechanics with { Contributions = _mechanics.Contributions.Remove(pair.Key) };
            }
            foreach (var pending in _mechanics.Revivals.Where(r => r.ReadyTick <= TickIndex).ToArray())
            {
                var unit = _units.First(u => u.RuntimeId == pending.OwnerId);
                if (unit.Alive) { _mechanics = _mechanics with { Revivals = _mechanics.Revivals.Remove(pending) }; continue; }
                var position = FindOpenNear(unit.Position,unit.Team,unit.BodyRadius);
                if (position is null) continue; // Reserve the same identity until a legal footprint becomes free.
                _mechanics = _mechanics with { Revivals = _mechanics.Revivals.Remove(pending) };
                RecordBattleRevival(unit);
                unit.Position = position.Value;
                unit.Health = unit.MaxHealth * pending.HealthRatio;
                unit.Shield = 0; unit.DisabledTicks = 0; unit.AttackCooldown = unit.EffectiveAttackTicks;
                unit.AttackCycleProgress = 0; unit.WaitingTicks = 0;
                unit.MoveCooldown = 0; unit.ManaLockedUntilTick = TickIndex+1;
                unit.CurrentMana = 0; unit.Mode = BattleUnitMode.Seeking;
                unit.LastActionKind = BattleActionKind.None; ClearActionTarget(unit);
                unit.ProjectileSequence = null; unit.ProjectileWindups = [];
                _movement?.ReleaseUnit(unit.RuntimeId);
                _deathProcUnits.Remove(unit.RuntimeId);
                _traitScope?.RearmOwnerGrants(unit.RuntimeId);
                _equipmentScope?.RearmOwnerGrants(unit.RuntimeId);
                unit.Health = unit.MaxHealth * pending.HealthRatio;
                _abilityScope?.RearmPassiveGrants(unit.RuntimeId);
                _pendingAbilityReactions.Add(new PendingAbilityReaction(unit.RuntimeId,AbilityTriggerKind.None,"",TickIndex,
                    $"{_combatPipeline.ScopeId}:revive:{unit.RuntimeId}:{TickIndex}",0,PassiveGrant:true));
                WriteCounter(unit,"revivals",ReadCounter(unit,"revivals")+1);
                Emit("revived",unit.RuntimeId,unit.RuntimeId,unit.Health,unit.Position,"idle");
                PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.UnitRevived,ResolveCombatSource(unit.RuntimeId),unit.RuntimeId,unit.RuntimeId,TickIndex,EffectiveValue:unit.Health));
            }
            foreach (var lease in _mechanics.Allegiances.ToArray())
            {
                if (!_mechanics.Allegiances.Contains(lease)) continue;
                var owner = _units.First(u => u.RuntimeId == lease.OwnerId);
                var target = _units.First(u => u.RuntimeId == lease.TargetId);
                var interrupted = !owner.Alive || !target.Alive || owner.Team == lease.OriginalTeam;
                if (!lease.Active)
                    interrupted |= owner.DisabledTicks > 0 || _statusScope.HasTag(owner.RuntimeId,StatusDefinitionCompiler.ActionDisabledTag) ||
                        target.Team != lease.OriginalTeam || !IsChannelTargetInRange(lease,owner,target) || !HasLineAccess(owner,target);
                else interrupted |= target.Team != owner.Team;
                if (interrupted || TickIndex >= lease.EndTick)
                {
                    EndAllegiance(lease);
                }
                else if (!lease.Active && TickIndex >= lease.BeginTick)
                {
                    ChangeAllegiance(target,owner.Team,owner.RuntimeId);
                    _mechanics = _mechanics with { Allegiances = _mechanics.Allegiances.Replace(lease,lease with { Active = true }) };
                }
            }
            resolution.Commit();
            checkpoint.Commit();
        }
        catch { checkpoint.Rollback(); throw; }
    }

    private bool IsChannelTargetInRange(AllegianceLease lease, BattleUnitState owner, BattleUnitState target)
    {
        // Continue following the chosen body. A newly closer candidate cannot steal an
        // established channel; the authored area still controls whether it stays connected.
        var query = lease.TargetQuery is CompiledFilteredTargetQuery filter
            ? filter with { MaxTargets = 0 }
            : lease.TargetQuery;
        return ResolveAbilityTargets(query,CaptureAbilitySnapshot(TickIndex),owner.RuntimeId,owner.RuntimeId,target.RuntimeId)
            .Contains(target.RuntimeId);
    }

    private void EndAllegiance(AllegianceLease lease)
    {
        if (!_mechanics.Allegiances.Contains(lease)) return;
        // Remove the lease first: reverting one controller can invalidate its own leases.
        _mechanics = _mechanics with { Allegiances = _mechanics.Allegiances.Remove(lease) };
        var owner = _units.FirstOrDefault(u => u.RuntimeId == lease.OwnerId);
        if (!lease.Active)
        {
            if (owner is not null && owner.ManaLockedUntilTick == lease.BeginTick)
                owner.ManaLockedUntilTick = TickIndex;
            if (owner?.ActionTargetRuntimeId == lease.TargetId) ClearActionTarget(owner);
            return;
        }
        var target = _units.FirstOrDefault(u => u.RuntimeId == lease.TargetId);
        if (target is not null) ChangeAllegiance(target,lease.OriginalTeam,lease.OwnerId);
    }

    private void ReleaseBattleLifecycleBindings(string runtimeId)
    {
        foreach (var lease in _mechanics.Allegiances.Where(a => a.OwnerId == runtimeId || a.TargetId == runtimeId).ToArray())
            EndAllegiance(lease);
        foreach (var pair in _mechanics.Contributions.Where(p => p.Value.Broadcast && p.Value.SourceId == runtimeId).ToArray())
        {
            _units.First(u => u.RuntimeId == pair.Value.TargetId).Attributes.Remove(pair.Value.Handle);
            _mechanics = _mechanics with { Contributions = _mechanics.Contributions.Remove(pair.Key) };
        }
    }

    private void InterruptBattleChannels(string runtimeId)
    {
        CancelEnemyAction(runtimeId);
        foreach (var lease in _mechanics.Allegiances.Where(a => !a.Active && a.OwnerId == runtimeId).ToArray())
            EndAllegiance(lease);
    }

    private void ChangeAllegiance(BattleUnitState target, int team, string sourceId)
    {
        if (target.Team == team) return;
        CancelDisplacement(target.RuntimeId);
        foreach (var lease in _mechanics.Allegiances.Where(a => a.OwnerId == target.RuntimeId).ToArray())
            EndAllegiance(lease);
        foreach (var pair in _mechanics.Contributions.Where(p => p.Value.Broadcast &&
                     (p.Value.SourceId == target.RuntimeId || p.Value.TargetId == target.RuntimeId)).ToArray())
        {
            _units.First(u => u.RuntimeId == pair.Value.TargetId).Attributes.Remove(pair.Value.Handle);
            _mechanics = _mechanics with { Contributions = _mechanics.Contributions.Remove(pair.Key) };
        }
        CancelAllegianceProjectiles(target);
        target.Team = team;
        CancelProjectileWindups(target); target.ProjectileSequence = null;
        ClearActionTarget(target); _movement?.ReleaseUnit(target.RuntimeId);
        foreach (var unit in _units.Where(u => u.ActionTargetRuntimeId == target.RuntimeId)) ClearActionTarget(unit);
        _traitScope?.ChangeOwnerTeam(target.RuntimeId,team);
        Emit("allegiance",sourceId,target.RuntimeId,team,target.Position,"idle");
        PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.AllegianceChanged,ResolveCombatSource(sourceId),sourceId,target.RuntimeId,TickIndex,EffectiveValue:team));
    }
}
