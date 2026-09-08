namespace TowerAutobattler.Vfx;

// Sprite, ribbon and particle layers all sample the owning stage's clock.
public interface IVfxTrack
{
    void Sample(float age, bool sustained, float release, float impact, bool reducedMotion);
}

// Projected endpoints are supplied explicitly by the instance; travel tracks
// never find units or a stage through hidden parent paths.
public interface IVfxSourceTrack
{
    void SetSource(Godot.Vector2 localSource, float localUnitScale);
}
