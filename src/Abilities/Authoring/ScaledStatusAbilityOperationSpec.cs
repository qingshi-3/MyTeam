using Godot;

namespace TowerAutobattler.Abilities;

/// <summary>A committed status application reading target stacks and an optional owner-team trait tier.</summary>
[GlobalClass]
public partial class ScaledStatusAbilityOperationSpec : ApplyStatusAbilityOperationSpec
{
    [Export] public int BaseStacks { get; set; } = 1;
    [Export] public string StatusId { get; set; } = string.Empty;
    [Export] public float ExistingStackRatio { get; set; }
    [Export] public float Chance { get; set; } = 1f;
    [Export] public string ChanceStatusId { get; set; } = string.Empty;
    [Export] public float ChancePerStack { get; set; }
    [Export] public string ChanceTraitId { get; set; } = string.Empty;
    [Export] public float[] ChanceByTraitTier { get; set; } = [];
    [Export] public float MaximumChance { get; set; } = 1f;
}
