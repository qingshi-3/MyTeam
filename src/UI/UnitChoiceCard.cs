using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class UnitChoiceCard : Button
{
    [Signal] public delegate void ChosenEventHandler(string stableId);
    [Export] public PackedScene TraitBadgeScene { get; set; } = null!;
    [Export] public PackedScene StatBlockScene { get; set; } = null!;

    public string StableId { get; private set; } = string.Empty;
    public string SearchText { get; private set; } = string.Empty;
    public UnitPortrait Portrait { get; private set; } = null!;

    private Label _name = null!;
    private HFlowContainer _identityFacts = null!;
    private HFlowContainer _attributeFacts = null!;
    private Label _description = null!;
    private Label _meta = null!;
    private ChosenEventHandler? _chosenHandler;

    public override void _Ready()
    {
        CacheNodes();
        Pressed += OnPressed;
    }

    public override void _ExitTree()
    {
        Pressed -= OnPressed;
        if (_chosenHandler is not null) Chosen -= _chosenHandler;
        _chosenHandler = null;
    }

    public void Bind(
        string stableId,
        UnitDefinition definition,
        Texture2D? fallback,
        string description,
        string meta,
        StringName? nameVariation = null,
        StringName? metaVariation = null)
    {
        CacheNodes();
        StableId = stableId;
        Text = string.Empty;
        var identityFacts = new List<SemanticFact> { UnitSemanticFacts.Responsibility(definition.Role) };
        identityFacts.AddRange(UnitSemanticFacts.Traits(definition.Faction, definition.Tags));
        SemanticFact[] attributeFacts =
        [
            UnitSemanticFacts.Health(definition.MaxHealth.ToString("0")),
            UnitSemanticFacts.Damage(definition.AttackDamage.ToString("0")),
            UnitSemanticFacts.Reach(definition.AttackRange)
        ];
        SearchText = string.Join("\n", new[] { definition.DisplayName }
            .Concat(identityFacts.Select(fact => fact.Text))
            .Concat(attributeFacts.Select(fact => fact.Text))
            .Concat(new[] { description, meta }));
        Portrait.Bind(definition.Portrait, fallback);
        _name.Text = definition.DisplayName;
        _name.ThemeTypeVariation = nameVariation ?? "ChoiceTitle";
        BindTraits(_identityFacts, identityFacts);
        BindStats(_attributeFacts, definition);
        _description.Text = description;
        _meta.Text = meta;
        _meta.Visible = !string.IsNullOrWhiteSpace(meta);
        _meta.ThemeTypeVariation = metaVariation ?? "ChoiceFooter";
        TooltipText = description;
    }

    public void ConnectChosen(Action<string> handler)
    {
        if (_chosenHandler is not null) Chosen -= _chosenHandler;
        _chosenHandler = handler.Invoke;
        Chosen += _chosenHandler;
    }

    private void OnPressed() => EmitSignal(SignalName.Chosen, StableId);

    private void CacheNodes()
    {
        Portrait ??= GetNode<UnitPortrait>("%UnitPortrait");
        _name ??= GetNode<Label>("%UnitName");
        _identityFacts ??= GetNode<HFlowContainer>("%IdentityFacts");
        _attributeFacts ??= GetNode<HFlowContainer>("%AttributeFacts");
        _description ??= GetNode<Label>("%UnitDescription");
        _meta ??= GetNode<Label>("%UnitMeta");
    }

    private static void Clear(Container parent)
    {
        foreach (var child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.Free();
        }
    }

    private void BindTraits(Container parent, IEnumerable<SemanticFact> facts)
    {
        Clear(parent);
        foreach (var fact in facts)
        {
            var badge = TraitBadgeScene.Instantiate<TraitBadge>();
            parent.AddChild(badge);
            badge.Bind(fact);
        }
    }

    private void BindStats(Container parent, UnitDefinition definition)
    {
        Clear(parent);
        var facts = new[]
        {
            (SemanticIconKeys.Health, definition.MaxHealth.ToString("0"), "生命", new StringName("HealthValue")),
            (SemanticIconKeys.Damage, definition.AttackDamage.ToString("0"), "伤害", new StringName("DamageValue")),
            (SemanticIconKeys.Reach, definition.AttackRange.ToString("0.#"), "攻击距离", new StringName("RangeValue"))
        };
        foreach (var fact in facts)
        {
            var block = StatBlockScene.Instantiate<StatBlock>();
            parent.AddChild(block);
            block.Bind(fact.Item1, fact.Item2, fact.Item3, fact.Item4);
        }
    }
}
