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

    internal Func<CatalogEntry, UnitContentRoot> PresenterFactory { get; set; } =
        entry => entry.Scene.Instantiate<UnitContentRoot>();

    private BattleBoard _board = null!;
    private BattleEnvironment _environment = null!;
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
    private readonly BattlePlaybackSession _playback = new();
    private BattlePresentationSession _presentation = null!;
    private BattleEndTransition _endTransition = null!;
    private ContentRegistry? _content;
    private BattleConfig? _config;
    private BattleSimulation? _simulation => _playback.Simulation;
    private float _speedScale => _playback.Speed;
    private bool _paused => _playback.Paused;
    private bool _ending => _playback.Ending;
    private BattleResult? _terminalResult => _playback.Result;
    private string _commandFeedback = string.Empty;
    private bool _commandFeedbackError;
    private string _selectedRuntimeId = string.Empty;
    public string LastRuntimeFailure { get; private set; } = string.Empty;
    public float SpeedScale => _speedScale;
    public int TacticalPoints => _simulation?.TacticalPoints ?? 0;
    public int MaximumTacticalPoints => _simulation?.MaximumTacticalPoints ?? 0;
    public int RemainingGold => _simulation?.RemainingGold ?? 0;
    public int TemporaryUnitCount => _simulation?.UnitViews.Count(unit => unit.IsTemporary) ?? 0;
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

    public override void _Notification(int what)
    {
        if (what == NotificationResized && _environment is not null)
            RefreshEnvironmentLighting();
    }

    public override void _Ready()
    {
        _board = GetNode<BattleBoard>("%BattleBoard");
        _environment = GetNode<BattleEnvironment>("BattleEnvironment");
        _unitsRoot = GetNode<Node2D>("%UnitsRoot");
        _geometry = GetNode<BattleGeometryOverlay>("%BattleGeometryOverlay");
        _collisionToggle = GetNode<Button>("%CollisionToggle");
        _attackRangeToggle = GetNode<Button>("%AttackRangeToggle");
        _geometryReadout = GetNode<Label>("%GeometryReadout");
        _collisionToggle.Toggled += OnGeometryToggled;
        _attackRangeToggle.Toggled += OnGeometryToggled;
        _rangedAttackLayer = GetNode<RangedAttackLayer>("%RangedAttackLayer");
        _rangedAttackLayer.UnitHitPresented += OnUnitHitPresented;
        _audio = GetNode<FeedbackAudio>("BattleAudio");
        _rangedAttackLayer.Bind(_board);
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
        _presentation = new BattlePresentationSession(_playback, _board, _environment, _unitsRoot,
            _rangedAttackLayer, _audio, _floatingCueLayer, entry => PresenterFactory(entry), OnActorSelected);
        _presentation.BindRangedPositions();
        _endTransition = new BattleEndTransition(this, _endFadeOverlay);
        _endTransition.Completed += OnEndTransitionCompleted;
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
        _rangedAttackLayer.UnitHitPresented -= OnUnitHitPresented;
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
        _endTransition.Completed -= OnEndTransitionCompleted;
        ClearPresenters();
    }

    public void StartBattle(ContentRegistry content, BattleConfig config, string title, float defaultSpeed = 1f)
    {
        BindRunHealth(0, 0);
        ResetEndSequence();
        ClearPresenters(replacement: true);
        try
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            LastRuntimeFailure = string.Empty;
            _playback.Start(config, defaultSpeed);
            _presentation.Bind(content);
            _title.Text = title;
            _rule.Text = $"{config.FloorRule.DisplayName}：{config.FloorRule.PreviewText}";
            _board.Bind(config.FloorRule);
            _environment.ResetClock();
            _rangedAttackLayer.SetClock(false, _speedScale * NormalSimulationScale);
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
            RefreshEnvironmentLighting();
            _rangedAttackLayer.SynchronizeUnits(_simulation!.UnitViews);
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

    public void BindRunHealth(int current, int maximum) => GetNode<RunHealthDisplay>("%RunHealth").Bind(current, maximum);

    public void SetLabControlsVisible(bool visible)
    {
        _collisionToggle.Visible = visible;
        _attackRangeToggle.Visible = visible;
        if (!visible)
        {
            _collisionToggle.SetPressedNoSignal(false);
            _attackRangeToggle.SetPressedNoSignal(false);
            RefreshGeometry();
        }
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
        _playback.SetPaused(paused);
        _pause.Text = _paused ? "继续" : "暂停";
        _step.Disabled = !_step.Visible || !_paused;
        // Resuming an animation may emit its pending first-frame marker immediately.
        _audio.SetPaused(_paused);
        foreach (var presenter in _presentation.Actors) presenter.SetPresentationPaused(_paused);
        _rangedAttackLayer.SetClock(_paused, _speedScale * NormalSimulationScale);
        RefreshGeometry();
    }

    public void SetSpeed(float speed)
    {
        if (_ending) return;
        _playback.SetSpeed(speed);
        _speed.Text = $"速度 x{_speedScale:0}";
        foreach (var presenter in _presentation.Actors) presenter.SetPresentationSpeed(_speedScale, _speedScale * NormalSimulationScale);
        _rangedAttackLayer.SetClock(_paused, _speedScale * NormalSimulationScale);
    }

    public bool StepOneTick()
    {
        if (!_paused || _ending || _simulation is null || _simulation.Outcome != BattleOutcome.Running)
            return false;
        try
        {
            foreach (var presenter in _presentation.Actors) presenter.StepAttackPresentation(BattleTiming.TickSeconds);
            _simulation.Step();
            PresentEvents(_simulation.DrainEvents());
            SnapPresentersToAuthority();
            _environment.Advance(BattleTiming.TickSeconds);
            RefreshEnvironmentLighting();
            PresentResolvedFacts();
            RefreshStatus();
            RefreshSelectedUnit();
            if (_simulation.Outcome != BattleOutcome.Running && !_ending) BeginEndSequence();
            return true;
        }
        catch (Exception exception)
        {
            CleanupRuntimeFailure(exception);
            throw;
        }
    }

    public ImmutableArray<BattleScreenRuntimeUnitSnapshot> ReadRuntimeUnits() => _simulation?.UnitViews
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
            _environment.Advance((float)delta * _speedScale * NormalSimulationScale);
            RefreshEnvironmentLighting();
            _playback.Advance(delta, () =>
            {
                PresentEvents(_simulation.DrainEvents());
                PresentResolvedFacts();
            });
            RefreshStatus();
            if (_simulation.Outcome != BattleOutcome.Running && !_ending)
                BeginEndSequence();
        }
        catch (Exception exception)
        {
            CleanupRuntimeFailure(exception);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_ending || _endTransition.Reported || !@event.IsActionPressed("ui_accept")) return;
        GetViewport().SetInputAsHandled();
        CompleteEndTransition();
    }

    private void OnUnitHitPresented(string id) => _presentation.OnUnitHitPresented(id);
    private void PresentEvents(IReadOnlyList<BattleEvent> events)
    {
        _presentation.PresentEvents(events);
        RefreshSelectedUnit();
    }
    private void PresentResolvedFacts()
    {
        _presentation.PresentResolvedFacts();
        RefreshGeometry();
    }
    private void SyncPresenters(string cue) => _presentation.SyncPresenters(cue);
    private BattleUnitView? FindState(string id) => _presentation.FindState(id);
    private void ClearFloatingCues() => _floatingCueLayer?.Clear();

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
        // Automatic Growth battles and Lab presets without commands expose no command HUD.
        _tacticalCommandHud.Visible = !_simulation.TacticalCommands.Slots.IsDefaultOrEmpty;
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
            _simulation.UnitViews.Count(unit => unit.Team == 0 && unit.Alive),
            _simulation.UnitViews.Count(unit => unit.Team == 1 && unit.Alive),
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
        var presenter = _presentation.Actors
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

    private BattleScreenRuntimeUnitSnapshot BuildRuntimeSnapshot(BattleUnitView unit) =>
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
        // Geometry remains an authority snapshot while actors keep their ordinary presentation
        // interpolation. A small visual offset between a circle and its actor is therefore expected.
        // Paused single-step owns the explicit authority snap before reaching this refresh.
        _geometry.Bind(snapshot, _board.CurrentProjection, _selectedRuntimeId,
            _collisionToggle.ButtonPressed, _attackRangeToggle.ButtonPressed);
    }

    private void SnapPresentersToAuthority() => _presentation.SnapPresentersToAuthority();

    private void OnBoardProjectionChanged(BattlefieldProjection previous, BattlefieldProjection next)
    {
        foreach (var presenter in _presentation.Actors)
        {
            presenter.RemapPresentationCoordinates(previous.Origin, previous.CellPitch, next.Origin, next.CellPitch);
            presenter.Scale = Vector2.One * next.UnitScale;
        }
        if (_geometry is not null) RefreshGeometry();
        if (_environment is not null) RefreshEnvironmentLighting();
    }

    private void RefreshEnvironmentLighting() => _presentation?.RefreshEnvironmentLighting();

    private void ClearPresenters(bool replacement = false)
    {
        _geometry?.Clear();
        if (_geometryReadout is not null) _geometryReadout.Visible = false;
        _playback.Stop(replacement);
        _presentation?.Clear();
        _selectedRuntimeId = string.Empty;
        if (_inspector is not null) _inspector.ResetSelection();
        _config = null;
        _content = null;
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
        if (!_playback.BeginTerminal()) return;
        // Preserve final ground positions after scope cleanup removed any airborne motion.
        _presentation.HoldTerminalPositions();
        _rangedAttackLayer.Clear();
        _rangedAttackLayer.SetClock(false, _playback.SimulationScale);
        _audio.Clear();
        _audio.Play(_terminalResult!.Outcome == BattleOutcome.PlayerVictory ? "victory" : "defeat");
        _pause.Disabled = true;
        _speed.Disabled = true;
        _step.Disabled = true;
        _board.MouseFilter = MouseFilterEnum.Ignore;
        _inspector.CollapsePanel();
        RefreshCommandHud();
        SetProcessUnhandledInput(true);
        ClearFloatingCues();

        SetProcess(false);
        _endTransition.Begin(_reset.Visible);

        Finished?.Invoke(_terminalResult!);
    }

    private void CompleteEndTransition() => _endTransition.Complete();

    private void OnEndTransitionCompleted()
    {
        SetProcessUnhandledInput(false);
        EndTransitionFinished?.Invoke();
    }

    private void ResetEndSequence()
    {
        _endTransition?.Reset();
        _playback.ResetTerminal();
        SetProcessUnhandledInput(false);
    }
}
