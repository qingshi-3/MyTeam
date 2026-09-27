using System;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;

public partial class PlayerUnitBalanceContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var content = gate.Package?.Content ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var enemyDefinitions = content.Catalog.Enemies.Select(x => x.Definition.ResourcePath).ToHashSet();
            foreach (var entry in content.Catalog.Heroes)
                Require(!enemyDefinitions.Contains(entry.Definition.ResourcePath) && entry.Definition is UnitDefinition { IsHero: true, IsEnemy: false },
                    "hero and enemy tuning use independent authored definitions: " + entry.StableId);
            var enemiesBefore = content.Catalog.Enemies.Select(x => BattleSetupFactory.Snapshot(x, content)).ToArray();
            foreach (var id in new[] { "hero_hc18_healing_reader", "hero_hc19_overheal_priest" })
            {
                Require(content.TryGet(id, out var entry), "published healer");
                var hero = BattleSetupFactory.Snapshot(entry, content);
                using var battle = new BattleSimulation(new BattleConfig
                {
                    Seed = 20260926,
                    FloorRule = new ClearFloorRuleRuntime("healer-contract", "常规", ""),
                    HeroRule = new(1,1,1,0,0,0,false,"",1,1,0,0,0,0,false,false,0,0,""),
                    Spawns = [new(hero, 0, new(1,2), "hero"), new(Dummy(), 0, new(2,1), "ally", .3f),
                              new(Dummy(), 1, new(4,2), "enemy")]
                });
                var healer = battle.Units.Single(x => x.RuntimeId == "hero");
                var ally = battle.Units.Single(x => x.RuntimeId == "ally");
                var enemy = battle.Units.Single(x => x.RuntimeId == "enemy");
                var mana = healer.MaxMana;
                // Even a stale save/test attribute may not turn a basic attack into healing.
                healer.Attributes.SetBaseValue(CombatAttribute.HealingPower, 100);
                healer.Attributes.SetBaseValue(CombatAttribute.MaxMana, 0);
                var before = ally.Health;
                for (var tick = 0; tick < 35; tick++) battle.Step();
                Require(ally.Health == before && enemy.Health < enemy.MaxHealth,
                    "healer damages an enemy with basic attacks while an ally is wounded");
                Require(!battle.CombatEvents.Any(x => x.Kind == BattleCombatEventKind.HealingResolved),
                    "ordinary attacks never emit healing or healing-to-damage reactions");
                healer.Attributes.SetBaseValue(CombatAttribute.MaxMana, mana);
                healer.CurrentMana = mana;
                for (var tick = 0; tick < 20 && ally.Health == before; tick++) battle.Step();
                Require(ally.Health > before && battle.CombatEvents.Any(x => x.Kind == BattleCombatEventKind.ManaSkillResolved),
                    "authored mana skill still heals a wounded ally");
                Require(battle.CombatEvents.Where(x => x.Kind == BattleCombatEventKind.HealingResolved && x.SourceRuntimeId == "hero")
                        .All(x => x.Source.StableId == (id.Contains("hc18") ? "ability_hc18_a" : "ability_hc19_a")),
                    "healing is attributed to the skill, not the basic attack");
                Require(((UnitDefinition)entry.Definition).HealPower == 0 &&
                        BattleSetupFactory.Snapshot(entry, content).HealPower == 0,
                    "runtime attribute changes do not mutate the reusable hero resource");
            }
            var enemiesAfter = content.Catalog.Enemies.Select(x => BattleSetupFactory.Snapshot(x, content)).ToArray();
            Require(enemiesBefore.Zip(enemiesAfter).All(pair => pair.First.MaxHealth == pair.Second.MaxHealth &&
                pair.First.Damage == pair.Second.Damage && pair.First.Armor == pair.Second.Armor &&
                pair.First.AbilityLoadout == pair.Second.AbilityLoadout), "player battle changes leave enemy definitions untouched");
            GD.Print("PLAYER_UNIT_BALANCE_CONTRACT_OK basic-damage skill-healing reaction-origin authored-isolation");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("PLAYER_UNIT_BALANCE_CONTRACT_FAILED " + error); GetTree().Quit(1); }
    }

    private static UnitSnapshot Dummy()
    {
        using var definition = new UnitDefinition { Id = "balance_dummy", DisplayName = "耐久靶", MaxHealth = 10000,
            AttackDamage = 0, Armor = 0 };
        return BattleSetupFactory.Snapshot(definition) with { Behavior = new(Stationary: true, DisableBasicAttacks: true) };
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
