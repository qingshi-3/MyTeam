using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Battle;

internal interface IBattleAbilitySummonWorld
{
    IReadOnlyList<BattleUnitView> Units { get; }
    UnitSnapshot? ResolveProfile(CompiledSummonAbilityOperation summon, string ownerId);
    bool TryFindOpenNear(Vector2 origin, int team, float radius, IReadOnlyList<(Vector2 Position, float Radius)> reserved, out Vector2 position);
    bool CanOccupy(Vector2 position, float radius, IReadOnlyList<(Vector2 Position, float Radius)> reserved);
    bool Spawn(UnitSnapshot profile, int team, Vector2 position, float health, float damage, string sourceId);
    void SpawnNear(UnitSnapshot profile, int team, Vector2 origin, float health, float damage, string sourceId);
    bool ConsumeExtraSummon(string ownerId);
}

internal sealed class BattleAbilitySummons(IBattleAbilitySummonWorld world)
{
    // Per-preparation state, never shared across abilities or stored on the battle.
    internal sealed class Reservations
    {
        internal readonly List<AbilitySummonReservation> Items = [];
        internal readonly List<(Vector2 Position, float Radius)> Space = [];
    }

    public BattleAbilityOperationPreparation Prepare(CompiledSummonAbilityOperation summon,
        BattleAbilityInvocation invocation, int index, Reservations reserved)
    {
        var owner = world.Units.First(unit => unit.RuntimeId == invocation.OwnerId);
        var profile = world.ResolveProfile(summon, invocation.OwnerId);
        if (profile is null) return BattleAbilityOperationPreparation.Reject("没有可用的召唤单位。");
        var living = Living(summon, owner);
        var available = summon.MaximumLivingTemporaryUnits <= 0 ? summon.Count : Math.Max(0, summon.MaximumLivingTemporaryUnits - living);
        for (var sequence = 0; sequence < Math.Min(summon.Count, available); sequence++)
        {
            if (!world.TryFindOpenNear(owner.Position, owner.Team, profile.BodyRadius, reserved.Space, out var position)) break;
            reserved.Space.Add((position, profile.BodyRadius));
            reserved.Items.Add(new(index, summon.Profile, sequence, position.X, position.Y, summon.HealthMultiplier, summon.DamageMultiplier));
        }
        if (summon.RequireAtLeastOne && !reserved.Items.Any(item => item.OperationIndex == index))
            return BattleAbilityOperationPreparation.Reject("没有可用的召唤单位或合法落点。");
        return new([]);
    }

    public string Validate(AbilityExecutionPlan plan)
    {
        var reserved = new List<(Vector2 Position, float Radius)>();
        foreach (var reservation in plan.Summons.OrderBy(item => item.OperationIndex).ThenBy(item => item.Sequence))
        {
            var summon = (CompiledSummonAbilityOperation)plan.Ability.Operations[reservation.OperationIndex];
            var profile = world.ResolveProfile(summon, plan.OwnerId);
            if (profile is null) return "召唤模板在提交前失效。";
            var position = new Vector2(reservation.PositionX, reservation.PositionY);
            if (!world.CanOccupy(position, profile.BodyRadius, reserved)) return "召唤落点在提交前失效。";
            reserved.Add((position, profile.BodyRadius));
        }
        return "";
    }

    public bool Execute(CompiledSummonAbilityOperation summon, ResolvedAbilityOperation resolved,
        AbilityExecutionPlan plan, ImmutableArray<string>.Builder facts)
    {
        var owner = world.Units.First(unit => unit.RuntimeId == plan.OwnerId);
        if (!owner.Alive && plan.Ability.Trigger != AbilityTriggerKind.OwnerDefeated) return false;
        var spawned = false;
        foreach (var reservation in plan.Summons.Where(item => item.OperationIndex == resolved.OperationIndex).OrderBy(item => item.Sequence))
        {
            var profile = world.ResolveProfile(summon, plan.OwnerId)!;
            if (!world.Spawn(profile, owner.Team, new(reservation.PositionX, reservation.PositionY),
                    reservation.HealthMultiplier, reservation.DamageMultiplier, plan.SourceId))
                throw new InvalidOperationException("Prepared ability summon failed during commit.");
            facts.Add($"Summon:{profile.ContentId}:{reservation.PositionX:R},{reservation.PositionY:R}");
            spawned = true;
        }
        return spawned;
    }

    public void CompleteBonuses(AbilityExecutionPlan plan, IEnumerable<int> successfulOperations)
    {
        var owner = world.Units.First(unit => unit.RuntimeId == plan.OwnerId);
        // All authored reservations have materialized before a trait can consume free space.
        foreach (var index in successfulOperations.OrderBy(index => index))
        {
            if (!world.ConsumeExtraSummon(plan.OwnerId)) break;
            var summon = (CompiledSummonAbilityOperation)plan.Ability.Operations[index];
            if (summon.MaximumLivingTemporaryUnits > 0 && Living(summon, owner) >= summon.MaximumLivingTemporaryUnits) continue;
            var profile = world.ResolveProfile(summon, plan.OwnerId)!;
            world.SpawnNear(profile, owner.Team, owner.Position, summon.HealthMultiplier, summon.DamageMultiplier, plan.SourceId);
        }
    }

    private int Living(CompiledSummonAbilityOperation summon, BattleUnitView owner) => world.Units.Count(unit =>
        unit.Alive && unit.IsTemporary && unit.Team == owner.Team && (!summon.LimitPerOwner || unit.SummonerRuntimeId == owner.RuntimeId));
}
