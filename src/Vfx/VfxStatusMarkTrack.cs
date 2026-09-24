using Godot;

namespace TowerAutobattler.Vfx;

// A compact head marker; motion uses the shared effect clock and never restarts on status refresh.
public partial class VfxStatusMarkTrack : Node2D, IVfxTrack
{
    private Vector2 _origin;
    public override void _Ready() => _origin = Position;
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        float intro = Mathf.Clamp(age / .15f, 0, 1);
        float pop = 1 + 2.7f * Mathf.Pow(intro - 1, 3) + 1.7f * Mathf.Pow(intro - 1, 2);
        float pulse = reducedMotion ? 1 : 1 + .035f * Mathf.Sin(age * 5);
        Scale = Vector2.One * (reducedMotion ? 1 : Mathf.Lerp(.55f, 1, pop) * pulse);
        Position = _origin + (reducedMotion ? Vector2.Zero : new Vector2(0, -9 * Mathf.Sin(age * 3.2f)));
        Rotation = reducedMotion ? 0 : .035f * Mathf.Sin(age * 4);
        Modulate = new Color(1, 1, 1, (1 - release) * Mathf.Min(age / .045f, 1));
    }
}
