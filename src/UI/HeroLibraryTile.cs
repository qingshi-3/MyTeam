using System;
using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.UI;

public partial class HeroLibraryTile : Button
{
    [Signal] public delegate void SelectionRequestedEventHandler(string stableId);
    [Export] public PackedScene TraitBadgeScene { get; set; } = null!;

    private UnitPortrait _portrait = null!;
    private Label _name = null!;
    private Label _state = null!;
    private Container _traits = null!;

    public string StableId { get; private set; } = string.Empty;
    public bool Unlocked { get; private set; }

    public override void _Ready()
    {
        CacheNodes();
        Pressed += RequestSelection;
    }

    public override void _ExitTree()
    {
        Pressed -= RequestSelection;
    }

    public void Bind(HeroSelectionViewModel model)
    {
        CacheNodes();
        StableId = model.StableId;
        Unlocked = model.Unlocked;
        _portrait.Bind(model.Definition.Portrait, model.Definition.Icon);
        _name.Text = model.Definition.DisplayName;
        _state.Text = model.Unlocked ? "可出征" : "未解锁";
        _state.ThemeTypeVariation = model.Unlocked ? "SecondaryLabel" : "DangerValue";
        BindTraits(model);
        TooltipText = model.Unlocked ? $"预览 {model.Definition.DisplayName}" : $"预览 {model.Definition.DisplayName}（未解锁）";
    }

    public void SetPreviewed(bool previewed)
    {
        ThemeTypeVariation = previewed ? "SelectedButton" : "CompactButton";
        _state.Text = previewed ? (Unlocked ? "◆ 当前预览" : "◆ 未解锁") : (Unlocked ? "可出征" : "未解锁");
        _state.ThemeTypeVariation = previewed ? "HeroIdentity" : Unlocked ? "SecondaryLabel" : "DangerValue";
    }

    private void BindTraits(HeroSelectionViewModel model)
    {
        foreach (var child in _traits.GetChildren())
        {
            _traits.RemoveChild(child);
            child.Free();
        }
        var facts = new List<SemanticFact> { UnitSemanticFacts.Responsibility(model.Definition.Role, false) };
        facts.AddRange(UnitSemanticFacts.Traits(model.Definition.Faction, model.Definition.Tags));
        foreach (var fact in facts)
        {
            var badge = TraitBadgeScene.Instantiate<TraitBadge>();
            _traits.AddChild(badge);
            badge.Bind(fact);
        }
    }

    private void RequestSelection() => EmitSignal(SignalName.SelectionRequested, StableId);

    private void CacheNodes()
    {
        _portrait ??= GetNode<UnitPortrait>("%HeroPortrait");
        _name ??= GetNode<Label>("%HeroName");
        _state ??= GetNode<Label>("%HeroState");
        _traits ??= GetNode<Container>("%HeroTraits");
    }
}
