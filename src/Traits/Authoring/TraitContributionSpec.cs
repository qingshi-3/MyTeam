using Godot;

namespace TowerAutobattler.Traits;

[GlobalClass]
public partial class TraitContributionSpec : Resource
{
    [Export] public string TraitId { get; set; } = string.Empty;
    [Export(PropertyHint.Range, "1,999,1")] public int Value { get; set; } = 1;
}
