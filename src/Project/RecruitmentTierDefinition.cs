using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class RecruitmentTierDefinition : Resource
{
    [Export] public string[] HeroIds { get; set; } = [];
}
