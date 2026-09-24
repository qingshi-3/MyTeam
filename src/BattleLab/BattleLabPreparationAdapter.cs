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
        snapshot = ValidateSnapshot(snapshot);
        var playerUnits = snapshot.Units.Where(unit => unit.Side == BattleLabSide.Player).ToArray();
        if (playerUnits.Length == 0) throw new InvalidOperationException("A 队至少需要一个单位。");
        if (!snapshot.Units.Any(unit => unit.Side == BattleLabSide.Enemy))
            throw new InvalidOperationException("B 队至少需要一个单位。");

        return BuildPreparedConfig(snapshot);
    }

    public BattleConfig BuildProjection(BattleLabStartSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return BuildPreparedConfig(ValidateSnapshot(snapshot));
    }

    private BattleConfig BuildPreparedConfig(BattleLabStartSnapshot snapshot)
    {
        var floorRoot = _index.ResolveFloorRuleScene(snapshot.FloorRuleId).Instantiate<FloorRuleContentRoot>();
        try
        {
            var relics = PrepareRelics(snapshot, "battle-lab");
            var units = snapshot.Units.Select(unit => new BattlePreparationUnitSource(
                unit.InstanceId,
                unit.ContentId,
                unit.Side == BattleLabSide.Player ? 0 : 1,
                1f,
                false,
                true,
                true,
                unit.Equipment.Select(item => new BattlePreparationEquipmentSource(
                    item.InstanceId,
                    item.ContentId,
                    unit.InstanceId,
                    item.SlotIndex)).ToImmutableArray(), unit.RetainAttackStacks)).ToImmutableArray();
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
                string.Empty,
                relics.Modifiers,
                0,
                0,
                relics.BattlePreparation,
                null,
                null,
                BattlePlacementValidation.ExactAll,
                HeroRuleOverride: HeroRuleSnapshot.Neutral,
                AdditionalBossTimelines: ResolveBossTimelines(snapshot));
            return BattlePreparationAssembler.Assemble(request);
        }
        finally { floorRoot.Free(); }
    }

    private BattleLabStartSnapshot ValidateSnapshot(BattleLabStartSnapshot snapshot)
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
        return validator.Freeze();
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

    private ImmutableArray<BossTimelineSnapshot> ResolveBossTimelines(BattleLabStartSnapshot snapshot)
    {
        var bosses = snapshot.Units.Select(unit => unit.ContentId).ToHashSet(StringComparer.Ordinal);
        var matches = _index.Package.Project.Campaign.Regions.SelectMany(region => region.Encounters.Values)
            .Select(encounter => encounter.BossTimeline).Where(timeline => timeline is not null &&
                bosses.Contains(timeline.BossContentId)).Select(timeline => timeline!)
            .DistinctBy(timeline => timeline.StableId).OrderBy(timeline => timeline.StableId, StringComparer.Ordinal).ToArray();
        if (matches.GroupBy(timeline => timeline.BossContentId).Any(group => group.Count() > 1))
            throw new InvalidOperationException("同一首领存在多套阶段配置，无法确定测试使用的配置。");
        return matches.Select(timeline => new BossTimelineSnapshot(timeline.StableId, timeline.BossContentId,
            timeline.Phases.Select(phase => new BossPhaseSnapshot(phase.StableId, phase.DisplayName,
                phase.StartHealthRatio, phase.AbilityLoadout)).ToImmutableArray())).ToImmutableArray();
    }

    private CatalogEntry Required(string id) => _index.Package.Content.TryGet(id, out var entry)
        ? entry
        : throw new InvalidOperationException($"发布内容不存在：{id}");
}
