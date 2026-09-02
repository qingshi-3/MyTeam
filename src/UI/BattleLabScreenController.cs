using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Traits;

namespace TowerAutobattler.UI;

public partial class BattleLabScreenController : Control
{
    [Signal] public delegate void BackRequestedEventHandler();
    [Signal] public delegate void StartRequestedEventHandler();
    [Export] public PackedScene LibraryCardScene { get; set; } = null!;
    [Export] public PackedScene BoardCellScene { get; set; } = null!;
    [Export] public BattleLabPresetCatalog PresetCatalog { get; set; } = null!;

    private readonly Dictionary<Vector2I, BattleLabBoardCell> _cells = [];
    private readonly List<BattleLabLibraryCard> _cards = [];
    private BattleLabContentIndex? _content;
    private BattleLabSession? _session;
    private VBoxContainer _playerLibrary = null!;
    private VBoxContainer _enemyLibrary = null!;
    private Control _playerDropZone = null!;
    private Control _enemyDropZone = null!;
    private GridContainer _battlefield = null!;
    private LineEdit _playerSearch = null!;
    private LineEdit _enemySearch = null!;
    private OptionButton _enemyFilter = null!;
    private OptionButton _mode = null!;
    private SpinBox _population = null!;
    private LineEdit _seed = null!;
    private Label _modeBanner = null!;
    private Label _status = null!;
    private Label _facts = null!;
    private Label _inspector = null!;
    private Label _readiness = null!;
    private OptionButton _equipmentChoice = null!;
    private OptionButton _equipmentSlot = null!;
    private Control _equipmentBox = null!;
    private Label _equipmentNotApplicable = null!;
    private OptionButton _relicChoice = null!;
    private OptionButton _existingRelicChoice = null!;
    private SpinBox _relicStacks = null!;
    private OptionButton _presetChoice = null!;
    private LineEdit _presetName = null!;
    private BattleLabPresetStore? _presetStore;
    private readonly List<string> _equipmentIds = [];
    private readonly List<string> _relicIds = [];
    private readonly List<string> _existingRelicIds = [];
    private readonly List<string> _presetNames = [];
    private string _dragContentId = string.Empty;
    private string _dragInstanceId = string.Empty;
    private BattleLabSide _dragSide;
    private bool _dragging;
    private string _selectedPrototype = string.Empty;
    private BattleLabSide _selectedSide;
    private string _selectedInstanceId = string.Empty;

    public BattleLabStartSnapshot? CurrentSnapshot => _session?.Freeze();
    public int CellCount => _cells.Count;
    public string LastFeedback => _status.Text;
    public void ShowFeedback(string message, bool success) => RefreshAll(message, success);

    public override void _Ready()
    {
        _playerLibrary = GetNode<VBoxContainer>("%PlayerLibrary");
        _enemyLibrary = GetNode<VBoxContainer>("%EnemyLibrary");
        _playerDropZone = GetNode<Control>("%PlayerPanel");
        _enemyDropZone = GetNode<Control>("%EnemyPanel");
        _battlefield = GetNode<GridContainer>("%Battlefield");
        _playerSearch = GetNode<LineEdit>("%PlayerSearch");
        _enemySearch = GetNode<LineEdit>("%EnemySearch");
        _enemyFilter = GetNode<OptionButton>("%EnemyFilter");
        _mode = GetNode<OptionButton>("%Mode");
        _population = GetNode<SpinBox>("%Population");
        _seed = GetNode<LineEdit>("%Seed");
        _modeBanner = GetNode<Label>("%ModeBanner");
        _status = GetNode<Label>("%Status");
        _facts = GetNode<Label>("%Facts");
        _inspector = GetNode<Label>("%Inspector");
        _readiness = GetNode<Label>("%Readiness");
        _equipmentChoice = GetNode<OptionButton>("%EquipmentChoice");
        _equipmentSlot = GetNode<OptionButton>("%EquipmentSlot");
        _equipmentBox = GetNode<Control>("%EquipmentBox");
        _equipmentNotApplicable = GetNode<Label>("%EquipmentNotApplicable");
        _relicChoice = GetNode<OptionButton>("%RelicChoice");
        _existingRelicChoice = GetNode<OptionButton>("%ExistingRelicChoice");
        _relicStacks = GetNode<SpinBox>("%RelicStacks");
        _presetChoice = GetNode<OptionButton>("%PresetChoice");
        _presetName = GetNode<LineEdit>("%PresetName");
        _mode.AddItem("正式规则", (int)BattleLabPlacementMode.Formal);
        _mode.AddItem("自由实验", (int)BattleLabPlacementMode.FreeExperiment);
        _enemyFilter.AddItem("全部", 0);
        _enemyFilter.AddItem("普通", (int)BattleLabUnitClassification.PveNormal);
        _enemyFilter.AddItem("精英", (int)BattleLabUnitClassification.PveElite);
        _enemyFilter.AddItem("Boss", (int)BattleLabUnitClassification.PveBoss);
        _enemyFilter.AddItem("召唤", (int)BattleLabUnitClassification.PublishedSummon);
        _playerSearch.TextChanged += OnSearchChanged;
        _enemySearch.TextChanged += OnSearchChanged;
        _enemyFilter.ItemSelected += OnEnemyFilterSelected;
        _mode.ItemSelected += OnModeSelected;
        _population.ValueChanged += OnPopulationChanged;
        GetNode<Button>("%BackButton").Pressed += OnBack;
        GetNode<Button>("%StartButton").Pressed += OnStart;
        GetNode<Button>("%ClearPlayerButton").Pressed += OnClearPlayer;
        GetNode<Button>("%ClearEnemyButton").Pressed += OnClearEnemy;
        GetNode<Button>("%ClearAllButton").Pressed += OnClearAll;
        GetNode<Button>("%DeleteSelectedButton").Pressed += DeleteSelected;
        GetNode<Button>("%ModeToggleButton").Pressed += ToggleMode;
        GetNode<Button>("%EquipButton").Pressed += EquipSelected;
        GetNode<Button>("%RemoveEquipmentButton").Pressed += RemoveSelectedEquipment;
        GetNode<Button>("%SetRelicButton").Pressed += SetTeamRelic;
        GetNode<Button>("%RemoveRelicButton").Pressed += RemoveTeamRelic;
        GetNode<Button>("%SetPrimaryButton").Pressed += SetSelectedPrimary;
        GetNode<Button>("%LoadPresetButton").Pressed += LoadSelectedPreset;
        GetNode<Button>("%SavePresetButton").Pressed += SaveNamedPreset;
        GetNode<Button>("%RestoreDefaultButton").Pressed += RestoreDefaultPreset;
        BuildBoard();
        SetProcessInput(true);
    }

    public void Bind(BattleLabContentIndex content, BattleLabSession session, BattleLabPresetStore? presetStore = null)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _presetStore = presetStore;
        var population = session.CurrentPopulation;
        _population.SetBlockSignals(true);
        _population.MinValue = 1;
        _population.MaxValue = session.Mode == BattleLabPlacementMode.Formal
            ? content.Rules.PhysicalDeploymentCeiling
            : BattlefieldLayout.Width * BattlefieldLayout.Height;
        _population.Value = population;
        _population.SetBlockSignals(false);
        _seed.Text = session.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _mode.Select(session.Mode == BattleLabPlacementMode.Formal ? 0 : 1);
        PopulateBuildChoices();
        PopulatePresetChoices();
        RebuildLibraries();
        RefreshAll("配置已载入。", true);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!_dragging || _session is null) return;
        if (inputEvent is InputEventMouseMotion motion)
        {
            RefreshDropStates(motion.Position);
            return;
        }
        if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release)
            return;
        CompleteDrag(release.Position);
        GetViewport().SetInputAsHandled();
    }

    private void BuildBoard()
    {
        foreach (var child in _battlefield.GetChildren()) child.QueueFree();
        _cells.Clear();
        for (var y = 0; y < BattlefieldLayout.Height; y++)
        for (var x = 0; x < BattlefieldLayout.Width; x++)
        {
            var cell = BoardCellScene.Instantiate<BattleLabBoardCell>();
            var coordinate = new Vector2I(x, y);
            cell.BindCell(coordinate);
            cell.UnitDragRequested += BeginInstanceDrag;
            cell.CellRequested += OnCellRequested;
            _battlefield.AddChild(cell);
            _cells.Add(coordinate, cell);
        }
    }

    private void RebuildLibraries()
    {
        foreach (var card in _cards)
        {
            card.DragRequested -= BeginLibraryDrag;
            card.QueueFree();
        }
        _cards.Clear();
        if (_content is null) return;
        foreach (var unit in _content.PlayerHeroes.Where(MatchesPlayerSearch))
            AddCard(_playerLibrary, unit.StableId, unit.DisplayName, BattleLabSide.Player, "我方英雄");
        foreach (var unit in _content.PveUnits.Where(MatchesEnemySearch))
            AddCard(_enemyLibrary, unit.StableId, unit.DisplayName, BattleLabSide.Enemy,
                DescribeClassification(unit.Classification));
    }

    private void AddCard(Control owner, string id, string name, BattleLabSide side, string classification)
    {
        var card = LibraryCardScene.Instantiate<BattleLabLibraryCard>();
        card.Bind(id, name, side, classification);
        card.DragRequested += BeginLibraryDrag;
        owner.AddChild(card);
        _cards.Add(card);
    }

    private void BeginLibraryDrag(string contentId, BattleLabSide side)
    {
        _dragContentId = contentId;
        _dragInstanceId = string.Empty;
        _dragSide = side;
        _dragging = true;
        _selectedPrototype = contentId;
        _selectedSide = side;
        _status.Text = "拖动中：释放到 ○ 合法格；× 表示不可放置。";
        RefreshDropStates(GetViewport().GetMousePosition());
    }

    private void BeginInstanceDrag(string instanceId)
    {
        if (_session?.TryGet(instanceId, out var unit) != true) return;
        _dragContentId = unit.ContentId;
        _dragInstanceId = instanceId;
        _dragSide = unit.Side;
        _selectedInstanceId = instanceId;
        _dragging = true;
        _selectedPrototype = string.Empty;
        _status.Text = "移动中：可移至空格或与 ⇄ 单位交换；拖回同侧库可召回。";
        RefreshDropStates(GetViewport().GetMousePosition());
    }

    private void CompleteDrag(Vector2 position)
    {
        if (_session is null) { CancelDrag(); return; }
        var target = _cells.FirstOrDefault(pair => pair.Value.GetGlobalRect().HasPoint(position));
        BattleLabPlacementResult? result = null;
        if (target.Value is not null)
            result = string.IsNullOrEmpty(_dragInstanceId)
                ? _session.AddAndPlace(_dragContentId, _dragSide, target.Key)
                : _session.Move(_dragInstanceId, target.Key);
        else if (!string.IsNullOrEmpty(_dragInstanceId) && OriginLibraryRect(_dragSide).HasPoint(position))
        {
            var recalled = _session.Recall(_dragInstanceId);
            if (recalled && _selectedInstanceId == _dragInstanceId) _selectedInstanceId = string.Empty;
            RefreshAll(recalled ? "单位已召回原始库。" : "召回失败：单位实例不存在。", recalled);
            CancelDrag(false);
            return;
        }
        if (result is null)
            RefreshAll("已取消：释放位置不是战场格或原始库。", false);
        else RefreshAll(result.Succeeded ? result.SwappedInstanceId is null ? "放置成功。" : "交换成功。" :
            result.RejectionReason, result.Succeeded);
        CancelDrag(false);
    }

    private void OnCellRequested(Vector2I cell)
    {
        if (_session is null || string.IsNullOrWhiteSpace(_selectedPrototype)) return;
        var result = _session.AddAndPlace(_selectedPrototype, _selectedSide, cell);
        RefreshAll(result.Succeeded ? "放置成功。" : result.RejectionReason, result.Succeeded);
        if (result.Succeeded) _selectedPrototype = string.Empty;
    }

    private void RefreshDropStates(Vector2 mousePosition)
    {
        if (_session is null) return;
        foreach (var (coordinate, cell) in _cells)
        {
            var prototype = new BattleLabUnitConfiguration(
                string.IsNullOrEmpty(_dragInstanceId) ? "lab-preview" : _dragInstanceId,
                _dragContentId, _dragSide, coordinate, []);
            if (!string.IsNullOrEmpty(_dragInstanceId) && _session.TryGet(_dragInstanceId, out var existing))
                prototype = existing;
            var evaluation = BattleLabPlacementPolicy.Evaluate(_session, prototype, coordinate, true);
            cell.ShowDropState(evaluation.Succeeded, evaluation.SwappedInstanceId is not null);
        }
        MouseDefaultCursorShape = _cells.Values.Any(cell => cell.GetGlobalRect().HasPoint(mousePosition))
            ? CursorShape.PointingHand : CursorShape.Forbidden;
    }

    private void RefreshAll(string feedback, bool success)
    {
        if (_session is null) return;
        foreach (var (coordinate, cell) in _cells)
        {
            var unit = _session.At(coordinate);
            cell.Refresh(unit, unit?.InstanceId == _selectedInstanceId);
        }
        var derived = BattleLabDerivedProjectionBuilder.Build(_session);
        var playerCount = derived.PlayerCount;
        var enemyCount = derived.EnemyCount;
        _modeBanner.Text = _session.Mode == BattleLabPlacementMode.FreeExperiment
            ? "⚠ 自由实验配置：双方可使用整个 10×6 战场"
            : "◆ 正式规则：我方左侧 3×6 / 敌方右侧 3×6";
        _facts.Text = $"我方 {playerCount}/{_session.CurrentPopulation}　敌方 {enemyCount}　" +
                      $"装备 {_session.Units.Sum(unit => unit.Equipment.Length)}　遗物 {_session.Relics.Count}　" +
                      $"种子 {_session.Seed}";
        _status.Text = (success ? "✓ " : "! ") + feedback;
        _readiness.Text = derived.IsReady
            ? "✓ 配置可开战"
            : "× 尚不可开战：" + string.Join("；", derived.RejectionReasons);
        var selectedEnemy = !string.IsNullOrWhiteSpace(_selectedInstanceId) &&
                            _session.TryGet(_selectedInstanceId, out var selected) &&
                            selected.Side == BattleLabSide.Enemy;
        _equipmentBox.Visible = !selectedEnemy;
        _equipmentNotApplicable.Visible = selectedEnemy;
        RefreshBuildChoices();
        RefreshInspector(derived);
    }

    private void CancelDrag(bool refresh = true)
    {
        _dragging = false;
        _dragContentId = string.Empty;
        _dragInstanceId = string.Empty;
        MouseDefaultCursorShape = CursorShape.Arrow;
        foreach (var cell in _cells.Values) cell.ClearDropState();
        if (refresh) RefreshAll("拖动已取消。", false);
    }

    private Rect2 OriginLibraryRect(BattleLabSide side) =>
        (side == BattleLabSide.Player ? _playerDropZone : _enemyDropZone).GetGlobalRect();

    private void OnSearchChanged(string _) => RebuildLibraries();
    private void OnEnemyFilterSelected(long _) => RebuildLibraries();
    private bool MatchesPlayerSearch(BattleLabPublishedUnit unit) => Matches(unit, _playerSearch.Text);
    private bool MatchesEnemySearch(BattleLabPublishedUnit unit)
    {
        var selectedId = _enemyFilter.Selected < 0 ? 0 : _enemyFilter.GetItemId(_enemyFilter.Selected);
        var matchesFilter = selectedId == 0 ||
            unit.Classification.HasFlag((BattleLabUnitClassification)selectedId);
        return matchesFilter && Matches(unit, _enemySearch.Text);
    }
    private static bool Matches(BattleLabPublishedUnit unit, string query) =>
        string.IsNullOrWhiteSpace(query) || unit.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        unit.StableId.Contains(query, StringComparison.OrdinalIgnoreCase);

    private void OnModeSelected(long index)
    {
        if (_session is null) return;
        TrySetRules(index == 0 ? BattleLabPlacementMode.Formal : BattleLabPlacementMode.FreeExperiment,
            "放置模式已切换。", "模式切换被拒绝");
    }

    private void ToggleMode()
    {
        if (_session is null) return;
        var next = _session.Mode == BattleLabPlacementMode.Formal
            ? BattleLabPlacementMode.FreeExperiment
            : BattleLabPlacementMode.Formal;
        TrySetRules(next,
            next == BattleLabPlacementMode.FreeExperiment ? "已进入自由实验配置。" : "已恢复正式规则配置。",
            "模式切换被拒绝");
    }

    private void OnPopulationChanged(double value)
    {
        if (_session is null) return;
        try
        {
            _session.SetRules(_session.Mode, (int)value, ParseSeed(), _session.FloorRuleId);
            RefreshAll("当前人口已更新。", true);
        }
        catch (Exception exception)
        {
            SyncRuleControls();
            RefreshAll("人口更新被拒绝：" + exception.Message, false);
        }
    }

    private long ParseSeed() => long.TryParse(
        _seed.Text,
        System.Globalization.NumberStyles.Integer,
        System.Globalization.CultureInfo.InvariantCulture,
        out var value)
        ? value
        : throw new InvalidOperationException("随机种子必须是有效的 64 位整数。");
    private void OnClearPlayer() { _session?.Clear(BattleLabSide.Player); RefreshAll("我方配置已清空。", true); }
    private void OnClearEnemy() { _session?.Clear(BattleLabSide.Enemy); RefreshAll("敌方配置已清空。", true); }
    private void OnClearAll() { _session?.Clear(); RefreshAll("全部单位已清空。", true); }
    private void DeleteSelected()
    {
        if (_session is null || string.IsNullOrWhiteSpace(_selectedInstanceId))
        {
            RefreshAll("请先按住一个已放置单位。", false);
            return;
        }
        var removed = _session.Recall(_selectedInstanceId);
        if (removed) _selectedInstanceId = string.Empty;
        CancelDrag(false);
        RefreshAll(removed ? "单位已删除。" : "删除失败。", removed);
    }
    private void OnBack() => EmitSignal(SignalName.BackRequested);
    private void OnStart()
    {
        if (_session is null) return;
        try { _session.SetRules(_session.Mode, (int)_population.Value, ParseSeed(), _session.FloorRuleId); }
        catch (Exception exception)
        {
            SyncRuleControls();
            RefreshAll("无法开战：" + exception.Message, false);
            return;
        }
        var derived = BattleLabDerivedProjectionBuilder.Build(_session);
        if (!derived.IsReady)
        {
            RefreshAll(string.Join("；", derived.RejectionReasons), false);
            return;
        }
        EmitSignal(SignalName.StartRequested);
    }

    private void TrySetRules(BattleLabPlacementMode mode, string success, string failure)
    {
        if (_session is null || _content is null) return;
        var cap = mode == BattleLabPlacementMode.Formal
            ? _content.Rules.PhysicalDeploymentCeiling
            : BattlefieldLayout.Width * BattlefieldLayout.Height;
        var population = Math.Min((int)_population.Value, cap);
        try
        {
            _session.SetRules(mode, population, ParseSeed(), _session.FloorRuleId);
            SyncRuleControls();
            RefreshAll(success, true);
        }
        catch (Exception exception)
        {
            SyncRuleControls();
            RefreshAll($"{failure}：{exception.Message}", false);
        }
    }

    private void SyncRuleControls()
    {
        if (_session is null || _content is null) return;
        _mode.SetBlockSignals(true);
        _mode.Select(_session.Mode == BattleLabPlacementMode.Formal ? 0 : 1);
        _mode.SetBlockSignals(false);
        _population.SetBlockSignals(true);
        _population.MaxValue = _session.Mode == BattleLabPlacementMode.Formal
            ? _content.Rules.PhysicalDeploymentCeiling
            : BattlefieldLayout.Width * BattlefieldLayout.Height;
        _population.Value = _session.CurrentPopulation;
        _population.SetBlockSignals(false);
        _seed.Text = _session.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public override void _ExitTree()
    {
        CancelDrag(false);
        if (_playerSearch is not null) _playerSearch.TextChanged -= OnSearchChanged;
        if (_enemySearch is not null) _enemySearch.TextChanged -= OnSearchChanged;
        if (_enemyFilter is not null) _enemyFilter.ItemSelected -= OnEnemyFilterSelected;
        if (_mode is not null) _mode.ItemSelected -= OnModeSelected;
        if (_population is not null) _population.ValueChanged -= OnPopulationChanged;
        GetNode<Button>("%BackButton").Pressed -= OnBack;
        GetNode<Button>("%StartButton").Pressed -= OnStart;
        GetNode<Button>("%ClearPlayerButton").Pressed -= OnClearPlayer;
        GetNode<Button>("%ClearEnemyButton").Pressed -= OnClearEnemy;
        GetNode<Button>("%ClearAllButton").Pressed -= OnClearAll;
        GetNode<Button>("%DeleteSelectedButton").Pressed -= DeleteSelected;
        GetNode<Button>("%ModeToggleButton").Pressed -= ToggleMode;
        GetNode<Button>("%EquipButton").Pressed -= EquipSelected;
        GetNode<Button>("%RemoveEquipmentButton").Pressed -= RemoveSelectedEquipment;
        GetNode<Button>("%SetRelicButton").Pressed -= SetTeamRelic;
        GetNode<Button>("%RemoveRelicButton").Pressed -= RemoveTeamRelic;
        GetNode<Button>("%SetPrimaryButton").Pressed -= SetSelectedPrimary;
        GetNode<Button>("%LoadPresetButton").Pressed -= LoadSelectedPreset;
        GetNode<Button>("%SavePresetButton").Pressed -= SaveNamedPreset;
        GetNode<Button>("%RestoreDefaultButton").Pressed -= RestoreDefaultPreset;
        foreach (var card in _cards) card.DragRequested -= BeginLibraryDrag;
        foreach (var cell in _cells.Values)
        {
            cell.UnitDragRequested -= BeginInstanceDrag;
            cell.CellRequested -= OnCellRequested;
        }
        _cards.Clear();
        _cells.Clear();
        _content = null;
        _session = null;
        _presetStore = null;
    }

    private void PopulatePresetChoices(string? preferred = null)
    {
        _presetNames.Clear();
        _presetChoice.Clear();
        foreach (var name in _presetStore?.ListNames() ?? [])
        {
            _presetNames.Add(name);
            _presetChoice.AddItem(name);
        }
        var selected = !string.IsNullOrWhiteSpace(preferred)
            ? _presetNames.IndexOf(preferred)
            : _presetNames.IndexOf(_presetStore?.DefaultPresetName ?? string.Empty);
        if (selected >= 0) _presetChoice.Select(selected);
    }

    private void LoadSelectedPreset()
    {
        if (_session is null || _presetStore is null ||
            _presetChoice.Selected < 0 || _presetChoice.Selected >= _presetNames.Count)
        {
            RefreshAll("没有可载入的预设。", false);
            return;
        }
        var name = _presetNames[_presetChoice.Selected];
        if (!_presetStore.TryLoad(name, out var preset))
        {
            RefreshAll("预设读取失败。", false);
            return;
        }
        try
        {
            _session.Restore(BattleLabPresetStore.ToSnapshot(preset));
            _selectedInstanceId = string.Empty;
            _selectedPrototype = string.Empty;
            SyncRuleControls();
            RefreshAll($"预设“{name}”已载入。", true);
        }
        catch (Exception exception) { RefreshAll("预设被拒绝：" + exception.Message, false); }
    }

    private void SaveNamedPreset()
    {
        if (_session is null || _presetStore is null)
        {
            RefreshAll("预设存储尚未就绪。", false);
            return;
        }
        var name = _presetName.Text.Trim();
        if (!_presetStore.Save(name, _session.Freeze()))
        {
            RefreshAll("预设保存失败：名称无效或文件不可写。", false);
            return;
        }
        PopulatePresetChoices(name);
        RefreshAll($"预设“{name}”已保存到独立实验室空间。", true);
    }

    private void RestoreDefaultPreset()
    {
        if (_presetStore is null)
        {
            RefreshAll("默认预设尚未就绪。", false);
            return;
        }
        var index = _presetNames.IndexOf(_presetStore.DefaultPresetName);
        if (index < 0)
        {
            RefreshAll("默认预设不存在。", false);
            return;
        }
        _presetChoice.Select(index);
        LoadSelectedPreset();
    }

    private void SetSelectedPrimary()
    {
        if (_session is null || string.IsNullOrWhiteSpace(_selectedInstanceId) ||
            !_session.SetPrimaryHero(_selectedInstanceId))
        {
            RefreshAll("只有已放置的我方英雄可设为主英雄。", false);
            return;
        }
        RefreshAll("主英雄已更新；整队 HeroRule 将以该实例为准。", true);
    }

    private void PopulateBuildChoices()
    {
        _equipmentIds.Clear();
        _relicIds.Clear();
        _equipmentChoice.Clear();
        _relicChoice.Clear();
        _equipmentSlot.Clear();
        for (var slot = 0; slot < (_content?.Rules.EquipmentSlotCapacity ?? 0); slot++)
            _equipmentSlot.AddItem($"槽位 {slot + 1}", slot);
        foreach (var item in _content?.Equipment ?? [])
        {
            _equipmentIds.Add(item.StableId);
            _equipmentChoice.AddItem(item.DisplayName);
        }
        foreach (var relic in _content?.Relics ?? [])
        {
            _relicIds.Add(relic.StableId);
            _relicChoice.AddItem(relic.DisplayName);
        }
        _relicStacks.MinValue = 1;
        _relicStacks.MaxValue = int.MaxValue;
        _relicStacks.Value = 1;
    }

    private void RefreshBuildChoices()
    {
        _existingRelicIds.Clear();
        _existingRelicChoice.Clear();
        if (_session is null) return;
        foreach (var relic in _session.Relics.OrderBy(item => item.InstanceId, StringComparer.Ordinal))
        {
            _existingRelicIds.Add(relic.InstanceId);
            var name = _content?.Relics.FirstOrDefault(item => item.StableId == relic.ContentId)?.DisplayName ?? relic.ContentId;
            _existingRelicChoice.AddItem($"{name} ×{relic.Stacks}");
        }
        if (_existingRelicChoice.ItemCount > 0) _existingRelicChoice.Select(0);
    }

    private void RefreshInspector(BattleLabDerivedProjection derived)
    {
        if (_session is null || string.IsNullOrWhiteSpace(_selectedInstanceId) ||
            !_session.TryGet(_selectedInstanceId, out var selected))
        {
            _inspector.Text = "选择一个已放置单位以查看实例、属性、装备与 Trait。";
            return;
        }
        var content = _content?.TryGetUnit(selected.ContentId, out var entry) == true ? entry : null;
        var primary = selected.InstanceId == _session.PrimaryHeroInstanceId ? "　★ 主英雄" : string.Empty;
        var identity = $"{content?.DisplayName ?? selected.ContentId}{primary}\n内容 {selected.ContentId}\n实例 {selected.InstanceId}";
        if (!derived.Units.TryGetValue(selected.InstanceId, out var prepared))
        {
            _inspector.Text = identity + "\n属性尚未准备：请先修复配置错误。";
            return;
        }
        var equipment = selected.Side == BattleLabSide.Enemy ? "装备：不适用（PvE）" :
            selected.Equipment.Length == 0 ? "装备：无" : "装备：" + string.Join("，", selected.Equipment
                .OrderBy(item => item.SlotIndex).Select(item => $"槽{item.SlotIndex + 1} {item.ContentId} / {item.InstanceId}"));
        var contributions = prepared.TraitContributions.Select(contribution =>
            $"{contribution.TraitId} +{contribution.Value}（{DescribeTraitSource(contribution.SourceKind)} · " +
            $"{contribution.SourceInstanceId} · {contribution.ContentIdentity}）").ToArray();
        var contributionText = contributions.Length == 0
            ? "单位贡献：无"
            : "单位贡献：" + string.Join("\n单位贡献：", contributions);
        var traits = derived.Traits.Where(trait => trait.Team == (selected.Side == BattleLabSide.Player ? 0 : 1))
            .Select(trait => trait.Text).ToArray();
        var tierText = traits.Length == 0 ? "团队档位：无 Trait 数据" : "团队档位：" + string.Join("；", traits);
        _inspector.Text = identity +
            $"\n生命 {prepared.Health:0.#}　伤害 {prepared.Damage:0.#}　攻速 {prepared.AttackSpeed:0.##}" +
            $"\n射程 {prepared.Reach:0.##}　控制抗性 {prepared.ControlResistance:P0}" +
            $"\n{equipment}\n{contributionText}\n{tierText}";
    }

    private void EquipSelected()
    {
        if (_session is null || string.IsNullOrWhiteSpace(_selectedInstanceId) ||
            _equipmentChoice.Selected < 0 || _equipmentChoice.Selected >= _equipmentIds.Count)
        {
            RefreshAll("请选择我方英雄与装备。", false);
            return;
        }
        var succeeded = _session.Equip(_selectedInstanceId, _equipmentSlot.Selected, _equipmentIds[_equipmentChoice.Selected]);
        RefreshAll(succeeded ? "装备实例已装入所选英雄。" : "装备失败：仅我方英雄可用且必须遵守三槽规则。", succeeded);
    }

    private void RemoveSelectedEquipment()
    {
        if (_session is null || string.IsNullOrWhiteSpace(_selectedInstanceId)) return;
        var succeeded = _session.RemoveEquipment(_selectedInstanceId, _equipmentSlot.Selected);
        RefreshAll(succeeded ? "所选装备已移除。" : "该槽位没有可移除的装备。", succeeded);
    }

    private void SetTeamRelic()
    {
        if (_session is null || _relicChoice.Selected < 0 || _relicChoice.Selected >= _relicIds.Count) return;
        var succeeded = _session.SetRelic(_relicIds[_relicChoice.Selected], (int)_relicStacks.Value);
        RefreshAll(succeeded ? "玩家团队遗物已更新。" : "遗物层数必须为正且来自发布内容。", succeeded);
    }

    private void RemoveTeamRelic()
    {
        if (_session is null || _existingRelicChoice.Selected < 0 ||
            _existingRelicChoice.Selected >= _existingRelicIds.Count) return;
        var succeeded = _session.RemoveRelic(_existingRelicIds[_existingRelicChoice.Selected]);
        RefreshAll(succeeded ? "玩家团队遗物已移除。" : "遗物实例不存在。", succeeded);
    }

    private static string DescribeClassification(BattleLabUnitClassification classification)
    {
        var labels = new List<string>();
        if (classification.HasFlag(BattleLabUnitClassification.PveNormal)) labels.Add("普通");
        if (classification.HasFlag(BattleLabUnitClassification.PveElite)) labels.Add("精英");
        if (classification.HasFlag(BattleLabUnitClassification.PveBoss)) labels.Add("Boss");
        if (classification.HasFlag(BattleLabUnitClassification.PublishedSummon)) labels.Add("召唤");
        return labels.Count == 0 ? "PvE" : string.Join(" + ", labels);
    }

    private static string DescribeTraitSource(TraitContributionSourceKind sourceKind) => sourceKind switch
    {
        TraitContributionSourceKind.Hero => "英雄",
        TraitContributionSourceKind.Equipment => "装备",
        TraitContributionSourceKind.ExplicitExtra => "额外",
        _ => sourceKind.ToString()
    };
}
