using System;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class BattleLabBoardCell : Button
{
    private const float DragThreshold = 8f;

    public event Action<string>? UnitSelected;
    public event Action<string>? UnitDragRequested;
    public Func<Variant, bool>? EquipmentDropAllowed { get; set; }
    public Action<Variant>? EquipmentDropReceived { get; set; }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (!EquipmentSlotButton.TryEquipmentId(data, out _)) return false;
        var allowed = EquipmentDropAllowed?.Invoke(data) == true;
        _dropMark.Text = allowed ? "装" : "×";
        _dropMark.Visible = true;
        ThemeTypeVariation = allowed ? "EquipmentSlotValid" : "EquipmentSlotInvalid";
        return allowed;
    }
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (EquipmentDropAllowed?.Invoke(data) == true) EquipmentDropReceived?.Invoke(data);
        ClearDropState();
    }
    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd && IsNodeReady()) ClearDropState();
    }
    public Vector2I Cell { get; private set; }
    public string InstanceId { get; private set; } = string.Empty;
    private UnitPortrait _portrait = null!;
    private Label _name = null!;
    private Label _team = null!;
    private Label _empty = null!;
    private Label _dropMark = null!;
    private string _baseTooltip = string.Empty;
    private string _baseTheme = "GridCellButton";
    private bool _hasPortraitBinding;
    private bool _selected;
    private bool _pointerHeld;
    private bool _dragStarted;
    private bool _releaseQueued;
    private Vector2 _pressPosition;
    private BattleLabTooltipInfo? _inspection;
    private string _inspectionInstanceId = string.Empty;

    public override void _Ready()
    {
        CacheNodes();
        Pressed += OnPressed;
        VisibilityChanged += OnVisibilityChanged;
        MouseExited += ClearDropState;
        SetProcessInput(false);
    }

    public void BindCell(Vector2I cell)
    {
        Cell = cell;
        Refresh(null, false);
    }

    public void Refresh(BattleLabUnitConfiguration? unit, bool selected, string? displayName = null,
        UnitPortraitDefinition? portrait = null, BattleLabPlacementMode mode = BattleLabPlacementMode.FreeExperiment,
        float bodyRadius = .32f)
    {
        CacheNodes();
        InstanceId = unit?.InstanceId ?? string.Empty;
        if (_inspectionInstanceId != InstanceId)
        {
            _inspection = null;
            _inspectionInstanceId = InstanceId;
        }
        _selected = selected;
        Text = string.Empty;
        _portrait.Visible = unit is not null && bodyRadius <= .5f;
        _name.Visible = unit is not null && bodyRadius <= .5f;
        _team.Visible = unit is not null;
        _empty.Visible = unit is null;
        _name.Text = string.IsNullOrWhiteSpace(displayName) ? "单位" : displayName;
        if (unit is not null)
        {
            // Selection and drop previews refresh often; keep the existing idle animation running.
            if (!_hasPortraitBinding || !ReferenceEquals(_portrait.Definition, portrait))
            {
                _portrait.Bind(portrait, SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Melee));
                _hasPortraitBinding = true;
            }
            _team.Text = unit.Side == BattleLabSide.Player ? "A" : "B";
            _team.Modulate = unit.Side == BattleLabSide.Player
                ? new Color(.58f, .82f, 1f) : new Color(1f, .61f, .55f);
        }
        _baseTheme = unit is not null
            ? unit.Side == BattleLabSide.Player ? "BattleLabPlayerCell" : "BattleLabEnemyCell"
            : "GridCellButton";
        const string emptyZoneText = "自由实验 · 空格";
        _baseTooltip = unit is null
            ? $"{emptyZoneText}\n第 {Cell.X + 1} 列 / 第 {Cell.Y + 1} 行"
            : $"{_name.Text} · {(unit.Side == BattleLabSide.Player ? "A 队" : "B 队")}\n点击查看，拖动以移动或交换。\n第 {Cell.X + 1} 列 / 第 {Cell.Y + 1} 行";
        ClearDropState();
    }

    public void SetInspection(BattleLabTooltipInfo? info)
    {
        _inspection = info;
        _inspectionInstanceId = InstanceId;
        BattleLabHoverHint.Bind(this, info ?? new BattleLabTooltipInfo(
            string.IsNullOrEmpty(InstanceId) ? "空格" : _name.Text, Hint: _baseTooltip));
    }

    public void ShowDropState(bool legal, bool swap)
    {
        BattleLabHoverHint.HideAll(true);
        CacheNodes();
        _dropMark.Text = legal ? swap ? "⇄" : "＋" : "×";
        _dropMark.Visible = true;
        ThemeTypeVariation = legal ? swap ? "DeploymentCellSwap" : "DeploymentCellLegal" : "DeploymentCellIllegal";
        TooltipText = $"{(legal ? swap ? "松开交换位置" : "松开放置" : "此处不可放置")}\n{_baseTooltip}";
    }

    public void ClearDropState()
    {
        CacheNodes();
        _dropMark.Text = "选";
        _dropMark.Visible = _selected;
        ThemeTypeVariation = _selected ? "DeploymentCellSelected" : _baseTheme;
        SetInspection(_inspection);
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (Disabled) return;
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            if (button.Pressed)
            {
                BattleLabHoverHint.HideAll(true);
                ResetPointer();
                _pointerHeld = true;
                _pressPosition = button.Position;
                SetProcessInput(true);
            }
            else QueuePointerRelease();
        }
        else if (_pointerHeld && !_dragStarted && !string.IsNullOrWhiteSpace(InstanceId) &&
                 inputEvent is InputEventMouseMotion motion &&
                 motion.Position.DistanceSquaredTo(_pressPosition) >= DragThreshold * DragThreshold)
        {
            BattleLabHoverHint.HideAll(true);
            _dragStarted = true;
            UnitDragRequested?.Invoke(InstanceId);
            AcceptEvent();
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
            QueuePointerRelease();
    }

    public override void _ExitTree()
    {
        ResetPointer();
        Pressed -= OnPressed;
        VisibilityChanged -= OnVisibilityChanged;
        UnitSelected = null;
        UnitDragRequested = null;
        InstanceId = string.Empty;
    }

    private void OnPressed()
    {
        if (Disabled || _dragStarted) return;
        // Activation inspects an existing piece; placement is exclusively a drag gesture.
        if (!string.IsNullOrWhiteSpace(InstanceId)) UnitSelected?.Invoke(InstanceId);
    }

    private void QueuePointerRelease()
    {
        if (!_pointerHeld || _releaseQueued) return;
        _releaseQueued = true;
        // A drop can refresh this cell before Button emits Pressed for the release.
        Callable.From(ResetPointer).CallDeferred();
    }

    private void ResetPointer()
    {
        _pointerHeld = false;
        _dragStarted = false;
        _releaseQueued = false;
        SetProcessInput(false);
    }

    private void OnVisibilityChanged()
    {
        if (!IsVisibleInTree()) ResetPointer();
    }

    private void CacheNodes()
    {
        _portrait ??= GetNode<UnitPortrait>("%UnitPortrait");
        _name ??= GetNode<Label>("%UnitName");
        _team ??= GetNode<Label>("%TeamMark");
        _empty ??= GetNode<Label>("%EmptyMark");
        _dropMark ??= GetNode<Label>("%DropMark");
    }
}
