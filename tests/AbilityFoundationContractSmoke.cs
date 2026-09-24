using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

// Bounded synthetic system contracts. This scene is not automatically launched by development.
public partial class AbilityFoundationContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            AttackEventExecutesAttributedAbility();
            DefeatedOwnerMayExecuteItsDeathAbility();
            PeriodicTargetingDoesNotRequireMana();
            PassiveGrantFollowsOwnerLifetime();
            FailedReactionRestoresOnlyItsOwnMutation();
            FormulaDamageUsesUnifiedDefense();
            InitialGrantsProjectBeforeTheirGameplayEffects();
            SummoningDoesNotReplayTeamHealth();
            ReactiveStatusLimitsSkipNormally();
            GD.Print("ABILITY_FOUNDATION_CONTRACT_OK");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PrintErr("ABILITY_FOUNDATION_CONTRACT_FAILED: " + exception);
            GetTree().Quit(1);
        }
    }

    private static void AttackEventExecutesAttributedAbility()
    {
        var ability = EffectAbility("hit_proc", AbilityActivationKind.Triggered, AbilityTriggerKind.AttackHit,
            EffectKind.Damage, 23);
        using var battle = Battle(Unit("owner", [ability], damage: 10), Unit("enemy", []));
        battle.Step();
        var owner = battle.Units.Single(unit => unit.Definition.ContentId == "owner");
        var proc = battle.CombatEvents.SingleOrDefault(fact => fact.Kind == BattleCombatEventKind.DamageResolved &&
            fact.Source.Kind == CombatSourceKind.Ability && fact.Source.StableId == "hit_proc");
        Require(proc is not null && proc.SourceRuntimeId == owner.RuntimeId && proc.EffectiveValue > 0,
            "AttackLanded must execute a typed, owner-attributed Ability damage invocation");
        Require(battle.CombatEvents.Count(fact => fact.Kind == BattleCombatEventKind.AbilityResolved &&
            fact.SubjectStableId == ability.StableId) == 1, "one attack fact triggers exactly one ability");
    }

    private static void DefeatedOwnerMayExecuteItsDeathAbility()
    {
        var ability = EffectAbility("death_proc", AbilityActivationKind.Triggered, AbilityTriggerKind.OwnerDefeated,
            EffectKind.Damage, 41);
        using var battle = Battle(Unit("owner", [ability], health: 8, damage: 0), Unit("enemy", [], damage: 30));
        battle.Step();
        Require(battle.CombatEvents.Any(fact => fact.Kind == BattleCombatEventKind.AbilityResolved &&
            fact.SubjectStableId == "death_proc"), "death trigger is allowed to retain its defeated source identity");
        Require(battle.CombatEvents.Any(fact => fact.Kind == BattleCombatEventKind.DamageResolved &&
            fact.Source.Kind == CombatSourceKind.Ability && fact.Source.StableId == "death_proc" && fact.EffectiveValue > 0),
            "death ability resolves against the living killer, not the corpse");
    }

    private static void PeriodicTargetingDoesNotRequireMana()
    {
        var ability = EffectAbility("periodic_heal", AbilityActivationKind.Automatic, AbilityTriggerKind.PeriodicTick,
            EffectKind.Heal, 27) with { IntervalTicks = 1, AutomaticTarget = AbilityAutomaticTargetKind.WoundedAlly };
        using var battle = Battle(Unit("owner", [ability]), Unit("enemy", []));
        var owner = battle.Units.Single(unit => unit.Definition.ContentId == "owner");
        owner.Health = 100;
        battle.Step();
        Require(owner.MaxMana == 0 && owner.Health > 120,
            "periodic explicit-target healing selects a wounded ally without a mana-full trigger");
    }

    private static void PassiveGrantFollowsOwnerLifetime()
    {
        using var status = new StatusDefinition
        {
            StableId = "passive_marker", DisplayName = "持续授予标记", Behavior = StatusBehaviorKind.None,
            DurationKind = StatusDurationKind.Permanent, AggregationPolicy = StatusAggregationPolicy.BySource,
            StackLimit = 1, DispelCategory = StatusDispelCategory.NonDispellable,
            GrantedTags = [new StringName("contract.passive")]
        };
        var compiled = StatusDefinitionCompiler.Compile(status).Definition ?? throw new InvalidOperationException("status compile");
        var passive = new CompiledAbilityDefinition("passive_grant", "持续授予", "", AbilityActivationKind.Passive,
            AbilityTriggerKind.None, 0, 0, 0, 0, 0,
            [new CompiledApplyStatusAbilityOperation(compiled, new CompiledRelativeTeamTargetQuery(EffectRelativeTeam.Allies, false))], null);
        using var battle = Battle(Unit("owner", [passive], health: 8), Unit("enemy", [], damage: 30),
            extraAlly: Unit("recipient", []));
        var recipient = battle.Units.Single(unit => unit.Definition.ContentId == "recipient");
        Require(recipient.Statuses.Any(item => item.StableId == "passive_marker"), "passive grants activate in real battle setup");
        battle.Step();
        Require(!recipient.Statuses.Any(item => item.StableId == "passive_marker"),
            "owner death revokes only the departing passive grant from its living recipients");
    }

    private static void FailedReactionRestoresOnlyItsOwnMutation()
    {
        var ability = EffectAbility("failing_proc", AbilityActivationKind.Triggered, AbilityTriggerKind.AttackHit,
            EffectKind.Damage, 23);
        using var battle = Battle(Unit("owner", [ability], damage: 10), Unit("enemy", []), bindings: bindings =>
            bindings.Subscribe(BattleCombatEventKind.AbilityResolved, CombatSourceRef.System("contract_fail"), 0,
                (fact, sink) =>
                {
                    if (fact.SubjectStableId == "failing_proc")
                        sink.Enqueue(CombatSourceRef.System("contract_fail"), 0,
                            _ => throw new InvalidOperationException("injected failure"));
                }));
        battle.Step();
        var enemy = battle.Units.Single(unit => unit.Definition.ContentId == "enemy");
        Require(Math.Abs(enemy.Health - 990) < .01f, "failed proc rolls back extra damage, not the committed basic attack");
        Require(!battle.CombatEvents.Any(fact => fact.Source.StableId == "failing_proc"),
            "failed proc leaves no successful typed effect event");
    }

    private static CompiledAbilityDefinition EffectAbility(string id, AbilityActivationKind kind, AbilityTriggerKind trigger,
        EffectKind effect, float amount, EffectDamageType damageType = EffectDamageType.Normal) => new(id, id, "", kind, trigger, 0, 0, 0, 0, 0,
        [new CompiledEffectAbilityOperation(new CompiledEffectBinding(id + "_effect", 0,
            new CompiledEffectTrigger(EffectTriggerKind.Manual, EffectDomainEventKind.None), [], new CompiledExplicitTargetQuery(),
            [new CompiledEffectStep(effect, EffectAmountSource.Fixed, amount, DamageType: damageType)], new CompiledEffectBindingLimits(0, 0, 0, 0), null),
            AbilityInvocationValueSource.Fixed, 1)], null);

    private static void FormulaDamageUsesUnifiedDefense()
    {
        CompiledAttributeMagnitude Scale(CombatAttribute attribute, float ratio) =>
            new CompiledCompositeMagnitude(AttributeMagnitudeOperation.Multiply,
                [new CompiledSourceAttributeMagnitude(attribute, AttributeCaptureMode.Snapshot),
                    new CompiledConstantMagnitude(ratio)], AttributeCaptureMode.Snapshot);
        CompiledAttributeMagnitude?[] formulas = [null, Scale(CombatAttribute.AttackDamage, 2),
            Scale(CombatAttribute.SpellPower, 4),
            new CompiledCompositeMagnitude(AttributeMagnitudeOperation.Add,
                [Scale(CombatAttribute.AttackDamage, 1), Scale(CombatAttribute.SpellPower, 1),
                    new CompiledConstantMagnitude(25)], AttributeCaptureMode.Snapshot)];
        foreach (var damageType in Enum.GetValues<EffectDamageType>())
        foreach (var formula in formulas)
        foreach (var shield in new[] { 0f, 20f, 200f })
        foreach (var health in new[] { 1000f, 25f })
        {
            var ability = EffectAbility("typed_damage", AbilityActivationKind.Automatic, AbilityTriggerKind.PeriodicTick,
                EffectKind.Damage, 100, damageType);
            var operation = (CompiledEffectAbilityOperation)ability.Operations[0];
            ability = ability with { IntervalTicks = 1, Operations = [operation with { Binding = operation.Binding with
                { Effects = [new CompiledEffectStep(EffectKind.Damage, EffectAmountSource.Fixed,
                    formula is null ? 100 : 1, formula, damageType)] } }] };
            using var battle = Battle(
                Unit("owner", [ability]) with { Behavior = new(Stationary: true, DisableBasicAttacks: true) },
                Unit("enemy", []) with { Behavior = new(Stationary: true, DisableBasicAttacks: true) },
                bindings: registry => registry.SubscribeCalculation(BattleCombatCalculationKind.Damage,
                    CombatSourceRef.System("shared_damage_modifier"), 0, (_, amount) => amount * 1.2f));
            var owner = battle.Units.Single(unit => unit.RuntimeId == "owner");
            owner.Attributes.SetBaseValue(CombatAttribute.AttackDamage, 50);
            owner.Attributes.SetBaseValue(CombatAttribute.SpellPower, 25);
            var enemy = battle.Units.Single(unit => unit.RuntimeId == "enemy");
            enemy.Attributes.SetBaseValue(CombatAttribute.Armor, 100f / 7f);
            enemy.Shield = shield;
            enemy.Health = health;
            battle.Step();
            var fact = battle.CombatEvents.Single(value => value.Kind == BattleCombatEventKind.DamageResolved &&
                value.Source.StableId == "typed_damage");
            var resolved = damageType == EffectDamageType.Normal ? 60f : 120f;
            var absorbed = Math.Min(shield, resolved);
            var healthLost = Math.Min(health, resolved - absorbed);
            var effective = absorbed + healthLost;
            Require(fact.DamageType == damageType && Math.Abs(fact.RequestedValue - 100) < .01f &&
                Math.Abs(fact.AppliedValue - resolved) < .01f && Math.Abs(fact.EffectiveValue - effective) < .01f,
                "fixed, attack, spell-power and mixed formulas share modifiers and defense; only true damage bypasses defense");
            Require(Math.Abs(enemy.Shield - (shield - absorbed)) < .01f &&
                Math.Abs(enemy.Health - (health - healthLost)) < .01f,
                "both damage categories absorb shields before health, without counting overkill");
            var reports = battle.CreateResult().Units;
            Require(Math.Abs(reports.Single(unit => unit.RuntimeId == "owner").DamageDealt - effective) < .01f &&
                Math.Abs(reports.Single(unit => unit.RuntimeId == "enemy").DamageTaken - effective) < .01f &&
                Math.Abs(reports.Single(unit => unit.RuntimeId == "enemy").ShieldAbsorbed - absorbed) < .01f,
                "effective damage and shield statistics retain attribution");
            if (healthLost == health)
                Require(battle.CombatEvents.Any(value => value.Kind == BattleCombatEventKind.UnitKilled &&
                    value.Source.StableId == "typed_damage" && value.DamageType == damageType),
                    "lethal damage retains ability and damage-category attribution");
        }
    }

    private static UnitSnapshot Unit(string id, ImmutableArray<CompiledAbilityDefinition> abilities, float health = 1000, float damage = 0)
    {
        using var definition = new UnitDefinition
        {
            Id = id, DisplayName = id, IsHero = true, MaxHealth = health, AttackDamage = damage,
            AttackRange = 5, AttackCooldown = 100, Armor = 0, HealPower = 0
        };
        return BattleSetupFactory.Snapshot(definition, abilityLoadout: abilities.IsEmpty ? null : new CompiledAbilityLoadout(abilities));
    }

    private static BattleSimulation Battle(UnitSnapshot owner, UnitSnapshot enemy, UnitSnapshot? extraAlly = null,
        Action<BattleCombatBindingRegistry>? bindings = null, TraitBattlePreparation? traits = null, float ownerHealthRatio = 1,
        UnitSnapshot? summon = null)
    {
        var spawns = ImmutableArray.CreateBuilder<BattleSpawn>();
        spawns.Add(new BattleSpawn(owner, 0, new Vector2I(2, 2), "owner", HealthRatio: ownerHealthRatio, IsPersistentRosterHero: true));
        spawns.Add(new BattleSpawn(enemy, 1, new Vector2I(4, 2), "enemy"));
        if (extraAlly is not null) spawns.Add(new BattleSpawn(extraAlly, 0, new Vector2I(0, 5), "ally", IsPersistentRosterHero: true));
        return new BattleSimulation(new BattleConfig
        {
            Seed = 6812, FloorRule = new ClearFloorRuleRuntime("foundation", "系统契约", ""),
            Spawns = spawns.ToList(), ConfigureCombatBindings = bindings,
            Traits = traits ?? TraitBattlePreparation.Empty,
            Summons = new SummonProfiles(DeathSummon: summon),
            HeroRule = new HeroRuleSnapshot(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, "")
        });
    }

    private static void InitialGrantsProjectBeforeTheirGameplayEffects()
    {
        foreach (var (kind, amount) in new[] { (EffectKind.Damage, 30f), (EffectKind.Heal, 30f), (EffectKind.Damage, 5000f) })
        {
            using var authored = new StatusDefinition
            {
                StableId = "setup_grant", DisplayName = "初始化授予", Behavior = StatusBehaviorKind.None,
                DurationKind = StatusDurationKind.Permanent, AggregationPolicy = StatusAggregationPolicy.BySource,
                StackLimit = 1, DispelCategory = StatusDispelCategory.NonDispellable,
                AttributeModifiers = [new AttributeModifierSpec { Attribute = CombatAttribute.MaxHealth,
                    Magnitude = new ConstantAttributeMagnitudeSpec { Value = 100 } }]
            };
            var binding = ((CompiledEffectAbilityOperation)EffectAbility("setup_effect", AbilityActivationKind.Automatic,
                AbilityTriggerKind.BattleStarted, kind, amount).Operations[0]).Binding with { TargetQuery = new CompiledOwnerTargetQuery() };
            var status = StatusDefinitionCompiler.Compile(authored).Definition! with
            {
                LifecycleBindings = [new CompiledStatusLifecycleBinding(StatusLifecycleTriggerKind.Applied, binding)]
            };
            var passive = new CompiledAbilityDefinition("setup_passive", "初始化被动", "", AbilityActivationKind.Passive,
                AbilityTriggerKind.None, 0, 0, 0, 0, 0,
                [new CompiledApplyStatusAbilityOperation(status, new CompiledOwnerTargetQuery())], null);
            using var traitAuthoring = new TraitDefinition
            {
                StableId = "setup_trait", DisplayName = "初始化羁绊", SemanticIconKey = "hero", CountingPolicy = new(),
                Breakpoints = [new TraitBreakpointSpec { MinValue = 1, MaxValue = 2, DisplayStyle = "active", GrantedStatuses = [authored] }]
            };
            var trait = TraitDefinitionCompiler.Compile(traitAuthoring, _ => status).Definition!;
            var preparation = TraitBattlePreparationBuilder.Build([trait],
                [new TraitContributionInput("setup_trait", 1, 0, TraitContributionSourceKind.Hero,
                    "owner", "owner", "owner", true, false, true)]);
            using var battle = Battle(Unit("owner", [passive]), Unit("enemy", []), traits: preparation, ownerHealthRatio: .5f);
            var owner = battle.Units.Single(unit => unit.RuntimeId == "owner");
            if (amount == 5000)
            {
                Require(!owner.Alive && owner.Health == 0,
                    "lethal initial Applied effect cannot be undone by later health normalization or revoked queued grants");
                continue;
            }
            var expected = kind == EffectKind.Damage ? 540 : 660;
            Require(Math.Abs(owner.MaxHealth - 1200) < .01f && Math.Abs(owner.Health - expected) < .01f,
                "Trait and Passive MaxHealth project before ratio initialization; both Applied effects survive exactly once");
        }
    }

    private static void SummoningDoesNotReplayTeamHealth()
    {
        using var authored = new StatusDefinition
        {
            StableId = "join_grant", DisplayName = "入场授予", Behavior = StatusBehaviorKind.None,
            DurationKind = StatusDurationKind.Permanent, AggregationPolicy = StatusAggregationPolicy.BySource,
            StackLimit = 1, DispelCategory = StatusDispelCategory.NonDispellable,
            AttributeModifiers = [new AttributeModifierSpec { Attribute = CombatAttribute.MaxHealth,
                Magnitude = new ConstantAttributeMagnitudeSpec { Value = 100 } }]
        };
        var binding = ((CompiledEffectAbilityOperation)EffectAbility("join_damage", AbilityActivationKind.Automatic,
            AbilityTriggerKind.BattleStarted, EffectKind.Damage, 30).Operations[0]).Binding with { TargetQuery = new CompiledOwnerTargetQuery() };
        var status = StatusDefinitionCompiler.Compile(authored).Definition! with
        {
            LifecycleBindings = [new CompiledStatusLifecycleBinding(StatusLifecycleTriggerKind.Applied, binding)]
        };
        using var traitAuthoring = new TraitDefinition
        {
            StableId = "join_trait", DisplayName = "入场羁绊", SemanticIconKey = "hero",
            CountingPolicy = new() { TemporaryUnitPolicy = TraitTemporaryUnitPolicy.Include },
            Breakpoints = [new TraitBreakpointSpec { MinValue = 2, MaxValue = 3, DisplayStyle = "active", GrantedStatuses = [authored] }]
        };
        var trait = TraitDefinitionCompiler.Compile(traitAuthoring, _ => status).Definition!;
        var preparation = TraitBattlePreparationBuilder.Build([trait],
            [new TraitContributionInput("join_trait", 1, 0, TraitContributionSourceKind.Hero,
                "owner", "owner", "owner", true, false, true)]);
        var ability = new CompiledAbilityDefinition("join_summon", "召唤", "", AbilityActivationKind.Automatic,
            AbilityTriggerKind.PeriodicTick, 0, 0, 0, 1, 1,
            [new CompiledSummonAbilityOperation(AbilitySummonProfile.DeathSummon, 1, 1, 1, 1, true, "")], null);
        var summon = Unit("summon", []) with { TraitContributions = [new CompiledTraitContribution("join_trait", 1)] };
        using var battle = Battle(Unit("owner", [ability]), Unit("enemy", []), traits: preparation,
            ownerHealthRatio: .5f, summon: summon);
        battle.Step();
        var owner = battle.Units.Single(unit => unit.RuntimeId == "owner");
        Require(owner.MaxHealth == 1100 && Math.Abs(owner.Health - 470) < .01f,
            "summon-triggered grant does not implicitly heal an existing ally or overwrite its Applied damage");
    }

    private static void ReactiveStatusLimitsSkipNormally()
    {
        foreach (var limits in new[] { new CompiledEffectBindingLimits(1, 0, 0, 0), new CompiledEffectBindingLimits(0, 10, 0, 0) })
        {
            using var authored = new StatusDefinition
            {
                StableId = "limited_status", DisplayName = "限流被动", Behavior = StatusBehaviorKind.None,
                DurationKind = StatusDurationKind.Permanent, AggregationPolicy = StatusAggregationPolicy.BySource,
                StackLimit = 1, DispelCategory = StatusDispelCategory.NonDispellable
            };
            var binding = ((CompiledEffectAbilityOperation)EffectAbility("limited_heal", AbilityActivationKind.Automatic,
                AbilityTriggerKind.BattleStarted, EffectKind.Heal, 23).Operations[0]).Binding with
                { TargetQuery = new CompiledOwnerTargetQuery(), Limits = limits };
            var status = StatusDefinitionCompiler.Compile(authored).Definition! with
            {
                CombatReactiveBindings = [new CompiledStatusCombatReactiveBinding(BattleCombatEventKind.AttackLanded,
                    StatusReactiveOwnerRole.OwnerIsSource, StatusReactiveEffectSourcePolicy.PrimaryContribution, 0, binding)]
            };
            var passive = new CompiledAbilityDefinition("limited_passive", "限流授予", "", AbilityActivationKind.Passive,
                AbilityTriggerKind.None, 0, 0, 0, 0, 0,
                [new CompiledApplyStatusAbilityOperation(status, new CompiledOwnerTargetQuery())], null);
            using var battle = Battle(Unit("owner", [passive]), Unit("enemy", []), ownerHealthRatio: .5f);
            battle.Step();
            var owner = battle.Units.Single(unit => unit.RuntimeId == "owner");
            owner.AttackCooldown = 0;
            battle.Step();
            Require(Math.Abs(owner.Health - 523) < .01f && battle.Outcome == BattleOutcome.Running,
                "a second reaction blocked by usage or interval limits is a normal skip, not a battle exception");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
