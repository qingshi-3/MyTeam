using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Relics;

public partial class V1RelicCombatContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var definitions=Enumerable.Range(6,5).ToDictionary(n=>n,n=>
            {
                var authored=GD.Load<RelicDefinition>($"res://content/relics/definitions/v1_relic_r{n:00}.tres");
                var result=RelicDefinitionCompiler.Compile(authored);
                Require(result.Definition is not null,string.Join(';',result.Report.CoreErrors));
                var changed=(RelicDefinition)authored.Duplicate(true);changed.CombatRules[0].Amount*=.9f;
                Require(RelicDefinitionCompiler.Compile(changed).Definition!.Fingerprint!=result.Definition!.Fingerprint,"combat rule changes fingerprint");
                changed.CombatRules[0].Amount=float.NaN;
                Require(RelicDefinitionCompiler.Compile(changed).Definition is null,"nonfinite combat rule rejected");
                return result.Definition!;
            });
            DamageBoundaries(definitions);CriticalHealing(definitions[9]);TimedHealing(definitions[10]);
            var content=await SkillSampleContent.LoadAsync(this);
            RealTimedDeath(content,definitions[10]);RealCritical(content,definitions[9]);
            GD.Print("V1_RELIC_COMBAT_OK r06-strict-prehit r07-live-body-distance-periodic r08-boss-only r09-original-derived-crit-heal r10-tick100-current-living late-summons scoped-rollback real-death-order");
            GetTree().Quit();
        }
        catch(Exception error){GD.PrintErr("V1_RELIC_COMBAT_FAILED "+error);GetTree().Quit(1);}
    }
    private static void DamageBoundaries(Dictionary<int,CompiledRelicDefinition> definitions)
    {
        using(var f=new Fixture(definitions[6]))
        {
            Near(f.Damage(),125,"full-health target receives bonus");
            f.Set("enemy",unit=>unit with{Health=90});Near(f.Damage(),100,"exactly 90 percent excluded");
            f.Set("enemy",unit=>unit with{Health=90.01f});Near(f.Damage(),125,"strictly above 90 included");
            f.Set("enemy",unit=>unit with{Health=89});Near(f.Damage(),100,"later hit rechecks health instead of caching cast bonus");
            Near(f.Damage("enemy","hero"),100,"enemy attacks receive no player relic bonus");
        }
        using(var f=new Fixture(definitions[7]))
        {
            // .5 radius at each end: center distance2.5 gives exactly1.5 edge distance.
            f.Set("enemy",unit=>unit with{Position=new(2.5f,0)});Near(f.Damage(),115,"body distance boundary included");
            f.Set("enemy",unit=>unit with{Position=new(2.501f,0)});Near(f.Damage(),100,"outside body distance excluded");
            f.Set("enemy",unit=>unit with{Position=new(2,0)});Near(f.Damage(kind:CombatDamageClass.Periodic),115,"periodic tick uses present source position");
            f.Set("hero",unit=>unit with{Position=new(-4,0)});Near(f.Damage(kind:CombatDamageClass.Periodic),100,"later periodic tick follows changed distance");
            f.Units.RemoveAll(unit=>unit.RuntimeId=="hero");Near(f.Damage(kind:CombatDamageClass.Periodic),100,"missing source provides no range condition");
        }
        using(var f=new Fixture(definitions[8]))
        {
            Near(f.Damage(),100,"ordinary guard excluded");f.Set("enemy",unit=>unit with{IsBoss=true});
            Near(f.Damage(),118,"formal boss included");f.Add("guard",1,100,false,false,new(2,0));
            Near(f.Damage(target:"guard"),100,"boss encounter guard is still ordinary");
        }
    }
    private static void CriticalHealing(CompiledRelicDefinition definition)
    {
        using var f=new Fixture(definition);
        Near(f.Unit("hero").Attributes.GetValue(CombatAttribute.CriticalChance),.05f,"existing team member receives +5 percentage points");
        f.Set("hero",unit=>unit with{Health=50});f.Crit(CombatDamageClass.BasicAttack,"attack:a");Near(f.Unit("hero").Health,58,"original critical heals eight");
        f.Crit(CombatDamageClass.BasicAttack,"derived:attack:a");Near(f.Unit("hero").Health,66,"qualified additional critical also heals");
        f.Crit(CombatDamageClass.ActiveSkill,"skill:a");Near(f.Unit("hero").Health,66,"skill critical excluded");
        f.Crit(CombatDamageClass.BasicAttack,"attack:shared");f.Crit(CombatDamageClass.BasicAttack,"attack:shared",target:"other");
        Near(f.Unit("hero").Health,82,"separate hits sharing action each earn one heal");
        f.Add("late",0,30,true,false,new(1,1));f.Publish(new(BattleCombatEventKind.UnitSummoned,CombatSourceRef.System("test"),"hero","late",f.Tick));
        Near(f.Unit("late").Attributes.GetValue(CombatAttribute.CriticalChance),.05f,"later temporary ally receives normal team attribute projection");
        f.Crit(CombatDamageClass.BasicAttack,"late:a",source:"late");Near(f.Unit("late").Health,38,"later living temporary ally can receive its own critical heal");
        f.Set("hero",unit=>unit with{Alive=false,Health=0});f.Crit(CombatDamageClass.BasicAttack,"dead:a");Near(f.Unit("hero").Health,0,"critical heal cannot revive dead source");
        f.Set("hero",unit=>unit with{Alive=true,Health=40});var scope=f.Scope.CaptureState();var pipeline=f.Combat.CaptureState();
        f.FailHeal=true;
        try {f.Crit(CombatDamageClass.BasicAttack,"retry:a");throw new InvalidOperationException("fault missing");}
        catch(InvalidOperationException error) when(error.Message=="expected heal fault") { }
        f.Scope.RestoreState(scope);f.Combat.RestoreState(pipeline);f.FailHeal=false;
        f.Crit(CombatDamageClass.BasicAttack,"retry:a");Near(f.Unit("hero").Health,48,"rollback restores critical receipt and permits same-sequence retry once");
    }
    private static void TimedHealing(CompiledRelicDefinition definition)
    {
        using var f=new Fixture(definition);f.Set("hero",unit=>unit with{Health=40});
        f.Add("temporary",0,20,true,false,new(1,1));f.Add("dead",0,0,false,false,new(1,2));f.Set("dead",unit=>unit with{Alive=false});
        f.Tick=99;f.Scope.EndOfTick();Near(f.Unit("hero").Health,40,"no early heal");
        var checkpoint=f.Scope.CaptureState();f.Tick=100;f.FailHeal=true;
        try {f.Scope.EndOfTick();throw new InvalidOperationException("fault missing");}
        catch(InvalidOperationException error) when(error.Message=="expected heal fault") { }
        f.Scope.RestoreState(checkpoint);f.FailHeal=false;f.Scope.EndOfTick();
        Near(f.Unit("hero").Health,61,"tick100 heals 35 percent missing health");Near(f.Unit("temporary").Health,48,"existing temporary ally participates");
        Near(f.Unit("dead").Health,0,"already dead ally excluded");Near(f.Unit("enemy").Health,100,"enemy excluded");
        f.Scope.EndOfTick();Near(f.Unit("hero").Health,61,"same tick is one-shot");
        f.Add("after",0,10,true,false,new(1,3));f.Tick=101;f.Scope.EndOfTick();Near(f.Unit("after").Health,10,"post-trigger birth never catches up");
        Require(!f.Scope.HasEndOfTickRules,"completed timer no longer requests per-tick work");
    }
    private sealed class Fixture : IDisposable
    {
        private readonly BattleAttributeScope _attributes=new("v1-relic-fixture");
        private readonly RelicRunScope _run=new(new(20261007,"hero",0,0));
        public BattleCombatEventPipeline Combat {get;}=new("v1-relic-combat");
        public RelicBattleScope Scope {get;}
        public List<RelicBattleUnitBinding> Units {get;}=[];
        public int Tick; public bool FailHeal;
        public Fixture(CompiledRelicDefinition definition)
        {
            _run.Activate(definition,new(){InstanceId="relic",ContentId=definition.StableId,Counters=RelicRunScope.InitialRunCounters(definition).ToList()});
            Add("hero",0,100,false,false,new(0,0));Add("enemy",1,100,false,false,new(2,0));
            Scope=new(_run.PrepareBattle());
            Scope.Activate(new(){CombatBindings=new(Combat),QueryUnits=()=>Units.ToImmutableArray(),ExecuteEffect=(_,_,_,_,_,_)=>{},Summon=(_,_,_,_,_)=>false,CurrentTick=()=>Tick,
                HealUnit=(id,amount,source)=>{if(FailHeal)throw new InvalidOperationException("expected heal fault");Require(source.Kind==CombatSourceKind.Relic,"healing retains relic provenance");Set(id,unit=>unit with{Health=Math.Min(100,unit.Health+amount)});}});
        }
        public void Add(string id,int team,float health,bool temporary,bool boss,CombatPoint position)
        {
            var attributes=_attributes.CreateSet(id,AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute,float>{[CombatAttribute.MaxHealth]=100,[CombatAttribute.CriticalChance]=0}));
            Units.Add(new(id,team,!temporary,temporary,!temporary,true,new((int)position.X,(int)position.Y),attributes,position,.5f,health,boss));
        }
        public RelicBattleUnitBinding Unit(string id)=>Units.Single(unit=>unit.RuntimeId==id);
        public void Set(string id,Func<RelicBattleUnitBinding,RelicBattleUnitBinding> change){var at=Units.FindIndex(unit=>unit.RuntimeId==id);Units[at]=change(Units[at]);}
        public float Damage(string source="hero",string target="enemy",CombatDamageClass kind=CombatDamageClass.BasicAttack)=>Combat.Resolve(new(BattleCombatCalculationKind.Damage,
            CombatSourceRef.Unit("fixture",source,source),source,target,Tick,100,DamageClass:kind)).ResolvedAmount;
        public void Publish(BattleCombatEventDraft draft){var result=Combat.Publish(draft);Require(result.Accepted,"fixture fact accepted: "+result.Message);}
        public void Crit(CombatDamageClass kind,string action,string source="hero",string target="enemy")=>Publish(new(BattleCombatEventKind.CriticalHit,
            CombatSourceRef.Unit("fixture",source,source),source,target,Tick,EffectiveValue:10,DamageClass:kind,ActionId:action));
        public void Dispose(){Scope.Dispose();Combat.Dispose();_run.Dispose();_attributes.Dispose();}
    }
    private static RelicBattlePreparation Preparation(CompiledRelicDefinition definition)
    {
        using var run=new RelicRunScope(new(20261007,"hero",0,0));
        run.Activate(definition,new(){InstanceId="real-relic",ContentId=definition.StableId,Counters=RelicRunScope.InitialRunCounters(definition).ToList()});
        return run.PrepareBattle();
    }
    private static BattleConfig Config(TowerAutobattler.Content.ContentRegistry content,CompiledRelicDefinition definition,IBattleFloorRuleRuntime floor)
    {
        var profile=AbilityPrototypeContent.Snapshot(content,2) with{AbilityLoadout=null,Behavior=new(Stationary:true)};
        var config=new BattleConfig{Seed=20261007,HeroRule=HeroRuleSnapshot.Neutral,FloorRule=floor,Relics=Preparation(definition)};
        config.Spawns.Add(new(profile,0,new(1,1),"survivor",IsPersistentRosterHero:true));
        config.Spawns.Add(new(profile,0,new(1,4),"doomed",IsPersistentRosterHero:true));
        config.Spawns.Add(new(profile,1,new(8,2),"enemy"));return config;
    }
    private static void RealTimedDeath(TowerAutobattler.Content.ContentRegistry content,CompiledRelicDefinition definition)
    {
        using var battle=new BattleSimulation(Config(content,definition,new DeathAt100()));
        foreach(var unit in battle.Units){unit.AttackCooldown=10000;unit.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond,0);}
        var survivor=battle.Units.Single(unit=>unit.SourceInstanceId=="survivor");survivor.Health=survivor.MaxHealth*.4f;
        var doomed=battle.Units.Single(unit=>unit.SourceInstanceId=="doomed");
        for(var tick=0;tick<100;tick++)battle.Step();
        Require(!doomed.Alive,"formal floor damage kills ally during tick100");Near(survivor.Health,survivor.MaxHealth*.61f,"end-tick healing runs after same-tick lethal damage");
        Require(!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.HealingResolved&&f.TargetRuntimeId==doomed.RuntimeId),"dead ally receives no heal event");
    }
    private sealed class DeathAt100():ClearFloorRuleRuntime("relic-death-order","遗物时序测试","")
    {
        public override void OnTick(BattleRuleContext context)
        {if(context.Tick==100){var dead=context.Units.Single(unit=>unit.SourceInstanceId=="doomed");context.Damage("enemy",dead,dead.MaxHealth*100);}}
    }
    private static void RealCritical(TowerAutobattler.Content.ContentRegistry content,CompiledRelicDefinition definition)
    {
        using var battle=new BattleSimulation(Config(content,definition,new ClearFloorRuleRuntime("relic-crit","暴击遗物测试","")));
        var hero=battle.Units.Single(unit=>unit.SourceInstanceId=="survivor");var enemy=battle.Units.Single(unit=>unit.Team==1);
        foreach(var unit in battle.Units){unit.AttackCooldown=10000;unit.CurrentMana=0;unit.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond,0);}
        enemy.Position=hero.Position+new Vector2(hero.BodyRadius+enemy.BodyRadius+.2f,0);
        hero.Attributes.SetBaseValue(CombatAttribute.CriticalChance,1);hero.Health=hero.MaxHealth/2;var before=hero.Health;hero.AttackCooldown=0;
        for(var tick=0;tick<8&&!battle.CombatEvents.Any(f=>f.Kind==BattleCombatEventKind.CriticalHit);tick++)battle.Step();
        Require(battle.CombatEvents.Count(f=>f.Kind==BattleCombatEventKind.CriticalHit&&f.SourceRuntimeId==hero.RuntimeId)==1,"one real ordinary critical occurred");
        Near(hero.Health,before+8,"real critical metadata reaches relic healing once");
    }
    private static void Near(float actual,float expected,string message)=>Require(Math.Abs(actual-expected)<.02f,$"{message}: {actual} != {expected}");
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
