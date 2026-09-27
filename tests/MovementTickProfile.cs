using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Growth;
using static TraitMatrixValidationSupport;

public partial class MovementTickProfile : Node
{
    private static readonly string[] Allies =
        ["hero_mx01", "hero_mx02", "hero_mx03", "hero_mx04", "hero_mx05", "hero_mx10"];
    private static readonly string[] Enemies =
        ["enemy_rust_guard", "enemy_crossbow", "enemy_cutpurse", "enemy_hexer", "enemy_carrion", "enemy_scale_brute"];

    public override async void _Ready()
    {
        try
        {
            var publication = await GrowthContentPackage.CreateReadyAsync(this);
            var package = publication.Package ??
                throw new InvalidOperationException(string.Join(';', publication.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            Profile("open", Laboratory(index, Allies, Enemies, 20260927));

            var congested = Laboratory(index, Allies, Enemies, 20260927);
            congested = new BattleConfig
            {
                Seed = congested.Seed,
                FloorRule = new NarrowLanesRuntime("movement-profile-narrow", "拥堵窄路", "temporary profile"),
                HeroRule = congested.HeroRule,
                Spawns = congested.Spawns
            };
            Profile("narrow", congested);
            GD.Print("MOVEMENT_TICK_PROFILE_OK");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PrintErr("MOVEMENT_TICK_PROFILE_FAILED " + exception);
            GetTree().Quit(1);
        }
    }

    private static void Profile(string label, BattleConfig config)
    {
        using var battle = new BattleSimulation(config);
        for (var tick = 0; tick < 5 && battle.Outcome == BattleOutcome.Running; tick++)
        {
            battle.Step();
            battle.DrainEvents();
        }

        var samples = new double[30];
        var moveEvents = 0;
        var measured = 0;
        for (; measured < samples.Length && battle.Outcome == BattleOutcome.Running; measured++)
        {
            var stopwatch = Stopwatch.StartNew();
            battle.Step();
            stopwatch.Stop();
            samples[measured] = stopwatch.Elapsed.TotalMilliseconds;
            moveEvents += battle.DrainEvents().Count(item => item.Type == "move");
        }
        if (measured == 0) throw new InvalidOperationException(label + " battle ended during warmup");
        var ordered = samples.Take(measured).Order().ToArray();
        var p95 = ordered[Math.Clamp((int)Math.Ceiling(ordered.Length * .95) - 1, 0, ordered.Length - 1)];
        GD.Print($"MOVEMENT_TICK_PROFILE label={label} ticks={measured} mean_ms={ordered.Average():F3} " +
                 $"p95_ms={p95:F3} max_ms={ordered[^1]:F3} over_100ms={ordered.Count(value => value >= 100)} moves={moveEvents}");
    }
}
