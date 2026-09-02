using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class CampaignDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public int FloorsPerRegion { get; set; } = 5;
    [Export] public TowerRegionDefinition[] Regions { get; set; } = [];
    [Export] public TowerNodeTableDefinition? NodeTable { get; set; }
    [Export] public ContentPoolDefinition? StarterPool { get; set; }
    [Export] public ContentPoolDefinition? RecruitmentPool { get; set; }
    [Export] public ContentPoolDefinition? ItemRewardPool { get; set; }
    [Export] public ContentPoolDefinition? ShopPool { get; set; }
}
