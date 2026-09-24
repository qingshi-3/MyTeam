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

public sealed partial class BattleSimulation : IDisposable, IAbilityRuntimeWorld
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
    private IContinuousMovementService? _movement;
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
    // Live UI reads an immutable report projection without ending combat or rebuilding its digest.
    public ImmutableArray<BattleUnitReportSnapshot> ReadUnitReports() =>
        _terminalUnitReports.IsDefault ? BuildUnitReports() : _terminalUnitReports;
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
            reactiveEffectSink: ExecuteStatusEffectNow,
            magnitudeContextFactory: request => new BattleAttributeMagnitudeContext(
                request.SourceAttributes, request.TargetAttributes,
                contextValue: key => throw new InvalidOperationException($"Status context key '{key}' has no battle binding."),
                teamCount: TeamCount,
                traitValue: (traitId, team) => CurrentTraitSnapshot.Value(traitId, team)));
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
                var resolvedCell = CanOccupy(requestedCell) ? requestedCell : FindOpenCellNear(requestedCell, spawn.Team);
                // Placement cells are anchors, not one-cell bodies. Resolve the full authored circle
                // against terrain and previously placed bodies before publishing any battle state.
                var anchor = BattlefieldSpace.CellCenter(resolvedCell);
                anchor = new Vector2(Math.Clamp(anchor.X, -.5f + unit.BodyRadius, Width - .5f - unit.BodyRadius),
                    Math.Clamp(anchor.Y, -.5f + unit.BodyRadius, Height - .5f - unit.BodyRadius));
                var resolvedPosition = FindOpenNear(anchor, spawn.Team, unit.BodyRadius)
                    ?? throw new InvalidOperationException($"No legal space for unit '{unit.ContentId}'.");
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
                    InitialTeam = spawn.Team,
                    Cell = resolvedCell,
                    Position = resolvedPosition,
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
                _units.Select(unit => new EquipmentOwnerBinding(
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
                    unit.Attributes, unit.SourceInstanceId)), CreateStatusGrantContext());
            var equipmentBindings = new BattleCombatBindingRegistry(_combatPipeline);
            try
            {
                _equipmentScope.Activate(new EquipmentBattleRuntimeContext
                {
                    CombatBindings = equipmentBindings,
                    CanReceiveStatus = runtimeId => _units.Any(unit =>
                        unit.RuntimeId == runtimeId && unit.Alive),
                    ApplyStatuses = applications => _statusScope.ApplyBatch(applications),
                    StatusGrants = CreateStatusGrantContext()
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
                        ExecuteAttributedEffect = ExecuteRelicEffect,
                        Summon = SummonRelic,
                        CurrentTick = () => TickIndex,
                        EmptyDeploymentSlots = config.EmptyDeploymentSlots,
                        StatusGrants = CreateStatusGrantContext()
                    });
                }
                finally { relicBindings.CloseRegistration(); }
            }
            // All initial grant carriers project before input health ratios and mana are
            // initialized. Their gameplay effects wait until this one-time normalization.
            InitializeAbilityScope(healthRatios);
            foreach (var unit in _units.Where(unit => unit.Alive).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray())
                ActivatePassiveGrants(unit.RuntimeId);
            foreach (var unit in _units)
            {
                unit.Health = unit.MaxHealth * healthRatios.GetValueOrDefault(unit.RuntimeId, 1f);
                BattleHeroMana.Initialize(unit);
            }
            if (config.TacticalCommands is not null)
                _tacticalCommandScope = new BattleTacticalCommandScope(
                    $"battle_{config.Seed}_tactical_commands",
                    this,
                    config.TacticalCommands);
            ExecuteInitialGrantEffects();
            // Typed setup starts here. Legacy BattleEvent/digest publication remains in its
            // historical position after configured setup mutations and battle-start abilities.
            PublishCombat(new BattleCombatEventDraft(
                BattleCombatEventKind.BattleStarted,
                CombatSourceRef.System("battle"),
                string.Empty,
                string.Empty,
                0,
                Cell: ToCombatCell(new Vector2I(Width / 2, Height / 2)),
                Position: ToCombatPoint(BattlefieldSpace.CellCenter(new Vector2I(Width / 2, Height / 2)))));
            AddConfiguredSummons();
            _floorRuleStartAttempted = true;
            _config.FloorRule.OnBattleStarted(CreateRuleContext());
            _movement = new DeterministicContinuousMovementService(
                Width, Height, () => _units, cell => _config.FloorRule.CanOccupy(cell), HasLineAccess, config.Seed,
                isGrounded: unit => !IsAirborne(unit), landingReservations: CaptureDisplacementReservations);
            ActivateBattleStartedAbilities();
            DrainAbilityReactions();
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
            AdvanceBattleLifecycles();
            AdvanceTechniques();
            AdvanceEnemyActions();
            DrainAbilityReactions();
            var previousPositions = _units.ToDictionary(unit => unit.RuntimeId, unit => unit.Position, StringComparer.Ordinal);
            AdvanceTramples();
            AdvanceDisplacements(previousPositions);
            DrainAbilityReactions();
            _movement!.BeginTick();
            ApplyFloorRule();
            foreach (var unit in _units.Where(unit => unit.Alive).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray())
            {
                Act(unit);
                DrainAbilityReactions();
            }
            using (var movementResolution = _combatPipeline.BeginAuthoritativeResolution())
            {
                _movement.ResolveIntents((unit, position) =>
                {
                    Emit("move", unit.RuntimeId, "", 0, position, "move");
                    PublishCombat(new BattleCombatEventDraft(
                        BattleCombatEventKind.UnitMoved,
                        ResolveCombatSource(unit.RuntimeId, unit),
                        unit.RuntimeId,
                        string.Empty,
                        TickIndex,
                        Cell: ToCombatCell(BattlefieldSpace.PositionToCell(position)),
                        Position: ToCombatPoint(position)));
                });
                movementResolution.Commit();
            }
            AdvanceProjectiles(previousPositions);
            AdvanceHookFlights(previousPositions);
            AdvanceReturningBlades(previousPositions);
            DrainAbilityReactions();
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
            TacticalPoints, source.Position, "skill_cast");
        DrainAbilityReactions();
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
                unit.InitialTeam >= 0 ? unit.InitialTeam : unit.Team,
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
                statistics.EffectiveHealingEvents,
                unit.Position, ActiveBattleTicks(unit));
        }).ToImmutableArray();

    private void AddConfiguredSummons()
    {
        var hero = _units.FirstOrDefault(unit => unit.Team == 0 && unit.Definition.IsHero);
        if (hero is not null && _config.HeroRule.AddBattleConstruct)
            SpawnTemporaryNear(_config.Summons.HeroConstruct, 0, hero.Position, .85f, .9f);
        _relicScope?.ExecuteBattleStartEffects();
        if (hero is not null && _config.Modifiers.SummonToken)
            SpawnTemporaryNear(_config.Summons.ItemToken, 0, hero.Position, .85f, .9f);
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
            unit.Attributes,
            ToCombatPoint(unit.Position),
            unit.BodyRadius))
        .ToImmutableArray();

    private void ExecuteRelicEffect(
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId,
        string targetId,
        int tick,
        float invocationValue) => ExecuteRelicEffect(binding, sourceId, ownerId, targetId, tick, invocationValue, default);

    private void ExecuteRelicEffect(
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId,
        string targetId,
        int tick,
        float invocationValue,
        CombatSourceRef origin)
    {
        if (_projectingInitialGrants)
        {
            QueueSetupEffect(() => ExecuteRelicEffect(binding, sourceId, ownerId, targetId, tick, invocationValue, origin));
            return;
        }
        var result = _effectCompatibility.ExecuteAuthored(
            binding,
            sourceId,
            ownerId,
            targetId,
            tick,
            invocationValue,
            origin);
        if (result.Status == EffectExecutionStatus.Failed || result.Status == EffectExecutionStatus.Interrupted &&
            result.Interruption is not (EffectInterruptionReason.UsageLimit or EffectInterruptionReason.RateLimited))
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
        return anchor is not null && SpawnTemporaryNear(
            snapshot,
            team,
            anchor.Position,
            healthMultiplier,
            damageMultiplier,
            sourceId);
    }

    private void InitializeAbilityScope(IReadOnlyDictionary<string, float> initialHealthRatios)
    {
        _abilityScope = new BattleAbilityScope($"battle_{_config.Seed}_abilities", this, 0);
        BindAbilityCombatEvents();
        foreach (var unit in _units.Where(unit => unit.Definition.AbilityLoadout is not null && !IsTimelineBoss(unit))
                     .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
            _abilityScope.RegisterLoadout(unit.RuntimeId, unit.Definition.AbilityLoadout!);
        foreach (var boss in _units.Where(IsTimelineBoss).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
            SynchronizeBossPhase(boss, initial: true, initialHealthRatio: initialHealthRatios.GetValueOrDefault(boss.RuntimeId, 1f));
    }

    private bool IsTimelineBoss(BattleUnitState unit) =>
        unit.Definition.IsBoss && _config.ResolveBossTimeline(unit.Definition.ContentId) is not null;

    private void SynchronizeBossPhase(BattleUnitState unit, bool initial = false, float? initialHealthRatio = null)
    {
        if (_abilityScope is null || !IsTimelineBoss(unit) ||
            _config.ResolveBossTimeline(unit.Definition.ContentId) is not { } timeline ||
            timeline.Phases.IsDefaultOrEmpty)
            return;
        var ratio = initialHealthRatio ?? (unit.MaxHealth <= 0 ? 0 : unit.Health / unit.MaxHealth);
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
            _abilityScope.RegisterLoadout(unit.RuntimeId, loadout, TickIndex);
        else
        {
            RevokePassiveGrants(unit.RuntimeId);
            _abilityScope.ReplaceLoadout(unit.RuntimeId, loadout, TickIndex);
            ActivatePassiveGrants(unit.RuntimeId);
        }
        _bossPhaseIndexes[unit.RuntimeId] = nextIndex;
        unit.BossPhaseId = phase.StableId;
    }

    private void ActivateBattleStartedAbilities()
    {
        if (_abilityScope is null) return;
        foreach (var unit in _units.Where(unit => unit.Alive).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
            _abilityScope.ActivateAutomatic(unit.RuntimeId, 0);
    }

    private string SelectAutomaticTarget(BattleUnitState owner, CompiledAbilityDefinition ability)
    {
        if (ability.Operations.OfType<CompiledHookOperation>().FirstOrDefault() is { } hook)
            return _units.Where(target => CanAimHook(owner,target,hook))
                .OrderByDescending(target => owner.Position.DistanceSquaredTo(target.Position))
                .ThenBy(target => target.RuntimeId,StringComparer.Ordinal).Select(target => target.RuntimeId).FirstOrDefault() ?? "";
        if (ability.Operations.OfType<CompiledGritPunchOperation>().FirstOrDefault() is { } punch)
            return _units.Where(target => target.Alive && target.Team != owner.Team &&
                    owner.Position.DistanceTo(target.Position) <= punch.Range && HasLineAccess(owner,target))
                .OrderBy(target => target.RuntimeId == owner.ActionTargetRuntimeId ? 0 : 1)
                .ThenBy(target => owner.Position.DistanceSquaredTo(target.Position))
                .ThenBy(target => target.RuntimeId,StringComparer.Ordinal).Select(target => target.RuntimeId).FirstOrDefault() ?? "";
        if (ability.Operations.OfType<CompiledEnemyAction>().FirstOrDefault() is { } enemyAction)
            return SelectEnemyActionTarget(owner,enemyAction);
        if (ability.Operations.OfType<CompiledTrampleOperation>().FirstOrDefault() is { } trample)
            return _units.Where(target => CanAimTrample(owner, target, trample))
                .OrderByDescending(target => owner.Position.DistanceSquaredTo(target.Position))
                .ThenBy(target => target.RuntimeId, StringComparer.Ordinal)
                .Select(target => target.RuntimeId).FirstOrDefault() ?? string.Empty;
        if (ability.Operations.OfType<CompiledChargedLineOperation>().FirstOrDefault() is { } line)
            return _units.Where(target => CanAimChargedLine(owner, target, line))
                .OrderBy(target => owner.Position.DistanceSquaredTo(target.Position))
                .ThenBy(target => target.RuntimeId, StringComparer.Ordinal)
                .Select(target => target.RuntimeId).FirstOrDefault() ?? string.Empty;
        var candidates = _units.Where(target => target.Alive &&
            BattlefieldSpace.IsWithinReach(owner, target, owner.AttackRange) && HasLineAccess(owner, target));
        if (ability.AutomaticTarget == AbilityAutomaticTargetKind.WoundedAlly)
            return candidates.Where(target => target.Team == owner.Team && target.Health < target.MaxHealth)
                .OrderBy(target => target.Health / target.MaxHealth)
                .ThenBy(target => target.RuntimeId, StringComparer.Ordinal)
                .Select(target => target.RuntimeId).FirstOrDefault() ?? string.Empty;
        return candidates.Where(target => target.Team != owner.Team)
            .OrderBy(target => target.RuntimeId == owner.ActionTargetRuntimeId ? 0 : 1)
            .ThenBy(target => BattlefieldSpace.EdgeDistance(owner, target))
            .ThenBy(target => target.RuntimeId, StringComparer.Ordinal)
            .Select(target => target.RuntimeId).FirstOrDefault() ?? string.Empty;
    }

    private bool ScheduleStatusEffect(StatusEffectInvocation invocation)
    {
        if (_discardStatusEffects) return true;
        if (_projectingInitialGrants)
        {
            QueueSetupEffect(() =>
            {
                if (!ExecuteStatusEffectNow(invocation))
                    throw new InvalidOperationException($"Initial status effect '{invocation.Binding.StableId}' failed.");
            }, invocation);
            return true;
        }
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
        if (_discardStatusEffects) return true;
        if (_projectingInitialGrants) return ScheduleStatusEffect(invocation);
        // A cash-out can consume a periodic tick after it was queued but before it executes.
        if (invocation.Kind == StatusEffectInvocationKind.Periodic &&
            !_statusScope.TryClaimPeriodicInvocation(invocation)) return true;
        var combatEvent = invocation.CombatEvent;
        var result = _effectCompatibility.ExecuteAuthored(
            invocation.Binding,
            invocation.SourceId,
            invocation.OwnerId,
            invocation.ExplicitTargetId,
            invocation.Tick,
            invocation.Kind == StatusEffectInvocationKind.Periodic ? invocation.PeriodicValue : combatEvent?.EffectiveValue ?? 0,
            CombatSourceRef.Status(invocation.Definition.StableId, invocation.OwnerId, invocation.InstanceId));
        return result.Status is EffectExecutionStatus.Succeeded or EffectExecutionStatus.Skipped ||
            result.Status == EffectExecutionStatus.Interrupted &&
            result.Interruption is EffectInterruptionReason.UsageLimit or EffectInterruptionReason.RateLimited;
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
            Position: owner is null ? default : ToCombatPoint(owner.Position),
            SubjectStableId: status.StableId,
            PreviousStacks: lifecycle.PreviousStacks,
            CurrentStacks: lifecycle.CurrentStacks,
            Reason: lifecycle.RemovalReason == StatusRemovalReason.None
                ? string.Empty
                : lifecycle.RemovalReason.ToString()));
        if (lifecycle.Kind != StatusLifecycleKind.Removed && status.Behavior == StatusBehaviorKind.DisableActions)
            PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.ControlApplied,ResolveCombatSource(status.SourceId),
                status.SourceId,status.OwnerId,lifecycle.Tick,EffectiveValue:status.RemainingTicks,SubjectStableId:status.StableId));
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
        var owner = _units.FirstOrDefault(unit => unit.RuntimeId == ownerId &&
            (unit.Alive || ability.Trigger == AbilityTriggerKind.OwnerDefeated));
        if (owner is null)
            return AbilityPreparationFailed(AbilityActivationFailure.SourceUnavailable, "英雄或能力拥有者已无法行动。");
        if (ability.GoldCost > RemainingGold)
            return AbilityPreparationFailed(AbilityActivationFailure.InsufficientGold, $"金币不足：需要 {ability.GoldCost} 金币。");
        if (ability.ActivationKind == AbilityActivationKind.Automatic &&
            (EnemyActionBusy(owner) || TechniqueBusy(owner) || UnitActionRecovering(owner) || DuelOpponent(owner) is not null || owner.Trample is not null || owner.ChargedLine is not null || owner.DisabledTicks > 0 || _statusScope.HasTag(ownerId, StatusDefinitionCompiler.ActionDisabledTag)))
            return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "受控期间不能施法。");
        if (ability.Trigger == AbilityTriggerKind.ManaFull)
        {
            if (IsDisplacing(owner))
                return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "位移过程中不能开始新的主动技能。");
            if (!BattleHeroMana.IsReady(owner, tick))
                return AbilityPreparationFailed(AbilityActivationFailure.InsufficientMana, "法力尚未充满或正在施法。");
        }
        // Targeting is independent of the trigger/resource policy. Triggered invocations
        // retain their event counterpart; every Automatic entry selects its own target.
        if (ability.ActivationKind == AbilityActivationKind.Automatic)
            explicitTargetId = SelectAutomaticTarget(owner, ability);
        var snapshot = CaptureAbilitySnapshot(tick);
        var ownerSnapshot = snapshot.Entities[ownerId];
        var operations = ImmutableArray.CreateBuilder<ResolvedAbilityOperation>();
        var summons = ImmutableArray.CreateBuilder<AbilitySummonReservation>();
        var reservedPositions = new List<(Vector2 Position, float Radius)>();
        for (var operationIndex = 0; operationIndex < ability.Operations.Length; operationIndex++)
        {
            var operation = ability.Operations[operationIndex];
            switch (operation)
            {
                case CompiledEnemyAction enemyAction:
                    if (!PrepareEnemyAction(owner,enemyAction,explicitTargetId))
                        return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet,"当前没有合法的机制目标或召唤资源。");
                    operations.Add(new ResolvedAbilityOperation(operationIndex,operation,[explicitTargetId],0));
                    break;
                case CompiledCombatTechniqueOperation technique:
                    if (!PrepareTechnique(technique,owner,explicitTargetId,out var techniqueTargets))
                        return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet,"没有合法的技能目标或积累条件。");
                    operations.Add(new ResolvedAbilityOperation(operationIndex,operation,techniqueTargets,0));
                    break;
                case CompiledTrampleOperation trample:
                    if (IsDisplacing(owner) || owner.ProjectileSequence is not null || !owner.ProjectileWindups.IsEmpty ||
                        !_units.Any(target => target.RuntimeId == explicitTargetId && CanAimTrample(owner, target, trample)))
                        return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "没有合法的冲锋方向。");
                    operations.Add(new ResolvedAbilityOperation(operationIndex, trample, [explicitTargetId], 0));
                    break;
                case CompiledChargedLineOperation line:
                    if (owner.ChargedLine is not null || IsDisplacing(owner) || owner.ProjectileSequence is not null ||
                        !owner.ProjectileWindups.IsEmpty || !_units.Any(target => target.RuntimeId == explicitTargetId && CanAimChargedLine(owner, target, line)))
                        return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "当前不能蓄力或没有合法的贯穿目标。");
                    operations.Add(new ResolvedAbilityOperation(operationIndex, line, [explicitTargetId], 0));
                    break;
                case CompiledDisplacementOperation displacement:
                    if (!PrepareDisplacementOperation(displacement, owner, explicitTargetId, out var displacementTargets))
                        return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "没有合法的位移目标或落点。");
                    operations.Add(new ResolvedAbilityOperation(operationIndex, operation, displacementTargets, 0));
                    break;
                case CompiledBattleValueOperation or CompiledConsumeStatusOperation or CompiledEchoOperation or CompiledLifecycleOperation:
                    if (!PrepareBattleOperation(operation,owner,explicitTargetId,out var battleTargets))
                        return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet,"当前不满足技能的资源、状态或目标条件。");
                    operations.Add(new ResolvedAbilityOperation(operationIndex,operation,battleTargets,0));
                    break;
                case CompiledProjectileSequenceAbilityOperation sequence:
                    if (owner.ProjectileSequence is not null || owner.AttackDelivery != Content.AttackDelivery.Projectile ||
                        !_units.Any(unit => unit.RuntimeId == explicitTargetId && unit.Alive && unit.Team != owner.Team))
                        return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "当前没有合法的连射目标。");
                    operations.Add(new ResolvedAbilityOperation(operationIndex, sequence, [explicitTargetId], 0));
                    break;
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
                        invocationValue,
                        AbilityOrigin(ability, ownerId));
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
                case CompiledScaledStatusAbilityOperation scaled:
                {
                    var targetIds = ResolveAbilityTargets(scaled.TargetQuery, snapshot, sourceId, ownerId, explicitTargetId);
                    if (targetIds.Length == 0) return AbilityPreparationFailed(AbilityActivationFailure.ConditionsUnmet, "当前没有合法的状态目标。");
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
                    var livingTemporary = _units.Count(unit => unit.Team == owner.Team && unit.IsTemporary && unit.Alive &&
                        (!summon.LimitPerOwner || unit.SummonerRuntimeId == owner.RuntimeId));
                    var availableByLimit = summon.MaximumLivingTemporaryUnits <= 0
                        ? summon.Count
                        : Math.Max(0, summon.MaximumLivingTemporaryUnits - livingTemporary);
                    var reserveCount = Math.Min(summon.Count, availableByLimit);
                    for (var sequence = 0; sequence < reserveCount; sequence++)
                    {
                        if (!TryFindOpenNear(
                                owner.Position,
                                owner.Team,
                                profile.BodyRadius,
                                reservedPositions,
                                out var position)) break;
                        reservedPositions.Add((position, profile.BodyRadius));
                        summons.Add(new AbilitySummonReservation(
                            operationIndex,
                            summon.Profile,
                            sequence,
                            position.X,
                            position.Y,
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
        var owner = _units.FirstOrDefault(unit => unit.RuntimeId == plan.OwnerId &&
            (unit.Alive || plan.Ability.Trigger == AbilityTriggerKind.OwnerDefeated));
        if (owner is null) return AbilityCommitFailed(AbilityActivationFailure.SourceUnavailable, "英雄或能力拥有者已无法行动。");
        var manaSkill = plan.Ability.Trigger == AbilityTriggerKind.ManaFull;
        if (manaSkill && (IsDisplacing(owner) || !BattleHeroMana.IsReady(owner, plan.Tick) || owner.DisabledTicks > 0 ||
                _statusScope.HasTag(owner.RuntimeId, StatusDefinitionCompiler.ActionDisabledTag)))
            return AbilityCommitFailed(AbilityActivationFailure.ConditionsUnmet, "施法条件在提交前失效。");
        if (plan.GoldCost > RemainingGold) return AbilityCommitFailed(AbilityActivationFailure.InsufficientGold, $"金币不足：需要 {plan.GoldCost} 金币。");

        foreach (var operation in plan.Operations)
            if (operation.Operation is not CompiledLifecycleOperation && operation.TargetIds.Any(targetId => !_units.Any(unit => unit.RuntimeId == targetId && unit.Alive)))
                return AbilityCommitFailed(AbilityActivationFailure.CommitFailed, "能力目标在提交前失效。");
        var reservedPositions = new List<(Vector2 Position, float Radius)>();
        foreach (var reservation in plan.Summons.OrderBy(item => item.OperationIndex).ThenBy(item => item.Sequence))
        {
            var summon = (CompiledSummonAbilityOperation)plan.Ability.Operations[reservation.OperationIndex];
            var profile = ResolveSummonProfile(summon, owner);
            if (profile is null)
                return AbilityCommitFailed(AbilityActivationFailure.CommitFailed, "召唤模板在提交前失效。");
            var position = new Vector2(reservation.PositionX, reservation.PositionY);
            if (!CanOccupyPosition(position, profile.BodyRadius, reservedPositions))
                return AbilityCommitFailed(AbilityActivationFailure.CommitFailed, "召唤落点在提交前失效。");
            reservedPositions.Add((position, profile.BodyRadius));
        }

        // Lock before effects so reflected/reactive damage cannot refill this cast. Any failed
        // operation restores mana, recovery and visuals together with all other world state.
        var manaSpent = manaSkill ? BattleHeroMana.Spend(owner, plan.Tick, plan.Ability.CooldownTicks) : 0;
        var facts = ImmutableArray.CreateBuilder<string>();
        foreach (var resolved in plan.Operations.OrderBy(operation => operation.OperationIndex))
        {
            // Initial preflight validated all targets. A preceding operation may now have
            // killed one; that is a resolved combat fact, not a reason to undo the kill or
            // restore a defeated unit through a later status/heal operation.
            if (resolved.Operation is not CompiledLifecycleOperation && resolved.TargetIds.Length > 0 && resolved.TargetIds.All(targetId =>
                    !_units.Any(unit => unit.RuntimeId == targetId && unit.Alive)))
                continue;
            switch (resolved.Operation)
            {
                case CompiledEnemyAction enemyAction:
                    BeginEnemyAction(owner,_units.First(u => u.RuntimeId == resolved.TargetIds[0]),enemyAction,plan.Ability);
                    facts.Add(enemyAction.GetType().Name);
                    break;
                case CompiledCombatTechniqueOperation technique:
                    ExecuteTechnique(technique,owner,resolved.TargetIds,plan.Ability);
                    facts.Add(technique.GetType().Name);
                    break;
                case CompiledTrampleOperation trample:
                    BeginTrample(owner, _units.First(target => target.RuntimeId == resolved.TargetIds[0]), trample, plan.Ability);
                    facts.Add("Trample");
                    break;
                case CompiledChargedLineOperation line:
                    BeginChargedLine(owner, _units.First(target => target.RuntimeId == resolved.TargetIds[0]),
                        line, plan.Ability);
                    facts.Add($"ChargedLine:{line.Delivery}");
                    break;
                case CompiledDisplacementOperation displacement:
                    ExecuteDisplacementOperation(displacement, resolved.TargetIds, owner, AbilityOrigin(plan.Ability, plan.OwnerId));
                    facts.Add($"Displacement:{displacement.Kind}:{string.Join(",", resolved.TargetIds)}");
                    break;
                case CompiledBattleValueOperation or CompiledConsumeStatusOperation or CompiledEchoOperation or CompiledLifecycleOperation:
                    ExecuteBattleOperation(resolved,plan,owner);
                    facts.Add($"BattleOperation:{resolved.OperationIndex}");
                    break;
                case CompiledProjectileSequenceAbilityOperation sequence:
                    owner.ProjectileSequence = new ProjectileSequenceState(sequence,
                        AbilityOrigin(plan.Ability, plan.OwnerId), resolved.TargetIds[0], sequence.ShotCount);
                    FireSequenceShot(owner);
                    facts.Add($"Sequence:{sequence.ShotCount}");
                    break;
                case CompiledEffectAbilityOperation effect:
                {
                    var result = _effectCompatibility.ExecuteAuthored(
                        effect.Binding,
                        plan.SourceId,
                        plan.OwnerId,
                        resolved.TargetIds.FirstOrDefault() ?? string.Empty,
                        plan.Tick,
                        resolved.InvocationValue,
                        AbilityOrigin(plan.Ability, plan.OwnerId));
                    if (result.Status != EffectExecutionStatus.Succeeded)
                        throw new InvalidOperationException($"Prepared ability effect '{effect.Binding.StableId}' failed during commit.");
                    var steps = result.Invocations.SelectMany(invocation => invocation.Steps)
                        .Where(step => step.Status == EffectExecutionStatus.Succeeded)
                        .ToArray();
                    if (plan.Ability.ActivationKind == AbilityActivationKind.Automatic)
                        foreach (var step in steps.Where(step => step.Kind == EffectKind.Shield))
                        {
                            var target = _units.First(unit => unit.RuntimeId == step.TargetId);
                            Emit("shield", plan.SourceId, step.TargetId, step.AppliedAmount, target.Position, "skill_cast");
                        }
                    facts.AddRange(steps.Select(step => $"{step.Kind}:{step.TargetId}:{step.AppliedAmount:0.###}"));
                    if (plan.Ability.Presentation?.DamageVfx is { Length: > 0 } vfx)
                        foreach (var step in steps.Where(step => step.Kind == EffectKind.Damage && step.AppliedAmount > 0))
                        {
                            var target = _units.First(unit => unit.RuntimeId == step.TargetId);
                            Emit("vfx", plan.SourceId, target.RuntimeId, step.AppliedAmount, target.Position, "",
                                vfx: new BattleVfxCue(BattleVfxPhase.Burst, vfx));
                        }
                    break;
                }
                case CompiledCooldownAbilityOperation cooldown:
                    foreach (var targetId in resolved.TargetIds)
                    {
                        var target = _units.First(unit => unit.RuntimeId == targetId);
                        if (!target.Alive) continue;
                        target.AttackCooldown = AdjustCooldown(target.AttackCooldown, cooldown.AttackAdjustment, cooldown.AttackValue);
                        target.MoveCooldown = AdjustCooldown(target.MoveCooldown, cooldown.MoveAdjustment, cooldown.MoveValue);
                        facts.Add($"Cooldown:{targetId}");
                    }
                    break;
                case CompiledScaledStatusAbilityOperation scaled:
                    CommitScaledStatus(scaled, resolved, plan, facts);
                    break;
                case CompiledApplyStatusAbilityOperation status:
                    foreach (var targetId in resolved.TargetIds)
                    {
                        if (!_units.Any(unit => unit.RuntimeId == targetId && unit.Alive)) continue;
                        var grantId = plan.Ability.ActivationKind == AbilityActivationKind.Passive
                            ? PassiveGrantId(plan.OwnerId, plan.Ability.StableId) : string.Empty;
                        var applied = _statusScope.ApplyBatch([new StatusApplicationRequest(
                            status.Status, plan.SourceId, targetId, plan.Tick, grantId)])[0];
                        if (!applied.Applied)
                            throw new InvalidOperationException($"Prepared status '{status.Status.StableId}' failed during commit.");
                        facts.Add($"Status:{status.Status.StableId}:{targetId}:{applied.Status?.Stacks ?? 0}");
                    }
                    if (!string.IsNullOrWhiteSpace(status.ApplicationVfx) && status.TargetQuery is CompiledFilteredTargetQuery area)
                    {
                        var anchorId = area.Anchor == EffectEntityReference.Owner ? plan.OwnerId : plan.SourceId;
                        var anchor = _units.First(unit => unit.RuntimeId == anchorId);
                        Emit("vfx", plan.SourceId, anchorId, 0, anchor.Position, "", origin: anchor.Position,
                            vfx: new BattleVfxCue(BattleVfxPhase.Burst, status.ApplicationVfx, area.Range));
                    }
                    break;
                case CompiledSummonAbilityOperation summon:
                    if (!owner.Alive && plan.Ability.Trigger != AbilityTriggerKind.OwnerDefeated) break;
                    foreach (var reservation in plan.Summons
                                 .Where(item => item.OperationIndex == resolved.OperationIndex)
                                 .OrderBy(item => item.Sequence))
                    {
                        var profile = ResolveSummonProfile(summon, owner)!;
                        if (!SpawnTemporary(
                                profile,
                                owner.Team,
                                new Vector2(reservation.PositionX, reservation.PositionY),
                                reservation.HealthMultiplier,
                                reservation.DamageMultiplier,
                                plan.SourceId))
                            throw new InvalidOperationException("Prepared ability summon failed during commit.");
                        facts.Add($"Summon:{profile.ContentId}:{reservation.PositionX:R},{reservation.PositionY:R}");
                    }
                    break;
            }
        }

        GoldSpent += plan.GoldCost;
        if (plan.Ability.IsActiveSkill)
        {
            owner.LastAbilityName = plan.Ability.DisplayName;
            var primaryTarget = plan.Operations.SelectMany(operation => operation.TargetIds).FirstOrDefault() ?? owner.RuntimeId;
            if (owner.Alive)
            {
                owner.Mode = BattleUnitMode.Casting;
                owner.LastActionKind = BattleActionKind.Ability;
                var actionTarget = _units.FirstOrDefault(unit => unit.RuntimeId == primaryTarget);
                if (actionTarget is not null) SetActionTarget(owner, actionTarget);
                else ClearActionTarget(owner);
                _movement?.ReleaseGoal(owner.RuntimeId);
                Emit("ability", owner.RuntimeId, primaryTarget, manaSpent, owner.Position,
                    string.IsNullOrWhiteSpace(plan.Ability.Presentation?.Cue) ? "skill_cast" : plan.Ability.Presentation.Cue,
                    sourceVfx: plan.Ability.Presentation?.CastVfx ?? "");
            }
        }
        if (manaSkill)
            PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.ManaSkillResolved,
                AbilityOrigin(plan.Ability,plan.OwnerId),plan.SourceId,plan.OwnerId,plan.Tick,SubjectStableId:plan.Ability.StableId));
        PublishCombat(new BattleCombatEventDraft(
            BattleCombatEventKind.AbilityResolved,
            AbilityOrigin(plan.Ability, plan.OwnerId),
            plan.SourceId,
            plan.OwnerId,
            plan.Tick,
            Cell: ToCombatCell(owner.Cell),
            Position: ToCombatPoint(owner.Position),
            SubjectStableId: plan.Ability.StableId));
        resolution.Commit();
        return new AbilityCommitResult(true, AbilityActivationFailure.None, string.Empty, facts.ToImmutable(), manaSpent);
    }

    private ImmutableArray<string> ResolveAbilityTargets(
        CompiledEffectTargetQuery query,
        AbilityWorldSnapshot snapshot,
        string sourceId,
        string ownerId,
        string explicitTargetId)
    {
        return EffectTargetResolver.Resolve(query, CaptureEffectSnapshot(snapshot.Tick), sourceId, ownerId, explicitTargetId)
            .Where(id => snapshot.Entities.TryGetValue(id, out var entity) && entity.Alive)
            .ToImmutableArray();
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
        RefreshGritActionRequests(unit);
        BattleHeroMana.Advance(unit, TickIndex);
        bool actionsDisabled;
        using (var statusResolution = _combatPipeline.BeginAuthoritativeResolution())
        {
            actionsDisabled = _statusScope.HasTag(unit.RuntimeId, StatusDefinitionCompiler.ActionDisabledTag);
            _statusScope.AdvanceOwner(unit.RuntimeId, TickIndex);
            statusResolution.Commit();
        }
        if (!unit.Alive) return;
        if (IsDisplacing(unit))
        {
            // Forced motion continues through control; status durations and legacy disables still
            // elapse, while attacks, fresh casts and normal navigation wait until landing.
            if (unit.DisabledTicks > 0) unit.DisabledTicks--;
            unit.Mode = BattleUnitMode.Moving;
            _movement!.ReleaseGoal(unit.RuntimeId);
            return;
        }
        if (actionsDisabled)
        {
            InterruptBattleChannels(unit.RuntimeId);
            CancelProjectileWindups(unit);
            unit.Mode = BattleUnitMode.Disabled;
            unit.WaitingTicks = 0;
            _movement!.ReleaseGoal(unit.RuntimeId);
            return;
        }
        if (unit.DisabledTicks > 0)
        {
            InterruptBattleChannels(unit.RuntimeId);
            CancelProjectileWindups(unit);
            unit.DisabledTicks--;
            unit.Mode = BattleUnitMode.Disabled;
            unit.WaitingTicks = 0;
            _movement!.ReleaseGoal(unit.RuntimeId);
            return;
        }
        if (EnemyActionBusy(unit) || TechniqueBusy(unit) || UnitActionRecovering(unit)) { unit.Mode=BattleUnitMode.Casting; _movement!.ReleaseGoal(unit.RuntimeId); return; }
        if (unit.Trample is not null) { _movement!.ReleaseGoal(unit.RuntimeId); return; }
        if (AdvanceChargedLine(unit)) return;
        var releasedProjectile = AdvanceProjectileWindups(unit);
        if (!unit.Alive) return;
        if (unit.ProjectileSequence is not null)
        {
            AdvanceProjectileSequence(unit);
            return;
        }
        if (!unit.ProjectileWindups.IsEmpty)
        {
            unit.Mode = unit.ProjectileWindups.Any(shot => shot.SkillOrigin.IsSpecified) ? BattleUnitMode.Casting : BattleUnitMode.Attacking;
            if (unit.AttackCooldown > 0) unit.AttackCooldown--;
            if (unit.Definition.AttackHitGrowth is not null && unit.Mode == BattleUnitMode.Attacking)
                unit.AttackCycleProgress += 1 / unit.PreciseAttackTicks;
            _movement!.ReleaseGoal(unit.RuntimeId);
            return;
        }
        if (TickIndex < unit.ManaLockedUntilTick)
        {
            unit.Mode = BattleUnitMode.Casting;
            unit.LastActionKind = BattleActionKind.Ability;
            _movement!.ReleaseGoal(unit.RuntimeId);
            return;
        }
        // Let the release pose finish before a mana skill starts a fresh draw on the
        // same unit. The following tick may cast normally; attack cadence is unchanged.
        var queuedResult = !releasedProjectile ? TryStartQueuedUnitAction(unit) : null;
        var automaticResults = queuedResult is { Succeeded: true } ? ImmutableArray.Create(queuedResult) : !releasedProjectile
            ? _abilityScope?.ActivateAutomatic(unit.RuntimeId, TickIndex) ?? [] : [];
        if (!unit.Alive || EnemyActionBusy(unit) || TechniqueBusy(unit) || unit.Trample is not null || unit.ChargedLine is not null || IsDisplacing(unit) || TickIndex < unit.ManaLockedUntilTick) return;
        ApplyPeriodicBehavior(unit);
        if (!unit.Alive) return;
        if (unit.AttackCooldown > 0) unit.AttackCooldown--;
        if (unit.MoveCooldown > 0) unit.MoveCooldown--;
        if (TryApproachAutomaticHealingTarget(unit, automaticResults)) return;
        if (ShouldHoldForChargedLine(unit))
        {
            unit.Mode = BattleUnitMode.Waiting;
            _movement!.ReleaseGoal(unit.RuntimeId);
            return;
        }
        if (unit.Definition.Behavior.DisableBasicAttacks)
        {
            unit.Mode = BattleUnitMode.Waiting;
            _movement!.ReleaseGoal(unit.RuntimeId);
            return;
        }

        if (unit.HealingPower > 0 && DuelOpponent(unit) is null)
        {
            var wounded = Allies(unit.Team).Where(ally => ally != unit && ally.Health < ally.MaxHealth)
                .OrderBy(ally => ally.Health / ally.MaxHealth).ThenBy(ally => ally.RuntimeId, StringComparer.Ordinal).ToArray();
            var protectedAlly = _movement!.SelectTarget(unit, wounded);
            if (protectedAlly is not null)
            {
                SetActionTarget(unit, protectedAlly);
                unit.LastActionKind = BattleActionKind.Heal;
                if (BattlefieldSpace.IsWithinReach(unit, protectedAlly, unit.AttackRange) && HasLineAccess(unit, protectedAlly))
                {
                    _movement.ReleaseGoal(unit.RuntimeId);
                    if (unit.AttackCooldown == 0)
                    {
                        var requestedHealing = unit.HealingPower;
                        HealLiving(unit.RuntimeId, protectedAlly, requestedHealing);
                        BattleHeroMana.OnAttack(unit, TickIndex);
                        unit.AttackCooldown = unit.EffectiveAttackTicks;
                        unit.Mode = BattleUnitMode.Casting;
                        unit.WaitingTicks = 0;
                        // Basic healing occupies the ordinary attack slot; only an ability
                        // resolution requests the active skill pose and source feedback.
                        Emit("heal", unit.RuntimeId, protectedAlly.RuntimeId, requestedHealing, protectedAlly.Position, "attack");
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
        if (BattlefieldSpace.IsWithinReach(unit, target, unit.AttackRange) && HasLineAccess(unit, target))
        {
            _movement!.ReleaseGoal(unit.RuntimeId);
            if (unit.Definition.AttackHitGrowth is not null)
                AdvanceGrowingAttacks(unit, target);
            else if (unit.AttackCooldown == 0) Attack(unit, target);
            else unit.Mode = BattleUnitMode.Recovering;
            return;
        }
        if (unit.Definition.Behavior.Stationary) unit.Mode = BattleUnitMode.Waiting;
        else if (unit.MoveCooldown == 0) _movement!.QueueMove(unit);
        else unit.Mode = BattleUnitMode.Seeking;
    }

    private BattleUnitState? SelectTarget(BattleUnitState unit)
    {
        if (DuelOpponent(unit) is { } opponent) return _movement!.SelectTarget(unit, [opponent]);
        var taunter = unit.Statuses.Where(status => status.Behavior == StatusBehaviorKind.Taunt)
            .OrderByDescending(status => status.LastAppliedTick).ThenByDescending(status => status.ApplicationSequence)
            .Select(status => _units.FirstOrDefault(candidate => candidate.RuntimeId == status.SourceId &&
                candidate.Alive && candidate.Team != unit.Team)).FirstOrDefault(candidate => candidate is not null);
        if (taunter is not null) return _movement!.SelectTarget(unit, [taunter]);
        var enemies = Allies(1 - unit.Team).ToList();
        if (enemies.Count == 0)
        {
            _movement!.ClearTarget(unit.RuntimeId);
            return null;
        }
        IEnumerable<BattleUnitState> ordered;
        if (unit.Team == 0 && unit.Definition.IsHero && _config.HeroRule.PreferBossTargets)
        {
            ordered = enemies.OrderByDescending(enemy => enemy.Definition.IsBoss && BattlefieldSpace.EdgeDistance(unit, enemy) <= 3f)
                .ThenBy(enemy => BattlefieldSpace.EdgeDistance(unit, enemy)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        }
        else if (unit.Definition.Behavior.PreferBacklineTargets)
            ordered = enemies.OrderByDescending(enemy => enemy.AttackRange + enemy.HealingPower)
                .ThenBy(enemy => BattlefieldSpace.EdgeDistance(unit, enemy)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        else if (unit.Definition.Role == Content.UnitRole.Assassin)
            ordered = enemies.OrderByDescending(enemy => enemy.AttackRange).ThenBy(enemy => BattlefieldSpace.EdgeDistance(unit, enemy))
                .ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        else
            ordered = enemies.OrderBy(enemy => BattlefieldSpace.EdgeDistance(unit, enemy)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        var desired = ordered.First();
        var wall = enemies.Where(candidate => IsRampartBody(candidate) &&
                LineHitTime(unit.Position, desired.Position-unit.Position, unit.BodyRadius*.5f, candidate) is not null)
            .OrderBy(candidate => unit.Position.DistanceSquaredTo(candidate.Position)).ThenBy(candidate => candidate.RuntimeId,StringComparer.Ordinal).FirstOrDefault();
        return _movement!.SelectTarget(unit, wall is null ? ordered.ToArray() : [wall]);
    }

    private void Attack(BattleUnitState attacker, BattleUnitState target)
    {
        attacker.AttackCooldown = attacker.Definition.AttackHitGrowth is null ? attacker.EffectiveAttackTicks : 0;
        if (attacker.AttackDelivery == Content.AttackDelivery.Projectile && attacker.Definition.ProjectileWindupSeconds > 0)
        {
            attacker.Mode = BattleUnitMode.Attacking;
            attacker.LastActionKind = BattleActionKind.Attack;
            attacker.WaitingTicks = 0;
            BeginProjectileWindup(attacker, target);
            return;
        }
        ResolveAttack(attacker, target);
    }

    private void ResolveAttack(BattleUnitState attacker, BattleUnitState target, bool timed = false)
    {
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        _statistics[attacker.RuntimeId].AttackActions++;
        SelectAttackChainTarget(attacker, target);
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
            Cell: ToCombatCell(target.Cell),
            Position: ToCombatPoint(target.Position)));
        var rawDamage = EffectiveDamage(attacker);
        if (attacker.Definition.Behavior.LowHealthDamageBonus > 0 && attacker.Health / attacker.MaxHealth <= .4f)
            rawDamage *= 1f + attacker.Definition.Behavior.LowHealthDamageBonus;
        BattleHeroMana.OnAttack(attacker, TickIndex);
        Emit("attack", attacker.RuntimeId, target.RuntimeId, rawDamage, target.Position, timed ? "" : "attack");
        if (attacker.AttackDelivery == TowerAutobattler.Content.AttackDelivery.Projectile)
            LaunchProjectile(attacker, target, rawDamage);
        else
        {
            if (attacker.AttackDelivery == TowerAutobattler.Content.AttackDelivery.Beam)
                Emit("beam", attacker.RuntimeId, target.RuntimeId, rawDamage, target.Position, "",
                    origin: attacker.Position);
            ResolveAttackHit(attacker, target, rawDamage, instantPiercing: true);
        }
        resolution.Commit();
    }

    // Shared impact rules execute only after an instant attack or an authoritative projectile collision.
    private void ResolveAttackHit(BattleUnitState attacker, BattleUnitState target, float rawDamage, bool instantPiercing)
    {
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        var source = ResolveCombatSource(attacker.RuntimeId, attacker);
        if (!target.Alive) { resolution.Commit(); return; }
        var dodge = target.Attributes.GetValue(CombatAttribute.DodgeChance);
        if (dodge > 0 && target.DisabledTicks <= 0 && !_statusScope.HasTag(target.RuntimeId,StatusDefinitionCompiler.ActionDisabledTag) && _random.NextFloat() < dodge)
        {
            PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.AttackDodged,source,attacker.RuntimeId,target.RuntimeId,TickIndex));
            Emit("dodge",attacker.RuntimeId,target.RuntimeId,0,target.Position,"hit");
            resolution.Commit(); return;
        }
        var chance = attacker.Attributes.GetValue(CombatAttribute.CriticalChance);
        var critical = chance > 0 && _random.NextFloat() < chance;
        if (critical) rawDamage *= attacker.Attributes.GetValue(CombatAttribute.CriticalDamage);
        var damage = ApplyDamage(attacker.RuntimeId, attacker, target, rawDamage);
        if (critical)
            PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.CriticalHit,source,attacker.RuntimeId,target.RuntimeId,
                TickIndex,RequestedValue:rawDamage,EffectiveValue:damage));
        RegisterGrowingHit(attacker, target);
        if (attacker.Alive && attacker.LifeSteal > 0)
            HealLiving(attacker.RuntimeId, attacker, damage * attacker.LifeSteal);
        WriteCounter(attacker,"_basic_hits",ReadCounter(attacker,"_basic_hits")+1);
        PublishCombat(new BattleCombatEventDraft(
            BattleCombatEventKind.AttackLanded,
            source,
            attacker.RuntimeId,
            target.RuntimeId,
            TickIndex,
            RequestedValue: rawDamage,
            AppliedValue: damage,
            EffectiveValue: damage,
            Cell: ToCombatCell(target.Cell),
            Position: ToCombatPoint(target.Position),
            CurrentStacks: (int)Math.Min(int.MaxValue,ReadCounter(attacker,"_basic_hits"))));
        if (attacker.Definition.SplashRadius > 0)
            Emit("vfx", attacker.RuntimeId, target.RuntimeId, 0, target.Position, "",
                vfx: new BattleVfxCue(BattleVfxPhase.Burst, "burst", attacker.Definition.SplashRadius));
        if (attacker.Definition.SplashRadius > 0)
            foreach (var splash in Allies(target.Team).Where(other =>
                         other != target &&
                         Math.Max(0f, other.Position.DistanceTo(target.Position) - other.BodyRadius) <=
                         attacker.Definition.SplashRadius + .0001f).ToArray())
                ApplyDamage(attacker.RuntimeId, attacker, splash, rawDamage * .45f);
        if (instantPiercing && attacker.Definition.Behavior.PiercingLine)
        {
            var attackDelta = target.Position - attacker.Position;
            var direction = attackDelta.LengthSquared() <= .000001f ? Vector2.Right : attackDelta.Normalized();
            var targetProjection = attackDelta.Length();
            var rayEnd = attacker.Position + direction * MathF.Sqrt(Width * Width + Height * Height);
            var behind = Allies(target.Team)
                .Where(other => other.RuntimeId != target.RuntimeId)
                .Select(other => new
                {
                    Unit = other,
                    Projection = (other.Position - attacker.Position).Dot(direction)
                })
                .Where(candidate => candidate.Projection > targetProjection + .0001f &&
                                    BattlefieldSpace.PointSegmentDistance(
                                        candidate.Unit.Position, target.Position, rayEnd) <=
                                    candidate.Unit.BodyRadius + .0001f)
                .GroupBy(candidate => candidate.Unit.RuntimeId, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(candidate => candidate.Projection)
                .ThenBy(candidate => candidate.Unit.RuntimeId, StringComparer.Ordinal)
                .Select(candidate => candidate.Unit)
                .FirstOrDefault();
            if (behind is not null) ApplyDamage(attacker.RuntimeId, attacker, behind, rawDamage * .35f);
        }
        if (attacker.Definition.Behavior.SlowOnHitTicks > 0 && target.Alive)
        {
            target.AttackCooldown += attacker.Definition.Behavior.SlowOnHitTicks / 2;
            target.MoveCooldown += attacker.Definition.Behavior.SlowOnHitTicks;
        }
        resolution.Commit();
    }

    private float ApplyDamage(string sourceRuntimeId, BattleUnitState? source, BattleUnitState target, float raw,
        CombatSourceRef origin = default, EffectDamageType damageType = EffectDamageType.Normal)
    {
        if (!target.Alive) return 0;
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        var healthBefore = target.Health;
        var wasAlive = target.Alive;
        var context = CreateRuleContext();
        if (target.Team == 0 && target.Definition.IsHero && _config.HeroRule.EmptySlotHeroDefense > 0)
            raw *= Math.Max(.25f, 1f - _config.EmptyDeploymentSlots * _config.HeroRule.EmptySlotHeroDefense);
        raw = _config.FloorRule.ModifyIncomingDamage(context, target, raw);
        if (source is not null && source.Definition.Behavior.ExecuteHealthThreshold > 0 && target.Health / target.MaxHealth <= source.Definition.Behavior.ExecuteHealthThreshold)
            raw *= 1.5f;
        var combatSource = origin.IsSpecified ? origin : ResolveCombatSource(sourceRuntimeId, source);
        var creditedKiller = source ?? _units.FirstOrDefault(unit => unit.RuntimeId == sourceRuntimeId);
        var calculated = _combatPipeline.Resolve(new BattleCombatCalculationRequest(
            BattleCombatCalculationKind.Damage,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            TickIndex,
            Math.Max(0, raw), damageType));
        raw = calculated.ResolvedAmount;
        var resistance = damageType switch
        {
            EffectDamageType.Normal => EffectiveArmor(target),
            EffectDamageType.True => 0,
            _ => throw new InvalidOperationException("Unsupported damage type.")
        };
        // True damage bypasses resistance, while shared incoming/outgoing modifiers and shields still apply.
        var damage = raw <= 0 ? 0 : damageType == EffectDamageType.True ? raw :
            Math.Max(1f, raw * 100f / (100f + resistance * 7f));
        var resolvedDamage = damage;
        var absorbed = Math.Min(target.Shield, damage);
        target.Shield -= absorbed;
        ConsumeTimedShields(target.RuntimeId, absorbed);
        if (absorbed > 0)
            Emit("vfx", sourceRuntimeId, target.RuntimeId, absorbed, target.Position, "",
                vfx: new BattleVfxCue(target.Shield > 0 ? BattleVfxPhase.ShieldImpact : BattleVfxPhase.ShieldDepleted, "shield"));
        damage -= absorbed;
        target.Health = Math.Max(0, target.Health - damage);
        RefreshGritActionRequests(target);
        var healthRemoved = Math.Min(healthBefore, damage);
        var effectiveDamage = absorbed + healthRemoved;
        BattleHeroMana.OnDamage(target, effectiveDamage, TickIndex);
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
            Cell: ToCombatCell(target.Cell),
            Position: ToCombatPoint(target.Position),
            DamageType: damageType));
        if (healthRemoved > 0)
            PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.HealthLost,combatSource,sourceRuntimeId,target.RuntimeId,
                TickIndex,EffectiveValue:healthRemoved,DamageType:damageType));
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
                Cell: ToCombatCell(target.Cell),
                Position: ToCombatPoint(target.Position),
                DamageType: damageType));
            if (creditedKiller is not null)
                PublishCombat(new BattleCombatEventDraft(
                    BattleCombatEventKind.UnitKilled,
                    combatSource,
                    sourceRuntimeId,
                    target.RuntimeId,
                    TickIndex,
                    AppliedValue: resolvedDamage,
                    EffectiveValue: effectiveDamage,
                    Cell: ToCombatCell(target.Cell),
                    Position: ToCombatPoint(target.Position),
                    DamageType: damageType));
            CancelProjectileWindups(target);
            target.ProjectileSequence = null;
            Emit("defeated", sourceRuntimeId, target.RuntimeId, damage, target.Position, "defeated");
            HandleDeath(source, target);
        }
        else Emit("damage", sourceRuntimeId, target.RuntimeId, damage, target.Position, "hit");
        resolution.Commit();
        return damage;
    }

    private void ApplyFloorRule()
    {
        _config.FloorRule.OnTick(CreateRuleContext());
    }

    private bool BeaconControlled(int team)
    {
        var center = BattlefieldSpace.CellCenter(new Vector2I(Width / 2, Height / 2));
        var friendly = Allies(team).Count(unit =>
            Math.Max(0f, unit.Position.DistanceTo(center) - unit.BodyRadius) <= 1.5f);
        var enemy = Allies(1 - team).Count(unit =>
            Math.Max(0f, unit.Position.DistanceTo(center) - unit.BodyRadius) <= 1.5f);
        return friendly > enemy && friendly > 0;
    }

    private bool HasLineAccess(BattleUnitState source, BattleUnitState target) =>
        HasLineAccess(source.Position, target.Position) &&
        (source.Team == target.Team || source.AttackDelivery != Content.AttackDelivery.Projectile ||
            BattlefieldSpace.IsSegmentTerrainClear(source.Position, target.Position, source.Definition.ProjectileRadius,
                Width, Height, cell => _config.FloorRule.CanOccupy(cell)));

    private bool HasLineAccess(Vector2 sourcePosition, Vector2 targetPosition) =>
        BattlefieldSpace.IsSegmentTerrainClear(
            sourcePosition,
            targetPosition,
            0f,
            Width,
            Height,
            cell => _config.FloorRule.CanOccupy(cell));

    private bool CanOccupy(Vector2I cell)
    {
        if (cell.X < 0 || cell.X >= Width || cell.Y < 0 || cell.Y >= Height) return false;
        if (!_config.FloorRule.CanOccupy(cell)) return false;
        return _units.All(unit => !unit.Alive || unit.Cell != cell);
    }

    private Vector2I FindOpenCellNear(Vector2I origin, int team)
    {
        foreach (var direction in new[] { Vector2I.Down, Vector2I.Up, Vector2I.Left, Vector2I.Right })
        {
            var cell = origin + direction;
            if (CanOccupy(cell)) return cell;
        }
        for (var y = 0; y < Height; y++)
        for (var x = team == 0 ? 0 : Width - 1; x >= 0 && x < Width; x += team == 0 ? 1 : -1)
            if (CanOccupy(new Vector2I(x, y))) return new Vector2I(x, y);
        return origin;
    }

    private bool CanOccupyPosition(
        Vector2 position,
        float radius,
        IReadOnlyList<(Vector2 Position, float Radius)>? reserved = null)
    {
        radius = Mathf.Clamp(radius, BattlefieldSpace.MinimumBodyRadius, BattlefieldSpace.MaximumBodyRadius);
        if (!BattlefieldSpace.IsPositionTerrainClear(
                position, radius, Width, Height, cell => _config.FloorRule.CanOccupy(cell))) return false;
        if (_movement?.IsPositionReserved(position, radius) == true) return false;
        if (_units.Any(unit => unit.Alive && !IsAirborne(unit) &&
                              unit.Position.DistanceTo(position) <
                              unit.BodyRadius + radius + BattlefieldSpace.BodyClearance)) return false;
        return reserved is null || reserved.All(item =>
            item.Position.DistanceTo(position) >= item.Radius + radius + BattlefieldSpace.BodyClearance);
    }

    private Vector2? FindOpenNear(Vector2 origin, int team, float radius)
    {
        return TryFindOpenNear(origin, team, radius, [], out var result) ? result : null;
    }

    private bool TryFindOpenNear(
        Vector2 origin,
        int team,
        float radius,
        IReadOnlyList<(Vector2 Position, float Radius)> reserved,
        out Vector2 result)
    {
        if (CanOccupyPosition(origin, radius, reserved))
        {
            result = origin;
            return true;
        }
        var step = Math.Max(.2f, radius * 2f + BattlefieldSpace.BodyClearance + .04f);
        var phase = team == 0 ? 0f : MathF.PI;
        for (var ring = 1; ring <= 12; ring++)
        for (var sample = 0; sample < 16; sample++)
        {
            var angle = phase + sample * Mathf.Tau / 16f;
            var candidate = origin + Vector2.FromAngle(angle) * step * ring;
            if (CanOccupyPosition(candidate, radius, reserved))
            {
                result = candidate;
                return true;
            }
        }
        var cells = Enumerable.Range(0, Height)
            .SelectMany(y => Enumerable.Range(0, Width).Select(x => new Vector2I(x, y)))
            .OrderBy(cell => BattlefieldSpace.CellCenter(cell).DistanceSquaredTo(origin))
            .ThenBy(cell => team == 0 ? cell.X : Width - 1 - cell.X)
            .ThenBy(cell => cell.Y);
        foreach (var cell in cells)
        {
            var candidate = BattlefieldSpace.CellCenter(cell);
            if (!CanOccupyPosition(candidate, radius, reserved)) continue;
            result = candidate;
            return true;
        }
        result = default;
        return false;
    }

    private bool SpawnTemporaryNear(
        UnitSnapshot? snapshot,
        int team,
        Vector2 origin,
        float healthScale,
        float damageScale,
        string sourceRuntimeId = "")
    {
        if (snapshot is null) return false;
        var position = FindOpenNear(origin, team, snapshot.BodyRadius);
        return position is { } spawnPosition && SpawnTemporary(
            snapshot, team, spawnPosition, healthScale, damageScale, sourceRuntimeId);
    }

    private bool SpawnTemporary(
        UnitSnapshot? snapshot,
        int team,
        Vector2 position,
        float healthScale,
        float damageScale,
        string sourceRuntimeId = "")
    {
        if (snapshot is null || !CanOccupyPosition(position, snapshot.BodyRadius)) return false;
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        var runtimeId = $"s-{team}-{_summonCounter++}";
        var maxHealth = snapshot.MaxHealth * healthScale;
        var damage = snapshot.Damage * damageScale;
        var lifeSteal = snapshot.LifeSteal;
        var unit = new BattleUnitState
        {
            RuntimeId = runtimeId,
            SourceInstanceId = string.Empty,
            Definition = snapshot,
            Attributes = _attributeScope.CreateSet(
                runtimeId,
                CreateBattleAttributeDefinition(snapshot, maxHealth, damage, lifeSteal)),
            Team = team,
            InitialTeam = team,
            SummonerRuntimeId = sourceRuntimeId,
            Cell = BattlefieldSpace.PositionToCell(position),
            Position = position,
            Health = maxHealth,
            IsTemporary = true
        };
        // Mid-battle join initializes this body once. Subsequent grants follow ordinary live
        // MaxHealth semantics; they do not heal recipients or replay the whole team's health.
        _units.Add(unit);
        _statistics.Add(unit.RuntimeId, new BattleUnitStatistics { JoinTick = TickIndex });
        BattleHeroMana.Initialize(unit);
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
        var healthBeforeRelics = unit.MaxHealth;
        _relicScope?.RefreshSummonModifiers();
        unit.Health += Math.Max(0, unit.MaxHealth - healthBeforeRelics);
        if (_abilityScope is not null && snapshot.AbilityLoadout is { } summonedLoadout)
        {
            _abilityScope.RegisterLoadout(runtimeId, summonedLoadout, TickIndex);
            _pendingAbilityReactions.Add(new PendingAbilityReaction(runtimeId, AbilityTriggerKind.None,
                string.Empty, TickIndex, $"{_combatPipeline.ScopeId}:summon:{runtimeId}", 0, PassiveGrant: true));
        }
        Emit("summoned", unit.RuntimeId, "", 0, position, "skill_cast");
        PublishCombat(new BattleCombatEventDraft(
            BattleCombatEventKind.UnitSummoned,
            ResolveCombatSource(sourceRuntimeId),
            sourceRuntimeId,
            unit.RuntimeId,
            TickIndex,
            Cell: ToCombatCell(unit.Cell),
            Position: ToCombatPoint(position),
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
            Emit("shield", unit.RuntimeId, unit.RuntimeId, behavior.PeriodicShieldAmount, unit.Position, "skill_cast");
        }
        if (behavior.PeriodicSummonTicks > 0 && TickIndex % behavior.PeriodicSummonTicks == 0 &&
            (behavior.PeriodicSummonLimit <= 0 || _units.Count(other => other.Team == unit.Team && other.IsTemporary && other.Alive) < behavior.PeriodicSummonLimit))
            SpawnTemporaryNear(unit.BehaviorSummon, unit.Team, unit.Position, .65f, .7f, unit.RuntimeId);
    }

    private BattleRuleContext CreateRuleContext() => new(
        TickIndex, _units, Allies,
        (source, target, amount) => ApplyFloorDamage(source, target, amount),
        (target, amount) => ApplyFloorHeal(target, amount),
        Emit, BeaconControlled);

    private void ApplyFloorDamage(string sourceRuntimeId, BattleUnitState target, float amount)
    {
        _effectCompatibility.Damage(sourceRuntimeId, target.RuntimeId, amount, TickIndex,
            new CombatSourceRef(CombatSourceKind.FloorRule, _config.FloorRule.Id, string.Empty, _config.FloorRule.Id));
    }

    private void ApplyFloorHeal(BattleUnitState target, float amount)
    {
        _effectCompatibility.FloorHeal(target.RuntimeId, amount, TickIndex,
            new CombatSourceRef(CombatSourceKind.FloorRule, _config.FloorRule.Id, string.Empty, _config.FloorRule.Id));
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
            unit.Definition.Tags.Concat(unit.Statuses.SelectMany(status => status.GrantedTags))
                .Distinct(StringComparer.Ordinal).ToImmutableArray(),
            unit.Attributes.Definition.Attributes.ToImmutableDictionary(attribute => attribute.Attribute,
                attribute => unit.Attributes.GetValue(attribute.Attribute)), unit.Position, unit.IsTemporary))) with
        {
            TeamCounts = Enum.GetValues<AttributeTeamCountKind>().SelectMany(kind => new[] { 0, 1 }.Select(team =>
                (Key: (kind, team), Value: TeamCount(kind, team)))).ToImmutableDictionary(item => item.Key, item => item.Value),
            TraitValues = CurrentTraitSnapshot.Values.ToImmutableDictionary(value => (value.TraitId, value.Team),
                value => (float)value.Value)
        };

    private EffectCommitOutcome CommitCompatibilityMutation(PreparedEffectMutation mutation)
    {
        var target = _units.FirstOrDefault(unit => unit.RuntimeId == mutation.Request.TargetId);
        if (target is null)
            return EffectCommitOutcome.Skipped(EffectInterruptionReason.TargetUnavailable, "Battle target no longer exists.");
        if (!target.Alive)
            return EffectCommitOutcome.Skipped(EffectInterruptionReason.TargetUnavailable, "Battle target is defeated.");
        var amount = mutation.Modifiers.ResolvedAmount;
        switch (mutation.Request.Kind)
        {
            case EffectKind.Damage:
                var beforeDamage = target.Health + target.Shield;
                // The migrated floor-rule delegate historically attributed by runtime id while
                // supplying no concrete source unit. Preserve that distinction until the complete
                // attack/death-chain ordering contract migrates together.
                ApplyDamage(mutation.Request.Context.SourceId, null, target, amount, mutation.Request.Context.Origin,
                    mutation.Request.DamageType);
                return EffectCommitOutcome.Succeeded(
                    amount,
                    Math.Max(0, beforeDamage - target.Health - target.Shield),
                    EffectDomainEventKind.DamageResolved);
            case EffectKind.Heal:
                var effectiveHealing = HealLiving(mutation.Request.Context.SourceId, target, amount, mutation.Request.Context.Origin);
                return EffectCommitOutcome.Succeeded(amount, effectiveHealing, EffectDomainEventKind.HealingResolved);
            case EffectKind.Shield:
                if (!target.Alive)
                    return EffectCommitOutcome.Skipped(EffectInterruptionReason.TargetUnavailable, "Battle target is defeated.");
                var effectiveShield = ApplyShield(mutation.Request.Context.SourceId, target, amount, mutation.Request.Context.Origin);
                return EffectCommitOutcome.Succeeded(effectiveShield, effectiveShield, EffectDomainEventKind.ShieldResolved);
            default:
                return EffectCommitOutcome.Failed(
                    $"Battle compatibility port does not own {mutation.Request.Kind} mutations.");
        }
    }

    private float HealLiving(string sourceRuntimeId, BattleUnitState target, float amount, CombatSourceRef origin = default)
    {
        if (!target.Alive || amount <= 0) return 0;
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        var combatSource = origin.IsSpecified ? origin : ResolveCombatSource(sourceRuntimeId);
        var calculated = _combatPipeline.Resolve(new BattleCombatCalculationRequest(
            BattleCombatCalculationKind.Healing,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            TickIndex,
            amount));
        var before = target.Health;
        target.Health = Math.Min(target.MaxHealth, target.Health + calculated.ResolvedAmount);
        RefreshGritActionRequests(target);
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
            Cell: ToCombatCell(target.Cell),
            Position: ToCombatPoint(target.Position)));
        resolution.Commit();
        return effectiveHealing;
    }

    private float ApplyShield(string sourceRuntimeId, BattleUnitState target, float amount, CombatSourceRef origin = default)
    {
        if (!target.Alive || amount <= 0) return 0;
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        var combatSource = origin.IsSpecified ? origin : ResolveCombatSource(sourceRuntimeId);
        var calculated = _combatPipeline.Resolve(new BattleCombatCalculationRequest(
            BattleCombatCalculationKind.Shield,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            TickIndex,
            amount));
        target.Shield += calculated.ResolvedAmount;
        if (calculated.ResolvedAmount > 0)
            Emit("vfx", sourceRuntimeId, target.RuntimeId, calculated.ResolvedAmount, target.Position, "",
                vfx: new BattleVfxCue(BattleVfxPhase.ShieldActive, "shield"));
        PublishCombat(new BattleCombatEventDraft(
            BattleCombatEventKind.ShieldResolved,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            TickIndex,
            RequestedValue: calculated.RequestedAmount,
            AppliedValue: calculated.ResolvedAmount,
            EffectiveValue: calculated.ResolvedAmount,
            Cell: ToCombatCell(target.Cell),
            Position: ToCombatPoint(target.Position)));
        resolution.Commit();
        return calculated.ResolvedAmount;
    }

    private void HandleDeath(BattleUnitState? source, BattleUnitState target)
    {
        if (!_deathProcUnits.Add(target.RuntimeId)) return;
        RemoveDefeatedTechniques(target);
        CancelEnemyAction(target.RuntimeId);
        RemoveEnemyProducts(target.RuntimeId);
        CancelChargedLine(target);
        CancelTrample(target);
        CancelDisplacement(target.RuntimeId);
        WriteCounter(target,"_dead_since",TickIndex);
        RevokePassiveGrants(target.RuntimeId);
        foreach (var unit in _units.Where(unit => unit.ActionTargetRuntimeId == target.RuntimeId))
            ClearActionTarget(unit);
        _movement?.ReleaseUnit(target.RuntimeId);
        _statusScope.HandleOwnerDeath(target.RuntimeId);
        if (target.Definition.Behavior.OnDeathDamage > 0)
            foreach (var enemy in Allies(1 - target.Team).Where(enemy => BattlefieldSpace.EdgeDistance(enemy, target) <= 1.5f).ToArray())
                ApplyDamage(target.RuntimeId, null, enemy, target.Definition.Behavior.OnDeathDamage);
        if (target.Team == 0 && _config.HeroRule.SummonOnAllyDeath && !target.Definition.IsHero && !target.IsTemporary)
            SpawnTemporaryNear(_config.Summons.DeathSummon, 0, target.Position, .6f, .65f, target.RuntimeId);
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
        ReleaseBattleLifecycleBindings(target.RuntimeId);
    }

    private float EffectiveDamage(BattleUnitState unit)
    {
        var multiplier = 1f;
        var adjacent = Allies(unit.Team).Where(ally => ally != unit && BattlefieldSpace.EdgeDistance(ally, unit) <= 1.5f).ToArray();
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
        var adjacent = Allies(unit.Team).Where(ally => ally != unit && BattlefieldSpace.EdgeDistance(ally, unit) <= 1.5f).ToArray();
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
            (unit.InitialTeam == 0 || unit.InitialTeam < 0 && unit.Team == 0) && unit.IsPersistentRosterHero && !unit.IsTemporary &&
            (unit.Alive || _mechanics.Revivals.Any(r => r.OwnerId == unit.RuntimeId)));
        var enemyAlive = _units.Any(unit => (unit.Alive && (unit.Team == 1 || _mechanics.Allegiances.Any(a => a.TargetId == unit.RuntimeId && a.OriginalTeam == 1))) ||
            unit.InitialTeam == 1 && _mechanics.Revivals.Any(r => r.OwnerId == unit.RuntimeId));
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
        CaptureTerminalSkillProgress();
        _projectiles.Clear();
        foreach (var unit in _units) { CancelTrample(unit); CancelChargedLine(unit); unit.ProjectileSequence = null; unit.ProjectileWindups = []; }
        _discardStatusEffects = true;
        _pendingSetupEffects.Clear();
        _pendingAbilityReactions.Clear();
        foreach (var subscription in _abilityCombatSubscriptions) subscription.Dispose();
        _abilityCombatSubscriptions.Clear();
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
        _mechanics = MechanicState.Empty;
        _techniques = TechniqueState.Empty;
        _unitActionQueues = ImmutableDictionary<string, UnitActionQueue>.Empty;
        _enemyActions = new(ImmutableDictionary<string, EnemyCast>.Empty,ImmutableDictionary<int, BladeFlight>.Empty,ImmutableDictionary<string, BroodState>.Empty,[]);
        _displacements = _displacements.Clear();
        _abilityTriggerEvent = null;
        _executingEcho = false;
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
    private static CombatPoint ToCombatPoint(Vector2 position) => new(position.X, position.Y);

    private void Emit(string type, string source, string target, float value, Vector2I cell, string cue)
        => Emit(type, source, target, value, BattlefieldSpace.CellCenter(cell), cue);

    private void Emit(string type, string source, string target, float value, Vector2 position, string cue,
        int entityId = 0, Vector2 origin = default, BattleVfxCue? vfx = null, BattleAttackTiming? attackTiming = null,
        BattleDisplacementCue? displacement = null, BattleLineCue? line = null, string sourceVfx = "", BattleTrampleCue? trample = null, BattleEnemyActionCue? enemyAction = null)
    {
        var cell = BattlefieldSpace.PositionToCell(position);
        var battleEvent = new BattleEvent(TickIndex, type, source, target, value, cell, cue, position, entityId, origin, vfx, attackTiming, displacement, line, sourceVfx, trample, enemyAction);
        _events.Add(battleEvent);
        _digest.Append(TickIndex).Append('|').Append(type).Append('|').Append(source).Append('|').Append(target).Append('|')
            .Append(value.ToString("0.###", CultureInfo.InvariantCulture)).Append('|')
            .Append(position.X.ToString("R", CultureInfo.InvariantCulture)).Append(',')
            .Append(position.Y.ToString("R", CultureInfo.InvariantCulture)).Append(';');
        if (entityId != 0 || type == "beam" || line is not null)
            _digest.Append(entityId).Append('@').Append(origin.X.ToString("R", CultureInfo.InvariantCulture))
                .Append(',').Append(origin.Y.ToString("R", CultureInfo.InvariantCulture)).Append(';');
        if (enemyAction is not null)
            _digest.Append(enemyAction.Vfx).Append('/').Append(enemyAction.Stage).Append('/').Append(enemyAction.StartTick).Append('/').Append(enemyAction.DurationTicks).Append(';');
        if (attackTiming is not null)
            _digest.Append(attackTiming.PlaybackSeconds.ToString("R", CultureInfo.InvariantCulture)).Append('/')
                .Append(attackTiming.ReleaseProgress.ToString("R", CultureInfo.InvariantCulture)).Append(';');
        if (trample is not null)
            _digest.Append(trample.StartTick).Append('/').Append(trample.ChargeTicks).Append('/')
                .Append(trample.Radius.ToString("R", CultureInfo.InvariantCulture)).Append('@')
                .Append(trample.End.X.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(trample.End.Y.ToString("R", CultureInfo.InvariantCulture)).Append(';');
        if (displacement is not null)
            _digest.Append(displacement.Kind).Append('@').Append(displacement.StartTick).Append('/')
                .Append(displacement.DurationTicks).Append(':')
                .Append(displacement.End.X.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(displacement.End.Y.ToString("R", CultureInfo.InvariantCulture)).Append('/')
                .Append(displacement.Progress.ToString("R", CultureInfo.InvariantCulture)).Append('/')
                .Append(displacement.Finished).Append('/').Append(displacement.Cancelled).Append(';');
    }
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
        private readonly ImmutableArray<BattleProjectileState> _projectileStates;
        private readonly int _projectileSequence;
        private readonly BattleSimulation _owner;
        private readonly MechanicState _mechanicState;
        private readonly EnemyActionState _enemyActionState;
        private readonly TechniqueState _techniqueState;
        private readonly ImmutableDictionary<string, UnitActionQueue> _unitActionQueueState;
        private readonly ImmutableDictionary<string, DisplacementMotion> _displacementState;
        private readonly ulong _randomState;
        private readonly BattleAbilityScope.ScopeStateCheckpoint? _abilityState;
        private readonly PendingAbilityReaction[] _abilityReactions;
        private readonly PendingSetupEffect[] _setupEffects;
        private readonly BattleAttributeScope.ScopeStateCheckpoint _attributeState;
        private readonly BattleCombatEventPipeline.CombatStateCheckpoint _combatState;
        private readonly BattleEffectScope.EffectStateCheckpoint _effectState;
        private readonly BattleStatusScope.WorldStateCheckpoint _statusState;
        private readonly BattleTraitScope.TraitStateCheckpoint _traitState;
        private readonly RelicBattleScope.RelicStateCheckpoint? _relicState;
        private readonly DeterministicContinuousMovementService? _movementOwner;
        private readonly DeterministicContinuousMovementService.MovementStateCheckpoint? _movementState;
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
            _mechanicState = owner._mechanics;
            _techniqueState = owner._techniques;
            _unitActionQueueState = owner._unitActionQueues;
            _enemyActionState = owner._enemyActions;
            _displacementState = owner._displacements;
            _randomState = owner._random.State;
            _abilityState = owner._abilityScope?.CaptureState();
            _abilityReactions = owner._pendingAbilityReactions.ToArray();
            _setupEffects = owner._pendingSetupEffects.ToArray();
            _projectileStates = owner.Projectiles;
            _projectileSequence = owner._projectileSequence;
            _attributeState = owner._attributeScope.CaptureState();
            _combatState = owner._combatPipeline.CaptureState();
            _effectState = owner._effectCompatibility.CaptureState();
            _traitState = owner._traitScope!.CaptureState();
            _relicState = owner._relicScope?.CaptureState();
            _movementOwner = owner._movement as DeterministicContinuousMovementService;
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
            if (_abilityState is not null) Restore(() => _owner._abilityScope!.RestoreState(_abilityState));
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

            _owner._mechanics = _mechanicState;
            _owner._techniques = _techniqueState;
            _owner._unitActionQueues = _unitActionQueueState;
            _owner._enemyActions = _enemyActionState;
            _owner._displacements = _displacementState;
            _owner._random.State = _randomState;
            _owner._units.Clear();
            _owner._pendingAbilityReactions.Clear();
            _owner._pendingAbilityReactions.AddRange(_abilityReactions);
            _owner._pendingSetupEffects.Clear();
            _owner._pendingSetupEffects.AddRange(_setupEffects);
            _owner._projectiles.Clear();
            foreach (var projectile in _projectileStates) _owner._projectiles.Add(projectile.Id, projectile);
            _owner._projectileSequence = _projectileSequence;
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
        Vector2 Position,
        float Health,
        float CurrentMana,
        int ManaLockedUntilTick,
        string LastAbilityName,
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
        string ActionTargetName,
        long AttackHitStacks, string AttackChainTargetId, double AttackCycleProgress, ProjectileSequenceState? ProjectileSequence,
        ImmutableArray<ProjectileWindupState> ProjectileWindups, int Team, ChargedLineState? ChargedLine, TrampleState? Trample, float? BodyRadiusOverride, Content.AttackDelivery? AttackDeliveryOverride, string ProjectileVisualId)
    {
        internal BattleUnitMutableState(BattleUnitState unit) : this(
            unit,
            unit.Position,
            unit.Health,
            unit.CurrentMana,
            unit.ManaLockedUntilTick,
            unit.LastAbilityName,
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
            unit.ActionTargetName, unit.AttackHitStacks, unit.AttackChainTargetId, unit.AttackCycleProgress, unit.ProjectileSequence, unit.ProjectileWindups, unit.Team, unit.ChargedLine, unit.Trample, unit.BodyRadiusOverride, unit.AttackDeliveryOverride, unit.ProjectileVisualId)
        {
        }

        internal void Restore()
        {
            Unit.Team = Team;
            Unit.Position = Position;
            Unit.Health = Health;
            Unit.CurrentMana = CurrentMana;
            Unit.ManaLockedUntilTick = ManaLockedUntilTick;
            Unit.LastAbilityName = LastAbilityName;
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
            Unit.AttackHitStacks = AttackHitStacks;
            Unit.AttackChainTargetId = AttackChainTargetId;
            Unit.AttackCycleProgress = AttackCycleProgress;
            Unit.ProjectileSequence = ProjectileSequence;
            Unit.ProjectileWindups = ProjectileWindups;
            Unit.ChargedLine = ChargedLine;
            Unit.Trample = Trample;
            Unit.BodyRadiusOverride = BodyRadiusOverride;
            Unit.AttackDeliveryOverride = AttackDeliveryOverride;
            Unit.ProjectileVisualId = ProjectileVisualId;
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
