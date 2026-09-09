using Godot;

namespace TowerAutobattler.Vfx;

public enum VfxEndReason { Completed, Depleted, OwnerDefeated, ScopeEnded }

// Coordinates belong to the injected stage. No visual instance queries battle state.
public readonly record struct VfxContext(Vector2 Source, Vector2 Target, float Radius = 0,
    VfxPlaybackParameters? Playback = null, float? TravelProgress = null, Vector2? Direction = null);

// Explicit stage injection for effects whose geometry belongs to the world plane.
public interface IVfxSpatialTrack
{
    void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance);
}

public interface IVfxStage
{
    Vector2 Project(Vector2 point, bool ground);
    float UnitScale { get; }
    float RadiusPixels(float radius);
}
