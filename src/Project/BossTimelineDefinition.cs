using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class BossTimelineDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public string BossContentId { get; set; } = string.Empty;
    [Export] public BossPhaseDefinition[] Phases { get; set; } = [];
}
