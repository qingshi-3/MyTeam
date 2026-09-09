using Godot;

namespace TowerAutobattler.Vfx;

[GlobalClass]
public partial class VfxDefinition : Resource
{
    [Export] public string StableId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string PreviewNote { get; set; } = "";
    [Export] public bool ReactsToImpact { get; set; }
    [Export] public PackedScene Scene { get; set; } = null!;
    [Export] public float Duration { get; set; } = .6f;
    [Export] public bool Persistent { get; set; }
    [Export] public bool Ground { get; set; }
    [Export] public bool UsesRadius { get; set; }
    [Export] public bool FlattenGround { get; set; } = true;
    [Export] public bool StretchBetween { get; set; }
    [Export] public float Size { get; set; } = 110;
    [Export] public float ReferenceRadius { get; set; } = 1.5f;
    [Export] public float CastDuration { get; set; }
    [Export] public float FlightDuration { get; set; }
    [Export] public float ActionSpan { get; set; }
}
