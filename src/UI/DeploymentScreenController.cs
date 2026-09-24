using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
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
    private HBoxContainer _roster = null!;
    private Label _reserveCount = null!;
    private Label _emptyReserve = null!;
    private DeploymentBoard _board = null!;
    private PanelContainer _bench = null!;
    private Button _back = null!;
    private Button _start = null!;
    private PackedScene _cardScene = null!;
    private readonly Dictionary<string, DeploymentUnitCard> _cards = new(StringComparer.Ordinal);
    private IReadOnlyList<DeploymentUnitViewModel> _pieces = [];
    private IReadOnlyList<EnemyDeploymentViewModel> _enemies = [];
    private BattleConfig? _config;
    private string _selectedId = string.Empty;
    private int _reserveCapacity;
    private RosterLoadoutView _equipmentPanel = null!;
    private RunApplication? _application;
    private EncounterPlan? _encounterPlan;
    private bool _equipmentEditing = true;
    private bool _showingEquipmentHover;
    private PreparedUnitDetailPanel _details = null!;
    private OptionButton _unitPicker = null!;
    private ContextPopup _unitPopup = null!;
    private ContextPopup _equipmentPopup = null!;
    private ContextPopup _encounterPopup = null!;
    private Button _unitButton = null!;
    private Button _equipmentButton = null!;
    private Button _encounterButton = null!;
    private string _inspectedId = string.Empty;
    private readonly List<string> _pickerIds = [];
    private readonly Dictionary<string, PreparedUnitDetails> _prepared = new(StringComparer.Ordinal);

    public string SelectedPieceId => _selectedId;
    public IBattleFloorRuleRuntime? FloorRule => _config?.FloorRule;

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _encounter = GetNode<Label>("%EncounterInfo");
        _status = GetNode<Label>("%Status");
        _roster = GetNode<HBoxContainer>("%RosterChoices");
        _reserveCount = GetNode<Label>("%ReserveCount");
        _emptyReserve = GetNode<Label>("%EmptyReserve");
        _board = GetNode<DeploymentBoard>("%DeploymentBoard");
        _bench = GetNode<PanelContainer>("%ReserveBench");
        _back = GetNode<Button>("%BackButton");
        _start = GetNode<Button>("%StartBattleButton");
        _cardScene = GD.Load<PackedScene>("res://scenes/ui/components/DeploymentUnitCard.tscn");
        _equipmentPanel = GetNode<RosterLoadoutView>("%EquipmentLoadoutPanel");
        _details = GetNode<PreparedUnitDetailPanel>("%PreparedUnitDetailPanel");
        _unitPicker = GetNode<OptionButton>("%InspectedUnit");
        _unitPopup = GetNode<ContextPopup>("%UnitDetailsPopup");
        _equipmentPopup = GetNode<ContextPopup>("%EquipmentPopup");
        _encounterPopup = GetNode<ContextPopup>("%EncounterPopup");
        _unitButton = GetNode<Button>("%UnitDetailsButton");
        _equipmentButton = GetNode<Button>("%EquipmentButton");
        _encounterButton = GetNode<Button>("%EncounterButton");
        _unitButton.Pressed += ToggleUnitDetails;
        _equipmentButton.Pressed += ToggleEquipment;
        _encounterButton.Pressed += ToggleEncounter;
        _unitPicker.ItemSelected += OnUnitPicked;
        _equipmentPanel.EquipmentChanged += OnEquipmentChanged;
        _equipmentPanel.HeroSelected += OnEquipmentHeroSelected;
        _board.CellSelected += OnCellSelected;
        _board.EnemySelected += OnEnemySelected;
        _board.PieceDropped += OnPieceDropped;
        _board.EquipmentDropEvaluator = EvaluateEquipmentDrop;
        _board.EquipmentReceiver = ReceiveEquipment;
        _bench.SetDragForwarding(Callable.From<Vector2, Variant>(_ => default),
            Callable.From<Vector2, Variant, bool>((_, data) => CanWithdrawDrag(data)),
            Callable.From<Vector2, Variant>((_, data) => ReceiveWithdrawDrag(data)));
        _back.Pressed += OnBack;
        _start.Pressed += OnStart;
    }

    public override void _ExitTree()
    {
        _equipmentPanel.EquipmentChanged -= OnEquipmentChanged;
        _unitPicker.ItemSelected -= OnUnitPicked;
        _board.EnemySelected -= OnEnemySelected;
        _equipmentPanel.HeroSelected -= OnEquipmentHeroSelected;
        foreach (var card in _cards.Values)
            if (IsInstanceValid(card)) card.UnitSelected -= OnPieceSelected;
        _cards.Clear();
        _board.CellSelected -= OnCellSelected;
        _board.PieceDropped -= OnPieceDropped;
        _board.EquipmentDropEvaluator = null;
        _board.EquipmentReceiver = null;
        _back.Pressed -= OnBack;
        _start.Pressed -= OnStart;
        _unitButton.Pressed -= ToggleUnitDetails;
        _equipmentButton.Pressed -= ToggleEquipment;
        _encounterButton.Pressed -= ToggleEncounter;
    }

    public void Bind(
        string title,
        string encounter,
        BattleConfig config,
        IReadOnlyList<DeploymentUnitViewModel> pieces,
        IReadOnlyList<EnemyDeploymentViewModel>? enemies,
        int reserveCapacity)
    {
        _application = null;
        _encounterPlan = null;
        _equipmentPanel.Visible = false;
        _equipmentButton.Disabled = true;
        _title.Text = title;
        _encounter.Text = encounter;
        _config = config;
        _pieces = pieces;
        _enemies = enemies ?? [];
        _prepared.Clear();
        using (var preview = new BattleSimulation(config))
            foreach (var unit in preview.Units)
                _prepared[unit.SourceInstanceId] = new PreparedUnitDetails(unit.Definition,
                    Enum.GetValues<CombatAttribute>().ToDictionary(attribute => attribute,
                        attribute => unit.Attributes.GetValue(attribute)), unit.Health, unit.Shield);
        _reserveCapacity = Math.Max(0, reserveCapacity);
        if (!string.IsNullOrEmpty(_selectedId) && !ContainsPiece(_selectedId)) _selectedId = string.Empty;
        Refresh();
        _status.Text = string.Empty;
        _status.ThemeTypeVariation = "SecondaryLabel";
    }

    public void Bind(RunApplication app, EncounterPlan encounter, bool equipmentEditing = true)
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
                BuildFormationEvaluations(app, instance.InstanceId, config.FloorRule), definition.BodyRadius);
        }).ToArray();
        var enemies = config.Spawns.Where(spawn => spawn.Team == 1).Select(spawn =>
        {
            var definition = (UnitDefinition)Required(app, spawn.Unit.ContentId).Definition;
            return new EnemyDeploymentViewModel(spawn.InstanceId, definition.DisplayName, spawn.Cell,
                definition.Role, definition.AttackRange, spawn.Unit.IsBoss, definition.Portrait, spawn.Unit.BodyRadius);
        }).ToArray();
        Bind(encounter.Title, DescribeEncounter(app, encounter), config, pieces, enemies, app.Rules.ReserveCapacity);
        _application = app;
        _encounterPlan = encounter;
        _equipmentEditing = equipmentEditing;
        _equipmentPanel.Bind(app, _selectedId, id => _prepared.GetValueOrDefault(id));
        _equipmentPanel.Visible = equipmentEditing;
        _equipmentButton.Disabled = !equipmentEditing;
        if (!equipmentEditing) _equipmentPopup.Close();
        RefreshEquipmentCards();
        RefreshDetails();
    }

    public void ShowMessage(string message, bool error)
    {
        _status.Text = message;
        _status.ThemeTypeVariation = error ? "FeedbackFailure" : "FeedbackSuccess";
    }

    public override void _Notification(int what)
    {
        if (what != NotificationDragEnd || !_showingEquipmentHover) return;
        _showingEquipmentHover = false;
        ShowMessage("拖放已取消，装备保持原位。", false);
    }

    public override void _Input(InputEvent input)
    {
        if (!IsVisibleInTree() || input is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }
            || !GetViewport().GuiIsDragging()) return;
        GetViewport().GuiCancelDrag();
        _board.ClearDragHover();
        GetViewport().SetInputAsHandled();
    }

    public void ShowCellResult(Vector2I cell, bool success) => _board.FlashCell(cell, success);
    public void RefreshEquipment()
    {
        if (IsVisibleInTree()) OnEquipmentChanged();
    }

    private void Refresh()
    {
        var reserves = _pieces.Where(piece => piece.Cell is null).ToArray();
        var currentIds = reserves.Select(piece => piece.InstanceId).ToHashSet(StringComparer.Ordinal);
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

        for (var index = 0; index < reserves.Length; index++)
        {
            var piece = reserves[index];
            if (!_cards.TryGetValue(piece.InstanceId, out var card))
            {
                card = _cardScene.Instantiate<DeploymentUnitCard>();
                _roster.AddChild(card);
                card.UnitSelected += OnPieceSelected;
                card.EquipmentDropEvaluator = data => EvaluateEquipmentDrop(card.InstanceId, data);
                card.EquipmentReceiver = data => ReceiveEquipment(card.InstanceId, data);
                card.RosterDropEvaluator = CanWithdrawDrag;
                card.RosterReceiver = ReceiveWithdrawDrag;
                _cards.Add(piece.InstanceId, card);
            }
            card.Bind(piece, piece.InstanceId == _selectedId);
            _roster.MoveChild(card, index);
        }
        if (_config is not null) _board.Bind(_config, _pieces, _selectedId, _enemies);

        var reserveCount = reserves.Length;
        _reserveCount.Text = $"{reserveCount} / {_reserveCapacity}";
        _emptyReserve.Visible = reserveCount == 0;
        _bench.TooltipText = reserveCount >= _reserveCapacity
                ? $"后备已满（{_reserveCapacity}/{_reserveCapacity}），无法撤回。"
                : "将场上英雄拖到这里撤回后备，点击不会改变阵型。";
        _start.Disabled = _config is null || _pieces.Any(piece => piece.Cell is { } cell && !_config.FloorRule.CanOccupy(cell));
        _start.TooltipText = _start.Disabled ? "有单位位于当前楼层禁止格，必须先调整阵型。" : "以当前预览阵型进入战斗。";
        if (_application is not null && _equipmentEditing)
            _equipmentPanel.Bind(_application, _selectedId, id => _prepared.GetValueOrDefault(id));
        RefreshDetails();
    }

    private void OnPieceSelected(string pieceId)
    {
        _inspectedId = pieceId;
        _selectedId = _selectedId == pieceId ? string.Empty : pieceId;
        Refresh();
    }

    private void OnEquipmentHeroSelected(string heroId)
    {
        _selectedId = heroId;
        _inspectedId = heroId;
        Refresh();
    }

    private void OnEquipmentChanged()
    {
        if (_application is { } app && _encounterPlan is { } encounter) Bind(app, encounter, _equipmentEditing);
    }

    private EquipmentDropEvaluation EvaluateEquipmentDrop(string heroId, Variant data)
    {
        var result = RunEquipmentDropRules.Evaluate(_application, heroId, data, _equipmentEditing);
        _showingEquipmentHover = result.Allowed;
        ShowMessage(result.Reason, !result.Allowed);
        return result;
    }

    private void ReceiveEquipment(string heroId, Variant data)
    {
        var result = EvaluateEquipmentDrop(heroId, data);
        _showingEquipmentHover = false;
        if (!result.Allowed || _application is null) return;
        if (!_application.EquipOwnedItem(result.ItemId, heroId, result.SlotIndex))
        {
            ShowMessage("未能保存换装，装备保持原位。", true);
            return;
        }
        _selectedId = heroId;
        _inspectedId = heroId;
        OnEquipmentChanged();
        ShowMessage($"已装入第 {result.SlotIndex + 1} 个装备槽。", false);
        if (_pieces.FirstOrDefault(piece => piece.InstanceId == heroId)?.Cell is { } cell)
            _board.FlashCell(cell, true);
    }

    private void RefreshEquipmentCards()
    {
        if (_application?.ActiveRun is not { } run) return;
        foreach (var hero in run.Roster)
            if (_cards.TryGetValue(hero.InstanceId, out var card))
                card.BindEquipment(hero.Equipment.Select(item =>
                    (item.SlotIndex, (ItemDefinition)Required(_application, item.ContentId).Definition)).ToArray());
    }

    private void OnCellSelected(Vector2I cell, string occupantId)
    {
        // Inspection never carries a pending placement command. Only a drop
        // supplies both the moving identity and its destination.
        _selectedId = occupantId == _selectedId ? string.Empty : occupantId;
        if (!string.IsNullOrEmpty(occupantId)) _inspectedId = occupantId;
        Refresh();
    }

    private void OnEnemySelected(string instanceId)
    {
        _inspectedId = instanceId;
        _selectedId = string.Empty;
        Refresh();
    }

    private void OnUnitPicked(long index)
    {
        if (index < 0 || index >= _pickerIds.Count) return;
        _inspectedId = _pickerIds[(int)index];
        _selectedId = ContainsPiece(_inspectedId) ? _inspectedId : string.Empty;
        Refresh();
    }

    private void RefreshDetails()
    {
        _pickerIds.Clear();
        _unitPicker.Clear();
        foreach (var piece in _pieces)
        {
            _pickerIds.Add(piece.InstanceId);
            _unitPicker.AddItem($"我方 · {piece.DisplayName}" + (piece.Cell is null ? " · 后备" : ""));
        }
        foreach (var enemy in _enemies)
        {
            _pickerIds.Add(enemy.InstanceId);
            _unitPicker.AddItem($"敌方 · {enemy.DisplayName}");
        }
        if (!_pickerIds.Contains(_inspectedId)) _inspectedId = _pickerIds.FirstOrDefault() ?? "";
        _unitPicker.Select(_pickerIds.IndexOf(_inspectedId));
        _unitButton.Disabled = _pickerIds.Count == 0;
        var pieceModel = _pieces.FirstOrDefault(piece => piece.InstanceId == _inspectedId);
        _prepared.TryGetValue(_inspectedId, out var prepared);
        var snapshot = prepared?.Snapshot;
        UnitDefinition? definition = null;
        if (_application is { } app)
        {
            var contentId = snapshot?.ContentId ?? app.ActiveRun?.Roster
                .FirstOrDefault(hero => hero.InstanceId == _inspectedId)?.ContentId;
            if (contentId is not null)
            {
                var entry = Required(app, contentId);
                definition = (UnitDefinition)entry.Definition;
                snapshot ??= BattleSetupFactory.Snapshot(entry, app.Content);
            }
        }
        _details.Visible = definition is not null && snapshot is not null;
        if (definition is null || snapshot is null) return;
        var context = pieceModel is null ? "敌方 · 开战配置" :
            pieceModel.Cell is null ? "后备 · 基础属性（部署后计入战斗加成）" : "我方 · 开战配置（含装备与开场加成）";
        _details.Bind(_inspectedId, definition, snapshot, context, prepared, pieceModel?.HealthRatio ?? 1);
    }

    private void OnPieceDropped(string pieceId, Vector2I cell) => MoveRequested?.Invoke(CreateMove(pieceId, cell));
    private bool CanWithdrawDrag(Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary) return false;
        var values = data.AsGodotDictionary();
        if (!values.ContainsKey("piece_id")) return false;
        var id = values["piece_id"].AsString();
        return _pieces.Any(piece => piece.InstanceId == id && piece.Cell is not null)
            && _pieces.Count(piece => piece.Cell is null) < _reserveCapacity;
    }
    private void ReceiveWithdrawDrag(Variant data)
    {
        if (CanWithdrawDrag(data)) WithdrawRequested?.Invoke(data.AsGodotDictionary()["piece_id"].AsString());
    }
    private void OnBack()
    {
        _board.ClearDragHover();
        ClosePopups();
        BackRequested?.Invoke();
    }
    private void OnStart()
    {
        ClosePopups();
        StartRequested?.Invoke();
    }

    private void ToggleUnitDetails() => TogglePopup(_unitPopup, _unitButton);
    private void ToggleEquipment() => TogglePopup(_equipmentPopup, _equipmentButton);
    private void ToggleEncounter() => TogglePopup(_encounterPopup, _encounterButton);

    private static void TogglePopup(ContextPopup popup, Control opener)
    {
        if (popup.IsOpen) popup.Close();
        else popup.Open(opener);
    }

    private void ClosePopups()
    {
        _unitPopup.Close();
        _equipmentPopup.Close();
        _encounterPopup.Close();
    }
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
