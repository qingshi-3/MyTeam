using System;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class EnemyTrampleContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            Require(package.Content.TryGet("enemy_ee03_trample_brute", out var entry), "published giant");
            var caster = BattleSetupFactory.Snapshot(entry, package.Content) with
                { Behavior = new(Stationary: true, DisableBasicAttacks: true), MaxHealth = 10000 };
            var growthGate = await GrowthContentPackage.CreateReadyAsync(this);
            var growth = growthGate.Package ?? throw new InvalidOperationException(string.Join(';', growthGate.Report.CoreErrors));
            var focusedCase = OS.GetCmdlineUserArgs().FirstOrDefault(argument => argument.StartsWith("--case="))?[7..];
            if (focusedCase is null or "roster") CheckGrowthRosterContacts(growth.Content);
            if (focusedCase is null or "alternate") CheckAlternateSide(growth.Content);
            if (focusedCase is null or "grounded") CheckGroundedDisplacement(package.Content);
            CheckManaCycle(caster, entry);
            using (var battle = new BattleSimulation(Config(caster)))
            {
                Step(battle, 8);
                var cast = Unit(battle, "caster").Trample!;
                Require(cast is not null && Unit(battle,"caster").BodyRadius == caster.BodyRadius,
                    $"authored large body and pending cast: body={Unit(battle,"caster").BodyRadius}, pos={Unit(battle,"caster").Position}, events={string.Join(';',battle.PendingEvents.Select(e=>$"{e.Type}:{e.Cue}"))}");
                Step(battle, 11);
                Require(Hits(battle) == 0 && Unit(battle,"caster").Position.X == 8, "windup holds body and deals no damage");
                Step(battle, 30);
                Require(Hits(battle) == 3, "passes multiple bodies with one hit each, not stop-on-first");
                Require((Unit(battle,"first").Position.Y - 2) * (Unit(battle,"second").Position.Y - 2) < 0,
                    "centerline contacts alternate to opposite sides");
                Require(Unit(battle,"ally").Health == 1000 && Unit(battle,"ally").Position.Y != 2,
                    "allied body displaced without friendly damage");
                Require(Unit(battle,"caster").Position.X < 2 && Unit(battle,"caster").Trample is null,
                    "full travel and bounded recovery finish");
                Require(battle.CombatEvents.Where(e => e.Kind == BattleCombatEventKind.SkillHitLanded)
                    .GroupBy(e => e.TargetRuntimeId).All(g => g.Count() == 1), "damage distinct per body");
            }
            using (var battle = new BattleSimulation(Config(caster)))
            {
                Step(battle, 8); var locked = Unit(battle,"caster").Trample!.End;
                Unit(battle,"third").Position = new(1,4);
                Step(battle, 27);
                Require(battle.PendingEvents.Single(e => e.Type == "trample_rush").Trample!.End == locked,
                    "target movement cannot steer locked charge");
            }
            foreach (var cancelTick in new[] { 10, 22 })
            using (var battle = new BattleSimulation(Config(caster)))
            {
                Step(battle,cancelTick); var before=Unit(battle,"caster").Position;
                Unit(battle,"caster").DisabledTicks=3; Step(battle,6);
                Require(Unit(battle,"caster").Trample is null && Unit(battle,"caster").Position == before,
                    "control cancels windup and moving phase");
            }
            using (var battle = new BattleSimulation(Config(caster)))
            {
                Step(battle,8);Unit(battle,"caster").Team=0;battle.Step();
                Require(Unit(battle,"caster").Trample is null,"allegiance invalidates charge");
            }
            var cramped=Config(caster);
            cramped.Spawns.Clear();
            var target=Target();
            cramped.Spawns.Add(new(caster,1,new(8,1),"caster"));
            cramped.Spawns.Add(new(target,0,new(5,1),"blocked"));
            cramped.Spawns.Add(new(target,0,new(1,1),"far"));
            cramped.Spawns.Add(new(target,1,new(5,2),"upper-wall"));
            using (var battle = new BattleSimulation(cramped))
            {
                // Keep the lateral lane narrower than the calibrated body; the former 1.8-cell
                // fixture now legitimately fits beside this wall with the smaller 1.4-cell giant.
                Unit(battle,"caster").Position=new(8,.8f);
                Unit(battle,"blocked").Position=new(5,.55f);
                Unit(battle,"far").Position=new(1,.8f);
                Unit(battle,"upper-wall").Position=new(5,1.35f);
                Step(battle,45);
                Require(Unit(battle,"caster").Position.X > 5 && Hits(battle)==1,
                    "wall prevents lateral clearing, charger stops safely without tunneling");
            }
            var crowded=Config(caster);crowded.Spawns.Clear();
            crowded.Spawns.Add(new(caster,1,new(0,0),"caster"));
            crowded.Spawns.Add(new(target,0,new(0,0),"other"));
            using (var battle=new BattleSimulation(crowded)) CheckBodies(battle);
            using (var first = new BattleSimulation(Config(caster)))
            using (var second = new BattleSimulation(Config(caster)))
            {
                Step(first,48);Step(second,48);
                Require(first.CreateResult().Digest == second.CreateResult().Digest,"deterministic contacts and sides");
            }
            using (var battle = new BattleSimulation(Config(caster, true)))
            {
                Step(battle,8);
                Require(Unit(battle,"caster").Trample is null && !battle.PendingEvents.Any(e => e.Type=="trample_charge"),
                    "failed ability commit rolls back pending motion and warning");
                Require(Unit(battle,"caster").CurrentMana == 100 && Unit(battle,"caster").ManaLockedUntilTick == 0,
                    "failed charge restores full mana and resource lock");
            }
            var store=new BattleLabPresetStore(GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres"));
            var index = new BattleLabContentIndex(package);
            var session = new BattleLabSession(index, 8);
            session.SetRules(BattleLabPlacementMode.FreeExperiment, 8, 20260919, "rule_clear");
            Require(!session.AddAndPlace(caster.ContentId, BattleLabSide.Enemy, new(9,2)).Succeeded,
                "lab rejects giant extending past arena edge before mutation");
            Require(session.AddAndPlace(caster.ContentId, BattleLabSide.Enemy, new(8,2)).Succeeded,
                "lab accepts space for entire giant");
            Require(!session.AddAndPlace("enemy_rust_guard", BattleLabSide.Enemy, new(8,3)).Succeeded && session.Units.Count==1,
                "adjacent anchor is still occupied by the giant's body, rejection is atomic");
            Require(session.AddAndPlace("enemy_rust_guard", BattleLabSide.Enemy, new(6,2)).Succeeded,
                "separated neighbor fits");
            Require(!session.Move(session.Units.Single(u=>u.ContentId==caster.ContentId).InstanceId, new(7,2), false).Succeeded,
                "moving a giant cannot overlap a neighbor without sharing its anchor");
            var preset=store.BuiltIns.Single(p=>p.Key.StartsWith("EE03 ·",StringComparison.Ordinal));
            var config=new BattleLabPreparationAdapter(new BattleLabContentIndex(package)).Build(BattleLabPresetStore.ToSnapshot(preset.Value));
            using (var battle=new BattleSimulation(config))
            {
                Step(battle,45);Require(battle.PendingEvents.Any(e=>e.Type=="trample_rush"),"shipped preset performs charge");
            }
            var generator=new TowerGenerator(package.Project.Campaign);
            var prepare=new RunBattlePreparationService(package.Content,package.Project,new RunRelicService(package.Content));
            for(ulong seed=1;seed<=4;seed++)
            {
                var run=new ActiveRunDto {Seed=seed,FloorIndex=5,
                    EquippedTacticalCommandIds=package.Project.RunRules.StarterTacticalCommandIds.ToList(),
                    Roster=[new(){InstanceId="hero",ContentId="hero_hc03_iron_guard"}]};
                run.Deployment[0]="hero";
                var encounter=generator.Encounter(run,TowerNodeType.Elite);
                Require(encounter.EnemyIds.Count(id=>id==caster.ContentId)==1,"single authored elite lead");
                var prepared=prepare.Build(run,encounter,false);
                using var battle=new BattleSimulation(prepared);CheckBodies(battle);
                Require(prepared.Spawns.All(spawn=>Unit(battle,spawn.InstanceId).Position==BattlefieldSpace.CellCenter(spawn.Cell)),
                    "formal deployment preview and battle have identical body anchors");
            }
            var disposable=new BattleSimulation(Config(caster));Step(disposable,10);
            var owner=Unit(disposable,"caster");disposable.Dispose();Require(owner.Trample is null,"scope cleanup");
            GD.Print("ENEMY_TRAMPLE_CONTRACT_OK mana-cycle resource-bar binding full-target/control-wait action-lock rollback large-spawn swept-sides multi-hit ally-safe locked-direction control wall deterministic preset encounter cleanup");
            GetTree().Quit();
        }
        catch(Exception error){GD.PrintErr("ENEMY_TRAMPLE_CONTRACT_FAILED "+error);GetTree().Quit(1);}
    }
    private void CheckManaCycle(UnitSnapshot caster, CatalogEntry entry)
    {
        var information = new UnitInformation("trample", (UnitDefinition)entry.Definition, caster);
        Require(information.UsesMana && information.Skills.Length == 1 && information.Skills[0].Category == "主动",
            "enemy has one mana-driven active skill without a fabricated passive");
        using (var battle = new BattleSimulation(Config(caster)))
        {
            var owner = Unit(battle, "caster");
            Require(owner.MaxMana == 100 && owner.CurrentMana == 90 && !owner.Definition.IsHero,
                "non-hero owns the authored mana pool");
            var presenter = entry.Scene.Instantiate<UnitContentRoot>();
            AddChild(presenter);
            try
            {
                var view = presenter.GetNode<HealthViewComponent>("HealthViewComponent");
                presenter.RefreshCombatResources(owner.CurrentMana, owner.MaxMana, 0, [], true);
                Require(view.ManaBar.Visible && view.ManaBar.Value == 90 && view.ManaBar.MaxValue == 100,
                    "authored enemy presenter binds the visible resource bar");
                BattleHeroMana.OnAttack(owner, 0); BattleHeroMana.OnDamage(owner, 1000, 0);
                Require(owner.CurrentMana == 90, "attacking and taking damage do not refill this enemy");
                Step(battle, 7);
                Require(owner.Trample is null && owner.CurrentMana == 98.75f, "no cast before full mana");
                Step(battle, 1);
                Require(owner.Trample is not null && owner.CurrentMana == 0,
                    "full mana starts the charge and spends the whole pool immediately");
                presenter.RefreshCombatResources(owner.CurrentMana, owner.MaxMana, 0, [], true);
                Require(view.ManaBar.Visible && view.ManaBar.Value == 0 &&
                    view.GetNodeOrNull<Label>("ReadyMarker") is null,
                    "resource bar stays present and empties on the committed cast");
                for (var tick = 0; tick < 60 && owner.Trample is not null; tick++)
                {
                    Require(owner.CurrentMana == 0, "no normal refill during windup, charge or recovery");
                    Step(battle, 1);
                }
                Require(owner.Trample is null && owner.CurrentMana > 0 && owner.CurrentMana < 100,
                    "refill resumes after the complete action");
                var casts = battle.PendingEvents.Count(e => e.Type == "trample_charge");
                owner.CurrentMana = 100;
                Step(battle, 1);
                Require(owner.Trample is not null && owner.CurrentMana == 0 &&
                    battle.PendingEvents.Count(e => e.Type == "trample_charge") == casts + 1,
                    "next full pool casts without the old hidden cooldown");
            }
            finally { presenter.Free(); }
        }
        var noTarget = Config(caster);
        noTarget.Spawns.Clear();
        noTarget.Spawns.Add(new(caster, 1, new(8, 2), "caster"));
        noTarget.Spawns.Add(new(Target(), 0, new(7, 3), "near"));
        using (var battle = new BattleSimulation(noTarget))
        {
            var owner = Unit(battle, "caster");
            Step(battle, 12);
            Require(owner.CurrentMana == 100 && owner.Trample is null,
                "no legal charge target preserves full mana");
            owner.DisabledTicks = 3;
            Unit(battle, "near").Position = new(4, 2);
            Step(battle, 3);
            Require(owner.CurrentMana == 100 && owner.Trample is null, "control preserves full mana");
            Step(battle, 1);
            Require(owner.CurrentMana == 0 && owner.Trample is not null, "cast starts when control ends");
        }
    }
    private static void CheckGrowthRosterContacts(ContentRegistry content)
    {
        foreach (var heroId in new[] { "hero_mx01", "gx04_phase_anchor", "hero_hc03_iron_guard" })
        {
            var battle = GrowthContactBattle(content, heroId);
            using (battle)
            {
                var hero = Unit(battle, "hero");
                var charger = Unit(battle, "caster");
                var contactX = hero.Position.X;
                Step(battle, 45);
                Require(Math.Abs(hero.Position.Y - 2) > .1f && charger.Position.X < contactX - charger.BodyRadius,
                    $"growth roster contact clears and charger continues: hero={heroId}, heroPos={hero.Position}, chargerPos={charger.Position}");
                Require(battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.SkillHitLanded &&
                    e.TargetRuntimeId == hero.RuntimeId) == 1, "growth roster target takes exactly one trample hit: " + heroId);
            }
        }
    }
    private static void CheckAlternateSide(ContentRegistry content)
    {
        using var battle = GrowthContactBattle(content, "hero_mx01", new Vector2(4, 2.2f),
            ("blocker", "hero_hc03_iron_guard", new Vector2(4, 3), 0));
        var hero = Unit(battle, "hero");
        var charger = Unit(battle, "caster");
        Step(battle, 45);
        Require(hero.Position.Y < 1.9f && charger.Position.X < 3,
            $"blocked preferred side retries open opposite side: heroPos={hero.Position}, blockerPos={Unit(battle,"blocker").Position}, chargerPos={charger.Position}");
    }
    private static void CheckGroundedDisplacement(ContentRegistry content)
    {
        using var battle = GrowthContactBattle(content, "hero_hc31_breach_lancer", new Vector2(5, 2),
            ("charge-target", "enemy_rust_guard", new Vector2(5, 1), 1));
        var hero = Unit(battle, "hero");
        hero.CurrentMana = 0;
        Step(battle, 21);
        hero.CurrentMana = hero.MaxMana;
        Step(battle, 1);
        Require(battle.PendingEvents.Any(e => e.Type == "displacement" && e.TargetRuntimeId == hero.RuntimeId &&
                e.Displacement is { Finished: false } && e.Displacement.Kind == DisplacementKind.Charge),
            "HC31 has an active grounded charge before EE03 contact");
        var contactX = hero.Position.X;
        Step(battle, 23);
        Require(Math.Abs(hero.Position.Y - 2) > .1f && Unit(battle, "caster").Position.X < contactX - .7f,
            $"trample takes over grounded displacement and continues: heroPos={hero.Position}, chargerPos={Unit(battle,"caster").Position}");
        Require(battle.PendingEvents.Any(e => e.Type == "displacement" && e.TargetRuntimeId == hero.RuntimeId &&
                e.Displacement is { Kind: DisplacementKind.Charge, Finished: true, Cancelled: true }),
            "superseded grounded displacement is cancelled");
    }
    private static BattleSimulation GrowthContactBattle(ContentRegistry content, string heroId,
        Vector2? heroPosition = null, params (string RuntimeId, string ContentId, Vector2 Position, int Team)[] extras)
    {
        Require(content.TryGet("enemy_ee03_trample_brute", out var casterEntry), "growth publishes EE03");
        var caster = BattleSetupFactory.Snapshot(casterEntry, content) with
            { Behavior = new(Stationary: true, DisableBasicAttacks: true), MaxHealth = 10000 };
        var hero = PlayerSnapshot(content, heroId) with
            { Behavior = new(Stationary: true, DisableBasicAttacks: true), MaxHealth = 10000 };
        var config = new BattleConfig
        {
            Seed = 20260927, FloorRule = new ClearFloorRuleRuntime("trample_growth", "常规", ""),
            HeroRule = HeroRuleSnapshot.Neutral,
            Spawns = [new(caster, 1, new(8, 2), "caster"), new(hero, 0, new(4, 2), "hero")]
        };
        foreach (var extra in extras)
        {
            Require(content.TryGet(extra.ContentId, out var extraEntry), "content publishes fixture body " + extra.ContentId);
            config.Spawns.Add(new(BattleSetupFactory.Snapshot(extraEntry, content) with
                { Behavior = new(Stationary: true, DisableBasicAttacks: true), MaxHealth = 10000 }, extra.Team,
                BattlefieldSpace.PositionToCell(extra.Position), extra.RuntimeId));
        }
        var battle = new BattleSimulation(config);
        Unit(battle, "hero").Position = heroPosition ?? new Vector2(4, 2);
        foreach (var extra in extras) Unit(battle, extra.RuntimeId).Position = extra.Position;
        return battle;
    }
    private static UnitSnapshot PlayerSnapshot(ContentRegistry content, string contentId)
    {
        if (content.TryGet(contentId, out var entry)) return BattleSetupFactory.Snapshot(entry, content);
        Require(contentId == "hero_hc31_breach_lancer", "content publishes player hero " + contentId);
        var definition = GD.Load<UnitDefinition>("res://content/definitions/heroes/hero_hc31_breach_lancer.tres");
        var authored = GD.Load<AbilityLoadoutDefinition>("res://content/abilities/loadouts/loadout_hero_hc31_breach_lancer.tres");
        var compiled = AbilityDefinitionCompiler.CompileLoadout(authored,
            status => status is null ? null : content.Graph.ResolveStatus(status.StableId));
        Require(definition is not null && compiled.Loadout is not null && !compiled.Report.HasCoreErrors,
            "retained HC31 definition and loadout compile for grounded displacement fixture");
        return BattleSetupFactory.Snapshot(definition!, abilityLoadout: compiled.Loadout);
    }
    public static BattleConfig Config(UnitSnapshot caster,bool fail=false)=>new()
    {
        Seed=20260919,FloorRule=new ClearFloorRuleRuntime("trample_test","常规",""),
        HeroRule=new(1,1,1,0,0,0,false,"",1,1,0,0,0,0,false,false,0,0,""),
        Spawns=[new(caster,1,new(8,2),"caster"),new(Target(),1,new(6,2),"ally"),
            new(Target(),0,new(4,2),"first"),new(Target(),0,new(2,2),"second"),new(Target(),0,new(0,2),"third")],
        ConfigureCombatBindings=fail?bindings=>bindings.Subscribe(BattleCombatEventKind.AbilityResolved,
            CombatSourceRef.System("trample_rollback"),0,(_,sink)=>sink.Enqueue(CombatSourceRef.System("trample_rollback"),0,
                _=>throw new InvalidOperationException("rollback probe"))):null
    };
    private static UnitSnapshot Target()
    {
        using var d=new UnitDefinition{Id="trample_body",DisplayName="碰撞测试",IsHero=true,MaxHealth=1000,AttackDamage=0,Armor=0};
        return BattleSetupFactory.Snapshot(d) with {Behavior=new(Stationary:true,DisableBasicAttacks:true)};
    }
    private static BattleUnitState Unit(BattleSimulation b,string id)=>b.Units.Single(u=>u.RuntimeId==id);
    private static int Hits(BattleSimulation b)=>b.CombatEvents.Count(e=>e.Kind==BattleCombatEventKind.SkillHitLanded);
    private static void Step(BattleSimulation b,int n){for(var i=0;i<n;i++){b.Step();CheckBodies(b);}}
    private static void CheckBodies(BattleSimulation b)
    {
        var live=b.Units.Where(u=>u.Alive).ToArray();
        foreach(var u in live)
        {
            Require(BattlefieldSpace.IsCircleInsideArena(u.Position,u.BodyRadius,BattleSimulation.Width,BattleSimulation.Height),"body inside arena: "+u.RuntimeId);
            foreach(var v in live.Where(v=>string.CompareOrdinal(v.RuntimeId,u.RuntimeId)>0))
                Require(u.Position.DistanceTo(v.Position)+.003f>=u.BodyRadius+v.BodyRadius,$"bodies do not overlap: {u.RuntimeId} {v.RuntimeId}");
        }
    }
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
