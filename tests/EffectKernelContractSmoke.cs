using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Effects;

public partial class EffectKernelContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private static Task<int> RunAsync()
    {
        try
        {
            var authored = CompileAuthoredBindings();
            ReactiveChainUsesDeterministicWaves(authored);
            ReverseEnqueuePreservesCommitOrder(authored.Damage);
            ModifierAndConditionContracts(authored.Damage);
            ReentrantExecutionIsRejected(authored.Damage);
            ExecutionBudgetsAreTyped(authored.Damage);
            CompilerRejectsCyclesAndMissingProcessors();
            EveryCompletionPathCleansScope(authored);
            ProcessorFailureIsExplainable(authored.Damage);
            GD.Print("EFFECT_KERNEL_CONTRACT_OK authored=resource compiled=immutable chain=damage-heal-shield " +
                     "ordering=stable snapshots=wave reentry=rejected budgets=invocation-step-event-depth-rate-usage-edge " +
                     "lifecycle=victory-defeat-timeout-abort-replacement-exception-disposal cleanup=zero trace=causal");
            return Task.FromResult(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr("EFFECT_KERNEL_CONTRACT_FAILED: " + exception);
            return Task.FromResult(1);
        }
    }

    private static AuthoredBindings CompileAuthoredBindings()
    {
        var damageResource = LoadBinding("res://tests/fixtures/effects/phase1_damage_binding.tres");
        var healResource = LoadBinding("res://tests/fixtures/effects/phase1_damage_to_heal_binding.tres");
        var shieldResource = LoadBinding("res://tests/fixtures/effects/phase1_heal_to_shield_binding.tres");
        var before = Fingerprint(damageResource, healResource, shieldResource);
        var batch = EffectBindingCompiler.CompileBatch([damageResource, healResource, shieldResource]);
        Expect(!batch.Report.HasCoreErrors && batch.Bindings.Length == 3,
            "authored effect binding batch did not compile: " + string.Join(" | ", batch.Report.CoreErrors));
        var result = new AuthoredBindings(
            batch.Bindings.Single(binding => binding.StableId == "phase1_damage"),
            batch.Bindings.Single(binding => binding.StableId == "phase1_damage_to_heal"),
            batch.Bindings.Single(binding => binding.StableId == "phase1_heal_to_shield"),
            before,
            () => Fingerprint(damageResource, healResource, shieldResource));
        Expect(result.Damage.Effects[0].Kind == EffectKind.Damage &&
               result.Heal.Effects[0].Kind == EffectKind.Heal &&
               result.Shield.Effects[0].Kind == EffectKind.Shield,
            "typed authored effects compiled to the wrong processors");
        return result;
    }

    private static void ReactiveChainUsesDeterministicWaves(AuthoredBindings authored)
    {
        var world = TestWorld.CreateDefault();
        using var scope = new BattleEffectScope("chain_scope", world);
        using var heal = scope.ActivateReactiveBinding(authored.Heal, "source", "source");
        using var shield = scope.ActivateReactiveBinding(authored.Shield, "source", "source");
        var drain = scope.ExecuteImmediate(authored.Damage, "source", "source", "target", 5);
        Expect(drain.Status == EffectExecutionStatus.Succeeded && drain.Invocations.Length == 3,
            "damage-heal-shield chain did not resolve three invocations");
        Expect(world.SnapshotCount == 3, "reactive chain did not capture exactly one snapshot per wave");
        Expect(world.CommitLog.SequenceEqual(new[]
        {
            "phase1_damage:0:target:Damage",
            "phase1_damage_to_heal:0:target:Heal",
            "phase1_heal_to_shield:0:target:Shield"
        }), "reactive chain commit order changed");
        var target = world.Entities["target"];
        Expect(Near(target.Health, 75) && Near(target.Shield, 5),
            $"reactive chain outcome mismatch: hp={target.Health} shield={target.Shield}");
        var contexts = drain.Invocations.Select(result => result.Context).ToArray();
        Expect(contexts.Select(context => context.ChainId).Distinct(StringComparer.Ordinal).Count() == 1,
            "reactive chain attribution lost chain id");
        Expect(contexts.Select(context => context.Depth).SequenceEqual(new[] { 0, 1, 2 }),
            "reactive chain depth attribution changed");
        Expect(contexts.All(context => context.SourceId == "source" && context.OwnerId == "source") &&
               contexts.Select(context => context.InvocationSequence).SequenceEqual(
                   contexts.Select(context => context.InvocationSequence).OrderBy(value => value)),
            "reactive source/owner/sequence attribution changed");
        Expect(scope.Events.Count == 3 && scope.Trace.Count == 3 &&
               scope.Trace.All(entry => entry.Status == EffectExecutionStatus.Succeeded),
            "successful chain lacks causal event/trace evidence");
        Expect(authored.BeforeFingerprint == authored.CurrentFingerprint(),
            "runtime execution mutated shared authored resources");
    }

    private static void ReverseEnqueuePreservesCommitOrder(CompiledEffectBinding template)
    {
        var first = template with { StableId = "order_a", Priority = 5, Effects = [new CompiledEffectStep(EffectKind.Damage, EffectAmountSource.Fixed, 1)] };
        var second = template with { StableId = "order_b", Priority = 5, Effects = [new CompiledEffectStep(EffectKind.Damage, EffectAmountSource.Fixed, 1)] };
        var forward = RunOrdered([first, second]);
        var reverse = RunOrdered([second, first]);
        Expect(forward.SequenceEqual(reverse) && forward.SequenceEqual(new[]
        {
            "order_a:0:target:Damage",
            "order_b:0:target:Damage"
        }), "commit order depends on enqueue order instead of the stable ordering key");
    }

    private static IReadOnlyList<string> RunOrdered(IReadOnlyList<CompiledEffectBinding> bindings)
    {
        var world = TestWorld.CreateDefault();
        using var scope = new BattleEffectScope("order_scope", world);
        foreach (var binding in bindings)
            Expect(scope.EnqueueRoot(binding, "source", "source", "target", 3).Accepted,
                "ordered root was rejected");
        var drain = scope.Drain();
        Expect(drain.Status == EffectExecutionStatus.Succeeded, "ordered roots did not resolve");
        return world.CommitLog.ToArray();
    }

    private static void ModifierAndConditionContracts(CompiledEffectBinding template)
    {
        var modified = template with
        {
            StableId = "modifier_probe",
            Effects = [new CompiledEffectStep(EffectKind.Damage, EffectAmountSource.Fixed, 4)]
        };
        var world = TestWorld.CreateDefault();
        world.ModifierDelta = 2;
        using (var scope = new BattleEffectScope("modifier_scope", world))
        {
            var result = scope.ExecuteImmediate(modified, "source", "source", "target", 1);
            Expect(result.Status == EffectExecutionStatus.Succeeded && Near(world.Entities["target"].Health, 74),
                "modifier request/result did not feed authoritative commit");
            Expect(world.ModifierRequests == 1, "modifier pipeline was bypassed");
        }

        var conditioned = template with
        {
            StableId = "condition_probe",
            Conditions = [new CompiledEntityAliveCondition(EffectEntityReference.ExplicitTarget, false)]
        };
        var conditionWorld = TestWorld.CreateDefault();
        using var conditionScope = new BattleEffectScope("condition_scope", conditionWorld);
        var skipped = conditionScope.ExecuteImmediate(conditioned, "source", "source", "target", 1);
        Expect(skipped.Status == EffectExecutionStatus.Skipped &&
               skipped.Interruption == EffectInterruptionReason.ConditionFailed &&
               conditionWorld.CommitLog.Count == 0,
            "read-only condition did not skip before mutation");
    }

    private static void ReentrantExecutionIsRejected(CompiledEffectBinding binding)
    {
        var world = TestWorld.CreateDefault();
        BattleEffectScope? scope = null;
        EffectQueueDrainResult? nested = null;
        world.OnFirstCommit = () => nested = scope!.ExecuteImmediate(binding, "source", "source", "target", 2);
        using (scope = new BattleEffectScope("reentry_scope", world))
        {
            var outer = scope.ExecuteImmediate(binding, "source", "source", "target", 2);
            Expect(outer.Status == EffectExecutionStatus.Succeeded && world.CommitLog.Count == 1,
                "outer invocation failed during reentry probe");
            Expect(nested?.Status == EffectExecutionStatus.Interrupted &&
                   nested.Interruption == EffectInterruptionReason.ReentrantExecution,
                "inline reentrant mutation was not rejected with typed evidence");
        }
    }

    private static void ExecutionBudgetsAreTyped(CompiledEffectBinding template)
    {
        InvocationBudget(template);
        StepBudget(template);
        EventBudget(template);
        DepthBudget(template);
        RateAndUsageBudgets(template);
        RepeatedEdgeBudget(template);
    }

    private static void InvocationBudget(CompiledEffectBinding template)
    {
        var world = TestWorld.CreateDefault();
        using var scope = new BattleEffectScope("invocation_budget", world, limits: new EffectExecutionLimits(MaxInvocationsPerDrain: 1));
        scope.EnqueueRoot(template with { StableId = "invocation_a" }, "source", "source", "target", 1);
        scope.EnqueueRoot(template with { StableId = "invocation_b" }, "source", "source", "target", 1);
        var result = scope.Drain();
        Expect(result.Status == EffectExecutionStatus.Interrupted &&
               result.Invocations.Any(invocation => invocation.Interruption == EffectInterruptionReason.InvocationBudget),
            "invocation budget did not interrupt excess work");
    }

    private static void StepBudget(CompiledEffectBinding template)
    {
        var binding = template with
        {
            StableId = "step_budget",
            Effects =
            [
                new CompiledEffectStep(EffectKind.Damage, EffectAmountSource.Fixed, 1),
                new CompiledEffectStep(EffectKind.Heal, EffectAmountSource.Fixed, 1)
            ]
        };
        var world = TestWorld.CreateDefault();
        using var scope = new BattleEffectScope("step_budget", world, limits: new EffectExecutionLimits(MaxStepsPerDrain: 1));
        var result = scope.ExecuteImmediate(binding, "source", "source", "target", 1);
        Expect(result.Status == EffectExecutionStatus.Interrupted &&
               result.Invocations.SelectMany(invocation => invocation.Steps)
                   .Any(step => step.Interruption == EffectInterruptionReason.StepBudget),
            "step budget did not produce typed interruption");
    }

    private static void EventBudget(CompiledEffectBinding template)
    {
        var binding = template with
        {
            StableId = "event_budget",
            Effects =
            [
                new CompiledEffectStep(EffectKind.Damage, EffectAmountSource.Fixed, 1),
                new CompiledEffectStep(EffectKind.Heal, EffectAmountSource.Fixed, 1)
            ]
        };
        var world = TestWorld.CreateDefault();
        using var scope = new BattleEffectScope("event_budget", world, limits: new EffectExecutionLimits(MaxEventsPerDrain: 1));
        var result = scope.ExecuteImmediate(binding, "source", "source", "target", 1);
        Expect(result.Status == EffectExecutionStatus.Interrupted && scope.Events.Count == 1 &&
               result.Invocations.SelectMany(invocation => invocation.Steps)
                   .Any(step => step.Interruption == EffectInterruptionReason.EventBudget),
            "event budget did not stop mutation before an unreportable event");
    }

    private static void DepthBudget(CompiledEffectBinding template)
    {
        var root = template with { StableId = "depth_root", Effects = [new CompiledEffectStep(EffectKind.Damage, EffectAmountSource.Fixed, 1)] };
        var reactive = root with
        {
            StableId = "depth_reactive",
            Trigger = new CompiledEffectTrigger(EffectTriggerKind.DomainEvent, EffectDomainEventKind.DamageResolved),
            Limits = root.Limits with { MaxDepth = 1, MaxRepeatedEdges = 8 }
        };
        var world = TestWorld.CreateDefault();
        using var scope = new BattleEffectScope("depth_budget", world);
        using var handle = scope.ActivateReactiveBinding(reactive, "source", "source");
        var result = scope.ExecuteImmediate(root, "source", "source", "target", 1);
        Expect(result.Status == EffectExecutionStatus.Interrupted && world.CommitLog.Count == 2 &&
               result.Invocations.Any(invocation => invocation.Interruption == EffectInterruptionReason.DepthLimit),
            "reactive depth budget did not stop the next wave");
    }

    private static void RateAndUsageBudgets(CompiledEffectBinding template)
    {
        var rate = template with
        {
            StableId = "rate_budget",
            Limits = template.Limits with { MinimumIntervalTicks = 2 }
        };
        var rateWorld = TestWorld.CreateDefault();
        using (var scope = new BattleEffectScope("rate_budget", rateWorld))
        {
            scope.EnqueueRoot(rate, "source", "source", "target", 1);
            scope.EnqueueRoot(rate, "source", "source", "target", 1);
            var result = scope.Drain();
            Expect(result.Invocations.Any(invocation => invocation.Interruption == EffectInterruptionReason.RateLimited),
                "binding minimum interval did not rate-limit simultaneous use");
        }

        var usage = template with
        {
            StableId = "usage_budget",
            Limits = template.Limits with { MaxUses = 1 }
        };
        var usageWorld = TestWorld.CreateDefault();
        using var usageScope = new BattleEffectScope("usage_budget", usageWorld);
        usageScope.EnqueueRoot(usage, "source", "source", "target", 1);
        usageScope.EnqueueRoot(usage, "source", "source", "target", 2);
        var usageResult = usageScope.Drain();
        Expect(usageResult.Invocations.Any(invocation => invocation.Interruption == EffectInterruptionReason.UsageLimit),
            "binding usage limit did not interrupt the second use");
    }

    private static void RepeatedEdgeBudget(CompiledEffectBinding template)
    {
        var root = template with { StableId = "edge_root", Effects = [new CompiledEffectStep(EffectKind.Damage, EffectAmountSource.Fixed, 1)] };
        var reactive = root with
        {
            StableId = "edge_reactive",
            Trigger = new CompiledEffectTrigger(EffectTriggerKind.DomainEvent, EffectDomainEventKind.DamageResolved),
            Limits = root.Limits with { MaxDepth = 10, MaxRepeatedEdges = 1 }
        };
        var world = TestWorld.CreateDefault();
        using var scope = new BattleEffectScope("edge_budget", world);
        using var handle = scope.ActivateReactiveBinding(reactive, "source", "source");
        var result = scope.ExecuteImmediate(root, "source", "source", "target", 1);
        Expect(result.Status == EffectExecutionStatus.Interrupted && world.CommitLog.Count == 3 &&
               result.Invocations.Any(invocation => invocation.Interruption == EffectInterruptionReason.RepeatedEdge),
            "repeated reactive edge was not bounded independently from depth");
    }

    private static void CompilerRejectsCyclesAndMissingProcessors()
    {
        var damageToHeal = AuthoredBinding(
            "cycle_damage_to_heal",
            EffectDomainEventKind.DamageResolved,
            new HealEffectSpec { AmountSource = EffectAmountSource.EventEffectiveValue, Amount = 1 });
        var healToDamage = AuthoredBinding(
            "cycle_heal_to_damage",
            EffectDomainEventKind.HealingResolved,
            new DamageEffectSpec { AmountSource = EffectAmountSource.EventEffectiveValue, Amount = 1 });
        var cycle = EffectBindingCompiler.CompileBatch([damageToHeal, healToDamage]);
        Expect(cycle.Report.HasCoreErrors && cycle.Report.CoreErrors.Any(error => error.Contains("dependency cycle", StringComparison.Ordinal)),
            "batch compiler accepted a reactive dependency cycle");

        var duplicate = EffectBindingCompiler.CompileBatch([damageToHeal, damageToHeal]);
        Expect(duplicate.Report.HasCoreErrors && duplicate.Report.CoreErrors.Any(error => error.Contains("Duplicate", StringComparison.Ordinal)),
            "batch compiler accepted a stable-id collision");

        var damageOnly = new EffectProcessorRegistry().Register(new DamageEffectProcessor());
        var missing = EffectBindingCompiler.Compile(damageToHeal, damageOnly);
        Expect(missing.Report.HasCoreErrors && missing.Report.CoreErrors.Any(error => error.Contains("no processor", StringComparison.Ordinal)),
            "compiler accepted an authored effect without a typed processor");
    }

    private static void EveryCompletionPathCleansScope(AuthoredBindings authored)
    {
        var reasons = new[]
        {
            BattleScopeCompletionReason.PlayerVictory,
            BattleScopeCompletionReason.PlayerDefeat,
            BattleScopeCompletionReason.Timeout,
            BattleScopeCompletionReason.Abort,
            BattleScopeCompletionReason.Replacement,
            BattleScopeCompletionReason.Exception
        };
        foreach (var reason in reasons)
        {
            var world = TestWorld.CreateDefault();
            using var scope = new BattleEffectScope("completion_" + reason, world);
            var handle = scope.ActivateReactiveBinding(authored.Heal, "source", "source");
            handle.Dispose();
            handle.Dispose();
            var liveHandle = scope.ActivateReactiveBinding(authored.Heal, "source", "source");
            scope.EnqueueRoot(authored.Damage, "source", "source", "target", 9);
            var first = scope.Complete(reason, 9);
            var second = scope.Complete(BattleScopeCompletionReason.Disposal, 99);
            Expect(ReferenceEquals(first, second), $"{reason}: completion was not idempotent");
            Expect(first.Validate().IsValid && first.RemainingSubscriptions == 0 &&
                   first.RemainingInvocations == 0 && first.RemainingRuntimeInstances == 0 &&
                   scope.SubscriptionCount == 0 && scope.PendingInvocationCount == 0 &&
                   scope.LiveRuntimeInstanceCount == 0,
                $"{reason}: scope cleanup retained owned state");
            Expect(first.Trace.Any(entry => entry.Interruption == EffectInterruptionReason.QueueAborted),
                $"{reason}: pending work lacks interruption trace");
            liveHandle.Dispose();
        }

        var disposalWorld = TestWorld.CreateDefault();
        var disposalScope = new BattleEffectScope("completion_disposal", disposalWorld);
        disposalScope.ActivateReactiveBinding(authored.Heal, "source", "source");
        disposalScope.EnqueueRoot(authored.Damage, "source", "source", "target", 4);
        disposalScope.Dispose();
        Expect(disposalScope.Transition?.Reason == BattleScopeCompletionReason.Disposal &&
               disposalScope.Transition.Validate().IsValid,
            "Dispose did not produce a valid zero-owned-state transition");
    }

    private static void ProcessorFailureIsExplainable(CompiledEffectBinding binding)
    {
        var world = TestWorld.CreateDefault();
        world.ThrowOnCommit = true;
        using var scope = new BattleEffectScope("exception_scope", world);
        var result = scope.ExecuteImmediate(binding, "source", "source", "target", 1);
        Expect(result.Status == EffectExecutionStatus.Failed &&
               result.Interruption == EffectInterruptionReason.ProcessorFailure &&
               scope.Trace.Any(entry => entry.Status == EffectExecutionStatus.Failed &&
                                        entry.Interruption == EffectInterruptionReason.ProcessorFailure),
            "processor exception did not become typed causal evidence");
        var transition = scope.Complete(BattleScopeCompletionReason.Exception, 1);
        Expect(transition.Validate().IsValid, "exception completion leaked scope state");
    }

    private static EffectBindingSpec AuthoredBinding(
        string stableId,
        EffectDomainEventKind triggerEvent,
        EffectStepSpec effect) => new()
    {
        StableId = stableId,
        Trigger = new EffectTriggerSpec { Kind = EffectTriggerKind.DomainEvent, EventKind = triggerEvent },
        Conditions = [],
        TargetQuery = new ExplicitTargetQuerySpec(),
        Effects = [effect],
        Limits = new EffectBindingLimitsSpec { MaxDepth = 8, MaxRepeatedEdges = 4 }
    };

    private static EffectBindingSpec LoadBinding(string path) =>
        GD.Load<EffectBindingSpec>(path) ?? throw new InvalidOperationException($"authored effect binding fixture load: {path}");

    private static string Fingerprint(params EffectBindingSpec[] bindings) => string.Join("|", bindings.Select(binding =>
        $"{binding.StableId}:{binding.Priority}:{binding.Trigger.Kind}:{binding.Trigger.EventKind}:" +
        $"{binding.Conditions.Count}:{binding.TargetQuery.GetType().Name}:" +
        string.Join(",", binding.Effects.Select(effect => $"{effect.GetType().Name}:{effect.AmountSource}:{effect.Amount:R}")) +
        $":{binding.Limits.MaxUses}:{binding.Limits.MinimumIntervalTicks}:{binding.Limits.MaxDepth}:{binding.Limits.MaxRepeatedEdges}"));

    private static bool Near(float actual, float expected) => Math.Abs(actual - expected) < 0.001f;

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed record AuthoredBindings(
        CompiledEffectBinding Damage,
        CompiledEffectBinding Heal,
        CompiledEffectBinding Shield,
        string BeforeFingerprint,
        Func<string> CurrentFingerprint);

    private sealed class TestWorld : IEffectRuntimeWorld
    {
        public Dictionary<string, MutableEntity> Entities { get; } = new(StringComparer.Ordinal);
        public List<string> CommitLog { get; } = [];
        public int SnapshotCount { get; private set; }
        public int ModifierRequests { get; private set; }
        public float ModifierDelta { get; set; }
        public Action? OnFirstCommit { get; set; }
        public bool ThrowOnCommit { get; set; }
        private bool _commitCallbackUsed;

        public static TestWorld CreateDefault()
        {
            var world = new TestWorld();
            world.Entities.Add("source", new MutableEntity("source", 0, 100, 100, 0));
            world.Entities.Add("target", new MutableEntity("target", 1, 80, 100, 0));
            return world;
        }

        public EffectWorldSnapshot CaptureSnapshot(int tick)
        {
            SnapshotCount++;
            return EffectWorldSnapshot.Create(tick, Entities.Values.Select(entity => new EffectEntitySnapshot(
                entity.RuntimeId,
                entity.Team,
                entity.Health > 0,
                entity.Health,
                entity.MaxHealth,
                entity.Shield)));
        }

        public EffectModifierResult ResolveModifiers(EffectModifierRequest request, EffectWorldSnapshot snapshot)
        {
            ModifierRequests++;
            var resolved = request.RequestedAmount + ModifierDelta;
            return new EffectModifierResult(
                request.RequestedAmount,
                resolved,
                ModifierDelta == 0
                    ? []
                    : [new EffectModifierContribution("test_modifier", request.RequestedAmount, resolved)]);
        }

        public EffectCommitOutcome Commit(PreparedEffectMutation mutation)
        {
            if (ThrowOnCommit) throw new InvalidOperationException("intentional commit failure");
            if (!_commitCallbackUsed)
            {
                _commitCallbackUsed = true;
                OnFirstCommit?.Invoke();
            }
            if (!Entities.TryGetValue(mutation.Request.TargetId, out var target))
                return EffectCommitOutcome.Skipped(EffectInterruptionReason.TargetUnavailable, "missing target");
            var amount = mutation.Modifiers.ResolvedAmount;
            float effective;
            switch (mutation.Request.Kind)
            {
                case EffectKind.Damage:
                    var absorbed = Math.Min(target.Shield, amount);
                    target.Shield -= absorbed;
                    var healthDamage = Math.Min(target.Health, amount - absorbed);
                    target.Health -= healthDamage;
                    effective = absorbed + healthDamage;
                    break;
                case EffectKind.Heal:
                    if (target.Health <= 0)
                        return EffectCommitOutcome.Skipped(EffectInterruptionReason.TargetUnavailable, "defeated target");
                    effective = Math.Min(target.MaxHealth - target.Health, amount);
                    target.Health += effective;
                    break;
                case EffectKind.Shield:
                    effective = amount;
                    target.Shield += amount;
                    break;
                default:
                    return EffectCommitOutcome.Failed("unsupported test effect");
            }
            CommitLog.Add($"{mutation.Request.BindingId}:{mutation.Request.StepIndex}:{mutation.Request.TargetId}:{mutation.Request.Kind}");
            return EffectCommitOutcome.Succeeded(
                amount,
                effective,
                mutation.Request.Kind switch
                {
                    EffectKind.Damage => EffectDomainEventKind.DamageResolved,
                    EffectKind.Heal => EffectDomainEventKind.HealingResolved,
                    EffectKind.Shield => EffectDomainEventKind.ShieldResolved,
                    _ => EffectDomainEventKind.None
                });
        }
    }

    private sealed class MutableEntity(
        string runtimeId,
        int team,
        float health,
        float maxHealth,
        float shield)
    {
        public string RuntimeId { get; } = runtimeId;
        public int Team { get; } = team;
        public float Health { get; set; } = health;
        public float MaxHealth { get; } = maxHealth;
        public float Shield { get; set; } = shield;
    }
}
