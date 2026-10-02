using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.UI;

public partial class HeroLibraryTile : Button
{
    [Signal] public delegate void SelectionRequestedEventHandler(string stableId);
    [Export] public PackedScene TraitBadgeScene { get; set; } = null!;
    [Export] public StyleBox? SelectedFrameStyle { get; set; }
    [Export] public bool SymbolicOpeningState { get; set; }

    private UnitPortrait _portrait = null!;
    private Label _name = null!;
    private Label _state = null!;
    private Container _traits = null!;

    public string StableId { get; private set; } = string.Empty;
    public bool Unlocked { get; private set; }
    public UnitCardOrbit Orbit => GetNode<UnitCardOrbit>("Orbit");
    public UiCardDisclosure Disclosure => Orbit.Disclosure;

    public override void _Ready()
    {
        CacheNodes();
        Pressed += RequestSelection;
        Orbit.Configure(this, GetNode<Control>("CardFrame"), GetNodeOrNull<Button>("DetailsButton"));
    }

    public override void _ExitTree()
    {
        Pressed -= RequestSelection;
    }

    public void Bind(HeroSelectionViewModel model)
    {
        CacheNodes();
        if (StableId != model.StableId) Disclosure.Reset();
        StableId = model.StableId;
        Unlocked = model.Unlocked;
        _portrait.Bind(model.Definition.Portrait, model.Definition.Icon);
        _name.Text = model.Definition.DisplayName;
        _state.Text = model.Unlocked ? "可出征" : "未解锁";
        _state.ThemeTypeVariation = model.Unlocked ? "SecondaryLabel" : "DangerValue";
        BindTraits(model);
        TooltipText = "";
        var information = model.Snapshot is null ? null : new UnitInformation(model.StableId, model.Definition, model.Snapshot);
        GetNode<Label>("Margin/Layout/CoreFacts/Attack/Value").Text = (information?.Read(CombatAttribute.AttackDamage) ?? model.Definition.AttackDamage).ToString("0.#");
        GetNode<Label>("Margin/Layout/CoreFacts/Spell/Value").Text = (information?.Read(CombatAttribute.SpellPower) ?? model.Definition.SpellPower).ToString("0.#");
        GetNode<Label>("Margin/Layout/CoreFacts/Health/Value").Text = (information?.Read(CombatAttribute.MaxHealth) ?? model.Definition.MaxHealth).ToString("0.#");
        if (information is not null) Orbit.Bind(information, model.RuleTitle, model.RuleDescription,
            string.Join(" · ", UnitSemanticFacts.Traits(model.Definition.Faction, model.Definition.Tags).Select(fact => fact.Text)));
        Orbit.BindEmptyEquipment();
        foreach (var (name, attribute) in new[] { ("Attack", CombatAttribute.AttackDamage), ("Spell", CombatAttribute.SpellPower), ("Health", CombatAttribute.MaxHealth) })
        {
            var target = GetNode<Control>("Margin/Layout/CoreFacts/" + name);
            target.MouseFilter = MouseFilterEnum.Stop;
            if (information is not null)
            {
                var fact = information.AttributeFact(attribute);
                BattleLabHoverHint.Bind(target, new(fact.Caption, Stats: fact.Explanation, SingleEntry: true));
            }
        }
    }

    public void SetPreviewed(bool previewed)
    {
        GetNode<Panel>("CardFrame").ThemeTypeVariation = previewed ? "OrbitFrameSelected" : "OrbitFrame";
        _state.Text = previewed ? (Unlocked ? "◆ 当前预览" : "◆ 未解锁") : (Unlocked ? "可出征" : "未解锁");
        _state.ThemeTypeVariation = previewed ? "HeroIdentity" : Unlocked ? "SecondaryLabel" : "DangerValue";
    }

    public void SetOpeningState(int tier, bool selected)
    {
        var frame = GetNode<Panel>("CardFrame");
        frame.ThemeTypeVariation = selected ? "OrbitFrameSelected" : "OrbitFrame";
        if (selected && SelectedFrameStyle is not null) frame.AddThemeStyleboxOverride("panel", SelectedFrameStyle);
        else frame.RemoveThemeStyleboxOverride("panel");
        _state.Text = SymbolicOpeningState ? tier switch
        { 1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", _ => tier.ToString() } : $"{tier} 阶";
        _state.ThemeTypeVariation = "SecondaryLabel";
        if (SymbolicOpeningState)
        {
            _state.MouseFilter = MouseFilterEnum.Stop;
            _state.AccessibilityName = $"{tier} 阶英雄";
            BattleLabHoverHint.Bind(_state, new("英雄阶位", Stats: $"招募阶位为 {tier} 阶。", SingleEntry: true));
        }
        AccessibilityName = $"{_name.Text}，{tier} 阶" + (selected ? "，已选择；点击取消选择" : "，点击查看并选择");
        GetNodeOrNull<OpeningCardMotion>("OpeningMotion")?.SetSelected(selected);
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
