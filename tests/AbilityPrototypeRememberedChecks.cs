using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;

internal static class AbilityPrototypeRememberedChecks
{
    internal static void Run(ContentRegistry content)
    {
        FixedLayer(content);
        PairAndTransformOrder(content);
        DeadAndTemporary(content);
        AtomicRollback(content);
        CompilerRejectsPartialGroups();
        TimelineBoss(content);
        GD.Print("REMEMBERED_BATTLE_CONTRACT_OK fixed-after-multiply pair-stable before-after-transform no-retarget dead-recipient temporary-source-no-receipt temporary-recipient-excluded hp-fault atomic-retry entry-before-first-full-mana compiler-groups");
        GD.Print("TIMELINE_BOSS_CONTRACT_OK designated-instance ordinary-guard phase-mana empty-phase no-free-refill actual-cast");
    }
    private static BattleConfig Config(ContentRegistry content, bool sourceFirst = true, bool temporarySource = false,
        bool temporaryTarget = false, bool fullAtOpening = false, bool brokenHealth = false,
        Action<BattleCombatBindingRegistry>? bindings = null)
    {
        var source = AbilityPrototypeContent.CreateBattle(content, sourceFirst ? [13,11,8,16,0,0] : [11,13,8,16,0,0], [4,0,0,0,0,0]);
        var spawns = source.Spawns.Select(spawn =>
        {
            var unit = spawn.Unit with { Behavior = spawn.Unit.Behavior with { Stationary = true, DisableBasicAttacks = true } };
            if(unit.ContentId == "prototype_u13" && brokenHealth)
                unit = unit with { AbilityLoadout = new CompiledAbilityLoadout(unit.AbilityLoadout!.Abilities.Select(ability =>
                    ability.StableId == "prototype_u13_a" ? ability with { Operations = ability.Operations.Select(op =>
                        op is CompiledMatrixOperation { Kind: MatrixOperationKind.Transform } form ? form with {
                            Effects = form.Effects.Select(child => child.Kind == MatrixOperationKind.GrantRememberableAttribute &&
                                child.Attribute == CombatAttribute.MaxHealth ? child with { Amount = float.PositiveInfinity } : child).ToImmutableArray() } : op).ToImmutableArray() } : ability).ToImmutableArray()) };
            if(fullAtOpening && unit.ContentId == "prototype_u13")
                unit = unit with { AttributeDefinition = unit.AttributeDefinition! with { Attributes = unit.AttributeDefinition!.Attributes.Select(value =>
                    value.Attribute == CombatAttribute.StartingMana ? value with { BaseValue = 85 } : value).ToImmutableArray() } };
            var temporary = unit.ContentId == "prototype_u13" && temporarySource || unit.ContentId == "prototype_u16" && temporaryTarget;
            if (temporarySource && unit.ContentId == "prototype_u13")
                unit = unit with { AllowTemporaryMana = true };
            return spawn with { Unit = unit, IsTemporary = temporary, IsPersistentRosterHero = !temporary };
        }).ToList();
        return new BattleConfig { Seed = source.Seed, HeroRule = source.HeroRule, FloorRule = source.FloorRule,
            Spawns = spawns, TacticalSummons = source.TacticalSummons, ConfigureCombatBindings = bindings };
    }
    private static void Quiet(BattleSimulation battle)
    {
        foreach(var unit in battle.Units)
        {
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond,0);
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerAttack,0);
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerDamageRatio,0);
            unit.CurrentMana = 0; unit.AttackCooldown = 10000;
        }
    }
    private static BattleUnitState Unit(BattleSimulation battle,int number) => battle.Units.Single(unit => unit.Definition.ContentId == AbilityPrototypeContent.Id(number));
    private static void Cast(BattleSimulation battle, BattleUnitState unit)
    {
        // U13's authored Retreat(CurrentEnemy) requires a visible enemy within its
        // current melee attack reach, even though the following Transform is Self.
        var enemy = battle.Units.First(candidate => candidate.Team != unit.Team && candidate.Alive);
        enemy.Position = unit.Position + new Vector2(unit.BodyRadius + enemy.BodyRadius + .2f, 0);
        unit.CurrentMana = unit.MaxMana;
        battle.Step();
    }
    private static void Near(float actual,float expected,string message) => Require(Math.Abs(actual-expected)<.02f,message+$" actual={actual} expected={expected}");
    private static void FixedLayer(ContentRegistry content)
    {
        using var battle = new BattleSimulation(Config(content)); Quiet(battle);
        var hunter = Unit(battle,11); var origin = CombatSourceRef.System("remember-layer-fixture");
        var baseHealth = hunter.MaxHealth;
        var add = hunter.Attributes.ApplyModifier(new(CombatAttribute.MaxHealth,AttributeModifierOperation.Add,new CompiledConstantMagnitude(10),0,"add"),origin);
        var multiply = hunter.Attributes.ApplyModifier(new(CombatAttribute.MaxHealth,AttributeModifierOperation.Multiply,new CompiledConstantMagnitude(1.6f),0,"multiply"),origin);
        var post = hunter.Attributes.ApplyModifier(new(CombatAttribute.MaxHealth,AttributeModifierOperation.PostMultiplyAdd,new CompiledConstantMagnitude(120),0,"post"),origin);
        Near(hunter.MaxHealth,(baseHealth+10)*1.6f+120,"new layer follows multiplication");
        var replace = hunter.Attributes.ApplyModifier(new(CombatAttribute.MaxHealth,AttributeModifierOperation.Override,new CompiledConstantMagnitude(50),0,"override"),origin);
        Near(hunter.MaxHealth,50,"old override wins after new layer"); hunter.Attributes.Remove(replace);
        hunter.Attributes.Remove(post); Near(hunter.MaxHealth,(baseHealth+10)*1.6f,"old Add/Multiply semantics remain");
        hunter.Attributes.Remove(add); hunter.Attributes.Remove(multiply); Near(hunter.MaxHealth,baseHealth,"modifier removal restores base");
    }
    private static void PairAndTransformOrder(ContentRegistry content)
    {
        foreach(var first in new[]{false,true})
        foreach(var hunterFirst in new[]{false,true})
        {
            using var battle = new BattleSimulation(Config(content,sourceFirst:first)); Quiet(battle);
            var dragon = Unit(battle,13); var hunter = Unit(battle,11);
            var links = battle.OpeningAllyLinks.Where(link => link.SourceRuntimeId == dragon.RuntimeId).ToArray();
            Require(links.Length == 2 && links.Any(link => link.TargetRuntimeId == hunter.RuntimeId),"nearest two and stable tie include hunter");
            Require(links.Select(link=>link.TargetInstanceId).Distinct().Count()==2,"frozen targets are distinct");
            hunter.Attributes.ApplyModifier(new(CombatAttribute.MaxHealth,AttributeModifierOperation.Multiply,
                new CompiledConstantMagnitude(1.6f),0,"fixture-transform-multiplier"),CombatSourceRef.System("remember-test"));
            if(hunterFirst)Cast(battle,hunter);
            hunter.Health = hunter.MaxHealth; // Existing delayed self-healing must not obscure the direct HP grant.
            var hp = hunter.MaxHealth; var current = hunter.Health; var damage = hunter.Damage;
            dragon.Position = new(1,4); // A cast's retreat and later movement cannot change the opening choice.
            Cast(battle,dragon);
            Require(battle.CombatEvents.Any(fact => fact.Kind == BattleCombatEventKind.UnitTransformed &&
                fact.SourceRuntimeId == dragon.RuntimeId), "published U13 transform commits before checking its grants");
            Near(hunter.MaxHealth,hp+120,"temporary HP is a fixed post-multiply amount");
            Near(hunter.Health,current+120,"grant adds current HP directly");
            Near(hunter.Damage,damage+12,"fixed attack grant");
            var gains = battle.CreateResult().RememberedGains;
            Require(gains.Length==2 && gains.All(gain=>gain.SourceInstanceId==dragon.SourceInstanceId && gain.TargetInstanceId==hunter.SourceInstanceId &&
                gain.AbilityId=="prototype_u13_a" && gain.OperationKey=="opening_encouragement" && gain.RememberAbilityId=="prototype_u11_remember" &&
                gain.RememberOperationKey=="ally_fixed_gains" && gain.GrantOrdinal==1),"only eligible hunter receives the full provenance pair");
            Require(gains.Select(gain=>gain.GrantedTick).Distinct().Count()==1 && gains.Any(gain=>gain.Attribute==CombatAttribute.AttackDamage&&gain.Amount==12) &&
                gains.Any(gain=>gain.Attribute==CombatAttribute.MaxHealth&&gain.Amount==120),"same-tick exact AD/HP receipt pair");
            Require(!battle.CombatEvents.Any(fact=>fact.Kind==BattleCombatEventKind.HealingResolved&&fact.Source.StableId=="prototype_u13_a"),"HP grant is not healing");
            if(!hunterFirst)Cast(battle,hunter);
            Near(hunter.MaxHealth,hp+120,"later hunter transform never multiplies received HP again");
            Require(battle.CreateResult().RememberedGains.Length==2,"self transform and historical value create no extra receipt");
            hunter.Health=0;
            Require(battle.CreateResult().RememberedGains.Length==2,"received pair survives recipient death for Run settlement");
        }
        foreach(var first in new[]{false,true})
        {
            using var battle = new BattleSimulation(Config(content,sourceFirst:first,fullAtOpening:true));
            var dragon = Unit(battle,13);
            Require(dragon.CurrentMana == dragon.MaxMana, "authored full opening mana is initialized before the first tick");
            Cast(battle, dragon);
            Require(battle.CreateResult().RememberedGains.Length==2,"all entry qualifications precede the first full-mana transform");
            Require(battle.OpeningAllyLinks.Length==2,"entry does not register a second pair");
        }
    }
    private static void DeadAndTemporary(ContentRegistry content)
    {
        using(var battle = new BattleSimulation(Config(content)))
        {
            Quiet(battle); var dragon=Unit(battle,13); var hunter=Unit(battle,11); var far=Unit(battle,8); var original=far.Damage;
            hunter.Health=0; Cast(battle,dragon);
            Require(battle.CreateResult().RememberedGains.IsEmpty,"dead selected hunter receives nothing");
            Near(far.Damage,original,"dead opening vacancy never chooses a replacement");
        }
        using(var battle = new BattleSimulation(Config(content,temporarySource:true)))
        {
            Quiet(battle); var dragon=Unit(battle,13); var hunter=Unit(battle,11); var hp=hunter.MaxHealth;
            Require(dragon.HasManaSkill,"clean temporary dragon retains its full base mana skill"); Cast(battle,dragon);
            Near(hunter.MaxHealth,hp+120,"temporary source still grants full combat HP");
            Require(battle.CreateResult().RememberedGains.IsEmpty,"temporary source has no permanent receipt qualification");
        }
        using(var battle = new BattleSimulation(Config(content,temporaryTarget:true)))
        {
            var flower=Unit(battle,16);
            Require(!battle.OpeningAllyLinks.Any(link=>link.TargetRuntimeId==flower.RuntimeId),"temporary recipient never occupies a frozen opening target");
        }
    }
    private static void AtomicRollback(ContentRegistry content)
    {
        using(var battle = new BattleSimulation(Config(content,brokenHealth:true)))
        {
            Quiet(battle); var dragon=Unit(battle,13); var hunter=Unit(battle,11); var hp=hunter.MaxHealth; var damage=hunter.Damage;
            Cast(battle,dragon);
            Near(hunter.MaxHealth,hp,"HP operation fault restores HP"); Near(hunter.Damage,damage,"HP operation fault rolls back earlier AD");
            Require(battle.CreateResult().RememberedGains.IsEmpty && dragon.CurrentMana==dragon.MaxMana,"HP fault restores receipts, transform, payment");
        }
        var reject=true;
        using(var battle = new BattleSimulation(Config(content,bindings: registry=>registry.Subscribe(BattleCombatEventKind.UnitTransformed,
            CombatSourceRef.System("remember-fault"),0,(fact,sink)=>
            {if(reject&&fact.Source.StableId=="prototype_u13_a")sink.Enqueue(CombatSourceRef.System("remember-fault"),0,_=>throw new InvalidOperationException("intentional remembered rollback"));}))))
        {
            Quiet(battle); var dragon=Unit(battle,13); var hunter=Unit(battle,11); var damage=hunter.Damage;
            Cast(battle,dragon); Near(hunter.Damage,damage,"rejected transform removes both temporary grants");
            Require(battle.CreateResult().RememberedGains.IsEmpty,"rejected transform removes receipt pair");
            reject=false; Cast(battle,dragon);
            Require(battle.CreateResult().RememberedGains.Length==2,"retry grants one pair after rollback restores keys and form state");
            Near(hunter.Damage,damage+12,"retry never doubles temporary attack");
        }
    }
    private static MatrixAbilityOperationSpec Grant(CombatAttribute attribute) => new() { Kind=MatrixOperationKind.GrantRememberableAttribute,
        Target=MatrixTargetKind.Self, Attribute=attribute, Amount=attribute==CombatAttribute.AttackDamage?12:120, Key="opening_encouragement" };
    private static void CompilerRejectsPartialGroups()
    {
        foreach(var malformed in new[]{0,1,2,3})
        {
            var pair=new[]{Grant(CombatAttribute.AttackDamage),Grant(CombatAttribute.MaxHealth)};
            MatrixAbilityOperationSpec[] forms = malformed switch {
                0 => [new(){Kind=MatrixOperationKind.Transform,Effects=[pair[0]]}],
                1 => [new(){Kind=MatrixOperationKind.Transform,Effects=[pair[0]]},new(){Kind=MatrixOperationKind.Transform,Effects=[pair[1]]}],
                2 => [new(){Kind=MatrixOperationKind.Transform,Effects=[pair[0],pair[1],Grant(CombatAttribute.MaxHealth)]}],
                _ => [new(){Kind=MatrixOperationKind.Transform,Effects=[pair[0],pair[1]]},new(){Kind=MatrixOperationKind.Counter,Key="opening_encouragement"}]
            };
            var authored = new AbilityDefinition { StableId="remember_contract_invalid",DisplayName="错误授予",
                ActivationKind=AbilityActivationKind.Automatic,Trigger=AbilityTriggerKind.ManaFull,ManaCost=85,MaxUses=1 };
            foreach(var form in forms)authored.Operations.Add(form);
            Require(AbilityDefinitionCompiler.Compile(authored).Report.HasCoreErrors,"compiler rejects partial/split/duplicate/shared group "+malformed);
        }
    }
    private static void TimelineBoss(ContentRegistry content)
    {
        Require(content.TryGet("enemy_ee03_trample_brute",out var entry),"published trample enemy");
        var profile=BattleSetupFactory.Snapshot(entry,content);
        var timeline=new BossTimelineSnapshot("boss-contract","enemy_ee03_trample_brute",
            [new("base","本体",1,profile.AbilityLoadout),new("empty","空阶段",.5f,null)]);
        var units=ImmutableArray.Create(new BattlePreparationUnitSource("player","prototype_u02",0,1,false,true,true,[]),
            new BattlePreparationUnitSource("boss","enemy_ee03_trample_brute",1,1,false,false,true,[]),
            new BattlePreparationUnitSource("guard","enemy_ee03_trample_brute",1,1,false,false,true,[]));
        var request=new BattlePreparationRequest(content,20261007,null,new ClearFloorRuleRuntime("boss-test","测试",""),units,
            [new("player",new(1,2)),new("boss",new(7,1)),new("guard",new(8,4))],"",new(),0,0,null,null,timeline,
            BattlePlacementValidation.ExactAll,HeroRuleOverride:HeroRuleSnapshot.Neutral);
        var config=BattlePreparationAssembler.Assemble(request);
        var prepared=BattlePreparationProjection.Build(config);
        Require(prepared.Units.Single(unit=>unit.RuntimeId=="boss").Definition.IsBoss &&
            !prepared.Units.Single(unit=>unit.RuntimeId=="guard").Definition.IsBoss,"only designated encounter instance is Boss");
        using(var battle=new BattleSimulation(config))
        {
            var boss=battle.Units.Single(unit=>unit.RuntimeId=="boss");var guard=battle.Units.Single(unit=>unit.RuntimeId=="guard");
            Require(boss.HasManaSkill&&boss.MaxMana>0&&guard.HasManaSkill,"Boss and ordinary guard keep their correct mana loadout");
            boss.Health=boss.MaxHealth*.4f; battle.Step();
            Require(!boss.HasManaSkill&&boss.MaxMana==0&&boss.AbilityLoadout!.Abilities.IsEmpty,"explicit empty phase is not a base fallback");
            boss.Health=boss.MaxHealth*.9f; battle.Step();
            Require(boss.HasManaSkill&&boss.CurrentMana<=boss.Attributes.GetValue(CombatAttribute.ManaPerSecond)*BattleTiming.TickSeconds+.001f,
                "phase return restores eligibility with only the normal tick regeneration, without refilling mana");
        }
        using(var battle=new BattleSimulation(config))
        {
            var boss=battle.Units.Single(unit=>unit.RuntimeId=="boss");boss.CurrentMana=boss.MaxMana; battle.Step();
            Require(battle.CombatEvents.Any(fact=>fact.Kind==BattleCombatEventKind.AbilityResolved&&fact.SourceRuntimeId==boss.RuntimeId),"timeline Boss actually casts published ability");
        }
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
