using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Abilities;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.TacticalCommands;

public static class TacticalCommandBattlePreparationBuilder
{
    public static TacticalCommandBattlePreparation Build(
        ActiveRunDto run,
        CompiledContentGraph graph)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(graph);
        if (run.EquippedTacticalCommandIds is null ||
            run.EquippedTacticalCommandIds.Count != ActiveRunTacticalCommandPolicy.SlotCount)
            throw new ArgumentException("Run must equip exactly two tactical commands.", nameof(run));
        var commands = run.EquippedTacticalCommandIds
            .Select(graph.ResolveTacticalCommand)
            .ToImmutableArray();
        return new TacticalCommandBattlePreparation(Fingerprint(commands), commands);
    }

    internal static string Fingerprint(IEnumerable<CompiledTacticalCommandDefinition> commands)
    {
        var canonical = string.Join("|", commands.Select(command =>
            $"{command.StableId}:{command.Fingerprint}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}

public sealed class BattleTacticalCommandScope : IDisposable
{
    public const int MaximumTacticalPoints = 3;

    private readonly IAbilityRuntimeWorld _world;
    private readonly List<RuntimeInstance> _instances = [];
    private readonly string _sourceFingerprint;
    private TacticalCommandScopeTransitionResult? _transition;
    private int _lastTick;

    public BattleTacticalCommandScope(
        string scopeId,
        IAbilityRuntimeWorld world,
        TacticalCommandBattlePreparation preparation)
    {
        if (string.IsNullOrWhiteSpace(scopeId))
            throw new ArgumentException("Tactical-command scope id is required.", nameof(scopeId));
        _world = world ?? throw new ArgumentNullException(nameof(world));
        ArgumentNullException.ThrowIfNull(preparation);
        if (preparation.Commands.Length != ActiveRunTacticalCommandPolicy.SlotCount ||
            preparation.Commands.Any(command => command is null) ||
            preparation.Commands.Select(command => command.StableId).Distinct(StringComparer.Ordinal).Count() !=
            ActiveRunTacticalCommandPolicy.SlotCount ||
            !string.Equals(
                preparation.SourceFingerprint,
                TacticalCommandBattlePreparationBuilder.Fingerprint(preparation.Commands),
                StringComparison.Ordinal))
            throw new ArgumentException(
                "Battle tactical-command preparation must contain exactly two unique canonical commands.",
                nameof(preparation));

        ScopeId = scopeId;
        _sourceFingerprint = preparation.SourceFingerprint;
        for (var index = 0; index < preparation.Commands.Length; index++)
            _instances.Add(new RuntimeInstance(index, preparation.Commands[index]));
        TacticalPoints = MaximumTacticalPoints;
    }

    public string ScopeId { get; }
    public int TacticalPoints { get; private set; }
    public int LiveRuntimeInstanceCount => _instances.Count;
    public bool IsCompleted => _transition is not null;
    public TacticalCommandScopeTransitionResult? Transition => _transition;

    public BattleTacticalCommandSnapshot Snapshot(int tick)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        return new BattleTacticalCommandSnapshot(
            TacticalPoints,
            MaximumTacticalPoints,
            _instances.OrderBy(instance => instance.SlotIndex)
                .Select(instance => new TacticalCommandSlotSnapshot(
                    instance.SlotIndex,
                    instance.Definition.StableId,
                    instance.Definition.DisplayName,
                    instance.Definition.Description,
                    instance.Definition.TacticalPointCost,
                    instance.Definition.GoldCost,
                    instance.Definition.CooldownTicks,
                    Math.Max(0, instance.ReadyTick - tick),
                    instance.Definition.MaxUses,
                    instance.Uses,
                    !IsCompleted && TacticalPoints >= instance.Definition.TacticalPointCost &&
                    instance.ReadyTick <= tick &&
                    (instance.Definition.MaxUses == 0 || instance.Uses < instance.Definition.MaxUses)))
                .ToImmutableArray());
    }

    public TacticalCommandActivationResult TryActivate(
        int slotIndex,
        string ownerRuntimeId,
        int tick,
        string explicitTargetId = "")
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        _lastTick = Math.Max(_lastTick, tick);
        if (_transition is not null)
            return Failed(slotIndex, string.Empty, TacticalCommandActivationFailure.ScopeCompleted,
                "战斗已经结束。");
        if (slotIndex < 0 || slotIndex >= _instances.Count)
            return Failed(slotIndex, string.Empty, TacticalCommandActivationFailure.InvalidSlot,
                "战术指令槽位无效。");

        var instance = _instances[slotIndex];
        var command = instance.Definition;
        if (TacticalPoints < command.TacticalPointCost)
            return Failed(slotIndex, command.StableId,
                TacticalCommandActivationFailure.InsufficientTacticalPoints, "战术点不足。");
        if (instance.ReadyTick > tick)
            return Failed(slotIndex, command.StableId,
                TacticalCommandActivationFailure.Cooldown, "战术指令仍在冷却中。");
        if (command.MaxUses > 0 && instance.Uses >= command.MaxUses)
            return Failed(slotIndex, command.StableId,
                TacticalCommandActivationFailure.UsageLimit, "战术指令使用次数已经耗尽。");
        if (string.IsNullOrWhiteSpace(ownerRuntimeId))
            return Failed(slotIndex, command.StableId,
                TacticalCommandActivationFailure.SourceUnavailable, "当前没有可用的我方单位。");

        AbilityPreparationResult prepared;
        try
        {
            prepared = _world.Prepare(
                command.Ability,
                ownerRuntimeId,
                ownerRuntimeId,
                explicitTargetId,
                tick);
        }
        catch (Exception exception)
        {
            return Failed(slotIndex, command.StableId,
                TacticalCommandActivationFailure.PreflightFailed, exception.Message);
        }
        if (!prepared.Succeeded || prepared.Plan is null)
            return Failed(
                slotIndex,
                command.StableId,
                MapFailure(prepared.Failure, commit: false),
                string.IsNullOrWhiteSpace(prepared.FailureReason)
                    ? "当前没有合法的战术指令目标。"
                    : prepared.FailureReason);

        AbilityCommitResult committed;
        try
        {
            committed = _world.Commit(prepared.Plan);
        }
        catch (Exception exception)
        {
            return Failed(slotIndex, command.StableId,
                TacticalCommandActivationFailure.CommitFailed, exception.Message);
        }
        if (!committed.Succeeded)
            return Failed(
                slotIndex,
                command.StableId,
                MapFailure(committed.Failure, commit: true),
                string.IsNullOrWhiteSpace(committed.FailureReason)
                    ? "战术指令提交失败。"
                    : committed.FailureReason);

        // The world commit is authoritative and transactional. Scope resources
        // move only after it succeeds, so every rejected activation is zero-cost.
        TacticalPoints -= command.TacticalPointCost;
        instance.Uses++;
        instance.ReadyTick = tick + command.CooldownTicks;
        return new TacticalCommandActivationResult(
            true,
            TacticalCommandActivationFailure.None,
            string.Empty,
            slotIndex,
            command.StableId,
            command.TacticalPointCost,
            prepared.Plan.GoldCost,
            committed.ResolvedFacts);
    }

    public TacticalCommandScopeTransitionResult Complete(
        TacticalCommandScopeCompletionReason reason,
        int finalTick)
    {
        if (_transition is not null) return _transition;
        if (reason == TacticalCommandScopeCompletionReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason));
        if (finalTick < 0) throw new ArgumentOutOfRangeException(nameof(finalTick));
        _lastTick = Math.Max(_lastTick, finalTick);
        TacticalPoints = 0;
        _instances.Clear();
        _transition = new TacticalCommandScopeTransitionResult(
            ScopeId,
            _sourceFingerprint,
            reason,
            _lastTick,
            TacticalPoints,
            LiveRuntimeInstanceCount);
        return _transition;
    }

    public void Dispose()
    {
        if (_transition is null)
            Complete(TacticalCommandScopeCompletionReason.Disposal, _lastTick);
    }

    private static TacticalCommandActivationFailure MapFailure(
        AbilityActivationFailure failure,
        bool commit) => failure switch
    {
        AbilityActivationFailure.SourceUnavailable => TacticalCommandActivationFailure.SourceUnavailable,
        AbilityActivationFailure.InsufficientGold => TacticalCommandActivationFailure.InsufficientGold,
        AbilityActivationFailure.Cooldown => TacticalCommandActivationFailure.Cooldown,
        AbilityActivationFailure.UsageLimit => TacticalCommandActivationFailure.UsageLimit,
        AbilityActivationFailure.CommitFailed => TacticalCommandActivationFailure.CommitFailed,
        _ => commit
            ? TacticalCommandActivationFailure.CommitFailed
            : TacticalCommandActivationFailure.PreflightFailed
    };

    private static TacticalCommandActivationResult Failed(
        int slotIndex,
        string commandId,
        TacticalCommandActivationFailure failure,
        string reason) => new(false, failure, reason, slotIndex, commandId, 0, 0, []);

    private sealed class RuntimeInstance(
        int slotIndex,
        CompiledTacticalCommandDefinition definition)
    {
        public int SlotIndex { get; } = slotIndex;
        public CompiledTacticalCommandDefinition Definition { get; } = definition;
        public int Uses { get; set; }
        public int ReadyTick { get; set; }
    }
}
