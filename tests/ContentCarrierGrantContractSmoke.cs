using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Equipment;
using TowerAutobattler.Effects;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

public partial class ContentCarrierGrantContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            GrantsAreIsolatedIdempotentAndAtomic();
            TraitTargetsFollowContributors();
            EquipmentCanReactAsEventTarget();
            EquipmentRejectsMissingMagnitudeContext();
            StatusReactiveFiltersRespectSourceAndDamage();
            CarrierFingerprintsIncludeReferencedStatusRules();
            GD.Print("CONTENT_CARRIER_GRANTS_OK isolation=grant-not-source revoke=atomic trait=members equipment=received-hit context=fail-fast");
            GetTree().Quit(0);
        }
        catch (Exception exception) { GD.PrintErr(exception); GetTree().Quit(1); }
    }

    private static StatusDefinition Passive() => new()
    {
        StableId = "probe_passive", DisplayName = "测试被动", Behavior = StatusBehaviorKind.None,
        AttributeModifiers = [new AttributeModifierSpec
        {
            Attribute = CombatAttribute.AttackDamage, Magnitude = new ConstantAttributeMagnitudeSpec { Value = 5 }
        }]
    };

    private static BattleAttributeSet Attributes(string id) => new("probe", id,
        AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute, float> { [CombatAttribute.AttackDamage] = 10 }));

    private static BattleStatusScope StatusScope(params BattleAttributeSet[] attributes) => new("probe_status", (_, _) => { },
        id => attributes.FirstOrDefault(item => item.OwnerRuntimeId == id), _ => true, null, null);

    private static void GrantsAreIsolatedIdempotentAndAtomic()
    {
        var stats = Attributes("owner");
        using var statuses = StatusScope(stats);
        var definition = StatusDefinitionCompiler.Compile(Passive()).Definition!;
        statuses.ApplyBatch([new(definition, "owner", "owner", 0, "gear_a"), new(definition, "owner", "owner", 0, "gear_b")]);
        Expect(stats.GetValue(CombatAttribute.AttackDamage) == 20, "same owner grants merged and lost one contribution");
        statuses.ApplyBatch([new(definition, "owner", "owner", 0, "gear_a")]);
        Expect(statuses.LiveInstanceCount == 2 && stats.GetValue(CombatAttribute.AttackDamage) == 20, "grant reapplication was not idempotent");
        try
        {
            statuses.ReplaceGrants([""], []);
            throw new InvalidOperationException("blank grant revocation was accepted");
        }
        catch (ArgumentException) { }
        try
        {
            statuses.ReplaceGrants(["gear_a"], [new(definition, "owner", "missing_owner", 0, "bad_grant")]);
            throw new InvalidOperationException("expected invalid grant failure");
        }
        catch (InvalidOperationException exception) when (!exception.Message.StartsWith("expected")) { }
        Expect(stats.GetValue(CombatAttribute.AttackDamage) == 20 && statuses.LiveInstanceCount == 2, "failed replacement lost prior grant");
        statuses.RevokeGrant("gear_a");
        Expect(stats.GetValue(CombatAttribute.AttackDamage) == 15 && statuses.LiveInstanceCount == 1, "revocation removed another grant with same source");
        statuses.HandleOwnerDeath("owner");
        Expect(stats.GetValue(CombatAttribute.AttackDamage) == 10 && statuses.LiveInstanceCount == 0, "death leaked passive grant modifiers");
    }

    private static void TraitTargetsFollowContributors()
    {
        var first = Attributes("runtime_a"); var second = Attributes("runtime_b");
        using var statuses = StatusScope(first, second);
        var authoredStatus = Passive(); var compiledStatus = StatusDefinitionCompiler.Compile(authoredStatus).Definition!;
        var trait = TraitDefinitionCompiler.Compile(new TraitDefinition
        {
            StableId = "probe_trait", DisplayName = "测试羁绊", SemanticIconKey = "hero", CountingPolicy = new(),
            Breakpoints = [new TraitBreakpointSpec { MinValue = 1, MaxValue = 2, DisplayStyle = "active",
                TargetPolicy = TraitTargetPolicy.Contributors, GrantedStatuses = [authoredStatus] }]
        }, _ => compiledStatus).Definition!;
        TraitContributionInput Contribution(string owner) => new("probe_trait", 1, 0, TraitContributionSourceKind.Hero,
            owner, owner, owner, true, false, true);
        using var scope = new BattleTraitScope("trait_probe", TraitBattlePreparationBuilder.Build([trait], [Contribution("roster_a")]),
            [new("runtime_a", 0, first, "roster_a"), new("runtime_b", 0, second, "roster_b")],
            new StatusGrantRuntimeContext { Tick = () => 0, CanReceive = _ => true, Replace = (revoke, grants) => statuses.ReplaceGrants(revoke, grants) });
        Expect(first.GetValue(CombatAttribute.AttackDamage) == 15 && second.GetValue(CombatAttribute.AttackDamage) == 10, "member-only tier leaked to teammate");
        scope.Refresh([Contribution("roster_b")]);
        Expect(first.GetValue(CombatAttribute.AttackDamage) == 10 && second.GetValue(CombatAttribute.AttackDamage) == 15, "same-tier member change did not revoke and grant");
        scope.Refresh([]);
        Expect(statuses.LiveInstanceCount == 0 && second.GetValue(CombatAttribute.AttackDamage) == 10, "tier loss leaked granted passive");
    }

    private static void EquipmentCanReactAsEventTarget()
    {
        var stats = Attributes("owner"); using var statuses = StatusScope(stats);
        var authoredStatus = Passive(); var compiledStatus = StatusDefinitionCompiler.Compile(authoredStatus).Definition!;
        var definition = EquipmentDefinitionCompiler.Compile(new EquipmentDefinition
        {
            StableId = "probe_gear", ReactiveStatusBindings = [new EquipmentReactiveStatusBindingSpec
            {
                EventKind = BattleCombatEventKind.DamageResolved, OwnerRole = StatusReactiveOwnerRole.OwnerIsTarget,
                Target = EquipmentReactiveStatusTarget.Owner, Source = EquipmentReactiveStatusSource.Owner, Status = authoredStatus
            }]
        }, _ => compiledStatus).Definition!;
        ImmutableArray<EquipmentBattleInstanceSnapshot> items = [new("gear", "probe_gear", "hero", 0, definition)];
        using var equipment = new EquipmentBattleScope("equipment_probe", new(EquipmentStateFingerprint.Compute(items), items),
            [new("hero", "owner", true, stats)]);
        var pipeline = new BattleCombatEventPipeline("equipment_events");
        equipment.Activate(new EquipmentBattleRuntimeContext { CombatBindings = new(pipeline), CanReceiveStatus = _ => true,
            ApplyStatuses = requests => statuses.ApplyBatch(requests) });
        pipeline.Publish(new(BattleCombatEventKind.DamageResolved, CombatSourceRef.Unit("probe_enemy", "enemy", "enemy"), "enemy", "owner", 1,
            EffectiveValue: 5));
        Expect(statuses.LiveInstanceCount == 1, "incoming damage did not trigger equipment owner-as-target binding");
        equipment.Remove("gear");
        Expect(pipeline.SubscriptionCount == 0, "equipment removal leaked event subscription");
    }

    private static void EquipmentRejectsMissingMagnitudeContext()
    {
        var result = EquipmentDefinitionCompiler.Compile(new EquipmentDefinition
        {
            StableId = "unsupported_context", AttributeModifiers = [new AttributeModifierSpec
            { Attribute = CombatAttribute.AttackDamage, Magnitude = new TeamCountAttributeMagnitudeSpec() }]
        });
        Expect(result.Definition is null && result.Report.HasCoreErrors, "equipment accepted missing team-count context");
    }

    private static void StatusReactiveFiltersRespectSourceAndDamage()
    {
        var stats = Attributes("owner");
        var pipeline = new BattleCombatEventPipeline("status_filter_events");
        var reactions = 0;
        using var statuses = new BattleStatusScope("filter_status", (_, _) => { }, id => id == "owner" ? stats : null,
            _ => true, null, null,
            combatReactiveRegistrar: request => pipeline.Subscribe(request.EventKind, request.Source, request.Priority, request.Listener),
            reactiveEffectSink: _ => { reactions++; return true; });
        var authored = Passive();
        var reactive = new StatusCombatReactiveBindingSpec
        {
            EventKind = BattleCombatEventKind.DamageResolved, OwnerRole = StatusReactiveOwnerRole.OwnerIsTarget,
            SourceKind = CombatSourceKind.Ability, FilterDamageType = true, DamageType = EffectDamageType.Normal,
            Binding = new EffectBindingSpec
            {
                StableId = "probe_reactive", Trigger = new EffectTriggerSpec { Kind = EffectTriggerKind.Manual },
                TargetQuery = new OwnerTargetQuerySpec(), Limits = new EffectBindingLimitsSpec(),
                Effects = [new ShieldEffectSpec { Amount = 1 }]
            }
        };
        authored.CombatReactiveBindings = [reactive];
        var compiled = StatusDefinitionCompiler.Compile(authored);
        Expect(compiled.Definition is not null, "valid reactive damage filter was rejected");
        statuses.ApplyBatch([new(compiled.Definition!, "owner", "owner", 0)]);
        void Damage(CombatSourceKind source, EffectDamageType damage) => pipeline.Publish(new(
            BattleCombatEventKind.DamageResolved, new(source, "probe_source", "enemy", "source_instance"),
            "enemy", "owner", 1, EffectiveValue: 5, DamageType: damage));
        Damage(CombatSourceKind.Unit, EffectDamageType.Normal);
        Damage(CombatSourceKind.Ability, EffectDamageType.True);
        Expect(reactions == 0, "reactive binding ignored source or damage-type filter");
        Damage(CombatSourceKind.Ability, EffectDamageType.Normal);
        Expect(reactions == 1, "matching normal ability damage did not trigger");
        reactive.EventKind = BattleCombatEventKind.HealingResolved;
        Expect(StatusDefinitionCompiler.Compile(authored).Definition is null, "nondamage event accepted a meaningless damage filter");
    }

    private static void CarrierFingerprintsIncludeReferencedStatusRules()
    {
        (CompiledStatusDefinition Status, string[] Fingerprints) Compile(float amount)
        {
            var authored = Passive();
            ((ConstantAttributeMagnitudeSpec)authored.AttributeModifiers[0].Magnitude).Value = amount;
            var status = StatusDefinitionCompiler.Compile(authored).Definition!;
            var equipment = EquipmentDefinitionCompiler.Compile(new EquipmentDefinition
                { StableId = "fingerprint_gear", GrantedStatuses = [authored] }, _ => status);
            var reactiveEquipment = EquipmentDefinitionCompiler.Compile(new EquipmentDefinition
            {
                StableId = "fingerprint_reactive_gear", ReactiveStatusBindings = [new EquipmentReactiveStatusBindingSpec
                {
                    EventKind = BattleCombatEventKind.AttackLanded, Source = EquipmentReactiveStatusSource.Owner,
                    Target = EquipmentReactiveStatusTarget.Owner, Status = authored
                }]
            }, _ => status);
            var trait = TraitDefinitionCompiler.Compile(new TraitDefinition
            {
                StableId = "fingerprint_trait", DisplayName = "指纹羁绊", SemanticIconKey = "hero", CountingPolicy = new(),
                Breakpoints = [new TraitBreakpointSpec
                    { MinValue = 1, MaxValue = 2, DisplayStyle = "active", GrantedStatuses = [authored] }]
            }, _ => status);
            var relic = RelicDefinitionCompiler.Compile(new RelicDefinition
            {
                StableId = "fingerprint_relic", StatusGrants = [new RelicStatusGrantSpec
                    { BindingId = "passive", Target = new RelicPlayerHeroesTargetSpec(), Status = authored }]
            }, resolveStatus: _ => status);
            Expect(equipment.Definition is not null && reactiveEquipment.Definition is not null &&
                trait.Definition is not null && relic.Definition is not null, "fingerprint fixture compilation failed");
            return (status, [equipment.Definition!.Fingerprint, reactiveEquipment.Definition!.Fingerprint,
                trait.Definition!.Fingerprint, relic.Definition!.Fingerprint]);
        }
        var before = Compile(5);
        var after = Compile(5.00001f);
        Expect(before.Status.StableId == after.Status.StableId && before.Status.Description == after.Status.Description,
            "fingerprint precision fixture must retain identity and rounded description");
        Expect(StatusDefinitionFingerprint.Compute(before.Status) != StatusDefinitionFingerprint.Compute(after.Status),
            "status fingerprint discarded precise numeric edit");
        for (var index = 0; index < before.Fingerprints.Length; index++)
            Expect(before.Fingerprints[index] != after.Fingerprints[index], $"carrier[{index}] ignored referenced status numeric edit");
        var repeated = Compile(5);
        Expect(before.Fingerprints.SequenceEqual(repeated.Fingerprints), "equivalent status recompilation changed carrier identity");
    }

    private static void Expect(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
