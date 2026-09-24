using System;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;

public partial class DuelGritHookContractSmoke : Node
{
    public static readonly string[] Heroes = ["hero_hc37_legion_duelist", "hero_hc38_grit_brawler", "hero_hc39_hook_machine"];
    public override async void _Ready()
    {
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this, GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var snapshots = Heroes.Select(id =>
            {
                Require(package.Content.TryGet(id, out var entry), "published hero " + id);
                return BattleSetupFactory.Snapshot(entry, package.Content);
            }).ToArray();
            CheckDuel(snapshots[0]); CheckGrit(snapshots[1]); CheckHook(snapshots[2]);
            var presets = new BattleLabPresetStore(GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres"));
            var adapter = new BattleLabPreparationAdapter(new BattleLabContentIndex(package));
            foreach (var prefix in new[] { "HC37", "HC38", "HC39" })
            {
                var preset = presets.BuiltIns.Single(p => p.Key.StartsWith(prefix, StringComparison.Ordinal));
                var config = adapter.Build(BattleLabPresetStore.ToSnapshot(preset.Value));
                using var battle = new BattleSimulation(config); Step(battle, 160);
                var kind = prefix == "HC37" ? "duel_begin" : prefix == "HC38" ? "line_release" : "hook_caught";
                Require(battle.PendingEvents.Any(e => e.Type == kind), "shipped preset demonstrates " + kind);
            }
            using var bad = new AbilityDefinition { StableId="invalid_hook", DisplayName="无效", ActivationKind=AbilityActivationKind.Automatic,
                Trigger=AbilityTriggerKind.ManaFull, ManaCost=10, Operations=[new HookAbilityOperationSpec{Speed=float.NaN}] };
            Require(AbilityDefinitionCompiler.Compile(bad).Report.HasCoreErrors,"non-finite technique authoring rejected");
            GD.Print("DUEL_GRIT_HOOK_CONTRACT_OK publication duel counter grit shield-expiry hook blocker boss miss control deterministic rollback presets compiler");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("DUEL_GRIT_HOOK_CONTRACT_FAILED " + error); GetTree().Quit(1); }
    }
    private static void CheckDuel(UnitSnapshot hero)
    {
        var quiet = hero with { Behavior=new(Stationary:true, DisableBasicAttacks:true) };
        using (var battle = new BattleSimulation(Config(quiet)))
        {
            var a=Unit(battle,"hero"); var b=Unit(battle,"front");
            b.Position=a.Position+new Vector2(.85f,0); a.CurrentMana=a.MaxMana;
            Step(battle,2);
            Require(battle.PendingEvents.Any(e=>e.Type=="duel_begin"),"duel begins in contact range");
            Require(a.ActionTargetRuntimeId==b.RuntimeId && b.ActionTargetRuntimeId==a.RuntimeId,"mutual target lock");
            var active=hero.AbilityLoadout!.Abilities.Single(s=>s.Trigger==AbilityTriggerKind.ManaFull);
            Require(!((IAbilityRuntimeWorld)battle).Prepare(active,a.RuntimeId,a.RuntimeId,b.RuntimeId,battle.TickIndex).Succeeded,"duel disallows a second active");
            b.Position=new(8,4);battle.Step();
            Require(battle.PendingEvents.Any(e=>e.Type=="duel_end"),"distance invalidates pair");
        }
        foreach(var end in new[]{"time","team","death"})
        using(var battle=new BattleSimulation(Config(quiet)))
        {
            var a=Unit(battle,"hero");var b=Unit(battle,"front");b.Position=a.Position+new Vector2(.85f,0);a.CurrentMana=a.MaxMana;
            Step(battle,2);a.CurrentMana=0;
            if(end=="team")b.Team=a.Team;
            if(end=="death")b.Health=0;
            Step(battle,end=="time"?42:1);
            Require(battle.PendingEvents.Any(e=>e.Type=="duel_end"),"duel cleanup "+end);
        }
        foreach(var shield in new[]{0f,1000f})
        {
            var config=Config(quiet);config.Spawns[1]=config.Spawns[1] with {Unit=Dummy() with {Damage=10,AttackTicks=2,Range=1,Behavior=new(Stationary:true)}};
            using var battle=new BattleSimulation(config);var a=Unit(battle,"hero");var b=Unit(battle,"front");
            a.Health=300;a.CurrentMana=0;a.ManaLockedUntilTick=999;b.Position=a.Position+new Vector2(.85f,0);b.Shield=shield;
            Step(battle,7);
            var attacks=battle.CombatEvents.Count(e=>e.Kind==BattleCombatEventKind.AttackLanded && e.TargetRuntimeId==a.RuntimeId);
            var counters=battle.PendingEvents.Count(e=>e.Type=="counter_strike");
            Require(counters==attacks/3 && counters>0,"counter every three actual basic hits, no recursion");
            var heals=battle.CombatEvents.Where(e=>e.Kind==BattleCombatEventKind.HealingResolved && e.Source.StableId=="ability_hc37_p").Sum(e=>e.EffectiveValue);
            Require(shield>0 ? heals==0 : heals>0,"counter lifesteal uses health loss only");
        }
        GD.Print("  duel and counter passed");
    }
    private static void CheckGrit(UnitSnapshot hero) => GritActionQueueContractSmoke.Run(hero);
    private static void CheckHook(UnitSnapshot hero)
    {
        hero=hero with {Behavior=new(Stationary:true,DisableBasicAttacks:true)};
        using(var battle=new BattleSimulation(Config(hero)))
        {
            Step(battle,22);
            var contact=battle.PendingEvents.Single(e=>e.Type=="hook_caught");
            Require(contact.TargetRuntimeId=="front" && contact.Tick>battle.PendingEvents.Single(e=>e.Type=="hook_start").Tick+3,"physical flight catches front blocker");
            Require(Unit(battle,"front").Position.X<3 && Unit(battle,"far").Health==10000,"pull only caught unit");
            CheckBodies(battle);
        }
        var boss=Config(hero);boss.Spawns[1]=boss.Spawns[1] with {Unit=Dummy() with {IsBoss=true}};
        using(var battle=new BattleSimulation(boss))
        { Step(battle,22);Require(Unit(battle,"front").Position.X==4 && Unit(battle,"front").Health<10000 && Unit(battle,"far").Health==10000,"boss blocks and takes damage without pull"); }
        using(var battle=new BattleSimulation(Config(hero)))
        {
            Step(battle,2);Unit(battle,"front").Position=new(4,4);Unit(battle,"far").Position=new(7,4);Step(battle,22);
            Require(!battle.PendingEvents.Any(e=>e.Type=="hook_caught") && battle.PendingEvents.Any(e=>e.Type=="hook_end"),"moving targets evade locked hook; empty retract");
        }
        foreach(var tick in new[]{2,7})
        using(var battle=new BattleSimulation(Config(hero)))
        { Step(battle,tick);Unit(battle,"hero").DisabledTicks=6;var before=Unit(battle,"front").Position;Step(battle,12);
          Require(battle.PendingEvents.Any(e=>e.Type=="hook_end") && Unit(battle,"front").Position==before,"control cancels flight or live pull"); }
        using(var battle=new BattleSimulation(Config(hero,new ProbeRule(null,true))))
        { Step(battle,20);Require(!battle.PendingEvents.Any(e=>e.Type=="hook_start"),"wall excludes unreachable aim"); }
        using(var battle=new BattleSimulation(Config(hero,new ProbeRule(c=>{if(c.Tick==6)c.Units.Single(u=>u.RuntimeId=="front").Position=new(2.5f,3);}))))
        {
            Unit(battle,"front").Position=new(2.5f,1);Step(battle,7);
            Require(battle.PendingEvents.Any(e=>e.Type=="hook_caught" && e.TargetRuntimeId=="front"),"relative sweep catches a body crossing between ticks");
        }
        using(var battle=new BattleSimulation(Config(hero,new ProbeRule(c=>{if(c.Tick is 2 or 8)c.Damage("front",c.Units.Single(u=>u.RuntimeId=="hero"),100);}))))
        {
            var a=Unit(battle,"hero");a.Health=a.MaxHealth*.4f;a.Attributes.SetBaseValue(CombatAttribute.Armor,0);Step(battle,3);var shield=a.Shield;
            Require(shield>0,$"low health shield activates: hp={a.Health}/{a.MaxHealth} shield={shield} events={string.Join(';',battle.CombatEvents.Select(e=>$"{e.Kind}:{e.Source.StableId}:{e.EffectiveValue}"))}");a.Shield=0;Step(battle,8);
            Require(a.Alive && a.Shield==0,"emergency shield is once per battle");
        }
        using(var a=new BattleSimulation(Config(hero)))using(var b=new BattleSimulation(Config(hero)))
        { Step(a,25);Step(b,25);Require(a.CreateResult().Digest==b.CreateResult().Digest,"deterministic hook"); }
        using(var b=new BattleSimulation(Config(hero,fail:true)))
        { Step(b,2);Require(!b.PendingEvents.Any(e=>e.Type=="hook_start"),"failed activation rolls back hook state and events"); }
        GD.Print("  hook and emergency shield passed");
    }
    private static void Normalize(BattleUnitState a)
    { a.MaxHealth=1000;a.Health=1000;a.Attributes.SetBaseValue(CombatAttribute.Armor,0);a.CurrentMana=0;a.ManaLockedUntilTick=999; }
    public static BattleConfig Config(UnitSnapshot hero,IBattleFloorRuleRuntime? floor=null,bool fail=false)=>new()
    {
        Seed=20260919,FloorRule=floor??new ClearFloorRuleRuntime("techniques","常规",""),
        HeroRule=new(1,1,1,0,0,0,false,"",1,1,0,0,0,0,false,false,0,0,""),
        Spawns=[new(hero,0,new(1,2),"hero"),new(Dummy(),1,new(4,2),"front"),new(Dummy(),1,new(7,2),"far")],
        ConfigureCombatBindings=fail?bindings=>bindings.Subscribe(BattleCombatEventKind.AbilityResolved,CombatSourceRef.System("rollback"),0,
            (_,sink)=>sink.Enqueue(CombatSourceRef.System("rollback"),0,_=>throw new InvalidOperationException("rollback probe"))):null
    };
    private sealed class ProbeRule(Action<BattleRuleContext>? action,bool wall=false):ClearFloorRuleRuntime("probe","测试","")
    { public override void OnTick(BattleRuleContext c)=>action?.Invoke(c); public override bool CanOccupy(Vector2I c)=>!wall || c.X!=3; }
    private static UnitSnapshot Dummy()
    { using var d=new UnitDefinition{Id="technique_dummy",DisplayName="目标",MaxHealth=10000,AttackDamage=0,Armor=0};
      return BattleSetupFactory.Snapshot(d) with {Behavior=new(Stationary:true,DisableBasicAttacks:true)}; }
    private static BattleUnitState Unit(BattleSimulation b,string id)=>b.Units.Single(u=>u.RuntimeId==id);
    private static void Step(BattleSimulation b,int n){for(var i=0;i<n;i++)b.Step();}
    private static void CheckBodies(BattleSimulation b)
    { foreach(var a in b.Units.Where(u=>u.Alive))foreach(var c in b.Units.Where(u=>u.Alive && string.CompareOrdinal(u.RuntimeId,a.RuntimeId)>0))
        Require(a.Position.DistanceTo(c.Position)+.003f>=a.BodyRadius+c.BodyRadius,"hook bodies do not overlap"); }
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
