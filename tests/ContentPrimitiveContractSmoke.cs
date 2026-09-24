using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;

public partial class ContentPrimitiveContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            FormulaCaptureAndContextContract();
            AuthoredFormulaValidation();
            UnitAttributeAuthoring();
            RetiredAuthoringValuesAreRejected();
            SelectionAndEffectComposition();
            GD.Print("CONTENT_PRIMITIVE_CONTRACT_OK");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PrintErr("CONTENT_PRIMITIVE_CONTRACT_FAILED: " + exception);
            GetTree().Quit(1);
        }
    }

    private static void FormulaCaptureAndContextContract()
    {
        var formula = new CompiledCompositeMagnitude(AttributeMagnitudeOperation.Add,
            [new CompiledCompositeMagnitude(AttributeMagnitudeOperation.Multiply,
                [new CompiledSourceAttributeMagnitude(CombatAttribute.AttackDamage, AttributeCaptureMode.Live), new CompiledConstantMagnitude(2)], AttributeCaptureMode.Live),
             new CompiledCompositeMagnitude(AttributeMagnitudeOperation.Multiply,
                [new CompiledTargetAttributeMagnitude(CombatAttribute.MaxHealth, AttributeCaptureMode.Live), new CompiledConstantMagnitude(.05f)], AttributeCaptureMode.Live)],
            AttributeCaptureMode.Snapshot);
        var attack = 30f;
        var context = new BattleAttributeMagnitudeContext(sourceValue: _ => attack, targetValue: _ => 100);
        Expect(Math.Abs(AttributeMagnitudeSupport.Evaluate(formula, context) - 65f) < .001f, "formula composition incorrect");
        attack = 40;
        Expect(Math.Abs(AttributeMagnitudeSupport.Evaluate(formula, context) - 85f) < .001f, "formula evaluator captured mutable values itself");

        var unsupported = new CompiledCompositeMagnitude(AttributeMagnitudeOperation.Add,
            [new CompiledConstantMagnitude(1), new CompiledTeamCountMagnitude(AttributeTeamCountKind.Alive, 0, AttributeCaptureMode.Live)], AttributeCaptureMode.Live);
        var report = new ValidationReport();
        AttributeMagnitudeSupport.Validate(unsupported, AttributeContextCapabilities.SourceAttribute, report, "fixture");
        Expect(report.HasCoreErrors, "nested unavailable context passed carrier validation");
        var threw = false;
        try { AttributeMagnitudeSupport.Evaluate(unsupported, new BattleAttributeMagnitudeContext()); }
        catch (InvalidOperationException) { threw = true; }
        Expect(threw, "missing context silently became zero");
    }

    private static void AuthoredFormulaValidation()
    {
        using var root = new CompositeAttributeMagnitudeSpec { Operation = AttributeMagnitudeOperation.Multiply };
        using var constant = new ConstantAttributeMagnitudeSpec { Value = .5f };
        using var source = new SourceAttributeMagnitudeSpec { Attribute = CombatAttribute.AttackDamage };
        root.Operands = [constant, source];
        var report = new ValidationReport();
        var compiled = AttributeDefinitionCompiler.CompileMagnitude(root, report);
        Expect(compiled is not null && !report.HasCoreErrors, "valid authored formula rejected");
        var identity = AttributeMagnitudeSupport.Fingerprint(compiled!);
        constant.Value = .75f;
        var edited = AttributeDefinitionCompiler.CompileMagnitude(root, new ValidationReport());
        Expect(AttributeMagnitudeSupport.Fingerprint(edited!) != identity, "formula edit did not affect compiled identity");
        Expect(Math.Abs(AttributeMagnitudeSupport.Evaluate(compiled!, new BattleAttributeMagnitudeContext(sourceValue: _ => 20)) - 10) < .001f,
            "authored edit mutated published expression");
        root.Operands = [constant, root];
        var cycle = new ValidationReport();
        Expect(AttributeDefinitionCompiler.CompileMagnitude(root, cycle) is null && cycle.HasCoreErrors, "cyclic Resource graph accepted");
        root.Operands.Clear();
    }

    private static void UnitAttributeAuthoring()
    {
        using var definition = new UnitDefinition { Id = "fixture_unit", SpellPower = 42, Armor = 6 };
        var snapshot = BattleSetupFactory.Snapshot(definition);
        Expect(snapshot.AttributeDefinition!.Find(CombatAttribute.SpellPower).BaseValue == 42 &&
            snapshot.AttributeDefinition.Find(CombatAttribute.Armor).BaseValue == 6,
            "unit authoring did not reach compiled combat attributes");
        var fingerprint = DefinitionFingerprint.Compute(definition);
        definition.SpellPower = 43;
        Expect(DefinitionFingerprint.Compute(definition) != fingerprint && snapshot.AttributeDefinition.Find(CombatAttribute.SpellPower).BaseValue == 42,
            "unit attribute change failed identity/frozen snapshot boundary");
    }

    private static void RetiredAuthoringValuesAreRejected()
    {
        using var retiredAttribute = new SourceAttributeMagnitudeSpec { Attribute = (CombatAttribute)5 };
        var report = new ValidationReport();
        AttributeDefinitionCompiler.CompileMagnitude(retiredAttribute, report);
        Expect(report.HasCoreErrors, "retired magic resistance cannot silently become another formula input");
        using var binding = new EffectBindingSpec
        {
            StableId = "retired_damage", Trigger = new EffectTriggerSpec { Kind = EffectTriggerKind.Manual },
            TargetQuery = new ExplicitTargetQuerySpec(), Limits = new EffectBindingLimitsSpec(),
            Effects = [new DamageEffectSpec { DamageType = (EffectDamageType)1, Amount = 100 }]
        };
        Expect(EffectBindingCompiler.Compile(binding).Report.HasCoreErrors,
            "unmigrated damage resources are rejected rather than reinterpreted as true damage");
    }

    private static void SelectionAndEffectComposition()
    {
        var world = new SnapshotWorld();
        var query = new CompiledFilteredTargetQuery(EffectRelativeTeam.Allies, EffectEntityReference.Owner,
            false, false, "support", 3, 2, EffectTargetOrder.LowestHealthRatio);
        var targets = EffectTargetResolver.Resolve(query, world.Snapshot, "owner", "owner", "");
        Expect(targets.SequenceEqual(["a", "b"]), "selector failed team/range/tag/health/tie/count filtering");
        var formula = new CompiledCompositeMagnitude(AttributeMagnitudeOperation.Add,
            [new CompiledSourceAttributeMagnitude(CombatAttribute.AttackDamage, AttributeCaptureMode.Snapshot),
                new CompiledCompositeMagnitude(AttributeMagnitudeOperation.Multiply,
                    [new CompiledTargetAttributeMagnitude(CombatAttribute.MaxHealth, AttributeCaptureMode.Snapshot), new CompiledConstantMagnitude(.1f)], AttributeCaptureMode.Snapshot)],
            AttributeCaptureMode.Snapshot);
        var binding = new CompiledEffectBinding("fixture_effect", 0, new CompiledEffectTrigger(EffectTriggerKind.Manual, EffectDomainEventKind.None),
            [new CompiledHealthRatioCondition(EffectEntityReference.Owner, EffectComparison.Greater, .5f),
                new CompiledEntityTagCondition(EffectEntityReference.Owner, "leader", true)],
            query, [new CompiledEffectStep(EffectKind.Heal, EffectAmountSource.Fixed, 1, formula)],
            new CompiledEffectBindingLimits(1, 0, 0, 0), null);
        var origin = new CombatSourceRef(CombatSourceKind.Ability, "fixture_ability", "owner", "fixture_instance");
        using var scope = new BattleEffectScope("fixture_scope", world);
        var preflight = scope.PreflightImmediate(binding, "owner", "owner", "", 0, origin: origin);
        Expect(preflight.Succeeded && world.Commits.Count == 0, "preflight mutated world or rejected valid composition");
        var result = scope.ExecuteImmediate(binding, "owner", "owner", "", 0, origin: origin);
        Expect(result.Status == EffectExecutionStatus.Succeeded && world.Commits.Count == 2, "composed effect did not execute selected recipients");
        Expect(world.Commits.All(mutation => mutation.Request.Context.Origin == origin), "effect origin lost");
        Expect(world.Commits.All(mutation => Math.Abs(mutation.Request.RequestedAmount - 40) < .001f), "effect did not read shared source/target attributes");
        scope.ExecuteImmediate(binding, "owner", "owner", "", 1, origin: origin);
        Expect(world.Commits.Count == 2, "preflight or repeated activation corrupted use budget");
        scope.ExecuteImmediate(binding, "owner", "owner", "", 1, origin: origin with { InstanceId = "independent_grant" });
        Expect(world.Commits.Count == 4, "independent effect origin inherited another grant's usage limit");
        Expect(EffectModelText.BindingFingerprint(binding) != EffectModelText.BindingFingerprint(binding with
            { Effects = [new CompiledEffectStep(EffectKind.Heal, EffectAmountSource.Fixed, 1, new CompiledConstantMagnitude(2))] }),
            "effect identity ignored formula");
    }

    private sealed class SnapshotWorld : IEffectRuntimeWorld
    {
        public readonly List<PreparedEffectMutation> Commits = [];
        public EffectWorldSnapshot Snapshot { get; } = EffectWorldSnapshot.Create(0,
        [
            Entity("owner", 0, 100, new Vector2(0, 0), "leader"),
            Entity("b", 0, 20, new Vector2(2, 0), "support"),
            Entity("a", 0, 20, new Vector2(1, 0), "support"),
            Entity("far", 0, 1, new Vector2(9, 0), "support"),
            Entity("enemy", 1, 1, new Vector2(1, 0), "support"),
            Entity("other", 0, 1, new Vector2(1, 0), "other")
        ]);
        private static EffectEntitySnapshot Entity(string id, int team, float health, Vector2 position, string tag) =>
            new(id, team, true, health, 100, 0, [tag], new Dictionary<CombatAttribute, float>
                { [CombatAttribute.AttackDamage] = 30, [CombatAttribute.MaxHealth] = 100 }.ToImmutableDictionary(), position);
        public EffectWorldSnapshot CaptureSnapshot(int tick) => Snapshot with { Tick = tick };
        public EffectModifierResult ResolveModifiers(EffectModifierRequest request, EffectWorldSnapshot snapshot) => EffectModifierResult.Identity(request.RequestedAmount);
        public EffectCommitOutcome Commit(PreparedEffectMutation mutation)
        {
            Commits.Add(mutation);
            return EffectCommitOutcome.Succeeded(mutation.Modifiers.ResolvedAmount, mutation.Modifiers.ResolvedAmount, EffectDomainEventKind.HealingResolved);
        }
    }
    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
