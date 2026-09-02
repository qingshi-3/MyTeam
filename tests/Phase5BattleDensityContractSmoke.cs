using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Equipment;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

public partial class Phase5BattleDensityContractSmoke : Node
{
    private const int PersistentPlayerCount = 18;
    private const int EnemyCount = 12;
    private const int TemporaryCount = 30;
    private const int PhysicalCellCount = BattlefieldLayout.Width * BattlefieldLayout.Height;
    private const double SetupBudgetMilliseconds = 5_000;
    private const double StepBudgetMilliseconds = 15_000;
    private const double CompletionBudgetMilliseconds = 5_000;

    private sealed record DensityMeasurement(
        string Fingerprint,
        BattleOutcome Outcome,
        int Ticks,
        int MaximumLivingBodies,
        int MaximumTotalBodies,
        int CombatEventCount,
        int ReactionCount,
        int CueCount,
        double SetupMilliseconds,
        double StepMilliseconds,
        double CompletionMilliseconds);

    public override async void _Ready()
    {
        var exitCode = Run();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private static int Run()
    {
        try
        {
            var catalog = GD.Load<ContentCatalog>("res://content/catalogs/alpha_catalog.tres") ??
                          throw new InvalidOperationException("production catalog is missing");
            var publication = ContentValidator.CompileProductionGraph(catalog, []);
            var graph = publication.Graph ?? throw new InvalidOperationException(
                "production package did not compile: " + string.Join(" | ", publication.Report.CoreErrors));
            if (!graph.TryGetEquipment("equipment_rimebrand", out var equipment) ||
                !graph.TryGetTrait("trait_winterbound", out var trait))
                throw new InvalidOperationException("production Frost Equipment or Trait is missing");

            var first = Measure(equipment, trait, 0xD3517UL);
            var second = Measure(equipment, trait, 0xD3517UL);
            if (!string.Equals(first.Fingerprint, second.Fingerprint, StringComparison.Ordinal) ||
                first.Outcome != second.Outcome || first.Ticks != second.Ticks ||
                first.MaximumLivingBodies != second.MaximumLivingBodies ||
                first.MaximumTotalBodies != second.MaximumTotalBodies ||
                first.CombatEventCount != second.CombatEventCount ||
                first.ReactionCount != second.ReactionCount || first.CueCount != second.CueCount)
                throw new InvalidOperationException("same-seed high-density Battle is not deterministic");

            AssertBudget(first, "first");
            AssertBudget(second, "second");
            GD.Print("PHASE5_BATTLE_DENSITY_CONTRACT_OK " + Format(first) + " repeat=" + Format(second));
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("PHASE5_BATTLE_DENSITY_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }

    private static DensityMeasurement Measure(
        CompiledEquipmentDefinition equipment,
        CompiledTraitDefinition trait,
        ulong seed)
    {
        var config = CreateConfig(equipment, trait, seed);
        var stopwatch = Stopwatch.StartNew();
        using var battle = new BattleSimulation(config);
        stopwatch.Stop();
        var setupMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

        AssertInitialDensity(battle, trait);
        var maximumLivingBodies = battle.Units.Count(unit => unit.Alive);
        var maximumTotalBodies = battle.Units.Count;
        var stepMilliseconds = 0d;
        var completionMilliseconds = 0d;
        while (battle.Outcome == BattleOutcome.Running)
        {
            stopwatch.Restart();
            var outcome = battle.Step();
            stopwatch.Stop();
            if (outcome == BattleOutcome.Running)
                stepMilliseconds += stopwatch.Elapsed.TotalMilliseconds;
            else
                completionMilliseconds += stopwatch.Elapsed.TotalMilliseconds;
            maximumLivingBodies = Math.Max(maximumLivingBodies, battle.Units.Count(unit => unit.Alive));
            maximumTotalBodies = Math.Max(maximumTotalBodies, battle.Units.Count);
        }

        if (battle.Outcome != BattleOutcome.PlayerVictory)
            throw new InvalidOperationException($"density Battle expected player victory, got {battle.Outcome}");
        if (maximumLivingBodies != PhysicalCellCount || maximumTotalBodies != PhysicalCellCount)
            throw new InvalidOperationException(
                $"density maxima changed: living={maximumLivingBodies}, total={maximumTotalBodies}");

        var combat = battle.CombatTransition ??
                     throw new InvalidOperationException("density combat transition is missing");
        var reactionCount = combat.Trace.Count(entry => entry.Kind == "reaction-executed");
        var cueCount = battle.StatusPresentationCues.Count;
        if (combat.Events.IsEmpty || reactionCount <= 0 || cueCount <= 0)
            throw new InvalidOperationException(
                $"density instrumentation was not exercised: events={combat.Events.Length}, " +
                $"reactions={reactionCount}, cues={cueCount}");
        AssertTerminalCleanup(battle);

        return new DensityMeasurement(
            Fingerprint(battle),
            battle.Outcome,
            battle.TickIndex,
            maximumLivingBodies,
            maximumTotalBodies,
            combat.Events.Length,
            reactionCount,
            cueCount,
            setupMilliseconds,
            stepMilliseconds,
            completionMilliseconds);
    }

    private static BattleConfig CreateConfig(
        CompiledEquipmentDefinition equipment,
        CompiledTraitDefinition trait,
        ulong seed)
    {
        var spawns = new List<BattleSpawn>(PhysicalCellCount);
        var equipmentInstances = ImmutableArray.CreateBuilder<EquipmentBattleInstanceSnapshot>(PersistentPlayerCount);
        for (var index = 0; index < PersistentPlayerCount; index++)
        {
            var ownerId = $"density-player-{index:D2}";
            spawns.Add(new BattleSpawn(
                Unit($"density_player_{index:D2}", true, 180, 8, 10, 3),
                0,
                BattlefieldLayout.PlayerDeploymentCells[index],
                ownerId,
                IsPersistentRosterHero: true));
            equipmentInstances.Add(new EquipmentBattleInstanceSnapshot(
                $"density-rime-{index:D2}", equipment.StableId, ownerId, 0, equipment));
        }

        var temporaryIndex = 0;
        for (var y = 0; y < BattlefieldLayout.Height; y++)
        for (var x = BattlefieldLayout.PlayerDeploymentColumns; x < BattlefieldLayout.Width - 2; x++)
        {
            var team = x <= 5 ? 0 : 1;
            var isPlayerTemporary = team == 0;
            spawns.Add(new BattleSpawn(
                Unit(
                    $"density_temporary_{temporaryIndex:D2}",
                    false,
                    isPlayerTemporary ? 90 : 100,
                    isPlayerTemporary ? 18 : 13,
                    1.1f,
                    2),
                team,
                new Vector2I(x, y),
                $"density-temporary-source-{temporaryIndex:D2}",
                IsTemporary: true,
                IsPersistentRosterHero: false));
            temporaryIndex++;
        }

        var enemyIndex = 0;
        for (var y = 0; y < BattlefieldLayout.Height; y++)
        for (var x = BattlefieldLayout.Width - 2; x < BattlefieldLayout.Width; x++)
        {
            spawns.Add(new BattleSpawn(
                Unit($"density_enemy_{enemyIndex:D2}", false, 200, 15, 1.1f, 2),
                1,
                new Vector2I(x, y),
                $"density-enemy-{enemyIndex:D2}",
                IsPersistentRosterHero: false));
            enemyIndex++;
        }

        if (spawns.Count != PhysicalCellCount || temporaryIndex != TemporaryCount || enemyIndex != EnemyCount ||
            spawns.Select(spawn => spawn.Cell).Distinct().Count() != PhysicalCellCount ||
            spawns.Any(spawn => !BattlefieldLayout.IsInBounds(spawn.Cell)))
            throw new InvalidOperationException("density fixture does not occupy every physical cell exactly once");

        var equipmentSnapshots = equipmentInstances.MoveToImmutable();
        var equipmentPreparation = new EquipmentBattlePreparation(
            EquipmentStateFingerprint.Compute(equipmentSnapshots),
            equipmentSnapshots);
        var traitPreparation = TraitBattlePreparationBuilder.Build(
            [trait],
            equipmentSnapshots.Select(instance => new TraitContributionInput(
                trait.StableId,
                1,
                0,
                TraitContributionSourceKind.Equipment,
                instance.InstanceId,
                instance.OwnerHeroInstanceId,
                equipment.StableId,
                true,
                false,
                true)));

        return new BattleConfig
        {
            Seed = seed,
            Identity = new BattleIdentity("phase5_density_contract", TowerNodeType.Combat, seed, 5, 1),
            FloorRule = new ClearFloorRuleRuntime("phase5_density", "phase5_density", "test"),
            HeroRule = new HeroRuleSnapshot(
                1, 1, 1, 0, 0, 0, false,
                string.Empty, 1, 1, 0, 0, 0, 0,
                false, false, 0, 0, string.Empty),
            Equipment = equipmentPreparation,
            Traits = traitPreparation,
            Spawns = spawns
        };
    }

    private static UnitSnapshot Unit(
        string id,
        bool hero,
        float health,
        float damage,
        float range,
        int attackTicks) =>
        new(
            id,
            id,
            UnitRole.Fighter,
            hero,
            false,
            health,
            damage,
            range,
            attackTicks,
            1,
            0,
            0,
            0,
            0,
            Array.Empty<string>(),
            new UnitBehaviorSnapshot());

    private static void AssertInitialDensity(BattleSimulation battle, CompiledTraitDefinition trait)
    {
        if (battle.Units.Count != PhysicalCellCount ||
            battle.Units.Count(unit => unit.Team == 0 && unit.IsPersistentRosterHero && !unit.IsTemporary) !=
            PersistentPlayerCount ||
            battle.Units.Count(unit => unit.Team == 1 && !unit.IsTemporary) != EnemyCount ||
            battle.Units.Count(unit => unit.IsTemporary) != TemporaryCount ||
            battle.Units.Select(unit => unit.Cell).Distinct().Count() != PhysicalCellCount ||
            battle.Units.Any(unit => !BattlefieldLayout.IsInBounds(unit.Cell)))
            throw new InvalidOperationException("Battle setup did not retain the exact full-board density fixture");
        if (battle.EquipmentSubscriptionCount != PersistentPlayerCount || battle.EquipmentModifierCount != 0)
            throw new InvalidOperationException("density Equipment subscriptions are not one per owner/event kind");
        var traitValue = battle.TraitSnapshot.Resolve(trait.StableId, 0);
        if (traitValue.Value != PersistentPlayerCount || traitValue.ActiveBreakpoint is null)
            throw new InvalidOperationException("density Trait contribution snapshot changed");
    }

    private static void AssertTerminalCleanup(BattleSimulation battle)
    {
        var equipment = battle.EquipmentTransition ??
                        throw new InvalidOperationException("density Equipment transition is missing");
        var status = battle.StatusTransition ??
                     throw new InvalidOperationException("density Status transition is missing");
        var trait = battle.TraitTransition ??
                    throw new InvalidOperationException("density Trait transition is missing");
        var attributes = battle.AttributeTransition ??
                         throw new InvalidOperationException("density Attribute transition is missing");
        var combat = battle.CombatTransition ??
                     throw new InvalidOperationException("density combat transition is missing");
        var effect = battle.EffectTransition ??
                     throw new InvalidOperationException("density Effect transition is missing");
        if (equipment.RemainingInstances != 0 || equipment.RemainingModifierHandles != 0 ||
            equipment.RemainingSubscriptions != 0 || battle.EquipmentSubscriptionCount != 0 ||
            battle.EquipmentModifierCount != 0 ||
            status.RemainingInstances != 0 || status.RemainingModifierHandles != 0 ||
            status.RemainingContributions != 0 || status.RemainingReactiveSubscriptions != 0 ||
            trait.RemainingTiers != 0 || trait.RemainingModifierHandles != 0 ||
            attributes.RemainingSets != 0 || attributes.RemainingModifiers != 0 ||
            combat.RemainingSubscriptions != 0 || combat.RemainingReactions != 0 ||
            combat.RemainingRuntimeEntries != 0 ||
            effect.RemainingSubscriptions != 0 || effect.RemainingInvocations != 0 ||
            effect.RemainingRuntimeInstances != 0 ||
            battle.Units.Any(unit => !unit.Statuses.IsEmpty))
            throw new InvalidOperationException("density Battle retained terminal scope state");
    }

    private static string Fingerprint(BattleSimulation battle)
    {
        var builder = new StringBuilder();
        var result = battle.CreateResult();
        builder.Append(result.Outcome).Append('|').Append(result.Ticks).Append('|').Append(result.Digest).AppendLine();
        foreach (var unit in result.Units.OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
            builder.Append(unit.RuntimeId).Append(':').Append(unit.Team).Append(':').Append(unit.IsTemporary)
                .Append(':').Append(unit.Alive).Append(':').Append(unit.FinalCell.X).Append(',').Append(unit.FinalCell.Y)
                .Append(':').Append(Float(unit.FinalHealth)).Append(':').Append(Float(unit.MaxHealth))
                .Append(':').Append(Float(unit.FinalShield)).Append(':').Append(Float(unit.FinalDamage))
                .Append(':').Append(Float(unit.DamageDealt)).Append(':').Append(Float(unit.DamageTaken))
                .Append(':').Append(Float(unit.ShieldAbsorbed)).Append(':').Append(Float(unit.HealingDone))
                .Append(':').Append(unit.Kills).Append(':').Append(unit.JoinTick).Append(':').Append(unit.DefeatTick)
                .Append(':').Append(unit.AttackActions).Append(':').Append(unit.EffectiveHealingEvents).AppendLine();
        foreach (var item in battle.PendingEvents)
            builder.Append("legacy:").Append(item.Tick).Append(':').Append(item.Type).Append(':')
                .Append(item.SourceRuntimeId).Append(':').Append(item.TargetRuntimeId).Append(':')
                .Append(Float(item.Value)).Append(':').Append(item.Cell.X).Append(',').Append(item.Cell.Y)
                .Append(':').Append(item.Cue).AppendLine();
        foreach (var item in battle.CombatEvents)
            builder.Append("combat:").Append(item.Sequence).Append(':').Append(item.ChainId).Append(':')
                .Append(item.Depth).Append(':').Append(item.Kind).Append(':').Append(item.Source).Append(':')
                .Append(item.SourceRuntimeId).Append(':').Append(item.TargetRuntimeId).Append(':').Append(item.Tick)
                .Append(':').Append(Float(item.RequestedValue)).Append(':').Append(Float(item.AppliedValue))
                .Append(':').Append(Float(item.EffectiveValue)).Append(':').Append(item.Cell.X).Append(',')
                .Append(item.Cell.Y).Append(':').Append(item.SubjectStableId).Append(':').Append(item.PreviousStacks)
                .Append(':').Append(item.CurrentStacks).Append(':').Append(item.Reason).AppendLine();
        foreach (var item in battle.CombatTransition!.Trace)
            builder.Append("trace:").Append(item.Sequence).Append(':').Append(item.ChainId).Append(':')
                .Append(item.Depth).Append(':').Append(item.Kind).Append(':').Append(item.Source).Append(':')
                .Append(item.Detail).AppendLine();
        foreach (var item in battle.StatusPresentationCues)
        {
            builder.Append("cue:").Append(item.Tick).Append(':').Append(item.Lifecycle).Append(':')
                .Append(item.Cue).Append(':').Append(item.Status.StableId).Append(':').Append(item.Status.SourceId)
                .Append(':').Append(item.Status.OwnerId).Append(':').Append(item.Status.InstanceId).Append(':')
                .Append(item.Status.ApplicationSequence).Append(':').Append(item.Status.Stacks).Append(':')
                .Append(item.Status.RemainingTicks).Append(':').Append(item.RemovalReason).Append(':')
                .Append(item.Status.SemanticIcon).Append(':').Append(item.Status.ReportLabel);
            foreach (var source in item.Status.SourceContributions)
                builder.Append(':').Append(source.SourceId).Append('@').Append(source.ApplicationSequence)
                    .Append('@').Append(source.AppliedTick).Append('@').Append(source.Stacks);
            builder.AppendLine();
        }
        foreach (var item in battle.EffectTrace)
            builder.Append("effect:").Append(item.TraceSequence).Append(':').Append(item.Ordering).Append(':')
                .Append(item.BindingId).Append(':').Append(item.StepIndex).Append(':').Append(item.TargetId)
                .Append(':').Append(item.Kind).Append(':').Append(item.Status).Append(':').Append(item.Interruption)
                .Append(':').Append(Float(item.AppliedAmount)).Append(':').Append(Float(item.EffectiveAmount))
                .Append(':').Append(item.Message).AppendLine();
        builder.Append("transitions:").Append(battle.EffectTransition).Append('|')
            .Append(battle.AttributeTransition).Append('|').Append(battle.CombatTransition!.Reason).Append('|')
            .Append(battle.EquipmentTransition).Append('|').Append(battle.StatusTransition).Append('|')
            .Append(battle.TraitTransition).AppendLine();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void AssertBudget(DensityMeasurement measurement, string label)
    {
        if (measurement.SetupMilliseconds > SetupBudgetMilliseconds ||
            measurement.StepMilliseconds > StepBudgetMilliseconds ||
            measurement.CompletionMilliseconds > CompletionBudgetMilliseconds)
            throw new InvalidOperationException(
                $"{label} density budget exceeded: setup={measurement.SetupMilliseconds:F2}ms, " +
                $"step={measurement.StepMilliseconds:F2}ms, completion={measurement.CompletionMilliseconds:F2}ms");
    }

    private static string Format(DensityMeasurement measurement) =>
        $"outcome={measurement.Outcome} ticks={measurement.Ticks} " +
        $"setup_ms={measurement.SetupMilliseconds:F2} step_ms={measurement.StepMilliseconds:F2} " +
        $"completion_ms={measurement.CompletionMilliseconds:F2} max_living={measurement.MaximumLivingBodies} " +
        $"max_total={measurement.MaximumTotalBodies} events={measurement.CombatEventCount} " +
        $"reactions={measurement.ReactionCount} cues={measurement.CueCount} cleanup=zero " +
        $"fingerprint={measurement.Fingerprint}";

    private static string Float(float value) => value.ToString("R", CultureInfo.InvariantCulture);
}
