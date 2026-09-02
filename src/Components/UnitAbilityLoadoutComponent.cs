using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Content;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class UnitAbilityLoadoutComponent : Node
{
    [Export] public AbilityLoadoutDefinition? Loadout { get; set; }

    public CompiledAbilityLoadout Resolve(CompiledContentGraph graph) =>
        graph.ResolveLoadout(Loadout ?? throw new System.InvalidOperationException("Unit ability loadout is missing."));

    public ValidationReport ValidateAuthoring()
    {
        var report = new ValidationReport();
        if (Loadout is null) report.Error($"{SceneFilePath}: unit ability loadout is missing");
        else if (Loadout.Abilities.Count == 0) report.Error($"{Loadout.ResourcePath}: ability loadout is empty");
        return report;
    }
}
