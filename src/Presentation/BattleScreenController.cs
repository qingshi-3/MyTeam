using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.UI;

namespace TowerAutobattler.Presentation;

public partial class BattleScreenController : Control
{
    public event Action<BattleResult>? Finished;
    public event Action? EndTransitionFinished;

    private const float NormalSimulationScale = .8f;
    private const double EndHoldSeconds = 1.1;
    private const double EndFadeSeconds = .45;

    private BattleBoard _board = null!;
    private Node2D _unitsRoot = null!;
    private Label _title = null!;
    private Label _rule = null!;
    private Label _status = null!;
    private Button _pause = null!;
    private Button _speed = null!;
    private HeroCommandHud _commandHud = null!;
    private SelectedUnitPanel _selectedUnit = null!;
    private ColorRect _endFadeOverlay = null!;
    private readonly Dictionary<string, UnitContentRoot> _presenters = new(StringComparer.Ordinal);
    private ContentRegistry? _content;
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
    private string _selectedRuntimeId = string.Empty;
    public float SpeedScale => _speedScale;
    public int CommandCharges => _simulation?.CommandCharges ?? 0;
    public int CurrentMana => _simulation?.CurrentMana ?? 0;
    public int MaxMana => _simulation?.MaxMana ?? 0;
    public int RemainingGold => _simulation?.RemainingGold ?? 0;
    public int TemporaryUnitCount => _simulation?.Units.Count(unit => unit.IsTemporary) ?? 0;
    public string CommandFeedback => _commandFeedback;
    public bool IsEnding => _ending;
    public BattleResult? TerminalResult => _terminalResult;

    public override void _Ready()
    {
        _board = GetNode<BattleBoard>("%BattleBoard");
        _unitsRoot = GetNode<Node2D>("%UnitsRoot");
        _title = GetNode<Label>("%BattleTitle");
        _rule = GetNode<Label>("%RuleText");
        _status = GetNode<Label>("%BattleStatus");
        _pause = GetNode<Button>("%PauseButton");
        _speed = GetNode<Button>("%SpeedButton");
        _commandHud = GetNode<HeroCommandHud>("%HeroCommandHud");
        _selectedUnit = GetNode<SelectedUnitPanel>("Margin/Layout/BattleBoard/SelectedUnitPanel");
        _endFadeOverlay = GetNode<ColorRect>("%EndFadeOverlay");
        _pause.Pressed += TogglePause;
        _speed.Pressed += CycleSpeed;
        _commandHud.UseRequested += UseCommand;
        _board.GuiInput += OnBoardGuiInput;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        _pause.Pressed -= TogglePause;
        _speed.Pressed -= CycleSpeed;
        _commandHud.UseRequested -= UseCommand;
        _board.GuiInput -= OnBoardGuiInput;
        ResetEndSequence();
        ClearPresenters();
    }

    public void StartBattle(ContentRegistry content, BattleConfig config, string title, float defaultSpeed = 1f)
    {
        ResetEndSequence();
        ClearPresenters();
        _content = content;
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
        _selectedRuntimeId = string.Empty;
        _selectedUnit.Visible = false;
        RefreshCommandHud();
        RefreshStatus();
        SyncPresenters("idle");
        SetProcess(true);
        SetProcessUnhandledInput(false);
    }

    public override void _Process(double delta)
    {
        if (_ending || _paused || _simulation is null || _simulation.Outcome != BattleOutcome.Running) return;
        _accumulator += delta * _speedScale * NormalSimulationScale;
        var steps = 0;
        while (_accumulator >= BattleSimulation.TickSeconds && steps++ < 12 && _simulation.Outcome == BattleOutcome.Running)
        {
            _accumulator -= BattleSimulation.TickSeconds;
            _simulation.Step();
            PresentEvents(_simulation.DrainEvents());
        }
        RefreshStatus();
        if (_simulation.Outcome != BattleOutcome.Running && !_reported)
            BeginEndSequence();
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
        var presenter = entry.Scene.Instantiate<UnitContentRoot>();
        _unitsRoot.AddChild(presenter);
        presenter.Bind(state.RuntimeId, state.Team, state.Health, state.MaxHealth);
        presenter.ActorSelected += OnActorSelected;
        presenter.Activate(new UnitBindingContext(new DeterministicRandom(1), NullEvents.Instance, NullCommands.Instance));
        presenter.SetPresentationSpeed(_speedScale);
        presenter.SetPresentationPaused(_paused);
        presenter.SnapPresentation(_board.CellToLocal(state.Cell), state.Health, state.MaxHealth);
        _presenters.Add(runtimeId, presenter);
    }

    private BattleUnitState? FindState(string runtimeId) => _simulation?.Units.FirstOrDefault(unit => unit.RuntimeId == runtimeId);

    private void TogglePause()
    {
        if (_ending) return;
        _paused = !_paused;
        _pause.Text = _paused ? "继续" : "暂停";
        foreach (var presenter in _presenters.Values) presenter.SetPresentationPaused(_paused);
    }

    private void CycleSpeed()
    {
        if (_ending) return;
        _speedScale = _speedScale switch { 1f => 2f, 2f => 4f, _ => 1f };
        _speed.Text = $"速度 x{_speedScale:0}";
        foreach (var presenter in _presenters.Values) presenter.SetPresentationSpeed(_speedScale);
    }

    private void UseCommand()
    {
        if (_ending || _simulation is null) return;
        var result = _simulation.TryUseHeroCommand();
        if (result.Succeeded)
        {
            PresentEvents(_simulation.DrainEvents());
            _commandFeedback = _simulation.CommandGoldCost > 0 ? "雇佣成功。" : "指令已发动。";
        }
        else
            _commandFeedback = result.FailureReason;
        RefreshCommandHud();
        RefreshStatus();
    }

    private void RefreshCommandHud()
    {
        if (_simulation is null) return;
        _commandHud.Bind(_simulation.CommandName, _simulation.CommandDescription,
            _simulation.CurrentMana, _simulation.MaxMana, _simulation.CommandManaCost,
            _simulation.CommandGoldCost, _commandFeedback, _simulation.Outcome == BattleOutcome.Running);
    }

    private void RefreshStatus()
    {
        if (_simulation is null) return;
        var economy = _simulation.CommandGoldCost > 0
            ? $"　{_simulation.CommandName} {_simulation.CommandGoldCost} 金币/次　剩余金币 {_simulation.RemainingGold}"
            : string.Empty;
        var feedback = string.IsNullOrWhiteSpace(_commandFeedback) ? string.Empty : $"　{_commandFeedback}";
        _status.Text = $"战斗时间 {_simulation.TickIndex * BattleSimulation.TickSeconds:0.0}s　我方 {_simulation.Units.Count(unit => unit.Team == 0 && unit.Alive)}　敌方 {_simulation.Units.Count(unit => unit.Team == 1 && unit.Alive)}{economy}{feedback}";
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
            .FirstOrDefault(candidate => candidate.Position.DistanceTo(mouse.Position) <= 44f);
        if (presenter is null) return;
        presenter.RequestSelection();
        _board.AcceptEvent();
    }

    private void RefreshSelectedUnit()
    {
        if (string.IsNullOrWhiteSpace(_selectedRuntimeId)) return;
        var state = FindState(_selectedRuntimeId);
        if (state is not null) _selectedUnit.Bind(state);
    }

    private void ClearPresenters()
    {
        try { _simulation?.Dispose(); }
        catch (Exception exception) { GD.PushError($"Battle abort failed: {exception.Message}"); }
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
        _board.MouseFilter = MouseFilterEnum.Ignore;
        _selectedUnit.Visible = false;
        RefreshCommandHud();
        SetProcessUnhandledInput(true);

        _endFadeOverlay.Color = new Color(0, 0, 0, 0);
        _endFadeOverlay.Visible = true;
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
