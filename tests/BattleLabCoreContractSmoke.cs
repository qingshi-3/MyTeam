using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;
using TowerAutobattler.Traits;

public partial class BattleLabCoreContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private async Task<int> RunAsync()
    {
        try
        {
            var userDataBefore = UserDataFingerprint();
            var authored = TestProjectFixture.Authored();
            var gate = await TestProjectFixture.PublishAsync(this);
            var package = gate.Package ?? throw new InvalidOperationException(
                "Battle Lab package publication: " + string.Join(';', gate.Report.CoreErrors));
            var resourceRoots = BuildResourceFingerprintRoots(authored, package);
            var resourceBefore = ResourceGraphFingerprint.Compute(resourceRoots);
            VerifyRunPreparationParity(package);
            VerifySessionAndPreparation(package);
            var resourceAfter = ResourceGraphFingerprint.Compute(resourceRoots);
            Equal(resourceAfter, resourceBefore, "shared Resource fingerprint");
            Equal(UserDataFingerprint(), userDataBefore, "production user-data zero write");
            GD.Print("BATTLE_LAB_CORE_CONTRACT_OK preparation=run-adapter-parity content=compiled-typed " +
                     "session=deep-snapshot placement=formal-free-swap equipment=instance relic=team " +
                     "preset=v1-shape deterministic=same-seed resources=immutable save=zero-write");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("BATTLE_LAB_CORE_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }

    private static IReadOnlyList<Resource?> BuildResourceFingerprintRoots(
        Resource authored,
        TowerAutobattler.Composition.CompiledGamePackage package)
    {
        var roots = new List<Resource?> { authored, package.Content.Catalog };
        roots.AddRange(package.Content.Catalog.AllEntries()
            .OrderBy(entry => entry.StableId, StringComparer.Ordinal)
            .Select(entry => entry.Definition));

        var compiledPaths = package.Content.Graph.Equipment.Select(definition => definition.ResourcePath)
            .Concat(package.Content.Graph.Relics.Select(definition => definition.ResourcePath))
            .Concat(package.Content.Graph.Statuses.Select(definition => definition.ResourcePath))
            .Concat(package.Content.Graph.Traits.Select(definition => definition.ResourcePath))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal);
        foreach (var path in compiledPaths)
        {
            var resource = GD.Load<Resource>(path) ??
                           throw new InvalidOperationException("Compiled authored Resource missing: " + path);
            roots.Add(resource);
        }
        return roots;
    }

    private static void VerifyRunPreparationParity(TowerAutobattler.Composition.CompiledGamePackage package)
    {
        var save = new CountingSave();
        var app = new RunApplication(package.Content, save, package.Project);
        var hero = package.Content.Catalog.Heroes[0].StableId;
        Require(app.StartNewRun(hero, 771UL), "start Run fixture");
        var run = app.ActiveRun!;
        run.SelectedNode = TowerNodeType.Combat;
        run.PendingNode = true;
        var encounter = app.CurrentEncounter();
        var saveCountBeforePreparation = save.TotalWrites;
        var permissive = app.BuildBattleConfig(encounter, false);
        var strict = app.BuildBattleConfig(encounter, true);
        Equal(save.TotalWrites, saveCountBeforePreparation, "Run preparation save spy zero write");
        Equal(ConfigProjection(strict), ConfigProjection(permissive), "legal Run false/true preparation parity");
        VerifyLegacyPreparationProjection(package, run, permissive);
        using var first = new BattleSimulation(permissive);
        using var second = new BattleSimulation(strict);
        Equal(ResultProjection(first.RunToEnd()), ResultProjection(second.RunToEnd()), "Run result parity");

        var deployedId = run.Deployment.First(id => !string.IsNullOrWhiteSpace(id));
        var emptyIndex = run.Deployment.FindIndex(string.IsNullOrWhiteSpace);
        Require(emptyIndex >= 0, "Run fixture has a spare deployment slot");
        run.Deployment[emptyIndex] = deployedId;
        var duplicateAccepted = app.BuildBattleConfig(encounter, false);
        Require(duplicateAccepted.Spawns.Count(spawn => spawn.InstanceId == deployedId) == 2,
            "Run requireLegalFormation=false preserves duplicate formation input");
        RequireThrows(() => app.BuildBattleConfig(encounter, true),
            "Run requireLegalFormation=true rejects duplicate formation input");

        run.Deployment[emptyIndex] = string.Empty;
        var ninthEnemy = permissive.Spawns.First(spawn => spawn.Team == 1).Unit.ContentId;
        var denseEncounter = encounter with { EnemyIds = Enumerable.Repeat(ninthEnemy, 9).ToArray() };
        var denseRequest = RunBattlePreparationAdapter.CreateRequest(
            package.Content,
            run,
            denseEncounter,
            permissive.FloorRule,
            permissive.Modifiers,
            permissive.Relics,
            permissive.TacticalCommands,
            null,
            package.Project.RunRules.PhysicalDeploymentCeiling,
            true);
        var dense = BattlePreparationAssembler.Assemble(denseRequest);
        Equal(dense.Spawns.Count(spawn => spawn.Team == 1), 9,
            "strict Run preserves 9+ enemy preparation");
        Require(dense.Spawns.Where(spawn => spawn.Team == 1).Select(spawn => spawn.Cell).Distinct().Count() < 9,
            "strict Run keeps legacy enemy collision input for Simulation repair");
    }

    private static void VerifyLegacyPreparationProjection(
        TowerAutobattler.Composition.CompiledGamePackage package,
        ActiveRunDto run,
        BattleConfig config)
    {
        var deployed = run.Deployment.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
        var expectedEquipment = EquipmentBattlePreparationBuilder.Build(run, package.Content.Graph, deployed);
        Equal(config.Equipment.SourceFingerprint, expectedEquipment.SourceFingerprint,
            "Run Equipment preparation preservation");

        var traitInputs = RunTraitSnapshotBuilder.CollectInputs(run, package.Content.Graph).ToBuilder();
        foreach (var spawn in config.Spawns.Where(spawn => spawn.Team == 1)
                     .OrderBy(spawn => spawn.InstanceId, StringComparer.Ordinal))
        foreach (var contribution in spawn.Unit.TraitContributions.IsDefault
                     ? ImmutableArray<CompiledTraitContribution>.Empty
                     : spawn.Unit.TraitContributions)
            traitInputs.Add(new TraitContributionInput(
                contribution.TraitId, contribution.Value, 1, TraitContributionSourceKind.Hero,
                spawn.InstanceId, spawn.InstanceId, spawn.Unit.ContentId, !spawn.IsTemporary,
                spawn.IsTemporary, true));
        var expectedTraits = TraitBattlePreparationBuilder.Build(package.Content.Graph.Traits, traitInputs);
        Equal(config.Traits.SourceFingerprint, expectedTraits.SourceFingerprint,
            "Run Trait preparation preservation");

        var startingEntry = Required(package.Content, run.Roster[0].ContentId);
        var startingRoot = startingEntry.Scene.Instantiate<UnitContentRoot>();
        try
        {
            Equal(config.HeroRule, BattleSetupFactory.Snapshot(startingRoot.HeroRule ??
                throw new InvalidOperationException("Run starting hero rule fixture")),
                "Run HeroRule preparation preservation");
        }
        finally { startingRoot.Free(); }

        foreach (var spawn in config.Spawns)
        {
            var entry = Required(package.Content, spawn.Unit.ContentId);
            var root = entry.Scene.Instantiate<UnitContentRoot>();
            try
            {
                var expected = BattleSetupFactory.Snapshot(
                    (UnitDefinition)entry.Definition,
                    root.Behavior,
                    root.AbilityLoadout?.Resolve(package.Content.Graph),
                    package.Content.Graph);
                if (config.BossTimeline?.BossContentId == expected.ContentId)
                    expected = expected with { AbilityLoadout = null };
                Equal(UnitProjection(spawn.Unit), UnitProjection(expected),
                    $"Run Unit snapshot preservation {spawn.InstanceId}");
            }
            finally { root.Free(); }
        }
    }

    private static void VerifySessionAndPreparation(TowerAutobattler.Composition.CompiledGamePackage package)
    {
        var index = new BattleLabContentIndex(package);
        Require(index.PlayerHeroes.Length == package.Content.Catalog.Heroes.Count, "published player hero completeness");
        Require(index.PveUnits.Any(unit => unit.Definition.IsEnemy), "published PvE index");
        Require(index.Equipment.Length == package.Content.Graph.Equipment.Length, "Equipment index completeness");
        Require(index.Relics.Length == package.Content.Graph.Relics.Length, "Relic index completeness");
        var session = new BattleLabSession(index, package.Project.RunRules.InitialPopulation, 991L);
        var player = session.AddAndPlace(index.PlayerHeroes[0].StableId, BattleLabSide.Player, new Vector2I(0, 0));
        var enemy = session.AddAndPlace(index.PveUnits.First(unit => unit.Definition.IsEnemy).StableId,
            BattleLabSide.Enemy, new Vector2I(9, 0));
        Require(player.Succeeded && enemy.Succeeded, "formal library placement");
        var beforeSameCell = session.Freeze().CanonicalDigest;
        var sameCell = session.Move(player.InstanceId, new Vector2I(0, 0));
        Require(!sameCell.Succeeded && sameCell.RejectionReason.Contains("已位于", StringComparison.Ordinal) &&
                session.Freeze().CanonicalDigest == beforeSameCell,
            "same-cell move is an explicit non-mutating rejection");
        var overlap = session.AddAndPlace(index.PlayerHeroes[0].StableId,
            BattleLabSide.Player, new Vector2I(0, 0));
        Require(!overlap.Succeeded && session.Units.Select(unit => unit.Cell).Distinct().Count() == session.Units.Count,
            "new library placement cannot overlap an occupied cell");
        var wrongRegion = session.Move(player.InstanceId, new Vector2I(5, 0));
        Require(!wrongRegion.Succeeded && wrongRegion.RejectionReason.Contains("3×6", StringComparison.Ordinal),
            "formal player region rejection");
        var secondPlayer = session.AddAndPlace(index.PlayerHeroes[1].StableId, BattleLabSide.Player, new Vector2I(1, 0));
        Require(secondPlayer.Succeeded && secondPlayer.InstanceId == overlap.InstanceId,
            "failed placement does not consume instance sequence");
        var swap = session.Move(secondPlayer.InstanceId, new Vector2I(0, 0));
        Require(swap.Succeeded && swap.SwappedInstanceId == player.InstanceId &&
                session.At(new Vector2I(1, 0))?.InstanceId == player.InstanceId, "atomic swap");
        var externalSwapReject = session.Move(secondPlayer.InstanceId, new Vector2I(1, 0), true,
            cell => cell != new Vector2I(0, 0));
        Require(!externalSwapReject.Succeeded && session.At(new Vector2I(0, 0))?.InstanceId == secondPlayer.InstanceId,
            "swap validates external occupancy for the displaced unit origin");
        session.SetRules(BattleLabPlacementMode.FreeExperiment, package.Project.RunRules.InitialPopulation, 991L, "");
        Require(session.Move(player.InstanceId, new Vector2I(6, 5)).Succeeded, "free player placement");
        if (index.Equipment.Length > 0)
        {
            Require(session.Equip(player.InstanceId, 0, index.Equipment[0].StableId), "hero Equipment attach");
            var firstEquipmentId = session.Units.Single(unit => unit.InstanceId == player.InstanceId).Equipment[0].InstanceId;
            Require(session.Equip(player.InstanceId, 0, index.Equipment[^1].StableId), "hero Equipment replacement");
            Require(session.Units.Single(unit => unit.InstanceId == player.InstanceId).Equipment.Length == 1 &&
                    session.Units.Single(unit => unit.InstanceId == player.InstanceId).Equipment[0].InstanceId != firstEquipmentId,
                "Equipment replacement instance identity");
            Require(session.Equip(secondPlayer.InstanceId, 0, index.Equipment[0].StableId), "duplicate Equipment attach");
            var owners = session.Units.Where(unit => unit.Side == BattleLabSide.Player).SelectMany(unit => unit.Equipment)
                .Select(item => item.InstanceId).ToArray();
            Require(owners.Distinct(StringComparer.Ordinal).Count() == owners.Length, "Equipment instance isolation");
        }
        if (index.Relics.Length > 0) Require(session.SetRelic(index.Relics[0].StableId, 2), "team Relic stacks");
        var derived = BattleLabDerivedProjectionBuilder.Build(session);
        Require(derived.IsReady && derived.Units.Count == session.Units.Count &&
                derived.Units.Values.All(unit => unit.Health > 0 && unit.AttackSpeed > 0 && unit.Reach > 0),
            "atomic prepared derived projection");
        var frozen = session.Freeze();
        var dto = BattleLabPresetStore.ToDto(frozen);
        Require(BattleLabPresetStore.ValidateShape(dto), "preset v1 shape");
        var digest = frozen.CanonicalDigest;
        session.Move(enemy.InstanceId, new Vector2I(8, 5));
        Equal(frozen.CanonicalDigest, digest, "frozen snapshot isolation");
        session.Restore(frozen);
        Equal(session.Freeze().CanonicalDigest, digest, "snapshot restoration");

        var adapter = new BattleLabPreparationAdapter(index);
        var configA = adapter.Build(frozen);
        var configB = adapter.Build(frozen);
        if (configA.Relics is not null)
            foreach (var relic in configA.Relics.Instances)
            {
                Require(relic.Charges == 0 && relic.Roll == 0,
                    "Lab Relic battle state starts from production defaults");
                Require(relic.Counters.All(counter => counter.Value == 0),
                    "Lab Relic counters rebuild from zero production initial state");
                Require(RelicRunScope.InitialRunCounters(relic.Definition).All(expected =>
                        relic.Counters.Any(counter => counter.CounterId == expected.CounterId &&
                                                     counter.Value == expected.Value)),
                    "Lab Relic Run counters preserve the production initial set");
            }
        using var first = new BattleSimulation(configA);
        using var second = new BattleSimulation(configB);
        Equal(ResultProjection(first.RunToEnd()), ResultProjection(second.RunToEnd()), "Lab same-seed determinism");

        var renamedUnits = frozen.Units.Select(unit => unit.InstanceId == frozen.PrimaryHeroInstanceId
            ? unit with { InstanceId = "z-primary-hero" }
            : unit.Side == BattleLabSide.Player
                ? unit with { InstanceId = "a-secondary-hero" }
                : unit).ToImmutableArray();
        var renamed = ReDigest(frozen with
        {
            PrimaryHeroInstanceId = "z-primary-hero",
            Units = renamedUnits
        });
        var renamedConfig = adapter.Build(renamed);
        Equal(renamedConfig.HeroRule, configA.HeroRule,
            "explicit primary hero is independent of instance-id sort order");

        var unknownContent = BattleLabPresetStore.ToDto(frozen);
        unknownContent.Units[0].ContentId = "missing-published-content";
        var untrustedSnapshot = BattleLabPresetStore.ToSnapshot(unknownContent);
        RequireThrows(() => session.Restore(untrustedSnapshot),
            "recomputed preset digest does not authorize unknown content");

        if (frozen.Units.Any(unit => unit.Equipment.Length > 0) && index.Equipment.Length > 0)
        {
            var sequenceOwnerId = frozen.Units.First(unit => unit.Equipment.Length > 0).InstanceId;
            var highSequenceUnits = frozen.Units.Select(unit => unit.InstanceId != sequenceOwnerId
                ? unit
                : unit with
                {
                    Equipment = unit.Equipment.Select((item, itemIndex) => itemIndex == 0
                        ? item with { InstanceId = "lab-equipment-900" }
                        : item).ToImmutableArray()
                }).ToImmutableArray();
            var highSequence = ReDigest(frozen with { Units = highSequenceUnits });
            var sequenceSession = new BattleLabSession(index, frozen.CurrentPopulation,
                frozen.Seed, frozen.Mode, frozen.FloorRuleId);
            sequenceSession.Restore(highSequence);
            var equipmentOwner = highSequenceUnits.Single(unit => unit.InstanceId == sequenceOwnerId);
            Require(sequenceSession.Equip(equipmentOwner.InstanceId, 1, index.Equipment[0].StableId),
                "post-Restore Equipment edit");
            Require(ParseSequence(sequenceSession.Units.Single(unit => unit.InstanceId == equipmentOwner.InstanceId)
                    .Equipment.Single(item => item.SlotIndex == 1).InstanceId) > 900,
                "Restore next-instance sequence includes Equipment ids");
        }

        VerifyFloorAndPresetValidation(index);
        VerifyTypedEliteIndex(index);
        VerifyUnreadyProjectionAndPresetRoundTrip(index);
        VerifyTemporarySummonProjection(index);
        VerifyFreeExperimentBeyondProductionSlots(index);
    }

    private static void VerifyFloorAndPresetValidation(BattleLabContentIndex index)
    {
        RequireThrows(() => new BattleLabSession(index, 1, floorRuleId: "missing-floor-rule"),
            "unknown FloorRule is rejected");
        RequireThrows(() => new BattleLabSession(index, int.MaxValue),
            "formal population cannot exceed production cap");
        var blocked = index.Package.Project.FloorRules.Keys
            .OrderBy(id => id, StringComparer.Ordinal)
            .SelectMany(id => Enumerable.Range(0, BattlefieldLayout.Height)
                .SelectMany(y => Enumerable.Range(0, BattlefieldLayout.Width)
                    .Select(x => (Id: id, Cell: new Vector2I(x, y)))))
            .FirstOrDefault(candidate => !index.CanOccupy(candidate.Id, candidate.Cell));
        if (!string.IsNullOrWhiteSpace(blocked.Id))
        {
            var session = new BattleLabSession(index, 1, mode: BattleLabPlacementMode.FreeExperiment,
                floorRuleId: blocked.Id);
            var result = session.AddAndPlace(index.PlayerHeroes[0].StableId, BattleLabSide.Player, blocked.Cell);
            Require(!result.Succeeded && result.RejectionReason.Contains("地形", StringComparison.Ordinal),
                "FloorRule forbidden cell is rejected during edit");
        }

        var invalid = new BattleLabPresetDto
        {
            SchemaVersion = BattleLabSession.SchemaVersion,
            Mode = (BattleLabPlacementMode)999,
            CurrentPopulation = 1,
            FloorRuleId = index.DefaultFloorRuleId,
            PrimaryHeroInstanceId = "unit-1"
        };
        Require(!BattleLabPresetStore.ValidateShape(invalid), "preset rejects undefined mode");

        var expanded = new BattleLabSession(index, index.Rules.PhysicalDeploymentCeiling, 1818L);
        Require(expanded.AddAndPlace(index.PlayerHeroes[0].StableId, BattleLabSide.Player,
            new Vector2I(0, 0)).Succeeded, "expanded formal player");
        Require(expanded.AddAndPlace(index.PveUnits[0].StableId, BattleLabSide.Enemy,
            new Vector2I(9, 0)).Succeeded, "expanded formal enemy");
        var expandedSnapshot = BattleLabPresetStore.ToSnapshot(
            BattleLabPresetStore.ToDto(expanded.Freeze()));
        var restored = new BattleLabSession(index, 1);
        restored.Restore(expandedSnapshot);
        Equal(restored.CurrentPopulation, index.Rules.PhysicalDeploymentCeiling,
            "formal effective population 11-18 preset round-trip");
    }

    private static void VerifyTypedEliteIndex(BattleLabContentIndex index)
    {
        var encounters = index.Package.Project.Campaign.Regions
            .SelectMany(region => region.Encounters.Values)
            .ToArray();
        var normalIds = encounters
            .Where(encounter => encounter.NodeType == TowerNodeType.Combat)
            .SelectMany(encounter => encounter.EnemyPool.ContentIds.Append(encounter.LeadEnemyId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var eliteIds = encounters
            .Where(encounter => encounter.NodeType == TowerNodeType.Elite)
            .SelectMany(encounter => encounter.EnemyPool.ContentIds.Append(encounter.LeadEnemyId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var id in eliteIds)
            Require(index.TryGetUnit(id, out var unit) &&
                    unit.Classification.HasFlag(BattleLabUnitClassification.PveElite),
                $"Elite encounter typed classification {id}");
        var overlap = normalIds.Intersect(eliteIds, StringComparer.Ordinal).ToArray();
        Require(overlap.Length > 0, "shared Combat/Elite enemy pool overlap exists");
        foreach (var id in overlap)
            Require(index.TryGetUnit(id, out var unit) &&
                    unit.Classification.HasFlag(BattleLabUnitClassification.PveNormal) &&
                    unit.Classification.HasFlag(BattleLabUnitClassification.PveElite),
                $"Normal and Elite classifications are additive {id}");

        var referenced = normalIds.Union(eliteIds, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        foreach (var target in index.PveUnits.Where(unit => unit.Definition.IsEnemy &&
                     unit.Definition.Role != UnitRole.Boss && !referenced.Contains(unit.StableId)))
            Require(target.Classification.HasFlag(BattleLabUnitClassification.PveNormal),
                $"unreferenced development PvE fallback is Normal {target.StableId}");
    }

    private static void VerifyUnreadyProjectionAndPresetRoundTrip(BattleLabContentIndex index)
    {
        var enemyOnly = new BattleLabSession(index, index.Rules.InitialPopulation, 7001L);
        Require(enemyOnly.AddAndPlace(index.PveUnits.First(unit => unit.Definition.IsEnemy).StableId,
            BattleLabSide.Enemy, new Vector2I(9, 1)).Succeeded, "enemy-only placement");
        Require(string.IsNullOrWhiteSpace(enemyOnly.PrimaryHeroInstanceId),
            "primary hero is empty when no player exists");
        var enemyProjection = BattleLabDerivedProjectionBuilder.Build(enemyOnly);
        Require(!enemyProjection.IsReady && enemyProjection.Units.Count == 1 &&
                enemyProjection.Units.Values.Single().Health > 0 && enemyProjection.Traits.Length > 0,
            "enemy-only unready configuration still projects prepared inspection");
        RoundTripUserPreset(index, enemyOnly.Freeze(), "enemy-only");

        enemyOnly.Clear();
        var empty = enemyOnly.Freeze();
        Require(empty.Units.Length == 0 && string.IsNullOrWhiteSpace(empty.PrimaryHeroInstanceId),
            "clear-all snapshot allows empty primary hero");
        RoundTripUserPreset(index, empty, "clear-all");

        var graph = index.Package.Content.Graph;
        var traitHero = index.PlayerHeroes.FirstOrDefault(hero =>
            graph.ResolveUnitTraitContributions(hero.StableId).Length > 0) ?? index.PlayerHeroes[0];
        var traitEquipment = index.Equipment.First(item =>
            graph.ResolveEquipment(item.StableId).TraitContributions.Length > 0);
        var playerOnly = new BattleLabSession(index, index.Rules.InitialPopulation, 7002L);
        var player = playerOnly.AddAndPlace(traitHero.StableId,
            BattleLabSide.Player, new Vector2I(0, 1));
        Require(player.Succeeded, "player-only placement");
        Require(playerOnly.Equip(player.InstanceId, 0, traitEquipment.StableId),
            "player-only Trait Equipment attach");
        var equipmentInstanceId = playerOnly.Units.Single().Equipment.Single().InstanceId;
        var playerProjection = BattleLabDerivedProjectionBuilder.Build(playerOnly);
        var prepared = playerProjection.Units[player.InstanceId];
        Require(!playerProjection.IsReady && playerProjection.Units.Count == 1 &&
                prepared.Equipment.Length == playerOnly.Units.Single().Equipment.Length,
            "player-only unready configuration still projects Equipment and prepared attributes");
        if (graph.ResolveUnitTraitContributions(traitHero.StableId).Length > 0)
            Require(prepared.TraitContributions.Any(contribution =>
                    contribution.SourceKind == TraitContributionSourceKind.Hero &&
                    contribution.SourceInstanceId == player.InstanceId &&
                    contribution.OwnerRuntimeId == player.InstanceId &&
                    contribution.ContentIdentity == traitHero.StableId &&
                    contribution.Value > 0),
                "player-only unready projection exposes formal Hero Trait contribution when authored");
        Require(prepared.TraitContributions.Any(contribution =>
                contribution.SourceKind == TraitContributionSourceKind.Equipment &&
                contribution.SourceInstanceId == equipmentInstanceId &&
                contribution.OwnerRuntimeId == player.InstanceId &&
                contribution.ContentIdentity == traitEquipment.StableId &&
                contribution.Value > 0),
            "player-only unready projection exposes formal Equipment Trait contribution");
    }

    private static void RoundTripUserPreset(
        BattleLabContentIndex index,
        BattleLabStartSnapshot snapshot,
        string label)
    {
        var store = new BattleLabPresetStore();
        var name = $"contract-{label}-{Guid.NewGuid():N}";
        var directory = ProjectSettings.GlobalizePath(BattleLabPresetStore.UserNamespace);
        var path = Path.Combine(directory, name + ".json");
        var temporary = path + ".tmp";
        try
        {
            Require(store.Save(name, snapshot), $"{label} user preset Save");
            Require(store.TryLoad(name, out var dto), $"{label} user preset TryLoad");
            var restored = new BattleLabSession(index, 1);
            restored.Restore(BattleLabPresetStore.ToSnapshot(dto));
            Equal(restored.Freeze().CanonicalDigest, snapshot.CanonicalDigest,
                $"{label} user preset Restore");
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void VerifyTemporarySummonProjection(BattleLabContentIndex index)
    {
        const string gearArchitectId = "hero_gear_architect";
        const string summonRelicId = "item_clockwork_seed";
        Require(index.TryGetUnit(gearArchitectId, out _), "Gear Architect published fixture");
        Require(index.Relics.Any(relic => relic.StableId == summonRelicId), "summon Relic published fixture");
        var session = new BattleLabSession(index, Math.Max(2, index.Rules.InitialPopulation), 7003L);
        Require(session.AddAndPlace(gearArchitectId, BattleLabSide.Player,
            new Vector2I(0, 2)).Succeeded, "Gear Architect placement");
        Require(session.AddAndPlace(index.PveUnits.First(unit => unit.Definition.IsEnemy).StableId,
            BattleLabSide.Enemy, new Vector2I(9, 2)).Succeeded, "summon projection enemy placement");
        Require(session.SetRelic(summonRelicId, 1), "summon Relic attach");
        var projection = BattleLabDerivedProjectionBuilder.Build(session);
        Require(projection.IsReady && projection.Units.Count == session.Units.Count,
            "temporary HeroRule/Relic summons do not break authored derived projection");
        Require(projection.Units.Values.SelectMany(unit => unit.TraitContributions)
                .All(contribution => !contribution.IsTemporary),
            "temporary summon Trait contributions do not enter authored-unit projection");
        using var simulation = new BattleSimulation(new BattleLabPreparationAdapter(index).Build(session.Freeze()));
        Require(simulation.Units.Count > session.Units.Count &&
                simulation.Units.Any(unit => string.IsNullOrWhiteSpace(unit.SourceInstanceId)),
            "temporary summon fixture actually exercises empty source identity");
    }

    private static void VerifyFreeExperimentBeyondProductionSlots(BattleLabContentIndex index)
    {
        var graph = index.Package.Content.Graph;
        var traitHero = index.PlayerHeroes.FirstOrDefault(hero =>
            graph.ResolveUnitTraitContributions(hero.StableId).Length > 0) ?? index.PlayerHeroes[0];
        var session = new BattleLabSession(index, BattlefieldLayout.Width * BattlefieldLayout.Height,
            19919L, BattleLabPlacementMode.FreeExperiment);
        BattleLabPlacementResult nineteenth = null!;
        for (var i = 0; i < 19; i++)
        {
            nineteenth = session.AddAndPlace(traitHero.StableId, BattleLabSide.Player,
                new Vector2I(i % BattlefieldLayout.Width, i / BattlefieldLayout.Width));
            Require(nineteenth.Succeeded, $"free player placement {i + 1}");
        }
        var enemy = session.AddAndPlace(index.PveUnits[0].StableId, BattleLabSide.Enemy,
            new Vector2I(BattlefieldLayout.Width - 1, BattlefieldLayout.Height - 1));
        Require(enemy.Succeeded, "free enemy placement");
        string? equipmentInstanceId = null;
        if (index.Equipment.Length > 0)
        {
            Require(session.Equip(nineteenth.InstanceId, 0, index.Equipment[0].StableId),
                "nineteenth hero Equipment attach");
            equipmentInstanceId = session.Units.Single(unit => unit.InstanceId == nineteenth.InstanceId)
                .Equipment.Single().InstanceId;
        }
        var config = new BattleLabPreparationAdapter(index).Build(session.Freeze());
        if (equipmentInstanceId is not null)
            Require(config.Equipment.Instances.Any(item => item.InstanceId == equipmentInstanceId),
                "nineteenth hero Equipment enters preparation");
        if (graph.ResolveUnitTraitContributions(traitHero.StableId).Length > 0)
            Require(config.Traits.Contributions.Any(input => input.SourceInstanceId == nineteenth.InstanceId),
                "nineteenth hero Trait contribution enters preparation");
        Equal(config.Spawns.Count(spawn => spawn.Team == 0), 19,
            "free preparation keeps every player beyond production 18 slots");
    }

    private static string ConfigProjection(BattleConfig config) =>
        $"{config.Seed}|{config.Identity}|{config.FloorRule.Id}|{config.EmptyDeploymentSlots}|{config.StartingGold}|" +
        string.Join(';', config.Spawns.Select(spawn =>
            $"{spawn.InstanceId}:{spawn.Unit.ContentId}:{spawn.Team}:{spawn.Cell.X},{spawn.Cell.Y}:{spawn.HealthRatio}")) +
        $"|{config.Equipment.SourceFingerprint}|{config.Traits.SourceFingerprint}|" +
        $"{config.Relics?.SourceFingerprint}|{config.BossTimeline?.StableId}";

    private static string UnitProjection(UnitSnapshot unit) =>
        $"{unit.ContentId}|{unit.DisplayName}|{unit.Role}|{unit.IsHero}|{unit.IsBoss}|" +
        $"{unit.MaxHealth}|{unit.Damage}|{unit.Range}|{unit.AttackTicks}|{unit.MoveTicks}|" +
        $"{unit.Armor}|{unit.HealPower}|{unit.SplashRadius}|{unit.LifeSteal}|" +
        $"{string.Join(',', unit.Tags)}|{unit.Behavior}|" +
        $"{string.Join(',', unit.AbilityLoadout?.Abilities.Select(ability => ability.StableId) ?? [])}|" +
        $"{unit.AttributeDefinition?.Fingerprint}|" +
        string.Join(',', (unit.TraitContributions.IsDefault
                ? ImmutableArray<CompiledTraitContribution>.Empty
                : unit.TraitContributions)
            .Select(contribution => $"{contribution.TraitId}:{contribution.Value}"));

    private static string ResultProjection(BattleResult result) =>
        $"{result.Outcome}|{result.Ticks}|{result.Digest}|{result.GoldSpent}|" +
        string.Join(';', result.Units.OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal)
            .Select(unit => $"{unit.RuntimeId}:{unit.FinalHealth}:{unit.FinalCell.X},{unit.FinalCell.Y}"));

    private static BattleLabStartSnapshot ReDigest(BattleLabStartSnapshot snapshot) => snapshot with
    {
        CanonicalDigest = BattleLabSession.CanonicalDigest(
            snapshot.Mode,
            snapshot.CurrentPopulation,
            snapshot.Seed,
            snapshot.FloorRuleId,
            snapshot.PrimaryHeroInstanceId,
            snapshot.Units,
            snapshot.Relics)
    };

    private static int ParseSequence(string instanceId)
    {
        var separator = instanceId.LastIndexOf('-');
        return separator >= 0 && int.TryParse(instanceId[(separator + 1)..], out var value) ? value : 0;
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }

    private static void Equal<T>(T actual, T expected, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }

    private static void RequireThrows(Action action, string label)
    {
        try { action(); }
        catch (Exception) { return; }
        throw new InvalidOperationException(label);
    }

    private static CatalogEntry Required(ContentRegistry content, string contentId) =>
        content.TryGet(contentId, out var entry)
            ? entry
            : throw new InvalidOperationException($"Missing fixture content: {contentId}");

    private static string UserDataFingerprint()
    {
        var root = ProjectSettings.GlobalizePath("user://");
        if (!Directory.Exists(root)) return "missing";
        var facts = new[] { "meta.json", "settings.json", "active_run.json", "meta.json.tmp",
                "settings.json.tmp", "active_run.json.tmp" }
            .Select(name => Path.Combine(root, name))
            .Where(File.Exists)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => $"{Path.GetFileName(path)}:{Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))}");
        return string.Join('|', facts);
    }

    private sealed class CountingSave : IRunSaveService
    {
        private ActiveRunDto? _run;
        public int TotalWrites { get; private set; }
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => _run;
        public bool SaveMeta(MetaProgressDto value) { TotalWrites++; return true; }
        public bool SaveSettings(SettingsDto value) { TotalWrites++; return true; }
        public bool SaveActiveRun(ActiveRunDto value) { TotalWrites++; _run = value; return true; }
        public void DeleteActiveRun() { TotalWrites++; _run = null; }
    }
}
