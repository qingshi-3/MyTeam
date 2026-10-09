using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;

public partial class V1EquipmentContractSmoke
{
    private static BattleSimulation EquipmentBattle(ContentRegistry content, CompiledEquipmentDefinition item, int copies)
    {
        var profile = AbilityPrototypeContent.Snapshot(content, 2) with { AbilityLoadout = null, Behavior = new(Stationary:true) };
        var items = Enumerable.Range(0,copies).Select(slot => new EquipmentBattleInstanceSnapshot(
            "item-"+slot,item.StableId,"hero",slot,item)).ToImmutableArray();
        var config = new BattleConfig { Seed=20261007,HeroRule=HeroRuleSnapshot.Neutral,
            FloorRule=new ClearFloorRuleRuntime("equipment-combat","装备测试",""),
            Equipment=new(EquipmentStateFingerprint.Compute(items),items) };
        config.Spawns.Add(new(profile,0,new(3,2),"hero",IsPersistentRosterHero:true));
        config.Spawns.Add(new(profile,1,new(4,2),"enemy"));
        var battle=new BattleSimulation(config);
        foreach(var unit in battle.Units)
        {
            unit.AttackCooldown=10000;
            unit.Attributes.SetBaseValue(CombatAttribute.Armor,0);
            unit.Attributes.SetBaseValue(CombatAttribute.CriticalChance,0);
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond,0);
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerDamageRatio,0);
        }
        var hero=battle.Units.Single(u=>u.Team==0);var enemy=battle.Units.Single(u=>u.Team==1);
        enemy.Position=hero.Position+new Vector2(hero.BodyRadius+enemy.BodyRadius+.1f,0);
        return battle;
    }

    private static void DamageProbe(BattleSimulation battle,float amount,bool area)
    {
        var hero=battle.Units.Single(u=>u.Team==0);
        var definition=AbilityDefinitionCompiler.Compile(new AbilityDefinition {StableId="equipment_probe",DisplayName="装备伤害检查",
            ActivationKind=AbilityActivationKind.Triggered,Trigger=AbilityTriggerKind.OwnerEnteredBattle,
            Operations=[new MatrixAbilityOperationSpec {Kind=MatrixOperationKind.Damage,Target=MatrixTargetKind.NearestEnemies,
                Range=20,MaxTargets=area?3:1,Amount=amount}]}).Ability!;
        var world=(IAbilityRuntimeWorld)battle;var ready=world.Prepare(definition,hero.RuntimeId,hero.RuntimeId,"",battle.TickIndex);
        Require(ready.Succeeded&&world.Commit(ready.Plan!).Succeeded,"real damage operation commits");
    }

    private static void LifeStealCombat(ContentRegistry content,CompiledEquipmentDefinition item)
    {
        foreach(var copies in new[]{1,2})
        {
            using var battle=EquipmentBattle(content,item,copies);
            var hero=battle.Units.Single(u=>u.Team==0);var enemy=battle.Units.Single(u=>u.Team==1);
            hero.Health=200;enemy.Shield=100;
            DamageProbe(battle,100,false);
            Require(Math.Abs(hero.Health-200)<.001f,"absorbed damage does not heal");
            DamageProbe(battle,100,false);
            Require(Math.Abs(hero.Health-212)<.001f,"one or two blades heal 12 percent actual health damage once");
            DamageProbe(battle,100,true);
            Require(Math.Abs(hero.Health-216)<.001f,"area efficiency applies even when only one target is present");
            enemy.Health=10;DamageProbe(battle,1000,false);
            Require(Math.Abs(hero.Health-217.2f)<.01f,"overkill only heals from the last ten health");
            Require(battle.CombatEvents.Where(e=>e.Kind==BattleCombatEventKind.HealthLost&&e.SourceRuntimeId==hero.RuntimeId)
                .Any(e=>e.IsAreaDamage),"area metadata reaches authoritative health loss");
        }
    }

    private static void ArmorBreakCombat(ContentRegistry content,CompiledEquipmentDefinition item)
    {
        using var battle=EquipmentBattle(content,item,2);
        var hero=battle.Units.Single(u=>u.Team==0);var enemy=battle.Units.Single(u=>u.Team==1);
        enemy.Attributes.SetBaseValue(CombatAttribute.Armor,100);
        DamageProbe(battle,10,false);
        Require(enemy.Armor==100,"skill damage does not apply attack-only armor break");
        hero.AttackCooldown=0;
        for(var i=0;i<30&&!battle.CombatEvents.Any(e=>e.Kind==BattleCombatEventKind.AttackLanded&&e.SourceRuntimeId==hero.RuntimeId);i++) battle.Step();
        Require(Math.Abs(enemy.Armor-80)<.001f,"two copies apply a single 20 percent armor reduction on actual attack impact");
        hero.AttackCooldown=10000;
        var first=battle.CombatEvents.Count(e=>e.Kind==BattleCombatEventKind.AttackLanded&&e.SourceRuntimeId==hero.RuntimeId);
        for(var i=0;i<10;i++)battle.Step();
        hero.AttackCooldown=0;
        for(var i=0;i<30&&battle.CombatEvents.Count(e=>e.Kind==BattleCombatEventKind.AttackLanded&&e.SourceRuntimeId==hero.RuntimeId)==first;i++)battle.Step();
        Require(Math.Abs(enemy.Armor-80)<.001f&&enemy.Statuses.Count(s=>s.StableId=="v1_armor_break")==1,
            "a later real hit refreshes instead of stacking armor break");
        hero.AttackCooldown=10000;
        for(var i=0;i<41;i++)battle.Step();
        Require(Math.Abs(enemy.Armor-100)<.001f,"armor returns after refreshed duration expires");
    }
}
