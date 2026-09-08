using Godot;

namespace TowerAutobattler.Vfx;

// Authored layers supply texture, timing and motion. The shared clock also drives shaders.
public partial class VfxSpriteTrack : Sprite2D, IVfxTrack
{
    [Export] public float Delay { get; set; }
    [Export] public float Duration { get; set; } = .5f;
    [Export] public float StartScale { get; set; } = .7f;
    [Export] public float EndScale { get; set; } = 1.15f;
    [Export] public float Spin { get; set; }
    [Export] public bool ShaderLifecycle { get; set; }
    [Export] public float SweepRadians { get; set; }
    [Export] public Vector2 Travel { get; set; }
    [Export] public float MotionDuration { get; set; } = .18f;
    [Export] public float Opacity { get; set; } = 1;
    private Vector2 _baseScale;
    private float _baseRotation;
    private Vector2 _basePosition;
    public override void _Ready()
    {
        _baseScale = Scale * (256f / Texture.GetWidth());
        _baseRotation = Rotation;
        _basePosition = Position;
        if (Material is not null) Material = (Material)Material.Duplicate();
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        float p = Mathf.Clamp((age - Delay) / Duration, 0, 1);
        var localAge = Mathf.Max(0, age - Delay);
        var sweep = 1 - Mathf.Pow(1 - Mathf.Clamp(localAge / Mathf.Max(.01f, MotionDuration), 0, 1), 3);
        Visible = age >= Delay && (sustained || p < 1);
        var envelope = sustained ? Mathf.Min(age / .15f, 1) : Mathf.Min(p * 12, 1) * Mathf.Pow(1 - p, .7f);
        var size = sustained ? 1 + (reducedMotion ? 0 : .025f * Mathf.Sin(age * 3)) : Mathf.Lerp(StartScale, EndScale, p);
        Scale = _baseScale * size * (1 + release * .25f);
        Rotation = _baseRotation + (reducedMotion ? 0 : localAge * Spin + (sweep - 1) * SweepRadians);
        Position = _basePosition + (reducedMotion ? Vector2.Zero : Travel * sweep);
        Modulate = ShaderLifecycle ? new Color(1, 1, 1, Opacity) :
            new Color(1 + impact, 1 + impact, 1 + impact, envelope * (1 - release));
        if (Material is ShaderMaterial shader)
        {
            // Local time preserves staggered layers; never use shader TIME, which
            // would keep moving when the battle/preview clock is paused.
            shader.SetShaderParameter("age", Mathf.Max(0, age - Delay));
            shader.SetShaderParameter("progress", p);
            shader.SetShaderParameter("release", Mathf.Clamp(release, 0, 1));
            shader.SetShaderParameter("impact", impact);
            shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
            shader.SetShaderParameter("sustained", sustained);
        }
    }
}
