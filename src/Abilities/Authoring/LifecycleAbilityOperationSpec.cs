using Godot;
using TowerAutobattler.Effects;
namespace TowerAutobattler.Abilities;

public enum LifecycleAbilityKind { ReviveOwner, ConsumeAlly, RaiseCorpse, Charm }

[GlobalClass]
public partial class LifecycleAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public LifecycleAbilityKind Kind { get; set; }
    [Export] public EffectTargetQuerySpec? TargetQuery { get; set; }
    [Export] public int DelayTicks { get; set; } = 20;
    [Export] public int DurationTicks { get; set; } = 60;
    [Export] public float HealthRatio { get; set; } = .4f;
    [Export] public float AttackTransferRatio { get; set; } = .25f;
    [Export] public string SummonContentId { get; set; } = "";
    [Export] public int MaximumLivingSummons { get; set; } = 4;
}
