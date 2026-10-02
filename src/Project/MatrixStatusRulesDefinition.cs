using Godot;

namespace TowerAutobattler.Project;

// Authoring only. Compilation copies these values; combat never mutates this resource.
[GlobalClass]
public partial class MatrixStatusRulesDefinition : Resource
{
    [ExportGroup("棘毒")]
    [Export(PropertyHint.Range, "1,3600,1")] public int PoisonDurationTicks { get; set; } = 120;
    [Export(PropertyHint.Range, "1,3600,1")] public int PoisonIntervalTicks { get; set; } = 10;
    [Export(PropertyHint.Range, "0,100000,0.1")] public float PoisonDamagePerLayer { get; set; } = 2;
    [ExportGroup("寒意")]
    [Export(PropertyHint.Range, "1,3600,1")] public int ChillDurationTicks { get; set; } = 40;
    [Export(PropertyHint.Range, "1,100,1")] public int ChillDefaultStackLimit { get; set; } = 2;
    [Export(PropertyHint.Range, "0,1,0.01")] public float ChillSlowPerLayer { get; set; } = .06f;
    [ExportGroup("余烬")]
    [Export(PropertyHint.Range, "1,3600,1")] public int EmberDurationTicks { get; set; } = 80;
    [Export(PropertyHint.Range, "1,100,1")] public int EmberStackLimit { get; set; } = 5;
}
