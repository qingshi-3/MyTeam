using Godot;
using TowerAutobattler.Domain;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class EncounterDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public TowerNodeType NodeType { get; set; } = TowerNodeType.Combat;
    [Export] public string TitlePattern { get; set; } = string.Empty;
    [Export] public ContentPoolDefinition? EnemyPool { get; set; }
    [Export] public ContentPoolDefinition? FloorRulePool { get; set; }
    [Export] public string LeadEnemyId { get; set; } = string.Empty;
    [Export] public int BaseEnemyCount { get; set; } = 4;
    [Export] public bool AddRegionIndexToCount { get; set; } = true;
    [Export] public int SeedSalt { get; set; }
    [Export] public BossTimelineDefinition? BossTimeline { get; set; }
}
