using System;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class RosterHeroSelector : Button
{
    public string HeroId { get; private set; } = "";
    public event Action<string>? Selected;
    public event Action<string, int, string>? EquipmentDropped;
    public Func<Variant, EquipmentDropEvaluation>? EvaluateDrop { get; set; }
    private string _normal = "RosterSelector";
    public override void _Ready() { Pressed += SelectHero; MouseExited += ResetStyle; }
    public override void _ExitTree() { Pressed -= SelectHero; MouseExited -= ResetStyle; }
    private void SelectHero() => Selected?.Invoke(HeroId);
    public void Bind(string id, UnitDefinition definition, bool selected, bool deployed)
    {
        HeroId = id;
        var portrait = GetNode<UnitPortrait>("%Portrait");
        if (portrait.Definition != definition.Portrait) portrait.Bind(definition.Portrait, definition.Icon);
        GetNode<Label>("%HeroName").Text = definition.DisplayName;
        GetNode<Label>("%SelectedMark").Visible = selected;
        _normal = selected ? "RosterSelectorSelected" : "RosterSelector";
        ThemeTypeVariation = _normal;
        AccessibilityName = definition.DisplayName + (deployed ? " · 出战" : " · 后备");
        TooltipText = AccessibilityName + "\n选择查看；装备可直接拖到此英雄，装入首个空槽。";
    }
    public override bool _CanDropData(Vector2 position, Variant data)
    {
        if (!EquipmentSlotButton.TryEquipmentId(data, out _)) return false;
        var result = EvaluateDrop?.Invoke(data);
        ThemeTypeVariation = result?.Allowed == true ? "RosterSelectorSelected" : "RosterSelector";
        UiDragVisual.Aim(data, this, result?.Allowed == true);
        return result?.Allowed == true;
    }
    public override void _DropData(Vector2 position, Variant data)
    {
        var result = EvaluateDrop?.Invoke(data);
        if (result is { Allowed: true } accepted) EquipmentDropped?.Invoke(HeroId, accepted.SlotIndex, accepted.ItemId);
        ResetStyle();
    }
    private void ResetStyle() => ThemeTypeVariation = _normal;
    public override void _Notification(int what) { if (what == NotificationDragEnd) ResetStyle(); }
}
