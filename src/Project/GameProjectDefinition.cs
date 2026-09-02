using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class GameProjectDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public ContentCatalog? Content { get; set; }
    [Export] public CampaignDefinition? Campaign { get; set; }
    [Export] public RunRulesDefinition? RunRules { get; set; }
    [Export] public ProjectPresentationDefinition? Presentation { get; set; }
}
