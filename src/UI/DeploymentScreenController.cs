using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class DeploymentScreenController : Control
{
    public event Action? BackRequested;
    public event Action? StartRequested;
    public event Action<FormationMoveCommand>? MoveRequested;
    public event Action<string>? WithdrawRequested;

    private Label _title = null!;
    private Label _encounter = null!;
    private Label _status = null!;
    private VBoxContainer _roster = null!;
    private DeploymentBoard _board = null!;
    private Button _withdraw = null!;
    private Button _back = null!;
    private Button _start = null!;
    private PackedScene _cardScene = null!;
    private readonly Dictionary<string, DeploymentUnitCard> _cards = new(StringComparer.Ordinal);
    private IReadOnlyList<DeploymentUnitViewModel> _pieces = [];
    private IReadOnlyList<EnemyDeploymentViewModel> _enemies = [];
    private BattleConfig? _config;
    private string _selectedId = string.Empty;
    private int _reserveCapacity;

    public string SelectedPieceId => _selectedId;
    public IBattleFloorRuleRuntime? FloorRule => _config?.FloorRule;

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _encounter = GetNode<Label>("%EncounterInfo");
        _status = GetNode<Label>("%Status");
        _roster = GetNode<VBoxContainer>("%RosterChoices");
        _board = GetNode<DeploymentBoard>("%DeploymentBoard");
        _withdraw = GetNode<Button>("%WithdrawButton");
        _back = GetNode<Button>("%BackButton");
        _start = GetNode<Button>("%StartBattleButton");
        _cardScene = GD.Load<PackedScene>("res://scenes/ui/components/DeploymentUnitCard.tscn");
        _board.CellSelected += OnCellSelected;
        _board.PieceDropped += OnPieceDropped;
        _withdraw.Pressed += OnWithdraw;
        _back.Pressed += OnBack;
        _start.Pressed += OnStart;
    }

    public override void _ExitTree()
    {
        foreach (var card in _cards.Values)
            if (IsInstanceValid(card)) card.UnitSelected -= OnPieceSelected;
        _cards.Clear();
        _board.CellSelected -= OnCellSelected;
        _board.PieceDropped -= OnPieceDropped;
        _withdraw.Pressed -= OnWithdraw;
        _back.Pressed -= OnBack;
        _start.Pressed -= OnStart;
    }

    public void Bind(
        string title,
        string encounter,
        BattleConfig config,
        IReadOnlyList<DeploymentUnitViewModel> pieces,
        IReadOnlyList<EnemyDeploymentViewModel>? enemies,
        int reserveCapacity)
    {
        _title.Text = title;
        _encounter.Text = encounter;
        _config = config;
        _pieces = pieces;
        _enemies = enemies ?? [];
        _reserveCapacity = Math.Max(0, reserveCapacity);
        if (!string.IsNullOrEmpty(_selectedId) && !ContainsPiece(_selectedId)) _selectedId = string.Empty;
        Refresh();
        _status.Text = string.Empty;
        _status.ThemeTypeVariation = "SecondaryLabel";
    }

    public void Bind(RunApplication app, EncounterPlan encounter)
    {
        var run = app.ActiveRun ?? throw new InvalidOperationException("No active run for deployment.");
        var config = app.BuildBattleConfig(encounter, false);
        var pieces = run.Roster.Select(instance =>
        {
            var definition = (UnitDefinition)Required(app, instance.ContentId).Definition;
            var slot = run.Deployment.IndexOf(instance.InstanceId);
            return new DeploymentUnitViewModel(instance.InstanceId, definition.DisplayName, definition.Description,
                instance.HealthRatio, definition.Role, definition.AttackRange, true, slot,
                slot >= 0 ? BattlefieldLayout.PlayerDeploymentCells[slot] : null, definition.Portrait,
                BuildFormationEvaluations(app, instance.InstanceId, config.FloorRule));
        }).ToArray();
        var enemies = config.Spawns.Where(spawn => spawn.Team == 1).Select(spawn =>
        {
            var definition = (UnitDefinition)Required(app, spawn.Unit.ContentId).Definition;
            return new EnemyDeploymentViewModel(spawn.InstanceId, definition.DisplayName, spawn.Cell,
                definition.Role, definition.AttackRange, spawn.Unit.IsBoss, definition.Portrait);
        }).ToArray();
        Bind(encounter.Title, DescribeEncounter(app, encounter), config, pieces, enemies, app.Rules.ReserveCapacity);
    }

    public void ShowMessage(string message, bool error)
    {
        _status.Text = message;
        _status.ThemeTypeVariation = error ? "FeedbackFailure" : "FeedbackSuccess";
    }

    public void ShowCellResult(Vector2I cell, bool success) => _board.FlashCell(cell, success);

    private void Refresh()
    {
        var currentIds = _pieces.Select(piece => piece.InstanceId).ToHashSet(StringComparer.Ordinal);
        foreach (var staleId in _cards.Keys.Where(id => !currentIds.Contains(id)).ToArray())
        {
            var stale = _cards[staleId];
            stale.UnitSelected -= OnPieceSelected;
            stale.Disabled = true;
            stale.FocusMode = FocusModeEnum.None;
            stale.Visible = false;
            stale.QueueFree();
            _cards.Remove(staleId);
        }

        for (var index = 0; index < _pieces.Count; index++)
        {
            var piece = _pieces[index];
            if (!_cards.TryGetValue(piece.InstanceId, out var card))
            {
                card = _cardScene.Instantiate<DeploymentUnitCard>();
                _roster.AddChild(card);
                card.UnitSelected += OnPieceSelected;
                _cards.Add(piece.InstanceId, card);
            }
            card.Bind(piece, piece.InstanceId == _selectedId);
            _roster.MoveChild(card, index);
        }
        if (_config is not null) _board.Bind(_config, _pieces, _selectedId, _enemies);

        var selected = _pieces.FirstOrDefault(piece => piece.InstanceId == _selectedId);
        var reserveCount = _pieces.Count(piece => piece.Cell is null);
        _withdraw.Disabled = selected is null || selected.Cell is null || reserveCount >= _reserveCapacity;
        _withdraw.TooltipText = reserveCount >= _reserveCapacity
                ? $"后备已满（{_reserveCapacity}/{_reserveCapacity}），无法撤回。"
                : "将已部署英雄撤回后备。";
        _start.Disabled = _config is null || _pieces.Any(piece => piece.Cell is { } cell && !_config.FloorRule.CanOccupy(cell));
        _start.TooltipText = _start.Disabled ? "有单位位于当前楼层禁止格，必须先调整阵型。" : "以当前预览阵型进入战斗。";
    }

    private void OnPieceSelected(string pieceId)
    {
        _selectedId = _selectedId == pieceId ? string.Empty : pieceId;
        Refresh();
    }

    private void OnCellSelected(Vector2I cell, string occupantId)
    {
        if (string.IsNullOrEmpty(_selectedId))
        {
            if (!string.IsNullOrEmpty(occupantId))
            {
                _selectedId = occupantId;
                Refresh();
            }
            return;
        }

        if (occupantId == _selectedId)
        {
            _selectedId = string.Empty;
            Refresh();
            return;
        }
        MoveRequested?.Invoke(CreateMove(_selectedId, cell));
    }

    private void OnPieceDropped(string pieceId, Vector2I cell) => MoveRequested?.Invoke(CreateMove(pieceId, cell));
    private void OnWithdraw()
    {
        if (!string.IsNullOrEmpty(_selectedId)) WithdrawRequested?.Invoke(_selectedId);
    }
    private void OnBack()
    {
        _board.ClearDragHover();
        BackRequested?.Invoke();
    }
    private void OnStart() => StartRequested?.Invoke();
    private bool ContainsPiece(string pieceId) => _pieces.Any(piece => piece.InstanceId == pieceId);
    private static FormationMoveCommand CreateMove(string pieceId, Vector2I cell) =>
        FormationMoveCommand.RosterHero(pieceId, cell);

    private static IReadOnlyDictionary<Vector2I, FormationEvaluation> BuildFormationEvaluations(
        RunApplication app,
        string instanceId,
        IBattleFloorRuleRuntime floorRule)
    {
        var result = new Dictionary<Vector2I, FormationEvaluation>();
        for (var y = 0; y < BattlefieldLayout.Height; y++)
        for (var x = 0; x < BattlefieldLayout.PlayerDeploymentColumns; x++)
        {
            var cell = new Vector2I(x, y);
            var command = FormationMoveCommand.RosterHero(instanceId, cell);
            result[cell] = app.EvaluateFormationCommand(command, floorRule);
        }
        return result;
    }

    private static string DescribeEncounter(RunApplication app, EncounterPlan encounter)
    {
        var enemies = encounter.EnemyIds
            .Select(id => ((UnitDefinition)Required(app, id).Definition).DisplayName)
            .GroupBy(name => name)
            .Select(group => group.Count() > 1 ? $"{group.Key}×{group.Count()}" : group.Key);
        var floorName = encounter.FloorRuleId;
        foreach (var scene in app.Content.Catalog.FloorRules)
        {
            var root = scene.Instantiate<FloorRuleContentRoot>();
            try
            {
                if (root.Id == encounter.FloorRuleId) floorName = $"{root.DisplayName}：{root.PreviewText}";
            }
            finally { root.Free(); }
        }
        return $"敌军：{string.Join("、", enemies)}\n楼层规则：{floorName}";
    }

    private static CatalogEntry Required(RunApplication app, string stableId) =>
        app.Content.TryGet(stableId, out var entry)
            ? entry
            : throw new InvalidOperationException("Missing content: " + stableId);
}
