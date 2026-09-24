using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class DeploymentBoard : Control
{
    public event Action<Vector2I, string>? CellSelected;
    public event Action<string, Vector2I>? PieceDropped;
    public event Action<string>? EnemySelected;

    private readonly List<DeploymentCell> _cells = [];
    private readonly List<(DeploymentMarker Marker, Vector2I Cell)> _markers = [];
    private readonly List<(EnemyDeploymentPreview Preview, Vector2I Cell)> _enemyPreviews = [];
    private readonly Dictionary<string, IReadOnlyDictionary<Vector2I, FormationEvaluation>> _evaluations =
        new(StringComparer.Ordinal);
    private PackedScene _cellScene = null!;
    private PackedScene _markerScene = null!;
    private PackedScene _enemyPreviewScene = null!;
    private IBattleFloorRuleRuntime? _floorRule;
    private BattlefieldProjection _projection;
    private DeploymentFootprintLayer _footprints = null!;
    private readonly Dictionary<string, float> _bodyRadii = new(StringComparer.Ordinal);
    public Func<string, Variant, EquipmentDropEvaluation>? EquipmentDropEvaluator { get; set; }
    public Action<string, Variant>? EquipmentReceiver { get; set; }

    public BattlefieldProjection CurrentProjection => _projection;
    public int CandidateCellCount => _cells.Count;
    public Vector2I? CurrentDragHoverCell { get; private set; }

    public override void _Ready()
    {
        _footprints = GetNode<DeploymentFootprintLayer>("%DeploymentFootprints");
        _cellScene = GD.Load<PackedScene>("res://scenes/ui/components/DeploymentCell.tscn");
        _markerScene = GD.Load<PackedScene>("res://scenes/ui/components/DeploymentMarker.tscn");
        _enemyPreviewScene = GD.Load<PackedScene>("res://scenes/ui/components/EnemyDeploymentPreview.tscn");
        for (var y = 0; y < BattlefieldLayout.Height; y++)
        for (var x = 0; x < BattlefieldLayout.PlayerDeploymentColumns; x++)
        {
            var cell = _cellScene.Instantiate<DeploymentCell>();
            AddChild(cell);
            cell.Bind(new Vector2I(x, y), string.Empty, string.Empty, false, false, true, FloorCellPreview.Normal);
            cell.CellSelected += OnCellSelected;
            cell.PieceDropped += OnPieceDropped;
            cell.ConfigureDrag(pieceId => EvaluationFor(pieceId, cell.Cell) ?? LegacyEvaluation(cell.Cell), OnDragHovered);
            cell.ConfigureEquipmentDrop(
                data => EquipmentDropEvaluator?.Invoke(cell.PieceId, data)
                    ?? EquipmentDropEvaluation.Reject("当前不能更换装备。"),
                data => EquipmentReceiver?.Invoke(cell.PieceId, data));
            _cells.Add(cell);
        }
        UpdateProjection();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && _cells.Count > 0) UpdateProjection();
        else if (what == NotificationDragEnd) ClearDragHover();
    }

    public override void _Process(double delta)
    {
        if (_footprints.DragBody is not null &&
            (!GetViewport().GuiIsDragging() || CurrentDragHoverCell is not { } cell ||
             !_projection.CellRect(cell).HasPoint(GetLocalMousePosition())))
            ClearDragHover();
    }

    public override void _ExitTree()
    {
        ClearDragHover();
        foreach (var cell in _cells)
        {
            cell.CellSelected -= OnCellSelected;
            cell.PieceDropped -= OnPieceDropped;
        }
    }

    public void Bind(
        BattleConfig config,
        IReadOnlyList<DeploymentUnitViewModel> pieces,
        string selectedId,
        IReadOnlyList<EnemyDeploymentViewModel>? enemies = null)
    {
        ClearDragHover();
        _floorRule = config.FloorRule;
        _evaluations.Clear();
        _bodyRadii.Clear();
        foreach (var piece in pieces)
        {
            _bodyRadii[piece.InstanceId] = piece.BodyRadius;
            if (piece.TargetEvaluations is not null)
                _evaluations[piece.InstanceId] = piece.TargetEvaluations;
        }
        _footprints.Bind(pieces.Where(piece => piece.Cell is not null).Select(piece =>
            new DeploymentBodyPreview(piece.InstanceId, piece.Cell!.Value, piece.BodyRadius, 0, piece.InstanceId == selectedId))
            .Concat((enemies ?? []).Select(enemy => new DeploymentBodyPreview(enemy.InstanceId, enemy.Cell, enemy.BodyRadius, 1))));
        var occupants = pieces.Where(piece => piece.Cell is not null).ToDictionary(piece => piece.Cell!.Value);
        foreach (var cellControl in _cells)
        {
            var cell = cellControl.Cell;
            occupants.TryGetValue(cell, out var piece);
            var preview = _floorRule.GetCellPreview(cell);
            cellControl.Bind(cell, piece?.InstanceId ?? string.Empty, piece?.DisplayName ?? string.Empty,
                piece?.IsHero == true, piece?.InstanceId == selectedId, _floorRule.CanOccupy(cell), preview,
                piece?.Portrait, piece?.Role ?? UnitRole.Fighter, piece?.AttackRange ?? 1f,
                false);
            cellControl.ApplyProjection(_projection);
        }
        WireCellFocus();

        ClearMarkers();
        SynchronizeEnemyPreviews(enemies ?? []);
        for (var y = 0; y < BattlefieldLayout.Height; y++)
        for (var x = BattlefieldLayout.PlayerDeploymentColumns; x < BattlefieldLayout.Width; x++)
        {
            var cell = new Vector2I(x, y);
            var preview = _floorRule.GetCellPreview(cell);
            if (preview == FloorCellPreview.Hazard)
                AddMarker(cell, SemanticIconKeys.Risk, "危险地形", new Color(1f, .64f, .35f));
            else if (preview == FloorCellPreview.Objective)
                AddMarker(cell, SemanticIconKeys.Loot, "楼层目标", new Color(.36f, 1f, .82f));
            else if (preview == FloorCellPreview.Blocked)
                AddMarker(cell, SemanticIconKeys.Deaths, "阻挡格", new Color(.62f, .68f, .76f));
        }
        QueueRedraw();
    }

    public void FlashCell(Vector2I cell, bool success) =>
        _cells.FirstOrDefault(candidate => candidate.Cell == cell)?.FlashResult(success);

    public override void _Draw()
    {
        if (!_projection.IsValid) return;
        for (var y = 0; y < BattlefieldLayout.Height; y++)
        for (var x = 0; x < BattlefieldLayout.Width; x++)
        {
            var cell = new Vector2I(x, y);
            var preview = _floorRule?.GetCellPreview(cell) ?? FloorCellPreview.Normal;
            var inPlayerZone = BattlefieldLayout.IsPlayerDeploymentCell(cell);
            var color = preview switch
            {
                FloorCellPreview.Blocked => new Color("243043"),
                FloorCellPreview.Hazard => new Color("6e2f2f"),
                FloorCellPreview.Objective => new Color("285b57"),
                _ when inPlayerZone => (x + y) % 2 == 0 ? new Color("34445a") : new Color("2d3c52"),
                _ => (x + y) % 2 == 0 ? new Color("252c3b") : new Color("202736")
            };
            var rect = _projection.CellRect(cell);
            DrawRect(rect, color, true);
            DrawRect(rect, inPlayerZone ? new Color(1f, .78f, .3f, .28f) : new Color(1f, 1f, 1f, .08f), false,
                inPlayerZone ? 2f : 1f);
        }

        var boundaryX = (_projection.CellToLocal(new Vector2I(2, 0)).X +
                         _projection.CellToLocal(new Vector2I(3, 0)).X) * .5f;
        DrawLine(new Vector2(boundaryX, _projection.CellRect(Vector2I.Zero).Position.Y),
            new Vector2(boundaryX, _projection.CellRect(new Vector2I(0, BattlefieldLayout.Height - 1)).End.Y),
            new Color(1f, .78f, .3f, .75f), 3f);
    }

    private void UpdateProjection()
    {
        _projection = BattlefieldProjection.Fit(Size);
        _footprints.SetGrid(_projection.Origin, _projection.CellPitch, _projection.CellRect(Vector2I.Zero).Size);
        foreach (var cell in _cells) cell.ApplyProjection(_projection);
        foreach (var (marker, cell) in _markers) ApplyMarkerProjection(marker, cell);
        foreach (var (preview, cell) in _enemyPreviews) ApplyEnemyProjection(preview, cell);
        QueueRedraw();
    }

    private void AddMarker(Vector2I cell, StringName semanticKey, string detail, Color color)
    {
        var marker = _markerScene.Instantiate<DeploymentMarker>();
        AddChild(marker);
        marker.Bind(semanticKey, detail, color);
        _markers.Add((marker, cell));
        ApplyMarkerProjection(marker, cell);
    }

    private void ApplyMarkerProjection(Control marker, Vector2I cell)
    {
        var rect = _projection.CellRect(cell);
        marker.Position = rect.Position;
        marker.Size = rect.Size;
        marker.CustomMinimumSize = rect.Size;
    }

    private void ClearMarkers()
    {
        foreach (var (marker, _) in _markers)
        {
            RemoveChild(marker);
            marker.Free();
        }
        _markers.Clear();
    }

    private void AddEnemyPreview(EnemyDeploymentViewModel model)
    {
        var preview = _enemyPreviewScene.Instantiate<EnemyDeploymentPreview>();
        AddChild(preview);
        preview.Bind(model);
        preview.Selected += OnEnemySelected;
        _enemyPreviews.Add((preview, model.Cell));
        ApplyEnemyProjection(preview, model.Cell);
    }

    private void ApplyEnemyProjection(EnemyDeploymentPreview preview, Vector2I cell)
    {
        var size = preview.BodyRadius > .5f ? _projection.CellPitch * (2 * preview.BodyRadius) : _projection.CellRect(cell).Size;
        preview.CustomMinimumSize = Vector2.Zero;
        preview.Size = size;
        preview.Position = _projection.CellToLocal(cell) - size * .5f;
    }

    private void ClearEnemyPreviews()
    {
        foreach (var (preview, _) in _enemyPreviews)
        {
            preview.Selected -= OnEnemySelected;
            RemoveChild(preview);
            preview.QueueFree();
        }
        _enemyPreviews.Clear();
    }

    private void SynchronizeEnemyPreviews(IReadOnlyList<EnemyDeploymentViewModel> enemies)
    {
        // Rebinding selection must preserve the focused button and its portrait playback.
        if (_enemyPreviews.Select(item => item.Preview.InstanceId)
            .SequenceEqual(enemies.Select(enemy => enemy.InstanceId)))
        {
            for (var index = 0; index < enemies.Count; index++)
            {
                var preview = _enemyPreviews[index].Preview;
                preview.Bind(enemies[index]);
                _enemyPreviews[index] = (preview, enemies[index].Cell);
                ApplyEnemyProjection(preview, enemies[index].Cell);
            }
            return;
        }
        ClearEnemyPreviews();
        foreach (var enemy in enemies) AddEnemyPreview(enemy);
    }

    private FormationEvaluation? EvaluationFor(string pieceId, Vector2I cell) =>
        _evaluations.TryGetValue(pieceId, out var byCell) && byCell.TryGetValue(cell, out var evaluation)
            ? evaluation
            : null;

    private FormationEvaluation LegacyEvaluation(Vector2I cell) =>
        _floorRule?.CanOccupy(cell) == true
            ? FormationEvaluation.Accept(FormationOperation.Move)
            : FormationEvaluation.Reject("该格受楼层规则阻挡。");

    private void OnDragHovered(DeploymentCell cell, FormationEvaluation? evaluation)
    {
        if (CurrentDragHoverCell is { } current && current != cell.Cell)
            _cells.FirstOrDefault(candidate => candidate.Cell == current)?.ClearDragState();
        CurrentDragHoverCell = cell.Cell;
        cell.SetDragHovered(evaluation ?? FormationEvaluation.Reject("该格当前不可部署。"));
        var data = GetViewport().GuiGetDragData();
        if (data.VariantType == Variant.Type.Dictionary && data.AsGodotDictionary().TryGetValue("piece_id", out var identity) &&
            _bodyRadii.TryGetValue(identity.AsString(), out var radius))
            _footprints.ShowDrag(new DeploymentBodyPreview(identity.AsString(), cell.Cell, radius, 0), evaluation?.IsValid == true);
    }

    public void ClearDragHover()
    {
        if (IsInstanceValid(_footprints)) _footprints.ShowDrag(null);
        foreach (var cell in _cells) cell.ClearDragState();
        CurrentDragHoverCell = null;
    }

    private void WireCellFocus()
    {
        var byCell = _cells.ToDictionary(cell => cell.Cell);
        foreach (var cell in _cells)
        {
            cell.FocusNeighborLeft = NeighborPath(byCell, cell.Cell, Vector2I.Left);
            cell.FocusNeighborRight = NeighborPath(byCell, cell.Cell, Vector2I.Right);
            cell.FocusNeighborTop = NeighborPath(byCell, cell.Cell, Vector2I.Up);
            cell.FocusNeighborBottom = NeighborPath(byCell, cell.Cell, Vector2I.Down);
        }
    }

    private static NodePath NeighborPath(IReadOnlyDictionary<Vector2I, DeploymentCell> cells, Vector2I start, Vector2I direction)
    {
        var next = start + direction;
        while (BattlefieldLayout.IsPlayerDeploymentCell(next))
        {
            if (cells.TryGetValue(next, out var cell) && cell.FocusMode == FocusModeEnum.All) return cell.GetPath();
            next += direction;
        }
        return new NodePath();
    }

    private void OnCellSelected(Vector2I cell, string pieceId) => CellSelected?.Invoke(cell, pieceId);
    private void OnEnemySelected(string instanceId) => EnemySelected?.Invoke(instanceId);
    private void OnPieceDropped(string pieceId, Vector2I cell)
    {
        ClearDragHover();
        PieceDropped?.Invoke(pieceId, cell);
    }
}
