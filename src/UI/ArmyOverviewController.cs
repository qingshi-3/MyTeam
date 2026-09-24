using Godot;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class ArmyOverviewController : Control
{
    public event System.Action? EquipmentChanged;
    private Button _summary = null!;
    private ArmyResourceStrip _resourceStrip = null!;
    private Button _close = null!;
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
    private RunApplication? _application;
    private string _inspectedHero = "";

    public bool IsOpen => _isOpen;
    // The global resource strip owns this header band. ScreenRouter reserves it
    // before laying out page-local controls, including their popup launchers.
    public float HeaderReservedHeight => System.Math.Max(_summary.OffsetBottom,
        _summary.OffsetTop + _summary.GetCombinedMinimumSize().Y) + 12f;

    public override void _Ready()
    {
        _summary = GetNode<Button>("%SummaryButton");
        _resourceStrip = GetNode<ArmyResourceStrip>("%ResourceStrip");
        _close = GetNode<Button>("%CloseButton");
        _backdrop = GetNode<Button>("%Backdrop");
        _drawer = GetNode<PanelContainer>("%Drawer");
        _rows = GetNode<VBoxContainer>("%Rows");
        _rowScene = GD.Load<PackedScene>("res://scenes/ui/components/ArmyDrawerRow.tscn");
        _sectionScene = GD.Load<PackedScene>("res://scenes/ui/components/ArmyDrawerSection.tscn");
        _equipmentPanel = GetNode<RosterLoadoutView>("%ArmyEquipmentPanel");
        var pages = GetNode<TabContainer>("%Pages");
        pages.SetTabTitle(0, "英雄与装备");
        pages.SetTabTitle(1, "军团总览");
        _equipmentPanel.HeroSelected += OnEquipmentHeroSelected;
        _equipmentPanel.EquipmentChanged += OnEquipmentChanged;
        _summary.Pressed += Open;
        _close.Pressed += Close;
        _backdrop.Pressed += Close;
        _backdrop.FocusMode = FocusModeEnum.None;
        Close();
    }

    public override void _ExitTree()
    {
        _equipmentPanel.EquipmentChanged -= OnEquipmentChanged;
        _equipmentPanel.HeroSelected -= OnEquipmentHeroSelected;
        RestoreModalFocus();
        _summary.Pressed -= Open;
        _close.Pressed -= Close;
        _backdrop.Pressed -= Close;
    }

    public void BindModalFocusScope(Control focusScope) => _focusScope = focusScope;

    public void BindEquipmentManagement(RunApplication app)
    {
        _application = app;
        RefreshRoster();
    }

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
    public void Close()
    {
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
        if (_application?.ActiveRun is { } run)
        {
            Bind(ArmyOverviewFactory.Build(run, _application.Content, _application.Rules));
        }
        foreach (var node in GetTree().GetNodesInGroup("context_popup_windows"))
            if (node is ContextPopup popup && popup.IsOpen) popup.Close();
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
        _close.GrabFocus();
    }

    public override void _Input(InputEvent @event)
    {
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

    private void RestoreModalFocus()
    {
        if (!_isOpen) return;
        if (_focusScope is not null && GodotObject.IsInstanceValid(_focusScope))
            _focusScope.FocusBehaviorRecursive = _previousScopeBehavior;
        _summary.FocusMode = _previousSummaryFocusMode;
        _isOpen = false;
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
