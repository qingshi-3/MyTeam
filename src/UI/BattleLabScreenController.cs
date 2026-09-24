using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using TowerAutobattler.Traits;

namespace TowerAutobattler.UI;

public partial class BattleLabScreenController : Control
{
    [Signal] public delegate void BackRequestedEventHandler();
    [Signal] public delegate void StartRequestedEventHandler();
    [Export] public PackedScene LibraryCardScene { get; set; } = null!;
    [Export] public PackedScene BoardCellScene { get; set; } = null!;
    [Export] public BattleLabPresetCatalog PresetCatalog { get; set; } = null!;
    [Export] public Vector2 InformationWindowSize { get; set; } = new(660, 700);

    private sealed record EditHistory(BattleLabStartSnapshot Snapshot, string Label, string PresetName, string PresetDigest);
    private readonly Dictionary<Vector2I, BattleLabBoardCell> _cells = [];
    private readonly List<BattleLabLibraryCard> _cards = [];
    private readonly List<(Button Button, Action Handler)> _buttonBindings = [];
    private readonly Dictionary<string, Button> _buttons = [];
    private readonly Dictionary<string, UnitSnapshot> _unitSnapshots = new(StringComparer.Ordinal);
    private readonly List<EditHistory> _history = [];
    private string _inspectedEquipmentId = string.Empty;
    private readonly List<string> _relicIds = [], _existingRelicIds = [];
    private BattleLabContentIndex? _content;
    private BattleLabSession? _session;
    private BattleLabPresetStore? _presetStore;
    private BattleLabDerivedProjection? _derived;
    private string _derivedDigest = string.Empty, _activePresetName = string.Empty, _presetDigest = string.Empty;
    private VBoxContainer _unitLibrary = null!;
    private Control _equipmentBox = null!;
    private BattleLabPresetPanel _presetPanel = null!;
    private Control _boardCenter = null!, _libraryPane = null!, _detailPane = null!;
    private ContextPopup _libraryPopup = null!, _detailsPopup = null!;
    private UnitAnimationAudioPreview _audioPreview = null!;
    private Label _detailPopupTitle = null!, _equipmentOwner = null!;
    private Vector4 _equipmentToolOffsets;
    private GridContainer _battlefield = null!;
    private DeploymentFootprintLayer _footprints = null!;
    private TabContainer _detailTabs = null!;
    private LineEdit _unitSearch = null!, _seed = null!;
    private OptionButton _unitFilter = null!, _placementTeam = null!;
    private OptionButton _relicChoice = null!, _existingRelicChoice = null!;
    private SpinBox _relicStacks = null!;
    private Label _libraryEmpty = null!, _modeBanner = null!, _status = null!, _facts = null!;
    private Label _placementHint = null!, _unitTitle = null!, _inspectorDetails = null!, _teamFacts = null!;
    private Label _readiness = null!, _equipmentNotApplicable = null!;
    private CombatRichText _inspector = null!;
    private CombatRichText _inspectorStats = null!, _inspectorGlossary = null!;
    private StatBlock _healthStat = null!, _attackStat = null!, _armorStat = null!, _speedStat = null!;
    private Control _statsPanel = null!;
    private BattleLabEquipmentPanel _equipmentDragPanel = null!;
    private string _equipmentDragContext = Guid.NewGuid().ToString("N");
    private CombatRichText _equipmentRules = null!, _relicRules = null!;
    private CheckButton _retentionUpgrade = null!;
    private string _dragContentId = string.Empty, _dragInstanceId = string.Empty;
    private BattleLabSide _dragSide;
    private bool _dragging;
    private bool _libraryCollapsed = true, _detailsCollapsed = true;
    private string _selectedPrototype = string.Empty, _selectedInstanceId = string.Empty;
    private BattleLabSide _selectedSide;

    public BattleLabStartSnapshot? CurrentSnapshot => _session?.Freeze();
    public int CellCount => _cells.Count;
    public string LastFeedback => _status.Text;
    public void ShowFeedback(string message, bool success) => RefreshAll(message, success);

    public override void _Ready()
    {
        _unitLibrary = GetNode<VBoxContainer>("%UnitLibrary");
        _placementTeam = GetNode<OptionButton>("%PlacementTeam");
        _battlefield = GetNode<GridContainer>("%Battlefield");
        _footprints = GetNode<DeploymentFootprintLayer>("%DeploymentFootprints");
        _boardCenter = GetNode<Control>("%BoardCenter");
        _libraryPane = GetNode<Control>("%LibraryPane");
        _detailPane = GetNode<Control>("%DetailPane");
        _equipmentToolOffsets = new Vector4(_detailPane.OffsetLeft, _detailPane.OffsetTop,
            _detailPane.OffsetRight, _detailPane.OffsetBottom);
        _libraryPopup = GetNode<ContextPopup>("%LibraryPopup");
        _detailsPopup = GetNode<ContextPopup>("%DetailsPopup");
        _audioPreview = GetNode<UnitAnimationAudioPreview>("%AudioPreviewPopup");
        _detailPopupTitle = GetNode<Label>("%DetailPopupTitle");
        _equipmentOwner = GetNode<Label>("%EquipmentOwner");
        _libraryPopup.Closed += OnLibraryClosed;
        _detailsPopup.Closed += OnDetailsClosed;
        _detailTabs = GetNode<TabContainer>("%DetailTabs");
        _unitSearch = GetNode<LineEdit>("%UnitSearch");
        _libraryEmpty = GetNode<Label>("%LibraryEmpty");
        _unitFilter = GetNode<OptionButton>("%UnitFilter");
        _seed = GetNode<LineEdit>("%Seed");
        _modeBanner = GetNode<Label>("%ModeBanner");
        _status = GetNode<Label>("%Status");
        _facts = GetNode<Label>("%Facts");
        _placementHint = GetNode<Label>("%PlacementHint");
        _unitTitle = GetNode<Label>("%UnitTitle");
        _inspector = GetNode<CombatRichText>("%Inspector");
        _inspectorStats = GetNode<CombatRichText>("%InspectorStats");
        _inspectorGlossary = GetNode<CombatRichText>("%InspectorGlossary");
        _statsPanel = GetNode<Control>("%StatsPanel");
        _healthStat = GetNode<StatBlock>("%LabHealth");
        _attackStat = GetNode<StatBlock>("%LabAttack");
        _armorStat = GetNode<StatBlock>("%LabArmor");
        _speedStat = GetNode<StatBlock>("%LabSpeed");
        _inspectorDetails = GetNode<Label>("%InspectorDetails");
        _teamFacts = GetNode<Label>("%TeamFacts");
        _retentionUpgrade = GetNode<CheckButton>("%RetentionUpgrade");
        _readiness = GetNode<Label>("%Readiness");
        _equipmentDragPanel = GetNode<BattleLabEquipmentPanel>("%EquipmentDragPanel");
        _equipmentDragPanel.EquipRequested += EquipDragged;
        _equipmentDragPanel.RemoveRequested += RemoveDragged;
        _equipmentDragPanel.InspectRequested += InspectEquipment;
        _equipmentBox = GetNode<Control>("%EquipmentBox");
        _equipmentNotApplicable = GetNode<Label>("%EquipmentNotApplicable");
        _equipmentRules = GetNode<CombatRichText>("%EquipmentRules");
        _relicRules = GetNode<CombatRichText>("%RelicRules");
        _relicChoice = GetNode<OptionButton>("%RelicChoice");
        _existingRelicChoice = GetNode<OptionButton>("%ExistingRelicChoice");
        _relicStacks = GetNode<SpinBox>("%RelicStacks");
        _presetPanel = GetNode<BattleLabPresetPanel>("%PresetPanel");
        _presetPanel.LoadRequested += LoadPreset;
        _presetPanel.SaveRequested += SavePreset;
        _detailTabs.SetTabTitle(0, "单位"); _detailTabs.SetTabTitle(1, "团队"); _detailTabs.SetTabTitle(2, "设置"); _detailTabs.SetTabTitle(3, "装备");
        _placementTeam.AddItem("放入 A 队", (int)BattleLabSide.Player);
        _placementTeam.AddItem("放入 B 队", (int)BattleLabSide.Enemy);
        _placementTeam.ItemSelected += OnPlacementTeamChanged;
        _unitFilter.AddItem("全部单位", 0);
        _unitFilter.AddItem("英雄", (int)BattleLabUnitClassification.PlayerHero);
        _unitFilter.AddItem("基础 / 普通", (int)BattleLabUnitClassification.PveNormal);
        _unitFilter.AddItem("精英", (int)BattleLabUnitClassification.PveElite);
        _unitFilter.AddItem("首领", (int)BattleLabUnitClassification.PveBoss);
        _unitFilter.AddItem("召唤物", (int)BattleLabUnitClassification.PublishedSummon);
        _unitFilter.AddItem("测试假人", (int)BattleLabUnitClassification.TestDummy);
        _unitFilter.Select(0);
        _unitSearch.TextChanged += OnSearchChanged;
        _unitFilter.ItemSelected += OnUnitFilterSelected;
        _retentionUpgrade.Toggled += OnRetentionUpgrade;
        _seed.TextSubmitted += OnSeedSubmitted;
        _relicChoice.ItemSelected += OnBuildChoiceSelected;
        _existingRelicChoice.ItemSelected += OnBuildChoiceSelected;
        _boardCenter.Resized += ResizeBoard; Resized += ResizeLayout;
        BindButton("%BackButton", OnBack); BindButton("%StartButton", OnStart);
        BindButton("%ClearPlayerButton", OnClearPlayer); BindButton("%ClearEnemyButton", OnClearEnemy);
        BindButton("%ClearAllButton", OnClearAll); BindButton("%DeleteSelectedButton", DeleteSelected);
        BindButton("%CancelSelectionButton", ClearSelection); BindButton("%UndoButton", UndoLastEdit);
        BindButton("%SetRelicButton", SetTeamRelic); BindButton("%RemoveRelicButton", RemoveTeamRelic);
        BindButton("%OpenPresetsButton", OpenPresets);
        BindButton("%SeedApplyButton", ApplySeed);
        BindButton("%ToggleLibraryButton", () => SetLibraryCollapsed(!_libraryCollapsed));
        BindButton("%UnitDetailsButton", () => ToggleDetailsPage(0));
        BindButton("%AudioPreviewButton", OpenAudioPreview);
        BindButton("%TeamDetailsButton", () => ToggleDetailsPage(1));
        BindButton("%SettingsDetailsButton", () => ToggleDetailsPage(2));
        BindButton("%EquipmentDetailsButton", () => ToggleDetailsPage(3));
        _detailTabs.TabChanged += OnDetailTabChanged;
        RefreshSidebarState();
        _presetPanel.Visible = false;
        BuildBoard(); ResizeLayout(); SetProcessInput(true);
    }

    public void Bind(BattleLabContentIndex content, BattleLabSession session, BattleLabPresetStore? presetStore = null)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _equipmentDragContext = Guid.NewGuid().ToString("N");
        _presetStore = presetStore; _history.Clear(); _unitSnapshots.Clear(); _derived = null;
        _derivedDigest = string.Empty; _presetDigest = session.Freeze().CanonicalDigest;
        _activePresetName = presetStore?.BuiltIns.FirstOrDefault(entry =>
            BattleLabPresetStore.ToSnapshot(entry.Value).CanonicalDigest == _presetDigest).Key ?? string.Empty;
        _selectedPrototype = string.Empty; _selectedInstanceId = string.Empty;
        _presetPanel.Bind(presetStore, UnitName);
        SyncRuleControls(); PopulateBuildChoices(); RebuildLibraries();
        RefreshAll("", true);
    }

    private void OpenAudioPreview()
    {
        var selected = _session?.TryGet(_selectedInstanceId, out var instance) == true ? instance : null;
        var contentId = selected?.ContentId ?? _selectedPrototype;
        if (string.IsNullOrEmpty(contentId) || _content?.TryGetUnit(contentId, out var content) != true)
        {
            _status.Text = "先选择场上单位，或在单位库中选择一个原型，再预览动画音效。";
            return;
        }
        _audioPreview.ShowUnit(content.Entry.Scene, content.DisplayName, _buttons["%AudioPreviewButton"]);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (_audioPreview.IsOpen) return;
        if (_session is null || !IsVisibleInTree()) return;
        if (inputEvent is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (_presetPanel.IsOpen) return;
            if (key.Keycode == Key.Escape)
            {
                if (_dragging) { ResetDrag(); RefreshAll("", true); }
                else if (_detailsPopup.IsOpen) _detailsPopup.Close();
                else if (_libraryPopup.IsOpen) _libraryPopup.Close();
                else ClearSelection();
                GetViewport().SetInputAsHandled(); return;
            }
            // Information windows keep editing shortcuts out of the underlying formation.
            if (_detailsPopup.IsOpen && _detailsPopup.Blocking) return;
            if (GetViewport().GuiGetFocusOwner() is not (LineEdit or TextEdit))
            {
                if (key.Keycode == Key.F1)
                { SetLibraryCollapsed(!_libraryCollapsed); GetViewport().SetInputAsHandled(); return; }
                if (key.Keycode == Key.F2)
                { SetDetailsCollapsed(!_detailsCollapsed); GetViewport().SetInputAsHandled(); return; }
                if (key.Keycode == Key.Delete && !_detailsPopup.IsOpen && !string.IsNullOrEmpty(_selectedInstanceId) &&
                    GetViewport().GuiGetFocusOwner() is not EquipmentSlotButton)
                { DeleteSelected(); GetViewport().SetInputAsHandled(); return; }
                if (key.Keycode == Key.Z && (key.CtrlPressed || key.MetaPressed))
                { UndoLastEdit(); GetViewport().SetInputAsHandled(); return; }
            }
        }
        if (!_dragging || _presetPanel.Visible) return;
        if (inputEvent is InputEventMouseMotion motion) RefreshDropStates(motion.Position);
        else if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release)
        { CompleteDrag(release.Position); GetViewport().SetInputAsHandled(); }
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
            cell.BindCell(coordinate); cell.UnitSelected += SelectInstance;
            cell.UnitDragRequested += BeginInstanceDrag;
            cell.EquipmentDropAllowed = data => CanEquipOnUnit(cell.InstanceId, data);
            cell.EquipmentDropReceived = data =>
            {
                if (!CanEquipOnUnit(cell.InstanceId, data) || _session is null ||
                    !_session.TryGet(cell.InstanceId, out var target) || !EquipmentSlotButton.TryEquipmentId(data, out var id)) return;
                var slot = Enumerable.Range(0, _session.Content.Rules.EquipmentSlotCapacity)
                    .First(index => target.Equipment.All(item => item.SlotIndex != index));
                SelectInstance(target.InstanceId); EquipDragged(id, slot);
            };
            _battlefield.AddChild(cell); _cells.Add(coordinate, cell);
        }
    }
    private void ResizeLayout()
    {
        if (!IsInstanceValid(_libraryPane)) return;
        // Overlay dimensions do not participate in the board layout.
        _libraryPane.OffsetTop = Size.Y < 780 ? 116 : 142;
        _libraryPane.OffsetBottom = Size.Y < 780 ? -78 : -94;
        LayoutDetailsWindow();
        ResizeBoard();
    }

    private void LayoutDetailsWindow()
    {
        if (_detailTabs.CurrentTab == 3)
        {
            // Equipment stays beside the board so the drag destination remains reachable.
            _detailPane.AnchorLeft = _detailPane.AnchorRight = 1;
            _detailPane.AnchorTop = 0; _detailPane.AnchorBottom = 1;
            _detailPane.OffsetLeft = _equipmentToolOffsets.X;
            _detailPane.OffsetRight = _equipmentToolOffsets.Z;
            _detailPane.OffsetTop = Size.Y < 780 ? 116 : _equipmentToolOffsets.Y;
            _detailPane.OffsetBottom = Size.Y < 780 ? -78 : _equipmentToolOffsets.W;
            return;
        }
        // Read-only information and low-frequency configuration use a centered dialog.
        var extent = new Vector2(Mathf.Min(InformationWindowSize.X, Size.X - 64),
            Mathf.Min(InformationWindowSize.Y, Size.Y - 96));
        _detailPane.AnchorLeft = _detailPane.AnchorRight = 0.5f;
        _detailPane.AnchorTop = _detailPane.AnchorBottom = 0.5f;
        _detailPane.OffsetLeft = -extent.X / 2; _detailPane.OffsetRight = extent.X / 2;
        _detailPane.OffsetTop = -extent.Y / 2; _detailPane.OffsetBottom = extent.Y / 2;
    }

    private void SetLibraryCollapsed(bool collapsed)
    {
        BattleLabHoverHint.HideAll(true);
        ResetDrag();
        if (collapsed) _libraryPopup.Close();
        else _libraryPopup.Open(_buttons["%ToggleLibraryButton"]);
        _libraryCollapsed = !_libraryPopup.IsOpen;
        RefreshSidebarState();
    }

    private void SetDetailsCollapsed(bool collapsed)
    {
        BattleLabHoverHint.HideAll(true);
        ResetDrag();
        if (collapsed) _detailsPopup.Close();
        else
        {
            _detailsPopup.Blocking = _detailTabs.CurrentTab != 3;
            LayoutDetailsWindow();
            _detailsPopup.Open(_buttons[DetailButtonFor(_detailTabs.CurrentTab)]);
        }
        _detailsCollapsed = !_detailsPopup.IsOpen;
        RefreshSidebarState();
    }

    private static string DetailButtonFor(int index) => index switch
    {
        1 => "%TeamDetailsButton", 2 => "%SettingsDetailsButton",
        3 => "%EquipmentDetailsButton", _ => "%UnitDetailsButton"
    };

    private void ToggleDetailsPage(int index)
    {
        var collapse = _detailsPopup.IsOpen && _detailTabs.CurrentTab == index;
        _detailTabs.CurrentTab = index;
        SetDetailsCollapsed(collapse);
    }

    private void OnLibraryClosed() { ResetDrag(); _libraryCollapsed = true; RefreshSidebarState(); }
    private void OnDetailsClosed() { ResetDrag(); _detailsCollapsed = true; RefreshSidebarState(); }
    private void OnDetailTabChanged(long _) => RefreshSidebarState();

    private void RefreshSidebarState()
    {
        if (!_buttons.ContainsKey("%EquipmentDetailsButton")) return;
        _buttons["%ToggleLibraryButton"].Text = "添加单位";
        _buttons["%ToggleLibraryButton"].TooltipText = "打开单位工具窗（F1）；拖入棋盘，关闭后保留搜索与选择";
        _buttons["%ToggleLibraryButton"].ThemeTypeVariation = _libraryPopup.IsOpen ? "SecondaryButton" : "CompactButton";
        foreach (var index in new[] { 0, 1, 2, 3 })
            _buttons[DetailButtonFor(index)].ThemeTypeVariation = _detailsPopup.IsOpen && _detailTabs.CurrentTab == index
                ? "SecondaryButton" : "CompactButton";
        _detailPopupTitle.Text = _detailTabs.CurrentTab switch
        { 1 => "团队与遗物", 2 => "实验设置", 3 => "装备", _ => "单位详情" };
    }
    private void ResizeBoard()
    {
        if (!IsInstanceValid(_boardCenter)) return;
        // The board host does not propagate the grid's minimum size back into the page.
        // Otherwise previously enlarged cells prevent the HBox from shrinking on opening a sidebar.
        var side = Mathf.Clamp(Mathf.Min((_boardCenter.Size.X - 27) / 10, (_boardCenter.Size.Y - 15) / 6), 32, 112);
        foreach (var cell in _cells.Values) cell.CustomMinimumSize = new Vector2(side, side);
        var gridSize = new Vector2(side * 10 + 27, side * 6 + 15);
        _battlefield.Size = gridSize;
        _battlefield.Position = (_boardCenter.Size - gridSize) / 2;
        _footprints.SetGrid(_battlefield.Position + Vector2.One * side * .5f,
            Vector2.One * (side + 3), Vector2.One * side);
    }
    private void RebuildLibraries()
    {
        foreach (var card in _cards)
        { card.Selected -= SelectPrototype; card.DragRequested -= BeginLibraryDrag; card.QueueFree(); }
        _cards.Clear();
        if (_content is null) return;
        var units = _content.Units.Where(MatchesLibrarySearch).ToArray();
        var side = (BattleLabSide)_placementTeam.GetSelectedId();
        foreach (var unit in units) AddCard(_unitLibrary, unit, side, DescribeClassification(unit.Classification));
        _libraryEmpty.Visible = units.Length == 0;
    }
    private void AddCard(Control owner, BattleLabPublishedUnit unit, BattleLabSide side, string classification)
    {
        var card = LibraryCardScene.Instantiate<BattleLabLibraryCard>();
        card.Bind(unit.StableId, unit.DisplayName, side, classification, unit.Definition.Portrait);
        card.SetInspection(BattleLabTooltipFormatter.Prototype(unit, UnitSnapshotFor(unit)));
        card.Selected += SelectPrototype; card.DragRequested += BeginLibraryDrag;
        owner.AddChild(card); card.SetSelected(_selectedPrototype == unit.StableId && _selectedSide == side); _cards.Add(card);
    }
    private void SelectPrototype(string contentId, BattleLabSide side)
    {
        ResetDrag(); _selectedPrototype = contentId; _selectedSide = side; _selectedInstanceId = string.Empty;
        GetNode<ScrollContainer>("DetailsPopup/DetailPane/Inset/DetailBox/DetailTabs/UnitPage").ScrollVertical = 0;
        RefreshAll("", true);
    }
    private void SelectInstance(string instanceId)
    {
        if (_session?.TryGet(instanceId, out _) != true) return;
        ResetDrag(); _selectedPrototype = string.Empty; _selectedInstanceId = instanceId;
        GetNode<ScrollContainer>("DetailsPopup/DetailPane/Inset/DetailBox/DetailTabs/UnitPage").ScrollVertical = 0;
        RefreshBuildHints();
        RefreshAll("", true);
    }
    private void BeginLibraryDrag(string contentId, BattleLabSide side)
    {
        BattleLabHoverHint.HideAll(true);
        _selectedPrototype = string.Empty; _selectedInstanceId = string.Empty;
        _dragContentId = contentId; _dragInstanceId = string.Empty; _dragSide = side; _dragging = true;
        foreach (var card in _cards) card.SetSelected(false);
        _placementHint.Text = $"拖入{SideName(side)} {UnitName(contentId)} · 释放到合法空格";
        RefreshDropStates(GetViewport().GetMousePosition());
    }
    private void BeginInstanceDrag(string instanceId)
    {
        BattleLabHoverHint.HideAll(true);
        if (_session?.TryGet(instanceId, out var unit) != true) return;
        _selectedPrototype = string.Empty; _selectedInstanceId = instanceId;
        _dragContentId = unit.ContentId; _dragInstanceId = instanceId; _dragSide = unit.Side; _dragging = true;
        _placementHint.Text = $"移动{SideName(unit.Side)} {UnitName(unit.ContentId)} · 可交换；拖回单位库可移除";
        RefreshDropStates(GetViewport().GetMousePosition());
    }
    private void CompleteDrag(Vector2 position)
    {
        if (_session is null) { ResetDrag(); return; }
        var target = _cells.FirstOrDefault(pair => !ToolContains(position) && pair.Value.GetGlobalRect().HasPoint(position));
        var before = _session.Freeze();
        BattleLabPlacementResult? result = null;
        var instanceId = _dragInstanceId;
        if (target.Value is not null)
        {
            if (!string.IsNullOrEmpty(instanceId) && _session.TryGet(instanceId, out var existing) && existing.Cell == target.Key)
            { ResetDrag(); RefreshAll("位置未改变。", true); return; }
            result = string.IsNullOrEmpty(instanceId)
                ? _session.AddAndPlace(_dragContentId, _dragSide, target.Key) : _session.Move(instanceId, target.Key);
            if (result.Succeeded) _selectedInstanceId = result.InstanceId;
        }
        else if (!string.IsNullOrEmpty(instanceId) && OriginLibraryContains(position))
        {
            var removed = _session.Recall(instanceId);
            ResetDrag(); RecordChange(before, "移除单位");
            RefreshAll(removed ? "单位已移除，可撤销。" : "单位已经不在战场。", removed); return;
        }
        ResetDrag(); RecordChange(before, string.IsNullOrEmpty(instanceId) ? "布置单位" : "移动单位");
        RefreshAll(result is null ? "拖动已取消。" : result.Succeeded
            ? result.SwappedInstanceId is null ? "放置完成。" : "位置已交换。" : result.RejectionReason,
            result?.Succeeded ?? true);
    }
    private void RefreshDropStates(Vector2 mousePosition)
    {
        if (_session is null) return;
        _footprints.ShowDrag(null);
        foreach (var (coordinate, cell) in _cells)
        {
            var prototype = new BattleLabUnitConfiguration(string.IsNullOrEmpty(_dragInstanceId) ? "lab-preview" : _dragInstanceId,
                _dragContentId, _dragSide, coordinate, []);
            if (!string.IsNullOrEmpty(_dragInstanceId) && _session.TryGet(_dragInstanceId, out var existing)) prototype = existing;
            var evaluation = BattleLabPlacementPolicy.Evaluate(_session, prototype, coordinate, !string.IsNullOrEmpty(_dragInstanceId));
            cell.ShowDropState(evaluation.Succeeded, evaluation.SwappedInstanceId is not null);
            if (!ToolContains(mousePosition) && cell.GetGlobalRect().HasPoint(mousePosition) &&
                _content?.TryGetUnit(_dragContentId, out var content) == true)
                _footprints.ShowDrag(new DeploymentBodyPreview(_dragInstanceId, coordinate,
                    content.Definition.BodyRadius, (int)_dragSide), evaluation.Succeeded);
        }
        MouseDefaultCursorShape = (!ToolContains(mousePosition) && _cells.Values.Any(cell => cell.GetGlobalRect().HasPoint(mousePosition))) ||
            OriginLibraryContains(mousePosition) ? CursorShape.PointingHand : CursorShape.Forbidden;
    }
    private void ResetDrag()
    {
        if (IsInstanceValid(_footprints)) _footprints.ShowDrag(null);
        _dragging = false; _dragContentId = string.Empty; _dragInstanceId = string.Empty;
        MouseDefaultCursorShape = CursorShape.Arrow;
        foreach (var cell in _cells.Values) if (IsInstanceValid(cell)) cell.ClearDropState();
    }
    private void ClearSelection()
    {
        ResetDrag(); _selectedPrototype = string.Empty; _selectedInstanceId = string.Empty;
        RefreshAll("已取消选择。", true);
    }
    private bool ToolContains(Vector2 position) =>
        (_libraryPopup.IsOpen && _libraryPane.GetGlobalRect().HasPoint(position)) ||
        (_detailsPopup.IsOpen && _detailPane.GetGlobalRect().HasPoint(position));

    private bool OriginLibraryContains(Vector2 position) =>
        _libraryPopup.IsOpen && _libraryPane.GetGlobalRect().HasPoint(position);

    private void OnPlacementTeamChanged(long _)
    {
        ResetDrag(); _selectedPrototype = string.Empty;
        RebuildLibraries();
        RefreshAll("", true);
    }
    private static string SideName(BattleLabSide side) => side == BattleLabSide.Player ? "A 队" : "B 队";

    private void RefreshAll(string feedback, bool success)
    {
        if (_session is null) return;
        _footprints.Bind(_session.Units.Select(unit => new DeploymentBodyPreview(unit.InstanceId,
            unit.Cell, _content!.TryGetUnit(unit.ContentId, out var content)
                ? content.Definition.BodyRadius : BattlefieldSpace.DefaultBodyRadius,
            (int)unit.Side, unit.InstanceId == _selectedInstanceId,
            content?.Definition.Portrait, content?.DisplayName ?? "")));
        if (!string.IsNullOrEmpty(_selectedInstanceId) && !_session.TryGet(_selectedInstanceId, out _)) _selectedInstanceId = string.Empty;
        foreach (var (coordinate, cell) in _cells)
        {
            var unit = _session.At(coordinate);
            var published = unit is not null && _content?.TryGetUnit(unit.ContentId, out var entry) == true ? entry : null;
            cell.Refresh(unit, unit?.InstanceId == _selectedInstanceId, published?.DisplayName, published?.Definition.Portrait,
                _session.Mode, published?.Definition.BodyRadius ?? BattlefieldSpace.DefaultBodyRadius);
        }
        foreach (var card in _cards) card.SetSelected(card.ContentId == _selectedPrototype && card.Side == _selectedSide);
        _equipmentOwner.Text = _session.TryGet(_selectedInstanceId, out var equipmentOwner)
            ? $"{UnitName(equipmentOwner.ContentId)} · {SideName(equipmentOwner.Side)}"
            : "在棋盘选中单位，再拖放装备";
        var snapshot = _session.Freeze();
        // Selection and feedback do not change prepared battle attributes.
        if (_derived is null || _derivedDigest != snapshot.CanonicalDigest)
        { _derived = BattleLabDerivedProjectionBuilder.Build(_session); _derivedDigest = snapshot.CanonicalDigest; }
        foreach (var (coordinate, cell) in _cells)
        {
            var unit = _session.At(coordinate);
            if (unit is not null && _content?.TryGetUnit(unit.ContentId, out var published) == true)
                cell.SetInspection(BattleLabTooltipFormatter.Instance(published, UnitSnapshotFor(published), unit,
                    _derived.Units.GetValueOrDefault(unit.InstanceId), _derived, _content));
            else cell.SetInspection(null);
        }
        _modeBanner.Text = "自由实验 · 全部单位可放入任意队伍，无人口与部署区域限制";
        var preset = string.IsNullOrEmpty(_activePresetName) ? "自定义配置" : _activePresetName;
        if (snapshot.CanonicalDigest != _presetDigest) preset += " · 已修改";
        _presetPanel.SetSummary(preset);
        _facts.Text = $"A 队 {_derived.PlayerCount}　B 队 {_derived.EnemyCount}";
        _status.Text = string.IsNullOrWhiteSpace(feedback) ? "" : (success ? "✓ " : "! ") + feedback;
        _readiness.Text = _derived.IsReady ? "配置就绪" : string.Join("；", _derived.RejectionReasons);
        _placementHint.Text = "";
        RefreshBuildChoices(); RefreshInspector(_derived); RefreshTeamFacts(_derived); RefreshActionAvailability();
    }
    private void RefreshActionAvailability()
    {
        if (_session is null) return;
        var selected = !string.IsNullOrEmpty(_selectedInstanceId) && _session.TryGet(_selectedInstanceId, out var unit) ? unit : null;
        _equipmentBox.Visible = selected is not null; _equipmentNotApplicable.Visible = selected is null;
        _equipmentNotApplicable.Text = "选中已布置的单位后配置装备。";
        _buttons["%DeleteSelectedButton"].Disabled = selected is null;
        _buttons["%SetRelicButton"].Disabled = _relicIds.Count == 0;
        _buttons["%RemoveRelicButton"].Disabled = _existingRelicIds.Count == 0;
        _buttons["%StartButton"].Disabled = _derived?.IsReady != true;
        _buttons["%StartButton"].TooltipText = _derived?.IsReady == true ? "使用当前配置开始战斗" : _readiness.Text;
        _buttons["%CancelSelectionButton"].Disabled = !_dragging && string.IsNullOrEmpty(_selectedInstanceId) && string.IsNullOrEmpty(_selectedPrototype);
        _buttons["%UndoButton"].Disabled = _history.Count == 0;
        _buttons["%UndoButton"].TooltipText = _history.Count == 0 ? "暂无可撤销操作" : $"撤销：{_history[^1].Label}（Ctrl+Z）";
        _buttons["%ClearPlayerButton"].Disabled = !_session.Units.Any(item => item.Side == BattleLabSide.Player);
        _buttons["%ClearEnemyButton"].Disabled = !_session.Units.Any(item => item.Side == BattleLabSide.Enemy);
        _buttons["%ClearAllButton"].Disabled = _session.Units.Count == 0;
    }
    private void RecordChange(BattleLabStartSnapshot before, string label)
    {
        if (_session is null || before.CanonicalDigest == _session.Freeze().CanonicalDigest) return;
        _history.Add(new EditHistory(before, label, _activePresetName, _presetDigest));
        if (_history.Count > 32) _history.RemoveAt(0);
    }
    private bool Edit(string label, Func<bool> mutation, string success, string failure)
    {
        if (_session is null) return false;
        var before = _session.Freeze();
        try
        {
            var succeeded = mutation();
            if (succeeded) RecordChange(before, label);
            RefreshAll(succeeded ? success : failure, succeeded); return succeeded;
        }
        catch (Exception exception) { RefreshAll(failure + "：" + exception.Message, false); return false; }
    }
    private void UndoLastEdit()
    {
        if (_session is null || _history.Count == 0) return;
        var entry = _history[^1];
        try
        {
            _session.Restore(entry.Snapshot); _history.RemoveAt(_history.Count - 1);
            _activePresetName = entry.PresetName; _presetDigest = entry.PresetDigest;
            ResetDrag(); _selectedPrototype = string.Empty; SyncRuleControls();
            RefreshAll($"已撤销：{entry.Label}。", true);
        }
        catch (Exception exception) { RefreshAll("无法撤销：" + exception.Message, false); }
    }
    private void OnSearchChanged(string _) => RebuildLibraries();
    private void OnUnitFilterSelected(long _) => RebuildLibraries();
    private bool MatchesLibrarySearch(BattleLabPublishedUnit unit)
    {
        var filter = _unitFilter.Selected < 0 ? 0 : _unitFilter.GetItemId(_unitFilter.Selected);
        return (filter == 0 || unit.Classification.HasFlag((BattleLabUnitClassification)filter)) && Matches(unit, _unitSearch.Text);
    }
    private static bool Matches(BattleLabPublishedUnit unit, string query) => string.IsNullOrWhiteSpace(query) ||
        unit.DisplayName.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase) ||
        unit.StableId.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase) ||
        unit.Definition.Description.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
    private string UnitName(string contentId) => _content?.TryGetUnit(contentId, out var unit) == true ? unit.DisplayName : contentId;
    private long ParseSeed() => long.TryParse(_seed.Text, System.Globalization.NumberStyles.Integer,
        System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : throw new InvalidOperationException("随机种子必须是有效的 64 位整数。");
    private bool CommitRuleInputs()
    {
        if (_session is null) return false;
        return Edit("调整实验参数", () => { _session.SetRules(_session.Mode, _session.CurrentPopulation, ParseSeed(), _session.FloorRuleId); return true; },
            "实验参数已应用。", "参数未应用");
    }
    private void ApplySeed() { if (CommitRuleInputs()) SyncRuleControls(); }
    private void OnSeedSubmitted(string _) => ApplySeed();
    private void OnClearPlayer() => ClearUnits(BattleLabSide.Player);
    private void OnClearEnemy() => ClearUnits(BattleLabSide.Enemy);
    private void OnClearAll() => ClearUnits(null);
    private void ClearUnits(BattleLabSide? side)
    {
        if (_session is null) return;
        ResetDrag(); _selectedPrototype = string.Empty;
        var label = side is null ? "清空战场单位" : side == BattleLabSide.Player ? "清空 A 队单位" : "清空 B 队单位";
        Edit(label, () => { _session.Clear(side); return true; }, label + "，可撤销。", "清空失败");
    }
    private void DeleteSelected()
    {
        if (_session is null || string.IsNullOrWhiteSpace(_selectedInstanceId)) return;
        ResetDrag(); Edit("移除单位", () => _session.Recall(_selectedInstanceId), "单位已移除，可撤销。", "单位已不存在");
    }
    private void OnBack() => EmitSignal(SignalName.BackRequested);
    private void OnStart()
    {
        if (_session is null || !CommitRuleInputs()) return;
        if (_derived?.IsReady != true) { RefreshAll(_readiness.Text, false); return; }
        ResetDrag(); EmitSignal(SignalName.StartRequested);
    }
    private void SyncRuleControls(bool includeSeed = true)
    {
        if (_session is null || _content is null) return;
        if (includeSeed) _seed.Text = _session.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    private void BindButton(string path, Action handler)
    {
        var button = GetNode<Button>(path); button.Pressed += handler;
        _buttonBindings.Add((button, handler)); _buttons.Add(path, button);
    }
    private void OpenPresets()
    {
        BattleLabHoverHint.HideAll(true);
        ResetDrag();
        _libraryPopup.Close();
        _detailsPopup.Close();
        _presetPanel.Open(_activePresetName, _buttons["%OpenPresetsButton"]);
    }
    private void LoadPreset(string name)
    {
        if (_presetStore is null || _session is null) return;
        if (!_presetStore.TryLoad(name, out var preset)) { _presetPanel.ShowFeedback("预设读取失败，当前配置未改变。"); return; }
        var before = _session.Freeze();
        try
        {
            _session.Restore(BattleLabPresetStore.ToSnapshot(preset)); RecordChange(before, "载入预设");
            _activePresetName = name; _presetDigest = _session.Freeze().CanonicalDigest;
            _selectedInstanceId = string.Empty; _selectedPrototype = string.Empty;
            ResetDrag(); SyncRuleControls(); _presetPanel.Close(); RefreshAll($"已载入“{name}”。", true);
        }
        catch (Exception exception) { _presetPanel.ShowFeedback("无法载入：" + exception.Message); }
    }
    private void SavePreset(string name)
    {
        if (_session is null || _presetStore is null) return;
        if (_presetStore.BuiltIns.ContainsKey(name)) { _presetPanel.ShowFeedback("这是内置预设名称，请为自己的配置取一个新名字。"); return; }
        if (name.Length is < 1 or > 64 || name.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_' || character > 127)))
        { _presetPanel.ShowFeedback("名称需为 1–64 个字，支持中文、字母、数字、横线和下划线；不含空格。"); return; }
        if (!CommitRuleInputs()) { _presetPanel.ShowFeedback(_status.Text); return; }
        if (!_presetStore.Save(name, _session.Freeze())) { _presetPanel.ShowFeedback("保存失败：实验室预设文件不可写。"); return; }
        _activePresetName = name; _presetDigest = _session.Freeze().CanonicalDigest;
        _presetPanel.SelectSaved(name);
        RefreshAll($"已保存“{name}”。", true);
    }
    private void PopulateBuildChoices()
    {
        _relicIds.Clear(); _relicChoice.Clear();
        _inspectedEquipmentId = _content?.Equipment.FirstOrDefault()?.StableId ?? string.Empty;
        foreach (var relic in _content?.Relics ?? [])
        {
            _relicIds.Add(relic.StableId); _relicChoice.AddItem(relic.DisplayName);
            _relicChoice.GetPopup().SetItemTooltip(_relicChoice.ItemCount - 1,
                BattleLabTooltipFormatter.Item(relic, _content!).PlainText);
        }
        _relicStacks.MinValue = 1; _relicStacks.MaxValue = int.MaxValue; _relicStacks.Value = 1;
    }
    private string EquipmentName(string contentId) => _content?.Equipment.FirstOrDefault(item => item.StableId == contentId)?.DisplayName ?? contentId;
    private void RefreshBuildChoices()
    {
        var selectedId = _existingRelicChoice.Selected >= 0 && _existingRelicChoice.Selected < _existingRelicIds.Count
            ? _existingRelicIds[_existingRelicChoice.Selected] : string.Empty;
        _existingRelicIds.Clear(); _existingRelicChoice.Clear();
        if (_session is null) return;
        foreach (var relic in _session.Relics.OrderBy(item => item.InstanceId, StringComparer.Ordinal))
        {
            _existingRelicIds.Add(relic.InstanceId);
            var name = _content?.Relics.FirstOrDefault(item => item.StableId == relic.ContentId)?.DisplayName ?? relic.ContentId;
            _existingRelicChoice.AddItem($"{name} ×{relic.Stacks}");
            var definition = _content?.Relics.FirstOrDefault(item => item.StableId == relic.ContentId);
            if (definition is not null)
                _existingRelicChoice.GetPopup().SetItemTooltip(_existingRelicChoice.ItemCount - 1,
                    BattleLabTooltipFormatter.Item(definition, _content!, relic.Stacks).PlainText);
        }
        if (_existingRelicChoice.ItemCount > 0) _existingRelicChoice.Select(Math.Max(0, _existingRelicIds.IndexOf(selectedId)));
        _equipmentDragPanel.Bind(_session, _selectedInstanceId, _equipmentDragContext);
        RefreshBuildHints();
    }

    private void OnBuildChoiceSelected(long _) => RefreshBuildHints();

    private void RefreshBuildHints()
    {
        if (_content is null || _session is null) return;
        var equipment = _content.Equipment.FirstOrDefault(item => item.StableId == _inspectedEquipmentId);
        var relicId = _relicChoice.Selected >= 0 && _relicChoice.Selected < _relicIds.Count
            ? _relicIds[_relicChoice.Selected] : string.Empty;
        var relic = _content.Relics.FirstOrDefault(item => item.StableId == relicId);
        BattleLabHoverHint.Bind(_relicChoice, relic is null ? null : BattleLabTooltipFormatter.Item(relic, _content));
        var appliedId = _existingRelicChoice.Selected >= 0 && _existingRelicChoice.Selected < _existingRelicIds.Count
            ? _existingRelicIds[_existingRelicChoice.Selected] : string.Empty;
        var applied = _session.Relics.FirstOrDefault(item => item.InstanceId == appliedId);
        var appliedDefinition = applied is null ? null : _content.Relics.FirstOrDefault(item => item.StableId == applied.ContentId);
        BattleLabHoverHint.Bind(_existingRelicChoice, appliedDefinition is null ? null :
            BattleLabTooltipFormatter.Item(appliedDefinition, _content, applied!.Stacks));
        var selected = _session.TryGet(_selectedInstanceId, out var owner) ? owner : null;
        var slotted = selected?.Equipment.FirstOrDefault(item => item.ContentId == _inspectedEquipmentId);
        var slottedDefinition = slotted is null ? null : _content.Equipment.FirstOrDefault(item => item.StableId == slotted.ContentId);
        _equipmentRules.Text = equipment is null ? "没有可选装备。" :
            "候选装备\n" + BattleLabTooltipFormatter.Item(equipment, _content).PlainText;
        if (slottedDefinition is not null)
            _equipmentRules.Text += "\n\n当前槽位装备\n" + BattleLabTooltipFormatter.Item(slottedDefinition, _content).PlainText;
        _relicRules.Text = relic is null ? "没有可选遗物。" :
            "候选遗物\n" + BattleLabTooltipFormatter.Item(relic, _content).PlainText;
        if (appliedDefinition is not null)
            _relicRules.Text += "\n\n已配置遗物\n" + BattleLabTooltipFormatter.Item(appliedDefinition, _content, applied!.Stacks).PlainText;
    }
    private void OnRetentionUpgrade(bool enabled)
    {
        if (_session is null) return;
        Edit("调整升阶", () => _session.SetRetentionUpgrade(_selectedInstanceId, enabled),
            enabled ? "已启用换目标保层升阶。" : "已恢复基础版。", "此单位不支持该升阶");
    }
    private void RefreshInspector(BattleLabDerivedProjection derived)
    {
        _retentionUpgrade.Visible = false; _inspectorDetails.Text = string.Empty;
        _statsPanel.Visible = false;
        _inspectorStats.Text = string.Empty;
        _inspectorGlossary.Text = string.Empty;
        if (_session is null) return;
        var selected = !string.IsNullOrEmpty(_selectedInstanceId) && _session.TryGet(_selectedInstanceId, out var instance) ? instance : null;
        var contentId = selected?.ContentId ?? _selectedPrototype;
        if (string.IsNullOrEmpty(contentId) || _content?.TryGetUnit(contentId, out var content) != true)
        { _unitTitle.Text = "选择一个单位"; _inspector.Text = "点击左侧单位了解能力；点击场上单位调整装备与升阶。"; return; }
        _unitTitle.Text = content.DisplayName + (selected is null ? " · 单位预览" : " · " + SideName(selected.Side));
        var identity = $"内容：{contentId}";
        if (selected is not null)
        {
            identity += $"\n实例：{selected.InstanceId}\n坐标：({selected.Cell.X}, {selected.Cell.Y})";
            _retentionUpgrade.Visible = UnitSnapshotFor(content).AttackHitGrowth?.RetentionUpgradeAvailable == true;
            _retentionUpgrade.SetPressedNoSignal(selected.RetainAttackStacks);
        }
        var definition = content.Definition;
        var snapshot = UnitSnapshotFor(content);
        float Read(CombatAttribute attribute, float fallback) => snapshot.AttributeDefinition?.Attributes
            .FirstOrDefault(item => item.Attribute == attribute)?.BaseValue ?? fallback;
        var health = definition.MaxHealth;
        var damage = definition.AttackDamage;
        var armor = definition.Armor;
        var speed = Read(CombatAttribute.AttackSpeed, 1);
        var reach = definition.AttackRange;
        var resistance = definition.BaseControlResistance;
        if (selected is not null && derived.Units.TryGetValue(selected.InstanceId, out var prepared))
        {
            health = prepared.Health; damage = prepared.Damage; armor = prepared.Armor;
            speed = prepared.AttackSpeed; reach = prepared.Reach; resistance = prepared.ControlResistance;
            var contributions = prepared.TraitContributions.Select(item =>
                $"{item.TraitId} +{item.Value}（{DescribeTraitSource(item.SourceKind)} · {item.SourceInstanceId} · {item.ContentIdentity}）");
            identity += "\n\n羁绊贡献来源\n" + string.Join("\n", contributions);
        }
        var equipment = selected is not null ? "\n\n装备：" +
            (selected.Equipment.IsDefaultOrEmpty ? "无" : string.Join("、", selected.Equipment.OrderBy(item => item.SlotIndex)
                .Select(item => $"{item.SlotIndex + 1}号槽 {EquipmentName(item.ContentId)}"))) : string.Empty;
        var abilities = DescribeAbilities(content);
        var interval = snapshot.AttackTicks * BattleTiming.TickSeconds / Math.Max(.01f, speed);
        _statsPanel.Visible = true;
        _healthStat.Bind(SemanticIconKeys.Health, $"{health:0.#}", "生命", "HealthValue");
        _attackStat.Bind(SemanticIconKeys.Attack, $"{damage:0.#}", "攻击", "DamageValue");
        _armorStat.Bind(SemanticIconKeys.Armor, $"{armor:0.#}", "防御", "ArmorValue");
        _speedStat.Bind(SemanticIconKeys.Time, snapshot.Behavior.DisableBasicAttacks ? "—" : $"{1 / Math.Max(.01f, interval):0.##}/秒", "普攻频率", "RangeValue");
        var attack = snapshot.Behavior.DisableBasicAttacks ? "不进行普攻" : $"普攻间隔 {interval:0.##} 秒 · 攻速倍率 ×{speed:0.##}";
        var range = snapshot.AttackDelivery == AttackDelivery.Melee ? "近战触及" : "远程射程";
        var maxMana = Read(CombatAttribute.MaxMana, definition.MaxMana);
        _inspectorStats.Text = (selected is null ? "基础属性" : "当前配置属性") + $"\n{attack}\n{range} {reach:0.##} 格 · 控制抗性 {resistance:P0}" +
            (maxMana > 0 ? $"\n基础法力 {Read(CombatAttribute.StartingMana, definition.StartingMana):0.#}/{maxMana:0.#} · 满蓝施法\n基础回蓝 {Read(CombatAttribute.ManaPerSecond, definition.ManaPerSecond):0.#}/秒" : "") + equipment;
        _inspector.Text = abilities.Length == 0 ? definition.Description : abilities;
        _inspectorGlossary.Text = string.Join("\n\n", _inspector.Vocabulary.Terms
            .Where(term => _inspector.Text.Contains(term.Word, StringComparison.Ordinal))
            .Select(term => term.Word + " · " + term.Explanation));
        _inspectorDetails.Text = identity + (abilities.Length == 0 ? string.Empty : "\n\n内容说明\n" + definition.Description);
    }
    private string DescribeAbilities(BattleLabPublishedUnit content)
    {
        if (_content is null) return string.Empty;
        var snapshot = UnitSnapshotFor(content);
        var retain = _session?.TryGet(_selectedInstanceId, out var selected) == true &&
            selected.ContentId == content.StableId && selected.RetainAttackStacks;
        return BattleLabTooltipFormatter.Abilities(snapshot, content.Definition.Description, retain);
    }
    private UnitSnapshot UnitSnapshotFor(BattleLabPublishedUnit content)
    {
        if (!_unitSnapshots.TryGetValue(content.StableId, out var snapshot))
        {
            snapshot = BattleSetupFactory.Snapshot(content.Entry, _content!.Package.Content);
            _unitSnapshots.Add(content.StableId, snapshot);
        }
        return snapshot;
    }
    private void RefreshTeamFacts(BattleLabDerivedProjection derived)
    {
        if (_session is null) return;
        var teams = new[] { (0, "A 队", derived.PlayerCount), (1, "B 队", derived.EnemyCount) };
        _teamFacts.Text = string.Join("\n\n", teams.Select(team =>
        {
            var traits = derived.Traits.Where(trait => trait.Team == team.Item1).Select(trait => trait.Text).ToArray();
            var equipment = _session.Units.Where(unit => (int)unit.Side == team.Item1).Sum(unit => unit.Equipment.Length);
            return $"{team.Item2} {team.Item3} 人 · 装备 {equipment} 件\n" +
                (traits.Length == 0 ? "当前没有已激活的团队羁绊。" : string.Join("\n", traits));
        }));
    }
    private void EquipDragged(string id, int slot)
    {
        if (_session is null) return;
        Edit("拖动装备", () => id.StartsWith("catalog:", StringComparison.Ordinal)
                ? _session.Equip(_selectedInstanceId, slot, id[8..])
                : _session.MoveEquipment(id, _selectedInstanceId, slot),
            "装备已配置；可拖回装备库移除。", "无法放入该槽位，原配置保持不变。");
    }
    private bool CanEquipOnUnit(string instanceId, Variant data)
    {
        if (_session is null || !EquipmentSlotButton.HasScope(data, "lab") ||
            !EquipmentSlotButton.HasContext(data, _equipmentDragContext) ||
            !EquipmentSlotButton.TryEquipmentId(data, out var id) || !_session.TryGet(instanceId, out var owner) ||
            owner.Equipment.Length >= _session.Content.Rules.EquipmentSlotCapacity) return false;
        return id.StartsWith("catalog:", StringComparison.Ordinal)
            ? _content!.Equipment.Any(item => "catalog:" + item.StableId == id)
            : _session.Units.Any(unit => unit.Equipment.Any(item => item.InstanceId == id));
    }
    private void RemoveDragged(string id)
    {
        var owner = _session?.Units.FirstOrDefault(unit => unit.Equipment.Any(item => item.InstanceId == id));
        var item = owner?.Equipment.FirstOrDefault(value => value.InstanceId == id);
        if (owner is null || item is null) return;
        Edit("拖回装备库", () => _session!.RemoveEquipment(owner.InstanceId, item.SlotIndex),
            "已移除装备配置；装备库仍可重复取用。", "无法移除装备，原配置保持不变。");
    }
    private void InspectEquipment(string contentId)
    {
        _inspectedEquipmentId = contentId;
        RefreshBuildHints();
    }
    private void SetTeamRelic()
    {
        if (_session is null || _relicChoice.Selected < 0 || _relicChoice.Selected >= _relicIds.Count) return;
        Edit("调整团队遗物", () => _session.SetRelic(_relicIds[_relicChoice.Selected], (int)_relicStacks.Value), "A 队遗物已更新。", "遗物配置无效");
    }
    private void RemoveTeamRelic()
    {
        if (_session is null || _existingRelicChoice.Selected < 0 || _existingRelicChoice.Selected >= _existingRelicIds.Count) return;
        var id = _existingRelicIds[_existingRelicChoice.Selected];
        Edit("移除团队遗物", () => _session.RemoveRelic(id), "团队遗物已移除。", "遗物已不存在");
    }
    private static string DescribeClassification(BattleLabUnitClassification classification)
    {
        var labels = new List<string>();
        if (classification.HasFlag(BattleLabUnitClassification.PlayerHero)) labels.Add("英雄");
        if (classification.HasFlag(BattleLabUnitClassification.TestDummy)) labels.Add("测试假人");
        if (classification.HasFlag(BattleLabUnitClassification.PveNormal)) labels.Add("基础 / 普通");
        if (classification.HasFlag(BattleLabUnitClassification.PveElite)) labels.Add("精英");
        if (classification.HasFlag(BattleLabUnitClassification.PveBoss)) labels.Add("首领");
        if (classification.HasFlag(BattleLabUnitClassification.PublishedSummon)) labels.Add("召唤物");
        return labels.Count == 0 ? "单位" : string.Join(" / ", labels);
    }
    private static string DescribeTraitSource(TraitContributionSourceKind sourceKind) => sourceKind switch
    {
        TraitContributionSourceKind.Hero => "英雄", TraitContributionSourceKind.Equipment => "装备",
        TraitContributionSourceKind.ExplicitExtra => "额外", _ => sourceKind.ToString()
    };
    public override void _ExitTree()
    {
        BattleLabHoverHint.HideAll(true);
        ResetDrag();
        foreach (var (button, handler) in _buttonBindings) if (IsInstanceValid(button)) button.Pressed -= handler;
        _buttonBindings.Clear(); _buttons.Clear();
        if (IsInstanceValid(_retentionUpgrade)) _retentionUpgrade.Toggled -= OnRetentionUpgrade;
        if (IsInstanceValid(_unitSearch)) _unitSearch.TextChanged -= OnSearchChanged;
        if (IsInstanceValid(_unitFilter)) _unitFilter.ItemSelected -= OnUnitFilterSelected;
        if (IsInstanceValid(_placementTeam)) _placementTeam.ItemSelected -= OnPlacementTeamChanged;
        if (IsInstanceValid(_presetPanel))
        { _presetPanel.LoadRequested -= LoadPreset; _presetPanel.SaveRequested -= SavePreset; }
        if (IsInstanceValid(_seed)) _seed.TextSubmitted -= OnSeedSubmitted;
        if (IsInstanceValid(_relicChoice)) _relicChoice.ItemSelected -= OnBuildChoiceSelected;
        if (IsInstanceValid(_existingRelicChoice)) _existingRelicChoice.ItemSelected -= OnBuildChoiceSelected;
        if (IsInstanceValid(_detailTabs)) _detailTabs.TabChanged -= OnDetailTabChanged;
        if (IsInstanceValid(_libraryPopup)) _libraryPopup.Closed -= OnLibraryClosed;
        if (IsInstanceValid(_detailsPopup)) _detailsPopup.Closed -= OnDetailsClosed;
        if (IsInstanceValid(_boardCenter)) _boardCenter.Resized -= ResizeBoard;
        Resized -= ResizeLayout;
        foreach (var card in _cards)
        { if (IsInstanceValid(card)) { card.Selected -= SelectPrototype; card.DragRequested -= BeginLibraryDrag; } }
        foreach (var cell in _cells.Values)
        {
            if (!IsInstanceValid(cell)) continue;
            cell.UnitSelected -= SelectInstance; cell.UnitDragRequested -= BeginInstanceDrag;
        }
        _cards.Clear(); _cells.Clear(); _history.Clear(); _unitSnapshots.Clear();
        _content = null; _session = null; _presetStore = null; _derived = null;
    }
}
