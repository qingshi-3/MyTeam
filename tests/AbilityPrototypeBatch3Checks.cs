using System;
using System.Linq;
using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;
using static AbilityPrototypeBatch1Checks;

internal static class AbilityPrototypeBatch3Checks
{
    internal static void Run(ContentRegistry content)
    {
        Channel(content);
        HealingCompletion(content);
        KillRefund(content);
        DualShot(content);
        OriginalAttackCycle(content);
        GD.Print("B4_BATCH3_ABILITY_OK seeded-fifteen-channel-lock-complete-refund-control-death healing-five-ticks-effective-completion-shield-death-rollback pounce-direct-native-kill-after-recovery dual-slide-reach-real-second-hit-single-bounce-no-basic-mana E32-fourth-original-fixed-windup-multiplier-cancel-spends-cycle");
    }
    private static void Near(float actual,float expected,string message)=>Require(Math.Abs(actual-expected)<.03f,$"{message}: actual={actual} expected={expected}");
    private static CompiledAbilityLoadout ActiveOnly(string suffix)=>new(Loadout(suffix).Abilities.Where(a=>a.IsActiveSkill).ToImmutableArray());
    private static BattleCombatEvent[] Hits(BattleSimulation battle,string source)=>battle.CombatEvents.Where(f=>f.Kind==BattleCombatEventKind.DamageResolved&&f.SourceRuntimeId==source).ToArray();
    private static void Channel(ContentRegistry content)
    {
        foreach(var ascended in new[]{false,true})
        {
            using var battle=Battle(content,21,ActiveOnly(ascended?"u21_ascended":"u21"));var hero=Hero(battle);var enemy=Enemy(battle);enemy.Position=new(5,2);
            Require(hero.MaxHealth==950&&hero.Damage==45&&hero.Definition.BodyRadius==.4f&&hero.MaxMana==160,"U21 published stats");
            hero.CurrentMana=hero.MaxMana;Steps(battle,9);var anchor=hero.Position;
            Require(Hits(battle,hero.RuntimeId).Length==0,"channel begins only after eight-tick windup and waits its first interval");
            hero.AttackCooldown=0;Steps(battle,28);
            Require(Hits(battle,hero.RuntimeId).Length==14&&hero.CurrentMana==0&&hero.Position==anchor,
                $"fourteen seeded bolts retain action and mana lock: tick={battle.TickIndex} hits={Hits(battle,hero.RuntimeId).Length} mana={hero.CurrentMana} pos={hero.Position} anchor={anchor} disabled={hero.DisabledTicks} lock={hero.ManaLockedUntilTick} fields={string.Join(',',battle.PendingEvents.Where(e=>e.Type.StartsWith("storm_channel",StringComparison.Ordinal)).Select(e=>$"{e.Tick}:{e.Type}"))}");
            Require(!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==hero.RuntimeId),"channel has no original basic action");
            Steps(battle,2);var hits=Hits(battle,hero.RuntimeId);
            Require(hits.Length==15&&hits.Select(f=>f.Tick).SequenceEqual(Enumerable.Range(1,15).Select(i=>9+i*2)),"exact fifteen two-tick bolts complete at tick39");
            Require(hits.All(f=>!f.IsAreaDamage&&f.DamageClass==CombatDamageClass.ActiveSkill&&f.TargetRuntimeId==enemy.RuntimeId),"one legal boss may receive every single-target bolt");
            foreach(var hit in hits)Near(hit.RequestedValue,70,"seventy percent spell power");
            Near(hero.CurrentMana,ascended?40:0,"only completed ascended channel refunds forty after unlock");
        }
        foreach(var dies in new[]{false,true})
        {
            using var battle=CoreBattle(content,21,"u21_ascended");var hero=battle.Units.Single(u=>u.RuntimeId=="core");
            hero.CurrentMana=hero.MaxMana;Steps(battle,17);Require(Hits(battle,hero.RuntimeId).Length==4,"four actual bolts precede interruption");
            if(dies)hero.Health=0;
            else Invoke(battle,battle.Units.First(u=>u.Team==1),new(){Kind=MatrixOperationKind.Control,Target=MatrixTargetKind.NearestEnemies,Range=20,DurationTicks=10});
            Steps(battle,25);Require(Hits(battle,hero.RuntimeId).Length==4&&hero.CurrentMana==0,"death or hard control loses remaining bolts and completion refund");
        }
        foreach(var controlTick in new[]{1,9})
        {
            using var battle=Battle(content,21,ActiveOnly("u21_ascended"));var hero=Hero(battle);Enemy(battle).Position=new(5,2);
            hero.CurrentMana=hero.MaxMana;Steps(battle,controlTick);
            Require(hero.DisabledTicks==0,"channel windup is an owned action lease, not the shared real-control counter");
            Invoke(battle,Enemy(battle),new(){Kind=MatrixOperationKind.Control,Target=MatrixTargetKind.NearestEnemies,Range=20,DurationTicks=6});
            Require(hero.Statuses.Any(s=>s.GrantedTags.Contains(StatusDefinitionCompiler.ActionDisabledTag)),"same-tick hard control remains a real active status");
            Steps(battle,40);Require(Hits(battle,hero.RuntimeId).Length==0&&hero.CurrentMana==0,
                $"same-tick control at windup/formation cancels all bolts and refund: controlTick={controlTick} tick={battle.TickIndex} hits={Hits(battle,hero.RuntimeId).Length} mana={hero.CurrentMana}");
        }
        using var first=Battle(content,21,enemies:3);using var second=Battle(content,21,enemies:3);
        Hero(first).CurrentMana=Hero(first).MaxMana;Hero(second).CurrentMana=Hero(second).MaxMana;Steps(first,40);Steps(second,40);
        Require(Hits(first,Hero(first).RuntimeId).Select(f=>f.TargetRuntimeId).SequenceEqual(Hits(second,Hero(second).RuntimeId).Select(f=>f.TargetRuntimeId))&&first.CreateResult().Digest==second.CreateResult().Digest,"same battle seed freezes identical random legal-target sequence");
    }
    private static BattleSimulation CoreBattle(ContentRegistry content,int number,string suffix,bool temporaryVictim=false,Action<BattleCombatBindingRegistry>? bindings=null)
    {
        var hero=Profile(content,number) with {AbilityLoadout=ActiveOnly(suffix),Behavior=new(Stationary:true)};
        var ally=Profile(content,16) with {AbilityLoadout=null,Behavior=new(Stationary:true,DisableBasicAttacks:true)};
        var enemy=Profile(content,4) with {AbilityLoadout=null,Behavior=new(Stationary:true,DisableBasicAttacks:true)};
        var config=new BattleConfig {Seed=20261007,HeroRule=HeroRuleSnapshot.Neutral,ConfigureCombatBindings=bindings,
            FloorRule=new ClearFloorRuleRuntime("batch3","完成收益测试",""),Spawns=[new(hero,0,new(3,2),"core",IsPersistentRosterHero:true),new(ally,0,new(1,4),"ally",IsPersistentRosterHero:true),new(enemy,1,new(5,2),"victim",IsTemporary:temporaryVictim),new(enemy,1,new(8,4),"reserve")]};
        var battle=new BattleSimulation(config);
        foreach(var unit in battle.Units)
        {unit.CurrentMana=0;unit.AttackCooldown=10000;unit.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond,0);unit.Attributes.SetBaseValue(CombatAttribute.ManaPerAttack,0);unit.Attributes.SetBaseValue(CombatAttribute.ManaPerDamageRatio,0);unit.Attributes.SetBaseValue(CombatAttribute.CriticalChance,0);unit.Attributes.SetBaseValue(CombatAttribute.Armor,0);if(unit.Team==1){unit.MaxHealth=20000;unit.Health=20000;}}
        battle.Units.Single(u=>u.RuntimeId=="core").Position=new(3,2);battle.Units.Single(u=>u.RuntimeId=="ally").Position=new(1,3);battle.Units.Single(u=>u.RuntimeId=="victim").Position=new(5,2);
        return battle;
    }
    private static void HealingCompletion(ContentRegistry content)
    {
        foreach(var ownerDies in new[]{false,true})
        {
            using var battle=CoreBattle(content,11,"u11_ascended");var hero=battle.Units.Single(u=>u.RuntimeId=="core");var ally=battle.Units.Single(u=>u.RuntimeId=="ally");
            hero.MaxHealth=1000;hero.Health=500;ally.MaxHealth=1000;ally.Health=850;hero.CurrentMana=hero.MaxMana;
            Steps(battle,21);Near(hero.Health,620,"U11 healing keeps release, +10 and +20 ticks");
            if(ownerDies)hero.Health=0;
            else Invoke(battle,battle.Units.First(u=>u.Team==1),new(){Kind=MatrixOperationKind.Control,Target=MatrixTargetKind.NearestEnemies,Range=20,DurationTicks=5});
            while(battle.TickIndex<50)battle.Step();Require(hero.Shield==0&&ally.Shield==0,"completion shield is absent before the full five seconds");battle.Step();
            Near(hero.Shield,ownerDies?0:40,"actual self healing creates forty shield only on full completion");Near(ally.Shield,ownerDies?0:30,"ally over-healing contributes zero to conversion");
            if(!ownerDies){Near(hero.Health,700,"five original forty heals remain unchanged");Near(ally.Health,1000,"recipient reaches maximum health");Steps(battle,40);Near(hero.Shield,0,"completion shield expires after four seconds");}
        }
        using(var battle=CoreBattle(content,11,"u11_ascended"))
        {var hero=battle.Units.Single(u=>u.RuntimeId=="core");var ally=battle.Units.Single(u=>u.RuntimeId=="ally");hero.MaxHealth=1000;hero.Health=500;ally.MaxHealth=1000;ally.Health=500;hero.CurrentMana=hero.MaxMana;Steps(battle,21);ally.Health=0;Steps(battle,30);Near(hero.Shield,40,"surviving recipient retains its own successful shield");Near(ally.Shield,0,"dead recipient receives no completion shield");}
        var reject=true;
        using(var battle=CoreBattle(content,11,"u11_ascended",bindings:registry=>registry.Subscribe(BattleCombatEventKind.AbilityResolved,CombatSourceRef.System("healing-fault"),0,(fact,sink)=>{if(reject&&fact.SubjectStableId=="prototype_u11_ascended_a")sink.Enqueue(CombatSourceRef.System("healing-fault"),0,_=>throw new InvalidOperationException("intentional healing rollback"));})))
        {var hero=battle.Units.Single(u=>u.RuntimeId=="core");hero.MaxHealth=1000;hero.Health=500;hero.CurrentMana=hero.MaxMana;battle.Step();Near(hero.Health,500,"failed invocation restores immediate healing");Require(hero.CurrentMana==hero.MaxMana,"failed series restores mana");reject=false;battle.Step();Steps(battle,50);Near(hero.Shield,40,"retry owns one successful healing series and conversion");}
    }
    private static void KillRefund(ContentRegistry content)
    {
        foreach(var temporary in new[]{false,true})
        foreach(var lethal in new[]{false,true})
        {
            using var battle=CoreBattle(content,17,"u17_ascended",temporary);var hero=battle.Units.Single(u=>u.RuntimeId=="core");var victim=battle.Units.Single(u=>u.RuntimeId=="victim");
            if(lethal)victim.Health=10;hero.CurrentMana=hero.MaxMana;Steps(battle,5);
            Require(hero.CurrentMana==0,"direct pounce does not refund during recovery");battle.Step();Near(hero.CurrentMana,!temporary&&lethal?30:0,"only direct native lethal pounce refunds after recovery");
            Require(battle.CombatEvents.Count(f=>f.Kind==BattleCombatEventKind.AttackLanded&&f.SourceRuntimeId==hero.RuntimeId)==1,"pounce has exactly one qualified direct skill attack");
            Steps(battle,8);Near(hero.CurrentMana,!temporary&&lethal?30:0,"one cast cannot refund twice");
        }
        using(var battle=CoreBattle(content,17,"u17_ascended"))
        {var hero=battle.Units.Single(u=>u.RuntimeId=="core");hero.CurrentMana=hero.MaxMana;battle.Step();Invoke(battle,battle.Units.First(u=>u.Team==1),new(){Kind=MatrixOperationKind.Control,Target=MatrixTargetKind.NearestEnemies,Range=20,DurationTicks=20});Steps(battle,10);Require(hero.CurrentMana==0&&!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.AttackLanded&&f.SourceRuntimeId==hero.RuntimeId),"cancelled pounce never earns a kill refund");}
    }
    private static void DualShot(ContentRegistry content)
    {
        foreach(var ascended in new[]{false,true})
        {
            using var battle=Battle(content,22,ActiveOnly(ascended?"u22_ascended":"u22"),enemies:2);var hero=Hero(battle);var victims=battle.Units.Where(u=>u.Team==1).ToArray();victims[0].Position=new(5,2);victims[1].Position=new(6,3);
            Require(hero.MaxHealth==680&&hero.Damage==62&&hero.MaxMana==45&&hero.AttackRange==3,"U22 published stats");
            var start=hero.Position;hero.CurrentMana=hero.MaxMana;Steps(battle,2);Require(!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.AttackLanded&&f.SourceRuntimeId==hero.RuntimeId),"two-tick windup cannot hit early");
            Steps(battle,8);var landed=battle.CombatEvents.Where(f=>f.Kind==BattleCombatEventKind.AttackLanded&&f.SourceRuntimeId==hero.RuntimeId).ToArray();
            Require(landed.Length==1&&landed[0].ActionId.StartsWith("derived:",StringComparison.Ordinal),"first skill attack carries qualified hit effects without an original declaration");
            Require(hero.Position.DistanceTo(start)<=1.0001f&&hero.Position.X<start.X&&BattlefieldSpace.IsWithinReach(hero,victims[0],3),"shared legal retreat remains bounded and keeps its locked target reachable");
            Require(hero.CurrentMana==0&&!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==hero.RuntimeId),"neither dual shot grants original attack mana or declaration");
            Steps(battle,8);var skillHits=battle.CombatEvents.Where(f=>f.Kind==BattleCombatEventKind.SkillHitLanded&&f.SourceRuntimeId==hero.RuntimeId).ToArray();
            Require(skillHits.Length==(ascended?2:1)&&skillHits.All(f=>!f.IsAreaDamage),"actual second bullet collides once and optional bounce is another single-target bullet");
            Near(skillHits[0].RequestedValue,90,"second bullet uses ninety percent spell power");
            if(ascended){Near(skillHits[1].RequestedValue,60,"one bounce uses sixty percent spell power");Require(skillHits[1].TargetRuntimeId!=skillHits[0].TargetRuntimeId,"bounce cannot return to original enemy");}
        }
        using(var battle=Battle(content,22,ActiveOnly("u22")))
        {var hero=Hero(battle);hero.Position=new(-.18f,2);Enemy(battle).Position=new(2,2);hero.CurrentMana=hero.MaxMana;Steps(battle,5);Require(hero.Position==new Vector2(-.18f,2)&&battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.AttackLanded&&f.SourceRuntimeId==hero.RuntimeId),"blocked retreat uses a real in-place shot");}
        using(var battle=Battle(content,22,ActiveOnly("u22")))
        {
            var hero=Hero(battle);var enemy=Enemy(battle);enemy.Position=new(5,2);hero.CurrentMana=hero.MaxMana;Steps(battle,3);
            Require(battle.PendingEvents.Any(e=>e.Type=="displacement"&&e.TargetRuntimeId==hero.RuntimeId&&e.Displacement is {Finished:false}),"dual slide has actually entered the shared displacement scope");
            var template=AbilityDefinitionCompiler.Compile(GD.Load<AbilityDefinition>("res://content/abilities/automatic/ability_ee03_trample.tres")).Ability!;
            var charge=((CompiledTrampleOperation)template.Operations.Single()) with {ChargeTicks=1,Speed=20,AttackMultiplier=.1f};
            var ability=template with {ActivationKind=AbilityActivationKind.Triggered,Trigger=AbilityTriggerKind.ActionQueued,Operations=[charge]};
            var world=(IAbilityRuntimeWorld)battle;var plan=world.Prepare(ability,enemy.RuntimeId,enemy.RuntimeId,hero.RuntimeId,battle.TickIndex);
            Require(plan.Succeeded&&world.Commit(plan.Plan!).Succeeded,"real trample is prepared against the moving dual shooter");Steps(battle,8);
            Require(battle.PendingEvents.Any(e=>e.Type=="displacement"&&e.TargetRuntimeId==hero.RuntimeId&&e.Displacement is {Cancelled:true}),"trample actually cancels the unfinished retreat");
            Require(!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.AttackLanded&&f.SourceRuntimeId==hero.RuntimeId)&&
                !battle.PendingEvents.Any(e=>e.Type=="projectile_spawn"&&e.SourceRuntimeId==hero.RuntimeId),"cancelled displacement completion does not fire either dual shot");
        }
    }
    private static void OriginalAttackCycle(ContentRegistry content)
    {
        var compiled=AbilityDefinitionCompiler.Compile(new AbilityDefinition{StableId="cycle_entry_test",DisplayName="周期登记",ActivationKind=AbilityActivationKind.Triggered,Trigger=AbilityTriggerKind.OwnerEnteredBattle,MaxUses=1,Operations=[new EnemyOriginalAttackCycleSpec()]});
        Require(compiled.Ability is not null&&!compiled.Report.HasCoreErrors,"original cycle entry compiles: "+string.Join("; ",compiled.Report.CoreErrors));
        foreach(var cancelFourth in new[]{false,true})
        {
            using var battle=Battle(content,19,new([compiled.Ability!]));var hero=Hero(battle);Enemy(battle).Position=new(5,2);
            var disarmAbility=compiled.Ability! with {StableId="cycle-disarm-test",Operations=[new CompiledApplyStatusAbilityOperation(
                BattleMechanicStatus.Create("cycle-disarm","缴械",3,harmful:true) with {GrantedTags=[StatusDefinitionCompiler.AttackDisabledTag]},new CompiledExplicitTargetQuery())]};
            var world=(IAbilityRuntimeWorld)battle;var enemy=Enemy(battle);var prepared=world.Prepare(disarmAbility,enemy.RuntimeId,enemy.RuntimeId,hero.RuntimeId,battle.TickIndex);
            Require(prepared.Succeeded&&world.Commit(prepared.Plan!).Succeeded,"formal disarm applies before admission");
            hero.AttackCooldown=0;battle.Step();Require(hero.ProjectileWindups.IsEmpty&&!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==hero.RuntimeId),"disarmed actor admits no original attack");hero.AttackCooldown=10000;Steps(battle,3);
            for(var i=1;i<=4;i++)
            {
                Invoke(battle,hero,new(){Kind=MatrixOperationKind.AdditionalAttack,Target=MatrixTargetKind.NearestEnemies,Range=20,Amount=1});
                hero.AttackCooldown=0;var admitted=battle.TickIndex+1;battle.Step();
                if(i==4)
                {
                    Require(hero.ProjectileWindups.Single().ReleaseTick==admitted+4&&hero.ProjectileWindups.Single().DamageMultiplier==1.8f,"fourth admitted original attack overrides windup with four ticks and multiplier1.8");
                    if(cancelFourth)
                    {Invoke(battle,Enemy(battle),new(){Kind=MatrixOperationKind.Control,Target=MatrixTargetKind.NearestEnemies,Range=20,DurationTicks=2});Steps(battle,3);hero.AttackCooldown=0;battle.Step();Require(hero.ProjectileWindups.All(p=>p.DamageMultiplier==1),"cancelled fourth attack spends its cycle; fifth is ordinary");break;}
                }
                for(var wait=0;wait<10&&battle.CombatEvents.Count(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==hero.RuntimeId)<i;wait++)battle.Step();
                Require(battle.CombatEvents.Count(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==hero.RuntimeId)==i,"each original release appears once");hero.AttackCooldown=10000;
            }
            if(!cancelFourth){Steps(battle,8);var damage=Hits(battle,hero.RuntimeId).Where(f=>f.ActionId.StartsWith("attack:",StringComparison.Ordinal)).ToArray();Require(damage.Length==4,"four original projectiles hit independently of qualified additional impacts");Near(damage[3].RequestedValue,hero.Damage*1.8f,"fourth actual projectile preserves multiplier through collision");}
        }
    }
    private static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException(message);}
}
