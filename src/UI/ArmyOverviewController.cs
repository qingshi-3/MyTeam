using Godot;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class ArmyOverviewController : Control, IUiMotionHost
{
    [Signal] public delegate void GrowthChangedEventHandler();
    public event System.Action? EquipmentChanged;
    public event System.Action<bool>? OpenChanged;
    private bool _pageInspectionOpen;
    private Color _summaryModulate = Colors.White;
    private Button _summary = null!;
    private ArmyResourceStrip _resourceStrip = null!;
    private Button _close = null!;
    private Button _pageToggle = null!;
    private Button _backdrop = null!;
    private PanelContainer _drawer = null!;
    private VBoxContainer _rows = null!;
    private PackedScene _rowScene = null!;
    private PackedScene _sectionScene = null!;
    private Control? _previousFocus;
    private Control? _focusScope;
    private FocusBehaviorRecursiveEnum _previousScopeBehavior;
    private FocusModeEnum _previousSummaryFocusMode;
    private bool _isOpen;
    private RosterLoadoutView _equipmentPanel = null!;
    private GrowthWorkbenchPanel _growthPanel = null!;
    private RunApplication? _application;
    private string _inspectedHero = "";

    public bool IsOpen => _isOpen;
    public GrowthWorkbenchPanel GrowthPanel => _growthPanel;
    public System.Func<bool> ReduceUiMotion { get; set; } = () => false;
    private UiPopupMotion? _motion;
    // The global resource strip owns this header band. ScreenRouter reserves it
    // before laying out page-local controls, including their popup launchers.
    public float HeaderReservedHeight => System.Math.Max(_summary.OffsetBottom,
        _summary.OffsetTop + _summary.GetCombinedMinimumSize().Y) + 12f;

    public override void _Ready()
    {
        _summary = GetNode<Button>("%SummaryButton");
        _summaryModulate = _summary.Modulate;
        _resourceStrip = GetNode<ArmyResourceStrip>("%ResourceStrip");
        _close = GetNode<Button>("%CloseButton");
        _backdrop = GetNode<Button>("%Backdrop");
        _drawer = GetNode<PanelContainer>("%Drawer");
        _motion = new UiPopupMotion(_drawer, _backdrop, () => ReduceUiMotion());
        _rows = GetNode<VBoxContainer>("%Rows");
        _rowScene = GD.Load<PackedScene>("res://scenes/ui/components/ArmyDrawerRow.tscn");
        _sectionScene = GD.Load<PackedScene>("res://scenes/ui/components/ArmyDrawerSection.tscn");
        _equipmentPanel = GetNode<RosterLoadoutView>("%ArmyEquipmentPanel");
        _growthPanel = GetNode<GrowthWorkbenchPanel>("%GrowthWorkbenchPanel");
        var pages = GetNode<TabContainer>("%Pages");
        pages.SetTabTitle(0, "英雄与装备");
        pages.SetTabTitle(1, "军团总览");
        pages.SetTabTitle(2, "成长工坊");
        pages.TabChanged += OnPageChanged;
        _pageToggle = GetNode<Button>("%PageToggle");
        _pageToggle.Pressed += TogglePage;
        _equipmentPanel.HeroSelected += OnEquipmentHeroSelected;
        _equipmentPanel.EquipmentChanged += OnEquipmentChanged;
        _growthPanel.Changed += OnGrowthChanged;
        _summary.Pressed += Open;
        _close.Pressed += Close;
        _backdrop.Pressed += Close;
        _backdrop.FocusMode = FocusModeEnum.None;
        Close();
    }

    public override void _ExitTree()
    {
        _motion?.Dispose();
        _equipmentPanel.EquipmentChanged -= OnEquipmentChanged;
        _equipmentPanel.HeroSelected -= OnEquipmentHeroSelected;
        _growthPanel.Changed -= OnGrowthChanged;
        GetNode<TabContainer>("%Pages").TabChanged -= OnPageChanged;
        RestoreModalFocus();
        _summary.Pressed -= Open;
        _close.Pressed -= Close;
        _backdrop.Pressed -= Close;
        _pageToggle.Pressed -= TogglePage;
    }

    public void BindModalFocusScope(Control focusScope) => _focusScope = focusScope;
    public void SetPageInspectionOpen(bool open)
    {
        _pageInspectionOpen = open;
        RefreshSummaryPresentation();
    }
    private void RefreshSummaryPresentation() =>
        _summary.Modulate = _isOpen || _pageInspectionOpen ? new Color(_summaryModulate, 0) : _summaryModulate;

    public void BindEquipmentManagement(RunApplication app)
    {
        _application = app;
        _growthPanel.Bind(app);
        GetNode<TabContainer>("%Pages").SetTabHidden(2, !_growthPanel.IsGrowthEnabled);
        RefreshRoster();
    }

    private void OnGrowthChanged() => EmitSignal(SignalName.GrowthChanged);

    private void OnEquipmentChanged()
    {
        if (_application?.ActiveRun is not { } run) return;
        Bind(ArmyOverviewFactory.Build(run, _application.Content, _application.Rules));
        EquipmentChanged?.Invoke();
    }

    public void Bind(ArmyOverviewViewModel model)
    {
        _summary.Text = string.Empty;
        _summary.TooltipText = "打开军团详情";
        _resourceStrip.Bind(model);
        ClearRows();
        AddSection("英雄名册");
        if (model.RosterHeroes.Count == 0) AddRow(new ArmyOverviewRowViewModel("暂无英雄", "", ""));
        else foreach (var hero in model.RosterHeroes) AddRow(hero);
        AddSection("战术指令");
        if (model.TacticalCommands.Count == 0) AddRow(new ArmyOverviewRowViewModel("暂无战术指令", "", ""));
        else foreach (var command in model.TacticalCommands) AddRow(command);
        AddSection("遗物与备用装备");
        if (model.Items.Count == 0) AddRow(new ArmyOverviewRowViewModel("暂无物品", "", ""));
        else foreach (var item in model.Items) AddRow(item);
        RefreshRoster();
    }

    private void RefreshRoster()
    {
        if (_application is not null) _equipmentPanel.Bind(_application, _inspectedHero);
    }

    private void OnEquipmentHeroSelected(string identity) => _inspectedHero = identity;
    private void TogglePage()
    {
        var pages = GetNode<TabContainer>("%Pages");
        var next = pages.CurrentTab;
        do next = (next + 1) % pages.GetTabCount(); while (pages.IsTabHidden(next));
        pages.CurrentTab = next;
        _pageToggle.Text = NextPageTitle(pages);
    }
    private static string NextPageTitle(TabContainer pages)
    {
        var next = pages.CurrentTab;
        do next = (next + 1) % pages.GetTabCount(); while (pages.IsTabHidden(next));
        return pages.GetTabTitle(next);
    }

    private void OnPageChanged(long index)
    {
        var pages = GetNode<TabContainer>("%Pages");
        GetNode<Label>("Drawer/Layout/Header/Title").Text = pages.GetTabTitle((int)index);
        if (_pageToggle is not null) _pageToggle.Text = NextPageTitle(pages);
    }
    public void OpenGrowthWorkbench()
    {
        if (!_growthPanel.IsGrowthEnabled) return;
        Open();
        var pages = GetNode<TabContainer>("%Pages");
        pages.CurrentTab = 2;
        _pageToggle.Text = NextPageTitle(pages);
        _growthPanel.GetNode<OptionButton>("%Producer").GrabFocus();
    }
    public void Close()
    {
        BattleLabHoverHint.HideAll(true);
        if (!_isOpen || !IsVisibleInTree()) { FinishClose(); return; }
        _motion!.Close(FinishClose);
    }

    private void FinishClose()
    {
        _motion?.Reset();
        BattleLabHoverHint.HideAll(true);
        _drawer.Visible = false;
        _backdrop.Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        RestoreModalFocus();
        if (_previousFocus is not null && GodotObject.IsInstanceValid(_previousFocus)) _previousFocus.GrabFocus();
        _previousFocus = null;
    }

    private void Open()
    {
        if (_isOpen) return;
        GetNode<TabContainer>("%Pages").CurrentTab = 0;
        _pageToggle.Text = NextPageTitle(GetNode<TabContainer>("%Pages"));
        if (_application?.ActiveRun is { } run)
        {
            Bind(ArmyOverviewFactory.Build(run, _application.Content, _application.Rules));
            _growthPanel.Refresh();
        }
        foreach (var node in GetTree().GetNodesInGroup("context_popup_windows"))
            if (node is ContextPopup popup && popup.IsOpen) popup.CloseImmediately();
        BattleLabHoverHint.HideAll(true);
        _previousFocus = GetViewport().GuiGetFocusOwner();
        _previousSummaryFocusMode = _summary.FocusMode;
        _summary.FocusMode = FocusModeEnum.None;
        if (_focusScope is not null)
        {
            _previousScopeBehavior = _focusScope.FocusBehaviorRecursive;
            _focusScope.FocusBehaviorRecursive = FocusBehaviorRecursiveEnum.Disabled;
        }
        _isOpen = true;
        MouseFilter = MouseFilterEnum.Stop;
        _backdrop.Visible = true;
        _drawer.Visible = true;
        _motion?.Open();
        RefreshSummaryPresentation();
        OpenChanged?.Invoke(true);
        _close.GrabFocus();
    }

    public override void _Input(InputEvent @event)
    {
        if (_isOpen && _motion?.IsClosing == true) { GetViewport().SetInputAsHandled(); return; }
        if (!_isOpen || @event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.Escape)
        {
            if (GetViewport().GuiIsDragging()) GetViewport().GuiCancelDrag();
            else Close();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Tab)
        {
            var focusable = FocusableControls(_drawer).ToArray();
            if (focusable.Length > 0)
            {
                var index = System.Array.IndexOf(focusable, GetViewport().GuiGetFocusOwner());
                focusable[(index + (key.ShiftPressed ? -1 : 1) + focusable.Length) % focusable.Length].GrabFocus();
            }
            GetViewport().SetInputAsHandled();
        }
    }

    private static IEnumerable<Control> FocusableControls(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Control control && control.IsVisibleInTree() && control.FocusMode == FocusModeEnum.All
                && control is not BaseButton { Disabled: true }) yield return control;
            foreach (var nested in FocusableControls(child)) yield return nested;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged && _isOpen && !IsVisibleInTree()) FinishClose();
    }

    private void RestoreModalFocus()
    {
        if (!_isOpen) return;
        if (_focusScope is not null && GodotObject.IsInstanceValid(_focusScope))
            _focusScope.FocusBehaviorRecursive = _previousScopeBehavior;
        _summary.FocusMode = _previousSummaryFocusMode;
        _isOpen = false;
        RefreshSummaryPresentation();
        OpenChanged?.Invoke(false);
    }

    private void AddSection(string title)
    {
        var label = _sectionScene.Instantiate<Label>();
        label.Text = title;
        _rows.AddChild(label);
    }

    private void AddRow(ArmyOverviewRowViewModel model)
    {
        var row = _rowScene.Instantiate<ArmyDrawerRow>();
        _rows.AddChild(row);
        row.Bind(model);
    }

    private void ClearRows()
    {
        foreach (var child in _rows.GetChildren())
        {
            _rows.RemoveChild(child);
            child.Free();
        }
    }
}
