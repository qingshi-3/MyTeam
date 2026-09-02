using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Components;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

public sealed class BattleSimulation : IDisposable, IAbilityRuntimeWorld
{
    public const int Width = BattlefieldLayout.Width;
    public const int Height = BattlefieldLayout.Height;
    public const int MaxTicks = 1800;

    private readonly BattleConfig _config;
    private readonly DeterministicRandom _random;
    private readonly List<BattleUnitState> _units = [];
    private readonly List<BattleEvent> _events = [];
    private readonly StringBuilder _digest = new();
    private readonly HashSet<string> _deathProcUnits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BattleUnitStatistics> _statistics = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _bossPhaseIndexes = new(StringComparer.Ordinal);
    private readonly BattleEffectCompatibilityAdapter _effectCompatibility;
    private readonly BattleAttributeScope _attributeScope;
    private readonly BattleCombatEventPipeline _combatPipeline;
    private readonly BattleStatusScope _statusScope;
    private readonly List<StatusPresentationCue> _statusPresentationCues = [];
    private readonly IReadOnlyList<StatusPresentationCue> _statusPresentationCueView;
    private readonly RelicBattleScope? _relicScope;
    private ImmutableArray<BattleUnitReportSnapshot> _terminalUnitReports;
    private EquipmentBattleScope? _equipmentScope;
    private BattleTraitScope? _traitScope;
    private BattleAbilityScope? _abilityScope;
    private BattleTacticalCommandScope? _tacticalCommandScope;
    private IGridMovementService? _movement;
    private int _summonCounter;
    private bool _floorRuleStartAttempted;
    private bool _floorRuleEnded;

    public int TickIndex { get; private set; }
    public BattleOutcome Outcome { get; private set; } = BattleOutcome.Running;
    public int GoldSpent { get; private set; }
    public int SuccessfulTacticalCommandUses { get; private set; }
    public int TacticalPoints => _tacticalCommandScope?.TacticalPoints ?? 0;
    public int MaximumTacticalPoints => BattleTacticalCommandScope.MaximumTacticalPoints;
    public BattleTacticalCommandSnapshot TacticalCommands =>
        _tacticalCommandScope?.Snapshot(TickIndex) ??
        new BattleTacticalCommandSnapshot(0, MaximumTacticalPoints, []);
    public int RemainingGold => _config.StartingGold - GoldSpent;
    public IReadOnlyList<BattleUnitState> Units => _units;
    public IReadOnlyList<BattleEvent> PendingEvents => _events;
    public IReadOnlyList<EffectTraceEntry> EffectTrace => _effectCompatibility.Trace;
    public BattleScopeTransitionResult? EffectTransition => _effectCompatibility.Transition;
    public AttributeScopeTransitionResult? AttributeTransition => _attributeScope.Transition;
    public IReadOnlyList<BattleCombatEvent> CombatEvents => _combatPipeline.Events;
    internal ImmutableArray<BattleCombatSubscriptionSnapshot> CombatSubscriptions =>
        _combatPipeline.CaptureSubscriptionSnapshot();
    public BattleCombatTransitionResult? CombatTransition => _combatPipeline.Transition;
    public AbilityScopeTransitionResult? AbilityTransition => _abilityScope?.Transition;
    public StatusScopeTransitionResult? StatusTransition => _statusScope.Transition;
    public IReadOnlyList<StatusPresentationCue> StatusPresentationCues => _statusPresentationCueView;
    public RelicBattleTransitionResult? RelicTransition => _relicScope?.Transition;
    public IReadOnlyList<RelicCounterTransitionSnapshot> RelicCounterTransitions =>
        _relicScope?.CounterTransitions ?? [];
    public int RelicModifierCount => _relicScope?.ModifierHandleCount ?? 0;
    public EquipmentBattleTransitionResult? EquipmentTransition => _equipmentScope?.Transition;
    public int EquipmentModifierCount => _equipmentScope?.LiveModifierHandleCount ?? 0;
    public int EquipmentSubscriptionCount => _equipmentScope?.LiveSubscriptionCount ?? 0;
    public TraitSnapshot TraitSnapshot => _traitScope?.Snapshot ??
        TraitSnapshotBuilder.Build([], []);
    public TraitBattleTransitionResult? TraitTransition => _traitScope?.Transition;
    public TacticalCommandScopeTransitionResult? TacticalCommandTransition =>
        _tacticalCommandScope?.Transition;

    public BattleSimulation(BattleConfig config)
    {
        _config = config;
        _random = new DeterministicRandom(config.Seed);
        _statusPresentationCueView = _statusPresentationCues.AsReadOnly();
        _attributeScope = new BattleAttributeScope($"battle_{config.Seed}_attributes");
        _combatPipeline = new BattleCombatEventPipeline($"battle_{config.Seed}_combat", identity: config.Identity);
        _effectCompatibility = new BattleEffectCompatibilityAdapter(
            $"battle_{config.Seed}",
            new LegacyBattleMutationPort(CaptureEffectSnapshot, CommitCompatibilityMutation));
        _statusScope = new BattleStatusScope(
            $"battle_{config.Seed}_statuses",
            SynchronizeStatusPresentation,
            runtimeId => _units.FirstOrDefault(unit => unit.RuntimeId == runtimeId)?.Attributes,
            ScheduleStatusEffect,
            OnStatusLifecycle,
            cue => _statusPresentationCues.Add(cue),
            combatReactiveRegistrar: request => _combatPipeline.Subscribe(
                request.EventKind,
                request.Source,
                request.Priority,
                request.Listener),
            reactiveEffectSink: ExecuteStatusEffectNow);
        _relicScope = null;
        try
        {
            var combatBindings = new BattleCombatBindingRegistry(_combatPipeline);
            try { config.ConfigureCombatBindings?.Invoke(combatBindings); }
            finally { combatBindings.CloseRegistration(); }

            var index = 0;
            foreach (var spawn in config.Spawns)
            {
                var unit = spawn.Unit;
                var taggedForHero = !string.IsNullOrWhiteSpace(config.HeroRule.RequiredSoldierTag) && unit.Tags.Contains(config.HeroRule.RequiredSoldierTag);
                var healthMultiplier = spawn.Team == 0
                    ? (unit.IsHero ? config.Modifiers.HeroHealthMultiplier : config.Modifiers.ArmyHealthMultiplier * config.HeroRule.SoldierHealthMultiplier)
                    : 1f;
                if (spawn.Team == 0 && !unit.IsHero && taggedForHero) healthMultiplier *= config.HeroRule.TaggedSoldierHealthMultiplier;
                var damageMultiplier = spawn.Team == 0
                    ? (unit.IsHero
                        ? config.Modifiers.HeroDamageMultiplier * config.HeroRule.HeroDamageMultiplier *
                          (1f + config.EmptyDeploymentSlots * (config.HeroRule.EmptySlotHeroBonus + config.Modifiers.EmptySlotPower / 100f))
                        : config.Modifiers.ArmyDamageMultiplier * config.HeroRule.SoldierDamageMultiplier)
                    : 1f;
                if (spawn.Team == 0 && !unit.IsHero && taggedForHero) damageMultiplier *= config.HeroRule.TaggedSoldierDamageMultiplier;
                var requestedCell = ClampCell(spawn.Cell);
                var resolvedCell = CanOccupy(requestedCell) ? requestedCell : FindOpenNear(requestedCell, spawn.Team);
                var maxHealth = unit.MaxHealth * healthMultiplier;
                var damage = unit.Damage * damageMultiplier;
                var lifeSteal = Mathf.Clamp(unit.LifeSteal + (spawn.Team == 0
                    ? (unit.IsHero ? config.Modifiers.HeroLifeStealBonus + config.HeroRule.HeroLifeStealBonus : config.Modifiers.ArmyLifeStealBonus)
                    : 0), 0, .8f);
                var runtimeId = string.IsNullOrWhiteSpace(spawn.InstanceId)
                    ? $"{(spawn.Team == 0 ? "p" : "e")}-{index}"
                    : spawn.InstanceId;
                var attributes = _attributeScope.CreateSet(
                    runtimeId,
                    CreateBattleAttributeDefinition(unit, maxHealth, damage, lifeSteal));
                var state = new BattleUnitState
                {
                    RuntimeId = runtimeId,
                    SourceInstanceId = spawn.InstanceId,
                    Definition = unit,
                    Attributes = attributes,
                    Team = spawn.Team,
                    Cell = resolvedCell,
                    Health = maxHealth * Mathf.Clamp(spawn.HealthRatio, .05f, 1f),
                    Shield = spawn.Team == 0 ? config.Modifiers.StartShield +
                        (unit.IsHero ? maxHealth * config.EmptyDeploymentSlots * config.HeroRule.EmptySlotStartShield : 0) : 0,
                    IsTemporary = spawn.IsTemporary,
                    IsPersistentRosterHero = spawn.IsPersistentRosterHero ??
                        (spawn.Team == 0 && !spawn.IsTemporary && unit.IsHero),
                    BehaviorSummon = spawn.BehaviorSummon
                };
                _units.Add(state);
                _statistics.Add(state.RuntimeId, new BattleUnitStatistics { JoinTick = 0 });
                index++;
            }
            var healthRatios = _units.ToDictionary(
                unit => unit.RuntimeId,
                unit => unit.MaxHealth <= 0 ? 1f : unit.Health / unit.MaxHealth,
                StringComparer.Ordinal);
            _equipmentScope = new EquipmentBattleScope(
                $"battle_{config.Seed}_equipment",
                config.Equipment,
                _units.Where(unit => unit.Team == 0)
                    .Select(unit => new EquipmentOwnerBinding(
                        unit.SourceInstanceId,
                        unit.RuntimeId,
                        unit.IsPersistentRosterHero && !unit.IsTemporary,
                        unit.Attributes)));
            _traitScope = new BattleTraitScope(
                $"battle_{config.Seed}_traits",
                config.Traits,
                _units.Select(unit => new TraitOwnerBinding(
                    unit.RuntimeId,
                    unit.Team,
                    unit.Attributes)));
            var equipmentBindings = new BattleCombatBindingRegistry(_combatPipeline);
            try
            {
                _equipmentScope.Activate(new EquipmentBattleRuntimeContext
                {
                    CombatBindings = equipmentBindings,
                    CanReceiveStatus = runtimeId => _units.Any(unit =>
                        unit.RuntimeId == runtimeId && unit.Alive),
                    ApplyStatuses = applications => _statusScope.ApplyBatch(applications)
                });
            }
            finally { equipmentBindings.CloseRegistration(); }
            if (config.Relics is not null)
            {
                _relicScope = new RelicBattleScope(config.Relics);
                var relicBindings = new BattleCombatBindingRegistry(_combatPipeline);
                try
                {
                    _relicScope.Activate(new RelicBattleRuntimeContext
                    {
                        CombatBindings = relicBindings,
                        QueryUnits = QueryRelicUnits,
                        ExecuteEffect = ExecuteRelicEffect,
                        Summon = SummonRelic,
                        CurrentTick = () => TickIndex,
                        EmptyDeploymentSlots = config.EmptyDeploymentSlots
                    });
                }
                finally { relicBindings.CloseRegistration(); }
            }
            foreach (var unit in _units)
                unit.Health = unit.MaxHealth * healthRatios[unit.RuntimeId];
            if (config.TacticalCommands is not null)
                _tacticalCommandScope = new BattleTacticalCommandScope(
                    $"battle_{config.Seed}_tactical_commands",
                    this,
                    config.TacticalCommands);
            // Typed setup starts here. Legacy BattleEvent/digest publication remains in its
            // historical position after configured setup mutations and battle-start abilities.
            PublishCombat(new BattleCombatEventDraft(
                BattleCombatEventKind.BattleStarted,
                CombatSourceRef.System("battle"),
                string.Empty,
                string.Empty,
                0,
                Cell: ToCombatCell(new Vector2I(Width / 2, Height / 2))));
            AddConfiguredSummons();
            InitializeAbilityScope();
            _floorRuleStartAttempted = true;
            _config.FloorRule.OnBattleStarted(CreateRuleContext());
            _movement = new DeterministicGridMovementService(
                Width, Height, () => _units, cell => _config.FloorRule.CanOccupy(cell), HasLineAccess, config.Seed);
            ActivateBattleStartedAbilities();
            Emit("battle_started", "", "", 0, new Vector2I(Width / 2, Height / 2), "idle");
        }
        catch
        {
            try { if (_floorRuleStartAttempted) EndFloorRule(BattleOutcome.Timeout); }
            catch { }
            try { CompleteBattleScopes(BattleScopeCompletionReason.Exception); }
            catch { }
            _movement?.Dispose();
            _movement = null;
            _abilityScope?.Dispose();
            _tacticalCommandScope?.Dispose();
            _statusScope.Dispose();
            _traitScope?.Dispose();
            _equipmentScope?.Dispose();
            _effectCompatibility.Dispose();
            _attributeScope.Dispose();
            _combatPipeline.Dispose();
            throw;
        }
    }

    public IReadOnlyList<BattleEvent> DrainEvents()
    {
        var copy = _events.ToArray();
        _events.Clear();
        return copy;
    }

    public BattleOutcome Step()
    {
        if (Outcome != BattleOutcome.Running) return Outcome;
        TickIndex++;
        try
        {
            _movement!.BeginTick();
            ApplyFloorRule();
            foreach (var unit in _units.Where(unit => unit.Alive).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray())
                Act(unit);
            using (var movementResolution = _combatPipeline.BeginAuthoritativeResolution())
            {
                _movement.ResolveIntents((unit, cell) =>
                {
                    Emit("move", unit.RuntimeId, "", 0, cell, "move");
                    PublishCombat(new BattleCombatEventDraft(
                        BattleCombatEventKind.UnitMoved,
                        ResolveCombatSource(unit.RuntimeId, unit),
                        unit.RuntimeId,
                        string.Empty,
                        TickIndex,
                        Cell: ToCombatCell(cell)));
                });
                movementResolution.Commit();
            }
            ResolveOutcome();
        }
        catch
        {
            try { EndAfterFailure(); }
            catch { }
            throw;
        }
        return Outcome;
    }

    public BattleResult RunToEnd()
    {
        while (Outcome == BattleOutcome.Running) Step();
        return CreateResult();
    }

    public void Abort() => EndExplicitly(BattleScopeCompletionReason.Abort);

    public void Replace() => EndExplicitly(BattleScopeCompletionReason.Replacement);

    private void EndExplicitly(BattleScopeCompletionReason reason)
    {
        if (Outcome == BattleOutcome.Running) Outcome = BattleOutcome.Timeout;
        try
        {
            EndFloorRule(Outcome);
            CompleteBattleScopes(reason);
        }
        catch
        {
            CompleteBattleScopes(BattleScopeCompletionReason.Exception);
            throw;
        }
    }

    public void Dispose()
    {
        Exception? failure = null;
        try
        {
            if (Outcome == BattleOutcome.Running) Outcome = BattleOutcome.Timeout;
            EndFloorRule(Outcome);
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        try
        {
            CompleteBattleScopes(failure is null
                ? BattleScopeCompletionReason.Disposal
                : BattleScopeCompletionReason.Exception);
        }
        catch (Exception exception)
        {
            failure ??= exception;
        }
        finally
        {
            foreach (var unit in _units) ClearActionTarget(unit);
            _events.Clear();
            _movement?.Dispose();
            _movement = null;
            _abilityScope?.Dispose();
            _tacticalCommandScope?.Dispose();
            _statusScope.Dispose();
            _traitScope?.Dispose();
            _equipmentScope?.Dispose();
            _effectCompatibility.Dispose();
            _attributeScope.Dispose();
            _combatPipeline.Dispose();
        }
        if (failure is not null) throw failure;
    }

    public TacticalCommandActivationResult TryUseTacticalCommand(
        int slotIndex,
        string explicitTargetId = "")
    {
        if (Outcome != BattleOutcome.Running || _tacticalCommandScope is null)
            return new TacticalCommandActivationResult(
                false,
                TacticalCommandActivationFailure.ScopeCompleted,
                "战斗已经结束或战术指令尚未就绪。",
                slotIndex,
                string.Empty,
                0,
                0,
                []);
        var source = _units
            .Where(unit => unit.Team == 0 && unit.IsPersistentRosterHero && !unit.IsTemporary && unit.Alive)
            .OrderBy(unit => unit.SourceInstanceId == "player-hero" ? 0 : 1)
            .ThenBy(unit => unit.RuntimeId, StringComparer.Ordinal)
            .FirstOrDefault();
        var activation = _tacticalCommandScope.TryActivate(
            slotIndex,
            source?.RuntimeId ?? string.Empty,
            TickIndex,
            explicitTargetId);
        if (!activation.Succeeded) return activation;
        SuccessfulTacticalCommandUses++;
        Emit("tactical_command", source!.RuntimeId, explicitTargetId,
            TacticalPoints, source.Cell, "skill_cast");
        return activation;
    }


    public BattleResult CreateResult()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_digest.ToString()))).ToLowerInvariant();
        var units = _terminalUnitReports.IsDefault ? BuildUnitReports() : _terminalUnitReports;
        return new BattleResult(Outcome, TickIndex, hash, units, GoldSpent, SuccessfulTacticalCommandUses,
            _relicScope?.Transition, _config.Identity);
    }

    private ImmutableArray<BattleUnitReportSnapshot> BuildUnitReports() =>
        _units.Select(unit =>
        {
            var statistics = _statistics[unit.RuntimeId];
            return new BattleUnitReportSnapshot(
                unit.RuntimeId,
                unit.SourceInstanceId,
                unit.Definition.ContentId,
                unit.Definition.DisplayName,
                unit.Definition.Role,
                unit.Team,
                unit.Definition.IsHero,
                unit.IsTemporary,
                unit.Alive,
                unit.Cell,
                unit.Health,
                unit.MaxHealth,
                unit.Shield,
                unit.Damage,
                statistics.DamageDealt,
                statistics.DamageTaken,
                statistics.ShieldAbsorbed,
                statistics.HealingDone,
                statistics.Kills,
                statistics.JoinTick,
                statistics.DefeatTick,
                statistics.AttackActions,
                statistics.EffectiveHealingEvents);
        }).ToImmutableArray();

    private void AddConfiguredSummons()
    {
        var hero = _units.FirstOrDefault(unit => unit.Team == 0 && unit.Definition.IsHero);
        if (hero is not null && _config.HeroRule.AddBattleConstruct)
            SpawnTemporary(_config.Summons.HeroConstruct, 0, FindOpenNear(hero.Cell, 0), .85f, .9f);
        _relicScope?.ExecuteBattleStartEffects();
        if (hero is not null && _config.Modifiers.SummonToken)
            SpawnTemporary(_config.Summons.ItemToken, 0, FindOpenNear(hero.Cell, 0), .85f, .9f);
    }

    private ImmutableArray<RelicBattleUnitBinding> QueryRelicUnits() => _units
        .Select(unit => new RelicBattleUnitBinding(
            unit.RuntimeId,
            unit.Team,
            unit.Definition.IsHero,
            unit.IsTemporary,
            !unit.IsTemporary,
            unit.Alive,
            ToCombatCell(unit.Cell),
            unit.Attributes))
        .ToImmutableArray();

    private void ExecuteRelicEffect(
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId,
        string targetId,
        int tick,
        float invocationValue)
    {
        var result = _effectCompatibility.ExecuteAuthored(
            binding,
            sourceId,
            ownerId,
            targetId,
            tick,
            invocationValue);
        if (result.Status is EffectExecutionStatus.Failed or EffectExecutionStatus.Interrupted)
            throw new InvalidOperationException(
                $"Relic effect '{binding.StableId}' failed: {result.Interruption} " +
                result.Invocations.FirstOrDefault()?.Message);
    }

    private bool SummonRelic(
        string contentId,
        int team,
        float healthMultiplier,
        float damageMultiplier,
        string sourceId)
    {
        if (!_config.RelicSummons.TryGetValue(contentId, out var snapshot)) return false;
        var anchor = _units
            .Where(unit => unit.Team == team && unit.Alive && !unit.IsTemporary)
            .OrderBy(unit => unit.Definition.IsHero ? 0 : 1)
            .ThenBy(unit => unit.RuntimeId, StringComparer.Ordinal)
            .FirstOrDefault();
        return anchor is not null && SpawnTemporary(
            snapshot,
            team,
            FindOpenNear(anchor.Cell, team),
            healthMultiplier,
            damageMultiplier,
            sourceId);
    }

    private void InitializeAbilityScope()
    {
        if (_config.BossTimeline is null &&
            _units.All(unit => unit.Definition.AbilityLoadout is null))
            return;

        _abilityScope = new BattleAbilityScope($"battle_{_config.Seed}_abilities", this, 0);
        foreach (var unit in _units.Where(unit => unit.Definition.AbilityLoadout is not null && !IsTimelineBoss(unit))
                     .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
            _abilityScope.RegisterLoadout(unit.RuntimeId, unit.Definition.AbilityLoadout!);
        foreach (var boss in _units.Where(IsTimelineBoss).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
            SynchronizeBossPhase(boss, initial: true);
    }

    private bool IsTimelineBoss(BattleUnitState unit) =>
        _config.BossTimeline is { } timeline && unit.Definition.IsBoss &&
        string.Equals(unit.Definition.ContentId, timeline.BossContentId, StringComparison.Ordinal);

    private void SynchronizeBossPhase(BattleUnitState unit, bool initial = false)
    {
        if (_abilityScope is null || !IsTimelineBoss(unit) || _config.BossTimeline is not { } timeline ||
            timeline.Phases.IsDefaultOrEmpty)
            return;
        var ratio = unit.MaxHealth <= 0 ? 0 : unit.Health / unit.MaxHealth;
        var nextIndex = 0;
        for (var index = 1; index < timeline.Phases.Length; index++)
            if (ratio <= timeline.Phases[index].StartHealthRatio)
                nextIndex = index;
        if (_bossPhaseIndexes.TryGetValue(unit.RuntimeId, out var currentIndex) && currentIndex == nextIndex)
            return;

        var phase = timeline.Phases[nextIndex];
        var loadout = phase.AbilityLoadout ??
            new CompiledAbilityLoadout(ImmutableArray<CompiledAbilityDefinition>.Empty);
        if (initial)
            _abilityScope.RegisterLoadout(unit.RuntimeId, loadout);
        else
            _abilityScope.ReplaceLoadout(unit.RuntimeId, loadout);
        _bossPhaseIndexes[unit.RuntimeId] = nextIndex;
        unit.BossPhaseId = phase.StableId;
    }

    private void ActivateBattleStartedAbilities()
    {
        if (_abilityScope is null) return;
        foreach (var unit in _units.Where(unit => unit.Alive).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
            _abilityScope.ActivateAutomatic(unit.RuntimeId, 0);
    }

    private void ActivateAutomaticAbilities(BattleUnitState unit) =>
        _abilityScope?.ActivateAutomatic(unit.RuntimeId, TickIndex);

    private bool ScheduleStatusEffect(StatusEffectInvocation invocation)
    {
        var source = CombatSourceRef.Status(
            invocation.Definition.StableId,
            invocation.OwnerId,
            invocation.InstanceId);
        return _combatPipeline.EnqueuePostResolution(source, 0, _ =>
        {
            if (!ExecuteStatusEffectNow(invocation))
                throw new InvalidOperationException($"Status effect '{invocation.Binding.StableId}' failed.");
        });
    }

    private bool ExecuteStatusEffectNow(StatusEffectInvocation invocation)
    {
        var combatEvent = invocation.CombatEvent;
        var result = _effectCompatibility.ExecuteAuthored(
            invocation.Binding,
            invocation.SourceId,
            invocation.OwnerId,
            invocation.ExplicitTargetId,
            invocation.Tick,
            combatEvent?.EffectiveValue ?? 0);
        return result.Status is EffectExecutionStatus.Succeeded or EffectExecutionStatus.Skipped;
    }

    private void SynchronizeStatusPresentation(string ownerId, ImmutableArray<StatusRuntimeSnapshot> statuses)
    {
        var unit = _units.FirstOrDefault(candidate => candidate.RuntimeId == ownerId);
        if (unit is null) return;
        unit.Statuses = statuses;
    }

    private void OnStatusLifecycle(StatusLifecycleEvent lifecycle)
    {
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        var status = lifecycle.Status;
        var owner = _units.FirstOrDefault(unit => unit.RuntimeId == status.OwnerId);
        var kind = lifecycle.Kind switch
        {
            StatusLifecycleKind.Applied => BattleCombatEventKind.StatusApplied,
            StatusLifecycleKind.StackChanged => BattleCombatEventKind.StatusStackChanged,
            _ => BattleCombatEventKind.StatusRemoved
        };
        PublishCombat(new BattleCombatEventDraft(
            kind,
            ResolveCombatSource(status.SourceId),
            status.SourceId,
            status.OwnerId,
            lifecycle.Tick,
            Cell: owner is null ? default : ToCombatCell(owner.Cell),
            SubjectStableId: status.StableId,
            PreviousStacks: lifecycle.PreviousStacks,
            CurrentStacks: lifecycle.CurrentStacks,
            Reason: lifecycle.RemovalReason == StatusRemovalReason.None
                ? string.Empty
                : lifecycle.RemovalReason.ToString()));
        resolution.Commit();
    }

    AbilityWorldSnapshot IAbilityRuntimeWorld.CaptureSnapshot(int tick) => CaptureAbilitySnapshot(tick);

    AbilityPreparationResult IAbilityRuntimeWorld.Prepare(
        CompiledAbilityDefinition ability,
        string sourceId,
        string ownerId,
        string explicitTargetId,
        int tick) => PrepareAbility(ability, sourceId, ownerId, explicitTargetId, tick);

    AbilityCommitResult IAbilityRuntimeWorld.Commit(AbilityExecutionPlan plan) => CommitAbility(plan);

    private AbilityWorldSnapshot CaptureAbilitySnapshot(int tick) => new(
        tick,
        _units.Select(unit => new AbilityEntitySnapshot(
                unit.RuntimeId,
                unit.Team,
                unit.Alive,
                unit.MaxHealth,
                unit.Definition.Tags.ToImmutableArray()))
            .ToImmutableDictionary(unit => unit.RuntimeId, StringComparer.Ordinal));

    private AbilityPreparationResult PrepareAbility(
        CompiledAbilityDefinition ability,
        string sourceId,
        string ownerId,
        string explicitTargetId,
        int tick)
    {
        var snapshot = CaptureAbilitySnapshot(tick);
        if (!snapshot.Entities.TryGetValue(ownerId, out var ownerSnapshot) || !ownerSnapshot.Alive)
            return AbilityPreparationFailed(AbilityActivationFailure.SourceUnavailable, "英雄或能力拥有者已无法行动。");
        if (ability.GoldCost > RemainingGold)
            return AbilityPreparationFailed(AbilityActivationFailure.InsufficientGold, $"金币不足：需要 {ability.GoldCost} 金币。");
        var owner = _units.First(unit => unit.RuntimeId == ownerId);
        var operations = ImmutableArray.CreateBuilder<ResolvedAbilityOperation>();
        var summons = ImmutableArray.CreateBuilder<AbilitySummonReservation>();
        var reservedCells = new HashSet<Vector2I>();
        for (var operationIndex = 0; operationIndex < ability.Operations.Length; operationIndex++)
        {
            var operation = ability.Operations[operationIndex];
            switch (operation)
            {
                case CompiledEffectAbilityOperation effect:
                {
                    var targetIds = ResolveAbilityTargets(effect.Binding.TargetQuery, snapshot, sourceId, ownerId, explicitTargetId);
                    if (targetIds.Length == 0) return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "当前没有合法的能力目标。");
                    var invocationValue = effect.InvocationValueSource == AbilityInvocationValueSource.OwnerMaxHealth
                        ? ownerSnapshot.MaxHealth * effect.InvocationValueScale
                        : effect.InvocationValueScale;
                    var preflight = _effectCompatibility.PreflightAuthored(
                        effect.Binding,
                        sourceId,
                        ownerId,
                        targetIds.FirstOrDefault() ?? explicitTargetId,
                        tick,
                        invocationValue);
                    if (!preflight.Succeeded)
                        return AbilityPreparationFailed(
                            AbilityActivationFailure.ConditionsUnmet,
                            string.IsNullOrWhiteSpace(preflight.Message) ? "当前不满足能力效果条件。" : preflight.Message);
                    operations.Add(new ResolvedAbilityOperation(operationIndex, operation, targetIds, invocationValue));
                    break;
                }
                case CompiledCooldownAbilityOperation cooldown:
                {
                    var targetIds = ResolveAbilityTargets(cooldown.TargetQuery, snapshot, sourceId, ownerId, explicitTargetId);
                    if (targetIds.Length == 0) return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "当前没有合法的能力目标。");
                    operations.Add(new ResolvedAbilityOperation(operationIndex, operation, targetIds, 0));
                    break;
                }
                case CompiledApplyStatusAbilityOperation status:
                {
                    var targetIds = ResolveAbilityTargets(status.TargetQuery, snapshot, sourceId, ownerId, explicitTargetId);
                    if (targetIds.Length == 0) return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "当前没有合法的状态目标。");
                    operations.Add(new ResolvedAbilityOperation(operationIndex, operation, targetIds, 0));
                    break;
                }
                case CompiledSummonAbilityOperation summon:
                {
                    var profile = ResolveSummonProfile(summon, owner);
                    if (profile is null) return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "没有可用的召唤单位。");
                    var livingTemporary = _units.Count(unit => unit.Team == owner.Team && unit.IsTemporary && unit.Alive);
                    var availableByLimit = summon.MaximumLivingTemporaryUnits <= 0
                        ? summon.Count
                        : Math.Max(0, summon.MaximumLivingTemporaryUnits - livingTemporary);
                    var reserveCount = Math.Min(summon.Count, availableByLimit);
                    for (var sequence = 0; sequence < reserveCount; sequence++)
                    {
                        if (!TryFindOpenNear(owner.Cell, owner.Team, reservedCells, out var cell)) break;
                        reservedCells.Add(cell);
                        summons.Add(new AbilitySummonReservation(
                            operationIndex,
                            summon.Profile,
                            sequence,
                            cell.X,
                            cell.Y,
                            summon.HealthMultiplier,
                            summon.DamageMultiplier));
                    }
                    var reservedForOperation = summons.Count(item => item.OperationIndex == operationIndex);
                    if (summon.RequireAtLeastOne && reservedForOperation == 0)
                        return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "没有可用的召唤单位或合法落点。");
                    operations.Add(new ResolvedAbilityOperation(operationIndex, operation, [], 0));
                    break;
                }
                default:
                    return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "能力包含不受支持的操作。");
            }
        }

        return new AbilityPreparationResult(
            true,
            AbilityActivationFailure.None,
            string.Empty,
            new AbilityExecutionPlan(
                ability,
                sourceId,
                ownerId,
                tick,
                operations.ToImmutable(),
                summons.ToImmutable(),
                ability.GoldCost));
    }

    private AbilityCommitResult CommitAbility(AbilityExecutionPlan plan)
    {
        var checkpoint = new BattleWorldStateCheckpoint(this);
        try
        {
            var result = CommitAbilityCore(plan);
            if (!result.Succeeded)
            {
                checkpoint.Rollback();
                return result;
            }
            checkpoint.Commit();
            return result;
        }
        catch (Exception commitFailure)
        {
            try
            {
                checkpoint.Rollback();
            }
            catch (Exception rollbackFailure)
            {
                throw new AggregateException("Ability world rollback failed.", commitFailure, rollbackFailure);
            }
            return AbilityCommitFailed(AbilityActivationFailure.CommitFailed, commitFailure.Message);
        }
    }

    private AbilityCommitResult CommitAbilityCore(AbilityExecutionPlan plan)
    {
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        if (plan.Tick != TickIndex)
            return AbilityCommitFailed(AbilityActivationFailure.CommitFailed, "能力计划已经过期。");
        var owner = _units.FirstOrDefault(unit => unit.RuntimeId == plan.OwnerId && unit.Alive);
        if (owner is null) return AbilityCommitFailed(AbilityActivationFailure.SourceUnavailable, "英雄或能力拥有者已无法行动。");
        if (plan.GoldCost > RemainingGold) return AbilityCommitFailed(AbilityActivationFailure.InsufficientGold, $"金币不足：需要 {plan.GoldCost} 金币。");

        foreach (var operation in plan.Operations)
            if (operation.TargetIds.Any(targetId => !_units.Any(unit => unit.RuntimeId == targetId && unit.Alive)))
                return AbilityCommitFailed(AbilityActivationFailure.CommitFailed, "能力目标在提交前失效。");
        var reservationCells = plan.Summons.Select(item => new Vector2I(item.CellX, item.CellY)).ToArray();
        if (reservationCells.Distinct().Count() != reservationCells.Length || reservationCells.Any(cell => !CanOccupy(cell)))
            return AbilityCommitFailed(AbilityActivationFailure.CommitFailed, "召唤落点在提交前失效。");
        foreach (var reservation in plan.Summons)
        {
            var summon = (CompiledSummonAbilityOperation)plan.Ability.Operations[reservation.OperationIndex];
            if (ResolveSummonProfile(summon, owner) is null)
                return AbilityCommitFailed(AbilityActivationFailure.CommitFailed, "召唤模板在提交前失效。");
        }

        var facts = ImmutableArray.CreateBuilder<string>();
        foreach (var resolved in plan.Operations.OrderBy(operation => operation.OperationIndex))
        {
            switch (resolved.Operation)
            {
                case CompiledEffectAbilityOperation effect:
                {
                    var result = _effectCompatibility.ExecuteAuthored(
                        effect.Binding,
                        plan.SourceId,
                        plan.OwnerId,
                        resolved.TargetIds.FirstOrDefault() ?? string.Empty,
                        plan.Tick,
                        resolved.InvocationValue);
                    if (result.Status != EffectExecutionStatus.Succeeded)
                        throw new InvalidOperationException($"Prepared ability effect '{effect.Binding.StableId}' failed during commit.");
                    var steps = result.Invocations.SelectMany(invocation => invocation.Steps)
                        .Where(step => step.Status == EffectExecutionStatus.Succeeded)
                        .ToArray();
                    if (plan.Ability.ActivationKind == AbilityActivationKind.Automatic)
                        foreach (var step in steps.Where(step => step.Kind == EffectKind.Shield))
                        {
                            var target = _units.First(unit => unit.RuntimeId == step.TargetId);
                            Emit("shield", plan.SourceId, step.TargetId, step.AppliedAmount, target.Cell, "skill_cast");
                        }
                    facts.AddRange(steps.Select(step => $"{step.Kind}:{step.TargetId}:{step.AppliedAmount:0.###}"));
                    break;
                }
                case CompiledCooldownAbilityOperation cooldown:
                    foreach (var targetId in resolved.TargetIds)
                    {
                        var target = _units.First(unit => unit.RuntimeId == targetId);
                        target.AttackCooldown = AdjustCooldown(target.AttackCooldown, cooldown.AttackAdjustment, cooldown.AttackValue);
                        target.MoveCooldown = AdjustCooldown(target.MoveCooldown, cooldown.MoveAdjustment, cooldown.MoveValue);
                        facts.Add($"Cooldown:{targetId}");
                    }
                    break;
                case CompiledApplyStatusAbilityOperation status:
                    foreach (var targetId in resolved.TargetIds)
                    {
                        var applied = _statusScope.Apply(status.Status, plan.SourceId, targetId, plan.Tick);
                        if (!applied.Applied)
                            throw new InvalidOperationException($"Prepared status '{status.Status.StableId}' failed during commit.");
                        facts.Add($"Status:{status.Status.StableId}:{targetId}:{applied.Status?.Stacks ?? 0}");
                    }
                    break;
                case CompiledSummonAbilityOperation summon:
                    foreach (var reservation in plan.Summons
                                 .Where(item => item.OperationIndex == resolved.OperationIndex)
                                 .OrderBy(item => item.Sequence))
                    {
                        var profile = ResolveSummonProfile(summon, owner)!;
                        if (!SpawnTemporary(
                                profile,
                                owner.Team,
                                new Vector2I(reservation.CellX, reservation.CellY),
                                reservation.HealthMultiplier,
                                reservation.DamageMultiplier,
                                plan.SourceId))
                            throw new InvalidOperationException("Prepared ability summon failed during commit.");
                        facts.Add($"Summon:{profile.ContentId}:{reservation.CellX},{reservation.CellY}");
                    }
                    break;
            }
        }

        GoldSpent += plan.GoldCost;
        PublishCombat(new BattleCombatEventDraft(
            BattleCombatEventKind.AbilityResolved,
            ResolveCombatSource(plan.SourceId),
            plan.SourceId,
            plan.OwnerId,
            plan.Tick,
            SubjectStableId: plan.Ability.StableId));
        resolution.Commit();
        return new AbilityCommitResult(true, AbilityActivationFailure.None, string.Empty, facts.ToImmutable());
    }

    private static ImmutableArray<string> ResolveAbilityTargets(
        CompiledEffectTargetQuery query,
        AbilityWorldSnapshot snapshot,
        string sourceId,
        string ownerId,
        string explicitTargetId)
    {
        IEnumerable<AbilityEntitySnapshot> targets = query switch
        {
            CompiledExplicitTargetQuery => AbilityLookup(snapshot, explicitTargetId),
            CompiledSourceTargetQuery => AbilityLookup(snapshot, sourceId),
            CompiledOwnerTargetQuery => AbilityLookup(snapshot, ownerId),
            CompiledRelativeTeamTargetQuery relative => ResolveAbilityRelative(snapshot, ownerId, sourceId, relative),
            _ => []
        };
        return targets.Select(target => target.RuntimeId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static IEnumerable<AbilityEntitySnapshot> AbilityLookup(AbilityWorldSnapshot snapshot, string runtimeId)
    {
        if (!string.IsNullOrWhiteSpace(runtimeId) && snapshot.Entities.TryGetValue(runtimeId, out var target) && target.Alive)
            yield return target;
    }

    private static IEnumerable<AbilityEntitySnapshot> ResolveAbilityRelative(
        AbilityWorldSnapshot snapshot,
        string ownerId,
        string sourceId,
        CompiledRelativeTeamTargetQuery relative)
    {
        var anchorId = snapshot.Entities.ContainsKey(ownerId) ? ownerId : sourceId;
        if (!snapshot.Entities.TryGetValue(anchorId, out var anchor)) return [];
        var team = relative.Team == EffectRelativeTeam.Allies ? anchor.Team : 1 - anchor.Team;
        return snapshot.Entities.Values.Where(target =>
            target.Team == team &&
            (relative.IncludeDefeated || target.Alive) &&
            (string.IsNullOrWhiteSpace(relative.RequiredTag) || target.Tags.Contains(relative.RequiredTag, StringComparer.Ordinal)));
    }

    private UnitSnapshot? ResolveSummonProfile(CompiledSummonAbilityOperation summon, BattleUnitState owner)
    {
        if (!string.IsNullOrWhiteSpace(summon.SummonContentId) &&
            _config.TacticalSummons.TryGetValue(summon.SummonContentId, out var authoredSummon))
            return authoredSummon;
        var profile = summon.Profile switch
        {
            AbilitySummonProfile.DeathSummon => _config.Summons.DeathSummon,
            AbilitySummonProfile.HeroConstruct => _config.Summons.HeroConstruct,
            AbilitySummonProfile.Mercenary => _config.Summons.Mercenary,
            AbilitySummonProfile.ItemToken => _config.Summons.ItemToken,
            AbilitySummonProfile.BehaviorSummon => owner.BehaviorSummon,
            _ => null
        };
        return profile is not null &&
               (string.IsNullOrWhiteSpace(summon.SummonContentId) || profile.ContentId == summon.SummonContentId)
            ? profile
            : null;
    }

    private static int AdjustCooldown(int current, CooldownAdjustmentKind kind, int value) => kind switch
    {
        CooldownAdjustmentKind.None => current,
        CooldownAdjustmentKind.Reset => 0,
        CooldownAdjustmentKind.Add => current + value,
        CooldownAdjustmentKind.Cap => Math.Min(current, value),
        CooldownAdjustmentKind.Divide => current / value,
        _ => current
    };

    private static AbilityPreparationResult AbilityPreparationFailed(AbilityActivationFailure failure, string reason) =>
        new(false, failure, reason, null);

    private static AbilityCommitResult AbilityCommitFailed(AbilityActivationFailure failure, string reason) =>
        new(false, failure, reason, []);

    private void Act(BattleUnitState unit)
    {
        if (!unit.Alive) return;
        SynchronizeBossPhase(unit);
        ActivateAutomaticAbilities(unit);
        ApplyPeriodicBehavior(unit);
        if (!unit.Alive) return;
        bool actionsDisabled;
        using (var statusResolution = _combatPipeline.BeginAuthoritativeResolution())
        {
            actionsDisabled = _statusScope.HasTag(unit.RuntimeId, StatusDefinitionCompiler.ActionDisabledTag);
            _statusScope.AdvanceOwner(unit.RuntimeId, TickIndex);
            statusResolution.Commit();
        }
        if (actionsDisabled)
        {
            unit.Mode = BattleUnitMode.Disabled;
            unit.WaitingTicks = 0;
            _movement!.ReleaseGoal(unit.RuntimeId);
            return;
        }
        if (unit.DisabledTicks > 0)
        {
            unit.DisabledTicks--;
            unit.Mode = BattleUnitMode.Disabled;
            unit.WaitingTicks = 0;
            _movement!.ReleaseGoal(unit.RuntimeId);
            return;
        }
        if (unit.AttackCooldown > 0) unit.AttackCooldown--;
        if (unit.MoveCooldown > 0) unit.MoveCooldown--;

        if (unit.HealingPower > 0)
        {
            var wounded = Allies(unit.Team).Where(ally => ally != unit && ally.Health < ally.MaxHealth)
                .OrderBy(ally => ally.Health / ally.MaxHealth).ThenBy(ally => ally.RuntimeId, StringComparer.Ordinal).ToArray();
            var protectedAlly = _movement!.SelectTarget(unit, wounded);
            if (protectedAlly is not null)
            {
                SetActionTarget(unit, protectedAlly);
                unit.LastActionKind = BattleActionKind.Heal;
                if (Distance(unit.Cell, protectedAlly.Cell) <= unit.AttackRange && HasLineAccess(unit, protectedAlly))
                {
                    _movement.ReleaseGoal(unit.RuntimeId);
                    if (unit.AttackCooldown == 0)
                    {
                        var requestedHealing = unit.HealingPower;
                        HealLiving(unit.RuntimeId, protectedAlly, requestedHealing);
                        unit.AttackCooldown = unit.EffectiveAttackTicks;
                        unit.Mode = BattleUnitMode.Casting;
                        unit.WaitingTicks = 0;
                        Emit("heal", unit.RuntimeId, protectedAlly.RuntimeId, requestedHealing, protectedAlly.Cell, "skill_cast");
                    }
                    else unit.Mode = BattleUnitMode.Recovering;
                }
                else if (unit.MoveCooldown == 0) _movement.QueueMove(unit);
                else unit.Mode = BattleUnitMode.Seeking;
                return;
            }
        }

        var target = SelectTarget(unit);
        if (target is null)
        {
            ClearActionTarget(unit);
            if (Allies(1 - unit.Team).Any())
            {
                unit.Mode = BattleUnitMode.Waiting;
                unit.WaitingTicks++;
            }
            return;
        }
        SetActionTarget(unit, target);
        unit.LastActionKind = BattleActionKind.Attack;
        if (Distance(unit.Cell, target.Cell) <= unit.AttackRange && HasLineAccess(unit, target))
        {
            _movement!.ReleaseGoal(unit.RuntimeId);
            if (unit.AttackCooldown == 0) Attack(unit, target);
            else unit.Mode = BattleUnitMode.Recovering;
            return;
        }
        if (unit.MoveCooldown == 0) _movement!.QueueMove(unit);
        else unit.Mode = BattleUnitMode.Seeking;
    }

    private BattleUnitState? SelectTarget(BattleUnitState unit)
    {
        var enemies = Allies(1 - unit.Team).ToList();
        if (enemies.Count == 0)
        {
            _movement!.ClearTarget(unit.RuntimeId);
            return null;
        }
        IEnumerable<BattleUnitState> ordered;
        if (unit.Team == 0 && unit.Definition.IsHero && _config.HeroRule.PreferBossTargets)
        {
            ordered = enemies.OrderByDescending(enemy => enemy.Definition.IsBoss && Distance(unit.Cell, enemy.Cell) <= 3f)
                .ThenBy(enemy => Distance(unit.Cell, enemy.Cell)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        }
        else if (unit.Definition.Behavior.PreferBacklineTargets)
            ordered = enemies.OrderByDescending(enemy => enemy.AttackRange + enemy.HealingPower)
                .ThenBy(enemy => Distance(unit.Cell, enemy.Cell)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        else if (unit.Definition.Role == Content.UnitRole.Assassin)
            ordered = enemies.OrderByDescending(enemy => enemy.AttackRange).ThenBy(enemy => Distance(unit.Cell, enemy.Cell))
                .ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        else
            ordered = enemies.OrderBy(enemy => Distance(unit.Cell, enemy.Cell)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        return _movement!.SelectTarget(unit, ordered.ToArray());
    }

    private void Attack(BattleUnitState attacker, BattleUnitState target)
    {
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        _statistics[attacker.RuntimeId].AttackActions++;
        attacker.AttackCooldown = attacker.EffectiveAttackTicks;
        attacker.Mode = BattleUnitMode.Attacking;
        attacker.LastActionKind = BattleActionKind.Attack;
        attacker.WaitingTicks = 0;
        SetActionTarget(attacker, target);
        var source = ResolveCombatSource(attacker.RuntimeId, attacker);
        PublishCombat(new BattleCombatEventDraft(
            BattleCombatEventKind.AttackDeclared,
            source,
            attacker.RuntimeId,
            target.RuntimeId,
            TickIndex,
            Cell: ToCombatCell(target.Cell)));
        var rawDamage = EffectiveDamage(attacker);
        if (attacker.Definition.Behavior.LowHealthDamageBonus > 0 && attacker.Health / attacker.MaxHealth <= .4f)
            rawDamage *= 1f + attacker.Definition.Behavior.LowHealthDamageBonus;
        var damage = ApplyDamage(attacker.RuntimeId, attacker, target, rawDamage);
        if (attacker.Alive && attacker.LifeSteal > 0)
            HealLiving(attacker.RuntimeId, attacker, damage * attacker.LifeSteal);
        Emit("attack", attacker.RuntimeId, target.RuntimeId, damage, target.Cell, "attack");
        PublishCombat(new BattleCombatEventDraft(
            BattleCombatEventKind.AttackLanded,
            source,
            attacker.RuntimeId,
            target.RuntimeId,
            TickIndex,
            RequestedValue: rawDamage,
            AppliedValue: damage,
            EffectiveValue: damage,
            Cell: ToCombatCell(target.Cell)));
        if (attacker.Definition.SplashRadius > 0)
            foreach (var splash in Allies(target.Team).Where(other => other != target && Distance(other.Cell, target.Cell) <= attacker.Definition.SplashRadius).ToArray())
                ApplyDamage(attacker.RuntimeId, attacker, splash, rawDamage * .45f);
        if (attacker.Definition.Behavior.PiercingLine)
        {
            var behind = Allies(target.Team).FirstOrDefault(other => other != target && other.Cell.Y == target.Cell.Y &&
                Math.Sign(other.Cell.X - target.Cell.X) == Math.Sign(target.Cell.X - attacker.Cell.X));
            if (behind is not null) ApplyDamage(attacker.RuntimeId, attacker, behind, rawDamage * .35f);
        }
        if (attacker.Definition.Behavior.SlowOnHitTicks > 0 && target.Alive)
        {
            target.AttackCooldown += attacker.Definition.Behavior.SlowOnHitTicks / 2;
            target.MoveCooldown += attacker.Definition.Behavior.SlowOnHitTicks;
        }
        resolution.Commit();
    }

    private float ApplyDamage(string sourceRuntimeId, BattleUnitState? source, BattleUnitState target, float raw)
    {
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        var healthBefore = target.Health;
        var wasAlive = target.Alive;
        var context = CreateRuleContext();
        if (target.Team == 0 && target.Definition.IsHero && _config.HeroRule.EmptySlotHeroDefense > 0)
            raw *= Math.Max(.25f, 1f - _config.EmptyDeploymentSlots * _config.HeroRule.EmptySlotHeroDefense);
        raw = _config.FloorRule.ModifyIncomingDamage(context, target, raw);
        if (source is not null && source.Definition.Behavior.ExecuteHealthThreshold > 0 && target.Health / target.MaxHealth <= source.Definition.Behavior.ExecuteHealthThreshold)
            raw *= 1.5f;
        var combatSource = ResolveCombatSource(sourceRuntimeId, source);
        var creditedKiller = source ?? _units.FirstOrDefault(unit => unit.RuntimeId == sourceRuntimeId);
        var calculated = _combatPipeline.Resolve(new BattleCombatCalculationRequest(
            BattleCombatCalculationKind.Damage,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            TickIndex,
            Math.Max(0, raw)));
        raw = calculated.ResolvedAmount;
        var armor = EffectiveArmor(target);
        var damage = Math.Max(1f, raw * 100f / (100f + armor * 7f));
        var resolvedDamage = damage;
        var absorbed = Math.Min(target.Shield, damage);
        target.Shield -= absorbed;
        damage -= absorbed;
        target.Health = Math.Max(0, target.Health - damage);
        var healthRemoved = Math.Min(healthBefore, damage);
        var effectiveDamage = absorbed + healthRemoved;
        var targetStatistics = _statistics[target.RuntimeId];
        targetStatistics.DamageTaken += effectiveDamage;
        targetStatistics.ShieldAbsorbed += absorbed;
        if (_statistics.TryGetValue(sourceRuntimeId, out var sourceStatistics))
        {
            sourceStatistics.DamageDealt += effectiveDamage;
            if (wasAlive && !target.Alive) sourceStatistics.Kills++;
        }
        PublishCombat(new BattleCombatEventDraft(
            BattleCombatEventKind.DamageResolved,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            TickIndex,
            RequestedValue: calculated.RequestedAmount,
            AppliedValue: resolvedDamage,
            EffectiveValue: effectiveDamage,
            Cell: ToCombatCell(target.Cell)));
        if (!target.Alive)
        {
            targetStatistics.DefeatTick ??= TickIndex;
            target.Mode = BattleUnitMode.Defeated;
            target.LastActionKind = BattleActionKind.None;
            ClearActionTarget(target);
            target.WaitingTicks = 0;
            PublishCombat(new BattleCombatEventDraft(
                BattleCombatEventKind.UnitDefeated,
                combatSource,
                sourceRuntimeId,
                target.RuntimeId,
                TickIndex,
                AppliedValue: resolvedDamage,
                EffectiveValue: effectiveDamage,
                Cell: ToCombatCell(target.Cell)));
            if (creditedKiller is not null)
                PublishCombat(new BattleCombatEventDraft(
                    BattleCombatEventKind.UnitKilled,
                    combatSource,
                    sourceRuntimeId,
                    target.RuntimeId,
                    TickIndex,
                    AppliedValue: resolvedDamage,
                    EffectiveValue: effectiveDamage,
                    Cell: ToCombatCell(target.Cell)));
            Emit("defeated", sourceRuntimeId, target.RuntimeId, damage, target.Cell, "defeated");
            HandleDeath(source, target);
        }
        else Emit("damage", sourceRuntimeId, target.RuntimeId, damage, target.Cell, "hit");
        resolution.Commit();
        return damage;
    }

    private void ApplyFloorRule()
    {
        _config.FloorRule.OnTick(CreateRuleContext());
    }

    private bool BeaconControlled(int team)
    {
        var center = new Vector2I(Width / 2, Height / 2);
        var friendly = Allies(team).Count(unit => Distance(unit.Cell, center) <= 1.5f);
        var enemy = Allies(1 - team).Count(unit => Distance(unit.Cell, center) <= 1.5f);
        return friendly > enemy && friendly > 0;
    }

    private bool HasLineAccess(BattleUnitState source, BattleUnitState target)
        => HasLineAccess(source.Cell, source.Definition, target.Cell);

    private bool HasLineAccess(Vector2I sourceCell, UnitSnapshot sourceDefinition, Vector2I targetCell)
    {
        var delta = targetCell - sourceCell;
        var steps = Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y));
        for (var step = 1; step < steps; step++)
        {
            var x = sourceCell.X + Mathf.RoundToInt(delta.X * (step / (float)steps));
            var y = sourceCell.Y + Mathf.RoundToInt(delta.Y * (step / (float)steps));
            if (!_config.FloorRule.CanOccupy(new Vector2I(x, y))) return false;
        }
        return true;
    }

    private bool CanOccupy(Vector2I cell)
    {
        if (cell.X < 0 || cell.X >= Width || cell.Y < 0 || cell.Y >= Height) return false;
        if (!_config.FloorRule.CanOccupy(cell)) return false;
        return _movement?.IsReserved(cell) != true && _units.All(unit => !unit.Alive || unit.Cell != cell);
    }

    private Vector2I FindOpenNear(Vector2I origin, int team)
    {
        var directions = new[] { Vector2I.Down, Vector2I.Up, Vector2I.Left, Vector2I.Right };
        foreach (var direction in directions)
        {
            var cell = origin + direction;
            if (CanOccupy(cell)) return cell;
        }
        for (var y = 0; y < Height; y++)
            for (var x = team == 0 ? 0 : Width - 1; x >= 0 && x < Width; x += team == 0 ? 1 : -1)
                if (CanOccupy(new Vector2I(x, y))) return new Vector2I(x, y);
        return origin;
    }

    private bool TryFindOpenNear(Vector2I origin, int team, IReadOnlySet<Vector2I> reserved, out Vector2I result)
    {
        var directions = new[] { Vector2I.Down, Vector2I.Up, Vector2I.Left, Vector2I.Right };
        foreach (var direction in directions)
        {
            var cell = origin + direction;
            if (!reserved.Contains(cell) && CanOccupy(cell))
            {
                result = cell;
                return true;
            }
        }
        for (var y = 0; y < Height; y++)
        for (var x = team == 0 ? 0 : Width - 1; x >= 0 && x < Width; x += team == 0 ? 1 : -1)
        {
            var cell = new Vector2I(x, y);
            if (reserved.Contains(cell) || !CanOccupy(cell)) continue;
            result = cell;
            return true;
        }
        result = default;
        return false;
    }

    private bool SpawnTemporary(
        UnitSnapshot? snapshot,
        int team,
        Vector2I cell,
        float healthScale,
        float damageScale,
        string sourceRuntimeId = "")
    {
        if (snapshot is null || !CanOccupy(cell)) return false;
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        var runtimeId = $"s-{team}-{_summonCounter++}";
        var maxHealth = snapshot.MaxHealth * healthScale;
        var damage = snapshot.Damage * damageScale;
        var lifeSteal = snapshot.LifeSteal;
        var healthRatios = _units.ToDictionary(
            existing => existing.RuntimeId,
            existing => existing.MaxHealth <= 0 ? 1f : existing.Health / existing.MaxHealth,
            StringComparer.Ordinal);
        var unit = new BattleUnitState
        {
            RuntimeId = runtimeId,
            SourceInstanceId = string.Empty,
            Definition = snapshot,
            Attributes = _attributeScope.CreateSet(
                runtimeId,
                CreateBattleAttributeDefinition(snapshot, maxHealth, damage, lifeSteal)),
            Team = team,
            Cell = cell,
            Health = maxHealth,
            IsTemporary = true
        };
        _traitScope?.AddOwnerAndContributions(
            new TraitOwnerBinding(runtimeId, team, unit.Attributes),
            (snapshot.TraitContributions.IsDefault
                    ? ImmutableArray<CompiledTraitContribution>.Empty
                    : snapshot.TraitContributions)
                .Select(contribution => new TraitContributionInput(
                    contribution.TraitId,
                    contribution.Value,
                    team,
                    TraitContributionSourceKind.Hero,
                    runtimeId,
                    runtimeId,
                    snapshot.ContentId,
                    false,
                    true,
                    true)));
        foreach (var existing in _units)
            existing.Health = existing.MaxHealth * healthRatios[existing.RuntimeId];
        unit.Health = unit.MaxHealth;
        _units.Add(unit);
        _statistics.Add(unit.RuntimeId, new BattleUnitStatistics { JoinTick = TickIndex });
        Emit("summoned", unit.RuntimeId, "", 0, cell, "skill_cast");
        PublishCombat(new BattleCombatEventDraft(
            BattleCombatEventKind.UnitSummoned,
            ResolveCombatSource(sourceRuntimeId),
            sourceRuntimeId,
            unit.RuntimeId,
            TickIndex,
            Cell: ToCombatCell(cell),
            SubjectStableId: snapshot.ContentId));
        resolution.Commit();
        return true;
    }

    private void ApplyPeriodicBehavior(BattleUnitState unit)
    {
        var behavior = unit.Definition.Behavior;
        if (behavior.PeriodicShieldTicks > 0 && TickIndex % behavior.PeriodicShieldTicks == 0)
        {
            ApplyShield(unit.RuntimeId, unit, behavior.PeriodicShieldAmount);
            Emit("shield", unit.RuntimeId, unit.RuntimeId, behavior.PeriodicShieldAmount, unit.Cell, "skill_cast");
        }
        if (behavior.PeriodicSummonTicks > 0 && TickIndex % behavior.PeriodicSummonTicks == 0 &&
            (behavior.PeriodicSummonLimit <= 0 || _units.Count(other => other.Team == unit.Team && other.IsTemporary && other.Alive) < behavior.PeriodicSummonLimit))
            SpawnTemporary(unit.BehaviorSummon, unit.Team, FindOpenNear(unit.Cell, unit.Team), .65f, .7f, unit.RuntimeId);
    }

    private BattleRuleContext CreateRuleContext() => new(
        TickIndex, _units, Allies,
        (source, target, amount) => ApplyFloorDamage(source, target, amount),
        (target, amount) => ApplyFloorHeal(target, amount),
        Emit, BeaconControlled);

    private void ApplyFloorDamage(string sourceRuntimeId, BattleUnitState target, float amount)
    {
        _effectCompatibility.Damage(sourceRuntimeId, target.RuntimeId, amount, TickIndex);
    }

    private void ApplyFloorHeal(BattleUnitState target, float amount)
    {
        _effectCompatibility.FloorHeal(target.RuntimeId, amount, TickIndex);
    }

    private EffectWorldSnapshot CaptureEffectSnapshot(int tick) => EffectWorldSnapshot.Create(
        tick,
        _units.Select(unit => new EffectEntitySnapshot(
            unit.RuntimeId,
            unit.Team,
            unit.Alive,
            unit.Health,
            unit.MaxHealth,
            unit.Shield,
            unit.Definition.Tags.ToImmutableArray())));

    private EffectCommitOutcome CommitCompatibilityMutation(PreparedEffectMutation mutation)
    {
        var target = _units.FirstOrDefault(unit => unit.RuntimeId == mutation.Request.TargetId);
        if (target is null)
            return EffectCommitOutcome.Skipped(EffectInterruptionReason.TargetUnavailable, "Battle target no longer exists.");
        var amount = mutation.Modifiers.ResolvedAmount;
        switch (mutation.Request.Kind)
        {
            case EffectKind.Damage:
                var beforeDamage = target.Health + target.Shield;
                // The migrated floor-rule delegate historically attributed by runtime id while
                // supplying no concrete source unit. Preserve that distinction until the complete
                // attack/death-chain ordering contract migrates together.
                ApplyDamage(mutation.Request.Context.SourceId, null, target, amount);
                return EffectCommitOutcome.Succeeded(
                    amount,
                    Math.Max(0, beforeDamage - target.Health - target.Shield),
                    EffectDomainEventKind.DamageResolved);
            case EffectKind.Heal:
                var effectiveHealing = HealLiving(mutation.Request.Context.SourceId, target, amount);
                return EffectCommitOutcome.Succeeded(amount, effectiveHealing, EffectDomainEventKind.HealingResolved);
            case EffectKind.Shield:
                if (!target.Alive)
                    return EffectCommitOutcome.Skipped(EffectInterruptionReason.TargetUnavailable, "Battle target is defeated.");
                var effectiveShield = ApplyShield(mutation.Request.Context.SourceId, target, amount);
                return EffectCommitOutcome.Succeeded(effectiveShield, effectiveShield, EffectDomainEventKind.ShieldResolved);
            default:
                return EffectCommitOutcome.Failed(
                    $"Battle compatibility port does not own {mutation.Request.Kind} mutations.");
        }
    }

    private float HealLiving(string sourceRuntimeId, BattleUnitState target, float amount)
    {
        if (!target.Alive || amount <= 0) return 0;
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        var combatSource = ResolveCombatSource(sourceRuntimeId);
        var calculated = _combatPipeline.Resolve(new BattleCombatCalculationRequest(
            BattleCombatCalculationKind.Healing,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            TickIndex,
            amount));
        var before = target.Health;
        target.Health = Math.Min(target.MaxHealth, target.Health + calculated.ResolvedAmount);
        var effectiveHealing = target.Health - before;
        if (effectiveHealing > 0 && _statistics.TryGetValue(sourceRuntimeId, out var sourceStatistics))
        {
            sourceStatistics.HealingDone += effectiveHealing;
            sourceStatistics.EffectiveHealingEvents++;
        }
        PublishCombat(new BattleCombatEventDraft(
            BattleCombatEventKind.HealingResolved,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            TickIndex,
            RequestedValue: calculated.RequestedAmount,
            AppliedValue: calculated.ResolvedAmount,
            EffectiveValue: effectiveHealing,
            Cell: ToCombatCell(target.Cell)));
        resolution.Commit();
        return effectiveHealing;
    }

    private float ApplyShield(string sourceRuntimeId, BattleUnitState target, float amount)
    {
        if (!target.Alive || amount <= 0) return 0;
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        var combatSource = ResolveCombatSource(sourceRuntimeId);
        var calculated = _combatPipeline.Resolve(new BattleCombatCalculationRequest(
            BattleCombatCalculationKind.Shield,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            TickIndex,
            amount));
        target.Shield += calculated.ResolvedAmount;
        PublishCombat(new BattleCombatEventDraft(
            BattleCombatEventKind.ShieldResolved,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            TickIndex,
            RequestedValue: calculated.RequestedAmount,
            AppliedValue: calculated.ResolvedAmount,
            EffectiveValue: calculated.ResolvedAmount,
            Cell: ToCombatCell(target.Cell)));
        resolution.Commit();
        return calculated.ResolvedAmount;
    }

    private void HandleDeath(BattleUnitState? source, BattleUnitState target)
    {
        if (!_deathProcUnits.Add(target.RuntimeId)) return;
        foreach (var unit in _units.Where(unit => unit.ActionTargetRuntimeId == target.RuntimeId))
            ClearActionTarget(unit);
        _movement?.ReleaseUnit(target.RuntimeId);
        _statusScope.HandleOwnerDeath(target.RuntimeId);
        if (target.Definition.Behavior.OnDeathDamage > 0)
            foreach (var enemy in Allies(1 - target.Team).Where(enemy => Distance(enemy.Cell, target.Cell) <= 1.5f).ToArray())
                ApplyDamage(target.RuntimeId, null, enemy, target.Definition.Behavior.OnDeathDamage);
        if (target.Team == 0 && _config.HeroRule.SummonOnAllyDeath && !target.Definition.IsHero && !target.IsTemporary)
            SpawnTemporary(_config.Summons.DeathSummon, 0, target.Cell, .6f, .65f, target.RuntimeId);
        if (source is { Team: 0 } && _config.HeroRule.KillGrowth > 0 &&
            (string.IsNullOrWhiteSpace(_config.HeroRule.RequiredSoldierTag) || source.Definition.Tags.Contains(_config.HeroRule.RequiredSoldierTag)))
            foreach (var ally in Allies(0).Where(ally => string.IsNullOrWhiteSpace(_config.HeroRule.RequiredSoldierTag) || ally.Definition.Tags.Contains(_config.HeroRule.RequiredSoldierTag)))
                ally.Attributes.ApplyModifier(
                    new CompiledAttributeModifier(
                        CombatAttribute.AttackDamage,
                        AttributeModifierOperation.Multiply,
                        new CompiledConstantMagnitude(1f + _config.HeroRule.KillGrowth),
                        0,
                        $"kill_{target.RuntimeId}"),
                    new CombatSourceRef(
                        CombatSourceKind.System,
                        "legacy_kill_growth",
                        source.RuntimeId,
                        $"{source.RuntimeId}:{target.RuntimeId}"));
    }

    private float EffectiveDamage(BattleUnitState unit)
    {
        var multiplier = 1f;
        var adjacent = Allies(unit.Team).Where(ally => ally != unit && Distance(ally.Cell, unit.Cell) <= 1.5f).ToArray();
        if (adjacent.Length > 0)
        {
            if (unit.Team == 0)
            {
                multiplier *= 1f + _config.HeroRule.FormationDamageBonus;
                multiplier *= _config.Modifiers.FormationAdjacentDamageMultiplier;
            }
            multiplier *= 1f + adjacent.Sum(ally => ally.Definition.Behavior.AdjacentDamageAura);
        }
        return unit.Damage * multiplier;
    }

    private float EffectiveArmor(BattleUnitState unit)
    {
        var armor = unit.Armor;
        var adjacent = Allies(unit.Team).Where(ally => ally != unit && Distance(ally.Cell, unit.Cell) <= 1.5f).ToArray();
        if (adjacent.Length > 0)
        {
            if (unit.Team == 0)
                armor += _config.HeroRule.FormationArmorBonus + _config.Modifiers.FormationAdjacentArmor;
            armor += adjacent.Sum(ally => ally.Definition.Behavior.AdjacentArmorAura);
        }
        return armor;
    }

    private IEnumerable<BattleUnitState> Allies(int team) => _units.Where(unit => unit.Team == team && unit.Alive);

    private static void SetActionTarget(BattleUnitState unit, BattleUnitState target)
    {
        unit.ActionTargetRuntimeId = target.RuntimeId;
        unit.ActionTargetName = target.Definition.DisplayName;
    }

    private static void ClearActionTarget(BattleUnitState unit)
    {
        unit.ActionTargetRuntimeId = string.Empty;
        unit.ActionTargetName = string.Empty;
    }

    private void ResolveOutcome()
    {
        var playerHeroAlive = _units.Any(unit =>
            unit.Team == 0 && unit.IsPersistentRosterHero && !unit.IsTemporary && unit.Alive);
        var enemyAlive = _units.Any(unit => unit.Team == 1 && unit.Alive);
        if (!playerHeroAlive) Outcome = BattleOutcome.PlayerDefeat;
        else if (!enemyAlive) Outcome = BattleOutcome.PlayerVictory;
        else if (TickIndex >= MaxTicks) Outcome = BattleOutcome.Timeout;
        if (Outcome != BattleOutcome.Running)
        {
            try
            {
                EndFloorRule(Outcome);
                CompleteBattleScopes(Outcome switch
                {
                    BattleOutcome.PlayerVictory => BattleScopeCompletionReason.PlayerVictory,
                    BattleOutcome.PlayerDefeat => BattleScopeCompletionReason.PlayerDefeat,
                    _ => BattleScopeCompletionReason.Timeout
                });
            }
            catch
            {
                CompleteBattleScopes(BattleScopeCompletionReason.Exception);
                throw;
            }
            Emit("battle_finished", "", "", (float)Outcome, new Vector2I(), "idle");
        }
    }

    private void EndAfterFailure()
    {
        if (Outcome == BattleOutcome.Running) Outcome = BattleOutcome.Timeout;
        try
        {
            EndFloorRule(Outcome);
        }
        finally
        {
            CompleteBattleScopes(BattleScopeCompletionReason.Exception);
        }
    }

    private void CompleteBattleScopes(BattleScopeCompletionReason reason)
    {
        Exception? failure = null;
        void Finish(Action action)
        {
            try { action(); }
            catch (Exception exception) { failure ??= exception; }
        }

        // Unit reports are the immutable combat-end projection. Capture them once before
        // Equipment, Status, or Attribute cleanup can revert combat-time values.
        Finish(() =>
        {
            if (_terminalUnitReports.IsDefault) _terminalUnitReports = BuildUnitReports();
        });
        _bossPhaseIndexes.Clear();
        Finish(() => _effectCompatibility.Complete(reason, TickIndex));
        Finish(() => _tacticalCommandScope?.Complete(reason switch
        {
            BattleScopeCompletionReason.Abort => TacticalCommandScopeCompletionReason.Abort,
            BattleScopeCompletionReason.Replacement => TacticalCommandScopeCompletionReason.Replacement,
            BattleScopeCompletionReason.Exception => TacticalCommandScopeCompletionReason.Exception,
            BattleScopeCompletionReason.Disposal => TacticalCommandScopeCompletionReason.Disposal,
            _ => TacticalCommandScopeCompletionReason.BattleCompleted
        }, TickIndex));
        Finish(() => _abilityScope?.Complete(reason switch
        {
            BattleScopeCompletionReason.Abort => AbilityScopeCompletionReason.Abort,
            BattleScopeCompletionReason.Replacement => AbilityScopeCompletionReason.Replacement,
            BattleScopeCompletionReason.Exception => AbilityScopeCompletionReason.Exception,
            BattleScopeCompletionReason.Disposal => AbilityScopeCompletionReason.Disposal,
            _ => AbilityScopeCompletionReason.BattleCompleted
        }, TickIndex));
        Finish(() =>
        {
            if (_statusScope.Transition is not null) return;
            using var resolution = _combatPipeline.BeginAuthoritativeResolution();
            _statusScope.Complete(reason switch
            {
                BattleScopeCompletionReason.Abort => StatusScopeCompletionReason.Abort,
                BattleScopeCompletionReason.Replacement => StatusScopeCompletionReason.Replacement,
                BattleScopeCompletionReason.Exception => StatusScopeCompletionReason.Exception,
                BattleScopeCompletionReason.Disposal => StatusScopeCompletionReason.Disposal,
                _ => StatusScopeCompletionReason.BattleCompleted
            }, TickIndex);
            resolution.Commit();
        });
        Finish(() => _relicScope?.Complete(reason switch
        {
            BattleScopeCompletionReason.PlayerVictory => RelicBattleCompletionReason.PlayerVictory,
            BattleScopeCompletionReason.PlayerDefeat => RelicBattleCompletionReason.PlayerDefeat,
            BattleScopeCompletionReason.Timeout => RelicBattleCompletionReason.Timeout,
            BattleScopeCompletionReason.Abort => RelicBattleCompletionReason.Abort,
            BattleScopeCompletionReason.Replacement => RelicBattleCompletionReason.Replacement,
            BattleScopeCompletionReason.Exception => RelicBattleCompletionReason.Exception,
            _ => RelicBattleCompletionReason.Disposal
        }));
        Finish(() => _equipmentScope?.Complete(reason switch
        {
            BattleScopeCompletionReason.Abort => EquipmentBattleCompletionReason.Abort,
            BattleScopeCompletionReason.Replacement => EquipmentBattleCompletionReason.Replacement,
            BattleScopeCompletionReason.Exception => EquipmentBattleCompletionReason.Exception,
            BattleScopeCompletionReason.Disposal => EquipmentBattleCompletionReason.Disposal,
            _ => EquipmentBattleCompletionReason.BattleCompleted
        }));
        Finish(() => _traitScope?.Complete(reason switch
        {
            BattleScopeCompletionReason.Abort => TraitBattleCompletionReason.Abort,
            BattleScopeCompletionReason.Replacement => TraitBattleCompletionReason.Replacement,
            BattleScopeCompletionReason.Exception => TraitBattleCompletionReason.Exception,
            BattleScopeCompletionReason.Disposal => TraitBattleCompletionReason.Disposal,
            _ => TraitBattleCompletionReason.BattleCompleted
        }));
        Finish(() => _attributeScope.Complete(reason switch
        {
            BattleScopeCompletionReason.Abort => AttributeScopeCompletionReason.Abort,
            BattleScopeCompletionReason.Replacement => AttributeScopeCompletionReason.Replacement,
            BattleScopeCompletionReason.Exception => AttributeScopeCompletionReason.Exception,
            BattleScopeCompletionReason.Disposal => AttributeScopeCompletionReason.Disposal,
            _ => AttributeScopeCompletionReason.BattleCompleted
        }, TickIndex));
        Finish(() => _combatPipeline.Complete(reason switch
        {
            BattleScopeCompletionReason.PlayerVictory => BattleCombatCompletionReason.PlayerVictory,
            BattleScopeCompletionReason.PlayerDefeat => BattleCombatCompletionReason.PlayerDefeat,
            BattleScopeCompletionReason.Timeout => BattleCombatCompletionReason.Timeout,
            BattleScopeCompletionReason.Abort => BattleCombatCompletionReason.Abort,
            BattleScopeCompletionReason.Replacement => BattleCombatCompletionReason.Replacement,
            BattleScopeCompletionReason.Exception => BattleCombatCompletionReason.Exception,
            _ => BattleCombatCompletionReason.Disposal
        }, TickIndex));
        if (failure is not null) throw failure;
    }

    private void EndFloorRule(BattleOutcome outcome)
    {
        if (_floorRuleEnded) return;
        _floorRuleEnded = true;
        _config.FloorRule.OnBattleEnded(CreateRuleContext(), outcome);
    }

    private static CompiledAttributeSetDefinition CreateBattleAttributeDefinition(
        UnitSnapshot unit,
        float maxHealth,
        float damage,
        float lifeSteal)
    {
        var definition = unit.AttributeDefinition ?? AttributeDefinitionCompiler.Legacy(
            new Dictionary<CombatAttribute, float>
            {
                [CombatAttribute.MaxHealth] = unit.MaxHealth,
                [CombatAttribute.AttackDamage] = unit.Damage,
                [CombatAttribute.SpellPower] = 0,
                [CombatAttribute.AttackSpeed] = 1,
                [CombatAttribute.Armor] = unit.Armor,
                [CombatAttribute.MagicResistance] = 0,
                [CombatAttribute.AttackRange] = unit.Range,
                [CombatAttribute.MoveSpeed] = 1,
                [CombatAttribute.CriticalChance] = 0,
                [CombatAttribute.CriticalDamage] = 1.5f,
                [CombatAttribute.MaxMana] = 0,
                [CombatAttribute.StartingMana] = 0,
                [CombatAttribute.HealingPower] = unit.HealPower,
                [CombatAttribute.LifeSteal] = unit.LifeSteal,
                [CombatAttribute.ControlResistance] = 0
            });
        return AttributeDefinitionCompiler.WithBaseValues(
            definition,
            new Dictionary<CombatAttribute, float>
            {
                [CombatAttribute.MaxHealth] = maxHealth,
                [CombatAttribute.AttackDamage] = damage,
                [CombatAttribute.Armor] = unit.Armor,
                [CombatAttribute.AttackRange] = unit.Range,
                [CombatAttribute.HealingPower] = unit.HealPower,
                [CombatAttribute.LifeSteal] = lifeSteal
            });
    }

    private CombatSourceRef ResolveCombatSource(string runtimeId, BattleUnitState? explicitUnit = null)
    {
        if (string.IsNullOrWhiteSpace(runtimeId)) return CombatSourceRef.None;
        var unit = explicitUnit ?? _units.FirstOrDefault(candidate => candidate.RuntimeId == runtimeId);
        return unit is null
            ? CombatSourceRef.System(runtimeId)
            : CombatSourceRef.Unit(
                unit.Definition.ContentId,
                unit.RuntimeId,
                string.IsNullOrWhiteSpace(unit.SourceInstanceId) ? unit.RuntimeId : unit.SourceInstanceId);
    }

    private void PublishCombat(BattleCombatEventDraft draft)
    {
        var result = _combatPipeline.Publish(draft);
        if (!result.Accepted)
            throw new InvalidOperationException($"Combat event '{draft.Kind}' was rejected: {result.Message}");
    }

    private static CombatCell ToCombatCell(Vector2I cell) => new(cell.X, cell.Y);

    private void Emit(string type, string source, string target, float value, Vector2I cell, string cue)
    {
        var battleEvent = new BattleEvent(TickIndex, type, source, target, value, cell, cue);
        _events.Add(battleEvent);
        _digest.Append(TickIndex).Append('|').Append(type).Append('|').Append(source).Append('|').Append(target).Append('|')
            .Append(value.ToString("0.###", CultureInfo.InvariantCulture)).Append('|').Append(cell.X).Append(',').Append(cell.Y).Append(';');
    }

    private static float Distance(Vector2I a, Vector2I b) => a.DistanceTo(b);
    private static Vector2I ClampCell(Vector2I cell) => new(Math.Clamp(cell.X, 0, Width - 1), Math.Clamp(cell.Y, 0, Height - 1));

    private sealed class BattleUnitStatistics
    {
        public float DamageDealt { get; set; }
        public float DamageTaken { get; set; }
        public float ShieldAbsorbed { get; set; }
        public float HealingDone { get; set; }
        public int Kills { get; set; }
        public int JoinTick { get; init; }
        public int? DefeatTick { get; set; }
        public int AttackActions { get; set; }
        public int EffectiveHealingEvents { get; set; }
    }

    private sealed class BattleWorldStateCheckpoint
    {
        private readonly BattleSimulation _owner;
        private readonly BattleAttributeScope.ScopeStateCheckpoint _attributeState;
        private readonly BattleCombatEventPipeline.CombatStateCheckpoint _combatState;
        private readonly BattleEffectScope.EffectStateCheckpoint _effectState;
        private readonly BattleStatusScope.WorldStateCheckpoint _statusState;
        private readonly BattleTraitScope.TraitStateCheckpoint _traitState;
        private readonly RelicBattleScope.RelicStateCheckpoint? _relicState;
        private readonly DeterministicGridMovementService? _movementOwner;
        private readonly DeterministicGridMovementService.MovementStateCheckpoint? _movementState;
        private readonly BattleUnitMutableState[] _unitStates;
        private readonly int _eventCount;
        private readonly int _digestLength;
        private readonly HashSet<string> _deathProcUnits;
        private readonly Dictionary<string, BattleUnitStatisticsState> _statistics;
        private readonly Dictionary<string, int> _bossPhaseIndexes;
        private readonly int _statusPresentationCueCount;
        private readonly ImmutableArray<BattleUnitReportSnapshot> _terminalUnitReports;
        private readonly int _summonCounter;
        private readonly bool _floorRuleStartAttempted;
        private readonly bool _floorRuleEnded;
        private readonly int _tickIndex;
        private readonly BattleOutcome _outcome;
        private readonly int _goldSpent;
        private readonly int _successfulTacticalCommandUses;
        private bool _finished;

        internal BattleWorldStateCheckpoint(BattleSimulation owner)
        {
            _owner = owner;
            _attributeState = owner._attributeScope.CaptureState();
            _combatState = owner._combatPipeline.CaptureState();
            _effectState = owner._effectCompatibility.CaptureState();
            _traitState = owner._traitScope!.CaptureState();
            _relicState = owner._relicScope?.CaptureState();
            _movementOwner = owner._movement as DeterministicGridMovementService;
            _movementState = _movementOwner?.CaptureState();
            _unitStates = owner._units.Select(unit => new BattleUnitMutableState(unit)).ToArray();
            // Legacy events, digest text, and presentation cues append during an Ability
            // commit. Store their boundaries instead of cloning the complete Battle history.
            _eventCount = owner._events.Count;
            _digestLength = owner._digest.Length;
            _deathProcUnits = new HashSet<string>(owner._deathProcUnits, StringComparer.Ordinal);
            _statistics = owner._statistics.ToDictionary(
                pair => pair.Key,
                pair => new BattleUnitStatisticsState(pair.Value),
                StringComparer.Ordinal);
            _bossPhaseIndexes = new Dictionary<string, int>(owner._bossPhaseIndexes, StringComparer.Ordinal);
            _statusPresentationCueCount = owner._statusPresentationCues.Count;
            _terminalUnitReports = owner._terminalUnitReports;
            _summonCounter = owner._summonCounter;
            _floorRuleStartAttempted = owner._floorRuleStartAttempted;
            _floorRuleEnded = owner._floorRuleEnded;
            _tickIndex = owner.TickIndex;
            _outcome = owner.Outcome;
            _goldSpent = owner.GoldSpent;
            _successfulTacticalCommandUses = owner.SuccessfulTacticalCommandUses;
            // Status is last because beginning its world transaction is the only capture step
            // that changes runtime coordination state.
            _statusState = owner._statusScope.BeginWorldTransaction();
        }

        internal void Commit()
        {
            if (_finished) return;
            _statusState.Commit();
            _finished = true;
        }

        internal void Rollback()
        {
            if (_finished) return;
            Exception? failure = null;
            void Restore(Action action)
            {
                try { action(); }
                catch (Exception exception) { failure ??= exception; }
            }

            // Status rollback first disposes only subscriptions created by this transaction.
            // Combat state is restored last so no rollback callback can publish or advance it.
            Restore(_statusState.Rollback);
            Restore(() => _owner._traitScope!.RestoreState(_traitState));
            if (_owner._relicScope is not null && _relicState is not null)
                Restore(() => _owner._relicScope.RestoreState(_relicState));
            Restore(() => _owner._attributeScope.RestoreState(_attributeState));
            Restore(RestoreBattleState);
            if (_movementOwner is not null && _movementState is not null)
                Restore(() => _movementOwner.RestoreState(_movementState));
            Restore(() => _owner._effectCompatibility.RestoreState(_effectState));
            Restore(() => _owner._combatPipeline.RestoreState(_combatState));
            _finished = true;
            if (failure is not null) throw failure;
        }

        private void RestoreBattleState()
        {
            if (_owner._events.Count < _eventCount ||
                _owner._digest.Length < _digestLength ||
                _owner._statusPresentationCues.Count < _statusPresentationCueCount)
                throw new InvalidOperationException("Battle append-only history changed before rollback.");

            _owner._units.Clear();
            foreach (var state in _unitStates)
            {
                state.Restore();
                _owner._units.Add(state.Unit);
            }
            if (_owner._events.Count > _eventCount)
                _owner._events.RemoveRange(_eventCount, _owner._events.Count - _eventCount);
            _owner._digest.Length = _digestLength;
            _owner._deathProcUnits.Clear();
            foreach (var runtimeId in _deathProcUnits) _owner._deathProcUnits.Add(runtimeId);
            _owner._statistics.Clear();
            foreach (var pair in _statistics)
                _owner._statistics.Add(pair.Key, pair.Value.Restore());
            _owner._bossPhaseIndexes.Clear();
            foreach (var pair in _bossPhaseIndexes) _owner._bossPhaseIndexes.Add(pair.Key, pair.Value);
            if (_owner._statusPresentationCues.Count > _statusPresentationCueCount)
                _owner._statusPresentationCues.RemoveRange(
                    _statusPresentationCueCount,
                    _owner._statusPresentationCues.Count - _statusPresentationCueCount);
            _owner._terminalUnitReports = _terminalUnitReports;
            _owner._summonCounter = _summonCounter;
            _owner._floorRuleStartAttempted = _floorRuleStartAttempted;
            _owner._floorRuleEnded = _floorRuleEnded;
            _owner.TickIndex = _tickIndex;
            _owner.Outcome = _outcome;
            _owner.GoldSpent = _goldSpent;
            _owner.SuccessfulTacticalCommandUses = _successfulTacticalCommandUses;
        }
    }

    private sealed record BattleUnitMutableState(
        BattleUnitState Unit,
        Vector2I Cell,
        float Health,
        float Shield,
        int AttackCooldown,
        int MoveCooldown,
        int DisabledTicks,
        int WaitingTicks,
        ImmutableArray<StatusRuntimeSnapshot> Statuses,
        string BossPhaseId,
        BattleUnitMode Mode,
        BattleActionKind LastActionKind,
        string ActionTargetRuntimeId,
        string ActionTargetName)
    {
        internal BattleUnitMutableState(BattleUnitState unit) : this(
            unit,
            unit.Cell,
            unit.Health,
            unit.Shield,
            unit.AttackCooldown,
            unit.MoveCooldown,
            unit.DisabledTicks,
            unit.WaitingTicks,
            unit.Statuses,
            unit.BossPhaseId,
            unit.Mode,
            unit.LastActionKind,
            unit.ActionTargetRuntimeId,
            unit.ActionTargetName)
        {
        }

        internal void Restore()
        {
            Unit.Cell = Cell;
            Unit.Health = Health;
            Unit.Shield = Shield;
            Unit.AttackCooldown = AttackCooldown;
            Unit.MoveCooldown = MoveCooldown;
            Unit.DisabledTicks = DisabledTicks;
            Unit.WaitingTicks = WaitingTicks;
            Unit.Statuses = Statuses;
            Unit.BossPhaseId = BossPhaseId;
            Unit.Mode = Mode;
            Unit.LastActionKind = LastActionKind;
            Unit.ActionTargetRuntimeId = ActionTargetRuntimeId;
            Unit.ActionTargetName = ActionTargetName;
        }
    }

    private sealed record BattleUnitStatisticsState(
        float DamageDealt,
        float DamageTaken,
        float ShieldAbsorbed,
        float HealingDone,
        int Kills,
        int JoinTick,
        int? DefeatTick,
        int AttackActions,
        int EffectiveHealingEvents)
    {
        internal BattleUnitStatisticsState(BattleUnitStatistics statistics) : this(
            statistics.DamageDealt,
            statistics.DamageTaken,
            statistics.ShieldAbsorbed,
            statistics.HealingDone,
            statistics.Kills,
            statistics.JoinTick,
            statistics.DefeatTick,
            statistics.AttackActions,
            statistics.EffectiveHealingEvents)
        {
        }

        internal BattleUnitStatistics Restore() => new()
        {
            DamageDealt = DamageDealt,
            DamageTaken = DamageTaken,
            ShieldAbsorbed = ShieldAbsorbed,
            HealingDone = HealingDone,
            Kills = Kills,
            JoinTick = JoinTick,
            DefeatTick = DefeatTick,
            AttackActions = AttackActions,
            EffectiveHealingEvents = EffectiveHealingEvents
        };
    }
}
