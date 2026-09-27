using Godot;

namespace TowerAutobattler.Vfx;

[GlobalClass]
public partial class VfxDefinition : Resource
{
    [Export] public string StableId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string PreviewNote { get; set; } = "";
    [Export] public bool ReactsToImpact { get; set; }
    // Physical projectiles may author their own hit aftermath in the same instance.
    [Export] public bool ProjectileImpactTail { get; set; }
    [Export] public PackedScene Scene { get; set; } = null!;
    [Export] public float Duration { get; set; } = .6f;
    // Opt-in natural tail for independently living particles; scope clear stays immediate.
    [Export] public float ReleaseDuration { get; set; } = .25f;
    [Export] public bool Persistent { get; set; }
    [Export] public bool Ground { get; set; }
    [Export] public bool UsesRadius { get; set; }
    [Export] public bool FlattenGround { get; set; } = true;
    [Export] public bool StretchBetween { get; set; }
    [Export] public bool UsesWidth { get; set; }
    [Export] public bool FaceDirection { get; set; }
    [Export] public float Size { get; set; } = 110;
    [Export] public float ReferenceRadius { get; set; } = 1.5f;
    [Export] public float CastDuration { get; set; }
    [Export] public float FlightDuration { get; set; }
    [Export] public float ActionSpan { get; set; }
    [Export] public bool AtSource { get; set; }
    [Export] public SpriteFrames? PreviewFrames { get; set; }
}
