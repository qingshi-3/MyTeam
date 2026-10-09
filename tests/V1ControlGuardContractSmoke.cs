using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Statuses;

public partial class V1ControlGuardContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var authored = GD.Load<EquipmentDefinition>("res://content/equipment/definitions/v1_equipment_e15.tres");
            var compiled = EquipmentDefinitionCompiler.Compile(authored);
            Require(compiled.Definition is not null, string.Join(';', compiled.Report.CoreErrors));
            var equipment = compiled.Definition!;
            Require(equipment.ControlGuard == new CompiledEquipmentControlGuard("first_hard_control",30,.5f), "authored guard contract");
            var changed = (EquipmentDefinition)authored.Duplicate(true);
            changed.ControlGuard!.WindowTicks = 31;
            Require(EquipmentDefinitionCompiler.Compile(changed).Definition!.Fingerprint != equipment.Fingerprint, "guard parameters affect content fingerprint");
            changed.ControlGuard.SubsequentDurationMultiplier = float.NaN;
            Require(EquipmentDefinitionCompiler.Compile(changed).Definition is null, "invalid guard rejected");
            Boundaries(equipment);
            Rollback(equipment.ControlGuard!);
            var content = await SkillSampleContent.LoadAsync(this);
            RealWindup(content, equipment);
            DragonStartup(content, equipment);
            GD.Print("V1_CONTROL_GUARD_OK authored-fingerprint duplicate-unique remove-provider first-intercept same-batch half-duration boundary local-world-rollback real-u14-windup real-u13-startup");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("V1_CONTROL_GUARD_FAILED " + error); GetTree().Quit(1); }
    }

    private static CompiledStatusDefinition Hard(int ticks = 9) => BattleMechanicStatus.Create("guard_probe", "控制测试", ticks, StatusBehaviorKind.DisableActions, true);
    private static void Boundaries(CompiledEquipmentDefinition definition)
    {
        using var attributes = new BattleAttributeScope("guard-boundaries");
        var owner = attributes.CreateSet("hero", AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute,float>
        { [CombatAttribute.MaxHealth]=700,[CombatAttribute.ControlResistance]=0 }));
        var items = new[]{1,0}.Select(slot => new EquipmentBattleInstanceSnapshot("guard-"+slot,definition.StableId,"hero",slot,definition)).ToImmutableArray();
        using var equipment = new EquipmentBattleScope("guard-boundaries",new(EquipmentStateFingerprint.Compute(items),items),[new("hero","hero",true,owner)]);
        Require(owner.GetValue(CombatAttribute.MaxHealth)==940, "ordinary HP stacks on two copies");
        var lifecycle = new List<StatusLifecycleEvent>();var cleanses = 0;
        using var status = new BattleStatusScope("guard-boundaries",(_,_)=>{},_=>owner,null,lifecycle.Add,null,
            controlGuardResolver:equipment.ResolveControlGuard,controlGuardSink:_=>cleanses++);
        var disarm = BattleMechanicStatus.Create("guard_disarm","缴械",12,harmful:true) with { GrantedTags=[StatusDefinitionCompiler.AttackDisabledTag] };
        Require(status.Apply(disarm,"enemy","hero",0).Status is not null && cleanses==0,"disarm does not consume guard");
        var before=lifecycle.Count;
        var pair=status.ApplyBatch([new(Hard(),"enemy","hero",1),new(Hard(),"enemy","hero",1)]);
        Require(pair[0].Applied&&pair[0].Status is null&&pair[0].AddedStacks==0&&pair[1].Status?.RemainingTicks==5,
            "same batch intercepts first and halves second with ceiling");
        Require(cleanses==1&&lifecycle.Skip(before).Count(e=>e.Kind==StatusLifecycleKind.Applied)==1,"intercept publishes no hard-control application");
        Require(equipment.Remove("guard-0"),"remove deterministic first provider");
        Require(status.Apply(Hard(7),"enemy","hero",2).Status?.RemainingTicks==4&&cleanses==1,"remaining copy cannot reset first use");
        Require(status.Apply(Hard(1),"enemy","hero",30).Status?.RemainingTicks==1,"one tick floor within half-open window");
        Require(status.Apply(Hard(),"enemy","hero",31).Status?.RemainingTicks==9&&cleanses==1,"window closes exactly at first tick+30 without rearming");
        Require(equipment.Remove("guard-1")&&equipment.ResolveControlGuard("hero") is null,"last removal removes qualification");
        Require(status.Apply(Hard(),"enemy","hero",32).Status?.RemainingTicks==9,"unequipped owner receives ordinary duration");
        using var resisted = new BattleStatusScope("guard-resistance",(_,_)=>{},_=>owner,null,null,null,
            controlGuardResolver:_=>definition.ControlGuard);
        owner.SetBaseValue(CombatAttribute.ControlResistance,.25f);
        Require(resisted.Apply(Hard(9),"enemy","hero",0).Status is null,"fresh battle arms once");
        Require(resisted.Apply(Hard(9),"enemy","hero",1).Status?.RemainingTicks==4,"ceil resistance first then guard half");
    }

    private static void Rollback(CompiledEquipmentControlGuard guard)
    {
        var reject=true;var accepted=0;
        using var status=new BattleStatusScope("guard-rollback",(_,_)=>{},_=>null,null,null,null,
            controlGuardResolver:_=>guard,controlGuardSink:_=>{if(reject)throw new InvalidOperationException("expected guard fault");accepted++;});
        try {status.Apply(Hard(),"enemy","hero",0);throw new InvalidOperationException("fault missing");}
        catch(InvalidOperationException error) when(error.Message=="expected guard fault") { }
        Require(status.LiveInstanceCount==0,"local failure leaves no hard-control instance");
        reject=false;
        Require(status.Apply(Hard(),"enemy","hero",0).Status is null&&accepted==1,"local retry can intercept again");
        using var world=new BattleStatusScope("guard-world",(_,_)=>{},_=>null,null,null,null,controlGuardResolver:_=>guard);
        var checkpoint=world.BeginWorldTransaction();
        Require(world.Apply(Hard(),"enemy","hero",0).Status is null,"world transaction first interception");
        checkpoint.Rollback();
        Require(world.Apply(Hard(),"enemy","hero",0).Status is null,"world rollback restores first-use ledger");
        Require(world.Apply(Hard(),"enemy","hero",1).Status?.RemainingTicks==5,"successful retry consumes exactly once");
        var failLifecycle=true;
        using var batch=new BattleStatusScope("guard-batch-fault",(_,_)=>{},_=>null,null,_=>{if(failLifecycle)throw new InvalidOperationException("expected second fault");},null,controlGuardResolver:_=>guard);
        try {batch.ApplyBatch([new(Hard(),"enemy","hero",0),new(Hard(),"enemy","hero",0)]);throw new InvalidOperationException("fault missing");}
        catch(InvalidOperationException error) when(error.Message=="expected second fault") { }
        failLifecycle=false;
        Require(batch.Apply(Hard(),"enemy","hero",0).Status is null&&batch.LiveInstanceCount==0,"later same-batch failure rolls back first interception");
    }

    private static BattleSimulation Battle(ContentRegistry content,int number,CompiledEquipmentDefinition equipment)
    {
        var profile=BattleSetupFactory.Snapshot(GD.Load<UnitDefinition>($"res://content/definitions/heroes/prototype_u{number:00}.tres"),
            abilityLoadout:AbilityDefinitionCompiler.CompileLoadout(GD.Load<AbilityLoadoutDefinition>($"res://content/abilities/loadouts/prototype_u{number:00}.tres")).Loadout!,graph:content.Graph) with {Behavior=new(Stationary:true)};
        var enemy=AbilityPrototypeContent.Snapshot(content,4) with {AbilityLoadout=null,Behavior=new(Stationary:true)};
        var items=ImmutableArray.Create(new EquipmentBattleInstanceSnapshot("amulet",equipment.StableId,"hero",0,equipment));
        var config=new BattleConfig {Seed=20261007,HeroRule=HeroRuleSnapshot.Neutral,FloorRule=new ClearFloorRuleRuntime("guard-test","护符测试",""),Equipment=new(EquipmentStateFingerprint.Compute(items),items)};
        config.Spawns.Add(new(profile,0,new(3,2),"hero",IsPersistentRosterHero:true));
        config.Spawns.Add(new(enemy,1,new(4,2),"enemy"));
        var battle=new BattleSimulation(config);
        foreach(var unit in battle.Units)
        {unit.AttackCooldown=10000;unit.CurrentMana=0;unit.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond,0);unit.Attributes.SetBaseValue(CombatAttribute.ManaPerDamageRatio,0);}
        var ally=battle.Units.Single(unit=>unit.Team==0);var foe=battle.Units.Single(unit=>unit.Team==1);
        foe.Position=ally.Position+new Vector2(ally.BodyRadius+foe.BodyRadius+.2f,0);
        return battle;
    }
    private static void Control(BattleSimulation battle)
    {
        var enemy=battle.Units.Single(unit=>unit.Team==1);
        var ability=AbilityDefinitionCompiler.Compile(new AbilityDefinition {StableId="guard_control",DisplayName="护符控制测试",
            ActivationKind=AbilityActivationKind.Triggered,Trigger=AbilityTriggerKind.OwnerEnteredBattle,
            Operations=[new MatrixAbilityOperationSpec {Kind=MatrixOperationKind.Control,Target=MatrixTargetKind.NearestEnemies,Range=20,DurationTicks=9}]}).Ability!;
        var world=(IAbilityRuntimeWorld)battle;var ready=world.Prepare(ability,enemy.RuntimeId,enemy.RuntimeId,"",battle.TickIndex);
        Require(ready.Succeeded&&world.Commit(ready.Plan!).Succeeded,"formal source control still succeeds when intercepted");
    }
    private static void RealWindup(ContentRegistry content,CompiledEquipmentDefinition equipment)
    {
        foreach(var second in new[]{false,true})
        {
            using var battle=Battle(content,14,equipment);var hero=battle.Units.Single(unit=>unit.Team==0);
            hero.CurrentMana=hero.MaxMana;battle.Step();Control(battle);
            Require(!battle.CombatEvents.Any(e=>e.Kind==BattleCombatEventKind.ControlApplied&&e.TargetRuntimeId==hero.RuntimeId),"first control never publishes cancellation fact");
            if(second)Control(battle);
            for(var tick=0;tick<5;tick++)battle.Step();
            Require(battle.CombatEvents.Any(e=>e.Kind==BattleCombatEventKind.UnitTransformed&&e.SourceRuntimeId==hero.RuntimeId)!=second,
                "real U14 windup survives first control and is cancelled by second control");
        }
    }
    private static void DragonStartup(ContentRegistry content,CompiledEquipmentDefinition equipment)
    {
        using var battle=Battle(content,13,equipment);var hero=battle.Units.Single(unit=>unit.Team==0);
        Control(battle);hero.CurrentMana=hero.MaxMana;battle.Step();
        Require(battle.CombatEvents.Any(e=>e.Kind==BattleCombatEventKind.UnitTransformed&&e.SourceRuntimeId==hero.RuntimeId),"real U13 still starts after intercepted control");
    }
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
