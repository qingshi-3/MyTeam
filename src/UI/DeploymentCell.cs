using System;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class DeploymentCell : Button
{
    public event Action<Vector2I, string>? CellSelected;
    public event Action<string, Vector2I>? PieceDropped;
    public Vector2I Cell { get; private set; } = new(-1, -1);
    public string PieceId { get; private set; } = string.Empty;
    public bool IsLegalTarget { get; private set; }
    public bool IsDragHovered => _dropHover;
    private UnitPortrait _portrait = null!;
    private TextureRect _heroBadge = null!;
    private TextureRect _roleBadge = null!;
    private TextureRect _reachBadge = null!;
    private bool _selected;
    private bool _hasSelection;
    private bool _dragSource;
    private bool _dropHover;
    private FormationEvaluation? _targetEvaluation;
    private FormationEvaluation? _dragEvaluation;
    private Func<string, FormationEvaluation?>? _dropEvaluator;
    private Action<DeploymentCell, FormationEvaluation?>? _dragHoverRequested;
    private Tween? _resultTween;

    public override void _Ready()
    {
        _portrait = GetNode<UnitPortrait>("%UnitPortrait");
        _heroBadge = GetNode<TextureRect>("%HeroBadge");
        _roleBadge = GetNode<TextureRect>("%RoleBadge");
        _reachBadge = GetNode<TextureRect>("%ReachBadge");
        Pressed += OnPressed;
    }
    public override void _ExitTree()
    {
        _resultTween?.Kill();
        ClearDragState();
        Pressed -= OnPressed;
    }

    public void ConfigureDrag(
        Func<string, FormationEvaluation?> dropEvaluator,
        Action<DeploymentCell, FormationEvaluation?> dragHoverRequested)
    {
        _dropEvaluator = dropEvaluator;
        _dragHoverRequested = dragHoverRequested;
    }

    public void FlashResult(bool success)
    {
        _resultTween?.Kill();
        ThemeTypeVariation = success ? "DeploymentCellSuccess" : "DeploymentCellFailure";
        _resultTween = CreateTween();
        _resultTween.TweenInterval(.45);
        _resultTween.TweenCallback(Callable.From(RefreshVisualRole));
    }

    public override void _Notification(int what)
    {
        if (what != NotificationDragEnd || !_dragSource) return;
        _dragSource = false;
        RefreshVisualRole();
    }

    public void Bind(
        Vector2I cell,
        string pieceId,
        string displayName,
        bool isHero,
        bool selected,
        bool legalTarget,
        FloorCellPreview preview,
        UnitPortraitDefinition? portrait = null,
        UnitRole role = UnitRole.Fighter,
        float attackRange = 1f,
        bool hasSelection = false,
        FormationEvaluation? evaluation = null)
    {
        _portrait ??= GetNode<UnitPortrait>("%UnitPortrait");
        _heroBadge ??= GetNode<TextureRect>("%HeroBadge");
        _roleBadge ??= GetNode<TextureRect>("%RoleBadge");
        _reachBadge ??= GetNode<TextureRect>("%ReachBadge");
        Cell = cell;
        PieceId = pieceId;
        IsLegalTarget = legalTarget;
        _targetEvaluation = evaluation;
        _selected = selected;
        _hasSelection = hasSelection;
        _dragSource = false;
        ClearDragState();
        Text = string.Empty;
        var occupied = !string.IsNullOrEmpty(pieceId);
        _portrait.Visible = occupied;
        if (occupied)
            _portrait.Bind(portrait, SemanticIcons.Catalog.ResolveIcon(
                role is UnitRole.Ranged or UnitRole.Artillery ? SemanticIconKeys.Ranged : SemanticIconKeys.Melee));
        _heroBadge.Texture = SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Hero);
        _heroBadge.Visible = occupied && isHero;
        _heroBadge.Modulate = new Color(1f, .82f, .25f);
        _roleBadge.Texture = SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Responsibility(role));
        _roleBadge.Visible = occupied;
        _roleBadge.Modulate = isHero ? new Color(1f, .82f, .25f) : new Color(.78f, .9f, 1f);
        _reachBadge.Texture = SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Reach);
        _reachBadge.Visible = occupied;
        _reachBadge.Modulate = new Color(.35f, .9f, 1f);
        var rejection = evaluation?.RejectionReason;
        TooltipText = string.IsNullOrEmpty(pieceId)
            ? legalTarget ? $"部署到第 {cell.X + 1} 列、第 {cell.Y + 1} 行" : rejection ?? "该格当前不可部署"
            : legalTarget
                ? $"{displayName} · {PlayerFacingText.DescribeUnitRole(role)} · {UnitRangeClassifier.Describe(attackRange)} {attackRange:0.#} 格。点击选择，或拖动以移动/交换。"
                : rejection ?? $"{displayName} 当前不能作为目标。";
        Disabled = false;
        FocusMode = FocusModeEnum.All;
        RefreshVisualRole();
    }

    public void ApplyProjection(BattlefieldProjection projection)
    {
        var rect = projection.CellRect(Cell);
        Position = rect.Position;
        Size = rect.Size;
        CustomMinimumSize = rect.Size;
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (string.IsNullOrWhiteSpace(PieceId)) return default;
        _dragSource = true;
        RefreshVisualRole();
        var preview = (Control)Duplicate();
        preview.MouseFilter = MouseFilterEnum.Ignore;
        SetDragPreview(preview);
        return new Godot.Collections.Dictionary { ["piece_id"] = PieceId };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary) return false;
        var dictionary = data.AsGodotDictionary();
        if (!dictionary.ContainsKey("piece_id")) return false;
        var pieceId = dictionary["piece_id"].AsString();
        if (string.IsNullOrWhiteSpace(pieceId)) return false;
        var evaluation = _dropEvaluator is not null
            ? _dropEvaluator(pieceId)
            : _targetEvaluation ?? (IsLegalTarget
                ? FormationEvaluation.Accept(FormationOperation.Move)
                : FormationEvaluation.Reject("该格当前不可部署。"));
        if (_dragHoverRequested is not null) _dragHoverRequested(this, evaluation);
        else SetDragHovered(evaluation);
        return evaluation is not null;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary) return;
        var dictionary = data.AsGodotDictionary();
        if (!dictionary.ContainsKey("piece_id")) return;
        var pieceId = dictionary["piece_id"].AsString();
        var evaluation = _dropEvaluator is not null ? _dropEvaluator(pieceId) : _dragEvaluation;
        if (evaluation is not null) PieceDropped?.Invoke(pieceId, Cell);
    }

    public void SetDragHovered(FormationEvaluation? evaluation)
    {
        _dropHover = evaluation is not null;
        _dragEvaluation = evaluation;
        RefreshVisualRole();
    }

    public void ClearDragState()
    {
        if (!_dropHover && _dragEvaluation is null) return;
        _dropHover = false;
        _dragEvaluation = null;
        RefreshVisualRole();
    }

    private void OnPressed() => CellSelected?.Invoke(Cell, PieceId);

    private void RefreshVisualRole()
    {
        var activeEvaluation = _dropHover ? _dragEvaluation : _targetEvaluation;
        var activeLegal = activeEvaluation?.IsValid ?? IsLegalTarget;
        ThemeTypeVariation = _dragSource ? "DeploymentCellDrag"
            : _dropHover && !activeLegal ? "DeploymentCellIllegal"
            : _dropHover && !string.IsNullOrEmpty(PieceId) ? "DeploymentCellSwap"
            : _dropHover ? "DeploymentCellLegal"
            : _selected ? "DeploymentCellSelected"
            : _hasSelection && !IsLegalTarget ? "DeploymentCellIllegal"
            : _hasSelection && !string.IsNullOrEmpty(PieceId) ? "DeploymentCellSwap"
            : _hasSelection ? "DeploymentCellLegal"
            : "GridCellButton";
    }
}
