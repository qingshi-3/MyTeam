using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using TowerAutobattler.Abilities;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.UI;

namespace TowerAutobattler.Project;

public static partial class GameProjectCompiler
{
    [GeneratedRegex("^[a-z0-9_]+$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdPattern();

    public static GameProjectCompilationResult Compile(
        GameProjectDefinition? authored,
        CompiledContentGraph contentGraph)
    {
        ArgumentNullException.ThrowIfNull(contentGraph);
        var report = new ValidationReport();
        if (authored is null)
        {
            report.Error("Game project definition is missing.");
            return new GameProjectCompilationResult(null, report);
        }

        var source = Source(authored);
        ValidateStableId(authored.StableId, source, report);
        if (authored.Content is null) report.Error($"{source}: missing ContentCatalog.");
        if (authored.Campaign is null) report.Error($"{source}: missing CampaignDefinition.");
        if (authored.RunRules is null) report.Error($"{source}: missing RunRulesDefinition.");
        if (authored.Presentation is null) report.Error($"{source}: missing ProjectPresentationDefinition.");
        if (report.HasCoreErrors)
            return new GameProjectCompilationResult(null, report);

        var context = new CompilationContext(authored.Content!, contentGraph, report);
        context.RegisterStableId(authored.StableId, authored);
        var campaign = CompileCampaign(authored.Campaign!, context);
        var rules = CompileRunRules(authored.RunRules!, authored.Content!, contentGraph, report);
        var presentation = CompilePresentation(authored.Presentation!, report);
        if (report.HasCoreErrors || campaign is null || rules is null || presentation is null)
            return new GameProjectCompilationResult(null, report);

        return new GameProjectCompilationResult(
            new CompiledGameProject(
                authored.StableId,
                authored.Content!,
                campaign,
                rules,
                presentation,
                context.FloorRules),
            report);
    }

    public static ImmutableArray<AbilityLoadoutDefinition?> CollectAbilityLoadoutReferences(
        GameProjectDefinition? authored)
    {
        if (authored?.Campaign?.Regions is null) return [];
        var seen = new HashSet<AbilityLoadoutDefinition>(ReferenceEqualityComparer.Instance);
        var references = ImmutableArray.CreateBuilder<AbilityLoadoutDefinition?>();
        foreach (var region in authored.Campaign.Regions)
        foreach (var encounter in region?.Encounters ?? [])
        foreach (var phase in encounter?.BossTimeline?.Phases ?? [])
            if (phase?.AbilityLoadout is not null && seen.Add(phase.AbilityLoadout))
                references.Add(phase.AbilityLoadout);
        return references.ToImmutable();
    }

    private static CompiledCampaign? CompileCampaign(CampaignDefinition authored, CompilationContext context)
    {
        var source = Source(authored);
        context.RegisterStableId(authored.StableId, authored);
        if (authored.FloorsPerRegion <= 0) context.Report.Error($"{source}: FloorsPerRegion must be positive.");
        if (authored.Regions is null || authored.Regions.Length == 0)
            context.Report.Error($"{source}: campaign requires at least one region.");
        if (authored.NodeTable is null) context.Report.Error($"{source}: missing tower node table.");

        var starter = context.CompilePool(authored.StarterPool, ContentPoolKind.Soldier, $"{source}.StarterPool");
        var recruitment = context.CompilePool(authored.RecruitmentPool, ContentPoolKind.Soldier, $"{source}.RecruitmentPool");
        var itemReward = context.CompilePool(authored.ItemRewardPool, ContentPoolKind.Item, $"{source}.ItemRewardPool");
        var shop = context.CompilePool(authored.ShopPool, ContentPoolKind.Item, $"{source}.ShopPool");
        var nodeTable = authored.NodeTable is null ? null : CompileNodeTable(authored.NodeTable, context.Report);

        if (nodeTable is not null && authored.FloorsPerRegion > 0 &&
            nodeTable.BossLocalFloor >= authored.FloorsPerRegion)
            context.Report.Error($"{source}: boss local floor must be within FloorsPerRegion.");

        var regions = ImmutableArray.CreateBuilder<CompiledTowerRegion>();
        var regionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var region in authored.Regions ?? [])
        {
            if (region is null)
            {
                context.Report.Error($"{source}: region reference is null.");
                continue;
            }
            var regionSource = Source(region);
            context.RegisterStableId(region.Id, region);
            if (!regionIds.Add(region.Id)) context.Report.Error($"{source}: duplicate region id '{region.Id}'.");
            if (string.IsNullOrWhiteSpace(region.DisplayName)) context.Report.Error($"{regionSource}: display name is required.");
            var encounters = ImmutableDictionary.CreateBuilder<TowerNodeType, CompiledEncounter>();
            foreach (var encounter in region.Encounters ?? [])
            {
                if (encounter is null)
                {
                    context.Report.Error($"{regionSource}: encounter reference is null.");
                    continue;
                }
                var compiled = CompileEncounter(encounter, context);
                if (compiled is not null && !encounters.TryAdd(compiled.NodeType, compiled))
                    context.Report.Error($"{regionSource}: duplicate encounter binding for {compiled.NodeType}.");
            }
            foreach (var required in new[] { TowerNodeType.Combat, TowerNodeType.Elite, TowerNodeType.Boss })
                if (!encounters.ContainsKey(required)) context.Report.Error($"{regionSource}: missing {required} encounter.");
            regions.Add(new CompiledTowerRegion(
                region.Id,
                region.DisplayName,
                region.Description,
                region.AccentColor,
                encounters.ToImmutable()));
        }

        if (context.Report.HasCoreErrors || starter is null || recruitment is null || itemReward is null ||
            shop is null || nodeTable is null)
            return null;
        return new CompiledCampaign(
            authored.StableId,
            authored.FloorsPerRegion,
            regions.ToImmutable(),
            nodeTable,
            starter,
            recruitment,
            itemReward,
            shop);
    }

    private static CompiledTowerNodeTable? CompileNodeTable(
        TowerNodeTableDefinition authored,
        ValidationReport report)
    {
        var source = Source(authored);
        var nodes = ImmutableDictionary.CreateBuilder<TowerNodeType, CompiledTowerNode>();
        foreach (var node in authored.Nodes ?? [])
        {
            if (node is null)
            {
                report.Error($"{source}: node definition is null.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(node.TitlePattern) || string.IsNullOrWhiteSpace(node.DescriptionPattern))
                report.Error($"{source}: {node.Type} title and description are required.");
            if (node.Risk < 0) report.Error($"{source}: {node.Type} risk cannot be negative.");
            if (!nodes.TryAdd(node.Type, new CompiledTowerNode(
                    node.Type, node.TitlePattern, node.DescriptionPattern, node.Risk)))
                report.Error($"{source}: duplicate node definition for {node.Type}.");
        }
        foreach (var required in Enum.GetValues<TowerNodeType>())
            if (!nodes.ContainsKey(required)) report.Error($"{source}: missing node definition for {required}.");
        if (authored.Rotation is null || authored.Rotation.Length == 0)
            report.Error($"{source}: regular node rotation is empty.");
        var rotation = ImmutableArray.CreateBuilder<TowerNodeType>();
        var rotationTypes = new HashSet<TowerNodeType>();
        foreach (var node in authored.Rotation ?? [])
        {
            if (node is null || !nodes.ContainsKey(node.Type))
            {
                report.Error($"{source}: rotation references an unregistered node definition.");
                continue;
            }
            if (node.Type == TowerNodeType.Boss)
                report.Error($"{source}: boss cannot appear in the regular node rotation.");
            if (!rotationTypes.Add(node.Type))
                report.Error($"{source}: duplicate regular rotation node {node.Type}.");
            rotation.Add(node.Type);
        }
        if (authored.BossLocalFloor < 0 || authored.RegularOptionCount <= 0 || authored.RotationStride <= 0 ||
            authored.FloorSeedStride <= 0)
            report.Error($"{source}: node table numeric settings must be positive and BossLocalFloor non-negative.");
        if (authored.RegularOptionCount > rotation.Count)
            report.Error($"{source}: regular option count cannot exceed the authored rotation size.");
        if (report.HasCoreErrors) return null;
        return new CompiledTowerNodeTable(
            nodes.ToImmutable(),
            rotation.ToImmutable(),
            authored.BossLocalFloor,
            authored.RegularOptionCount,
            authored.RotationStride,
            authored.FloorSeedStride);
    }

    private static CompiledEncounter? CompileEncounter(
        EncounterDefinition authored,
        CompilationContext context)
    {
        var source = Source(authored);
        context.RegisterStableId(authored.StableId, authored);
        if (authored.NodeType is not (TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss))
            context.Report.Error($"{source}: encounters may bind only Combat, Elite, or Boss nodes.");
        if (string.IsNullOrWhiteSpace(authored.TitlePattern)) context.Report.Error($"{source}: title pattern is required.");
        if (authored.BaseEnemyCount <= 0) context.Report.Error($"{source}: base enemy count must be positive.");
        if (authored.SeedSalt < 0) context.Report.Error($"{source}: seed salt cannot be negative.");
        var enemyPool = context.CompilePool(authored.EnemyPool, ContentPoolKind.Enemy, $"{source}.EnemyPool");
        var floorPool = context.CompilePool(authored.FloorRulePool, ContentPoolKind.FloorRule, $"{source}.FloorRulePool");
        if (!string.IsNullOrWhiteSpace(authored.LeadEnemyId) && !context.IsContent(authored.LeadEnemyId, ContentPoolKind.Enemy))
            context.Report.Error($"{source}: unknown lead enemy '{authored.LeadEnemyId}'.");
        if (authored.NodeType == TowerNodeType.Boss && string.IsNullOrWhiteSpace(authored.LeadEnemyId))
            context.Report.Error($"{source}: boss encounter requires a lead enemy.");
        if (authored.NodeType != TowerNodeType.Boss && authored.BossTimeline is not null)
            context.Report.Error($"{source}: only a boss encounter may reference a boss timeline.");
        var timeline = authored.BossTimeline is null
            ? null
            : CompileBossTimeline(authored.BossTimeline, authored.LeadEnemyId, context);
        if (authored.NodeType == TowerNodeType.Boss && timeline is null)
            context.Report.Error($"{source}: boss encounter requires a valid boss timeline.");
        if (context.Report.HasCoreErrors || enemyPool is null || floorPool is null) return null;
        return new CompiledEncounter(
            authored.StableId,
            authored.NodeType,
            authored.TitlePattern,
            enemyPool,
            floorPool,
            authored.LeadEnemyId,
            authored.BaseEnemyCount,
            authored.AddRegionIndexToCount,
            authored.SeedSalt,
            timeline);
    }

    private static CompiledBossTimeline? CompileBossTimeline(
        BossTimelineDefinition authored,
        string expectedBossId,
        CompilationContext context)
    {
        var source = Source(authored);
        context.RegisterStableId(authored.StableId, authored);
        if (!string.Equals(authored.BossContentId, expectedBossId, StringComparison.Ordinal))
            context.Report.Error($"{source}: boss content id does not match the encounter lead enemy.");
        if (!context.IsContent(authored.BossContentId, ContentPoolKind.Enemy))
            context.Report.Error($"{source}: unknown boss content id '{authored.BossContentId}'.");
        if (authored.Phases is null || authored.Phases.Length == 0)
            context.Report.Error($"{source}: boss timeline requires at least one phase.");

        var phases = ImmutableArray.CreateBuilder<CompiledBossPhase>();
        var previousThreshold = float.PositiveInfinity;
        foreach (var phase in authored.Phases ?? [])
        {
            if (phase is null)
            {
                context.Report.Error($"{source}: boss phase reference is null.");
                continue;
            }
            var phaseSource = Source(phase);
            context.RegisterStableId(phase.StableId, phase);
            if (string.IsNullOrWhiteSpace(phase.DisplayName)) context.Report.Error($"{phaseSource}: display name is required.");
            if (!float.IsFinite(phase.StartHealthRatio) || phase.StartHealthRatio <= 0 || phase.StartHealthRatio > 1 ||
                phase.StartHealthRatio >= previousThreshold)
                context.Report.Error($"{phaseSource}: phase health thresholds must be finite, positive, and strictly descending from at most 1.");
            previousThreshold = phase.StartHealthRatio;
            var loadout = phase.AbilityLoadout is null
                ? null
                : context.ResolveAbilityLoadout(phase.AbilityLoadout, phaseSource);
            phases.Add(new CompiledBossPhase(
                phase.StableId,
                phase.DisplayName,
                phase.StartHealthRatio,
                loadout));
        }

        context.ValidateBossSceneLoadout(
            authored.BossContentId,
            authored.Phases?.FirstOrDefault()?.AbilityLoadout,
            source);
        if (context.Report.HasCoreErrors) return null;
        return new CompiledBossTimeline(authored.StableId, authored.BossContentId, phases.ToImmutable());
    }

    private static CompiledRunRules? CompileRunRules(
        RunRulesDefinition authored,
        ContentCatalog catalog,
        CompiledContentGraph contentGraph,
        ValidationReport report)
    {
        var source = Source(authored);
        var physicalCellCount = BattlefieldLayout.PlayerDeploymentColumns * BattlefieldLayout.Height;
        if (authored.OrdinaryPopulationCap != 10 ||
            authored.PhysicalDeploymentCeiling != physicalCellCount)
            report.Error($"{source}: population caps must preserve the ordinary 10 ceiling and the {physicalCellCount}-cell physical board.");
        if (authored.ReserveCapacity < 0 || authored.StarterRosterHeroCount <= 0 ||
            (long)authored.StarterRosterHeroCount + 1 > authored.OrdinaryPopulationCap ||
            authored.EquipmentSlotCapacity is < 1 or > 6 ||
            authored.RecruitmentChoiceCount <= 0 ||
            authored.ItemChoiceCount <= 0 || authored.StartingGold < 0 || authored.NormalBattleGold < 0 ||
            authored.EliteBattleGold < 0 || authored.BossBattleGold < 0 || authored.RiskyEventSuccessGold < 0 ||
            authored.SafeEventGold < 0 || authored.RestGold < 0)
            report.Error($"{source}: capacities, choices, and economy values are invalid.");
        if (authored.InitialPopulation < (long)authored.StarterRosterHeroCount + 1 ||
            authored.InitialPopulation > authored.OrdinaryPopulationCap)
            report.Error($"{source}: initial population must cover the selected hero plus starter roster and remain within the ordinary population cap.");
        foreach (var (value, label) in new[]
        {
            (authored.VictoryHeroRecovery, nameof(authored.VictoryHeroRecovery)),
            (authored.VictorySoldierRecovery, nameof(authored.VictorySoldierRecovery)),
            (authored.MinimumVictoryHeroHealth, nameof(authored.MinimumVictoryHeroHealth)),
            (authored.MinimumLivingSoldierHealth, nameof(authored.MinimumLivingSoldierHealth)),
            (authored.DefeatedSoldierHealth, nameof(authored.DefeatedSoldierHealth)),
            (authored.RiskyEventSuccessChance, nameof(authored.RiskyEventSuccessChance)),
            (authored.RiskyEventHealthLoss, nameof(authored.RiskyEventHealthLoss)),
            (authored.RiskyEventMinimumHealth, nameof(authored.RiskyEventMinimumHealth)),
            (authored.RestHeroHealing, nameof(authored.RestHeroHealing)),
            (authored.RestSoldierHealing, nameof(authored.RestSoldierHealing))
        })
            if (!float.IsFinite(value) || value < 0 || value > 1)
                report.Error($"{source}: {label} must be finite and within 0..1.");
        if (authored.InitialUnlockedHeroCount <= 0 || authored.InitialUnlockedHeroCount > catalog.Heroes.Count)
            report.Error($"{source}: initial unlocked hero count is invalid.");
        var starterCommandIds = ImmutableArray.CreateBuilder<string>();
        var starterCommands = authored.StarterTacticalCommands ?? [];
        if (starterCommands.Length != Run.ActiveRunTacticalCommandPolicy.SlotCount)
            report.Error($"{source}: exactly two starter tactical commands are required.");
        var uniqueStarterCommands = new HashSet<string>(StringComparer.Ordinal);
        foreach (var command in starterCommands)
        {
            if (command is null || !contentGraph.TryResolveTacticalCommand(command, out var compiled))
            {
                report.Error($"{source}: starter tactical command is missing from the canonical content graph.");
                continue;
            }
            if (!uniqueStarterCommands.Add(compiled.StableId))
                report.Error($"{source}: starter tactical commands must be unique.");
            starterCommandIds.Add(compiled.StableId);
        }

        var legacyMappings = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        var authoredMappings = authored.LegacyHeroTacticalCommandMappings ?? [];
        foreach (var mapping in authoredMappings)
        {
            if (mapping is null || string.IsNullOrWhiteSpace(mapping.HeroContentId) ||
                !catalog.Heroes.Any(entry => entry.StableId == mapping.HeroContentId))
            {
                report.Error($"{source}: legacy tactical-command mapping references an unknown hero.");
                continue;
            }
            if (mapping.Command is null ||
                !contentGraph.TryResolveTacticalCommand(mapping.Command, out var compiled))
            {
                report.Error($"{source}: legacy tactical-command mapping references an unknown command.");
                continue;
            }
            if (!legacyMappings.TryAdd(mapping.HeroContentId, compiled.StableId))
                report.Error($"{source}: duplicate legacy tactical-command mapping for '{mapping.HeroContentId}'.");
        }
        foreach (var hero in catalog.Heroes)
            if (!legacyMappings.ContainsKey(hero.StableId))
                report.Error($"{source}: legacy tactical-command mapping is missing hero '{hero.StableId}'.");
        if (report.HasCoreErrors) return null;
        return new CompiledRunRules(
            authored.OrdinaryPopulationCap,
            authored.PhysicalDeploymentCeiling,
            authored.ReserveCapacity,
            authored.StarterRosterHeroCount,
            authored.InitialPopulation,
            authored.EquipmentSlotCapacity,
            Run.ActiveRunTacticalCommandPolicy.SlotCount,
            starterCommandIds.ToImmutable(),
            legacyMappings.ToImmutable(),
            authored.RecruitmentChoiceCount,
            authored.ItemChoiceCount,
            authored.StartingGold,
            authored.NormalBattleGold,
            authored.EliteBattleGold,
            authored.BossBattleGold,
            authored.VictoryHeroRecovery,
            authored.VictorySoldierRecovery,
            authored.MinimumVictoryHeroHealth,
            authored.MinimumLivingSoldierHealth,
            authored.DefeatedSoldierHealth,
            authored.RiskyEventSuccessGold,
            authored.RiskyEventSuccessChance,
            authored.RiskyEventHealthLoss,
            authored.RiskyEventMinimumHealth,
            authored.SafeEventGold,
            authored.RestHeroHealing,
            authored.RestSoldierHealing,
            authored.RestGold,
            authored.InitialUnlockedHeroCount);
    }

    private static CompiledProjectPresentation? CompilePresentation(
        ProjectPresentationDefinition authored,
        ValidationReport report)
    {
        var source = Source(authored);
        if (authored.SemanticIcons is null) report.Error($"{source}: missing semantic icon catalog.");
        else report.Merge(authored.SemanticIcons.Validate());
        ValidateScene<ChoiceCard>(authored.ChoiceCard, $"{source}.ChoiceCard", report);
        ValidateScene<UnitChoiceCard>(authored.UnitChoiceCard, $"{source}.UnitChoiceCard", report);
        ValidateScene<ItemChoiceCard>(authored.ItemChoiceCard, $"{source}.ItemChoiceCard", report);
        if (report.HasCoreErrors || authored.SemanticIcons is null || authored.ChoiceCard is null ||
            authored.UnitChoiceCard is null || authored.ItemChoiceCard is null)
            return null;
        return new CompiledProjectPresentation(
            authored.SemanticIcons,
            authored.ChoiceCard,
            authored.UnitChoiceCard,
            authored.ItemChoiceCard);
    }

    private static void ValidateScene<T>(Godot.PackedScene? scene, string source, ValidationReport report)
        where T : Godot.Node
    {
        if (scene is null)
        {
            report.Error($"{source}: scene is missing.");
            return;
        }
        Godot.Node? instance = null;
        try
        {
            instance = scene.Instantiate();
            if (instance is not T) report.Error($"{source}: scene root must be {typeof(T).Name}.");
        }
        catch (Exception exception)
        {
            report.Error($"{source}: scene instantiation failed: {exception.Message}");
        }
        finally
        {
            instance?.Free();
        }
    }

    private static void ValidateStableId(string stableId, string source, ValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(stableId) || !StableIdPattern().IsMatch(stableId))
            report.Error($"{source}: invalid stable id '{stableId}'.");
    }

    private static string Source(Godot.Resource resource) =>
        string.IsNullOrWhiteSpace(resource.ResourcePath) ? resource.GetType().Name : resource.ResourcePath;

    private sealed class CompilationContext
    {
        private readonly ContentCatalog _catalog;
        private readonly CompiledContentGraph _contentGraph;
        private readonly Dictionary<string, CatalogEntry> _entries;
        private readonly HashSet<string> _floorRuleIds = new(StringComparer.Ordinal);
        private readonly ImmutableDictionary<string, Godot.PackedScene>.Builder _floorRules =
            ImmutableDictionary.CreateBuilder<string, Godot.PackedScene>(StringComparer.Ordinal);
        private readonly Dictionary<ContentPoolDefinition, CompiledContentPool> _pools =
            new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, (Godot.Resource Owner, string Source)> _stableIds = new(StringComparer.Ordinal);

        public CompilationContext(
            ContentCatalog catalog,
            CompiledContentGraph contentGraph,
            ValidationReport report)
        {
            _catalog = catalog;
            _contentGraph = contentGraph;
            Report = report;
            _entries = new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
            foreach (var entry in catalog.AllEntries())
            {
                if (entry is null || string.IsNullOrWhiteSpace(entry.StableId))
                {
                    Report.Error($"{catalog.ResourcePath}: catalog contains a missing or blank entry.");
                    continue;
                }
                if (!_entries.TryAdd(entry.StableId, entry))
                    Report.Error($"{catalog.ResourcePath}: duplicate catalog stable id '{entry.StableId}'.");
            }
            foreach (var scene in catalog.FloorRules)
            {
                FloorRuleContentRoot? root = null;
                try
                {
                    root = scene.Instantiate<FloorRuleContentRoot>();
                    if (!_floorRuleIds.Add(root.Id)) Report.Error($"{scene.ResourcePath}: duplicate floor-rule id '{root.Id}'.");
                    else _floorRules.Add(root.Id, scene);
                }
                catch (Exception exception)
                {
                    Report.Error($"{scene.ResourcePath}: floor-rule identity load failed: {exception.Message}");
                }
                finally
                {
                    root?.Free();
                }
            }
        }

        public ValidationReport Report { get; }
        public ImmutableDictionary<string, Godot.PackedScene> FloorRules => _floorRules.ToImmutable();

        public void RegisterStableId(string stableId, Godot.Resource owner)
        {
            var source = Source(owner);
            ValidateStableId(stableId, source, Report);
            if (!string.IsNullOrWhiteSpace(stableId) && _stableIds.TryGetValue(stableId, out var prior) &&
                !ReferenceEquals(prior.Owner, owner))
                Report.Error($"{source}: stable id '{stableId}' collides with {prior.Source}.");
            else if (!string.IsNullOrWhiteSpace(stableId))
                _stableIds[stableId] = (owner, source);
        }

        public CompiledContentPool? CompilePool(
            ContentPoolDefinition? authored,
            ContentPoolKind expectedKind,
            string bindingSource)
        {
            if (authored is null)
            {
                Report.Error($"{bindingSource}: pool is missing.");
                return null;
            }
            if (_pools.TryGetValue(authored, out var cached))
            {
                if (cached.Kind != expectedKind)
                    Report.Error($"{bindingSource}: pool '{cached.StableId}' has kind {cached.Kind}, expected {expectedKind}.");
                return cached.Kind == expectedKind ? cached : null;
            }

            var source = Source(authored);
            RegisterStableId(authored.StableId, authored);
            if (authored.Kind != expectedKind)
                Report.Error($"{bindingSource}: pool '{authored.StableId}' has kind {authored.Kind}, expected {expectedKind}.");
            if (authored.ContentIds is null || authored.ContentIds.Length == 0)
                Report.Error($"{source}: pool is empty.");
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in authored.ContentIds ?? [])
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    Report.Error($"{source}: pool contains a blank content id.");
                    continue;
                }
                if (!unique.Add(id)) Report.Error($"{source}: duplicate pool content id '{id}'.");
                if (!IsContent(id, authored.Kind)) Report.Error($"{source}: '{id}' is not valid {authored.Kind} content.");
            }
            var compiled = new CompiledContentPool(authored.StableId, authored.Kind, [.. authored.ContentIds ?? []]);
            _pools.Add(authored, compiled);
            return authored.Kind == expectedKind ? compiled : null;
        }

        public bool IsContent(string stableId, ContentPoolKind kind)
        {
            if (string.IsNullOrWhiteSpace(stableId)) return false;
            if (kind == ContentPoolKind.FloorRule) return _floorRuleIds.Contains(stableId);
            if (!_entries.TryGetValue(stableId, out var entry)) return false;
            return kind switch
            {
                ContentPoolKind.Soldier => entry.Definition is UnitDefinition { IsHero: false, IsEnemy: false },
                ContentPoolKind.Item => entry.Definition is ItemDefinition,
                ContentPoolKind.Enemy => entry.Definition is UnitDefinition { IsEnemy: true },
                _ => false
            };
        }

        public CompiledAbilityLoadout? ResolveAbilityLoadout(
            AbilityLoadoutDefinition authored,
            string source)
        {
            if (_contentGraph.TryResolveLoadout(authored, out var loadout)) return loadout;
            Report.Error($"{source}: ability loadout is not part of the compiled content publication graph.");
            return null;
        }

        public void ValidateBossSceneLoadout(
            string bossContentId,
            AbilityLoadoutDefinition? expectedLoadout,
            string source)
        {
            if (!_entries.TryGetValue(bossContentId, out var entry)) return;
            UnitContentRoot? root = null;
            try
            {
                root = entry.Scene.Instantiate<UnitContentRoot>();
                var actual = root.AbilityLoadout?.Loadout;
                if (!ReferenceEquals(actual, expectedLoadout) && actual?.ResourcePath != expectedLoadout?.ResourcePath)
                    Report.Error($"{source}: first boss phase loadout does not match the independently authored boss scene.");
            }
            catch (Exception exception)
            {
                Report.Error($"{source}: boss scene loadout validation failed: {exception.Message}");
            }
            finally
            {
                root?.Free();
            }
        }
    }
}
