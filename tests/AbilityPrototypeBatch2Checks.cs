using System;
using System.Linq;
using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using static AbilityPrototypeBatch1Checks;

internal static class AbilityPrototypeBatch2Checks
{
    internal static void Run(ContentRegistry content)
    {
        Guard(content);
        IncomingProjectile(content);
        Storm(content);
        StormDenseAndOverlap(content);
        StormInterruptAndRollback(content);
        GD.Print("B4_BATCH2_ABILITY_OK guard-fixed-facing-front-back-angle-skill-pass holds-action original-projectile-bearing storm-dense-anchor-ten-area-pulses enter-exit-overlap owner-death expiry interrupt atomic-retry deterministic");
    }
    private static void Near(float actual,float expected,string message)=>Require(Math.Abs(actual-expected)<.03f,$"{message}: actual={actual} expected={expected}");
    private static void Guard(ContentRegistry content)
    {
        var profile=Profile(content,1);Require(profile.MaxHealth==1000&&profile.Damage==40&&profile.Armor==8&&profile.AttackTicks==18,"U01 published stats");
        using var battle=Battle(content,1);var hero=Hero(battle);var enemy=Enemy(battle);enemy.Position=new(5,2);
        hero.Attributes.SetBaseValue(CombatAttribute.Armor,0);enemy.Attributes.SetBaseValue(CombatAttribute.AttackRange,10);enemy.Damage=100;
        hero.CurrentMana=hero.MaxMana;Steps(battle,3);Require(battle.FrontalGuards.IsEmpty,"guard waits for 0.3-second windup");Steps(battle,1);
        var guard=battle.FrontalGuards.Single();var start=hero.Position;Require(guard.Direction==Vector2.Right,"guard freezes the initial target bearing");
        var heldActive=Loadout("u01").Find("prototype_u01_a")! with {ActivationKind=AbilityActivationKind.Triggered,Trigger=AbilityTriggerKind.ActionQueued};
        var abilityWorld=(IAbilityRuntimeWorld)battle;
        Require(!abilityWorld.Prepare(heldActive,hero.RuntimeId,hero.RuntimeId,enemy.RuntimeId,battle.TickIndex).Succeeded,
            "explicit triggered active ability cannot bypass a held guard");
        var passive=heldActive with {Trigger=AbilityTriggerKind.OwnerEnteredBattle};
        var passivePlan=abilityWorld.Prepare(passive,hero.RuntimeId,hero.RuntimeId,enemy.RuntimeId,battle.TickIndex);
        Require(passivePlan.Succeeded,"holding an action does not block passive preparation");
        Require(!abilityWorld.Commit(passivePlan.Plan! with {Ability=heldActive}).Succeeded,
            "commit rechecks held action before starting an active ability");
        foreach(var angle in new[]{0f,60f,61f,180f})
        {
            enemy.Position=hero.Position+Vector2.Right.Rotated(Mathf.DegToRad(angle))*2;
            var health=hero.Health;enemy.AttackCooldown=0;hero.AttackCooldown=0;battle.Step();enemy.AttackCooldown=10000;
            Near(health-hero.Health,angle<=60?45:100,"120-degree inclusive attack boundary");
            Require(hero.Position==start&&!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==hero.RuntimeId),"holding guard prevents voluntary movement and basic attack");
            Require(battle.FrontalGuards.Single().Direction==Vector2.Right,"moving target does not turn the shield");
        }
        enemy.Position=new(5,2);var before=hero.Health;
        Invoke(battle,enemy,new() {Kind=MatrixOperationKind.AdditionalAttack,Target=MatrixTargetKind.NearestEnemies,Range=20,Amount=100});
        Near(before-hero.Health,45,"qualified additional attack shares frontal protection");
        // Direct damage deliberately uses the normal matrix world; it is not an attack impact.
        before=hero.Health;Invoke(battle,enemy,new() {Kind=MatrixOperationKind.Damage,Target=MatrixTargetKind.NearestEnemies,Range=20,Amount=100});
        Near(before-hero.Health,100,"direct ability damage bypasses the attack-only guard");
        while(battle.TickIndex<guard.ExpiresTick)battle.Step();Require(battle.FrontalGuards.IsEmpty,"guard ends at its four-second lease boundary");
        enemy.Position=new(3.95f,2);hero.AttackCooldown=0;battle.Step();
        Require(battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==hero.RuntimeId),"basic action resumes after the guard");
    }
    private static void IncomingProjectile(ContentRegistry content)
    {
        var shooter=Profile(content,19) with {AbilityLoadout=null,Behavior=new(Stationary:true),ProjectileSpeed=2,ProjectileWindupSeconds=0};
        using var battle=Battle(content,1,enemyProfile:shooter);var hero=Hero(battle);var enemy=Enemy(battle);
        hero.Attributes.SetBaseValue(CombatAttribute.Armor,0);enemy.Attributes.SetBaseValue(CombatAttribute.AttackRange,10);enemy.Damage=100;
        enemy.Position=new(7,2);hero.CurrentMana=hero.MaxMana;Steps(battle,4);
        enemy.AttackCooldown=0;battle.Step();enemy.AttackCooldown=10000;
        Require(battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==enemy.RuntimeId),"actual original projectile is released from the front");
        enemy.Position=new(1,2);var health=hero.Health;Steps(battle,24);
        var fact=battle.CombatEvents.Single(f=>f.Kind==BattleCombatEventKind.DamageResolved&&f.TargetRuntimeId==hero.RuntimeId);
        Require(fact.AttackIncomingDirection is {X:> .99f},"projectile carries reverse velocity, independent of later source movement");
        Near(health-hero.Health,45,"front flying attack remains protected after shooter moves behind");
    }
    private static void Storm(ContentRegistry content)
    {
        var profile=Profile(content,6);Require(profile.MaxHealth==760&&profile.Damage==40&&profile.Armor==4&&profile.AttackTicks==15,"U06 published stats");
        foreach(var ownerDies in new[]{false,true})
        {
            // Another persistent ally keeps the battle running after the caster dies.
            using var battle=StormBattle(content);var hero=HeroStorm(battle);var enemy=Enemy(battle);
            hero.CurrentMana=hero.MaxMana;Steps(battle,8);Require(battle.GroundStorms.IsEmpty,"storm is absent throughout interruptible windup");Steps(battle,1);
            var field=battle.GroundStorms.Single();Require(field.Center==new Vector2(5,2)&&field.Radius==2&&field.ExpiresTick-field.StartedTick==50,"dense stable target becomes a fixed five-second circle");
            Near(enemy.Attributes.GetValue(CombatAttribute.AttackSpeed),.75f,"storm applies one 25 percent multiplicative slow");
            hero.Position=new(1,4);if(ownerDies)hero.Health=0;
            Steps(battle,5);Require(battle.GroundStorms.Single().Center==field.Center,"formed storm is independent of caster movement and death");
            var pulses=battle.CombatEvents.Where(f=>f.Kind==BattleCombatEventKind.DamageResolved&&f.Source.StableId=="prototype_u06_a").ToArray();
            Require(pulses.Length==1&&pulses[0].Tick==field.StartedTick+5&&pulses[0].IsAreaDamage,"first half-second pulse is marked area even with one enemy");Near(pulses[0].RequestedValue,38,"38 percent spell power pulse");
            enemy.Position=new(8,2);battle.Step();Near(enemy.Attributes.GetValue(CombatAttribute.AttackSpeed),.75f,"slow remains on first detected exit");
            Steps(battle,4);Near(enemy.Attributes.GetValue(CombatAttribute.AttackSpeed),.75f,"slow lasts the authored five ticks after exit");
            Steps(battle,1);Near(enemy.Attributes.GetValue(CombatAttribute.AttackSpeed),1,"leaving slow expires without residue");
            enemy.Position=field.Center;battle.Step();Near(enemy.Attributes.GetValue(CombatAttribute.AttackSpeed),.75f,"entering formed field reapplies slow");
            while(battle.TickIndex<field.ExpiresTick)battle.Step();Require(battle.GroundStorms.IsEmpty,"formed field expires exactly once after five seconds");
            Steps(battle,5);Near(enemy.Attributes.GetValue(CombatAttribute.AttackSpeed),1,"field expiry releases the final lingering slow");
            Require(battle.CombatEvents.Where(f=>f.Kind==BattleCombatEventKind.DamageResolved&&f.Source.StableId=="prototype_u06_a").All(f=>f.IsAreaDamage),"every storm damage pulse retains area provenance");
        }
        using(var battle=StormBattle(content))
        {
            var hero=HeroStorm(battle);hero.CurrentMana=hero.MaxMana;Steps(battle,9);var field=battle.GroundStorms.Single();
            while(battle.TickIndex<field.ExpiresTick)battle.Step();
            var pulses=battle.CombatEvents.Where(f=>f.Kind==BattleCombatEventKind.DamageResolved&&f.Source.StableId=="prototype_u06_a").ToArray();
            Require(pulses.Length==10&&pulses.Select(f=>f.Tick).SequenceEqual(Enumerable.Range(1,10).Select(i=>field.StartedTick+i*5)),"a stationary enemy takes exactly ten scheduled pulses");
        }
    }
    private static BattleUnitState HeroStorm(BattleSimulation battle)=>battle.Units.Single(u=>u.RuntimeId=="storm-hero");
    private static BattleSimulation StormBattle(ContentRegistry content,CompiledAbilityDefinition? active=null,Action<BattleCombatBindingRegistry>? bindings=null,bool extraCaster=false,int extraEnemies=0)
    {
        var source=Profile(content,6) with {Behavior=new(Stationary:true,DisableBasicAttacks:true)};
        if(active is not null)source=source with {AbilityLoadout=new([active])};
        var ally=Profile(content,16) with {AbilityLoadout=null,Behavior=new(Stationary:true,DisableBasicAttacks:true)};
        var enemy=Profile(content,4) with {AbilityLoadout=null,Behavior=new(Stationary:true,DisableBasicAttacks:true)};
        var config=new BattleConfig {Seed=20261007,HeroRule=HeroRuleSnapshot.Neutral,FloorRule=new ClearFloorRuleRuntime("storm-test","风暴边界测试",""),
            ConfigureCombatBindings=bindings,Spawns=[new(source,0,new(3,2),"storm-hero",IsPersistentRosterHero:true),new(ally,0,new(1,4),"storm-ally",IsPersistentRosterHero:true),new(enemy,1,new(5,2),"storm-enemy")]};
        if(extraCaster)config.Spawns.Add(new(source with {AllowTemporaryMana=true},0,new(2,2),"storm-copy",IsTemporary:true,IsPersistentRosterHero:false));
        for(var i=0;i<extraEnemies;i++)config.Spawns.Add(new(enemy,1,new(6+i%3,i/3*2),"storm-extra-"+i));
        var battle=new BattleSimulation(config);
        foreach(var unit in battle.Units)
        {unit.CurrentMana=0;unit.AttackCooldown=10000;unit.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond,0);unit.Attributes.SetBaseValue(CombatAttribute.ManaPerAttack,0);unit.Attributes.SetBaseValue(CombatAttribute.ManaPerDamageRatio,0);if(unit.Team==1){unit.MaxHealth=20000;unit.Health=20000;}}
        HeroStorm(battle).Position=new(3,2);Enemy(battle).Position=new(5,2);return battle;
    }
    private static void StormDenseAndOverlap(ContentRegistry content)
    {
        using(var battle=StormBattle(content,extraCaster:true))
        {
            foreach(var caster in battle.Units.Where(u=>u.Team==0&&u.Definition.ContentId=="prototype_u06"))caster.CurrentMana=caster.MaxMana;
            Steps(battle,9);Require(battle.GroundStorms.Length==2,"clean temporary caster forms its independent second field");
            Near(Enemy(battle).Attributes.GetValue(CombatAttribute.AttackSpeed),.75f,"overlapping same-type slows never multiply or add");
            HeroStorm(battle).Health=0;Steps(battle,5);Require(battle.GroundStorms.Length==2,"death does not cancel either formed field");
            Near(Enemy(battle).Attributes.GetValue(CombatAttribute.AttackSpeed),.75f,"provider death cannot leave a stacked or orphan slow");
        }
        using(var battle=StormBattle(content,extraEnemies:3))
        {
            var enemies=battle.Units.Where(u=>u.Team==1).ToArray();enemies[0].Position=new(5,2);enemies[1].Position=new(5,3);enemies[2].Position=new(6,2);enemies[3].Position=new(2.3f,.5f);
            HeroStorm(battle).CurrentMana=HeroStorm(battle).MaxMana;Steps(battle,9);
            Require(battle.GroundStorms.Single().Center==new Vector2(5,2),"densest center outranks isolated near target and ties by stable identity");
        }
    }
    private static void StormInterruptAndRollback(ContentRegistry content)
    {
        using(var battle=StormBattle(content))
        {var hero=HeroStorm(battle);hero.CurrentMana=hero.MaxMana;battle.Step();Invoke(battle,Enemy(battle),new() {Kind=MatrixOperationKind.Control,Target=MatrixTargetKind.NearestEnemies,Range=20,MaxTargets=1,DurationTicks=20});Steps(battle,10);Require(battle.GroundStorms.IsEmpty,"hard control during windup creates no field");}
        var skill=Loadout("u06").Find("prototype_u06_a")!;var atom=((CompiledMatrixOperation)skill.Operations.Single()).Effects.Single();var reject=true;
        using(var battle=StormBattle(content,skill with {Operations=[atom]},registry=>registry.Subscribe(BattleCombatEventKind.AbilityResolved,
            CombatSourceRef.System("storm-fault"),0,(fact,sink)=>{if(reject&&fact.SubjectStableId==skill.StableId)sink.Enqueue(CombatSourceRef.System("storm-fault"),0,_=>throw new InvalidOperationException("intentional storm rollback"));})))
        {
            var hero=HeroStorm(battle);hero.CurrentMana=hero.MaxMana;battle.Step();Require(battle.GroundStorms.IsEmpty&&hero.CurrentMana==hero.MaxMana,"failed invocation rolls back field, serial and payment");
            reject=false;battle.Step();Require(battle.GroundStorms.Single().StableId=="ground-storm:1","retry creates one field with rolled-back serial");
        }
        using var first=StormBattle(content);using var second=StormBattle(content);
        HeroStorm(first).CurrentMana=HeroStorm(first).MaxMana;HeroStorm(second).CurrentMana=HeroStorm(second).MaxMana;Steps(first,65);Steps(second,65);
        Require(first.CreateResult().Digest==second.CreateResult().Digest,"storm timeline is deterministic");
    }
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
