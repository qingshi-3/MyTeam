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
    private readonly bool _preparationOnly;
    private readonly DeterministicRandom _random;
    private readonly List<BattleUnitState> _units = [];
    private readonly List<BattleEvent> _events = [];
    private readonly StringBuilder _digest = new();
    private readonly BattleVitalityScope _vitality;
    private readonly BattleStatistics _statistics;
    private readonly Dictionary<string, int> _bossPhaseIndexes = new(StringComparer.Ordinal);
    private readonly BattleEffectCompatibilityAdapter _effectCompatibility;
    private readonly BattleAttributeScope _attributeScope;
    private readonly BattleCombatEventPipeline _combatPipeline;
    private readonly BattleStatusScope _statusScope;
    private readonly List<StatusPresentationCue> _statusPresentationCues = [];
    private readonly IReadOnlyList<StatusPresentationCue> _statusPresentationCueView;
    private readonly RelicBattleScope? _relicScope;
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
    // Mutable compatibility access is internal to combat and its contract fixtures.
    internal IReadOnlyList<BattleUnitState> Units => _units;
    public ReadOnlyListView<BattleUnitView> UnitViews { get; }
    // Live UI reads an immutable report projection without ending combat or rebuilding its digest.
    public ImmutableArray<BattleUnitReportSnapshot> ReadUnitReports() =>
        _statistics.ReadReports(UnitViews, ActiveBattleTicks);
    public IReadOnlyList<BattleEvent> PendingEvents { get; }
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

    public BattleSimulation(BattleConfig config) : this(config, preparationOnly: false) { }

    private BattleSimulation(BattleConfig config, bool preparationOnly)
    {
        _preparationOnly = preparationOnly;
        _config = config;
        _statistics = new(config.CaptureDetailedStatistics && !preparationOnly);
        PendingEvents = _events.AsReadOnly();
        UnitViews = new(() => _units.Count, index => BattleUnitView.For(_units[index]));
        var enemyWorld = new EnemyWorld(this);
        _enemyRuntime = new BattleEnemyActionScope(enemyWorld, enemyWorld, enemyWorld);
        _projectileScope = new BattleProjectileScope(new ProjectileWorld(this));
        var matrixWorld = new MatrixWorld(this);
        _matrixQueries = new BattleMatrixQueries(matrixWorld);
        _matrixReactions = new BattleMatrixReactionScope(matrixWorld, _matrixQueries);
        _random = new DeterministicRandom(config.Seed);
        _statusPresentationCueView = _statusPresentationCues.AsReadOnly();
        _attributeScope = new BattleAttributeScope($"battle_{config.Seed}_attributes");
        _combatPipeline = new BattleCombatEventPipeline($"battle_{config.Seed}_combat", identity: config.Identity);
        RegisterEnemyDamageCalculations();
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
                traitValue: (traitId, team) => CurrentTraitSnapshot.Value(traitId, team)),
            controlGuardResolver: ownerId => _equipmentScope?.ResolveControlGuard(ownerId),
            controlGuardSink: fact =>
            {
                var recipient = _units.FirstOrDefault(unit => unit.RuntimeId == fact.OwnerId);
                if (recipient is not null) Emit("vfx", fact.OwnerId, fact.OwnerId, 0, recipient.Position, "", vfx: new BattleVfxCue(BattleVfxPhase.Burst, "cleanse"));
            });
        _values = new BattleValueLedger(() => TickIndex, () => _units, _statistics);
        _lifecycleRuntime = new BattleLifecycleScope(new LifecycleWorld(this), _values, _statistics, _statusScope, _projectileScope);
        _valueOperations = new BattleValueOperations(new ValueWorld(this), _values, _lifecycleRuntime, _statusScope, _statistics);
        _actionQueue = new BattleUnitActionQueueScope(() => TickIndex, () => _abilityScope);
        _techniqueRuntime = new BattleTechniqueScope(new TechniqueWorld(this), _statusScope, _actionQueue);
        _displacementRuntime = new BattleDisplacementScope(new DisplacementWorld(this), _statusScope);
        var directedActions = new DirectedActionWorld(this);
        _chargedLines = new BattleChargedLineScope(directedActions, _combatPipeline, _projectileScope);
        _tramples = new BattleTrampleScope(directedActions, _statusScope, _displacementRuntime);
        _attacks = new BattleAttackScope(new AttackWorld(this), _combatPipeline, _statistics, _statusScope, _random.NextFloat);
        _vitality = new BattleVitalityScope(new VitalityWorld(this), config, _combatPipeline, _statistics, _statusScope);
        var traitWorld = new TraitWorld(this);
        _traitBindings = new BattleTraitBindings(_combatPipeline.ScopeId);
        _elementStatuses = new BattleElementStatusScope(traitWorld, traitWorld, _traitBindings, _statusScope, config.MatrixStatusRules);
        _supportEffects = new BattleSupportEffectScope(traitWorld, traitWorld, _traitBindings);
        _traitReactions = new BattleTraitReactionScope(traitWorld, traitWorld, traitWorld, _traitBindings, _statusScope, _elementStatuses, _supportEffects);
        _matrixRuntime = new BattleMatrixAbilityScope(new MatrixOperationWorld(this), _matrixQueries, _matrixReactions,
            _displacementRuntime, _elementStatuses, _supportEffects, _statistics, _permanentGrowth);
        _targeting = new BattleTargetingPolicy(new TargetingWorld(this), config.HeroRule.PreferBossTargets);
        _skillProgress = new BattleSkillProgressProjection(new SkillProgressWorld(this));
        _abilityExecutor = new BattleAbilityExecutor(new AbilityHost(this),
            new BattleAbilityEffectOperations(new AbilityEffectWorld(this), _effectCompatibility, _statusScope,
                () => CurrentTraitSnapshot, _random.NextFloat),
            new BattleAbilitySummons(new AbilitySummonWorld(this)), new AbilityMechanisms(this));
        _relicScope = null;
        try
        {
            BindStatusRules();
            var combatBindings = new BattleCombatBindingRegistry(_combatPipeline);
            try { config.ConfigureCombatBindings?.Invoke(combatBindings); }
            finally { combatBindings.CloseRegistration(); }

            var unitFactory = new BattleUnitFactory(config, _attributeScope);
            var index = 0;
            foreach (var spawn in config.Spawns)
            {
                var requestedCell = ClampCell(spawn.Cell);
                var resolvedCell = CanOccupy(requestedCell) ? requestedCell : FindOpenCellNear(requestedCell, spawn.Team);
                // Placement cells are anchors, not one-cell bodies. Resolve the full authored circle
                // against terrain and previously placed bodies before publishing any battle state.
                var anchor = BattlefieldSpace.CellCenter(resolvedCell);
                anchor = new Vector2(Math.Clamp(anchor.X, -.5f + spawn.Unit.BodyRadius, Width - .5f - spawn.Unit.BodyRadius),
                    Math.Clamp(anchor.Y, -.5f + spawn.Unit.BodyRadius, Height - .5f - spawn.Unit.BodyRadius));
                var resolvedPosition = FindOpenNear(anchor, spawn.Team, spawn.Unit.BodyRadius)
                    ?? throw new InvalidOperationException($"No legal space for unit '{spawn.Unit.ContentId}'.");
                var state = unitFactory.CreateInitial(spawn, index, resolvedCell, resolvedPosition);
                _units.Add(state);
                _statistics.Join(state.RuntimeId, 0);
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
                    QueryUnit = runtimeId => _units.FirstOrDefault(unit => unit.RuntimeId == runtimeId) is { } unit
                        ? new EquipmentCombatUnit(unit.RuntimeId, unit.Team, unit.Alive) : null,
                    HealUnit = (runtimeId, amount, origin) =>
                    {
                        var target = _units.FirstOrDefault(unit => unit.RuntimeId == runtimeId);
                        if (target is not null) HealLiving(runtimeId, target, amount, origin);
                    },
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
                        HealUnit = (runtimeId, amount, origin) =>
                        {
                            var target = _units.FirstOrDefault(unit => unit.RuntimeId == runtimeId);
                            if (target is not null) HealLiving(runtimeId, target, amount, origin);
                        },
                        RemainingGold = () => RemainingGold,
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
            TraitMechanicsInitialize();
            BindMatrixAbilityEvents();
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
            if (!_preparationOnly) _movement = new DeterministicContinuousMovementService(
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
        if (_preparationOnly) throw new InvalidOperationException("A preparation world cannot advance combat.");
        if (Outcome != BattleOutcome.Running) return Outcome;
        TickIndex++;
        _statistics.AdvanceTick(TickIndex);
        try
        {
            AdvanceBattleLifecycles();
            AdvanceTechniques();
            TraitMechanicsAdvance();
            AdvanceMatrixAbilities();
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
            if (_relicScope?.HasDueEndOfTickRules == true)
            {
                var checkpoint = new BattleWorldStateCheckpoint(this);
                try
                {
                    using var resolution = _combatPipeline.BeginAuthoritativeResolution();
                    _relicScope.EndOfTick();
                    resolution.Commit();
                    DrainAbilityReactions();
                    checkpoint.Commit();
                }
                catch { checkpoint.Rollback(); throw; }
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
            TacticalPoints, source.Position, "skill_cast");
        DrainAbilityReactions();
        return activation;
    }


    public ImmutableArray<BattleOpeningAllyLink> OpeningAllyLinks => _permanentGrowth.OpeningLinks;

    public BattleResult CreateResult()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_digest.ToString()))).ToLowerInvariant();
        var units = _statistics.ReadReports(UnitViews, ActiveBattleTicks);
        return new BattleResult(Outcome, TickIndex, hash, units, GoldSpent, SuccessfulTacticalCommandUses,
            _relicScope?.Transition, _config.Identity, _permanentGrowth.Gains, _permanentGrowth.RememberedGains);
    }

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
            unit.BodyRadius, unit.Health, unit.Definition.IsBoss))
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
        foreach (var unit in _units.Where(unit => unit.AbilityLoadout is not null && !IsTimelineBoss(unit))
                     .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
            _abilityScope.RegisterLoadout(unit.RuntimeId, unit.AbilityLoadout!);
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
        unit.CurrentAbilityLoadout = loadout;
        unit.CurrentMana = Math.Clamp(unit.CurrentMana, 0, unit.MaxMana);
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
        var initial = _units.Where(unit => unit.Alive).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray();
        // Entry bindings/qualifications for all bodies precede any tick-zero cast.
        foreach (var unit in initial)
            ActivateTriggeredWithRepeats(unit.RuntimeId, AbilityTriggerKind.OwnerEnteredBattle, "");
        foreach (var unit in initial)
            _abilityScope.ActivateAutomatic(unit.RuntimeId, 0);
    }

    private string SelectAutomaticTarget(BattleUnitState owner, CompiledAbilityDefinition ability) => _targeting.SelectAutomaticTarget(owner, ability);

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
        var value = invocation.Kind == StatusEffectInvocationKind.Periodic ? invocation.PeriodicValue :
            invocation.UseOverheal ? Math.Max(0, (combatEvent?.AppliedValue ?? 0) - (combatEvent?.EffectiveValue ?? 0)) : combatEvent?.EffectiveValue ?? 0;
        if (_effectCompatibility.IsExecuting)
            return _effectCompatibility.EnqueueAuthored(invocation.Binding, invocation.SourceId, invocation.OwnerId,
                invocation.ExplicitTargetId, invocation.Tick, value,
                CombatSourceRef.Status(invocation.Definition.StableId, invocation.OwnerId, invocation.InstanceId));
        var result = _effectCompatibility.ExecuteAuthored(
            invocation.Binding,
            invocation.SourceId,
            invocation.OwnerId,
            invocation.ExplicitTargetId,
            invocation.Tick,
            value,
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

    private AbilityPreparationResult PrepareAbility(CompiledAbilityDefinition ability, string sourceId,
        string ownerId, string explicitTargetId, int tick) =>
        _abilityExecutor.Prepare(ability, sourceId, ownerId, explicitTargetId, tick);

    private AbilityCommitResult CommitAbility(AbilityExecutionPlan plan) => _abilityExecutor.Commit(plan);

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
            if (_statistics.CapturesDetails)
                _statistics.ControlTick(unit.RuntimeId, unit.Statuses, actionsDisabled, unit.DisabledTicks > 0, TickIndex);
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
        if (EnemyActionBusy(unit) || TechniqueBusy(unit) || UnitActionRecovering(unit) || MatrixHoldsAction(unit.RuntimeId)) { unit.Mode=BattleUnitMode.Casting; _movement!.ReleaseGoal(unit.RuntimeId); return; }
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
        if (!unit.Alive || EnemyActionBusy(unit) || TechniqueBusy(unit) || MatrixHoldsAction(unit.RuntimeId) || unit.Trample is not null || unit.ChargedLine is not null || IsDisplacing(unit) || TickIndex < unit.ManaLockedUntilTick) return;
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

        // Basic actions always attack. Healing is an authored ability/effect; a positive
        // healing attribute must not silently replace the unit's ordinary attack slot.
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
            if (_statusScope.HasTag(unit.RuntimeId, StatusDefinitionCompiler.AttackDisabledTag))
            { unit.Mode = BattleUnitMode.Waiting; return; }
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

    private BattleUnitState? SelectTarget(BattleUnitState owner) => _targeting.SelectTarget(owner);

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
                BattleUnitFactory.CreateAttributes(snapshot, maxHealth, damage, lifeSteal)),
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
        _statistics.Join(unit.RuntimeId, TickIndex);
        TraitMechanicsOnSummoned(unit);
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
            (unit.Alive || _lifecycleRuntime.RevivalPending(unit.RuntimeId)));
        var enemyAlive = _units.Any(unit => (unit.Alive && (unit.Team == 1 || _lifecycleRuntime.PreservesTeam(unit.RuntimeId, 1))) ||
            unit.InitialTeam == 1 && _lifecycleRuntime.RevivalPending(unit.RuntimeId));
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
        _projectileScope.Clear();
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
            if (!_preparationOnly) _statistics.Freeze(UnitViews, ActiveBattleTicks);
        });
        _lifecycleRuntime.Clear();
        _values.Clear();
        _techniqueRuntime.Clear();
        TraitMechanicsComplete();
        ClearMatrixAbilities();
        _abilityActionIds = _abilityActionIds.Clear();
        _actionQueue.Clear();
        _enemyRuntime.Clear();
        _displacementRuntime.Clear();
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
        _statistics.Observe(draft);
    }

    private static CombatCell ToCombatCell(Vector2I cell) => new(cell.X, cell.Y);
    private static CombatPoint ToCombatPoint(Vector2 position) => new(position.X, position.Y);

    private void Emit(string type, string source, string target, float value, Vector2I cell, string cue)
        => Emit(type, source, target, value, BattlefieldSpace.CellCenter(cell), cue);

    private void Emit(string type, string source, string target, float value, Vector2 position, string cue,
        int entityId = 0, Vector2 origin = default, BattleVfxCue? vfx = null, BattleAttackTiming? attackTiming = null,
        BattleDisplacementCue? displacement = null, BattleLineCue? line = null, string sourceVfx = "", BattleTrampleCue? trample = null, BattleEnemyActionCue? enemyAction = null)
    {
        if (_preparationOnly) return; // Presentation history/digest is not part of prepared unit state.
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

    private sealed class BattleWorldStateCheckpoint
    {
        private readonly BattleProjectileScope.Checkpoint _projectileState;
        private readonly BattleSimulation _owner;
        private readonly BattleLifecycleScope.Checkpoint _lifecycleState;
        private readonly BattleValueLedger.Checkpoint _valueState;
        private readonly BattleEnemyActionScope.Checkpoint _enemyActionState;
        private readonly BattleTechniqueScope.Checkpoint _techniqueState;
        private readonly BattleTraitReactionScope.Checkpoint _traitMechanicsState;
        private readonly BattleElementStatusScope.Checkpoint _elementState;
        private readonly BattleSupportEffectScope.Checkpoint _supportState;
        private readonly BattleMatrixAbilityScope.Checkpoint _matrixAbilitiesState;
        private readonly BattleMatrixReactionScope.Checkpoint _matrixReactionState;
        private readonly long _abilityActionSequence;
        private readonly ImmutableDictionary<string, string> _abilityActionIds;
        private readonly BattleUnitActionQueueScope.Checkpoint _unitActionQueueState;
        private readonly BattleDisplacementScope.Checkpoint _displacementState;
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
        private readonly BattleVitalityScope.Checkpoint _vitalityState;
        private readonly BattleStatistics.Checkpoint _statistics;
        private readonly Dictionary<string, int> _bossPhaseIndexes;
        private readonly int _statusPresentationCueCount;
        private readonly int _summonCounter;
        private readonly bool _floorRuleStartAttempted;
        private readonly bool _floorRuleEnded;
        private readonly int _tickIndex;
        private readonly BattleOutcome _outcome;
        private readonly int _goldSpent;
        private readonly int _successfulTacticalCommandUses;
        private readonly BattlePermanentGrowthScope.Checkpoint _permanentGrowth;
        private bool _finished;

        internal BattleWorldStateCheckpoint(BattleSimulation owner)
        {
            _owner = owner;
            _lifecycleState = owner._lifecycleRuntime.Capture();
            _valueState = owner._values.Capture();
            _techniqueState = owner._techniqueRuntime.Capture();
            _traitMechanicsState = owner._traitReactions.Capture();
            _elementState = owner._elementStatuses.Capture();
            _supportState = owner._supportEffects.Capture();
            _matrixAbilitiesState = owner._matrixRuntime.Capture();
            _matrixReactionState = owner._matrixReactions.Capture();
            _abilityActionSequence = owner._abilityActionSequence;
            _abilityActionIds = owner._abilityActionIds;
            _unitActionQueueState = owner._actionQueue.Capture();
            _enemyActionState = owner._enemyRuntime.Capture();
            _displacementState = owner._displacementRuntime.Capture();
            _randomState = owner._random.State;
            _abilityState = owner._abilityScope?.CaptureState();
            _abilityReactions = owner._pendingAbilityReactions.ToArray();
            _setupEffects = owner._pendingSetupEffects.ToArray();
            _projectileState = owner._projectileScope.Capture();
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
            _vitalityState = owner._vitality.Capture();
            _statistics = owner._statistics.Capture();
            _bossPhaseIndexes = new Dictionary<string, int>(owner._bossPhaseIndexes, StringComparer.Ordinal);
            _statusPresentationCueCount = owner._statusPresentationCues.Count;
            _summonCounter = owner._summonCounter;
            _floorRuleStartAttempted = owner._floorRuleStartAttempted;
            _floorRuleEnded = owner._floorRuleEnded;
            _tickIndex = owner.TickIndex;
            _outcome = owner.Outcome;
            _goldSpent = owner.GoldSpent;
            _successfulTacticalCommandUses = owner.SuccessfulTacticalCommandUses;
            _permanentGrowth = owner._permanentGrowth.Capture();
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

            _owner._lifecycleRuntime.Restore(_lifecycleState);
            _owner._values.Restore(_valueState);
            _owner._techniqueRuntime.Restore(_techniqueState);
            _owner._traitReactions.Restore(_traitMechanicsState);
            _owner._elementStatuses.Restore(_elementState);
            _owner._supportEffects.Restore(_supportState);
            _owner._matrixRuntime.Restore(_matrixAbilitiesState);
            _owner._matrixReactions.Restore(_matrixReactionState);
            _owner._abilityActionSequence = _abilityActionSequence;
            _owner._abilityActionIds = _abilityActionIds;
            _owner._actionQueue.Restore(_unitActionQueueState);
            _owner._enemyRuntime.Restore(_enemyActionState);
            _owner._displacementRuntime.Restore(_displacementState);
            _owner._random.State = _randomState;
            _owner._units.Clear();
            _owner._pendingAbilityReactions.Clear();
            _owner._pendingAbilityReactions.AddRange(_abilityReactions);
            _owner._pendingSetupEffects.Clear();
            _owner._pendingSetupEffects.AddRange(_setupEffects);
            _owner._projectileScope.Restore(_projectileState);
            foreach (var state in _unitStates)
            {
                state.Restore();
                _owner._units.Add(state.Unit);
            }
            if (_owner._events.Count > _eventCount)
                _owner._events.RemoveRange(_eventCount, _owner._events.Count - _eventCount);
            _owner._digest.Length = _digestLength;
            _owner._vitality.Restore(_vitalityState);
            _owner._statistics.Restore(_statistics);
            _owner._bossPhaseIndexes.Clear();
            foreach (var pair in _bossPhaseIndexes) _owner._bossPhaseIndexes.Add(pair.Key, pair.Value);
            if (_owner._statusPresentationCues.Count > _statusPresentationCueCount)
                _owner._statusPresentationCues.RemoveRange(
                    _statusPresentationCueCount,
                    _owner._statusPresentationCues.Count - _statusPresentationCueCount);
            _owner._summonCounter = _summonCounter;
            _owner._floorRuleStartAttempted = _floorRuleStartAttempted;
            _owner._floorRuleEnded = _floorRuleEnded;
            _owner.TickIndex = _tickIndex;
            _owner.Outcome = _outcome;
            _owner.GoldSpent = _goldSpent;
            _owner.SuccessfulTacticalCommandUses = _successfulTacticalCommandUses;
            _owner._permanentGrowth.Restore(_permanentGrowth);
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
        CompiledAbilityLoadout? CurrentAbilityLoadout,
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
            unit.CurrentAbilityLoadout,
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
            Unit.CurrentAbilityLoadout = CurrentAbilityLoadout;
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

}
