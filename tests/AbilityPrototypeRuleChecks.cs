using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;

internal static class AbilityPrototypeRuleChecks
{
    internal static void Run(ContentRegistry content)
    {
        CloneAndHealth(content);
        ShieldRollback(content);
        DelayedStorm(content);
        AssassinAndFollowups(content);
        EmpoweredAttacks(content);
        AncientTreeProtection(content);
        AncientTreeInterruptAndRollback(content);
        AbilityPrototypeRememberedChecks.Run(content);
        AbilityPrototypeBatch1Checks.Run(content);
        Godot.GD.Print("ANCIENT_TREE_PROTECTION_OK authored three-tick-release original-derived-only skill-dot-pass follow enter-exit expiry owner-death overlap control cast-aura-rollback deterministic");
    }
    private static void Quiet(BattleSimulation battle)
    {
        foreach(var unit in battle.Units)
        {
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond,0);
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerAttack,0);
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerDamageRatio,0);
            unit.CurrentMana=0;
            unit.AttackCooldown=10000;
        }
    }
    private static void Steps(BattleSimulation battle,int count) { for(var i=0;i<count;i++)battle.Step(); }
    private static void CloneAndHealth(ContentRegistry content)
    {
        using(var battle=new BattleSimulation(AbilityPrototypeContent.CreateBattle(content,
            AbilityPrototypeContent.Presets[1],AbilityPrototypeContent.Presets[0])))
        {
            var clone=battle.Units.Single(u=>u.IsTemporary);
            var original=battle.Units.Single(u=>u.RuntimeId==clone.SummonerRuntimeId);
            Require(battle.TraitSnapshot.Value("prototype_032",0)==3,"forest clone does not count toward thresholds");
            Require(clone.HasManaSkill && !ReferenceEquals(clone.Attributes,original.Attributes),"clone has independent full mana skill");
            Require(Math.Abs(clone.MaxHealth-original.Definition.MaxHealth*.6f)<.01f,"clone clean base health scaling");
            var before=original.Health; clone.Health-=40;
            Require(original.Health==before,"clone health does not mutate original");
        }
        using(var battle=new BattleSimulation(AbilityPrototypeContent.CreateBattle(content,[2,8,11,13,16,18],[4,0,0,0,0,0])))
        {
            Quiet(battle);
            var shifter=battle.Units.Single(u=>u.Team==0&&u.Definition.ContentId=="prototype_u08");
            Require(Math.Abs(shifter.MaxHealth-shifter.Definition.MaxHealth*1.2f)<.01f,"brawler initial base health");
            shifter.CurrentMana=shifter.MaxMana;
            Steps(battle,20);
            Require(battle.CombatEvents.Count(e=>e.Kind==BattleCombatEventKind.UnitTransformed&&e.SourceRuntimeId==shifter.RuntimeId)==1,"one real transform");
            Require(Math.Abs(shifter.MaxHealth-shifter.Definition.MaxHealth*1.6f)<.01f,"brawler and transform add on same base");
            Require(shifter.AttackDelivery==AttackDelivery.Melee && shifter.MaxMana==0,"form changes delivery and stops mana");
            shifter.AttackCooldown=0;
            var enemy=battle.Units.First(u=>u.Team==1);shifter.Position=enemy.Position-new Godot.Vector2(1,0);
            var eventsBefore=battle.CombatEvents.Count;
            Steps(battle,30);
            Require(battle.CombatEvents.Skip(eventsBefore).Any(e=>e.Kind==BattleCombatEventKind.AttackLanded&&e.SourceRuntimeId==shifter.RuntimeId),"transformed unit keeps attacking without mana");
            Steps(battle,30);
            Require(battle.CombatEvents.Count(e=>e.Kind==BattleCombatEventKind.UnitTransformed&&e.SourceRuntimeId==shifter.RuntimeId)==1,"transform cannot repeat");
        }
    }
    private static void ShieldRollback(ContentRegistry content)
    {
        var original=AbilityPrototypeContent.CreateBattle(content,[16,18,0,0,0,0],[4,0,0,0,0,0]);
        var reject=true; var injected=0;
        var config=new BattleConfig { Seed=original.Seed,HeroRule=original.HeroRule,FloorRule=original.FloorRule,
            Spawns=original.Spawns,Traits=original.Traits,TacticalSummons=original.TacticalSummons,
            ConfigureCombatBindings=bindings=>bindings.Subscribe(BattleCombatEventKind.AbilityResolved,
                CombatSourceRef.System("prototype-fault"),0,(fact,sink)=>
                {
                    if(!reject||fact.SubjectStableId!="prototype_u16_a")return;
                    injected++;
                    sink.Enqueue(CombatSourceRef.System("prototype-fault"),0,_=>throw new InvalidOperationException("intentional prototype rollback"));
                })};
        using var battle=new BattleSimulation(config);
        Quiet(battle);
        var flower=battle.Units.Single(u=>u.Definition.ContentId=="prototype_u16");
        flower.Health-=200;
        flower.CurrentMana=flower.MaxMana;
        Steps(battle,1);
        Require(injected==1 && flower.Shield==0 && flower.CurrentMana==flower.MaxMana,"failed cast rolls back shield, mana and windup");
        Require(!battle.CombatEvents.Any(e=>e.Kind==BattleCombatEventKind.ShieldResolved),"failed shield publishes no committed fact");
        reject=false;
        Steps(battle,1);
        var granted=flower.MaxHealth*.2f;
        Require(Math.Abs(flower.Shield-granted)<.01f,"protector shield granted before windup finishes");
        Steps(battle,10);
        flower.Health-=200;
        flower.CurrentMana=flower.MaxMana; flower.ManaLockedUntilTick=0;
        Steps(battle,1);
        Require(Math.Abs(flower.Shield-granted)<.01f,"same source refresh does not stack shields");
        Steps(battle,41);
        Require(flower.Shield==0,"refreshed shield expires without removing unrelated health");
    }
    private static void DelayedStorm(ContentRegistry content)
    {
        using var battle=new BattleSimulation(AbilityPrototypeContent.CreateBattle(content,[13,18,0,0,0,0],[4,7,0,0,0,0]));
        Quiet(battle); Steps(battle,79);
        Require(!battle.CombatEvents.Any(e=>e.Source.StableId=="prototype_270"),"storm waits eight seconds");
        Steps(battle,1);
        var hits=battle.CombatEvents.Where(e=>e.Kind==BattleCombatEventKind.DamageResolved&&e.Source.StableId=="prototype_270").ToArray();
        Require(hits.Length==2 && hits.All(e=>e.Tick==80),"one global strike hits each enemy at tick 80");
        Require(battle.Units.Where(u=>u.Team==0).All(u=>Math.Abs(u.Attributes.GetValue(CombatAttribute.AttackSpeed)-1.25f)<.01f),"element members gain speed after strike");
        Steps(battle,90);
        Require(battle.CombatEvents.Count(e=>e.Kind==BattleCombatEventKind.DamageResolved&&e.Source.StableId=="prototype_270")==2,"storm cannot repeat");
    }
    private static void AssassinAndFollowups(ContentRegistry content)
    {
        var config=AbilityPrototypeContent.CreateBattle(content,[5,0,0,0,0,0],[4,0,0,3,0,0]);
        for(var i=0;i<config.Spawns.Count;i++)config.Spawns[i]=config.Spawns[i] with
        {Unit=config.Spawns[i].Unit with {Behavior=new UnitBehaviorSnapshot(Stationary:true)}};
        using(var battle=new BattleSimulation(config))
        {
            Quiet(battle);
            Require(battle.TraitSnapshot.Resolve("prototype_756",0).ActiveBreakpoint is null,"single assassin does not activate crit tier");
            Steps(battle,6);
            var jump=battle.PendingEvents.FirstOrDefault(e=>e.Type=="displacement"&&e.SourceRuntimeId=="prototype-0-0");
            Require(jump?.Displacement is {StartTick:6} && jump.Displacement.End.X>7,"single assassin independently jumps to enemy backline at 0.6 seconds");
            Steps(battle,5);
            var assassin=battle.Units.Single(u=>u.Team==0);
            Require(battle.PendingEvents.Any(e=>e.SourceRuntimeId==assassin.RuntimeId && e.Displacement is {Finished:true,Cancelled:false}),"backline leap actually completes");
            Require(battle.Units.Where(u=>u!=assassin&&u.Alive).All(u=>u.Position.DistanceTo(assassin.Position)+.001f>=u.BodyRadius+assassin.BodyRadius),"landing does not overlap bodies");
        }
        using(var battle=new BattleSimulation(AbilityPrototypeContent.CreateBattle(content,AbilityPrototypeContent.Presets[0],AbilityPrototypeContent.Presets[3])))
        {
            Steps(battle,250);
            var derived=battle.CombatEvents.Where(e=>e.Kind==BattleCombatEventKind.AttackLanded && e.ActionId.StartsWith("derived:attack:",StringComparison.Ordinal)).ToArray();
            Require(derived.Length>0,"authored chain or shooter followups execute");
            Require(derived.All(e=>!e.ActionId.Contains("derived:derived:",StringComparison.Ordinal)),"derived attacks never recursively generate attacks");
            foreach(var group in derived.GroupBy(e=>string.Join(":",e.ActionId.Split(':').Take(4))))
                Require(group.Count()<=2 && group.Select(e=>e.TargetRuntimeId).Distinct().Count()==group.Count(),"followups bounded to distinct secondary targets");
        }
    }
    private static void EmpoweredAttacks(ContentRegistry content)
    {
        using var battle=new BattleSimulation(AbilityPrototypeContent.CreateBattle(content,[2,0,0,0,0,0],[4,7,0,0,0,0]));
        Quiet(battle);
        var owner=battle.Units.Single(u=>u.Team==0);
        var targets=battle.Units.Where(u=>u.Team==1).ToArray();
        owner.Position=new(3.5f,2.5f); targets[0].Position=new(4.5f,2.5f);targets[1].Position=new(5.5f,2.5f);
        owner.CurrentMana=owner.MaxMana; owner.AttackCooldown=0;
        Steps(battle,60);
        Require(battle.CombatEvents.Any(e=>e.Kind==BattleCombatEventKind.AttackLanded&&e.SourceRuntimeId==owner.RuntimeId&&e.Tick>5),"empowered unit attacks during its mana suppression");
        Require(battle.CombatEvents.Any(e=>e.Kind==BattleCombatEventKind.AttackLanded&&e.SourceRuntimeId==owner.RuntimeId&&e.ActionId.StartsWith("derived:attack:",StringComparison.Ordinal)),"empowered attacks produce chain hits within buff window");
        Require(owner.CurrentMana==0 && owner.MaxMana==0,"empowerment disables only mana regeneration");
        Steps(battle,25);
        Require(owner.MaxMana>0,"mana capacity returns when empowerment expires");
    }
    private static CompiledAbilityDefinition TreeEnemySkill(params MatrixAbilityOperationSpec[] operations)
    {
        var report = new ValidationReport();
        var compiled = operations.Select(op => (CompiledAbilityOperation)(MatrixAbilityCompiler.Compile(op, "tree-contract", report)
            ?? throw new InvalidOperationException("tree fixture compile"))).ToImmutableArray();
        Require(!report.HasCoreErrors, "tree fixture compiles");
        return new("tree_contract_enemy", "测试命中", "", AbilityActivationKind.Automatic,
            AbilityTriggerKind.ManaFull, 1, 0, 0, 0, 0, compiled, null);
    }
    private static MatrixAbilityOperationSpec TreeEnemyOperation(MatrixOperationKind kind, float amount = 20) => new()
    { Kind = kind, Target = MatrixTargetKind.NearestEnemies, Range = 30, MaxTargets = 64, Amount = amount, DurationTicks = 5 };
    private static BattleSimulation TreeBattle(ContentRegistry content, CompiledAbilityDefinition? enemySkill = null,
        Action<BattleCombatBindingRegistry>? bindings = null, CompiledAbilityDefinition? treeSkill = null, bool extraTree = false)
    {
        var source = AbilityPrototypeContent.CreateBattle(content, [25,16,0,0,0,0], [4,0,0,0,0,0]);
        var spawns = source.Spawns.Select(spawn => spawn with { Unit = spawn.Unit with
        {
            Behavior = spawn.Unit.Behavior with { Stationary = true, DisableBasicAttacks = spawn.Team == 0 },
            Range = 10,
            AbilityLoadout = spawn.Team == 1 && enemySkill is not null ? new CompiledAbilityLoadout([enemySkill]) :
                spawn.Unit.ContentId == "prototype_u25" && treeSkill is not null ? new CompiledAbilityLoadout([treeSkill]) : spawn.Unit.AbilityLoadout
        }}).ToList();
        if(extraTree)spawns.Add(spawns[0] with { InstanceId = "tree-overlap", Cell = new Godot.Vector2I(3,3), IsPersistentRosterHero = false });
        var battle = new BattleSimulation(new BattleConfig { Seed = source.Seed, HeroRule = source.HeroRule,
            FloorRule = source.FloorRule, Spawns = spawns, TacticalSummons = source.TacticalSummons,
            ConfigureCombatBindings = bindings });
        Quiet(battle);
        battle.Units.First(u => u.Definition.ContentId == "prototype_u25").Position = new(3,2);
        battle.Units.Single(u => u.Definition.ContentId == "prototype_u16").Position = new(4.5f,2);
        battle.Units.Single(u => u.Team == 1).Position = new(6,2);
        return battle;
    }
    private static bool TreeAura(BattleUnitState unit) => unit.Statuses.Any(status => status.StableId == "attack_dodge_aura");
    private static void AncientTreeProtection(ContentRegistry content)
    {
        var profile = AbilityPrototypeContent.Snapshot(content,25);
        Require(profile.MaxHealth == 1000 && profile.Damage == 42 && profile.Armor == 6 && profile.Range == .35f && profile.AttackTicks == 17,
            "tree published base stats and 0.60 attack speed quantization");
        var skill = profile.AbilityLoadout!.Find("prototype_u25_a")!;
        Require(skill.ManaCost == 110 && skill.Operations is [CompiledMatrixOperation { Kind: MatrixOperationKind.Windup,
            DurationTicks: 3, Effects: [CompiledMatrixOperation { Kind: MatrixOperationKind.AttackDodgeAura, Radius: 1.5f, DurationTicks: 25 }] }],
            "tree published interruptible aura contract");
        var hostile = TreeEnemySkill(TreeEnemyOperation(MatrixOperationKind.AdditionalAttack),
            TreeEnemyOperation(MatrixOperationKind.Damage), TreeEnemyOperation(MatrixOperationKind.Poison,1));
        using var battle = TreeBattle(content, hostile);
        var tree = battle.Units.Single(u => u.Definition.ContentId == "prototype_u25");
        var flower = battle.Units.Single(u => u.Definition.ContentId == "prototype_u16");
        var enemy = battle.Units.Single(u => u.Team == 1);
        Require(tree.CurrentMana == 0 && tree.MaxMana == 110, "quiet fixture and published max mana");
        tree.CurrentMana = tree.MaxMana;
        Steps(battle,1);
        Require(tree.CurrentMana == 0 && !TreeAura(tree), "committed windup pays mana before aura release");
        Steps(battle,2);
        Require(!TreeAura(tree), "aura absent before 0.3 second release");
        Steps(battle,1);
        Require(TreeAura(tree) && tree.Statuses.Single(status => status.StableId == "attack_dodge_aura").Magnitude == 1.5f,
            "release projects exact range and Chinese status");
        var release = battle.TickIndex;
        var health = flower.Health;
        enemy.AttackCooldown = 0; flower.DisabledTicks = 3;
        Steps(battle,1);
        Require(flower.Health == health && battle.CombatEvents.Any(f => f.Kind == BattleCombatEventKind.AttackDodged && f.TargetRuntimeId == flower.RuntimeId),
            "original attack dodges at inclusive 1.5 center boundary even for controlled ally");
        var before = battle.CombatEvents.Count;
        enemy.CurrentMana = enemy.MaxMana;
        Steps(battle,1);
        var facts = battle.CombatEvents.Skip(before).ToArray();
        Require(facts.Count(f => f.Kind == BattleCombatEventKind.AttackDodged) == 2 && !facts.Any(f => f.Kind == BattleCombatEventKind.AttackLanded),
            "qualified additional attack dodges for each protected ally and grants no landed reward");
        Require(facts.Any(f => f.Kind == BattleCombatEventKind.DamageResolved && f.DamageClass == CombatDamageClass.ActiveSkill) && flower.Health < health,
            "direct skill bypasses protection");
        var afterSkill = flower.Health;
        enemy.AttackCooldown = 10000;
        Steps(battle,10);
        Require(flower.Health < afterSkill && battle.CombatEvents.Any(f => f.Kind == BattleCombatEventKind.DamageResolved && f.Source.StableId == "matrix_poison"),
            "poison DOT bypasses protection");
        tree.Position = new(2,2); flower.Position = new(4.5f,2);
        before = battle.CombatEvents.Count; enemy.AttackCooldown = 0;
        Steps(battle,1);
        Require(battle.CombatEvents.Skip(before).Any(f => f.Kind == BattleCombatEventKind.AttackLanded && f.TargetRuntimeId == flower.RuntimeId),
            "moving provider away removes old-area protection immediately");
        tree.Position = new(3,2); flower.Position = new(4.5f,2);
        before = battle.CombatEvents.Count; enemy.AttackCooldown = 0;
        Steps(battle,1);
        Require(battle.CombatEvents.Skip(before).Any(f => f.Kind == BattleCombatEventKind.AttackDodged && f.TargetRuntimeId == flower.RuntimeId),
            "re-entering the current area restores protection");
        enemy.AttackCooldown = 10000;
        Steps(battle, release + 24 - battle.TickIndex);
        before = battle.CombatEvents.Count; enemy.AttackCooldown = 0;
        Steps(battle,1);
        Require(battle.CombatEvents.Skip(before).Any(f => f.Kind == BattleCombatEventKind.AttackLanded && f.TargetRuntimeId == flower.RuntimeId),
            "exact release plus 25 tick boundary has no stale protection");
        using var death = TreeBattle(content);
        var deadTree = death.Units.Single(u => u.Definition.ContentId == "prototype_u25");
        deadTree.CurrentMana = deadTree.MaxMana; Steps(death,4); deadTree.Health = 0;
        death.Units.Single(u => u.Team == 1).AttackCooldown = 0;
        Steps(death,1);
        Require(death.CombatEvents.Any(f => f.Kind == BattleCombatEventKind.AttackLanded && f.TargetRuntimeId != deadTree.RuntimeId),
            "dead provider never protects surviving ally");
        using(var overlap = TreeBattle(content, extraTree:true))
        {
            var trees = overlap.Units.Where(u => u.Definition.ContentId == "prototype_u25").ToArray();
            trees[0].Position = new(3,2); trees[1].Position = new(3,3);
            overlap.Units.Single(u => u.Definition.ContentId == "prototype_u16").Position = new(4,2);
            foreach(var provider in trees)provider.CurrentMana = provider.MaxMana;
            Steps(overlap,4);
            Require(trees.All(TreeAura), "overlapping providers release independent areas");
            var attacker = overlap.Units.Single(u => u.Team == 1);
            var factCount = overlap.CombatEvents.Count; attacker.AttackCooldown = 0; Steps(overlap,1);
            Require(overlap.CombatEvents.Skip(factCount).Count(f => f.Kind == BattleCombatEventKind.AttackDodged) == 1,
                "overlap produces one dodge receipt, no stacked reactions");
            trees[0].Health = 0; factCount = overlap.CombatEvents.Count; attacker.AttackCooldown = 0; Steps(overlap,1);
            Require(overlap.CombatEvents.Skip(factCount).Count(f => f.Kind == BattleCombatEventKind.AttackDodged) == 1,
                "other living provider remains after one provider dies");
        }
        using var a = TreeBattle(content); using var b = TreeBattle(content);
        a.Units.Single(u => u.Team == 0 && u.Definition.ContentId == "prototype_u25").CurrentMana = 110;
        b.Units.Single(u => u.Team == 0 && u.Definition.ContentId == "prototype_u25").CurrentMana = 110;
        Steps(a,35); Steps(b,35);
        Require(a.CreateResult().Digest == b.CreateResult().Digest, "tree aura deterministic replay");
    }
    private static void AncientTreeInterruptAndRollback(ContentRegistry content)
    {
        using(var battle = TreeBattle(content, TreeEnemySkill(TreeEnemyOperation(MatrixOperationKind.Control))))
        {
            var tree = battle.Units.Single(u => u.Definition.ContentId == "prototype_u25");
            tree.CurrentMana = tree.MaxMana; Steps(battle,1);
            battle.Units.Single(u => u.Team == 1).CurrentMana = 120; Steps(battle,8);
            Require(tree.CurrentMana == 0 && !TreeAura(tree) && !battle.PendingEvents.Any(e => e.Type == "attack_aura_start"),
                "real hard control cancels only uncompleted windup without refunding committed mana");
        }
        // Execute the same compiled release atom in an immediate invocation to inject
        // a failure while its world checkpoint is active, without ending the battle
        // through the outer Step failure boundary of an asynchronous job.
        var treeSkill = AbilityPrototypeContent.Snapshot(content,25).AbilityLoadout!.Find("prototype_u25_a")!;
        var releaseAtom = ((CompiledMatrixOperation)treeSkill.Operations.Single()).Effects.Single();
        foreach(var failRelease in new[]{false,true})
        {
            var injected = 0;
            using var battle = TreeBattle(content, bindings: bindings => bindings.Subscribe(
                failRelease ? BattleCombatEventKind.StatusApplied : BattleCombatEventKind.AbilityResolved,
                CombatSourceRef.System("tree-rollback"), 0, (fact,sink) =>
                {
                    if(fact.SubjectStableId != (failRelease ? "attack_dodge_aura" : "prototype_u25_a"))return;
                    injected++;
                    sink.Enqueue(CombatSourceRef.System("tree-rollback"),0,_=>throw new InvalidOperationException("intentional tree rollback"));
                }), treeSkill: failRelease ? treeSkill with { Operations = [releaseAtom] } : null);
            var tree = battle.Units.Single(u => u.Definition.ContentId == "prototype_u25");
            tree.CurrentMana = tree.MaxMana; Steps(battle,1);
            Require(injected > 0 && !TreeAura(tree) && !battle.PendingEvents.Any(e => e.Type == "attack_aura_start"),
                "failed invocation restores windup or release-atom lease and status/events");
            if(!failRelease)Require(tree.CurrentMana == tree.MaxMana, "failed initial commit restores mana");
            // A rolled-back provider lease must not influence the formal impact path.
            var enemy = battle.Units.Single(u => u.Team == 1); enemy.AttackCooldown = 0; Steps(battle,1);
            Require(battle.CombatEvents.Any(f => f.Kind == BattleCombatEventKind.AttackLanded), "failed aura cannot dodge subsequent attack");
        }
    }
    private static void Require(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
}
