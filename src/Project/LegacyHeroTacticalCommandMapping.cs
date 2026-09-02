using Godot;
using TowerAutobattler.TacticalCommands;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class LegacyHeroTacticalCommandMapping : Resource
{
    [Export] public string HeroContentId { get; set; } = string.Empty;
    [Export] public TacticalCommandDefinition Command { get; set; } = null!;
}
