using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class EnemyFiveContractSmoke : Node
{
    private ContentRegistry _content = null!;
    private Dictionary<string,UnitSnapshot> _products = [];
    public override async void _Ready()
    {
        try
        {
            var gate=await GamePackagePublisher.CreateReadyAsync(this,GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package=gate.Package??throw new InvalidOperationException(string.Join(';',gate.Report.CoreErrors));
            _content=package.Content;
            foreach(var id in new[]{"enemy_rampart_segment","enemy_brood_egg","enemy_brood_larva"}) _products.Add(id,Profile(id));
            Cone();Blade();Swap();Rampart();Brood();Rollback();
            var store=new BattleLabPresetStore(GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres"));
            var adapter=new BattleLabPreparationAdapter(new BattleLabContentIndex(package));
            foreach(var prefix in new[]{"EE07","EE08","EE09","EB01","EB02"})
            {
                var preset=store.BuiltIns.Single(p=>p.Key.StartsWith(prefix+" ·",StringComparison.Ordinal));
                var config=adapter.Build(BattleLabPresetStore.ToSnapshot(preset.Value));
                using var one=new BattleSimulation(config);using var two=new BattleSimulation(config);
                Step(one,110);Step(two,110);
                Require(one.CreateResult().Digest==two.CreateResult().Digest,prefix+" deterministic production preset");
                Require(one.PendingEvents.Any(e=>e.Type is "enemy_action" or "blade_release" or "shell_break"),prefix+" actual authored action");
            }
            var generator=new TowerGenerator(package.Project.Campaign);
            var leads=new HashSet<string>();
            var prepare=new RunBattlePreparationService(package.Content,package.Project,new RunRelicService(package.Content));
            foreach(var floor in new[]{0,4,10,14})
            for(ulong seed=1;seed<=8;seed++)
            {
                var run=new ActiveRunDto{Seed=seed,FloorIndex=floor,EquippedTacticalCommandIds=package.Project.RunRules.StarterTacticalCommandIds.ToList(),Roster=[new(){InstanceId="hero",ContentId="hero_hc03_iron_guard"}]};run.Deployment[0]="hero";
                var plan=generator.Encounter(run,floor%5==4?TowerNodeType.Boss:TowerNodeType.Elite);
                leads.Add(plan.EnemyIds[0]);
                using var battle=new BattleSimulation(prepare.Build(run,plan,false));CheckBodies(battle);
            }
            foreach(var key in new[]{"ee07_furnace_lizard","ee08_return_blade","ee09_swap_envoy","eb01_rampart_warden","eb02_shell_matriarch"})
                Require(leads.Contains("enemy_"+key),"formal reachability "+key);
            GD.Print("ENEMY_FIVE_CONTRACT_OK cone interruption; blade legs; atomic swap; attackable walls; phase retention; eggs/caps; rollback; presets and formal placement");
            GetTree().Quit();
        }
        catch(Exception e){GD.PrintErr("ENEMY_FIVE_CONTRACT_FAILED "+e);GetTree().Quit(1);}
    }
    private void Cone()
    {
        using var b=new BattleSimulation(Config("ee07_furnace_lizard",new(6,2),[(new(4,2),"front"),(new(7,0),"back")]));
        Step(b,42);
        Require(Hits(b,"front")==3 && Hits(b,"back")==0,"three cone pulses, not radial splash");
        using var interrupted=new BattleSimulation(Config("ee07_furnace_lizard",new(6,2),[(new(4,2),"front")]));
        Step(interrupted,20);var hits=Hits(interrupted,"front");Unit(interrupted,"caster").DisabledTicks=5;Step(interrupted,20);
        Require(hits==1 && Hits(interrupted,"front")==1,"control removes remaining breath pulses");
    }
    private void Blade()
    {
        using var b=new BattleSimulation(Config("ee08_return_blade",new(8,2),[(new(6,2),"near"),(new(3,2),"far")]));
        Step(b,40);
        Require(Hits(b,"near")==2 && Hits(b,"far")==2,"each target once outbound and once inbound");
        var release=b.PendingEvents.Single(e=>e.Type=="blade_release");
        var finish=b.PendingEvents.Single(e=>e.Type=="blade_end");
        Require(release.Tick==12 && finish.Position==new Vector2(8,2),"natural hand release, fixed return origin");
        using var dead=new BattleSimulation(Config("ee08_return_blade",new(8,2),[(new(6,2),"near"),(new(3,2),"far")],extraEnemy:true));
        Step(dead,13);Unit(dead,"caster").Health=0;Step(dead,30);
        Require(Hits(dead,"far")==2 && dead.PendingEvents.Any(e=>e.Type=="blade_end" && e.Position==new Vector2(8,2)),"released blade survives caster death");
        using var moved=new BattleSimulation(Config("ee08_return_blade",new(8,2),[(new(6,2),"near")]));
        Step(moved,13);Unit(moved,"caster").Position=new(8,3);Step(moved,28);
        Require(moved.PendingEvents.Single(e=>e.Type=="blade_end").Position==new Vector2(8,2),"moving caster does not steer return");
    }
    private void Swap()
    {
        using var b=new BattleSimulation(Config("ee09_swap_envoy",new(8,2),[(new(5,2),"near"),(new(1,2),"far")]));
        Step(b,10);Unit(b,"far").Position=new(1,3);Step(b,11);
        Require(Unit(b,"caster").Position==new Vector2(1,3) && Unit(b,"far").Position==new Vector2(8,2) && Hits(b,"far")==0,"farthest target current positions swap atomically without damage");
        using var invalid=new BattleSimulation(Config("ee09_swap_envoy",new(8,2),[(new(1,2),"far")]));
        Step(invalid,10);Unit(invalid,"far").Position=new(-.25f,2);Step(invalid,11,check:false);
        Require(Unit(invalid,"caster").Position==new Vector2(8,2) && Unit(invalid,"far").Position==new Vector2(-.25f,2),"invalid full-body endpoint cancels both");
        Unit(invalid,"far").Position=new(1,2);Step(invalid,20);
        Require(!invalid.PendingEvents.Any(e=>e.Type=="swap_complete"),"failed swap still spends cooldown");
    }
    private void Rampart()
    {
        using var b=new BattleSimulation(Config("eb01_rampart_warden",new(8,2),[(new(3,2),"front")]));
        Step(b,60);
        var walls=b.Units.Where(u=>u.Definition.ContentId=="enemy_rampart_segment" && u.Alive).ToArray();
        Require(walls.Length==3 && walls.All(u=>u.Definition.Behavior.DisableBasicAttacks),"short targetable wall bodies");
        Step(b,19);
        Require(Hits(b,"front")==1 && walls.All(u=>!u.Alive),"fissure crosses own wall then wall expires");
        using var controlled=new BattleSimulation(Config("eb01_rampart_warden",new(8,2),[(new(3,2),"front")]));
        Step(controlled,62);Unit(controlled,"caster").DisabledTicks=5;Step(controlled,25);
        Require(controlled.Units.All(u=>u.Definition.ContentId!="enemy_rampart_segment" || !u.Alive) && Hits(controlled,"front")==0,"control cancels followup and removes wall without respam");
        var cfg=Config("eb01_rampart_warden",new(8,2),[(new(3,2),"front")]);
        cfg.Spawns[1]=cfg.Spawns[1] with {Unit=cfg.Spawns[1].Unit with{Behavior=new(),Damage=100,AttackTicks=4}};
        using var attacking=new BattleSimulation(cfg);Step(attacking,78);
        Require(attacking.PendingEvents.Any(e=>e.Type=="attack" &&
            attacking.Units.Any(u=>u.RuntimeId==e.TargetRuntimeId && u.Definition.ContentId=="enemy_rampart_segment")),"ordinary AI attacks a blocking wall");
    }
    private void Brood()
    {
        using var b=new BattleSimulation(Config("eb02_shell_matriarch",new(6,2),[(new(2,2),"front")]));
        var mother=Unit(b,"caster");mother.Health=499;mother.Shield=31;
        Step(b,1);mother.Health-=37;var health=mother.Health;Step(b,12);
        Require(mother.BodyRadius==.7f && mother.Health==health && mother.Shield==31 && mother.AttackDelivery==AttackDelivery.Projectile,"phase keeps same health/shield and changes real body/delivery");
        Step(b,6);Require(mother.Position.X>6 && mother.Position.X<=8.001,"one legal retreat");
        var eggs=b.Units.Where(u=>u.Definition.ContentId=="enemy_brood_egg" && u.Alive).ToArray();
        Require(eggs.Length==2,"first two visible eggs");
        eggs[0].Health=0;Step(b,31);
        Require(b.Units.Count(u=>u.Alive && u.Definition.ContentId=="enemy_brood_larva")==1,"destroyed egg cannot hatch");
        foreach(var u in b.Units.Where(u=>u.SummonerRuntimeId==mother.RuntimeId))u.Health=0;
        Step(b,80);Require(b.Units.Count(u=>u.Definition.ContentId=="enemy_brood_egg")==4,"second and final brood");
        mother.Health=0;Step(b,1);
        Require(b.Units.All(u=>u.Definition.ContentId!="enemy_brood_egg" || !u.Alive),"mother death cancels immature eggs");
        using var cap=new BattleSimulation(Config("eb02_shell_matriarch",new(6,2),[(new(1,2),"front")]));
        Unit(cap,"caster").Health=499;Step(cap,240);
        Require(cap.Units.Count(u=>u.Definition.ContentId=="enemy_brood_egg")==2 &&
            cap.Units.Count(u=>u.Alive && u.SummonerRuntimeId=="caster")<=2,"living larvae reserve both capacity slots");
    }
    private void Rollback()
    {
        using var b=new BattleSimulation(Config("ee08_return_blade",new(8,2),[(new(4,2),"front")],fail:true));
        Step(b,15);
        Require(!b.PendingEvents.Any(e=>e.Type is "blade_throw" or "blade_release"),"failed commit removes action, cue and paid world state");
    }
    private BattleConfig Config(string key,Vector2I at,(Vector2I,string)[] targets,bool fail=false,bool extraEnemy=false)
    {
        var profile=Profile("enemy_"+key) with{Behavior=new(Stationary:true,DisableBasicAttacks:true),MaxHealth=1000};
        var spawns=new List<BattleSpawn>{new(profile,1,at,"caster")};
        foreach(var (cell,id) in targets)spawns.Add(new(Target(),0,cell,id));
        if(extraEnemy)spawns.Add(new(Target(),1,new(9,0),"survivor"));
        return new BattleConfig{Seed=971,FloorRule=new ClearFloorRuleRuntime("rule_clear","常规",""),TacticalSummons=_products,
            HeroRule=new(1,1,1,0,0,0,false,"",1,1,0,0,0,0,false,false,0,0,""),Spawns=spawns,
            ConfigureCombatBindings=fail?bindings=>bindings.Subscribe(BattleCombatEventKind.AbilityResolved,CombatSourceRef.System("enemy_rollback"),0,
                (_,sink)=>sink.Enqueue(CombatSourceRef.System("enemy_rollback"),0,_=>throw new InvalidOperationException("probe"))):null};
    }
    private UnitSnapshot Profile(string id){Require(_content.TryGet(id,out var entry),"published "+id);return BattleSetupFactory.Snapshot(entry,_content);}
    private static UnitSnapshot Target(){using var d=new UnitDefinition{Id="test_body",DisplayName="测试靶",IsHero=true,MaxHealth=10000,AttackDamage=0};return BattleSetupFactory.Snapshot(d) with{Behavior=new(Stationary:true,DisableBasicAttacks:true)};}
    private static BattleUnitState Unit(BattleSimulation b,string id)=>b.Units.Single(u=>u.RuntimeId==id);
    private static int Hits(BattleSimulation b,string id)=>b.CombatEvents.Count(e=>e.Kind==BattleCombatEventKind.SkillHitLanded && e.TargetRuntimeId==id);
    private static void Step(BattleSimulation b,int ticks,bool check=true){for(int i=0;i<ticks && b.Outcome==BattleOutcome.Running;i++){b.Step();if(check)CheckBodies(b);}}
    private static void CheckBodies(BattleSimulation b)
    {
        var alive=b.Units.Where(u=>u.Alive).ToArray();
        foreach(var u in alive)
        {
            Require(BattlefieldSpace.IsCircleInsideArena(u.Position,u.BodyRadius,BattleSimulation.Width,BattleSimulation.Height),"inside arena "+u.RuntimeId);
            foreach(var v in alive.Where(v=>string.CompareOrdinal(v.RuntimeId,u.RuntimeId)>0))
                Require(u.Position.DistanceTo(v.Position)+.003f>=u.BodyRadius+v.BodyRadius,"nonoverlap "+u.RuntimeId+" / "+v.RuntimeId);
        }
    }
    private static void Require(bool b,string message){if(!b)throw new InvalidOperationException(message);}
}
