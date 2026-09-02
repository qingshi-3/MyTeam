using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;

namespace TowerAutobattler.BattleLab;

public sealed class BattleLabPreparationAdapter
{
    private readonly BattleLabContentIndex _index;

    public BattleLabPreparationAdapter(BattleLabContentIndex index) =>
        _index = index ?? throw new ArgumentNullException(nameof(index));

    public BattleConfig Build(BattleLabStartSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateSnapshot(snapshot);
        var playerUnits = snapshot.Units.Where(unit => unit.Side == BattleLabSide.Player).ToArray();
        if (playerUnits.Length == 0) throw new InvalidOperationException("战斗实验室至少需要一个我方英雄。");
        if (!snapshot.Units.Any(unit => unit.Side == BattleLabSide.Enemy))
            throw new InvalidOperationException("战斗实验室至少需要一个敌方单位。");

        return BuildPreparedConfig(snapshot, snapshot.PrimaryHeroInstanceId, playerUnits.Length);
    }

    public BattleConfig BuildProjection(BattleLabStartSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateSnapshot(snapshot);
        var playerUnits = snapshot.Units.Where(unit => unit.Side == BattleLabSide.Player).ToArray();
        var primaryInstanceId = playerUnits.Length > 0
            ? snapshot.PrimaryHeroInstanceId
            : string.Empty;
        return BuildPreparedConfig(snapshot, primaryInstanceId, playerUnits.Length);
    }

    private BattleConfig BuildPreparedConfig(
        BattleLabStartSnapshot snapshot,
        string primaryInstanceId,
        int playerCount)
    {
        var primaryContentId = string.IsNullOrWhiteSpace(primaryInstanceId)
            ? _index.PlayerHeroes.FirstOrDefault()?.StableId ?? throw new InvalidOperationException(
                "战斗实验室没有可用于只读派生的发布英雄规则。")
            : snapshot.Units.Single(unit => unit.InstanceId == primaryInstanceId).ContentId;

        var floorRoot = _index.ResolveFloorRuleScene(snapshot.FloorRuleId).Instantiate<FloorRuleContentRoot>();
        try
        {
            var relics = PrepareRelics(snapshot, primaryContentId);
            var units = snapshot.Units.Select(unit => new BattlePreparationUnitSource(
                unit.InstanceId,
                unit.ContentId,
                unit.Side == BattleLabSide.Player ? 0 : 1,
                1f,
                false,
                unit.Side == BattleLabSide.Player,
                true,
                unit.Equipment.Select(item => new BattlePreparationEquipmentSource(
                    item.InstanceId,
                    item.ContentId,
                    unit.InstanceId,
                    item.SlotIndex)).ToImmutableArray())).ToImmutableArray();
            var placements = snapshot.Units.Select(unit => new BattlePreparationPlacementSource(
                unit.InstanceId,
                unit.Cell)).ToImmutableArray();
            var request = new BattlePreparationRequest(
                _index.Package.Content,
                unchecked((ulong)snapshot.Seed),
                new BattleIdentity("battle-lab", TowerNodeType.Combat, unchecked((ulong)snapshot.Seed), 0, 0),
                floorRoot.CreateRuntime(),
                units,
                placements,
                primaryContentId,
                relics.Modifiers,
                snapshot.Mode == BattleLabPlacementMode.Formal
                    ? snapshot.CurrentPopulation - playerCount
                    : 0,
                0,
                relics.BattlePreparation,
                null,
                ResolveBossTimeline(snapshot),
                BattlePlacementValidation.ExactAll);
            return BattlePreparationAssembler.Assemble(request);
        }
        finally { floorRoot.Free(); }
    }

    private void ValidateSnapshot(BattleLabStartSnapshot snapshot)
    {
        // Restore is the authoritative semantic gate. Recomputing a digest is
        // integrity checking only and never authorizes untrusted preset data.
        var validator = new BattleLabSession(
            _index,
            snapshot.CurrentPopulation,
            snapshot.Seed,
            snapshot.Mode,
            snapshot.FloorRuleId);
        validator.Restore(snapshot);
    }

    private RunRelicPreparation PrepareRelics(BattleLabStartSnapshot snapshot, string heroContentId)
    {
        var bindings = snapshot.Relics.Select(relic =>
        {
            var definition = _index.Package.Content.Graph.ResolveRelic(relic.ContentId);
            return new RunItemBinding(
                Required(relic.ContentId),
                new ItemInstanceState
                {
                    InstanceId = relic.InstanceId,
                    ContentId = relic.ContentId,
                    Stacks = relic.Stacks,
                    Charges = 0,
                    Roll = 0,
                    Counters = RelicRunScope.InitialRunCounters(definition).ToList()
                },
                definition);
        }).ToArray();
        return new RunRelicService(_index.Package.Content).PrepareBattle(
            new RelicRunKey(unchecked((ulong)snapshot.Seed), heroContentId, 0, 0), bindings);
    }

    private BossTimelineSnapshot? ResolveBossTimeline(BattleLabStartSnapshot snapshot)
    {
        var bosses = snapshot.Units.Where(unit => unit.Side == BattleLabSide.Enemy)
            .Select(unit => unit.ContentId).ToHashSet(StringComparer.Ordinal);
        var matches = _index.Package.Project.Campaign.Regions.SelectMany(region => region.Encounters.Values)
            .Select(encounter => encounter.BossTimeline).Where(timeline => timeline is not null &&
                bosses.Contains(timeline.BossContentId)).Distinct().ToArray();
        if (matches.Length != 1) return null;
        var timeline = matches[0]!;
        return new BossTimelineSnapshot(timeline.StableId, timeline.BossContentId,
            timeline.Phases.Select(phase => new BossPhaseSnapshot(phase.StableId, phase.DisplayName,
                phase.StartHealthRatio, phase.AbilityLoadout)).ToImmutableArray());
    }

    private CatalogEntry Required(string id) => _index.Package.Content.TryGet(id, out var entry)
        ? entry
        : throw new InvalidOperationException($"发布内容不存在：{id}");
}
