using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class MatrixReactionParameters : Resource
{
    [Export] public MatrixEventKind Event { get; set; }
    [Export] public MatrixRelation SourceRelation { get; set; }
    [Export] public MatrixRelation TargetRelation { get; set; }
    [Export] public float MinimumEventValue { get; set; }
    [Export] public int CooldownTicks { get; set; }
    [Export] public int Every { get; set; } = 1;
    [Export] public bool PerTargetCooldown { get; set; }
    [Export] public bool OncePerTarget { get; set; }
    [Export] public bool OncePerSource { get; set; }
    [Export] public bool OncePerAction { get; set; }
    [Export] public bool OncePerBattle { get; set; }
    [Export] public bool ActiveSkillOnly { get; set; }
    [Export] public bool TemporaryOnly { get; set; }
    [Export] public bool LivingOwnerRequired { get; set; } = true;
}
