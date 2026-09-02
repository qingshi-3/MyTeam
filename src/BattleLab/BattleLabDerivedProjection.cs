using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

namespace TowerAutobattler.BattleLab;

public sealed record BattleLabPreparedUnitProjection(
    string InstanceId,
    string ContentId,
    BattleLabSide Side,
    float Health,
    float Damage,
    float AttackSpeed,
    float Reach,
    float ControlResistance,
    ImmutableArray<BattleLabEquipmentConfiguration> Equipment,
    ImmutableArray<TraitContributionSnapshot> TraitContributions,
    ImmutableArray<StatusRuntimeSnapshot> Statuses);

public sealed record BattleLabDerivedProjection(
    bool IsReady,
    int PlayerCount,
    int EnemyCount,
    int PopulationUsed,
    int CurrentPopulation,
    ImmutableArray<string> RejectionReasons,
    ImmutableDictionary<string, BattleLabPreparedUnitProjection> Units,
    ImmutableArray<TraitPresentationSnapshot> Traits,
    ImmutableArray<BattleLabRelicConfiguration> Relics);

public static class BattleLabDerivedProjectionBuilder
{
    public static BattleLabDerivedProjection Build(BattleLabSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var snapshot = session.Freeze();
        var reasons = ImmutableArray.CreateBuilder<string>();
        var players = snapshot.Units.Count(unit => unit.Side == BattleLabSide.Player);
        var enemies = snapshot.Units.Length - players;
        if (players == 0) reasons.Add("至少需要一个我方英雄。");
        if (enemies == 0) reasons.Add("至少需要一个敌方单位。");
        if (snapshot.Mode == BattleLabPlacementMode.Formal && players > snapshot.CurrentPopulation)
            reasons.Add("我方部署数量超过当前人口。");
        var unitFacts = ImmutableDictionary.CreateBuilder<string, BattleLabPreparedUnitProjection>(StringComparer.Ordinal);
        var traitFacts = ImmutableArray<TraitPresentationSnapshot>.Empty;
        if (snapshot.Units.Length > 0)
        {
            try
            {
                var authoredById = snapshot.Units.ToDictionary(
                    unit => unit.InstanceId,
                    StringComparer.Ordinal);
                var config = new BattleLabPreparationAdapter(session.Content).BuildProjection(snapshot);
                using var simulation = new BattleSimulation(config);
                foreach (var state in simulation.Units)
                {
                    if (!authoredById.TryGetValue(state.SourceInstanceId, out var authored)) continue;
                    var authoredSources = authored.Equipment.Select(item => item.InstanceId)
                        .Append(authored.InstanceId)
                        .ToHashSet(StringComparer.Ordinal);
                    var contributions = simulation.TraitSnapshot.Contributions
                        .Where(contribution => !contribution.IsTemporary && contribution.Team == state.Team &&
                            (contribution.OwnerRuntimeId == authored.InstanceId ||
                             contribution.OwnerRuntimeId == state.RuntimeId ||
                             authoredSources.Contains(contribution.SourceInstanceId)))
                        .OrderBy(contribution => contribution.TraitId, StringComparer.Ordinal)
                        .ThenBy(contribution => contribution.SourceKind)
                        .ThenBy(contribution => contribution.SourceInstanceId, StringComparer.Ordinal)
                        .ToImmutableArray();
                    unitFacts.Add(authored.InstanceId, new BattleLabPreparedUnitProjection(
                        authored.InstanceId,
                        authored.ContentId,
                        authored.Side,
                        state.MaxHealth,
                        state.Damage,
                        state.Attributes.GetValue(CombatAttribute.AttackSpeed),
                        state.Attributes.GetValue(CombatAttribute.AttackRange),
                        state.Attributes.GetValue(CombatAttribute.ControlResistance),
                        authored.Equipment,
                        contributions,
                        state.Statuses));
                }
                traitFacts = simulation.TraitSnapshot.Values.Select(value => value.Presentation).ToImmutableArray();
            }
            catch (Exception exception)
            {
                reasons.Add(exception.Message);
            }
        }
        return new BattleLabDerivedProjection(
            reasons.Count == 0,
            players,
            enemies,
            players,
            snapshot.CurrentPopulation,
            reasons.Distinct(StringComparer.Ordinal).ToImmutableArray(),
            unitFacts.ToImmutable(),
            traitFacts,
            snapshot.Relics);
    }
}
