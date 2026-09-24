using Godot;

namespace TowerAutobattler.Vfx;

// Both portal endpoints use explicit world-plane projection. This group only
// plays authored layers; it never moves, hides, or looks up a gameplay unit.
public partial class VfxTeleportEndpointTrack : VfxTimedGroup, IVfxSpatialTrack
{
    [Export] public bool SourceEndpoint { get; set; }

    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        Position = stageToInstance * stage.Project(SourceEndpoint ? context.Source : context.Target, true);
    }
}
