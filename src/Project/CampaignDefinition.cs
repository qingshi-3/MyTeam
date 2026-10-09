using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class CampaignDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public ProgressionDefinition Progression { get; set; } = new TowerProgressionDefinition();
    // Source compatibility for older authoring fixtures; production resources use Progression.
    private TowerProgressionDefinition Tower => Progression as TowerProgressionDefinition
        ?? throw new System.InvalidOperationException("This campaign is not a tower.");
    public int FloorsPerRegion { get => Tower.FloorsPerRegion; set => Tower.FloorsPerRegion = value; }
    public TowerRegionDefinition[] Regions { get => Tower.Regions; set => Tower.Regions = value; }
    public TowerNodeTableDefinition? NodeTable { get => Tower.NodeTable; set => Tower.NodeTable = value; }
    [Export] public ContentPoolDefinition? StarterPool { get; set; }
    [Export] public ContentPoolDefinition? RecruitmentPool { get; set; }
    [Export] public ContentPoolDefinition? ItemRewardPool { get; set; }
    [Export] public bool SeparateItemRewardCategories { get; set; }
    [Export] public ContentPoolDefinition? ShopPool { get; set; }
    [Export] public RunOfferDefinition[] RunOffers { get; set; } = [];
    [Export] public RecruitmentSupplyDefinition? RecruitmentSupply { get; set; }
}
