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
    ImmutableArray<BattlePreparationEquipmentSource> Equipment);

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
    Action<BattleCombatBindingRegistry>? ConfigureCombatBindings = null);

public static class BattlePreparationAssembler
{
    public static BattleConfig Assemble(BattlePreparationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);
        ArgumentNullException.ThrowIfNull(request.FloorRule);
        ArgumentNullException.ThrowIfNull(request.Modifiers);
        if (request.EmptyDeploymentSlots < 0) throw new ArgumentOutOfRangeException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.HeroRuleContentId))
            throw new InvalidOperationException("Battle preparation requires a HeroRule content source.");

        var spawns = BuildSpawns(request);
        ValidatePlacements(request.FloorRule, spawns, request.PlacementValidation);
        var heroRule = BuildHeroRule(request.Content, request.HeroRuleContentId);
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
            if (request.BossTimeline?.BossContentId == snapshot.ContentId)
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
        }
    }

    private static EquipmentBattlePreparation BuildEquipment(
        CompiledContentGraph graph,
        ImmutableArray<BattlePreparationUnitSource> units)
    {
        var instances = ImmutableArray.CreateBuilder<EquipmentBattleInstanceSnapshot>();
        foreach (var unit in units.Where(unit => unit.Team == 0 && unit.IsDeployed))
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
        if (preparation is null) return ImmutableDictionary<string, UnitSnapshot>.Empty;
        var summons = ImmutableDictionary.CreateBuilder<string, UnitSnapshot>(StringComparer.Ordinal);
        foreach (var contentId in preparation.Commands
                     .SelectMany(command => command.Ability.Operations)
                     .OfType<CompiledSummonAbilityOperation>()
                     .Select(operation => operation.SummonContentId)
                     .Where(contentId => !string.IsNullOrWhiteSpace(contentId))
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(contentId => contentId, StringComparer.Ordinal))
            summons.Add(contentId, SnapshotOptional(content, contentId) ?? throw new InvalidOperationException(
                $"Tactical command references an unavailable summon unit: {contentId}"));
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
                content.Graph);
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
