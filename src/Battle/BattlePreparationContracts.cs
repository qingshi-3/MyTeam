using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

public sealed record BattlePreparationEquipmentSource(
    string InstanceId,
    string ContentId,
    string OwnerUnitInstanceId,
    int SlotIndex);

// A source is a logical unit instance. It may be outside the physical battle
// (an undeployed Run hero) while still contributing to production Trait rules.
public sealed record BattlePreparationUnitSource(
    string InstanceId,
    string ContentId,
    int Team,
    float HealthRatio,
    bool IsTemporary,
    bool IsPersistentRosterHero,
    bool IsDeployed,
    ImmutableArray<BattlePreparationEquipmentSource> Equipment, bool RetainAttackStacks = false);

// Placements are deliberately separate from sources. This preserves the
// legacy Run `requireLegalFormation=false` contract, including duplicate
// deployment references that BattleSimulation historically repaired.
public sealed record BattlePreparationPlacementSource(
    string UnitInstanceId,
    Vector2I Cell);

public enum BattlePlacementValidation
{
    None,
    PlayerFormation,
    ExactAll
}

// Production-neutral immutable source boundary. Run and developer tools only
// project their owned state into this request; all formal content preparation
// below is shared and owned by Battle.
public sealed record BattlePreparationRequest(
    ContentRegistry Content,
    ulong Seed,
    BattleIdentity? Identity,
    IBattleFloorRuleRuntime FloorRule,
    ImmutableArray<BattlePreparationUnitSource> Units,
    ImmutableArray<BattlePreparationPlacementSource> Placements,
    string HeroRuleContentId,
    ModifierSnapshot Modifiers,
    int EmptyDeploymentSlots,
    int StartingGold,
    RelicBattlePreparation? Relics,
    TacticalCommandBattlePreparation? TacticalCommands,
    BossTimelineSnapshot? BossTimeline,
    BattlePlacementValidation PlacementValidation,
    ImmutableArray<TraitExplicitContribution> ExplicitTraitContributions = default,
    Action<BattleCombatBindingRegistry>? ConfigureCombatBindings = null,
    float EnemyHealthMultiplier = 1, float EnemyDamageMultiplier = 1,
    HeroRuleSnapshot? HeroRuleOverride = null,
    ImmutableArray<BossTimelineSnapshot> AdditionalBossTimelines = default);

public static class BattlePreparationAssembler
{
    public static BattleConfig Assemble(BattlePreparationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);
        ArgumentNullException.ThrowIfNull(request.FloorRule);
        ArgumentNullException.ThrowIfNull(request.Modifiers);
        if (request.EmptyDeploymentSlots < 0) throw new ArgumentOutOfRangeException(nameof(request));
        if (!float.IsFinite(request.EnemyHealthMultiplier) || request.EnemyHealthMultiplier is <= 0 or > 100 ||
            !float.IsFinite(request.EnemyDamageMultiplier) || request.EnemyDamageMultiplier is <= 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(request), "Invalid encounter strength multipliers.");
        if (request.HeroRuleOverride is null && string.IsNullOrWhiteSpace(request.HeroRuleContentId))
            throw new InvalidOperationException("Battle preparation requires a HeroRule content source.");

        var spawns = BuildSpawns(request);
        ValidatePlacements(request.FloorRule, spawns, request.PlacementValidation);
        var heroRule = request.HeroRuleOverride ?? BuildHeroRule(request.Content, request.HeroRuleContentId);
        var equipment = BuildEquipment(request.Content.Graph, request.Units);
        var traits = BuildTraits(request.Content.Graph, request.Units, request.ExplicitTraitContributions);
        var relicSummons = BuildRelicSummons(request.Content, request.Relics);
        var tacticalSummons = BuildTacticalSummons(request.Content, request.TacticalCommands);
        var heroSummon = SnapshotOptional(request.Content, heroRule.SummonContentId);
        var itemSummon = SnapshotOptional(request.Content, request.Modifiers.SummonContentId);

        return new BattleConfig
        {
            Seed = request.Seed,
            Identity = request.Identity,
            FloorRule = request.FloorRule,
            Spawns = spawns.ToList(),
            HeroRule = heroRule,
            Modifiers = request.Modifiers,
            Summons = new SummonProfiles(heroSummon, heroSummon, heroSummon, itemSummon),
            EmptyDeploymentSlots = request.EmptyDeploymentSlots,
            StartingGold = request.StartingGold,
            Relics = request.Relics,
            RelicSummons = relicSummons,
            Equipment = equipment,
            Traits = traits,
            TacticalCommands = request.TacticalCommands,
            TacticalSummons = tacticalSummons,
            BossTimeline = request.BossTimeline,
            AdditionalBossTimelines = request.AdditionalBossTimelines.IsDefault ? [] : request.AdditionalBossTimelines,
            ConfigureCombatBindings = request.ConfigureCombatBindings
        };
    }

    private static ImmutableArray<BattleSpawn> BuildSpawns(BattlePreparationRequest request)
    {
        var builder = ImmutableArray.CreateBuilder<BattleSpawn>();
        foreach (var placement in request.Placements)
        {
            if (placement is null || string.IsNullOrWhiteSpace(placement.UnitInstanceId))
                throw new InvalidOperationException("Battle preparation contains an invalid placement source.");
            var source = request.Units.FirstOrDefault(candidate =>
                string.Equals(candidate.InstanceId, placement.UnitInstanceId, StringComparison.Ordinal)) ??
                throw new InvalidOperationException(
                    $"Battle preparation placement references a missing unit: {placement.UnitInstanceId}");
            var snapshot = SnapshotRequired(request.Content, source.ContentId, out var behaviorSummonId);
            if (source.Team == 1)
                snapshot = snapshot with { MaxHealth = snapshot.MaxHealth * request.EnemyHealthMultiplier,
                    Damage = snapshot.Damage * request.EnemyDamageMultiplier };
            if (source.RetainAttackStacks)
            {
                if (snapshot.AttackHitGrowth is not { RetentionUpgradeAvailable: true } growth)
                    throw new InvalidOperationException("此单位没有可测试的保层升阶。");
                snapshot = snapshot with { AttackHitGrowth = growth with { ResetOnTargetChange = false } };
            }
            if (request.BossTimeline?.BossContentId == snapshot.ContentId ||
                !request.AdditionalBossTimelines.IsDefaultOrEmpty && request.AdditionalBossTimelines.Any(
                    timeline => timeline.BossContentId == snapshot.ContentId))
                snapshot = snapshot with { AbilityLoadout = null };
            builder.Add(new BattleSpawn(
                snapshot,
                source.Team,
                placement.Cell,
                source.InstanceId,
                source.HealthRatio,
                source.IsTemporary,
                SnapshotOptional(request.Content, behaviorSummonId),
                source.IsPersistentRosterHero));
        }
        return builder.ToImmutable();
    }

    private static void ValidatePlacements(
        IBattleFloorRuleRuntime floorRule,
        ImmutableArray<BattleSpawn> spawns,
        BattlePlacementValidation validation)
    {
        if (!Enum.IsDefined(validation)) throw new ArgumentOutOfRangeException(nameof(validation));
        if (validation == BattlePlacementValidation.None) return;
        var instanceIds = new HashSet<string>(StringComparer.Ordinal);
        var cells = new HashSet<Vector2I>();
        var bodies = new List<(Vector2 Position, float Radius)>();
        foreach (var spawn in spawns.Where(spawn =>
                     validation == BattlePlacementValidation.ExactAll || spawn.Team == 0))
        {
            if (string.IsNullOrWhiteSpace(spawn.InstanceId) || spawn.Team is < 0 or > 1)
                throw new InvalidOperationException("Battle preparation contains an invalid spawn identity or team.");
            if (!instanceIds.Add(spawn.InstanceId))
                throw new InvalidOperationException($"Battle preparation contains duplicate instance id: {spawn.InstanceId}");
            if (!BattlefieldLayout.IsInBounds(spawn.Cell) || !floorRule.CanOccupy(spawn.Cell))
                throw new InvalidOperationException($"Battle preparation contains an illegal cell: {spawn.Cell}");
            if (!cells.Add(spawn.Cell))
                throw new InvalidOperationException($"Battle preparation contains duplicate cell occupancy: {spawn.Cell}");
            var position = BattlefieldSpace.CellCenter(spawn.Cell);
            if (!BattlefieldSpace.IsPositionTerrainClear(position, spawn.Unit.BodyRadius,
                    BattlefieldLayout.Width, BattlefieldLayout.Height, floorRule.CanOccupy) ||
                bodies.Any(body => position.DistanceTo(body.Position) < body.Radius + spawn.Unit.BodyRadius + BattlefieldSpace.BodyClearance))
                throw new InvalidOperationException("战斗准备的单位身体重叠、越界或碰到禁行地形。");
            bodies.Add((position, spawn.Unit.BodyRadius));
        }
    }

    private static EquipmentBattlePreparation BuildEquipment(
        CompiledContentGraph graph,
        ImmutableArray<BattlePreparationUnitSource> units)
    {
        var instances = ImmutableArray.CreateBuilder<EquipmentBattleInstanceSnapshot>();
        foreach (var unit in units.Where(unit => unit.IsDeployed))
        foreach (var equipment in unit.Equipment.OrderBy(item => item.SlotIndex))
        {
            if (equipment is null || string.IsNullOrWhiteSpace(equipment.InstanceId) ||
                !string.Equals(equipment.OwnerUnitInstanceId, unit.InstanceId, StringComparison.Ordinal) ||
                equipment.SlotIndex < 0)
                throw new InvalidOperationException("Battle preparation contains invalid Equipment source state.");
            instances.Add(new EquipmentBattleInstanceSnapshot(
                equipment.InstanceId,
                equipment.ContentId,
                equipment.OwnerUnitInstanceId,
                equipment.SlotIndex,
                graph.ResolveEquipment(equipment.ContentId)));
        }
        var result = instances.ToImmutable();
        return new EquipmentBattlePreparation(EquipmentStateFingerprint.Compute(result), result);
    }

    private static TraitBattlePreparation BuildTraits(
        CompiledContentGraph graph,
        ImmutableArray<BattlePreparationUnitSource> units,
        ImmutableArray<TraitExplicitContribution> explicitContributions)
    {
        var inputs = ImmutableArray.CreateBuilder<TraitContributionInput>();
        foreach (var unit in units)
        {
            var isPersistentTraitSource = unit.Team == 1 ? !unit.IsTemporary : unit.IsPersistentRosterHero;
            foreach (var contribution in graph.ResolveUnitTraitContributions(unit.ContentId))
                inputs.Add(new TraitContributionInput(
                    contribution.TraitId,
                    contribution.Value,
                    unit.Team,
                    TraitContributionSourceKind.Hero,
                    unit.InstanceId,
                    unit.InstanceId,
                    unit.ContentId,
                    isPersistentTraitSource,
                    unit.IsTemporary,
                    unit.IsDeployed));
            foreach (var equipment in unit.Equipment.OrderBy(item => item.SlotIndex))
            foreach (var contribution in graph.ResolveEquipment(equipment.ContentId).TraitContributions)
                inputs.Add(new TraitContributionInput(
                    contribution.TraitId,
                    contribution.Value,
                    unit.Team,
                    TraitContributionSourceKind.Equipment,
                    equipment.InstanceId,
                    unit.InstanceId,
                    equipment.ContentId,
                    isPersistentTraitSource,
                    unit.IsTemporary,
                    unit.IsDeployed));
        }
        foreach (var extra in explicitContributions.IsDefault
                     ? ImmutableArray<TraitExplicitContribution>.Empty
                     : explicitContributions)
            inputs.Add(new TraitContributionInput(
                extra.TraitId,
                extra.Value,
                extra.Team,
                TraitContributionSourceKind.ExplicitExtra,
                extra.SourceInstanceId,
                string.Empty,
                extra.ContentIdentity,
                false,
                false,
                false));
        return TraitBattlePreparationBuilder.Build(graph.Traits, inputs);
    }

    private static HeroRuleSnapshot BuildHeroRule(ContentRegistry content, string contentId)
    {
        var entry = Required(content, contentId);
        var root = entry.Scene.Instantiate<UnitContentRoot>();
        try
        {
            return BattleSetupFactory.Snapshot(root.HeroRule ?? throw new InvalidOperationException(
                $"HeroRule source '{contentId}' has no compatibility rule component."));
        }
        finally { root.Free(); }
    }

    private static IReadOnlyDictionary<string, UnitSnapshot> BuildRelicSummons(
        ContentRegistry content,
        RelicBattlePreparation? preparation)
    {
        if (preparation is null) return ImmutableDictionary<string, UnitSnapshot>.Empty;
        var summons = ImmutableDictionary.CreateBuilder<string, UnitSnapshot>(StringComparer.Ordinal);
        foreach (var contentId in preparation.Instances
                     .SelectMany(instance => instance.Definition.BattleStartEffects)
                     .OfType<CompiledRelicBattleStartSummon>()
                     .Select(effect => effect.ContentId)
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(contentId => contentId, StringComparer.Ordinal))
            summons.Add(contentId, SnapshotOptional(content, contentId) ?? throw new InvalidOperationException(
                $"Relic references an unavailable summon unit: {contentId}"));
        return summons.ToImmutable();
    }

    private static IReadOnlyDictionary<string, UnitSnapshot> BuildTacticalSummons(
        ContentRegistry content,
        TacticalCommandBattlePreparation? preparation)
    {
        var summons = ImmutableDictionary.CreateBuilder<string, UnitSnapshot>(StringComparer.Ordinal);
        // The shared spawn registry serves automatic heroes and lifecycle products as
        // well as tactical commands; it must exist even when no command is equipped.
        var operations = content.Graph.Abilities.SelectMany(ability => ability.Operations)
            .Concat(preparation?.Commands.SelectMany(command => command.Ability.Operations) ?? []);
        foreach (var contentId in operations.Select(operation => operation switch {
                     CompiledSummonAbilityOperation summon => summon.SummonContentId,
                     CompiledLifecycleOperation { Kind: LifecycleAbilityKind.RaiseCorpse } corpse => corpse.SummonContentId,
                     _ => "" }).Concat(operations.OfType<CompiledEnemyAction>().SelectMany(action => action.ContentDependencies))
                     .Where(contentId => !string.IsNullOrWhiteSpace(contentId))
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(contentId => contentId, StringComparer.Ordinal))
            summons.Add(contentId, SnapshotOptional(content, contentId) ?? throw new InvalidOperationException(
                $"Battle ability references an unavailable summon unit: {contentId}"));
        return summons.ToImmutable();
    }

    private static UnitSnapshot SnapshotRequired(
        ContentRegistry content,
        string contentId,
        out string behaviorSummonId)
    {
        var entry = Required(content, contentId);
        if (entry.Definition is not UnitDefinition definition)
            throw new InvalidOperationException($"Battle preparation source is not a unit: {contentId}");
        var root = entry.Scene.Instantiate<UnitContentRoot>();
        try
        {
            behaviorSummonId = root.Behavior?.SummonContentId ?? string.Empty;
            return BattleSetupFactory.Snapshot(
                definition,
                root.Behavior,
                root.AbilityLoadout?.Resolve(content.Graph),
                content.Graph) with { AttackHitGrowth = root.AttackHitGrowth?.Snapshot() };
        }
        finally { root.Free(); }
    }

    private static UnitSnapshot? SnapshotOptional(ContentRegistry content, string contentId)
    {
        if (string.IsNullOrWhiteSpace(contentId) || !content.TryGet(contentId, out var entry) ||
            entry.Definition is not UnitDefinition)
            return null;
        return SnapshotRequired(content, contentId, out _);
    }

    private static CatalogEntry Required(ContentRegistry content, string contentId) =>
        content.TryGet(contentId, out var entry)
            ? entry
            : throw new InvalidOperationException($"Missing content: {contentId}");
}
