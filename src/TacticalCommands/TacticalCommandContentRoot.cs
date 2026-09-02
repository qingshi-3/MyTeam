using Godot;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Content;

namespace TowerAutobattler.TacticalCommands;

[GlobalClass]
public partial class TacticalCommandContentRoot : Node
{
    [Export] public TacticalCommandDefinition Definition { get; set; } = null!;

    public CompiledTacticalCommandDefinition Resolve(CompiledContentGraph graph) =>
        graph.ResolveTacticalCommand(Definition ??
            throw new System.InvalidOperationException("Tactical-command definition is missing."));

    public ValidationReport ValidateAuthoring()
    {
        var report = new ValidationReport();
        if (Definition is null)
        {
            report.Error($"{SceneFilePath}: tactical command requires a definition");
            return report;
        }
        if (Definition.AbilityLoadout is null)
            report.Error($"{SceneFilePath}: tactical command requires an ability loadout");
        if (string.IsNullOrWhiteSpace(Definition.PrimaryAbilityId))
            report.Error($"{SceneFilePath}: tactical command requires a primary ability id");
        else if (Definition.AbilityLoadout?.Abilities is { } abilities &&
                 !abilities.Any(ability => ability is not null &&
                     ability.StableId == Definition.PrimaryAbilityId &&
                     ability.ActivationKind == AbilityActivationKind.ManualCommand))
            report.Error($"{SceneFilePath}: tactical command primary ability is missing or not manual");
        return report;
    }
}
