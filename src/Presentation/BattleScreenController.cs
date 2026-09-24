using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.UI;
using TowerAutobattler.Audio;

namespace TowerAutobattler.Presentation;

public partial class BattleScreenController : Control
{
    public event Action<BattleResult>? Finished;
    public event Action? EndTransitionFinished;
    public event Action? ResetRequested;
    public event Action? ReturnToConfigurationRequested;
    public event Action<int, int, string, bool, string>? TacticalCommandAttempted;

    private const float NormalSimulationScale = .8f;
    private const double EndHoldSeconds = 1.1;
    private const double EndFadeSeconds = .45;

    internal Func<CatalogEntry, UnitContentRoot> PresenterFactory { get; set; } =
        entry => entry.Scene.Instantiate<UnitContentRoot>();

    private BattleBoard _board = null!;
    private Node2D _unitsRoot = null!;
    private BattleGeometryOverlay _geometry = null!;
    private Button _collisionToggle = null!;
    private Button _attackRangeToggle = null!;
    private Label _geometryReadout = null!;
    private RangedAttackLayer _rangedAttackLayer = null!;
    private FeedbackAudio _audio = null!;
    private BattleFloatingCueLayer _floatingCueLayer = null!;
    private Label _title = null!;
    private Label _rule = null!;
    private Button _ruleButton = null!;
    private ContextPopup _rulesPopup = null!;
    private BattleStatusStrip _status = null!;
    private Button _pause = null!;
    private Button _speed = null!;
    private Button _step = null!;
    private Button _reset = null!;
    private Button _returnToConfiguration = null!;
    private TacticalCommandHud _tacticalCommandHud = null!;
    private SelectedUnitPanel _selectedUnit = null!;
    private BattleInspectorDock _inspector = null!;
    private ColorRect _endFadeOverlay = null!;
    private readonly Dictionary<string, UnitContentRoot> _presenters = new(StringComparer.Ordinal);
    private ContentRegistry? _content;
    private BattleConfig? _config;
    private BattleSimulation? _simulation;
    private double _accumulator;
    private float _speedScale = 1f;
    private bool _paused;
    private bool _reported;
    private bool _ending;
    private bool _endTransitionReported;
    private Tween? _endTween;
    private BattleResult? _terminalResult;
    private string _commandFeedback = string.Empty;
    private bool _commandFeedbackError;
    private string _selectedRuntimeId = string.Empty;
    private int _combatCueCursor;
    private int _statusCueCursor;
    public string LastRuntimeFailure { get; private set; } = string.Empty;
    public float SpeedScale => _speedScale;
    public int TacticalPoints => _simulation?.TacticalPoints ?? 0;
    public int MaximumTacticalPoints => _simulation?.MaximumTacticalPoints ?? 0;
    public int RemainingGold => _simulation?.RemainingGold ?? 0;
    public int TemporaryUnitCount => _simulation?.Units.Count(unit => unit.IsTemporary) ?? 0;
    public string CommandFeedback => _commandFeedback;
    public bool IsEnding => _ending;
    public bool IsPaused => _paused;
    public int TickIndex => _simulation?.TickIndex ?? 0;
    public BattleOutcome Outcome => _simulation?.Outcome ?? _terminalResult?.Outcome ?? BattleOutcome.Running;
    public bool HasActiveBattle => _simulation is not null;
    public BattleResult? TerminalResult => _terminalResult;
    public int MaximumFloatingCueCount => BattleFloatingCueLayer.MaximumCueCount;
    public int ActiveFloatingCueCount => _floatingCueLayer?.ActiveCueCount ?? 0;
    public int ActiveFloatingTweenCount => _floatingCueLayer?.ActiveTweenCount ?? 0;

    public override void _Ready()
    {
        _board = GetNode<BattleBoard>("%BattleBoard");
        _unitsRoot = GetNode<Node2D>("%UnitsRoot");
        _geometry = GetNode<BattleGeometryOverlay>("%BattleGeometryOverlay");
        _collisionToggle = GetNode<Button>("%CollisionToggle");
        _attackRangeToggle = GetNode<Button>("%AttackRangeToggle");
        _geometryReadout = GetNode<Label>("%GeometryReadout");
        _collisionToggle.Toggled += OnGeometryToggled;
        _attackRangeToggle.Toggled += OnGeometryToggled;
        _rangedAttackLayer = GetNode<RangedAttackLayer>("%RangedAttackLayer");
        _audio = GetNode<FeedbackAudio>("BattleAudio");
        _rangedAttackLayer.Bind(_board);
        _rangedAttackLayer.BindUnitPositions(id => _presenters.TryGetValue(id, out var unit) ? unit.Position : null);
        _rangedAttackLayer.BindUnitBodyPositions(id => _presenters.TryGetValue(id, out var unit)
            ? _unitsRoot.ToLocal(unit.BodyGlobalPosition) : null);
        _rangedAttackLayer.BindUnitBodyVisuals(id => _presenters.TryGetValue(id, out var unit)
            ? unit.CaptureBodyVisual(_rangedAttackLayer.GlobalTransform.AffineInverse()) : null);
        _floatingCueLayer = GetNode<BattleFloatingCueLayer>("%FloatingCueOverlay");
        _title = GetNode<Label>("%BattleTitle");
        _rule = GetNode<Label>("%RuleText");
        _ruleButton = GetNode<Button>("%RuleButton");
        _rulesPopup = GetNode<ContextPopup>("%RulesPopup");
        _ruleButton.Pressed += ShowRules;
        _status = GetNode<BattleStatusStrip>("%BattleStatus");
        _pause = GetNode<Button>("%PauseButton");
        _speed = GetNode<Button>("%SpeedButton");
        _step = GetNode<Button>("%StepButton");
        _reset = GetNode<Button>("%ResetBattleButton");
        _returnToConfiguration = GetNode<Button>("%ReturnConfigurationButton");
        _tacticalCommandHud = GetNode<TacticalCommandHud>("%TacticalCommandHud");
        _inspector = GetNode<BattleInspectorDock>("%BattleInspectorDock");
        _selectedUnit = _inspector.Details;
        _inspector.StatisticsRequested += RefreshInspectorStatistics;
        _inspector.UnitSelected += OnActorSelected;
        _endFadeOverlay = GetNode<ColorRect>("%EndFadeOverlay");
        _pause.Pressed += TogglePause;
        _speed.Pressed += CycleSpeed;
        _step.Pressed += OnStepPressed;
        _reset.Pressed += OnResetRequested;
        _returnToConfiguration.Pressed += OnReturnToConfigurationRequested;
        _tacticalCommandHud.UseRequested += UseCommand;
        _board.GuiInput += OnBoardGuiInput;
        _board.ProjectionChanged += OnBoardProjectionChanged;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        _collisionToggle.Toggled -= OnGeometryToggled;
        _attackRangeToggle.Toggled -= OnGeometryToggled;
        _inspector.StatisticsRequested -= RefreshInspectorStatistics;
        _ruleButton.Pressed -= ShowRules;
        _inspector.UnitSelected -= OnActorSelected;
        _pause.Pressed -= TogglePause;
        _speed.Pressed -= CycleSpeed;
        _step.Pressed -= OnStepPressed;
        _reset.Pressed -= OnResetRequested;
        _returnToConfiguration.Pressed -= OnReturnToConfigurationRequested;
        _tacticalCommandHud.UseRequested -= UseCommand;
        _board.GuiInput -= OnBoardGuiInput;
        _board.ProjectionChanged -= OnBoardProjectionChanged;
        ResetEndSequence();
        ClearPresenters();
    }

    public void StartBattle(ContentRegistry content, BattleConfig config, string title, float defaultSpeed = 1f)
    {
        ResetEndSequence();
        ClearPresenters(replacement: true);
        try
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            LastRuntimeFailure = string.Empty;
            _simulation = new BattleSimulation(config);
            _title.Text = title;
            _rule.Text = $"{config.FloorRule.DisplayName}：{config.FloorRule.PreviewText}";
            _board.Bind(config.FloorRule);
            _accumulator = 0;
            _speedScale = NormalizeSpeed(defaultSpeed);
            _paused = false;
            _rangedAttackLayer.SetClock(false, _speedScale * NormalSimulationScale);
            _reported = false;
            _ending = false;
            _terminalResult = null;
            _pause.Text = "暂停";
            _pause.Disabled = false;
            _speed.Text = $"速度 x{_speedScale:0}";
            _speed.Disabled = false;
            _board.MouseFilter = MouseFilterEnum.Stop;
            _commandFeedback = string.Empty;
            _commandFeedbackError = false;
            _selectedRuntimeId = string.Empty;
            _inspector.ResetSelection();
            RefreshCommandHud();
            RefreshStatus();
            SyncPresenters("idle");
            _rangedAttackLayer.SynchronizeUnits(_simulation.Units);
            PresentResolvedFacts();
            SetProcess(true);
            SetProcessUnhandledInput(false);
        }
        catch (Exception exception)
        {
            CleanupRuntimeFailure(exception);
            throw;
        }
    }

    public void SetLabControlsVisible(bool visible)
    {
        _step.Visible = visible;
        _reset.Visible = visible;
        _returnToConfiguration.Visible = visible;
        _step.Disabled = !visible || !_paused || _ending || _simulation?.Outcome != BattleOutcome.Running;
        RefreshCommandHud();
        if (_endFadeOverlay is not null)
            _endFadeOverlay.MouseFilter = visible ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
    }

    public void SetPaused(bool paused)
    {
        if (_ending || _simulation?.Outcome != BattleOutcome.Running) return;
        _paused = paused;
        _pause.Text = _paused ? "继续" : "暂停";
        _step.Disabled = !_step.Visible || !_paused;
        // Resuming an animation may emit its pending first-frame marker immediately.
        _audio.SetPaused(_paused);
        foreach (var presenter in _presenters.Values) presenter.SetPresentationPaused(_paused);
        _rangedAttackLayer.SetClock(_paused, _speedScale * NormalSimulationScale);
        RefreshGeometry();
    }

    public void SetSpeed(float speed)
    {
        if (_ending) return;
        _speedScale = NormalizeSpeed(speed);
        _speed.Text = $"速度 x{_speedScale:0}";
        foreach (var presenter in _presenters.Values) presenter.SetPresentationSpeed(_speedScale, _speedScale * NormalSimulationScale);
        _rangedAttackLayer.SetClock(_paused, _speedScale * NormalSimulationScale);
    }

    public bool StepOneTick()
    {
        if (!_paused || _ending || _simulation is null || _simulation.Outcome != BattleOutcome.Running)
            return false;
        try
        {
            foreach (var presenter in _presenters.Values) presenter.StepAttackPresentation(BattleTiming.TickSeconds);
            _simulation.Step();
            PresentEvents(_simulation.DrainEvents());
            SnapPresentersToAuthority();
            PresentResolvedFacts();
            RefreshStatus();
            RefreshSelectedUnit();
            if (_simulation.Outcome != BattleOutcome.Running && !_reported) BeginEndSequence();
            return true;
        }
        catch (Exception exception)
        {
            CleanupRuntimeFailure(exception);
            throw;
        }
    }

    public ImmutableArray<BattleScreenRuntimeUnitSnapshot> ReadRuntimeUnits() => _simulation?.Units
        .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal)
        .Select(BuildRuntimeSnapshot)
        .ToImmutableArray() ?? [];

    public void StopBattle(bool replacement = true)
    {
        ResetEndSequence();
        ClearPresenters(replacement);
        SetProcess(false);
        SetLabControlsVisible(false);
    }

    public override void _Process(double delta)
    {
        if (_ending || _paused || _simulation is null || _simulation.Outcome != BattleOutcome.Running) return;
        try
        {
            _accumulator += delta * _speedScale * NormalSimulationScale;
            var steps = 0;
            while (_accumulator >= BattleTiming.TickSeconds && steps++ < 12 && _simulation.Outcome == BattleOutcome.Running)
            {
                _accumulator -= BattleTiming.TickSeconds;
                _simulation.Step();
                PresentEvents(_simulation.DrainEvents());
                PresentResolvedFacts();
            }
            RefreshStatus();
            if (_simulation.Outcome != BattleOutcome.Running && !_reported)
                BeginEndSequence();
        }
        catch (Exception exception)
        {
            CleanupRuntimeFailure(exception);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_ending || _endTransitionReported || !@event.IsActionPressed("ui_accept")) return;
        GetViewport().SetInputAsHandled();
        CompleteEndTransition();
    }

    private void PresentEvents(IReadOnlyList<BattleEvent> events)
    {
        foreach (var change in events.Where(e => e.Type is "revived" or "allegiance"))
        {
            var id = change.TargetRuntimeId;
            if (_presenters.Remove(id,out var previous))
            {
                previous.ActorSelected -= OnActorSelected;
                previous.AnimationSoundRequested -= OnAnimationSoundRequested;
                previous.Deactivate(); previous.GetParent()?.RemoveChild(previous); previous.Free();
            }
            EnsurePresenter(id);
        }
        BattleAudioFeedback.Present(_audio, events,
            id => FindState(id)?.AttackDelivery ?? AttackDelivery.Melee,
            (id, cue) =>
            {
                EnsurePresenter(id);
                return _presenters.TryGetValue(id, out var actor) && actor.HasAnimationSounds(cue);
            });
        PresentDisplacements(events);
        var displacedOwners = events.Where(fact => fact.Displacement is not null)
            .Select(fact => fact.TargetRuntimeId).ToHashSet(StringComparer.Ordinal);
        var timedAttackOwners = new HashSet<string>(StringComparer.Ordinal);
        foreach (var fact in events)
        {
            var released = fact.Type is "attack" or "skill_arrow" && FindState(fact.SourceRuntimeId)?.Definition.ProjectileWindupSeconds > 0;
            if (fact.AttackTiming is null && fact.Type is not ("attack_cancel" or "line_cancel" or "line_release" or "blade_release") && !released) continue;
            EnsurePresenter(fact.SourceRuntimeId);
            if (!_presenters.TryGetValue(fact.SourceRuntimeId, out var actor)) continue;
            if (fact.AttackTiming is { } timing)
            {
                actor.FaceToward(_board.LogicalToLocal(fact.Position));
                actor.BeginAttackPresentation(timing);
                timedAttackOwners.Add(fact.SourceRuntimeId);
            }
            else if (fact.Type == "line_release") actor.CompleteAttackWindup(fact.Line?.ReleaseProgress ?? .8f);
            else if (fact.Type == "blade_release") actor.CompleteAttackWindup(FindState(fact.SourceRuntimeId)!.Definition.AttackReleaseProgress);
            else if (released) actor.CompleteAttackWindup(FindState(fact.SourceRuntimeId)!.Definition.AttackReleaseProgress);
            else actor.CancelAttackPresentation();
        }
        _rangedAttackLayer.Present(events, _paused);
        if (_simulation is not null && !events.Any(e => e.Type == "battle_finished"))
            _rangedAttackLayer.SynchronizeUnits(_simulation.Units);
        foreach (var battleEvent in events)
        {
            if (battleEvent.Type != "move" || string.IsNullOrWhiteSpace(battleEvent.SourceRuntimeId) ||
                displacedOwners.Contains(battleEvent.SourceRuntimeId)) continue;
            EnsurePresenter(battleEvent.SourceRuntimeId);
            if (_presenters.TryGetValue(battleEvent.SourceRuntimeId, out var mover))
            {
                var logicalPosition = battleEvent.Position != default
                    ? battleEvent.Position
                    : BattlefieldSpace.CellCenter(battleEvent.Cell);
                mover.QueueMovement(_board.LogicalToLocal(logicalPosition));
            }
        }

        foreach (var battleEvent in events.Where(battleEvent => battleEvent.Type is "attack" or "skill_arrow" or "heal" or "ability"))
        {
            EnsurePresenter(battleEvent.SourceRuntimeId);
            EnsurePresenter(battleEvent.TargetRuntimeId);
            if (_presenters.TryGetValue(battleEvent.SourceRuntimeId, out var source) &&
                _presenters.TryGetValue(battleEvent.TargetRuntimeId, out var target))
                source.FaceToward(target.Position);
            if (battleEvent.Type == "ability" && _presenters.TryGetValue(battleEvent.SourceRuntimeId, out var caster))
                caster.PresentAbility(FindState(battleEvent.SourceRuntimeId)?.LastAbilityName ?? "技能");
        }
        foreach (var fact in events.Where(fact => fact.Type == "line_charge"))
            if (_presenters.TryGetValue(fact.SourceRuntimeId, out var caster))
                caster.PresentAbility(FindState(fact.SourceRuntimeId)?.LastAbilityName ?? "蓄力");

        var presented = new HashSet<string>(StringComparer.Ordinal);
        foreach (var selection in BattlePresentationCueArbiter.Select(events))
        {
            var presentationId = selection.Key;
            EnsurePresenter(presentationId);
            if (_presenters.TryGetValue(presentationId, out var presenter))
            {
                var state = FindState(presentationId);
                if (state is not null)
                {
                    presenter.RefreshPresentation(state.Alive && timedAttackOwners.Contains(presentationId) ? "" : selection.Value, state.Health, state.MaxHealth);
                    presented.Add(presentationId);
                }
            }
        }
        SyncPresenters("idle", presented);
        RefreshSelectedUnit();
    }

    private void PresentResolvedFacts()
    {
        if (_simulation is null) return;
        var combatStart = Math.Min(_combatCueCursor, _simulation.CombatEvents.Count);
        var combatFacts = _simulation.CombatEvents.Skip(combatStart).ToArray();
        var statusStart = Math.Min(_statusCueCursor, _simulation.StatusPresentationCues.Count);
        var statusCues = _simulation.StatusPresentationCues.Skip(statusStart).ToArray();
        foreach (var fact in combatFacts)
        {
            _audio.Play(BattleAudioFeedback.CombatCue(fact,
                FindState(fact.SourceRuntimeId)?.AttackDelivery ?? AttackDelivery.Melee));
            if (fact.Kind == BattleCombatEventKind.AbilityResolved &&
                !(_presenters.TryGetValue(fact.SourceRuntimeId, out var caster) && caster.HasAnimationSounds("skill_cast")))
                _audio.Play(_audio.AbilityCue(fact.SubjectStableId));
        }
        foreach (var cue in statusCues) _audio.Play(BattleAudioFeedback.StatusCue(cue));
        _floatingCueLayer.Present(combatFacts, statusCues,
            id => FindState(id)?.Position, _board.LogicalToLocal);
        _combatCueCursor = _simulation.CombatEvents.Count;
        _statusCueCursor = _simulation.StatusPresentationCues.Count;
        RefreshGeometry();
    }

    private void ClearFloatingCues() => _floatingCueLayer?.Clear();

    private void SyncPresenters(string cue, IReadOnlySet<string>? preserveCue = null)
    {
        if (_simulation is null) return;
        foreach (var state in _simulation.Units)
        {
            EnsurePresenter(state.RuntimeId);
            if (_presenters.TryGetValue(state.RuntimeId, out var presenter))
            {
                presenter.RefreshPresentation(state.Alive && preserveCue?.Contains(state.RuntimeId) == true ? "" : state.Alive ? cue : "defeated",
                    state.Health, state.MaxHealth);
                presenter.RefreshBattleForm(state.BodyRadius);
                presenter.RefreshCombatResources(state.CurrentMana, state.MaxMana, state.Shield, state.Statuses, state.Alive,
                    _simulation.ReadUnitSkills(state.RuntimeId).Primary);
            }
        }
    }

    private void EnsurePresenter(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) || _presenters.ContainsKey(runtimeId) || _content is null) return;
        var state = FindState(runtimeId);
        if (state is null || !_content.TryGet(state.Definition.ContentId, out var entry)) return;
        UnitContentRoot? presenter = null;
        var subscribed = false;
        try
        {
            presenter = PresenterFactory(entry);
            _unitsRoot.AddChild(presenter);
            presenter.Bind(state.RuntimeId, state.Team, state.Health, state.MaxHealth);
            presenter.ActorSelected += OnActorSelected;
            presenter.AnimationSoundRequested += OnAnimationSoundRequested;
            subscribed = true;
            presenter.Activate(new UnitBindingContext(
                new DeterministicRandom(1), NullEvents.Instance, NullCommands.Instance));
            presenter.SetPresentationSpeed(_speedScale, _speedScale * NormalSimulationScale);
            presenter.SetPresentationPaused(_paused);
            presenter.Scale = Vector2.One * _board.CurrentProjection.UnitScale;
            presenter.SnapPresentation(_board.LogicalToLocal(state.Position), state.Health, state.MaxHealth);
            _presenters.Add(runtimeId, presenter);
        }
        catch
        {
            if (presenter is not null)
            {
                if (subscribed)
                {
                    presenter.ActorSelected -= OnActorSelected;
                    presenter.AnimationSoundRequested -= OnAnimationSoundRequested;
                }
                try { presenter.Deactivate(); }
                catch { }
                presenter.GetParent()?.RemoveChild(presenter);
                presenter.Free();
            }
            throw;
        }
    }

    private BattleUnitState? FindState(string runtimeId) => _simulation?.Units.FirstOrDefault(unit => unit.RuntimeId == runtimeId);
    private void OnAnimationSoundRequested(FeedbackSound sound)
    {
        if (!_ending && !_reported) _audio.Play(sound);
    }

    private void ShowRules() => _rulesPopup.Open(_ruleButton);

    private void TogglePause()
    {
        SetPaused(!_paused);
    }

    private void CycleSpeed()
    {
        if (_ending) return;
        SetSpeed(_speedScale switch { 1f => 2f, 2f => 4f, _ => 1f });
    }

    private void OnStepPressed() => StepOneTick();
    private void OnResetRequested() => ResetRequested?.Invoke();
    private void OnReturnToConfigurationRequested() => ReturnToConfigurationRequested?.Invoke();

    private void UseCommand(int slotIndex)
    {
        if (_ending || _simulation is null) return;
        var result = _simulation.TryUseTacticalCommand(slotIndex, _selectedRuntimeId);
        TacticalCommandAttempted?.Invoke(_simulation.TickIndex, slotIndex, _selectedRuntimeId, result.Succeeded, result.FailureReason);
        if (result.Succeeded)
        {
            PresentEvents(_simulation.DrainEvents());
            PresentResolvedFacts();
            _commandFeedback = result.GoldSpent > 0 ? "战术指令已发动，金币已支付。" : "战术指令已发动。";
            _commandFeedbackError = false;
        }
        else
        {
            _commandFeedback = result.FailureReason;
            _commandFeedbackError = true;
        }
        RefreshCommandHud();
        RefreshStatus();
    }

    private void RefreshCommandHud()
    {
        if (_simulation is null) return;
        // Lab presets without commands should give this space back to the board and unit details.
        _tacticalCommandHud.Visible = !_step.Visible || !_simulation.TacticalCommands.Slots.IsDefaultOrEmpty;
        _tacticalCommandHud.Bind(
            _simulation.TacticalCommands,
            _commandFeedback,
            _commandFeedbackError,
            _simulation.Outcome == BattleOutcome.Running);
    }

    private void RefreshStatus()
    {
        if (_simulation is null) return;
        _status.Bind(_simulation.TickIndex * BattleTiming.TickSeconds,
            _simulation.Units.Count(unit => unit.Team == 0 && unit.Alive),
            _simulation.Units.Count(unit => unit.Team == 1 && unit.Alive),
            _simulation.TacticalCommands.Slots.Any(slot => slot.GoldCost > 0), _simulation.RemainingGold,
            _commandFeedback, _commandFeedbackError);
        RefreshInspectorStatistics();
    }

    private void RefreshInspectorStatistics()
    {
        if (_simulation is not null && _inspector.WantsStatistics)
            _inspector.BindStatistics(_simulation.ReadUnitReports(), ResolveUnitPortrait);
    }

    private UnitPortraitDefinition? ResolveUnitPortrait(string contentId) =>
        _content is not null && _content.TryGet(contentId, out var entry) && entry.Definition is UnitDefinition unit
            ? unit.Portrait : null;

    private void OnActorSelected(string runtimeId)
    {
        if (_ending) return;
        _selectedRuntimeId = runtimeId;
        RefreshSelectedUnit();
        RefreshGeometry();
        if (!GeometryEnabled) _inspector.ShowUnitDetails();
    }

    private void OnBoardGuiInput(InputEvent @event)
    {
        if (_ending || @event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse || _simulation is null) return;
        var presenter = _presenters.Values
            .Where(candidate => candidate.Visible && FindState(candidate.RuntimeId)?.Alive == true)
            .OrderBy(candidate => candidate.Position.DistanceSquaredTo(mouse.Position))
            .FirstOrDefault(candidate =>
            {
                var delta = (mouse.Position - candidate.Position) / _board.CurrentProjection.CellPitch;
                return delta.Length() <= (FindState(candidate.RuntimeId)?.BodyRadius ?? 0) ||
                    candidate.Position.DistanceTo(mouse.Position) <= _board.CurrentProjection.SelectionRadius;
            });
        if (presenter is null) return;
        presenter.RequestSelection();
        _board.AcceptEvent();
    }

    private void RefreshSelectedUnit()
    {
        if (string.IsNullOrWhiteSpace(_selectedRuntimeId)) return;
        var state = FindState(_selectedRuntimeId);
        if (state is not null) _inspector.BindSelection(BuildRuntimeSnapshot(state), ResolveUnitPortrait(state.Definition.ContentId));
    }

    private BattleScreenRuntimeUnitSnapshot BuildRuntimeSnapshot(BattleUnitState unit) =>
        BattleRuntimeUnitProjection.Build(unit, _content, _config, _simulation?.TraitSnapshot,
            _simulation?.DescribeBattleResources(unit.RuntimeId) ?? "", _simulation?.ReadUnitSkills(unit.RuntimeId));

    private bool GeometryEnabled => _collisionToggle.ButtonPressed || _attackRangeToggle.ButtonPressed;

    private void OnGeometryToggled(bool enabled)
    {
        if (GeometryEnabled) _inspector.CollapsePanel();
        RefreshGeometry();
    }

    private void RefreshGeometry()
    {
        _geometryReadout.Visible = GeometryEnabled && _simulation is not null;
        if (_simulation is null || !GeometryEnabled)
        {
            _geometry.Clear();
            return;
        }
        var snapshot = _simulation.ReadSpatialSnapshot();
        if (!snapshot.Units.Any(unit => unit.RuntimeId == _selectedRuntimeId))
        {
            _selectedRuntimeId = snapshot.Units.OrderByDescending(unit => unit.BodyRadius).FirstOrDefault()?.RuntimeId ?? "";
            RefreshSelectedUnit();
        }
        var selected = snapshot.Units.FirstOrDefault(unit => unit.RuntimeId == _selectedRuntimeId);
        _geometryReadout.Text = selected is null ? "" : $"{selected.DisplayName} · 直径 {selected.BodyRadius * 2:0.##} 格 · 普攻 {selected.AttackReach:0.##} 格";
        // Diagnostic mode displays the actual committed positions instead of allowing interpolation
        // lag to separate an actor from its collision circle. It never writes simulation state.
        SnapPresentersToAuthority();
        _geometry.Bind(snapshot, _board.CurrentProjection, _selectedRuntimeId,
            _collisionToggle.ButtonPressed, _attackRangeToggle.ButtonPressed);
    }

    private void SnapPresentersToAuthority()
    {
        if (_simulation is null) return;
        foreach (var state in _simulation.Units)
        {
            EnsurePresenter(state.RuntimeId);
            if (_presenters.TryGetValue(state.RuntimeId, out var presenter))
                presenter.SnapSpatialPresentation(_board.LogicalToLocal(state.Position));
        }
    }

    private void OnBoardProjectionChanged(BattlefieldProjection previous, BattlefieldProjection next)
    {
        foreach (var presenter in _presenters.Values)
        {
            presenter.RemapPresentationCoordinates(previous.Origin, previous.CellPitch, next.Origin, next.CellPitch);
            presenter.Scale = Vector2.One * next.UnitScale;
        }
        if (_geometry is not null) RefreshGeometry();
    }

    private void ClearPresenters(bool replacement = false)
    {
        _audio?.Clear();
        _rangedAttackLayer?.Clear();
        _geometry?.Clear();
        if (_geometryReadout is not null) _geometryReadout.Visible = false;
        if (replacement)
        {
            try { _simulation?.Replace(); }
            catch (Exception exception) { GD.PushError($"Battle replacement failed: {exception.Message}"); }
        }
        try { _simulation?.Dispose(); }
        catch (Exception exception) { GD.PushError($"Battle abort failed: {exception.Message}"); }
        ClearFloatingCues();
        foreach (var presenter in _presenters.Values)
        {
            presenter.ActorSelected -= OnActorSelected;
            presenter.AnimationSoundRequested -= OnAnimationSoundRequested;
            presenter.Deactivate();
            if (presenter.GetParent() is not null) presenter.GetParent().RemoveChild(presenter);
            presenter.Free();
        }
        _presenters.Clear();
        _selectedRuntimeId = string.Empty;
        if (_inspector is not null) _inspector.ResetSelection();
        _simulation = null;
        _config = null;
        _content = null;
        _combatCueCursor = 0;
        _statusCueCursor = 0;
    }

    private void CleanupRuntimeFailure(Exception exception)
    {
        LastRuntimeFailure = exception.Message;
        ResetEndSequence();
        ClearPresenters(replacement: true);
        SetProcess(false);
        SetLabControlsVisible(false);
    }

    private void BeginEndSequence()
    {
        if (_ending || _reported || _simulation is null || _simulation.Outcome == BattleOutcome.Running) return;
        _ending = true;
        // Scope completion can remove an airborne motion without a per-unit finish fact.
        // The held result screen must retain ground positions, never suspended leap poses.
        foreach (var state in _simulation.Units)
            if (_presenters.TryGetValue(state.RuntimeId, out var presenter))
                presenter.SnapPresentation(_board.LogicalToLocal(state.Position), state.Health, state.MaxHealth);
        _rangedAttackLayer.Clear();
        _rangedAttackLayer.SetClock(false, _speedScale * NormalSimulationScale);
        _reported = true;
        _accumulator = 0;
        _terminalResult = _simulation.CreateResult();
        _audio.Clear();
        _audio.Play(_terminalResult.Outcome == BattleOutcome.PlayerVictory ? "victory" : "defeat");
        _pause.Disabled = true;
        _speed.Disabled = true;
        _step.Disabled = true;
        _board.MouseFilter = MouseFilterEnum.Ignore;
        _inspector.CollapsePanel();
        RefreshCommandHud();
        SetProcessUnhandledInput(true);
        ClearFloatingCues();

        _endFadeOverlay.Color = new Color(0, 0, 0, 0);
        _endFadeOverlay.Visible = true;
        _endFadeOverlay.MouseFilter = _reset.Visible ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        SetProcess(false);
        _endTween?.Kill();
        _endTween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        _endTween.TweenInterval(EndHoldSeconds);
        _endTween.TweenProperty(_endFadeOverlay, "color:a", 1f, EndFadeSeconds)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        _endTween.TweenCallback(Callable.From(CompleteEndTransition));

        Finished?.Invoke(_terminalResult);
    }

    private void CompleteEndTransition()
    {
        if (!_ending || _endTransitionReported) return;
        _endTransitionReported = true;
        _endTween?.Kill();
        _endTween = null;
        _endFadeOverlay.Color = Colors.Black;
        SetProcessUnhandledInput(false);
        EndTransitionFinished?.Invoke();
    }

    private void ResetEndSequence()
    {
        _endTween?.Kill();
        _endTween = null;
        _ending = false;
        _endTransitionReported = false;
        _terminalResult = null;
        SetProcessUnhandledInput(false);
        if (_endFadeOverlay is not null)
        {
            _endFadeOverlay.Visible = false;
            _endFadeOverlay.Color = new Color(0, 0, 0, 0);
        }
    }

    private static float NormalizeSpeed(float speed) => speed >= 4f ? 4f : speed >= 2f ? 2f : 1f;

    private sealed class NullEvents : ISemanticBattleEventSink
    {
        public static readonly NullEvents Instance = new();
        public void Publish(SemanticBattleEvent battleEvent) { }
    }

    private sealed class NullCommands : IBattleCommandGateway
    {
        public static readonly NullCommands Instance = new();
        public bool Submit(BattleCommandRequest command) => false;
    }
}
