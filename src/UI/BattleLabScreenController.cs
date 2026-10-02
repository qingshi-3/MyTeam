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

public partial class BattleLabScreenController : Control, IUiMotionHost
{
    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    [Signal] public delegate void BackRequestedEventHandler();
    [Signal] public delegate void StartRequestedEventHandler();
    [Export] public PackedScene LibraryCardScene { get; set; } = null!;
    [Export] public PackedScene BoardCellScene { get; set; } = null!;
    [Export] public BattleLabPresetCatalog PresetCatalog { get; set; } = null!;
    [Export] public Vector2 InformationWindowSize { get; set; } = new(660, 700);

    private readonly Dictionary<Vector2I, BattleLabBoardCell> _cells = [];
    private BattleLabLibraryView _library = null!;
    private IReadOnlyList<BattleLabLibraryCard> _cards => _library.Cards;
    private readonly List<(Button Button, Action Handler)> _buttonBindings = [];
    private readonly Dictionary<string, Button> _buttons = [];
    private string _inspectedEquipmentId = string.Empty;
    private readonly List<string> _relicIds = [], _existingRelicIds = [];
    private BattleLabContentIndex? _content;
    private BattleLabSession? _session;
    private BattleLabPresetStore? _presetStore;
    private BattleLabDerivedProjection? _derived;
    private BattleLabEditorSession? _editor;
    private BattleLabDragController? _drag;
    private readonly BattleLabSelection _selection = new();
    private string _activePresetName => _editor?.PresetName ?? "";
    private string _presetDigest => _editor?.PresetDigest ?? "";
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
    private Label _libraryEmpty = null!, _status = null!, _facts = null!;
    private Label _placementHint = null!, _teamFacts = null!;
    private Label _readiness = null!, _equipmentNotApplicable = null!;
    private BattleLabEquipmentPanel _equipmentDragPanel = null!;
    private string _equipmentDragContext = Guid.NewGuid().ToString("N");
    private CombatRichText _equipmentRules = null!, _relicRules = null!;
    private CheckButton _retentionUpgrade = null!;
    private BattleLabUnitInspector _unitInspector = null!;
    private bool _dragging => _drag?.IsDragging ?? false;
    private bool _libraryCollapsed = true, _detailsCollapsed = true;
    private string _selectedPrototype { get => _selection.Prototype; set => _selection.Prototype = value; }
    private string _selectedInstanceId { get => _selection.InstanceId; set => _selection.InstanceId = value; }
    private BattleLabSide _selectedSide { get => _selection.Side; set => _selection.Side = value; }

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
        _libraryPopup.CancelActiveInteraction = CancelPointerDrag;
        _detailsPopup.CancelActiveInteraction = CancelPointerDrag;
        _detailTabs = GetNode<TabContainer>("%DetailTabs");
        _unitSearch = GetNode<LineEdit>("%UnitSearch");
        _libraryEmpty = GetNode<Label>("%LibraryEmpty");
        _unitFilter = GetNode<OptionButton>("%UnitFilter");
        _seed = GetNode<LineEdit>("%Seed");
        _status = GetNode<Label>("%Status");
        _facts = GetNode<Label>("%Facts");
        _placementHint = GetNode<Label>("%PlacementHint");
        _teamFacts = GetNode<Label>("%TeamFacts");
        _retentionUpgrade = GetNode<CheckButton>("%RetentionUpgrade");
        _unitInspector = new BattleLabUnitInspector(GetNode<Control>("DetailsPopup/DetailPane/Inset/DetailBox/DetailTabs/UnitPage"), _retentionUpgrade);
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
        _library = new BattleLabLibraryView(LibraryCardScene, _unitLibrary, _libraryEmpty, _unitSearch,
            _unitFilter, _placementTeam, _selection, SelectPrototype, BeginLibraryDrag);
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
        ResetDrag();
        _presetStore = presetStore;
        _editor = new BattleLabEditorSession(content, session, presetStore);
        _library.Bind(_editor);
        _drag = new BattleLabDragController(_editor, _selection, this, _libraryPane, _placementHint, _footprints,
            _cells, _cards, ToolContains, OriginLibraryContains, () => ReduceUiMotion(), RefreshAll);
        _derived = null;
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
    private void RebuildLibraries() => _library.RebuildLibraries();
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
    private void BeginLibraryDrag(string id, BattleLabSide side) => _drag?.BeginLibraryDrag(id, side);
    private void BeginInstanceDrag(string id) => _drag?.BeginInstanceDrag(id);
    private void CompleteDrag(Vector2 position) => _drag?.CompleteDrag(position);
    private void RefreshDropStates(Vector2 position) => _drag?.RefreshDropStates(position);
    private void ResetDrag() => _drag?.ResetDrag();
    private bool CancelPointerDrag() => _drag?.CancelPointerDrag() ?? false;

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

    internal static string PlayerFacingPresetName(string name)
    {
        var separator = name.IndexOf('·');
        if (separator < 0) return name;
        var prefix = name[..separator].Trim();
        return prefix.Length == 4 && char.IsLetter(prefix[0]) && char.IsLetter(prefix[1]) &&
            char.IsDigit(prefix[2]) && char.IsDigit(prefix[3]) ? name[(separator + 1)..].Trim() : name;
    }

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
        _library.RefreshSelection();
        _equipmentOwner.Text = _session.TryGet(_selectedInstanceId, out var equipmentOwner)
            ? $"{UnitName(equipmentOwner.ContentId)} · {SideName(equipmentOwner.Side)}"
            : "在棋盘选中单位，再拖放装备";
        var snapshot = _session.Freeze();
        // Selection and feedback do not change prepared battle attributes.
        _derived = _editor!.Prepared();
        foreach (var (coordinate, cell) in _cells)
        {
            var unit = _session.At(coordinate);
            if (unit is not null && _content?.TryGetUnit(unit.ContentId, out var published) == true)
                cell.SetInspection(BattleLabTooltipFormatter.Instance(published, UnitSnapshotFor(published), unit,
                    _derived.Units.GetValueOrDefault(unit.InstanceId), _derived, _content));
            else cell.SetInspection(null);
        }
        var preset = string.IsNullOrEmpty(_activePresetName) ? "自定义配置" : _activePresetName;
        if (snapshot.CanonicalDigest != _presetDigest) preset += " · 已修改";
        _presetPanel.SetSummary(_presetStore?.BuiltIns.ContainsKey(_activePresetName) == true
            ? PlayerFacingPresetName(preset) : preset);
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
        _buttons["%UndoButton"].Disabled = _editor!.UndoCount == 0;
        _buttons["%UndoButton"].TooltipText = _editor!.UndoCount == 0 ? "暂无可撤销操作" : $"撤销：{_editor!.UndoLabel}（Ctrl+Z）";
        _buttons["%ClearPlayerButton"].Disabled = !_session.Units.Any(item => item.Side == BattleLabSide.Player);
        _buttons["%ClearEnemyButton"].Disabled = !_session.Units.Any(item => item.Side == BattleLabSide.Enemy);
        _buttons["%ClearAllButton"].Disabled = _session.Units.Count == 0;
    }
    private bool Edit(string label, Func<bool> mutation, string success, string failure)
    {
        if (_editor is null) return false;
        var result = _editor.Edit(label, mutation, success, failure);
        RefreshAll(result.Message, result.Succeeded);
        return result.Succeeded;
    }
    private void UndoLastEdit()
    {
        if (_editor is null || _editor.UndoCount == 0) return;
        var result = _editor.Undo();
        if (result.Succeeded) { ResetDrag(); _selectedPrototype = ""; SyncRuleControls(); }
        RefreshAll(result.Message, result.Succeeded);
    }
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
        if (_editor?.Presets is null) return;
        var result = _editor.LoadPreset(name);
        if (!result.Succeeded) { _presetPanel.ShowFeedback(result.Message); return; }
        _selectedInstanceId = ""; _selectedPrototype = "";
        ResetDrag(); SyncRuleControls(); _presetPanel.Close(); RefreshAll(result.Message, true);
    }
    private void SavePreset(string name)
    {
        if (_editor?.Presets is null) return;
        var invalid = _editor.ValidatePresetName(name);
        if (invalid.Length > 0) { _presetPanel.ShowFeedback(invalid); return; }
        if (!CommitRuleInputs()) { _presetPanel.ShowFeedback(_status.Text); return; }
        var result = _editor.SavePreset(name);
        if (!result.Succeeded) { _presetPanel.ShowFeedback(result.Message); return; }
        _presetPanel.SelectSaved(name);
        RefreshAll(result.Message, true);
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
        if (_editor is not null) _unitInspector.Refresh(_editor, _selection, derived);
    }
    private UnitSnapshot UnitSnapshotFor(BattleLabPublishedUnit content) => _editor!.UnitSnapshotFor(content);
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
        if (Edit("拖动装备", () => id.StartsWith("catalog:", StringComparison.Ordinal)
                ? _session.Equip(_selectedInstanceId, slot, id[8..])
                : _session.MoveEquipment(id, _selectedInstanceId, slot),
            "装备已配置；可拖回装备库移除。", "无法放入该槽位，原配置保持不变。"))
            UiDragVisual.Committed(GetViewport(), id);
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
        if (Edit("拖回装备库", () => _session!.RemoveEquipment(owner.InstanceId, item.SlotIndex),
            "已移除装备配置；装备库仍可重复取用。", "无法移除装备，原配置保持不变。"))
            UiDragVisual.Committed(GetViewport(), id);
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
    public override void _ExitTree()
    {
        BattleLabHoverHint.HideAll(true);
        ResetDrag();
        foreach (var (button, handler) in _buttonBindings) if (IsInstanceValid(button)) button.Pressed -= handler;
        _buttonBindings.Clear(); _buttons.Clear();
        if (IsInstanceValid(_retentionUpgrade)) _retentionUpgrade.Toggled -= OnRetentionUpgrade;
        _library?.Dispose();
        if (IsInstanceValid(_placementTeam)) _placementTeam.ItemSelected -= OnPlacementTeamChanged;
        if (IsInstanceValid(_presetPanel))
        { _presetPanel.LoadRequested -= LoadPreset; _presetPanel.SaveRequested -= SavePreset; }
        if (IsInstanceValid(_seed)) _seed.TextSubmitted -= OnSeedSubmitted;
        if (IsInstanceValid(_relicChoice)) _relicChoice.ItemSelected -= OnBuildChoiceSelected;
        if (IsInstanceValid(_existingRelicChoice)) _existingRelicChoice.ItemSelected -= OnBuildChoiceSelected;
        if (IsInstanceValid(_detailTabs)) _detailTabs.TabChanged -= OnDetailTabChanged;
        if (IsInstanceValid(_libraryPopup)) { _libraryPopup.Closed -= OnLibraryClosed; _libraryPopup.CancelActiveInteraction = null; }
        if (IsInstanceValid(_detailsPopup)) { _detailsPopup.Closed -= OnDetailsClosed; _detailsPopup.CancelActiveInteraction = null; }
        if (IsInstanceValid(_boardCenter)) _boardCenter.Resized -= ResizeBoard;
        Resized -= ResizeLayout;
        foreach (var cell in _cells.Values)
        {
            if (!IsInstanceValid(cell)) continue;
            cell.UnitSelected -= SelectInstance; cell.UnitDragRequested -= BeginInstanceDrag;
        }
        _cells.Clear(); _editor = null; _drag = null;
        _content = null; _session = null; _presetStore = null; _derived = null;
    }
}
