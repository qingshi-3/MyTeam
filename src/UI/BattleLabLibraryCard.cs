using System;
using Godot;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class BattleLabLibraryCard : Button
{
    private const float DragThreshold = 8f;

    public event Action<string, BattleLabSide>? Selected;
    public event Action<string, BattleLabSide>? DragRequested;
    public string ContentId { get; private set; } = string.Empty;
    public BattleLabSide Side { get; private set; }
    private UnitPortrait _portrait = null!;
    private Label _name = null!;
    private Label _classification = null!;
    private Label _selection = null!;
    private bool _pointerHeld;
    private bool _dragStarted;
    private bool _releaseQueued;
    private Vector2 _pressPosition;

    public override void _Ready()
    {
        CacheNodes();
        Pressed += OnPressed;
        VisibilityChanged += OnVisibilityChanged;
        SetProcessInput(false);
    }

    public void Bind(string contentId, string displayName, BattleLabSide side, string classification,
        UnitPortraitDefinition? portrait = null)
    {
        CacheNodes();
        ContentId = contentId ?? string.Empty;
        Side = side;
        Text = string.Empty;
        _name.Text = string.IsNullOrWhiteSpace(displayName) ? "未命名单位" : displayName;
        _classification.Text = classification;
        _classification.Visible = !string.IsNullOrWhiteSpace(classification);
        _portrait.Bind(portrait, SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Melee));
        SetInspection(new(_name.Text));
        Disabled = string.IsNullOrWhiteSpace(ContentId);
    }

    public void SetInspection(BattleLabTooltipInfo? info)
    {
        BattleLabHoverHint.Bind(this, info);
    }

    public void SetSelected(bool selected)
    {
        CacheNodes();
        ThemeTypeVariation = selected ? "SelectedButton" : "SecondaryButton";
        _selection.Visible = selected;
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
        else if (_pointerHeld && !_dragStarted && inputEvent is InputEventMouseMotion motion &&
                 motion.Position.DistanceSquaredTo(_pressPosition) >= DragThreshold * DragThreshold)
        {
            BattleLabHoverHint.HideAll(true);
            _dragStarted = true;
            DragRequested?.Invoke(ContentId, Side);
            AcceptEvent();
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        // The GUI release may belong to another control when the pointer leaves this card.
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
            QueuePointerRelease();
    }

    public override void _ExitTree()
    {
        ResetPointer();
        Pressed -= OnPressed;
        VisibilityChanged -= OnVisibilityChanged;
        Selected = null;
        DragRequested = null;
        ContentId = string.Empty;
    }

    private void OnPressed()
    {
        if (!Disabled && !_dragStarted) Selected?.Invoke(ContentId, Side);
    }

    private void QueuePointerRelease()
    {
        if (!_pointerHeld || _releaseQueued) return;
        _releaseQueued = true;
        // Keep suppression until Button has processed this same release event.
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
        _classification ??= GetNode<Label>("%Classification");
        _selection ??= GetNode<Label>("%SelectionMark");
    }
}
