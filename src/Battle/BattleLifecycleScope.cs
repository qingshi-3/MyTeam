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

internal interface IBattleLifecycleWorld
{
    int Tick { get; }
    bool ExecutingEcho { get; }
    IReadOnlyList<BattleUnitState> Units { get; }
    IReadOnlyDictionary<string, UnitSnapshot> SummonProfiles { get; }
    ImmutableArray<string> ResolveTargets(CompiledEffectTargetQuery query, BattleUnitState owner, string target, BattleTargetPolicy policy);
    ImmutableArray<string> ResolveChannelTargets(CompiledEffectTargetQuery query, string owner, string target);
    bool TargetMatches(BattleTargetPolicy policy, BattleUnitState owner, BattleUnitState target);
    bool HasLineAccess(BattleUnitState owner, BattleUnitState target);
    Vector2? FindOpenNear(Vector2 origin, int team, float radius);
    bool SpawnNear(UnitSnapshot profile, int team, Vector2 origin, float health, float damage, string owner);
    float Heal(string owner, BattleUnitState target, float amount, CombatSourceRef origin);
    void HandleDeath(BattleUnitState? owner, BattleUnitState target);
    void CancelProjectileWindups(BattleUnitState unit);
    void CancelEnemyAction(string id);
    void CancelDisplacement(string id);
    void ReleaseUnit(string id);
    void RearmBodyGrants(string id);
    void RearmPassiveGrants(string id);
    void ChangeTraitTeam(string id, int team);
    CombatSourceRef Source(string id);
    void Resolve(Action work);
    void Publish(BattleCombatEventDraft fact);
    void Present(string type, string source, string target, float value, Vector2 position, string cue);
}

internal sealed class BattleLifecycleScope(IBattleLifecycleWorld world, BattleValueLedger values,
    BattleStatistics statistics, BattleStatusScope statuses, BattleProjectileScope projectiles)
{
    private static CombatCell ToCombatCell(Vector2I cell) => new(cell.X, cell.Y);
    private static CombatPoint ToCombatPoint(Vector2 position) => new(position.X, position.Y);
    private static void SetActionTarget(BattleUnitState owner, BattleUnitState target)
    { owner.ActionTargetRuntimeId = target.RuntimeId; owner.ActionTargetName = target.Definition.DisplayName; }
    private static void ClearActionTarget(BattleUnitState unit)
    { unit.ActionTargetRuntimeId = ""; unit.ActionTargetName = ""; }
    internal sealed class Checkpoint
    {
        private readonly BattleLifecycleScope _owner;
        private readonly LifecycleState _state;
        internal Checkpoint(BattleLifecycleScope owner) { _owner = owner; _state = owner._lifecycles; }
        internal void Restore(BattleLifecycleScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Checkpoint belongs to another lifecycle scope.");
            owner._lifecycles = _state;
        }
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    public void Clear() => _lifecycles = LifecycleState.Empty;
    public bool RevivalPending(string id) => _lifecycles.Revivals.Any(r => r.OwnerId == id);
    public bool CorpseSpent(string id) => _lifecycles.SpentCorpses.Contains(id);
    public void SpendCorpse(string id) => _lifecycles = _lifecycles with { SpentCorpses = _lifecycles.SpentCorpses.Add(id) };
    public bool IsLeased(string id) => _lifecycles.Allegiances.Any(a => a.TargetId == id);
    public bool PreservesTeam(string id, int team) => _lifecycles.Allegiances.Any(a => a.TargetId == id && a.OriginalTeam == team);
    private sealed record Revival(string OwnerId, int ReadyTick, float HealthRatio);
    private sealed record AllegianceLease(string OwnerId, string TargetId, int OriginalTeam, int EndTick, int BeginTick, bool Active, CompiledEffectTargetQuery TargetQuery);
    private sealed record LifecycleState(ImmutableArray<Revival> Revivals, ImmutableArray<AllegianceLease> Allegiances, ImmutableHashSet<string> SpentCorpses)
    { public static LifecycleState Empty => new([], [], ImmutableHashSet<string>.Empty); }
    private LifecycleState _lifecycles = LifecycleState.Empty;
    public bool PrepareLifecycleOperation(CompiledLifecycleOperation operation, BattleUnitState owner, string explicitTarget,
        out ImmutableArray<string> targets)
    {
        targets = [];
        switch (operation.Kind)
        {
            case LifecycleAbilityKind.ReviveOwner:
                if (world.ExecutingEcho || owner.Alive || _lifecycles.SpentCorpses.Contains(owner.RuntimeId) ||
                    _lifecycles.Revivals.Any(r => r.OwnerId == owner.RuntimeId)) return false;
                targets = [owner.RuntimeId]; return true;
            case LifecycleAbilityKind.ConsumeAlly:
                targets = world.ResolveTargets(operation.TargetQuery,owner,explicitTarget,BattleTargetPolicy.Temporary)
                    .Where(id => id != owner.RuntimeId).Take(1).ToImmutableArray();
                return targets.Length > 0;
            case LifecycleAbilityKind.RaiseCorpse:
                if (!world.SummonProfiles.TryGetValue(operation.SummonContentId,out var product) ||
                    world.Units.Count(u => u.Alive && u.SummonerRuntimeId == owner.RuntimeId) >= operation.MaximumLivingSummons ||
                    world.FindOpenNear(owner.Position,owner.Team,product.BodyRadius) is null) return false;
                targets = world.ResolveTargets(operation.TargetQuery,owner,explicitTarget,BattleTargetPolicy.Corpse).Take(1).ToImmutableArray();
                return targets.Length > 0;
            case LifecycleAbilityKind.Charm:
                if (_lifecycles.Allegiances.Any(a => a.OwnerId == owner.RuntimeId)) return false;
                var selection = operation.TargetQuery is CompiledFilteredTargetQuery filter
                    ? filter with { MaxTargets = 0 }
                    : operation.TargetQuery;
                targets = world.ResolveTargets(selection,owner,explicitTarget,BattleTargetPolicy.NonBossEnemy)
                    .Where(id => { var u = world.Units.First(x => x.RuntimeId == id); return u.Health/u.MaxHealth <= operation.HealthRatio && world.HasLineAccess(owner,u); })
                    .Take(1).ToImmutableArray();
                return targets.Length > 0;
            default: return false;
        }
    }

    public void ExecuteLifecycleOperation(CompiledLifecycleOperation operation, ImmutableArray<string> targets,
        BattleUnitState owner, CombatSourceRef origin)
    {
        switch (operation.Kind)
        {
            case LifecycleAbilityKind.ReviveOwner:
                if (owner.Alive || world.ExecutingEcho || _lifecycles.SpentCorpses.Contains(owner.RuntimeId) ||
                    _lifecycles.Revivals.Any(r => r.OwnerId == owner.RuntimeId)) return;
                _lifecycles = _lifecycles with { Revivals = _lifecycles.Revivals.Add(new(owner.RuntimeId,world.Tick+operation.DelayTicks,operation.HealthRatio)) };
                break;
            case LifecycleAbilityKind.ConsumeAlly:
            {
                var donor = world.Units.First(u => u.RuntimeId == targets[0]);
                if (!donor.Alive || donor.Team != owner.Team || !donor.IsTemporary) return;
                // Only named numeric attributes transfer. No equipment, ability, status or roster identity is copied.
                var attack = donor.Damage * operation.AttackTransferRatio;
                var health = donor.MaxHealth * operation.HealthRatio;
                ConsumeTemporaryAlly(owner, donor, origin);
                values.WriteCounter(owner,"eaten_attack",values.ReadCounter(owner,"eaten_attack")+attack);
                values.WriteCounter(owner,"eaten_health",values.ReadCounter(owner,"eaten_health")+health);
                values.SetValueContribution($"consume:attack:{owner.RuntimeId}",owner,owner,CombatAttribute.AttackDamage,values.ReadCounter(owner,"eaten_attack"),false,origin);
                values.SetValueContribution($"consume:health:{owner.RuntimeId}",owner,owner,CombatAttribute.MaxHealth,values.ReadCounter(owner,"eaten_health"),false,origin);
                world.Heal(owner.RuntimeId,owner,health,origin);
                break;
            }
            case LifecycleAbilityKind.RaiseCorpse:
            {
                var corpse = world.Units.First(u => u.RuntimeId == targets[0]);
                if (corpse.Alive || corpse.Team == owner.Team || corpse.Definition.IsBoss ||
                    _lifecycles.SpentCorpses.Contains(corpse.RuntimeId) ||
                    _lifecycles.Revivals.Any(r => r.OwnerId == corpse.RuntimeId)) return;
                if (!world.SummonProfiles.TryGetValue(operation.SummonContentId,out var product) ||
                    world.Units.Count(u => u.Alive && u.SummonerRuntimeId == owner.RuntimeId) >= operation.MaximumLivingSummons ||
                    !world.SpawnNear(product,owner.Team,owner.Position,1,1,owner.RuntimeId))
                    throw new InvalidOperationException("Corpse product has no legal position.");
                _lifecycles = _lifecycles with { SpentCorpses = _lifecycles.SpentCorpses.Add(corpse.RuntimeId) };
                values.WriteCounter(owner,"raised_corpses",values.ReadCounter(owner,"raised_corpses")+1);
                break;
            }
            case LifecycleAbilityKind.Charm:
            {
                var target = world.Units.First(u => u.RuntimeId == targets[0]);
                if (!world.TargetMatches(BattleTargetPolicy.NonBossEnemy,owner,target) ||
                    target.Health/target.MaxHealth > operation.HealthRatio ||
                    _lifecycles.Allegiances.Any(a => a.OwnerId == owner.RuntimeId)) return;
                _lifecycles = _lifecycles with { Allegiances = _lifecycles.Allegiances.Add(new(owner.RuntimeId,target.RuntimeId,
                    target.Team,world.Tick+operation.DelayTicks+operation.DurationTicks,world.Tick+operation.DelayTicks,false,operation.TargetQuery)) };
                world.CancelProjectileWindups(owner);
                owner.ProjectileSequence = null;
                owner.ManaLockedUntilTick = Math.Max(owner.ManaLockedUntilTick,world.Tick+operation.DelayTicks);
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
        statistics.Defeat(donor.RuntimeId, world.Tick);
        ClearActionTarget(donor);
        world.CancelProjectileWindups(donor);
        donor.ProjectileSequence = null;
        world.Publish(new BattleCombatEventDraft(BattleCombatEventKind.UnitDefeated,origin,
            owner.RuntimeId,donor.RuntimeId,world.Tick,Cell:ToCombatCell(donor.Cell),Reason:"Consumed",
            Position:ToCombatPoint(donor.Position)));
        world.Present("defeated",owner.RuntimeId,donor.RuntimeId,0,donor.Position,"defeated");
        world.HandleDeath(null,donor);
    }

    public void AdvanceBattleLifecycles()
    {
        // Changes are in the same world transaction as status grants, navigation and presentation facts.
        if (_lifecycles.Revivals.IsEmpty && _lifecycles.Allegiances.IsEmpty &&
            !values.HasDeadBroadcasts) return;
        world.Resolve(() =>
        {
            values.CleanDeadBroadcasts();
            foreach (var pending in _lifecycles.Revivals.Where(r => r.ReadyTick <= world.Tick).ToArray())
            {
                var unit = world.Units.First(u => u.RuntimeId == pending.OwnerId);
                if (unit.Alive) { _lifecycles = _lifecycles with { Revivals = _lifecycles.Revivals.Remove(pending) }; continue; }
                var position = world.FindOpenNear(unit.Position,unit.Team,unit.BodyRadius);
                if (position is null) continue; // Reserve the same identity until a legal footprint becomes free.
                _lifecycles = _lifecycles with { Revivals = _lifecycles.Revivals.Remove(pending) };
                values.RecordBattleRevival(unit);
                unit.Position = position.Value;
                unit.Health = unit.MaxHealth * pending.HealthRatio;
                unit.Shield = 0; unit.DisabledTicks = 0; unit.AttackCooldown = unit.EffectiveAttackTicks;
                unit.AttackCycleProgress = 0; unit.WaitingTicks = 0;
                unit.MoveCooldown = 0; unit.ManaLockedUntilTick = world.Tick+1;
                unit.CurrentMana = 0; unit.Mode = BattleUnitMode.Seeking;
                unit.LastActionKind = BattleActionKind.None; ClearActionTarget(unit);
                unit.ProjectileSequence = null; unit.ProjectileWindups = [];
                world.ReleaseUnit(unit.RuntimeId);
                world.RearmBodyGrants(unit.RuntimeId);
                unit.Health = unit.MaxHealth * pending.HealthRatio;
                world.RearmPassiveGrants(unit.RuntimeId);
                values.WriteCounter(unit,"revivals",values.ReadCounter(unit,"revivals")+1);
                world.Present("revived",unit.RuntimeId,unit.RuntimeId,unit.Health,unit.Position,"idle");
                world.Publish(new BattleCombatEventDraft(BattleCombatEventKind.UnitRevived,world.Source(unit.RuntimeId),unit.RuntimeId,unit.RuntimeId,world.Tick,EffectiveValue:unit.Health));
            }
            foreach (var lease in _lifecycles.Allegiances.ToArray())
            {
                if (!_lifecycles.Allegiances.Contains(lease)) continue;
                var owner = world.Units.First(u => u.RuntimeId == lease.OwnerId);
                var target = world.Units.First(u => u.RuntimeId == lease.TargetId);
                var interrupted = !owner.Alive || !target.Alive || owner.Team == lease.OriginalTeam;
                if (!lease.Active)
                    interrupted |= owner.DisabledTicks > 0 || statuses.HasTag(owner.RuntimeId,StatusDefinitionCompiler.ActionDisabledTag) ||
                        target.Team != lease.OriginalTeam || !IsChannelTargetInRange(lease,owner,target) || !world.HasLineAccess(owner,target);
                else interrupted |= target.Team != owner.Team;
                if (interrupted || world.Tick >= lease.EndTick)
                {
                    EndAllegiance(lease);
                }
                else if (!lease.Active && world.Tick >= lease.BeginTick)
                {
                    ChangeAllegiance(target,owner.Team,owner.RuntimeId);
                    _lifecycles = _lifecycles with { Allegiances = _lifecycles.Allegiances.Replace(lease,lease with { Active = true }) };
                }
            }
        });
    }

    private bool IsChannelTargetInRange(AllegianceLease lease, BattleUnitState owner, BattleUnitState target)
    {
        // Continue following the chosen body. A newly closer candidate cannot steal an
        // established channel; the authored area still controls whether it stays connected.
        var query = lease.TargetQuery is CompiledFilteredTargetQuery filter
            ? filter with { MaxTargets = 0 }
            : lease.TargetQuery;
        return world.ResolveChannelTargets(query, owner.RuntimeId, target.RuntimeId)
            .Contains(target.RuntimeId);
    }

    private void EndAllegiance(AllegianceLease lease)
    {
        if (!_lifecycles.Allegiances.Contains(lease)) return;
        // Remove the lease first: reverting one controller can invalidate its own leases.
        _lifecycles = _lifecycles with { Allegiances = _lifecycles.Allegiances.Remove(lease) };
        var owner = world.Units.FirstOrDefault(u => u.RuntimeId == lease.OwnerId);
        if (!lease.Active)
        {
            if (owner is not null && owner.ManaLockedUntilTick == lease.BeginTick)
                owner.ManaLockedUntilTick = world.Tick;
            if (owner?.ActionTargetRuntimeId == lease.TargetId) ClearActionTarget(owner);
            return;
        }
        var target = world.Units.FirstOrDefault(u => u.RuntimeId == lease.TargetId);
        if (target is not null) ChangeAllegiance(target,lease.OriginalTeam,lease.OwnerId);
    }

    public void ReleaseBattleLifecycleBindings(string runtimeId)
    {
        foreach (var lease in _lifecycles.Allegiances.Where(a => a.OwnerId == runtimeId || a.TargetId == runtimeId).ToArray())
            EndAllegiance(lease);
        values.ReleaseSource(runtimeId);
    }

    public void InterruptBattleChannels(string runtimeId)
    {
        world.CancelEnemyAction(runtimeId);
        foreach (var lease in _lifecycles.Allegiances.Where(a => !a.Active && a.OwnerId == runtimeId).ToArray())
            EndAllegiance(lease);
    }

    private void ChangeAllegiance(BattleUnitState target, int team, string sourceId)
    {
        if (target.Team == team) return;
        world.CancelDisplacement(target.RuntimeId);
        foreach (var lease in _lifecycles.Allegiances.Where(a => a.OwnerId == target.RuntimeId).ToArray())
            EndAllegiance(lease);
        values.ReleaseAllegiance(target.RuntimeId);
        projectiles.CancelOwner(target.RuntimeId);
        target.Team = team;
        world.CancelProjectileWindups(target); target.ProjectileSequence = null;
        ClearActionTarget(target); world.ReleaseUnit(target.RuntimeId);
        foreach (var unit in world.Units.Where(u => u.ActionTargetRuntimeId == target.RuntimeId)) ClearActionTarget(unit);
        world.ChangeTraitTeam(target.RuntimeId,team);
        world.Present("allegiance",sourceId,target.RuntimeId,team,target.Position,"idle");
        world.Publish(new BattleCombatEventDraft(BattleCombatEventKind.AllegianceChanged,world.Source(sourceId),sourceId,target.RuntimeId,world.Tick,EffectiveValue:team));
    }
}
