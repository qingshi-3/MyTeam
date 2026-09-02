using Godot;
using TowerAutobattler.UI;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class ProjectPresentationDefinition : Resource
{
    [Export] public SemanticIconCatalog? SemanticIcons { get; set; }
    [Export] public PackedScene? ChoiceCard { get; set; }
    [Export] public PackedScene? UnitChoiceCard { get; set; }
    [Export] public PackedScene? ItemChoiceCard { get; set; }
}
