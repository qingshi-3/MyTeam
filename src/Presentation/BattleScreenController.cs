using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Equipment;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;
using TowerAutobattler.UI;

namespace TowerAutobattler.Presentation;

public sealed record BattleScreenRuntimeUnitSnapshot(
    string RuntimeId,
    string SourceInstanceId,
    string ContentId,
    string DisplayName,
    UnitRole Role,
    bool IsHero,
    int Team,
    Vector2I Cell,
    bool Alive,
    float Health,
    float MaxHealth,
    float Damage,
    float AttackSpeed,
    float Reach,
    float ControlResistance,
    ImmutableArray<BattleScreenEquipmentSnapshot> Equipment,
    ImmutableArray<TraitContributionSnapshot> TraitContributions,
    ImmutableArray<TraitPresentationSnapshot> TeamTraits,
    ImmutableArray<StatusRuntimeSnapshot> Statuses,
    BattleUnitMode Mode,
    BattleActionKind LastActionKind,
    string ActionTargetName,
    int AttackCooldown,
    int DisabledTicks);

public sealed record BattleScreenEquipmentSnapshot(
    string InstanceId,
    string ContentId,
    int SlotIndex);

public partial class BattleScreenController : Control
{
    public event Action<BattleResult>? Finished;
    public event Action? EndTransitionFinished;
    public event Action? ResetRequested;
    public event Action? ReturnToConfigurationRequested;

    private const float NormalSimulationScale = .8f;
    private const double EndHoldSeconds = 1.1;
    private const double EndFadeSeconds = .45;
    private const int MaxActiveFloatingCues = 64;

    [Export] public PackedScene? FloatingCueScene { get; set; }

    internal Func<CatalogEntry, UnitContentRoot> PresenterFactory { get; set; } =
        entry => entry.Scene.Instantiate<UnitContentRoot>();

    private BattleBoard _board = null!;
    private Node2D _unitsRoot = null!;
    private Control _floatingCueOverlay = null!;
    private Label _title = null!;
    private Label _rule = null!;
    private BattleStatusStrip _status = null!;
    private Button _pause = null!;
    private Button _speed = null!;
    private Button _step = null!;
    private Button _reset = null!;
    private Button _returnToConfiguration = null!;
    private TacticalCommandHud _tacticalCommandHud = null!;
    private SelectedUnitPanel _selectedUnit = null!;
    private ColorRect _endFadeOverlay = null!;
    private readonly Dictionary<string, UnitContentRoot> _presenters = new(StringComparer.Ordinal);
    private readonly List<BattleFloatingCue> _floatingCues = [];
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
    private long _floatingCueSequence;
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
    public int MaximumFloatingCueCount => MaxActiveFloatingCues;
    public int ActiveFloatingCueCount => _floatingCues.Count;
    public int ActiveFloatingTweenCount => _floatingCues.Count(cue => cue.HasActiveTween);

    public override void _Ready()
    {
        _board = GetNode<BattleBoard>("%BattleBoard");
        _unitsRoot = GetNode<Node2D>("%UnitsRoot");
        _floatingCueOverlay = GetNode<Control>("%FloatingCueOverlay");
        if (FloatingCueScene is null)
            throw new InvalidOperationException("BattleScreen requires an authored floating-cue scene.");
        _title = GetNode<Label>("%BattleTitle");
        _rule = GetNode<Label>("%RuleText");
        _status = GetNode<BattleStatusStrip>("%BattleStatus");
        _pause = GetNode<Button>("%PauseButton");
        _speed = GetNode<Button>("%SpeedButton");
        _step = GetNode<Button>("%StepButton");
        _reset = GetNode<Button>("%ResetBattleButton");
        _returnToConfiguration = GetNode<Button>("%ReturnConfigurationButton");
        _tacticalCommandHud = GetNode<TacticalCommandHud>("%TacticalCommandHud");
        _selectedUnit = GetNode<SelectedUnitPanel>("%SelectedUnitPanel");
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
            _selectedUnit.Visible = false;
            RefreshCommandHud();
            RefreshStatus();
            SyncPresenters("idle");
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
        if (_endFadeOverlay is not null)
            _endFadeOverlay.MouseFilter = visible ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
    }

    public void SetPaused(bool paused)
    {
        if (_ending || _simulation?.Outcome != BattleOutcome.Running) return;
        _paused = paused;
        _pause.Text = _paused ? "继续" : "暂停";
        _step.Disabled = !_step.Visible || !_paused;
        foreach (var presenter in _presenters.Values) presenter.SetPresentationPaused(_paused);
    }

    public void SetSpeed(float speed)
    {
        if (_ending) return;
        _speedScale = NormalizeSpeed(speed);
        _speed.Text = $"速度 x{_speedScale:0}";
        foreach (var presenter in _presenters.Values) presenter.SetPresentationSpeed(_speedScale);
    }

    public bool StepOneTick()
    {
        if (!_paused || _ending || _simulation is null || _simulation.Outcome != BattleOutcome.Running)
            return false;
        try
        {
            _simulation.Step();
            PresentEvents(_simulation.DrainEvents());
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
        foreach (var battleEvent in events)
        {
            if (battleEvent.Type != "move" || string.IsNullOrWhiteSpace(battleEvent.SourceRuntimeId)) continue;
            EnsurePresenter(battleEvent.SourceRuntimeId);
            if (_presenters.TryGetValue(battleEvent.SourceRuntimeId, out var mover))
                mover.QueueMovement(_board.CellToLocal(battleEvent.Cell));
        }

        foreach (var battleEvent in events.Where(battleEvent => battleEvent.Type is "attack" or "heal"))
        {
            EnsurePresenter(battleEvent.SourceRuntimeId);
            EnsurePresenter(battleEvent.TargetRuntimeId);
            if (_presenters.TryGetValue(battleEvent.SourceRuntimeId, out var source) &&
                _presenters.TryGetValue(battleEvent.TargetRuntimeId, out var target))
                source.FaceToward(target.Position);
        }

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
                    presenter.RefreshPresentation(selection.Value, state.Health, state.MaxHealth);
                    presented.Add(presentationId);
                }
            }
        }
        SyncPresenters("idle", presented);
        RefreshSelectedUnit();
    }

    private void PresentResolvedFacts()
    {
        if (_simulation is null || FloatingCueScene is null) return;
        var lanes = new Dictionary<string, int>(StringComparer.Ordinal);

        var combatStart = Math.Min(_combatCueCursor, _simulation.CombatEvents.Count);
        var combatFacts = _simulation.CombatEvents.Skip(combatStart)
            .Where(item => (item.Kind is BattleCombatEventKind.DamageResolved or
                            BattleCombatEventKind.HealingResolved) &&
                           item.EffectiveValue > 0 &&
                           !string.IsNullOrWhiteSpace(item.TargetRuntimeId))
            .GroupBy(item => (item.Tick, item.Kind, item.TargetRuntimeId))
            .Select(group => new
            {
                group.Key.Tick,
                group.Key.Kind,
                group.Key.TargetRuntimeId,
                Value = group.Sum(item => item.EffectiveValue),
                Cell = group.OrderBy(item => item.Sequence).Last().Cell,
                Sequence = group.Min(item => item.Sequence)
            })
            .OrderBy(item => item.Tick)
            .ThenBy(item => item.Sequence)
            .ToArray();
        _combatCueCursor = _simulation.CombatEvents.Count;
        foreach (var fact in combatFacts)
        {
            var kind = fact.Kind == BattleCombatEventKind.DamageResolved
                ? BattleFloatingCueKind.Damage
                : BattleFloatingCueKind.Healing;
            var sign = kind == BattleFloatingCueKind.Damage ? "-" : "+";
            SpawnFloatingCue(
                kind,
                sign + FormatCueValue(fact.Value),
                fact.TargetRuntimeId,
                fact.Tick,
                fact.Cell,
                lanes);
        }

        var statusStart = Math.Min(_statusCueCursor, _simulation.StatusPresentationCues.Count);
        var statusCues = _simulation.StatusPresentationCues.Skip(statusStart).ToArray();
        _statusCueCursor = _simulation.StatusPresentationCues.Count;
        var onActive = statusCues
            .Where(cue => cue.Lifecycle == StatusPresentationCueLifecycle.OnActive)
            .Select(cue => (cue.Tick, cue.Status.OwnerId, cue.Status.StableId))
            .ToHashSet();
        var presented = new HashSet<(int Tick, string OwnerId, string StableId, BattleFloatingCueKind Kind)>();
        foreach (var cue in statusCues)
        {
            BattleFloatingCueKind? kind = cue.Lifecycle switch
            {
                StatusPresentationCueLifecycle.OnActive => BattleFloatingCueKind.StatusActive,
                StatusPresentationCueLifecycle.Executed when cue.Status.Stacks > 1 =>
                    BattleFloatingCueKind.StatusStack,
                StatusPresentationCueLifecycle.Executed when !onActive.Contains(
                    (cue.Tick, cue.Status.OwnerId, cue.Status.StableId)) => BattleFloatingCueKind.StatusActive,
                StatusPresentationCueLifecycle.Removed => BattleFloatingCueKind.StatusRemoved,
                _ => null
            };
            if (kind is null || !presented.Add(
                    (cue.Tick, cue.Status.OwnerId, cue.Status.StableId, kind.Value)))
                continue;
            var label = !string.IsNullOrWhiteSpace(cue.Status.ReportLabel)
                ? cue.Status.ReportLabel
                : !string.IsNullOrWhiteSpace(cue.Status.DisplayName)
                    ? cue.Status.DisplayName
                    : "状态";
            var text = kind.Value switch
            {
                BattleFloatingCueKind.StatusActive => label + " 生效",
                BattleFloatingCueKind.StatusStack => $"{label} ×{cue.Status.Stacks}",
                _ => label + " 消退"
            };
            SpawnFloatingCue(
                kind.Value,
                text,
                cue.Status.OwnerId,
                cue.Tick,
                default,
                lanes);
        }
    }

    private void SpawnFloatingCue(
        BattleFloatingCueKind kind,
        string text,
        string targetRuntimeId,
        int tick,
        CombatCell eventCell,
        IDictionary<string, int> lanes)
    {
        if (FloatingCueScene is null || _floatingCueOverlay is null) return;
        while (_floatingCues.Count >= MaxActiveFloatingCues)
            ReleaseFloatingCue(_floatingCues[0], queueFree: false);

        lanes.TryGetValue(targetRuntimeId, out var sequence);
        lanes[targetRuntimeId] = sequence + 1;
        var lane = sequence % 4;
        var column = sequence / 4 % 3 - 1;
        var state = FindState(targetRuntimeId);
        var cell = state?.Cell ?? new Vector2I(eventCell.X, eventCell.Y);
        var cue = FloatingCueScene.Instantiate<BattleFloatingCue>();
        cue.Name = $"FloatingCue{++_floatingCueSequence}";
        cue.Finished += OnFloatingCueFinished;
        _floatingCueOverlay.AddChild(cue);
        _floatingCues.Add(cue);
        cue.Play(kind, text, targetRuntimeId, tick, _board.CellToLocal(cell), lane, column);
    }

    private void OnFloatingCueFinished(BattleFloatingCue cue) =>
        ReleaseFloatingCue(cue, queueFree: true);

    private void ClearFloatingCues()
    {
        foreach (var cue in _floatingCues.ToArray())
            ReleaseFloatingCue(cue, queueFree: false);
        _floatingCues.Clear();
    }

    private void ReleaseFloatingCue(BattleFloatingCue cue, bool queueFree)
    {
        if (!_floatingCues.Remove(cue)) return;
        cue.Finished -= OnFloatingCueFinished;
        cue.Stop();
        cue.GetParent()?.RemoveChild(cue);
        if (queueFree) cue.QueueFree();
        else cue.Free();
    }

    private static string FormatCueValue(float value) => value >= 10 || Math.Abs(value - MathF.Round(value)) < .01f
        ? MathF.Round(value).ToString("0")
        : value.ToString("0.#");

    private void SyncPresenters(string cue, IReadOnlySet<string>? preserveCue = null)
    {
        if (_simulation is null) return;
        foreach (var state in _simulation.Units)
        {
            EnsurePresenter(state.RuntimeId);
            if (_presenters.TryGetValue(state.RuntimeId, out var presenter))
                presenter.RefreshPresentation(state.Alive && preserveCue?.Contains(state.RuntimeId) == true ? "" : state.Alive ? cue : "defeated",
                    state.Health, state.MaxHealth);
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
            subscribed = true;
            presenter.Activate(new UnitBindingContext(
                new DeterministicRandom(1), NullEvents.Instance, NullCommands.Instance));
            presenter.SetPresentationSpeed(_speedScale);
            presenter.SetPresentationPaused(_paused);
            presenter.Scale = Vector2.One * _board.CurrentProjection.UnitScale;
            presenter.SnapPresentation(_board.CellToLocal(state.Cell), state.Health, state.MaxHealth);
            _presenters.Add(runtimeId, presenter);
        }
        catch
        {
            if (presenter is not null)
            {
                if (subscribed) presenter.ActorSelected -= OnActorSelected;
                try { presenter.Deactivate(); }
                catch { }
                presenter.GetParent()?.RemoveChild(presenter);
                presenter.Free();
            }
            throw;
        }
    }

    private BattleUnitState? FindState(string runtimeId) => _simulation?.Units.FirstOrDefault(unit => unit.RuntimeId == runtimeId);

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
    }

    private void OnActorSelected(string runtimeId)
    {
        if (_ending) return;
        _selectedRuntimeId = runtimeId;
        RefreshSelectedUnit();
    }

    private void OnBoardGuiInput(InputEvent @event)
    {
        if (_ending || @event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse || _simulation is null) return;
        var presenter = _presenters.Values
            .Where(candidate => candidate.Visible && FindState(candidate.RuntimeId)?.Alive == true)
            .OrderBy(candidate => candidate.Position.DistanceSquaredTo(mouse.Position))
            .FirstOrDefault(candidate => candidate.Position.DistanceTo(mouse.Position) <= _board.CurrentProjection.SelectionRadius);
        if (presenter is null) return;
        presenter.RequestSelection();
        _board.AcceptEvent();
    }

    private void RefreshSelectedUnit()
    {
        if (string.IsNullOrWhiteSpace(_selectedRuntimeId)) return;
        var state = FindState(_selectedRuntimeId);
        if (state is not null) _selectedUnit.Bind(BuildRuntimeSnapshot(state));
    }

    private BattleScreenRuntimeUnitSnapshot BuildRuntimeSnapshot(BattleUnitState unit)
    {
        var equipment = _config?.Equipment.Instances
            .Where(item => item.OwnerHeroInstanceId == unit.SourceInstanceId)
            .OrderBy(item => item.SlotIndex)
            .Select(item => new BattleScreenEquipmentSnapshot(item.InstanceId, item.ContentId, item.SlotIndex))
            .ToImmutableArray() ?? [];
        var traitSnapshot = _simulation?.TraitSnapshot;
        var contributions = traitSnapshot?.Contributions
            .Where(item => item.OwnerRuntimeId == unit.SourceInstanceId ||
                           item.OwnerRuntimeId == unit.RuntimeId)
            .OrderBy(item => item.TraitId, StringComparer.Ordinal)
            .ThenBy(item => item.SourceInstanceId, StringComparer.Ordinal)
            .ToImmutableArray() ?? [];
        var teamTraits = traitSnapshot?.Values
            .Where(value => value.Team == unit.Team)
            .OrderBy(value => value.TraitId, StringComparer.Ordinal)
            .Select(value => value.Presentation)
            .ToImmutableArray() ?? [];
        return new BattleScreenRuntimeUnitSnapshot(
            unit.RuntimeId,
            unit.SourceInstanceId,
            unit.Definition.ContentId,
            unit.Definition.DisplayName,
            unit.Definition.Role,
            unit.Definition.IsHero,
            unit.Team,
            unit.Cell,
            unit.Alive,
            unit.Health,
            unit.MaxHealth,
            unit.Damage,
            unit.Attributes.GetValue(CombatAttribute.AttackSpeed),
            unit.Attributes.GetValue(CombatAttribute.AttackRange),
            unit.Attributes.GetValue(CombatAttribute.ControlResistance),
            equipment,
            contributions,
            teamTraits,
            unit.Statuses,
            unit.Mode,
            unit.LastActionKind,
            unit.ActionTargetName,
            unit.AttackCooldown,
            unit.DisabledTicks);
    }

    private void OnBoardProjectionChanged(BattlefieldProjection previous, BattlefieldProjection next)
    {
        foreach (var presenter in _presenters.Values)
        {
            presenter.RemapPresentationCoordinates(previous.Origin, previous.CellPitch, next.Origin, next.CellPitch);
            presenter.Scale = Vector2.One * next.UnitScale;
        }
    }

    private void ClearPresenters(bool replacement = false)
    {
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
            presenter.Deactivate();
            if (presenter.GetParent() is not null) presenter.GetParent().RemoveChild(presenter);
            presenter.Free();
        }
        _presenters.Clear();
        _selectedRuntimeId = string.Empty;
        if (_selectedUnit is not null) _selectedUnit.Visible = false;
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
        _reported = true;
        _accumulator = 0;
        _terminalResult = _simulation.CreateResult();
        _pause.Disabled = true;
        _speed.Disabled = true;
        _step.Disabled = true;
        _board.MouseFilter = MouseFilterEnum.Ignore;
        _selectedUnit.Visible = false;
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
