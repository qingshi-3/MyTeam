using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Statuses;

internal static class AbilityPrototypeBatch1Checks
{
    internal static void Run(ContentRegistry content)
    {
        Ranger(content);
        RangerInterruptAndRollback(content);
        LightningAssassin(content);
        ChainAscension(content);
        ArrowAscension(content);
        PunchAscension(content);
        BoostableQueries(content);
        AbilityPrototypeBatch2Checks.Run(content);
        AbilityPrototypeBatch3Checks.Run(content);
        GD.Print("B4_BATCH1_ABILITY_OK authored u14-windup-near-far-three-original-attacks control-transform-rollback u20-legal-blink-fallback-line-disarm-no-crit-skill-allowed assassin-entry u02-three-secondary u19-first-two-cap-seven");
    }
    internal static CompiledAbilityLoadout Loadout(string suffix)
    {
        var result=AbilityDefinitionCompiler.CompileLoadout(GD.Load<AbilityLoadoutDefinition>("res://content/abilities/loadouts/prototype_"+suffix+".tres"));
        Require(result.Loadout is not null&&!result.Report.HasCoreErrors,"authored loadout compiles: "+suffix);
        return result.Loadout!;
    }
    internal static UnitSnapshot Profile(ContentRegistry content,int n) =>
        BattleSetupFactory.Snapshot(GD.Load<UnitDefinition>($"res://content/definitions/heroes/prototype_u{n:00}.tres"),
            abilityLoadout:Loadout($"u{n:00}"),graph:content.Graph);
    internal static BattleSimulation Battle(ContentRegistry content,int n,CompiledAbilityLoadout? loadout=null,int enemies=1,
        Action<BattleCombatBindingRegistry>? bindings=null,UnitSnapshot? enemyProfile=null,IBattleFloorRuleRuntime? floor=null)
    {
        var hero=Profile(content,n) with {AbilityLoadout=loadout??Loadout($"u{n:00}"),Behavior=new(Stationary:true)};
        var target=enemyProfile??(AbilityPrototypeContent.Snapshot(content,4) with {AbilityLoadout=null,Behavior=new(Stationary:true)});
        var config=new BattleConfig {Seed=20261007,HeroRule=HeroRuleSnapshot.Neutral,
            FloorRule=floor??new ClearFloorRuleRuntime("batch1-test","首批能力规则",""),ConfigureCombatBindings=bindings};
        config.Spawns.Add(new(hero,0,new(3,2),"hero",IsPersistentRosterHero:true));
        for(var i=0;i<enemies;i++)config.Spawns.Add(new(target,1,new(6+i%3,i/3*2),"enemy-"+i));
        var battle=new BattleSimulation(config);
        foreach(var unit in battle.Units)
        {
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond,0);
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerAttack,0);
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerDamageRatio,0);
            unit.Attributes.SetBaseValue(CombatAttribute.CriticalChance,0);
            unit.CurrentMana=0;unit.AttackCooldown=10000;
            if(unit.Team==1){unit.Attributes.SetBaseValue(CombatAttribute.MaxHealth,20000);unit.Health=unit.MaxHealth;}
        }
        battle.Units.Single(u=>u.Team==0).Position=new(3,2);
        return battle;
    }
    internal static BattleUnitState Hero(BattleSimulation battle)=>battle.Units.Single(u=>u.Team==0);
    internal static BattleUnitState Enemy(BattleSimulation battle)=>battle.Units.First(u=>u.Team==1);
    internal static void Steps(BattleSimulation battle,int count){for(var i=0;i<count;i++)battle.Step();}
    private static bool HasTag(BattleUnitState unit,string tag)=>unit.Statuses.Any(status=>status.GrantedTags.Contains(tag));
    private static void Near(float actual,float expected,string message)=>Require(Math.Abs(actual-expected)<.02f,$"{message}: actual={actual} expected={expected}");
    private static void Ranger(ContentRegistry content)
    {
        var profile=Profile(content,14);
        Require(profile.MaxHealth==850&&profile.Damage==62&&profile.Armor==6&&profile.Range==.35f&&profile.AttackTicks==14,"U14 base stats");
        foreach(var nearby in new[]{false,true})
        {
            using var battle=Battle(content,14);var hero=Hero(battle);var enemy=Enemy(battle);
            enemy.Position=nearby?new(3.95f,2):new(7,2);
            hero.CurrentMana=hero.MaxMana;Steps(battle,3);
            Require(hero.AttackDelivery==AttackDelivery.Melee,"U14 payoff waits for three-tick windup");
            Steps(battle,1);
            Require(hero.AttackDelivery==AttackDelivery.Projectile&&hero.MaxMana==0,"U14 transforms with or without a nearby enemy");
            Near(hero.Damage,82,"U14 permanent combat form attack gain");Near(hero.AttackRange,4,"U14 combat form reach");
            Near(hero.Attributes.GetValue(CombatAttribute.AttackSpeed),2,"U14 original attack speed boost");
            var hits=battle.CombatEvents.Where(f=>f.Kind==BattleCombatEventKind.SkillHitLanded&&f.SourceRuntimeId==hero.RuntimeId).ToArray();
            Require(hits.Length==(nearby?1:0),"optional melee strike hits only in melee reach");
            if(nearby)Require(battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.ControlApplied&&f.TargetRuntimeId==enemy.RuntimeId),"U14 near strike applies stun");
            Steps(battle,4);enemy.Position=new(5,2);
            for(var i=0;i<3;i++)
            {
                hero.AttackCooldown=0;var before=battle.CombatEvents.Count(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==hero.RuntimeId);
                for(var tick=0;tick<10&&battle.CombatEvents.Count(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==hero.RuntimeId)==before;tick++)battle.Step();
                Require(battle.CombatEvents.Count(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==hero.RuntimeId)==before+1,"one formal original declaration per forced action");
                Near(hero.Attributes.GetValue(CombatAttribute.AttackSpeed),i<2?2:1,"U14 speed ends after exactly three original declarations");
                Require(hero.AttackCooldown<=7,"third declared action retains the boosted cadence selected before lease removal");
                hero.AttackCooldown=10000;
                if(i==0)
                {
                    var beforeDerived=battle.CombatEvents.Count(f=>f.Kind==BattleCombatEventKind.AttackDeclared);
                    Invoke(battle,hero,new() {Kind=MatrixOperationKind.AdditionalAttack,Target=MatrixTargetKind.NearestEnemies,Range=20,MaxTargets=1,Amount=1});
                    Near(hero.Attributes.GetValue(CombatAttribute.AttackSpeed),2,"qualified extra attack does not consume U14 original count");
                    Require(battle.CombatEvents.Count(f=>f.Kind==BattleCombatEventKind.AttackDeclared)==beforeDerived,"additional hit does not declare an original action");
                }
            }
        }
    }
    private static void RangerInterruptAndRollback(ContentRegistry content)
    {
        using(var battle=Battle(content,14))
        {
            var hero=Hero(battle);var enemy=Enemy(battle);enemy.Position=new(4,2);hero.CurrentMana=hero.MaxMana;battle.Step();
            var control=new MatrixAbilityOperationSpec {Kind=MatrixOperationKind.Control,Target=MatrixTargetKind.NearestEnemies,Range=20,DurationTicks=10};
            var ability=AbilityDefinitionCompiler.Compile(new AbilityDefinition {StableId="batch1_control",DisplayName="测试打断",ActivationKind=AbilityActivationKind.Triggered,
                Trigger=AbilityTriggerKind.OwnerEnteredBattle,Operations=[control]}).Ability!;
            var runtime=(IAbilityRuntimeWorld)battle;var prepared=runtime.Prepare(ability,enemy.RuntimeId,enemy.RuntimeId,hero.RuntimeId,battle.TickIndex);
            Require(prepared.Succeeded&&runtime.Commit(prepared.Plan!).Succeeded,"formal control fixture commits");Steps(battle,4);
            Require(hero.AttackDelivery==AttackDelivery.Melee&&!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.UnitTransformed),"hard control cancels pending U14 form payoff");
        }
        var rejected=true;
        var published=Loadout("u14").Find("prototype_u14_a")!;
        var form=((CompiledMatrixOperation)published.Operations.Single()).Effects.Single(op=>op.Kind==MatrixOperationKind.Transform);
        using var rollback=Battle(content,14,new([published with {Operations=[form]}]),bindings:registry=>registry.Subscribe(BattleCombatEventKind.UnitTransformed,
            CombatSourceRef.System("batch1-transform-fault"),0,(fact,sink)=>{if(rejected)sink.Enqueue(CombatSourceRef.System("batch1-transform-fault"),0,_=>throw new InvalidOperationException("intentional U14 payoff rollback"));}));
        var ranger=Hero(rollback);ranger.CurrentMana=ranger.MaxMana;rollback.Step();
        Near(ranger.Damage,62,"form fault restores AD");Near(ranger.Attributes.GetValue(CombatAttribute.AttackSpeed),1,"form fault restores counted lease");
        Require(ranger.AttackDelivery==AttackDelivery.Melee&&ranger.CurrentMana==ranger.MaxMana,"form fault restores delivery and payment");
        rejected=false;rollback.Step();Near(ranger.Attributes.GetValue(CombatAttribute.AttackSpeed),2,"retry creates exactly one lease");
    }
    private static void LightningAssassin(ContentRegistry content)
    {
        var full=Loadout("u20");var active=full.Find("prototype_u20_a")!;
        Require(full.Find("prototype_assassin_entry") is not null,"U20 retains independent assassin entry");
        var profile=Profile(content,20);Require(profile.MaxHealth==760&&profile.Damage==62&&profile.Armor==4&&profile.AttackTicks==12,"U20 base stats");
        using(var battle=Battle(content,20,new([active]),enemyProfile:Profile(content,19) with {Behavior=new(Stationary:true)}))
        {
            var hero=Hero(battle);var enemy=Enemy(battle);enemy.Position=new(4.6f,2);hero.CurrentMana=hero.MaxMana;
            hero.Attributes.SetBaseValue(CombatAttribute.CriticalChance,1);
            var start=hero.Position;Steps(battle,2);Require(hero.Position==start,"U20 waits for its interruptible two-tick windup");Steps(battle,1);
            Require(hero.Position.DistanceTo(start)>0&&hero.Position.DistanceTo(start)<=2.0001f&&hero.Position.DistanceTo(enemy.Position)>=hero.BodyRadius+enemy.BodyRadius,"legal bounded nonoverlapping blink");
            Require(HasTag(enemy,StatusDefinitionCompiler.AttackDisabledTag)&&!HasTag(enemy,StatusDefinitionCompiler.ActionDisabledTag),"disarm is not hard control");
            var hits=battle.CombatEvents.Where(f=>f.Kind==BattleCombatEventKind.DamageResolved&&f.SourceRuntimeId==hero.RuntimeId).ToArray();
            Require(hits.Length==1&&hits[0].DamageClass==CombatDamageClass.ActiveSkill,"slash is a direct active skill");Near(hits[0].RequestedValue,62*1.8f,"slash exact raw attack ratio");
            Require(!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.CriticalHit||f.Kind==BattleCombatEventKind.AttackLanded),"slash grants no crit or basic landed facts");
            enemy.AttackCooldown=0;var before=battle.CombatEvents.Count;Steps(battle,2);
            Require(!battle.CombatEvents.Skip(before).Any(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==enemy.RuntimeId),"disarm blocks a new original attack");
            enemy.CurrentMana=enemy.MaxMana;Steps(battle,1);
            Require(enemy.ChargedLine is not null,"disarmed enemy can begin its real mana skill");
            Steps(battle,3);Require(battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.SkillHitLanded&&f.SourceRuntimeId==enemy.RuntimeId),"disarm leaves the existing skill windup and shot intact");
            enemy.AttackCooldown=10000;Steps(battle,7);Require(!HasTag(enemy,StatusDefinitionCompiler.AttackDisabledTag),"disarm expires at authored duration");
        }
        using(var battle=Battle(content,20,new([active])))
        {
            var hero=Hero(battle);Enemy(battle).Position=new(8,2);hero.CurrentMana=hero.MaxMana;var start=hero.Position;Steps(battle,3);
            Require(hero.Position==start&&battle.PendingEvents.Any(e=>e.Type=="line_slash_start"),"no bounded landing still slashes from current position");
            Require(!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.DamageResolved),"out-of-line target does not receive invented damage");
        }
        using(var battle=Battle(content,20))
        {Steps(battle,6);Require(battle.PendingEvents.Any(e=>e.Displacement is {StartTick:6}),"authored full U20 independently performs assassin entry");}
    }
    private static void ChainAscension(ContentRegistry content)
    {
        foreach(var ascended in new[]{false,true})
        {
            using var battle=Battle(content,2,ascended?Loadout("u02_ascended"):null,4);var hero=Hero(battle);
            var enemies=battle.Units.Where(u=>u.Team==1).ToArray();enemies[0].Position=new(3.95f,2);enemies[1].Position=new(4,3);enemies[2].Position=new(5,2);enemies[3].Position=new(5,3);
            hero.CurrentMana=hero.MaxMana;battle.Step();hero.AttackCooldown=0;
            // The authored cast owns five recovery ticks. Await its first legal
            // original declaration instead of sampling the next suppressed tick.
            for(var tick=0;tick<12&&!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.AttackDeclared&&f.SourceRuntimeId==hero.RuntimeId);tick++)battle.Step();
            var derived=battle.CombatEvents.Where(f=>f.Kind==BattleCombatEventKind.AttackLanded&&f.ActionId.StartsWith("derived:attack:",StringComparison.Ordinal)).ToArray();
            Require(derived.Length==(ascended?3:2)&&derived.Select(f=>f.TargetRuntimeId).Distinct().Count()==derived.Length,$"U02 ascension adds exactly one distinct secondary target: ascended={ascended} tick={battle.TickIndex} manaLock={hero.ManaLockedUntilTick} derived={derived.Length} facts={string.Join(",",battle.CombatEvents.Where(f=>f.Kind is BattleCombatEventKind.AttackDeclared or BattleCombatEventKind.AttackLanded).Select(f=>$"{f.Kind}:{f.ActionId}:{f.TargetRuntimeId}"))}");
            var primary=battle.CombatEvents.Single(f=>f.Kind==BattleCombatEventKind.AttackLanded&&!f.ActionId.StartsWith("derived:",StringComparison.Ordinal));
            Require(derived.All(f=>f.TargetRuntimeId!=primary.TargetRuntimeId&&!f.ActionId.Contains("derived:derived:",StringComparison.Ordinal)),"chain excludes original target and recursive followups");
            Require(derived.All(f=>Math.Abs(f.RequestedValue-hero.Damage*.6f)<.02f),"chain retains the 60% raw attack ratio");
        }
    }
    private static void ArrowAscension(ContentRegistry content)
    {
        foreach(var ascended in new[]{false,true})
        {
            using var battle=Battle(content,19,ascended?Loadout("u19_ascended"):null);var hero=Hero(battle);Enemy(battle).Position=new(5,2);
            for(var cast=0;cast<8;cast++)
            {
                hero.CurrentMana=hero.MaxMana;hero.ManaLockedUntilTick=0;Steps(battle,20);
                Require(battle.CombatEvents.Count(f=>f.Kind==BattleCombatEventKind.SkillHitLanded&&f.SourceRuntimeId==hero.RuntimeId)==cast+1,"one successful real arrow hit per cast");
                var layers=Math.Min(cast+1+(ascended?1:0),ascended?7:5);
                Near(hero.Attributes.GetValue(CombatAttribute.AttackSpeed),1+layers*.12f,"arrow ascension first hit grants two and caps at seven");
            }
        }
    }
    private static void PunchAscension(ContentRegistry content)
    {
        foreach(var ascended in new[]{false,true})
        foreach(var interrupted in new[]{false,true})
        {
            using var battle=Battle(content,7,ascended?Loadout("u07_ascended"):null,floor:new GritProbe());
            var hero=Hero(battle);var enemy=Enemy(battle);enemy.Position=new(5,2);
            hero.MaxHealth=1000;hero.Health=1000;hero.Attributes.SetBaseValue(CombatAttribute.Armor,0);
            Steps(battle,2);
            Require(battle.PendingEvents.Count(f=>f.Type=="line_charge")==1&&hero.Shield==500,"actual capped 500 Grit starts and is consumed by one punch");
            Near(hero.Health,800,"setup health before successful-release reward");
            if(interrupted)Invoke(battle,enemy,new() {Kind=MatrixOperationKind.Control,Target=MatrixTargetKind.NearestEnemies,Range=20,DurationTicks=10});
            Steps(battle,7);
            var healing=battle.CombatEvents.Where(f=>f.Kind==BattleCombatEventKind.HealingResolved&&f.Source.StableId=="prototype_u07_ascended_a").ToArray();
            Near(hero.Health,ascended&&!interrupted?900:800,"punch heals 20 percent of consumed Grit only after successful release");
            Require(healing.Length==(ascended&&!interrupted?1:0),"base and interrupted punches do not produce the ascension heal fact");
            if(healing.Length==1)Near(healing[0].EffectiveValue,100,"release healing uses the consumed amount, not current Grit");
            Require(battle.PendingEvents.Any(f=>f.Type==(interrupted?"line_cancel":"line_release")),"real release or control cancellation reaches its formal branch");
            Require(battle.DescribeBattleResources(hero.RuntimeId).Contains("怒劲 0",StringComparison.Ordinal),"success and cancellation leave consumed Grit spent");
        }
        GD.Print("B4_GRIT_ASCENSION_OK actual-consumed-grit successful-release-only base-unchanged control-no-heal no-grit-refund");
    }
    private sealed class GritProbe() : ClearFloorRuleRuntime("batch1-grit", "怒劲规则测试", "")
    {
        public override void OnTick(BattleRuleContext context)
        {
            var hero=context.Units.Single(u=>u.Team==0);
            if(context.Tick==1){context.Damage("enemy-0",hero,400);context.Heal(hero,300);}
            if(context.Tick==2)context.Damage("enemy-0",hero,100);
        }
    }
    private static void BoostableQueries(ContentRegistry content)
    {
        foreach(var number in new[]{14,20})
        {
            using var battle=Battle(content,number);
            var report=new ValidationReport();
            var query=MatrixAbilityCompiler.Compile(new MatrixAbilityOperationSpec {Kind=MatrixOperationKind.Damage,
                Target=MatrixTargetKind.NearestBoostableCaster,Range=20,Amount=1},"boostable-batch1",report)!;
            Require(!report.HasCoreErrors,"boostable query fixture compiles");
            var selected=new BattleMatrixQueries(new ReadWorld(battle)).Select(query,BattleUnitView.For(Hero(battle)),"",null).ToArray();
            Require(selected.Length==1&&selected[0].RuntimeId==Hero(battle).RuntimeId,"new damage atoms remain eligible for the existing next-skill boost query");
        }
    }
    private sealed class ReadWorld(BattleSimulation battle) : IBattleMatrixReadWorld
    {
        public System.Collections.Generic.IReadOnlyList<BattleUnitView> Units=>battle.UnitViews;
        public BattleUnitView? FindUnit(string id)=>Units.FirstOrDefault(u=>u.RuntimeId==id);
        public float Counter(string ownerId,string key)=>0;
        public int Poison(string id)=>0;
        public int Chill(string id)=>0;
        public int Ember(string id)=>0;
        public bool ActionDisabled(string id)=>false;
    }
    internal static void Invoke(BattleSimulation battle,BattleUnitState owner,MatrixAbilityOperationSpec operation)
    {
        var ability=AbilityDefinitionCompiler.Compile(new AbilityDefinition {StableId="batch1_qualified_attack",DisplayName="额外命中测试",
            ActivationKind=AbilityActivationKind.Triggered,Trigger=AbilityTriggerKind.OwnerEnteredBattle,Operations=[operation]}).Ability!;
        var world=(IAbilityRuntimeWorld)battle;var result=world.Prepare(ability,owner.RuntimeId,owner.RuntimeId,"",battle.TickIndex);
        Require(result.Succeeded&&world.Commit(result.Plan!).Succeeded,"qualified additional attack fixture commits through the formal world");
    }
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
