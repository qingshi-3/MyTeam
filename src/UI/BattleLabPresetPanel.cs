using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.BattleLab;

namespace TowerAutobattler.UI;

// Owns the preset browser, not the editable session or persistence transaction.
public partial class BattleLabPresetPanel : Control
{
    public event Action<string>? LoadRequested;
    public event Action<string>? SaveRequested;

    private readonly List<string> _names = [];
    private BattleLabPresetStore? _store;
    private Func<string, string> _unitName = id => id;
    private Control? _returnFocus;
    private LineEdit _search = null!, _name = null!;
    private ItemList _choices = null!;
    private Label _summary = null!, _preview = null!;
    private Button _load = null!, _save = null!, _restore = null!, _close = null!;

    public bool IsOpen => IsVisibleInTree();

    public override void _Ready()
    {
        _search = GetNode<LineEdit>("%PresetSearch");
        _name = GetNode<LineEdit>("%PresetName");
        _choices = GetNode<ItemList>("%PresetChoice");
        _summary = GetNode<Label>("%PresetSummary");
        _preview = GetNode<Label>("%PresetPreview");
        _load = GetNode<Button>("%LoadPresetButton");
        _save = GetNode<Button>("%SavePresetButton");
        _restore = GetNode<Button>("%RestoreDefaultButton");
        _close = GetNode<Button>("%ClosePresetsButton");
        _search.TextChanged += OnSearchChanged;
        _choices.ItemSelected += OnSelected;
        _choices.ItemActivated += OnActivated;
        _load.Pressed += RequestLoad;
        _save.Pressed += RequestSave;
        _restore.Pressed += RequestDefault;
        _close.Pressed += Close;
    }

    public void Bind(BattleLabPresetStore? store, Func<string, string> unitName)
    {
        _store = store;
        _unitName = unitName;
        _returnFocus = null;
        Hide();
        RefreshChoices();
    }

    public void Open(string activePresetName, Control returnFocus)
    {
        _returnFocus = returnFocus;
        RefreshChoices(activePresetName);
        Show();
        _search.GrabFocus();
    }

    public void Close()
    {
        Hide();
        if (IsInstanceValid(_returnFocus) && _returnFocus!.IsVisibleInTree())
            _returnFocus.GrabFocus();
        _returnFocus = null;
    }

    public void SetSummary(string summary) => _summary.Text = summary;
    public void ShowFeedback(string message) => _preview.Text = message;

    public void SelectSaved(string name)
    {
        _search.Text = string.Empty;
        RefreshChoices(name);
        ShowFeedback($"“{name}”已保存到我的预设。");
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!IsOpen || inputEvent is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.Escape)
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Tab) CycleFocus(key.ShiftPressed);
    }

    private void CycleFocus(bool backwards)
    {
        var controls = new Control[] { _search, _choices, _load, _restore, _name, _save, _close }
            .Where(control => control.IsVisibleInTree() && (control is not BaseButton button || !button.Disabled)).ToArray();
        if (controls.Length == 0) return;
        var current = Array.IndexOf(controls, GetViewport().GuiGetFocusOwner());
        controls[(current + (backwards ? -1 : 1) + controls.Length) % controls.Length].GrabFocus();
        GetViewport().SetInputAsHandled();
    }

    private string? SelectedName()
    {
        var selected = _choices.GetSelectedItems();
        return selected.Length > 0 && selected[0] >= 0 && selected[0] < _names.Count ? _names[selected[0]] : null;
    }

    private void RefreshChoices(string? preferred = null)
    {
        preferred ??= SelectedName();
        _names.Clear();
        _choices.Clear();
        var query = _search.Text.Trim();
        foreach (var name in _store?.ListNames() ?? [])
        {
            if (query.Length > 0 && !name.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
            _names.Add(name);
            _choices.AddItem($"{(_store?.BuiltIns.ContainsKey(name) == true ? "内置" : "我的")} · {name}");
            _choices.SetItemTooltip(_choices.ItemCount - 1, _store?.BuiltInDescription(name) ?? string.Empty);
        }
        var selected = _names.IndexOf(preferred ?? _store?.DefaultPresetName ?? string.Empty);
        if (_names.Count > 0) _choices.Select(Math.Max(0, selected));
        _load.Disabled = _names.Count == 0;
        _save.Disabled = _store is null;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        var name = SelectedName();
        if (_store is null || name is null || !_store.TryLoad(name, out var preset))
        { ShowFeedback("没有匹配的预设。可搜索 BC 编号、英雄名或机制。"); return; }
        var players = preset.Units.Where(unit => unit.Side == BattleLabSide.Player).ToArray();
        var heroes = players.GroupBy(unit => unit.ContentId).Select(group =>
            _unitName(group.Key) + (group.Count() > 1 ? $" ×{group.Count()}" : string.Empty));
        var text = $"自由实验 · " +
            $"A 队 {players.Length} / B 队 {preset.Units.Count - players.Length} · 种子 {preset.Seed}\n" +
            string.Join("、", heroes) + " · 载入可撤销";
        var description = _store.BuiltInDescription(name);
        ShowFeedback(text + (string.IsNullOrWhiteSpace(description) ? "" : "\n" + description));
    }

    private void OnSearchChanged(string _) => RefreshChoices();
    private void OnSelected(long _) => RefreshPreview();
    private void OnActivated(long _) => RequestLoad();
    private void RequestLoad()
    {
        if (SelectedName() is { } name) LoadRequested?.Invoke(name);
    }
    private void RequestSave() => SaveRequested?.Invoke(_name.Text.Trim());
    private void RequestDefault()
    {
        if (_store is null) return;
        _search.Text = string.Empty;
        RefreshChoices(_store.DefaultPresetName);
        RequestLoad();
    }

    public override void _ExitTree()
    {
        _search.TextChanged -= OnSearchChanged;
        _choices.ItemSelected -= OnSelected;
        _choices.ItemActivated -= OnActivated;
        _load.Pressed -= RequestLoad;
        _save.Pressed -= RequestSave;
        _restore.Pressed -= RequestDefault;
        _close.Pressed -= Close;
        _store = null;
        _unitName = id => id;
        _returnFocus = null;
        _names.Clear();
    }
}
