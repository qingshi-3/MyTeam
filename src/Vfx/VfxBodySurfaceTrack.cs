using Godot;

namespace TowerAutobattler.Vfx;

// Matches the current animation frame, including facing and movement, in the
// injected stage. It is optional: a standalone effect can still show its outer layers.
public partial class VfxBodySurfaceTrack : Sprite2D, IVfxTrack, IVfxSpatialTrack
{
    [Export] public float Duration { get; set; } = .55f;
    private bool _hasBody;
    private ShaderMaterial _shader = null!;

    public override void _Ready()
    {
        _shader = (ShaderMaterial)Material.Duplicate();
        Material = _shader;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        _hasBody = context.Body is not null;
        if (context.Body is not { } body) return;
        Transform = stageToInstance * body.Transform;
        Texture = body.Texture;
        Offset = body.Offset;
        Centered = body.Centered;
        FlipH = body.FlipH;
        FlipV = body.FlipV;
        // Sprite2D samples atlas UVs. Normalize against this frame instead of
        // letting the scan run over the entire animation sheet or neighboring cells.
        var region = body.Texture is AtlasTexture atlas
            ? atlas.Region : new Rect2(Vector2.Zero, body.Texture.GetSize());
        _shader.SetShaderParameter("frame_region", new Vector4(region.Position.X, region.Position.Y, region.Size.X, region.Size.Y));
    }

    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        Visible = _hasBody && age < Duration && release < 1;
        _shader.SetShaderParameter("age", age);
        _shader.SetShaderParameter("duration", Duration);
        _shader.SetShaderParameter("release", release);
        _shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
    }
}
