using Godot;

namespace TowerAutobattler.Project;

public enum ContentPoolKind
{
    Soldier,
    Item,
    Enemy,
    FloorRule
}

[GlobalClass]
public partial class ContentPoolDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public ContentPoolKind Kind { get; set; }
    [Export] public string[] ContentIds { get; set; } = [];
}
