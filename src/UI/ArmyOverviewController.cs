using Godot;

namespace TowerAutobattler.UI;

public partial class ArmyOverviewController : Control
{
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

    public bool IsOpen => _isOpen;

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
        _summary.Pressed += Open;
        _close.Pressed += Close;
        _backdrop.Pressed += Close;
        _backdrop.FocusMode = FocusModeEnum.None;
        Close();
    }

    public override void _ExitTree()
    {
        RestoreModalFocus();
        _summary.Pressed -= Open;
        _close.Pressed -= Close;
        _backdrop.Pressed -= Close;
    }

    public void BindModalFocusScope(Control focusScope) => _focusScope = focusScope;

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
        AddSection("物品");
        if (model.Items.Count == 0) AddRow(new ArmyOverviewRowViewModel("暂无物品", "", ""));
        else foreach (var item in model.Items) AddRow(item);
    }

    public void Close()
    {
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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen || !@event.IsActionPressed("ui_cancel")) return;
        Close();
        GetViewport().SetInputAsHandled();
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
