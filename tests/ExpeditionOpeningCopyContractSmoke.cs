using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

// A regression fixture, not a claim that these heroes or fabricated stat gains
// have been obtained on a natural route. All preparation uses the real Run path.
public partial class ExpeditionOpeningCopyContractSmoke : Node
{
    private static readonly string[] ForestIds = ["prototype_u11", "prototype_u16", "prototype_u17"];

    public override async void _Ready()
    {
        try
        {
            var publication = await GrowthContentPackage.CreateReadyAsync(this);
            var package = publication.Package ?? throw new InvalidOperationException(string.Join("; ", publication.Report.CoreErrors));
            var expedition = (CompiledExpedition)package.Project.Campaign.Progression;
            var preparation = new RunBattlePreparationService(package.Content, package.Project,
                new RunRelicService(package.Content), package.GrowthRules);
            var profile = expedition.Encounters["ember_combat_profile"];
            var plan = new EncounterPlan(profile.Encounter.Title("森林复制回归"),
                profile.Encounter.FloorRulePool.ContentIds[0], profile.Composition.EnemyIds,
                false, false, profile.Encounter.StableId, profile.Encounter.NodeType)
            {
                CompositionId = profile.Composition.StableId, EnemyCells = profile.Composition.Cells,
                ExpeditionProfileId = profile.StableId, ExpeditionSiteId = "opening-copy-contract"
            };

            var chosen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var seed in new ulong[] { 20261006, 1776, 9173 })
            {
                var run = Fixture(package, seed, 3);
                var config = preparation.Build(run, plan, true);
                foreach (var id in ForestIds)
                {
                    Require(config.TacticalSummons.TryGetValue(id, out var clean), "every potential copy member has a profile: " + id);
                    var authored = BattleSetupFactory.Snapshot(package.Content.Catalog.Heroes.Single(entry => entry.StableId == id), package.Content);
                    Near(clean!.MaxHealth, authored.MaxHealth, "copy template author base health");
                    Near(clean.Damage, authored.Damage, "copy template author base damage");
                    var grown = config.Spawns.Single(spawn => spawn.Team == 0 && spawn.Unit.ContentId == id).Unit;
                    Near(grown.MaxHealth, authored.MaxHealth + 5000, "original keeps fixture health growth");
                    Near(grown.Damage, authored.Damage + 1000, "original keeps fixture attack growth");
                }
                using var battle = new BattleSimulation(config);
                var clone = battle.Units.Single(unit => unit.Team == 0 && unit.IsTemporary);
                var original = battle.Units.Single(unit => unit.RuntimeId == clone.SummonerRuntimeId);
                var cleanProfile = config.TacticalSummons[clone.Definition.ContentId];
                chosen.Add(clone.Definition.ContentId);
                Require(battle.Units.Count(unit => unit.Team == 0 && !unit.IsTemporary) == 3,
                    "copy does not replace any permanent roster member");
                Require(battle.TraitSnapshot.Value("prototype_032", 0) == 3,
                    "copy contributes no extra forest count");
                Require(!clone.IsPersistentRosterHero && clone.SourceInstanceId.Length == 0,
                    "copy is a temporary battle body without persistent roster identity");
                Near(clone.MaxHealth, cleanProfile.MaxHealth * .6f, "copy scaled clean authored health");
                Near(clone.Definition.Damage, cleanProfile.Damage, "copy definition excludes original growth");
                Require(clone.HasManaSkill && clone.Definition.AllowTemporaryMana &&
                    clone.Definition.AbilityLoadout!.Abilities.Select(ability => ability.StableId).SequenceEqual(
                        cleanProfile.AbilityLoadout!.Abilities.Select(ability => ability.StableId)),
                    "copy retains complete base mana abilities");
                Require(!ReferenceEquals(clone.Attributes, original.Attributes), "copy owns independent attribute state");
                var originalAttack = original.Damage;
                var originalHealth = original.Health;
                clone.Attributes.SetBaseValue(CombatAttribute.AttackDamage, clone.Damage + 10);
                clone.Health -= 10;
                Near(original.Damage, originalAttack, "copy attribute edits do not mutate original");
                Near(original.Health, originalHealth, "copy health edits do not mutate original");
            }
            using (var inactive = new BattleSimulation(preparation.Build(Fixture(package, 20261006, 2), plan, true)))
            {
                Require(inactive.TraitSnapshot.Value("prototype_032", 0) == 2 &&
                    !inactive.Units.Any(unit => unit.IsTemporary), "unreached forest tier does not spawn a copy");
            }
            GD.Print("EXPEDITION_OPENING_COPY_CONTRACT_OK run-preparation all-member-clean-profiles zero-inherited-fixture-growth temporary noncounting independent base-mana inactive-tier seeds=20261006,1776,9173 sampled=" + string.Join(',', chosen.Order()));
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PrintErr("EXPEDITION_OPENING_COPY_CONTRACT_FAILED " + error);
            GetTree().Quit(1);
        }
    }

    private static ActiveRunDto Fixture(CompiledGamePackage package, ulong seed, int count)
    {
        var run = new ActiveRunDto
        {
            ProfileId = package.DefaultNewRunProfileId, Seed = seed, BattleNumber = 0,
            CurrentPopulation = package.Project.RunRules.InitialPopulation, Gold = 0,
            CurrentRunHealth = 100, MaximumRunHealth = 100,
            Growth = new GrowthRunDto { RulesId = package.GrowthRules!.StableId }
        };
        var cells = new[] { new Vector2I(0, 2), new Vector2I(2, 2), new Vector2I(2, 4) };
        for (var i = 0; i < count; i++)
        {
            var hero = new RosterHeroInstanceDto
            {
                InstanceId = "forest-contract-" + i, ContentId = ForestIds[i],
                Growth = new HeroGrowthDto { AddedAttack = 1000, AddedMaxHealth = 5000 }
            };
            run.Roster.Add(hero);
            run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(cells[i])] = hero.InstanceId;
        }
        return run;
    }

    private static void Near(float actual, float expected, string message) => Require(Math.Abs(actual - expected) < .01f, message);
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
