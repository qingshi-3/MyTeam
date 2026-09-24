using System;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class EnemyPiercingContractSmoke : Node
{
    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var snapshots = new[] { "enemy_es01_piercing_crossbow", "enemy_es04_prism_caster" }.Select(id =>
            {
                Require(package.Content.TryGet(id, out var entry), "published specialist: " + id);
                return BattleSetupFactory.Snapshot(entry, package.Content);
            }).ToArray();
            foreach (var snapshot in snapshots)
            {
                var ability = snapshot.AbilityLoadout!.Abilities.Single();
                var line = (CompiledChargedLineOperation)ability.Operations.Single();
                using (var battle = new BattleSimulation(Config(snapshot)))
                {
                    Step(battle, 21);
                    var caster = Unit(battle, "caster");
                    Require(caster.ChargedLine is not null && caster.Position == new Vector2(9, 2), "cast without walking into basic range");
                    Require(Hits(battle) == 0 && !battle.PendingEvents.Any(e => e.Type == "attack"), "charge reserves action slot and deals no early damage");
                    battle.Step();
                    if (line.Delivery == ChargedLineDelivery.Projectile)
                    {
                        Require(Hits(battle) == 0 && battle.Projectiles.Length == 1, "arrow has real travel and no launch-time damage");
                        var endpoint = battle.PendingEvents.Single(e => e.Type == "line_release").Position;
                        for (var i = 0; i < 12 && Hits(battle) < 2; i++) battle.Step();
                        Require(Hits(battle) == 2 && battle.Projectiles.Length == 1,
                            "piercing flight survives reaching the damage cap");
                        var lastHitTick = battle.TickIndex;
                        for (var i = 0; i < 12 && !battle.Projectiles.IsEmpty; i++) battle.Step();
                        var end = battle.PendingEvents.Single(e => e.Type == "projectile_end");
                        Require(battle.Projectiles.IsEmpty && end.Tick > lastHitTick && end.Position.DistanceTo(endpoint) < .001f,
                            "piercing arrow reaches the telegraphed endpoint after both impacts");
                    }
                    Require(Hits(battle) == 2 && Unit(battle, "third").Health == 1000, "front-to-back maximum two distinct targets");
                    var first = 1000 - Unit(battle, "first").Health;
                    var second = 1000 - Unit(battle, "second").Health;
                    Near(second / first, 2f / 3f, "second target damage falloff");
                    var expected = snapshot.Damage * line.AttackMultiplier;
                    Near(first, expected * 100f / (100f + 30f * 7f), "projectile and beam both use the target defense");
                    Require(battle.CombatEvents.Where(e => e.Kind == BattleCombatEventKind.SkillHitLanded).All(e => e.DamageType == line.DamageType), "damage type retained on skill hit facts");
                    var hits = Hits(battle); Step(battle, 10);
                    Require(Hits(battle) == hits, "no repeat hits or continuous beam damage");
                }
                using (var battle = new BattleSimulation(Config(snapshot)))
                {
                    Step(battle, 10);
                    var locked = Unit(battle, "caster").ChargedLine!;
                    Unit(battle, "first").Position = new(3, 4);
                    Step(battle, 24);
                    Require(Unit(battle, "first").Health == 1000 && Unit(battle, "second").Health < 1000,
                        "moving original target cannot redirect the locked line");
                    Require(battle.PendingEvents.Single(e => e.Type == "line_release").Position == locked.End, "release and warning share endpoints");
                }
                using (var battle = new BattleSimulation(Config(snapshot)))
                {
                    Step(battle, 10); Unit(battle, "first").Health = 0; Step(battle, 24);
                    Require(Hits(battle) == 2, "dead original target does not cancel or retarget the shot");
                }
                using (var battle = new BattleSimulation(Config(snapshot)))
                {
                    Step(battle, 10); Unit(battle, "caster").DisabledTicks = 2; Step(battle, 24);
                    Require(Unit(battle, "caster").ChargedLine is null && Hits(battle) == 0 && battle.Projectiles.IsEmpty &&
                        battle.PendingEvents.Count(e => e.Type == "line_cancel") == 1, "control cancels exactly once and preserves cooldown");
                }
                using (var battle = new BattleSimulation(Config(snapshot)))
                {
                    Step(battle, 10); Unit(battle, "caster").Position += Vector2.Up; Step(battle, 24);
                    Require(Hits(battle) == 0 && battle.PendingEvents.Any(e => e.Type == "line_cancel"), "moved caster cannot fire from stale warning");
                }
                using (var battle = new BattleSimulation(Config(snapshot)))
                {
                    Step(battle, 10); Unit(battle, "caster").Team = 0; battle.Step();
                    Require(Unit(battle, "caster").ChargedLine is null && Hits(battle) == 0, "allegiance change cancels pending attack");
                }
                var deathConfig = Config(snapshot);
                var killer = deathConfig.Spawns[1];
                deathConfig.Spawns[1] = killer with { Unit = killer.Unit with
                    { Damage = 1000, Range = 10, AttackTicks = 9999, Behavior = new(Stationary: true) } };
                deathConfig.Spawns.Add(new(killer.Unit, 1, new(9, 5), "spare"));
                using (var battle = new BattleSimulation(deathConfig))
                {
                    Unit(battle, "first").AttackCooldown = 16;
                    Step(battle, 30);
                    Require(!Unit(battle, "caster").Alive && battle.Outcome == BattleOutcome.Running &&
                        Unit(battle, "caster").ChargedLine is null &&
                        battle.PendingEvents.Count(e => e.Type == "line_cancel") == 1 &&
                        !battle.PendingEvents.Any(e => e.Type == "line_release"), "actual lethal damage cancels before scope teardown");
                }
                using (var first = new BattleSimulation(Config(snapshot)))
                using (var second = new BattleSimulation(Config(snapshot)))
                {
                    Step(first, 10); Require(Unit(second, "caster").ChargedLine is null, "shared ability never shares mutable cast state");
                    Step(first, 24); Step(second, 34);
                    Require(first.CreateResult().Digest == second.CreateResult().Digest, "deterministic cast and impact ordering");
                }
                using (var battle = new BattleSimulation(Config(snapshot, failCommit: true)))
                {
                    Step(battle, 10);
                    Require(Unit(battle, "caster").ChargedLine is null && !battle.PendingEvents.Any(e => e.Type == "line_charge"),
                        "failed ability commit restores cast and telegraph facts");
                }
                var disposable = new BattleSimulation(Config(snapshot));
                Step(disposable, 10); var owner = Unit(disposable, "caster"); disposable.Dispose();
                Require(owner.ChargedLine is null && disposable.Projectiles.IsEmpty, "scope teardown clears pending cast and projectiles");
            }
            var store = new BattleLabPresetStore(GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres"));
            var index = new BattleLabContentIndex(package);
            foreach (var code in new[] { "ES01", "ES04" })
            {
                var preset = store.BuiltIns.Single(p => p.Key.StartsWith(code + " ·", StringComparison.Ordinal));
                using var battle = new BattleSimulation(new BattleLabPreparationAdapter(index).Build(BattleLabPresetStore.ToSnapshot(preset.Value)));
                Step(battle, 38);
                Require(battle.PendingEvents.Any(e => e.Type == "line_release"), "shipped preset releases " + code);
            }
            CheckTerrain(snapshots[1]);
            CheckDefense(snapshots[1]);
            CheckEncountersAndScaling(package);
            GD.Print("ENEMY_PIERCING_CONTRACT_OK publication range charge travel locked-line target-death max-hits falloff unified-defense control movement allegiance rollback isolation deterministic teardown presets terrain");
        }
        catch (Exception error) { exit = 1; GD.PrintErr("ENEMY_PIERCING_CONTRACT_FAILED " + error); }
        GetTree().Quit(exit);
    }

    public static BattleConfig Config(UnitSnapshot caster, bool failCommit = false)
    {
        using var definition = new UnitDefinition { Id = "piercing_test_body", DisplayName = "贯穿测试目标", IsHero = true,
            MaxHealth = 1000, AttackDamage = 0, Armor = 30 };
        var target = BattleSetupFactory.Snapshot(definition) with { Behavior = new(Stationary: true, DisableBasicAttacks: true) };
        return new BattleConfig
        {
            Seed = 1609, FloorRule = new ClearFloorRuleRuntime("piercing_test", "常规", ""),
            HeroRule = new HeroRuleSnapshot(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, ""),
            Spawns = [new(caster, 1, new(9, 2), "caster"), new(target, 0, new(3, 2), "first"),
                new(target, 0, new(2, 2), "second"), new(target, 0, new(1, 2), "third")],
            ConfigureCombatBindings = failCommit ? bindings => bindings.Subscribe(BattleCombatEventKind.AbilityResolved,
                CombatSourceRef.System("charged_line_rollback"), 0, (_, sink) =>
                    sink.Enqueue(CombatSourceRef.System("charged_line_rollback"), 0,
                        _ => throw new InvalidOperationException("charged line rollback probe"))) : null
        };
    }
    private static void CheckEncountersAndScaling(CompiledGamePackage package)
    {
        var generator = new TowerGenerator(package.Project.Campaign);
        var prepare = new RunBattlePreparationService(package.Content, package.Project, new RunRelicService(package.Content));
        for (var region = 0; region < 3; region++)
        foreach (var kind in new[] { TowerNodeType.Combat, TowerNodeType.Elite, TowerNodeType.Boss })
        for (ulong seed = 1; seed <= 12; seed++)
        {
            var run = new ActiveRunDto { Seed = seed, FloorIndex = region * 5,
                EquippedTacticalCommandIds = package.Project.RunRules.StarterTacticalCommandIds.ToList(),
                Roster = [new() { InstanceId = "hero", ContentId = "hero_hc03_iron_guard" }] };
            run.Deployment[0] = "hero";
            var encounter = generator.Encounter(run, kind);
            Require(encounter.EnemyIds.Count(id => id.StartsWith("enemy_es", StringComparison.Ordinal)) <= 1,
                "bounded lead slot never randomly repeats specialists");
            if (seed != 1) continue;
            var config = prepare.Build(run, encounter, false);
            var authored = package.Project.Campaign.Regions[region].Encounters[kind];
            foreach (var spawn in config.Spawns.Where(s => s.Team == 1))
            {
                package.Content.TryGet(spawn.Unit.ContentId, out var entry);
                var baseline = BattleSetupFactory.Snapshot(entry, package.Content);
                Near(spawn.Unit.MaxHealth, baseline.MaxHealth * authored.EnemyHealthMultiplier, "encounter HP applied to snapshot");
                Near(spawn.Unit.Damage, baseline.Damage * authored.EnemyDamageMultiplier, "encounter attack applied to snapshot");
                using var battle = new BattleSimulation(config);
                var state = battle.Units.Single(u => u.RuntimeId == spawn.InstanceId);
                Near(state.MaxHealth, spawn.Unit.MaxHealth, "scaled HP reaches battle attributes");
                Near(state.Damage, spawn.Unit.Damage, "scaled attack reaches skill input");
            }
        }
    }
    private static void CheckDefense(UnitSnapshot caster)
    {
        using var battle = new BattleSimulation(Config(caster));
        Unit(battle, "first").Attributes.SetBaseValue(CombatAttribute.Armor, 60);
        Step(battle, 22);
        Near(1000 - Unit(battle, "first").Health, caster.Damage * 1.2f * 100f / 520f, "higher defense reduces beam damage through the same curve");
    }
    private static void CheckTerrain(UnitSnapshot caster)
    {
        // A target moved behind a boundary cannot enlarge the already clipped trajectory.
        using var battle = new BattleSimulation(Config(caster));
        Step(battle, 10);
        var cast = Unit(battle, "caster").ChargedLine!;
        Require(cast.End.X >= -.5f && cast.End.X < .5f && cast.End.Y == 2, "telegraph clipped at arena terrain boundary");
    }
    public static void Step(BattleSimulation battle, int ticks) { for (var i = 0; i < ticks; i++) battle.Step(); }
    private static BattleUnitState Unit(BattleSimulation battle, string id) => battle.Units.Single(u => u.RuntimeId == id);
    private static int Hits(BattleSimulation battle) => battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.SkillHitLanded);
    private static void Near(float a, float b, string message) => Require(Math.Abs(a - b) < .01f, $"{message}: {a} != {b}");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
