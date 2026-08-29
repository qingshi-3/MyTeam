using Godot;

namespace TowerAutobattler.UI;

[GlobalClass]
public partial class SemanticIconEntry : Resource
{
    [Export] public StringName Key { get; set; } = new();
    [Export] public Texture2D? Icon { get; set; }
    [Export] public StringName PresentationRole { get; set; } = new();
}
