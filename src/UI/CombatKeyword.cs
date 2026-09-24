using Godot;

namespace TowerAutobattler.UI;

// Presentation vocabulary only. These definitions never execute or modify a combat rule.
[GlobalClass]
public partial class CombatKeyword : Resource
{
    [Export] public string Word { get; set; } = "";
    [Export] public string[] Aliases { get; set; } = [];
    [Export] public StringName SemanticIcon { get; set; } = new();
    [Export(PropertyHint.MultilineText)] public string Explanation { get; set; } = "";
    [Export] public Color Tint { get; set; } = Colors.White;
}
