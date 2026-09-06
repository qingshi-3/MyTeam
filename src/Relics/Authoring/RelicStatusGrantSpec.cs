using Godot;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Relics;

[GlobalClass]
public partial class RelicStatusGrantSpec : Resource
{
    [Export] public string BindingId { get; set; } = "";
    [Export] public RelicUnitTargetSpec Target { get; set; } = null!;
    [Export] public StatusDefinition Status { get; set; } = null!;
}
