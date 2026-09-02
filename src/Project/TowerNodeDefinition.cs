using Godot;
using TowerAutobattler.Domain;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class TowerNodeDefinition : Resource
{
    [Export] public TowerNodeType Type { get; set; }
    [Export] public string TitlePattern { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string DescriptionPattern { get; set; } = string.Empty;
    [Export] public int Risk { get; set; }
}
