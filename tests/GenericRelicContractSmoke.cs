using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;

public partial class GenericRelicContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var publication = await GrowthContentPackage.CreateReadyAsync(this,
                defaultNewRunProfileId: RunProfileIds.GrowthTowerLegacy);
            var package = publication.Package ?? throw new InvalidOperationException(string.Join(";", publication.Report.CoreErrors));
            var relics = package.Content.Graph.Relics.Where(r => r.StableId.StartsWith("item_rl", StringComparison.Ordinal)).ToArray();
            Check(relics.Length == 25, "25 independent definitions published");
            foreach (var relic in relics)
            {
                Check(package.Content.TryGet(relic.StableId, out var entry) && entry.Definition is ItemDefinition { Icon: null }, "icons intentionally blank");
                var node = entry.Scene.Instantiate<ItemContentRoot>();
                Check(!node.ValidateAuthoring().HasCoreErrors, "standalone item scene"); node.Free();
            }
            VerifyRunRules(package);
            VerifyBattleRules(package);
            VerifyRepeatRules(package);
            VerifyBountyTransaction(package);
            GD.Print("GENERIC_RELIC_CONTRACT_OK definitions=25 icons=blank run=discount-bank-heal-interest-skip-pools battle=conditional-crit-overheal-burn-shred-haste-dodge repeat=entry-death bounty=real-kills-save-retry-once");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("GENERIC_RELIC_CONTRACT_FAILED: " + e); GetTree().Quit(1); }
    }

    private static void VerifyRunRules(CompiledGamePackage package)
    {
        var (app, save) = RunHealthFixture.New(package);
        var run = app.ActiveRun!;
        foreach (var id in new[] { "item_rl01_card", "item_rl02_bank", "item_rl03_ticket", "item_rl04_idol", "item_rl07_bowl", "item_rl08_piggy", "item_rl10_recycler", "item_rl05_star" }) Add(run, package, id);
        var rules = new RunRelicRules(package.Content);
        run.CurrentRunHealth = 1; run.Gold = 10;
        rules.EnterNode(run, TowerNodeType.Shop);
        Check(run.Gold == 12 && run.CurrentRunHealth == 5, "node gold + gold heal + shop heal");
        rules.PaidShopPurchase(run); rules.EnterNode(run, TowerNodeType.Combat);
        Check(run.Gold == 12, "bank stops after paid purchase");
        var stock = new CompiledRunChoice("stock", "库存", "", [], [new(RunOperationKind.SpendGold, 5)],
            [new(RunOperationKind.GrantItem, 1, ContentId: "equipment_field_focus")], FailureOperations: []);
        run.SelectedNode = TowerNodeType.Shop; run.PendingNode = true;
        run.PendingOffer = new("relic-shop", RunOfferKind.Shop, "商店", true, true, run.FloorIndex, run.BattleNumber, [stock, stock with { StableId = "stock2" }]);
        save.SaveActiveRun(run);
        Check(app.PendingOffer!.Choices[0].Costs[0].Amount == 3 && run.PendingOffer.Choices[0].Costs[0].Amount == 5, "discount does not mutate frozen base price");
        var before = JsonSerializer.Serialize(run); save.FailWrites = true;
        Check(app.ResolveOffer("relic-shop", "stock").Failure == RunDecisionFailure.PersistenceFailed && JsonSerializer.Serialize(run) == before, "shop failure atomic");
        save.FailWrites = false;
        Check(app.ResolveOffer("relic-shop", "stock").Succeeded && run.Gold == 9 && app.PendingOffer!.Choices[0].Costs[0].Amount == 3, "discount payment and remaining shelf never compounded");
        var restored = new RunApplication(package.Content, save, package.Project);
        Check(restored.PendingOffer!.Choices.Single().Costs[0].Amount == 3, "discount reload stable");
        var skip = new PendingRunOffer("skip", RunOfferKind.CombatReward, "装备奖励", true, false, 0, 0, []) { RewardCategory = RunRewardCategory.Equipment };
        var max = run.MaximumRunHealth; var hp = run.CurrentRunHealth; var gold = run.Gold;
        rules.SkipReward(run, skip);
        Check(run.MaximumRunHealth == max + 2 && run.CurrentRunHealth == Math.Min(max + 2, hp + 3) && run.Gold == gold + 5, "skip max health + salvage + gold healing");
        run.SelectedNode = TowerNodeType.Combat;
        var elite = app.CurrentEncounter() with { IsElite = true };
        run.BattleNumber = 1;
        var result = new BattleResult(BattleOutcome.PlayerVictory, 1, "test", [], 2);
        string awarded = "";
        gold = run.Gold;
        Check(rules.SettleBattle(run, result, elite, 12, (_, id) => { awarded = id; return true; }) &&
            run.Gold == gold + 2 && !string.IsNullOrEmpty(awarded), "interest uses held pre-reward gold and elite uses configured pool");
        run.BattleNumber = 9; gold = run.Gold;
        rules.SettleBattle(run, result, elite, 100, (_, _) => true);
        Check(run.Gold == gold, "interest ends after configured phase");
        run.Items.RemoveAll(item => item.ContentId == "item_rl08_piggy");
        Check(!RunItemSupplyPolicy.CanAcquire(run, "item_rl08_piggy", package.Content), "expired interest relic cannot enter late supply");
        GD.Print("GENERIC_RELIC_RUN_OK");
    }

    private static void VerifyBattleRules(CompiledGamePackage package)
    {
        foreach (var (id, expected) in new[] { ("item_rl11_crowbar", 15f), ("item_rl12_crystal", 12f), ("item_rl13_rounds", 12f) })
        {
            using var battle = Probe(package, [id], out var floor, boss: true);
            var enemy = battle.Units.First(u => u.Team == 1); var before = enemy.Health;
            floor.Context!.Damage(battle.Units[0].RuntimeId, enemy, 10);
            Near(before - enemy.Health, expected, id);
        }
        using (var battle = Probe(package, ["item_rl11_crowbar"], out var floor))
        {
            var enemy = battle.Units.First(u => u.Team == 1); enemy.Health = enemy.MaxHealth * .5f;
            var hp = enemy.Health; floor.Context!.Damage(battle.Units[0].RuntimeId, enemy, 10);
            Near(hp - enemy.Health, 10, "crowbar switches off below threshold");
        }
        foreach (var different in new[] { false, true })
        {
            var statusIds = different ? new[] { "status_bc_burn", "status_bc_poison", "status_bc_mark" } :
                new[] { "status_bc_burn", "status_bc_burn", "status_bc_burn" };
            var setup = new CompiledAbilityDefinition("fixture_debuffs", "减益验证", "", AbilityActivationKind.Triggered,
                AbilityTriggerKind.OwnerEnteredBattle, 0, 0, 0, 0, 0,
                statusIds.Select(id => (CompiledAbilityOperation)new CompiledApplyStatusAbilityOperation(package.Content.Graph.ResolveStatus(id),
                    new CompiledRelativeTeamTargetQuery(EffectRelativeTeam.Enemies, false))).ToImmutableArray(), null);
            using var battle = Probe(package, ["item_rl18_mark"], out var floor, ability: setup);
            var target = battle.Units.Last(); var hp = target.Health;
            floor.Context!.Damage(battle.Units[0].RuntimeId, target, 10);
            Near(hp - target.Health, different ? 13 : 10, "death mark counts distinct debuffs, not stacks");
        }
        {
            var (app, _) = RunHealthFixture.New(package); var run = app.ActiveRun!;
            Add(run, package, "item_rl06_insect"); run.SelectedNode = TowerNodeType.Elite;
            using var battle = new BattleSimulation(app.BuildBattleConfig(app.CurrentEncounter(), false));
            foreach (var enemy in battle.Units.Where(unit => unit.Team == 1))
                Near(enemy.Health / enemy.MaxHealth, .75f, "elite starts wounded without reducing max health");
        }
        using (var battle = Probe(package, ["item_rl09_padding", "item_rl21_hoodie"], out _))
        {
            var owner = battle.Units[0]; Near(owner.MaxHealth, 1020, "gold contributes temporary health");
            Near(owner.Attributes.GetValue(CombatAttribute.DodgeChance), .1f, "hoodie dodge");
            Near(owner.Attributes.GetValue(CombatAttribute.AttackSpeed), 1.2f, "hoodie live conversion");
            owner.Attributes.SetBaseValue(CombatAttribute.DodgeChance, .2f);
            Near(owner.Attributes.GetValue(CombatAttribute.AttackSpeed), 1.6f, "external dodge updates conversion");
            Near(owner.AttackRange, 2.4f, "hoodie range penalty");
        }
        using (var battle = Probe(package, ["item_rl14_gasoline", "item_rl19_topaz"], out var floor))
        {
            var owner = battle.Units[0]; var victim = battle.Units.First(u => u.Team == 1);
            floor.Context!.Damage(owner.RuntimeId, victim, 20000);
            Check(!victim.Alive && battle.Units.Last().Statuses.Any(s => s.StableId == "status_bc_burn"), "real kill ignites nearby survivor");
            Near(owner.Shield, 15, "real kill shields killer");
        }
        using (var battle = Probe(package, ["item_rl15_scythe", "item_rl16_aegis", "item_rl17_hammer", "item_rl20_instinct"], out var floor))
        {
            var owner = battle.Units[0]; owner.Attributes.SetBaseValue(CombatAttribute.CriticalChance, 1);
            for (var i = 0; i < 80 && !battle.Units.Any(u => u.Statuses.Any(s => s.StableId == "status_rl17_armor_broken")); i++) battle.Step();
            Check(battle.Units.Any(u => u.Statuses.Any(s => s.StableId == "status_rl17_armor_broken")), "five real attack hits shred target");
            Check(owner.Statuses.Single(s => s.StableId == "status_rl20_haste").Stacks == 3, "crit haste bounded at three stacks");
            Check(owner.Shield >= 4, "crit healing at full health becomes shield");
            var shield = owner.Shield; floor.Context!.Heal(owner, 20); Near(owner.Shield - shield, 10, "overheal 50 percent");
        }
        using (var battle = Probe(package, ["item_rl22_riposte"], out _))
        {
            var owner = battle.Units[0]; owner.Attributes.SetBaseValue(CombatAttribute.DodgeChance, 1);
            var enemy = battle.Units.First(u => u.Team == 1); enemy.Damage = 10;
            for (var i = 0; i < 30 && !battle.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.AttackDodged); i++) battle.Step();
            Check(battle.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.AttackDodged) &&
                battle.CombatEvents.Any(e => e.Source.StableId == "status_rl22_passive" && e.Kind == BattleCombatEventKind.DamageResolved), "dodge creates actual retaliation damage");
        }
        GD.Print("GENERIC_RELIC_BATTLE_OK");
    }

    private static void VerifyRepeatRules(CompiledGamePackage package)
    {
        foreach (var entry in new[] { true, false })
        {
            var ability = new CompiledAbilityDefinition("fixture_trigger", "验证能力", "", AbilityActivationKind.Triggered,
                entry ? AbilityTriggerKind.OwnerEnteredBattle : AbilityTriggerKind.OwnerDefeated, 0, 0, 0, 0, 0,
                [new CompiledEffectAbilityOperation(new("fixture_damage", 0, new(EffectTriggerKind.Manual, EffectDomainEventKind.None), [],
                    new CompiledRelativeTeamTargetQuery(EffectRelativeTeam.Enemies, false), [new(EffectKind.Damage, EffectAmountSource.Fixed, 7)],
                    new(0, 0, 0, 0), null), AbilityInvocationValueSource.Fixed, 1)], null);
            using var battle = Probe(package, [entry ? "item_rl23_ashes" : "item_rl24_snuffer"], out var floor, ability: ability);
            if (!entry) { floor.Context!.Damage(battle.Units.Last().RuntimeId, battle.Units[0], 2000); battle.Step(); }
            foreach (var enemy in battle.Units.Where(u => u.Team == 1)) Near(enemy.MaxHealth - enemy.Health, 14, "entry/death extra execution");
        }
        GD.Print("GENERIC_RELIC_REPEAT_OK");
    }

    private static void VerifyBountyTransaction(CompiledGamePackage package)
    {
        var rules = GrowthContentPackage.LoadRules(package.Content);
        var save = new RunHealthMemorySave(); var app = new RunApplication(package.Content, save, package.Project, rules);
        Check(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 77112), "growth run");
        var run = app.ActiveRun!; Add(run, package, "item_rl25_bounty");
        run.Items.Single().Counters.Single().Value = 4; save.SaveActiveRun(run);
        var opponents = app.CurrentOpponentOptions(TowerNodeType.Combat);
        Check(app.SelectNode(TowerNodeType.Combat, opponents.Count == 0 ? null : opponents[0].OptionId), "select real opponent");
        var encounter = app.CurrentEncounter(); Check(app.TryBeginGrowthBattle(encounter).Succeeded, "freeze battle");
        BattleResult result;
        using (var battle = new BattleSimulation(app.BuildBattleConfig(encounter, false)))
        {
            foreach (var ally in battle.Units.Where(u => u.Team == 0)) { ally.Damage = 100000; ally.Shield = 100000; ally.Attributes.SetBaseValue(CombatAttribute.AttackRange, 100); }
            result = battle.RunToEnd();
            Check(result.Outcome == BattleOutcome.PlayerVictory && result.RelicTransition!.CounterTransitions.Sum(c => c.ThresholdExecutions) > 0, "real attack kills cross persisted threshold");
        }
        var before = JsonSerializer.Serialize(run); var saved = save.Json; save.FailWrites = true;
        Check(!app.ResolveBattle(result, encounter).Accepted && JsonSerializer.Serialize(run) == before && save.Json == saved, "bounty award and counter rollback with failed save");
        save.FailWrites = false; var count = run.EquipmentInventory.Count;
        var awards = result.RelicTransition!.CounterTransitions.Sum(c => c.ThresholdExecutions);
        Check(app.ResolveBattle(result, encounter).Accepted && run.EquipmentInventory.Count == count + awards &&
            run.EquipmentInventory.Skip(count).All(i => i.ContentId == "equipment_field_focus"), "configured equipment awarded on retry");
        Check(!app.ResolveBattle(result, encounter).Accepted && run.EquipmentInventory.Count == count + awards, "duplicate receipt cannot pay twice");
        var restored = new RunApplication(package.Content, save, package.Project, rules);
        Check(restored.ActiveRun!.Items.Single().Counters.Single().Value == run.Items.Single().Counters.Single().Value, "remainder survives reload");
        GD.Print("GENERIC_RELIC_BOUNTY_OK");
    }

    private static BattleSimulation Probe(CompiledGamePackage package, string[] ids, out ProbeFloor floor, bool boss = false, CompiledAbilityDefinition? ability = null)
    {
        using var scope = new RelicRunScope(new(998, "probe", 0, 0));
        foreach (var id in ids)
        {
            var definition = package.Content.Graph.ResolveRelic(id);
            scope.Activate(definition, new() { InstanceId = id, ContentId = id, Stacks = 1, Counters = RelicRunScope.InitialRunCounters(definition).ToList() });
        }
        floor = new ProbeFloor();
        var owner = new UnitSnapshot("probe", "验证者", UnitRole.Fighter, true, false, 1000, 1, 3, 2, 10, 0, 0, 0, 0, [],
            new(Stationary: true), ability is null ? null : new([ability]));
        var enemy = owner with { ContentId = "target", IsHero = false, IsBoss = boss, MaxHealth = 10000, Damage = 0, AbilityLoadout = null };
        return new(new BattleConfig { Seed = 998, FloorRule = floor, StartingGold = 10, Relics = scope.PrepareBattle(),
            HeroRule = new(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, ""),
            Spawns = [new(owner, 0, new(0, 0), "owner"), new(enemy, 1, new(1, 0), "enemy1"), new(enemy, 1, new(2, 0), "enemy2")] });
    }
    private static void Add(ActiveRunDto run, CompiledGamePackage package, string id) => run.Items.Add(new()
    { InstanceId = $"item-{100 + run.Items.Count}", ContentId = id,
      Counters = RelicRunScope.InitialRunCounters(package.Content.Graph.ResolveRelic(id)).Select(c => new RelicCounterStateDto { CounterId = c.CounterId, Value = c.Value }).ToList() });
    private sealed class ProbeFloor() : ClearFloorRuleRuntime("probe", "验证场", "")
    { public BattleRuleContext? Context; public override void OnBattleStarted(BattleRuleContext context) => Context = context; }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Near(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < .01, $"{message}: {actual} != {expected}");
}
