using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class ExpeditionProgressionDefinition : ProgressionDefinition
{
    [Export] public string StableId { get; set; } = "expedition_v1";
    [Export] public int Size { get; set; } = 16;
    [Export] public int GenerationVersion { get; set; } = 1;
    [Export] public int MaximumAttempts { get; set; } = 64;
    [Export] public ExpeditionBiomeDefinition[] Biomes { get; set; } = [];
    [Export] public ExpeditionStageDefinition[] Stages { get; set; } = [];
    [Export] public ExpeditionSiteDefinition[] Sites { get; set; } = [];
}
