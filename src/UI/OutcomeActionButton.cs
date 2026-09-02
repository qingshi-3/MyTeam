using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.UI;

public partial class OutcomeActionButton : Button
{
    private Label _title = null!;
    private readonly List<SemanticChip> _facts = [];

    public override void _Ready()
    {
        _title = GetNode<Label>("%ActionTitle");
        if (_facts.Count > 0) return;
        _facts.Add(GetNode<SemanticChip>("%OutcomeFact1"));
        _facts.Add(GetNode<SemanticChip>("%OutcomeFact2"));
        _facts.Add(GetNode<SemanticChip>("%OutcomeFact3"));
    }

    public void Bind(string title, IReadOnlyList<SemanticFact> facts, string detail, StringName themeVariation)
    {
        _title ??= GetNode<Label>("%ActionTitle");
        if (_facts.Count == 0)
        {
            _facts.Add(GetNode<SemanticChip>("%OutcomeFact1"));
            _facts.Add(GetNode<SemanticChip>("%OutcomeFact2"));
            _facts.Add(GetNode<SemanticChip>("%OutcomeFact3"));
        }
        Text = string.Empty;
        ThemeTypeVariation = themeVariation;
        _title.Text = title;
        TooltipText = detail;
        for (var index = 0; index < _facts.Count; index++)
        {
            _facts[index].Visible = index < facts.Count;
            if (index < facts.Count) _facts[index].Bind(facts[index]);
        }
    }
}
