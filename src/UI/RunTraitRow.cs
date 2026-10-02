using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.UI;

public partial class RunTraitRow : Button
{
    private readonly List<Label> _markers = [];
    private PackedScene _markerScene = null!;
    public RunTraitViewModel? Model { get; private set; }
    public override void _Ready() => _markerScene = GD.Load<PackedScene>("res://scenes/ui/components/TraitTierMarker.tscn");

    public void Bind(RunTraitViewModel model)
    {
        Model = model;
        var active = model.ActiveIndex >= 0;
        ThemeTypeVariation = active ? "RunTraitActiveRow" : "RunTraitInactiveRow";
        GetNode<Label>("%TraitName").Text = model.DisplayName;
        GetNode<Label>("%TraitName").ThemeTypeVariation = active ? "RunTraitName" : "RunTraitMutedName";
        GetNode<Label>("%TraitValue").Text = model.Value.ToString();
        GetNode<Label>("%TraitValue").ThemeTypeVariation = model.ActiveIndex switch
        {
            0 => "RunTraitBronzeCount", 1 => "RunTraitSilverCount", >= 2 => "RunTraitActiveCount", _ => "RunTraitMutedCount"
        };
        GetNode<Label>("%TraitStatus").Text = model.Status;
        var tint = GetNode<Label>("%TraitValue").GetThemeColor("font_color");
        GetNode<TextureRect>("%TraitIcon").Texture = model.Icon;
        GetNode<TextureRect>("%TraitIcon").Modulate = tint;
        GetNode<TextureRect>("%TraitFacet").Modulate = active ? Colors.White : new Color(.76f, .74f, .70f);
        var tiers = GetNode<HBoxContainer>("%TraitTiers");
        while (_markers.Count > model.Thresholds.Count)
        {
            var marker = _markers[^1];
            _markers.RemoveAt(_markers.Count - 1);
            tiers.RemoveChild(marker);
            marker.QueueFree();
        }
        for (var index = 0; index < model.Thresholds.Count; index++)
        {
            if (index >= _markers.Count)
            {
                var marker = _markerScene.Instantiate<Label>();
                tiers.AddChild(marker);
                _markers.Add(marker);
            }
            _markers[index].Text = index == model.ActiveIndex ? $"◆ {model.Thresholds[index]}" : model.Thresholds[index].ToString();
            _markers[index].ThemeTypeVariation = index == model.ActiveIndex ? model.ActiveIndex switch
            {
                0 => "RunTraitBronzeTier", 1 => "RunTraitSilverTier", _ => "RunTraitCurrentTier"
            } : "RunTraitOtherTier";
        }
        AccessibilityName = model.Hint.PlainText;
        BattleLabHoverHint.Bind(this, model.Hint);
    }

    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) ReleaseFocus();
    }
}
