using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

public partial class ExpeditionScreenController : Control, IUiMotionHost
{
    [Signal] public delegate void CellSelectedEventHandler(int cellIndex);
    [Signal] public delegate void MoveRequestedEventHandler(int cellIndex);
    [Signal] public delegate void InspectRequestedEventHandler(string siteId);
    [Signal] public delegate void StepRequestedEventHandler(Vector2I direction);
    [Signal] public delegate void InteractRequestedEventHandler();
    [Signal] public delegate void BossRequestedEventHandler();
    [Signal] public delegate void ArmyRequestedEventHandler();
    [Signal] public delegate void MenuRequestedEventHandler();

    [Export] public PackedScene CellScene { get; set; } = null!;

    private readonly Dictionary<int, ExpeditionMapCell> _cells = [];
    public Func<bool> ReduceUiMotion { get; set; } = () => false;

    private GridContainer _grid = null!;
    private Control _mapCanvas = null!;
    private Control _partyMarker = null!;
    private ExpeditionPartyLight _partyLight = null!;
    private Control _detailsPanel = null!;
    private Tween? _detailsTween;
    private int _detailShownIndex = -1;
    private bool _presentationInputBlocked;
    private bool _centerRequested = true;
    private bool _snapCamera = true;
    private bool _followParty;
    private bool _partyPositioned;
    private bool _animateParty;
    private readonly Queue<int> _committedSteps = new();
    private int _visualStepIndex = -1;
    private Vector2 _partyFrom;
    private double _partyMoveElapsed;
    private Vector2 _savedScroll;
    private bool _restoreScroll;

    // Only root flow uses this for a new journey / Continue. Returning from a
    // service keeps the current viewport, selection and authoritative position.
    public void PrepareForJourneyEntry()
    {
        _centerRequested = _snapCamera = true;
        _restoreScroll = false;
        _centerLayoutFrames = 1;
        if (_ready) SetProcess(IsVisibleInTree());
    }

    public void SetPresentationInputBlocked(bool blocked) => _presentationInputBlocked = blocked;

    public Vector2 PartyScreenCenter => _ready && _partyPositioned
        ? _partyMarker.GetGlobalRect().GetCenter() : GetGlobalRect().GetCenter();

    public Vector2 SelectedPlaceScreenCenter => _ready && _cells.TryGetValue(_selectedIndex, out var cell)
        ? cell.GetGlobalRect().GetCenter() : PartyScreenCenter;

    public void PulseSelectedPlace()
    {
        if (_ready && _cells.TryGetValue(_selectedIndex, out var cell)) cell.PulseSiteCue();
    }
    private ScrollContainer _mapScroll = null!;
    private Label _title = null!;
    private Label _stage = null!;
    private Label _actions = null!;
    private ProgressBar _actionBar = null!;
    private Label _hint = null!;
    private Label _bossName = null!;
    private Label _bossSummary = null!;
    private Button _boss = null!;
    private Button _army = null!;
    private Button _menu = null!;
    private Button _move = null!;
    private Button _inspect = null!;
    private Button _zoomIn = null!;
    private Button _zoomOut = null!;
    private Button _center = null!;
    private Label _zoom = null!;
    private Label _coordinate = null!;
    private TextureRect _detailIcon = null!;
    private Label _detailTitle = null!;
    private Label _detailSubtitle = null!;
    private Label _description = null!;
    private Label _cost = null!;
    private Label _reward = null!;
    private Label _risk = null!;
    private Label _availability = null!;
    private Label _status = null!;
    private ExpeditionScreenModel? _model;
    private int _selectedIndex = -1;
    private int _lastPlayerIndex = -1;
    private int _cellSize = 56;
    private string _mapId = string.Empty;
    private bool _ready;
    private int _centerLayoutFrames;
    private bool _globalInspectionOpen;
    private ulong _lastKeyboardStepAt;

    public void SetGlobalInspectionOpen(bool open) => _globalInspectionOpen = open;

    public override void _Input(InputEvent input)
    {
        if (!_ready || !IsVisibleInTree() || _globalInspectionOpen || _presentationInputBlocked ||
            input is not InputEventKey key || key.AltPressed || key.CtrlPressed || key.MetaPressed ||
            GetViewport().GuiIsDragging()) return;
        var focus = GetViewport().GuiGetFocusOwner();
        if (focus is LineEdit or TextEdit) return;
        for (Node? node = focus; node is not null; node = node.GetParent())
            if (node is ContextPopup { IsOpen: true }) return;

        var direction = input.IsAction("expedition_move_up") ? Vector2I.Up
            : input.IsAction("expedition_move_down") ? Vector2I.Down
            : input.IsAction("expedition_move_left") ? Vector2I.Left
            : input.IsAction("expedition_move_right") ? Vector2I.Right : Vector2I.Zero;
        var interact = input.IsAction("expedition_interact");
        if (direction == Vector2I.Zero && !interact) return;
        // Claim the map keys before Button focus navigation. Invalid moves and
        // repeated Enter must not fall through to a focused Boss or remote site.
        GetViewport().SetInputAsHandled();
        if (!key.Pressed || _model is not { InteractionLocked: false }) return;
        if (interact)
        {
            if (!key.Echo) EmitSignal(SignalName.InteractRequested);
            return;
        }
        var now = Time.GetTicksMsec();
        if (key.Echo && now - _lastKeyboardStepAt < 120) return;
        _lastKeyboardStepAt = now;
        EmitSignal(SignalName.StepRequested, direction);
    }

    public override void _Ready()
    {
        _grid = GetNode<GridContainer>("%MapGrid");
        _mapScroll = GetNode<ScrollContainer>("%MapScroll");
        _mapCanvas = GetNode<Control>("%MapCanvas");
        _partyMarker = GetNode<Control>("%PartyMarker");
        _partyLight = GetNode<ExpeditionPartyLight>("%PartyLight");
        _partyLight.ReduceUiMotion = () => ReduceUiMotion();
        _detailsPanel = GetNode<Control>("%DetailsPanel");
        _detailsPanel.OffsetTransformEnabled = true;
        _detailsPanel.OffsetTransformVisualOnly = false;
        _detailsPanel.OffsetTransformPivotRatio = new Vector2(0, .5f);
        _title = GetNode<Label>("%ExpeditionTitle");
        _stage = GetNode<Label>("%StageText");
        _actions = GetNode<Label>("%ActionText");
        _actionBar = GetNode<ProgressBar>("%ActionBudget");
        _hint = GetNode<Label>("%ExplorationHint");
        _bossName = GetNode<Label>("%BossName");
        _bossSummary = GetNode<Label>("%BossSummary");
        _boss = GetNode<Button>("%BossButton");
        _army = GetNode<Button>("%ArmyButton");
        _menu = GetNode<Button>("%MenuButton");
        _move = GetNode<Button>("%MoveButton");
        _inspect = GetNode<Button>("%InspectButton");
        _zoomIn = GetNode<Button>("%ZoomIn");
        _zoomOut = GetNode<Button>("%ZoomOut");
        _center = GetNode<Button>("%CenterParty");
        _zoom = GetNode<Label>("%ZoomLabel");
        _coordinate = GetNode<Label>("%Coordinate");
        _detailIcon = GetNode<TextureRect>("%DetailIcon");
        _detailTitle = GetNode<Label>("%DetailTitle");
        _detailSubtitle = GetNode<Label>("%DetailSubtitle");
        _description = GetNode<Label>("%DetailDescription");
        _cost = GetNode<Label>("%DetailCost");
        _reward = GetNode<Label>("%DetailReward");
        _risk = GetNode<Label>("%DetailRisk");
        _availability = GetNode<Label>("%DetailAvailability");
        _status = GetNode<Label>("%StatusMessage");
        _move.Pressed += OnMove;
        _inspect.Pressed += OnInspect;
        _boss.Pressed += OnBoss;
        _army.Pressed += OnArmy;
        _menu.Pressed += OnMenu;
        _zoomIn.Pressed += OnZoomIn;
        _zoomOut.Pressed += OnZoomOut;
        _center.Pressed += CenterOnParty;
        VisibilityChanged += OnMapVisibilityChanged;
        _mapScroll.Resized += OnMapLayoutChanged;
        _grid.Resized += OnMapLayoutChanged;
        SetProcess(false);
        _ready = true;
        if (_model is not null) Refresh();
    }

    public override void _ExitTree()
    {
        if (!_ready) return;
        _move.Pressed -= OnMove;
        _inspect.Pressed -= OnInspect;
        _boss.Pressed -= OnBoss;
        _army.Pressed -= OnArmy;
        _menu.Pressed -= OnMenu;
        _zoomIn.Pressed -= OnZoomIn;
        _zoomOut.Pressed -= OnZoomOut;
        _center.Pressed -= CenterOnParty;
        VisibilityChanged -= OnMapVisibilityChanged;
        _mapScroll.Resized -= OnMapLayoutChanged;
        _grid.Resized -= OnMapLayoutChanged;
        ResetDetailMotion();
        _partyLight.Clear();
        SetProcess(false);
        foreach (var cell in _cells.Values)
            if (GodotObject.IsInstanceValid(cell)) cell.Selected -= OnCellSelected;
        _ready = false;
    }

    public void Bind(ExpeditionScreenModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        if (_ready) Refresh();
    }

    public void ShowMessage(string message)
    {
        if (!_ready) return;
        _status.Text = message;
        _status.Visible = !string.IsNullOrWhiteSpace(message);
    }

    private void Refresh()
    {
        var model = _model!;
        if (model.Columns < 1 || model.Rows < 1) throw new ArgumentException("Expedition dimensions must be positive.");
        var mapChanged = _mapId != model.MapId;
        if (mapChanged)
        {
            _partyLight.Clear();
            foreach (var oldCell in _cells.Values)
            {
                oldCell.SettleReveal();
                oldCell.Selected -= OnCellSelected;
                _grid.RemoveChild(oldCell);
                oldCell.QueueFree();
            }
            _cells.Clear();
            _detailShownIndex = -1;
            _mapId = model.MapId;
            _selectedIndex = model.PlayerCellIndex;
            _lastPlayerIndex = -1;
            _partyPositioned = false;
            _committedSteps.Clear();
            _visualStepIndex = -1;
            PrepareForJourneyEntry();
        }
        if (model.SelectedCellIndex >= 0) _selectedIndex = model.SelectedCellIndex;
        if (_selectedIndex < 0) _selectedIndex = model.PlayerCellIndex;
        _title.Text = model.Title;
        _stage.Text = model.StageText;
        _actions.Text = $"行动  {model.RemainingActions} / {model.ActionBudget}";
        _actionBar.MaxValue = Math.Max(1, model.ActionBudget);
        _actionBar.Value = model.RemainingActions;
        _hint.Text = model.ExplorationHint;
        _bossName.Text = model.BossName;
        _bossSummary.Text = model.BossSummary;
        _boss.Text = model.BossActionText;
        _boss.Disabled = model.InteractionLocked || !model.CanChallengeBoss;
        _boss.TooltipText = model.BossDisabledReason;
        _army.Disabled = !model.CanOpenArmy;
        _menu.Disabled = !model.CanOpenMenu;
        ShowMessage(model.StatusMessage);
        SyncCells(model);
        RefreshDetails();
        if (_lastPlayerIndex != model.PlayerCellIndex)
        {
            var previous = _lastPlayerIndex;
            var adjacent = previous >= 0 &&
                Math.Abs(previous % model.Columns - model.PlayerCellIndex % model.Columns) +
                Math.Abs(previous / model.Columns - model.PlayerCellIndex / model.Columns) == 1;
            // Bind contains only saved positions. Never animate an optimistic
            // move or invent a path between distant authoritative snapshots.
            if (adjacent && _partyPositioned && IsVisibleInTree() && !ReduceUiMotion())
            {
                if (_animateParty) _committedSteps.Enqueue(model.PlayerCellIndex);
                else
                {
                    _visualStepIndex = model.PlayerCellIndex;
                    _partyFrom = _partyMarker.Position;
                    _partyMoveElapsed = 0;
                    _animateParty = true;
                }
            }
            else
            {
                _animateParty = false;
                _committedSteps.Clear();
                _visualStepIndex = model.PlayerCellIndex;
            }
            _followParty = !mapChanged && IsVisibleInTree();
            _lastPlayerIndex = model.PlayerCellIndex;
            _centerLayoutFrames = Math.Max(_centerLayoutFrames, mapChanged ? 1 : 0);
        }
        SetProcess(IsVisibleInTree());
    }

    private void SyncCells(ExpeditionScreenModel model)
    {
        // One wave radius for this committed reveal batch keeps adjacent cells'
        // front speed continuous. It is derived only from supplied visible facts.
        var revealReach = model.Cells.Where(c => c.IsRevealed &&
                _cells.TryGetValue(c.Index, out var old) && !old.IsRevealed)
            .Select(c => new Vector2(Math.Abs(model.PlayerCellIndex % model.Columns - c.Index % model.Columns) + .5f,
                Math.Abs(model.PlayerCellIndex / model.Columns - c.Index / model.Columns) + .5f).Length() + .25f)
            .DefaultIfEmpty(2.4f).Max();
        var expected = checked(model.Columns * model.Rows);
        var data = model.Cells.ToDictionary(cell => cell.Index);
        _grid.Columns = model.Columns;
        _mapCanvas.CustomMinimumSize = new Vector2(model.Columns * _cellSize, model.Rows * _cellSize);
        foreach (var stale in _cells.Keys.Where(index => index >= expected).ToArray())
        {
            var cell = _cells[stale];
            cell.Selected -= OnCellSelected;
            _grid.RemoveChild(cell);
            cell.QueueFree();
            _cells.Remove(stale);
        }
        for (var index = 0; index < expected; index++)
        {
            if (!_cells.TryGetValue(index, out var cell))
            {
                cell = CellScene.Instantiate<ExpeditionMapCell>();
                cell.Name = $"Cell{index}";
                _grid.AddChild(cell);
                cell.Selected += OnCellSelected;
                _cells.Add(index, cell);
            }
            // Unprovided cells are opaque fog, never inferred terrain.
            var cellModel = data.GetValueOrDefault(index) ?? new ExpeditionCellModel { Index = index };
            cell.SetRevealOrigin(new Vector2(model.PlayerCellIndex % model.Columns - index % model.Columns + .5f,
                model.PlayerCellIndex / model.Columns - index / model.Columns + .5f), revealReach);
            cell.UseExternalPartyMarker = true;
            cell.ReduceUiMotion = ReduceUiMotion;
            cell.Bind(cellModel, index == model.PlayerCellIndex, index == _selectedIndex);
            cell.SetSiteInteractionAvailable(!model.InteractionLocked && index == model.PlayerCellIndex &&
                model.SelectedDetails is { CanInspect: true } details && details.CellIndex == index);
            cell.CustomMinimumSize = Vector2.One * _cellSize;
            SetFocusNeighbour(cell, "focus_neighbor_left", index % model.Columns > 0 ? index - 1 : index);
            SetFocusNeighbour(cell, "focus_neighbor_right", index % model.Columns < model.Columns - 1 ? index + 1 : index);
            SetFocusNeighbour(cell, "focus_neighbor_top", index >= model.Columns ? index - model.Columns : index);
            SetFocusNeighbour(cell, "focus_neighbor_bottom", index + model.Columns < expected ? index + model.Columns : index);
        }
        RefreshZoom();
    }

    private static void SetFocusNeighbour(Control cell, StringName property, int index) =>
        cell.Set(property, new NodePath($"../Cell{index}"));

    private void OnCellSelected(int index)
    {
        _selectedIndex = index;
        foreach (var pair in _cells) pair.Value.SetSelected(pair.Key == index);
        RefreshDetails(animate: true);
        EmitSignal(SignalName.CellSelected, index);
    }

    private void RefreshDetails(bool animate = false)
    {
        if (_model is not { } model) return;
        var cell = model.Cells.FirstOrDefault(candidate => candidate.Index == _selectedIndex);
        var known = cell?.IsRevealed == true;
        var details = known && model.SelectedDetails?.CellIndex == _selectedIndex ? model.SelectedDetails : null;
        _coordinate.Text = _selectedIndex < 0 ? "选择地块" : $"{_selectedIndex % model.Columns + 1} 列 · {_selectedIndex / model.Columns + 1} 行";
        _detailTitle.Text = !known ? "战争迷雾" : details?.Title ?? (string.IsNullOrEmpty(cell!.SiteName) ? cell.TerrainName : cell.SiteName);
        _detailSubtitle.Text = known ? details?.Subtitle ?? cell!.RegionName : "未知之地";
        _description.Text = !known ? "向迷雾边缘前进，发现新的道路、伙伴与遭遇。" : details?.Description ?? (_selectedIndex == model.PlayerCellIndex ? "军团在此驻足。选择道路继续探索，或整理已有的英雄与装备。" : "沿已知道路前进，靠近后处理此处的遭遇。");
        _detailIcon.Texture = known ? details?.Icon ?? cell!.SiteIcon ?? _cells.GetValueOrDefault(_selectedIndex)?.ResolveSiteIcon(cell.SiteAppearance) : null;
        _detailIcon.Visible = _detailIcon.Texture is not null;
        SetCopy(_cost, details?.CostText);
        SetCopy(_reward, details?.RewardText);
        SetCopy(_risk, details?.RiskText);
        SetCopy(_availability, details?.AvailabilityText ?? (known ? cell!.MoveBlockedReason : "靠近后揭示"));
        _move.Visible = known;
        _move.Text = _selectedIndex == model.PlayerCellIndex ? "军团在此" : "沿路前往";
        _move.Disabled = model.InteractionLocked || cell?.CanMove != true;
        _move.TooltipText = known ? cell!.MoveBlockedReason : string.Empty;
        _inspect.Visible = details is not null && !string.IsNullOrEmpty(details.SiteId);
        _inspect.Text = details?.InspectLabel ?? "查看地点";
        _inspect.Disabled = model.InteractionLocked || details?.CanInspect != true;
        if (_detailShownIndex != _selectedIndex)
        {
            _detailShownIndex = _selectedIndex;
            // Walking refreshes the persistent inspector; only deliberate
            // inspection opens it from a source. Cell identity is not an open event.
            if (animate) AnimateDetailsFromSelection();
            else ResetDetailMotion();
        }
    }

    private void AnimateDetailsFromSelection()
    {
        ResetDetailMotion();
        if (!IsVisibleInTree() || ReduceUiMotion() || !_cells.TryGetValue(_selectedIndex, out var origin)) return;
        var direction = origin.GetGlobalRect().GetCenter() - _detailsPanel.GetGlobalRect().GetCenter();
        _detailsPanel.OffsetTransformPosition = direction.Normalized() * 18f;
        _detailsPanel.Modulate = new Color(1, 1, 1, .65f);
        _detailsTween = CreateTween().SetIgnoreTimeScale(true).SetParallel()
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _detailsTween.TweenProperty(_detailsPanel, "offset_transform_position", Vector2.Zero, .20);
        _detailsTween.TweenProperty(_detailsPanel, "modulate:a", 1f, .13);
        _detailsTween.Chain().TweenCallback(Callable.From(() => _detailsTween = null));
        SetProcess(true);
    }

    private void ResetDetailMotion()
    {
        _detailsTween?.Kill();
        _detailsTween = null;
        if (_detailsPanel is null) return;
        _detailsPanel.OffsetTransformPosition = Vector2.Zero;
        _detailsPanel.Modulate = Colors.White;
    }

    private static void SetCopy(Label label, string? text)
    {
        label.Text = text ?? string.Empty;
        label.Visible = !string.IsNullOrWhiteSpace(text);
    }

    private void OnMove()
    {
        if (_model is { InteractionLocked: false } model &&
            model.Cells.Any(cell => cell.Index == _selectedIndex && cell.IsRevealed && cell.CanMove))
            EmitSignal(SignalName.MoveRequested, _selectedIndex);
    }

    private void OnInspect()
    {
        if (_model is { InteractionLocked: false, SelectedDetails: { CanInspect: true } details } &&
            details.CellIndex == _selectedIndex && !string.IsNullOrEmpty(details.SiteId))
            EmitSignal(SignalName.InspectRequested, details.SiteId);
    }

    private void OnBoss()
    {
        if (_model is { InteractionLocked: false, CanChallengeBoss: true }) EmitSignal(SignalName.BossRequested);
    }

    private void OnArmy() { if (_model?.CanOpenArmy == true) EmitSignal(SignalName.ArmyRequested); }
    private void OnMenu() { if (_model?.CanOpenMenu == true) EmitSignal(SignalName.MenuRequested); }
    private void OnZoomIn() => ChangeZoom(8);
    private void OnZoomOut() => ChangeZoom(-8);

    private void ChangeZoom(int delta)
    {
        _partyLight.Clear();
        _cellSize = Math.Clamp(_cellSize + delta, 40, 80);
        foreach (var cell in _cells.Values) cell.CustomMinimumSize = Vector2.One * _cellSize;
        if (_model is { } model) _mapCanvas.CustomMinimumSize = new Vector2(model.Columns * _cellSize, model.Rows * _cellSize);
        _animateParty = false;
        _committedSteps.Clear();
        RefreshZoom();
        CenterOnParty();
    }

    private void RefreshZoom()
    {
        _zoom.Text = $"{Math.Round(_cellSize / 56.0 * 100)}%";
        _zoomOut.Disabled = _cellSize <= 40;
        _zoomIn.Disabled = _cellSize >= 80;
    }

    public void CenterOnParty()
    {
        if (!_ready || !IsInsideTree() || _model is null) return;
        _centerRequested = true;
        _centerLayoutFrames = 1;
        SetProcess(IsVisibleInTree());
    }

    private void OnMapLayoutChanged()
    {
        if (!_ready) return;
        _centerLayoutFrames = 1;
        SetProcess(IsVisibleInTree());
    }

    private void OnMapVisibilityChanged()
    {
        if (!_ready) return;
        if (IsVisibleInTree())
        {
            _centerLayoutFrames = 1;
            _restoreScroll = !_centerRequested;
            SetProcess(true);
        }
        else
        {
            _savedScroll = new Vector2(_mapScroll.ScrollHorizontal, _mapScroll.ScrollVertical);
            _animateParty = _followParty = false;
            _committedSteps.Clear();
            ResetDetailMotion();
            _partyLight.Clear();
            foreach (var cell in _cells.Values) cell.SettleReveal();
            SetProcess(false);
        }
    }

    public override void _Process(double delta)
    {
        if (!_ready || !IsVisibleInTree() || _model is null ||
            !_cells.TryGetValue(_model.PlayerCellIndex, out var cell))
        {
            SetProcess(false);
            return;
        }
        if (_centerLayoutFrames-- > 0) return;
        var horizontal = _mapScroll.GetHScrollBar();
        var vertical = _mapScroll.GetVScrollBar();
        var minimum = _grid.GetCombinedMinimumSize();
        // The authored canvas is not a Container: explicitly let the grid shrink
        // with zoom instead of retaining its previous larger Control size.
        if (_grid.Size != minimum) _grid.Size = minimum;
        if (_grid.Size.X < minimum.X || _grid.Size.Y < minimum.Y ||
            horizontal.Page <= 0 || vertical.Page <= 0 || cell.Size.X <= 0 || cell.Size.Y <= 0)
            return;
        _partyMarker.Size = cell.Size;
        var destination = _animateParty && _cells.TryGetValue(_visualStepIndex, out var visualCell)
            ? visualCell.Position : cell.Position;
        if (!_partyPositioned || !_animateParty || ReduceUiMotion())
        {
            _partyMarker.Position = cell.Position;
            _animateParty = false;
            _committedSteps.Clear();
        }
        else
        {
            _partyMoveElapsed += delta;
            var t = Math.Clamp((float)(_partyMoveElapsed / (_committedSteps.Count > 1 ? .08 : .14)), 0f, 1f);
            var smooth = t * t * (3f - 2f * t);
            _partyMarker.Position = _partyFrom.Lerp(destination, smooth);
            if (t >= 1)
            {
                if (_committedSteps.TryDequeue(out var nextCell))
                {
                    _visualStepIndex = nextCell;
                    _partyFrom = destination;
                    _partyMoveElapsed = 0;
                }
                else _animateParty = false;
            }
        }
        _partyPositioned = true;
        _partyMarker.Visible = true;
        _partyLight.Track(_partyMarker.Position + _partyMarker.Size * .5f, _cellSize, _animateParty);
        if (_restoreScroll)
        {
            _mapScroll.ScrollHorizontal = (int)_savedScroll.X;
            _mapScroll.ScrollVertical = (int)_savedScroll.Y;
            _restoreScroll = false;
        }
        if (_centerRequested || _followParty)
        {
            var point = _partyMarker.Position + _partyMarker.Size * .5f;
            var target = new Vector2(
                (float)Math.Clamp(point.X - horizontal.Page * .5, 0, Math.Max(0, horizontal.MaxValue - horizontal.Page)),
                (float)Math.Clamp(point.Y - vertical.Page * .5, 0, Math.Max(0, vertical.MaxValue - vertical.Page)));
            var current = new Vector2(_mapScroll.ScrollHorizontal, _mapScroll.ScrollVertical);
            var next = _snapCamera || ReduceUiMotion() ? target
                : current.Lerp(target, 1f - Mathf.Exp(-12f * (float)delta));
            // Integer ScrollContainer coordinates would otherwise stall one pixel short.
            if (next.DistanceTo(target) < 2f) next = target;
            _mapScroll.ScrollHorizontal = (int)Math.Round(next.X);
            _mapScroll.ScrollVertical = (int)Math.Round(next.Y);
            if (!_animateParty && next.DistanceTo(target) < 2f) _followParty = false;
            else _followParty = true;
            _centerRequested = _snapCamera = false;
        }
        if (ReduceUiMotion()) ResetDetailMotion();
        if (!_animateParty && !_followParty && !_centerRequested && _detailsTween is null) SetProcess(false);
    }

    public void FocusParty()
    {
        if (_model is not null && _cells.TryGetValue(_model.PlayerCellIndex, out var cell)) cell.GrabFocus();
    }
}
