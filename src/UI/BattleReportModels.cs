using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Domain;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public enum BattleReportDimension
{
    Overview,
    Offense,
    Survival,
    Healing
}

[Flags]
public enum BattleReportAwards
{
    None = 0,
    DamageLeader = 1,
    DamageTakenLeader = 2,
    HealingLeader = 4
}

public sealed record BattleReportTeamViewModel(
    int Team,
    string Title,
    int Survivors,
    int Casualties,
    int Kills,
    float DamageDealt,
    float HealingDone,
    float RemainingHealth,
    float MaximumHealth,
    float EnvironmentDamage)
{
    public float RemainingHealthRatio => MaximumHealth <= 0 ? 0 : Math.Clamp(RemainingHealth / MaximumHealth, 0, 1);
}

public sealed record BattleReportUnitViewModel(
    BattleUnitReportSnapshot Unit,
    float ActiveLifetimeSeconds,
    float DamageShare,
    float HealingShare,
    float DamageTakenShare,
    float DamagePerSecond,
    float HealingPerSecond,
    float FinalHealthRatio,
    BattleReportAwards Awards);

public sealed record BattleReportViewModel(
    BattleReportDimension Dimension,
    int SelectedTeam,
    BattleReportTeamViewModel PlayerTeam,
    BattleReportTeamViewModel EnemyTeam,
    IReadOnlyList<BattleReportUnitViewModel> Units,
    IReadOnlyList<BattleReportUnitViewModel> OutputLeaders,
    IReadOnlyList<BattleReportUnitViewModel> DamageTakenLeaders,
    IReadOnlyList<BattleReportUnitViewModel> HealingLeaders,
    IReadOnlyList<BattleUnitReportSnapshot> PlayerRoster,
    IReadOnlyList<BattleUnitReportSnapshot> EnemyRoster,
    float PrimaryMaximum,
    bool ShowHealingEmptyState);

public sealed record BattleReportRosterPortraitModel(
    BattleUnitReportSnapshot Unit,
    UnitPortraitDefinition? Portrait,
    Texture2D Fallback);

public sealed record BattleReportCoreMatchupViewModel(
    BattleReportDimension Dimension,
    IReadOnlyList<BattleReportUnitViewModel> PlayerLeaders,
    IReadOnlyList<BattleReportUnitViewModel> EnemyLeaders)
{
    public bool BothSidesZero => PlayerLeaders.Count == 0 && EnemyLeaders.Count == 0;
}

public static class BattleReportViewModels
{
    private const float Epsilon = 0.001f;

    public static BattleReportViewModel Build(BattleResult result, int selectedTeam, BattleReportDimension dimension)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (selectedTeam is not 0 and not 1) throw new ArgumentOutOfRangeException(nameof(selectedTeam));

        var selected = result.Units.Where(unit => unit.Team == selectedTeam).ToArray();
        var totalDamage = selected.Sum(unit => unit.DamageDealt);
        var totalHealing = selected.Sum(unit => unit.HealingDone);
        var totalTaken = selected.Sum(unit => unit.DamageTaken);
        var maxDamage = selected.Select(unit => unit.DamageDealt).DefaultIfEmpty().Max();
        var maxTaken = selected.Select(unit => unit.DamageTaken).DefaultIfEmpty().Max();
        var maxHealing = selected.Select(unit => unit.HealingDone).DefaultIfEmpty().Max();

        var units = selected.Select(unit =>
        {
            var activeTicks = Math.Max(1, (unit.DefeatTick ?? result.Ticks) - unit.JoinTick);
            var activeSeconds = activeTicks * BattleTiming.TickSeconds;
            var awards = BattleReportAwards.None;
            if (maxDamage > Epsilon && NearlyEqual(unit.DamageDealt, maxDamage)) awards |= BattleReportAwards.DamageLeader;
            if (maxTaken > Epsilon && NearlyEqual(unit.DamageTaken, maxTaken)) awards |= BattleReportAwards.DamageTakenLeader;
            if (maxHealing > Epsilon && NearlyEqual(unit.HealingDone, maxHealing)) awards |= BattleReportAwards.HealingLeader;
            return new BattleReportUnitViewModel(
                unit,
                activeSeconds,
                Share(unit.DamageDealt, totalDamage),
                Share(unit.HealingDone, totalHealing),
                Share(unit.DamageTaken, totalTaken),
                unit.DamageDealt / activeSeconds,
                unit.HealingDone / activeSeconds,
                unit.MaxHealth <= 0 ? 0 : Math.Clamp(unit.FinalHealth / unit.MaxHealth, 0, 1),
                awards);
        });

        var unitModels = units.ToArray();
        var ordered = dimension switch
        {
            BattleReportDimension.Offense => unitModels.OrderByDescending(model => model.Unit.DamageDealt)
                .ThenBy(model => model.Unit.RuntimeId, StringComparer.Ordinal),
            BattleReportDimension.Survival => unitModels.OrderByDescending(model => model.Unit.DamageTaken)
                .ThenBy(model => model.Unit.RuntimeId, StringComparer.Ordinal),
            BattleReportDimension.Healing => unitModels.OrderByDescending(model => model.Unit.HealingDone)
                .ThenBy(model => model.Unit.RuntimeId, StringComparer.Ordinal),
            _ => unitModels.OrderByDescending(model => model.Unit.IsHero)
                .ThenBy(model => model.Unit.IsTemporary)
                .ThenBy(model => model.Unit.RuntimeId, StringComparer.Ordinal)
        };

        var primaryMaximum = dimension switch
        {
            BattleReportDimension.Offense => maxDamage,
            BattleReportDimension.Survival => maxTaken,
            BattleReportDimension.Healing => maxHealing,
            _ => selected.Select(unit => unit.MaxHealth).DefaultIfEmpty().Max()
        };

        return new BattleReportViewModel(
            dimension,
            selectedTeam,
            Team(result, 0),
            Team(result, 1),
            ordered.ToArray(),
            Leaders(unitModels, model => model.Unit.DamageDealt, maxDamage),
            Leaders(unitModels, model => model.Unit.DamageTaken, maxTaken),
            Leaders(unitModels, model => model.Unit.HealingDone, maxHealing),
            Roster(result, 0),
            Roster(result, 1),
            primaryMaximum,
            dimension == BattleReportDimension.Healing && totalHealing <= Epsilon);
    }

    public static IReadOnlyList<BattleReportCoreMatchupViewModel> BuildCoreMatchups(BattleResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var player = Build(result, 0, BattleReportDimension.Overview);
        var enemy = Build(result, 1, BattleReportDimension.Overview);
        return
        [
            new BattleReportCoreMatchupViewModel(
                BattleReportDimension.Offense,
                player.OutputLeaders,
                enemy.OutputLeaders),
            new BattleReportCoreMatchupViewModel(
                BattleReportDimension.Survival,
                player.DamageTakenLeaders,
                enemy.DamageTakenLeaders),
            new BattleReportCoreMatchupViewModel(
                BattleReportDimension.Healing,
                player.HealingLeaders,
                enemy.HealingLeaders)
        ];
    }

    private static BattleReportUnitViewModel[] Leaders(
        IEnumerable<BattleReportUnitViewModel> units,
        Func<BattleReportUnitViewModel, float> value,
        float maximum) => maximum <= Epsilon
        ? []
        : units.Where(unit => NearlyEqual(value(unit), maximum))
            .OrderBy(unit => unit.Unit.RuntimeId, StringComparer.Ordinal)
            .ToArray();

    private static BattleUnitReportSnapshot[] Roster(BattleResult result, int team) => result.Units
        .Where(unit => unit.Team == team)
        .OrderByDescending(unit => unit.IsHero)
        .ThenBy(unit => unit.IsTemporary)
        .ThenBy(unit => unit.RuntimeId, StringComparer.Ordinal)
        .ToArray();

    private static BattleReportTeamViewModel Team(BattleResult result, int team)
    {
        var own = result.Units.Where(unit => unit.Team == team).ToArray();
        var opponent = result.Units.Where(unit => unit.Team == 1 - team).ToArray();
        var damageTaken = own.Sum(unit => unit.DamageTaken);
        var creditedOpponentDamage = opponent.Sum(unit => unit.DamageDealt);
        var environmentDamage = Math.Max(0, damageTaken - creditedOpponentDamage);
        if (environmentDamage <= Epsilon) environmentDamage = 0;
        return new BattleReportTeamViewModel(
            team,
            team == 0 ? "我方" : "敌方",
            own.Count(unit => unit.Alive),
            own.Count(unit => !unit.Alive),
            own.Sum(unit => unit.Kills),
            own.Sum(unit => unit.DamageDealt),
            own.Sum(unit => unit.HealingDone),
            own.Sum(unit => Math.Max(0, unit.FinalHealth)),
            own.Sum(unit => Math.Max(0, unit.MaxHealth)),
            environmentDamage);
    }

    private static float Share(float value, float total) => total <= Epsilon ? 0 : Math.Clamp(value / total, 0, 1);
    private static bool NearlyEqual(float left, float right) => Math.Abs(left - right) <= Epsilon;
}
