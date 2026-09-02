using Godot;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class SummonAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public AbilitySummonProfile Profile { get; set; }
    [Export] public int Count { get; set; } = 1;
    [Export] public float HealthMultiplier { get; set; } = 1f;
    [Export] public float DamageMultiplier { get; set; } = 1f;
    [Export] public int MaximumLivingTemporaryUnits { get; set; }
    [Export] public bool RequireAtLeastOne { get; set; } = true;
    [Export] public string SummonContentId { get; set; } = string.Empty;
}
