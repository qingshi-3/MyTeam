using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

public interface IRunFormationPersistence
{
    bool TryCommitFormation(ActiveRunDto run, System.Action mutation);
}

public sealed class FormationCellDto
{
    public int X { get; set; }
    public int Y { get; set; }

    public Vector2I ToCell() => new(X, Y);
    public FormationCellDto Clone() => new() { X = X, Y = Y };
    public static FormationCellDto FromCell(Vector2I cell) => new() { X = cell.X, Y = cell.Y };
}

public enum FormationPieceKind { RosterHero }

public enum FormationOperation
{
    None,
    Deploy,
    Replace,
    Move,
    Swap
}

public sealed record FormationEvaluation(
    bool IsValid,
    FormationOperation Operation,
    string RejectionReason,
    int SourceSlot = -1,
    int TargetSlot = -1,
    RunPopulationFacts? Population = null)
{
    public static FormationEvaluation Accept(
        FormationOperation operation,
        int sourceSlot = -1,
        int targetSlot = -1,
        RunPopulationFacts? population = null) =>
        new(true, operation, string.Empty, sourceSlot, targetSlot, population);

    public static FormationEvaluation Reject(string reason, RunPopulationFacts? population = null) =>
        new(false, FormationOperation.None, reason, Population: population);
}

public sealed record FormationMoveCommand(
    FormationPieceKind PieceKind,
    string InstanceId,
    Vector2I TargetCell)
{
    public static FormationMoveCommand RosterHero(string instanceId, Vector2I targetCell) =>
        new(FormationPieceKind.RosterHero, instanceId, targetCell);
}

public static class ActiveRunFormationSchema
{
    public const int CurrentVersion = 4;
    private const int LegacyDeploymentCapacity = 6;

    public static void InitializeVersion4(ActiveRunDto run)
    {
        run.Version = CurrentVersion;
        run.Deployment = EmptyDeployment();
        run.EquippedTacticalCommandIds = [];
        run.LegacyHeroId = null;
        run.LegacyHeroHealthRatio = 0;
        run.LegacyHeroCell = null;
        run.LegacyDeploymentCells = null;
    }

    public static void InitializeVersion4(ActiveRunDto run, CompiledRunRules rules)
    {
        InitializeVersion4(run);
        run.EquippedTacticalCommandIds = ActiveRunTacticalCommandPolicy.StarterLoadout(rules);
    }

    public static bool TryMigrateToCurrent(ActiveRunDto run) =>
        run is not null && run.Version == CurrentVersion &&
        run.LegacyHeroId is null && run.LegacyHeroHealthRatio == 0 &&
        run.LegacyHeroCell is null && run.LegacyDeploymentCells is null;

    public static bool TryMigrateToCurrent(ActiveRunDto run, CompiledRunRules rules) =>
        rules is not null && TryMigrateToCurrentCore(run, rules);

    private static bool TryMigrateToCurrentCore(
        ActiveRunDto run,
        CompiledRunRules rules)
    {
        if (run is null) return false;
        if (run.Version == CurrentVersion)
            return run.LegacyHeroId is null && run.LegacyHeroHealthRatio == 0 &&
                   run.LegacyHeroCell is null && run.LegacyDeploymentCells is null;
        if (run.Version is not (2 or 3) || string.IsNullOrWhiteSpace(run.LegacyHeroId) ||
            !float.IsFinite(run.LegacyHeroHealthRatio) || run.LegacyHeroHealthRatio < 0 ||
            run.LegacyHeroHealthRatio > 1 || run.Roster is null || run.Deployment is null ||
            run.Items is null || run.PopulationCapSources is null ||
            run.EquippedTacticalCommandIds is null || run.EquippedTacticalCommandIds.Count != 0 ||
            run.Roster.Any(hero => hero is null || hero.Equipment is null || hero.Equipment.Count != 0) ||
            run.Items.Any(item => item is null || item.Counters is null || item.Counters.Count != 0) ||
            run.PopulationCapSources.Count != 0 || run.CurrentPopulation != 1 ||
            run.Deployment.Count != LegacyDeploymentCapacity || run.Deployment.Any(id => id is null))
            return false;

        var migratedRosterCount = (long)run.Roster.Count + 1;
        var migratedPopulation = Math.Max((long)rules.InitialPopulation, migratedRosterCount);
        if (rules.InitialPopulation <= 0 || rules.OrdinaryPopulationCap <= 0 ||
            rules.InitialPopulation > rules.OrdinaryPopulationCap ||
            migratedPopulation > rules.OrdinaryPopulationCap ||
            !ActiveRunTacticalCommandPolicy.TryLegacyLoadout(
                run.LegacyHeroId,
                rules,
                out var migratedCommands))
            return false;

        var heroCell = run.Version == 2
            ? BattlefieldLayout.Version2HeroCell
            : run.LegacyHeroCell?.ToCell();
        var deploymentCells = run.Version == 2
            ? BattlefieldLayout.Version2SoldierCells.Select(FormationCellDto.FromCell).ToList()
            : run.LegacyDeploymentCells;
        if (heroCell is null || deploymentCells is null ||
            deploymentCells.Count != LegacyDeploymentCapacity ||
            !BattlefieldLayout.IsPlayerDeploymentCell(heroCell.Value) ||
            deploymentCells.Any(cell => cell is null || !BattlefieldLayout.IsPlayerDeploymentCell(cell.ToCell())))
            return false;

        var rosterIds = new HashSet<string>(StringComparer.Ordinal);
        if (run.Roster.Any(hero => string.IsNullOrWhiteSpace(hero.InstanceId) || !rosterIds.Add(hero.InstanceId)))
            return false;
        var deployedIds = new HashSet<string>(StringComparer.Ordinal);
        for (var slot = 0; slot < LegacyDeploymentCapacity; slot++)
        {
            var instanceId = run.Deployment[slot];
            if (!string.IsNullOrEmpty(instanceId) &&
                (!rosterIds.Contains(instanceId) || !deployedIds.Add(instanceId)))
                return false;
        }

        var startingInstanceId = "player-hero";
        for (var suffix = 2; rosterIds.Contains(startingInstanceId); suffix++)
            startingInstanceId = $"player-hero-{suffix}";
        var migratedDeployment = EmptyDeployment();
        var occupiedCells = new HashSet<Vector2I>();
        if (!TryPlace(migratedDeployment, occupiedCells, startingInstanceId, heroCell.Value)) return false;
        for (var slot = 0; slot < LegacyDeploymentCapacity; slot++)
        {
            var instanceId = run.Deployment[slot];
            if (!string.IsNullOrEmpty(instanceId) &&
                !TryPlace(migratedDeployment, occupiedCells, instanceId, deploymentCells[slot].ToCell()))
                return false;
        }

        run.Roster.Insert(0, new RosterHeroInstanceDto
        {
            InstanceId = startingInstanceId,
            ContentId = run.LegacyHeroId,
            HealthRatio = run.LegacyHeroHealthRatio,
            Rank = 1
        });
        run.CurrentPopulation = (int)migratedPopulation;
        run.PopulationCapSources = [];
        run.Deployment = migratedDeployment;
        run.EquippedTacticalCommandIds = migratedCommands;
        run.Version = CurrentVersion;
        run.LegacyHeroId = null;
        run.LegacyHeroHealthRatio = 0;
        run.LegacyHeroCell = null;
        run.LegacyDeploymentCells = null;
        return true;
    }

    public static List<string> EmptyDeployment() =>
        Enumerable.Repeat(string.Empty, BattlefieldLayout.PlayerDeploymentCells.Length).ToList();

    public static List<FormationCellDto> CloneCells(IEnumerable<Vector2I> cells)
    {
        var result = new List<FormationCellDto>();
        foreach (var cell in cells) result.Add(FormationCellDto.FromCell(cell));
        return result;
    }

    private static bool TryPlace(
        List<string> deployment,
        HashSet<Vector2I> occupied,
        string instanceId,
        Vector2I cell)
    {
        var slot = BattlefieldLayout.PlayerDeploymentSlot(cell);
        if (slot < 0 || !occupied.Add(cell) || !string.IsNullOrEmpty(deployment[slot])) return false;
        deployment[slot] = instanceId;
        return true;
    }
}

// Pure validation policy shared by persistence and battle preparation. It has
// no service dependency and never mutates the Run projection.
public static class RunFormationPolicy
{
    public static bool Validate(
        ActiveRunDto run,
        CompiledRunRules rules,
        System.Func<Vector2I, bool>? canOccupy = null)
    {
        if (run is null || rules is null || run.Deployment is null ||
            run.Deployment.Count != rules.PhysicalDeploymentCeiling ||
            run.Deployment.Any(id => id is null) ||
            rules.PhysicalDeploymentCeiling != BattlefieldLayout.PlayerDeploymentCells.Length ||
            !RunPopulationPolicy.Validate(run, rules))
            return false;

        var deployed = new HashSet<string>(StringComparer.Ordinal);
        for (var slot = 0; slot < run.Deployment.Count; slot++)
        {
            if (string.IsNullOrEmpty(run.Deployment[slot])) continue;
            if (!deployed.Add(run.Deployment[slot]) ||
                canOccupy?.Invoke(BattlefieldLayout.PlayerDeploymentCells[slot]) == false)
                return false;
        }
        return true;
    }
}
