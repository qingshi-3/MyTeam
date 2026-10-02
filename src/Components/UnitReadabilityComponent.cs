using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class UnitReadabilityComponent : Node2D
{
    [Export(PropertyHint.Range, "0,2,0.25")] public float OutlineWidth { get; set; } = 1f;
    [Export] public bool ShadowEnabled { get; set; } = true;
    [Export] public bool HitFeedbackEnabled { get; set; } = true;
    [Export] public bool ReducedMotion { get; set; }
    [Export] public float HitDuration { get; set; } = .075f;
    [Export] public float HitCooldown { get; set; } = .14f;
    private AnimatedSprite2D? _sprite;
    private ShaderMaterial? _material;
    private Polygon2D _shadow = null!;
    private float _remaining;
    private float _cooldown;
    public bool Paused { get; set; }
    public float Speed { get; set; } = 1f;
    public float HitAmount { get; private set; }

    public override void _Ready()
    {
        _shadow = GetNode<Polygon2D>("ContactShadow");
        SetProcess(false);
    }

    public void BindSprite(AnimatedSprite2D sprite)
    {
        _sprite = sprite;
        // Never mutate the authored/shared material or the shared SpriteFrames.
        _material = (ShaderMaterial)sprite.Material.Duplicate();
        sprite.Material = _material;
        sprite.FrameChanged += RefreshFrame;
        sprite.AnimationChanged += RefreshFrame;
        RefreshFrame();
    }

    public void Bind(UnitDefinition definition, int team)
    {
        _shadow.Scale = Vector2.One * Mathf.Clamp(definition.BodyRadius / .32f, .45f, 3.5f);
        Configure(OutlineWidth, ShadowEnabled, HitFeedbackEnabled);
    }

    public void Configure(float outlineWidth, bool shadow, bool feedback)
    {
        OutlineWidth = Mathf.Clamp(outlineWidth, 0, 2);
        ShadowEnabled = shadow;
        HitFeedbackEnabled = feedback;
        _material?.SetShaderParameter("outline_width", OutlineWidth);
        _shadow.Visible = shadow;
        if (!feedback) ResetFeedback();
    }

    // The owning stage explicitly enables the shared procedural cloud lighting.
    // Sampling is cosmetic and never changes the sprite's pose, HUD or game stats.
    public void BindEnvironmentLighting()
    {
        _material?.SetShaderParameter("environment_enabled", true);
    }

    public void SetEnvironmentLighting(Vector2 uv, float elapsed)
    {
        _material?.SetShaderParameter("environment_uv", uv);
        _material?.SetShaderParameter("environment_elapsed", elapsed);
    }

    private void RefreshFrame()
    {
        if (_sprite?.SpriteFrames is null || _material is null) return;
        var texture = _sprite.SpriteFrames.GetFrameTexture(_sprite.Animation, _sprite.Frame);
        var bounds = new Vector4(0, 0, 1, 1);
        if (texture is AtlasTexture atlas && atlas.Atlas is not null)
        {
            var size = atlas.Atlas.GetSize();
            var region = atlas.Region;
            if (region.Size == Vector2.Zero) region.Size = size;
            bounds = new(region.Position.X / size.X, region.Position.Y / size.Y,
                region.End.X / size.X, region.End.Y / size.Y);
        }
        _material.SetShaderParameter("frame_uv", bounds);
        _material.SetShaderParameter("rect_center", _sprite.Offset +
            (_sprite.Centered ? Vector2.Zero : texture.GetSize() * .5f));
    }

    public void PresentHit()
    {
        if (!HitFeedbackEnabled || _cooldown > 0 || _material is null) return;
        _remaining = Mathf.Max(.001f, HitDuration);
        _cooldown = Mathf.Max(_remaining, HitCooldown);
        SetHitAmount(ReducedMotion ? .10f : .38f);
        SetProcess(true);
    }

    public void Advance(float seconds)
    {
        _remaining = Mathf.Max(0, _remaining - Mathf.Max(0, seconds));
        _cooldown = Mathf.Max(0, _cooldown - Mathf.Max(0, seconds));
        SetHitAmount(_remaining / Mathf.Max(.001f, HitDuration) * (ReducedMotion ? .10f : .38f));
        if (_cooldown <= 0) SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (!Paused) Advance((float)delta * Speed);
    }

    public void ResetFeedback()
    {
        _remaining = _cooldown = 0;
        SetHitAmount(0);
        SetProcess(false);
    }

    private void SetHitAmount(float value)
    {
        HitAmount = value;
        _material?.SetShaderParameter("hit_amount", value);
    }

    public override void _ExitTree()
    {
        if (_sprite is not null && GodotObject.IsInstanceValid(_sprite))
        {
            _sprite.FrameChanged -= RefreshFrame;
            _sprite.AnimationChanged -= RefreshFrame;
        }
    }
}
