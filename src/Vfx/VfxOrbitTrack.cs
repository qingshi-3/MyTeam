using Godot;

namespace TowerAutobattler.Vfx;

// A textured arc moves on a projected orbital plane. Separate front/back
// passes give it depth around the stable shield, without sorting the body.
public partial class VfxOrbitTrack : Node2D, IVfxTrack
{
    [Export] public float Radius { get; set; } = 113;
    [Export] public float Flattening { get; set; } = .42f;
    [Export] public float PlaneRotation { get; set; } = -.3f;
    [Export] public float AngularSpeed { get; set; } = 1.8f;
    [Export] public float Phase { get; set; }
    [Export] public float ArcLength { get; set; } = 1.9f;
    [Export] public float Width { get; set; } = 15;
    [Export] public float HeadSize { get; set; } = 30;
    [Export] public Color Tint { get; set; } = new(1, .75f, .24f);
    private ShaderMaterial[] _materials = [];
    private Sprite2D _head = null!;
    private Sprite2D _headBack = null!;
    private const float DepthBlend = .3f;

    public override void _Ready()
    {
        const int segments = 64;
        var vertices = new Vector2[(segments + 1) * 2];
        var uv = new Vector2[vertices.Length];
        var indices = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            for (int side = 0; side < 2; side++)
            {
                uv[i * 2 + side] = new(i / (float)segments, side);
                // A conservative local bound also covers shader displacement.
                vertices[i * 2 + side] = new((i / (float)segments * 2 - 1) * (Radius + Width),
                    (side * 2 - 1) * (Radius + Width));
            }
            if (i == segments) continue;
            int k = i * 6, v = i * 2;
            indices[k] = v; indices[k + 1] = v + 1; indices[k + 2] = v + 2;
            indices[k + 3] = v + 1; indices[k + 4] = v + 3; indices[k + 5] = v + 2;
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.TexUV] = uv;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        _materials = new ShaderMaterial[2];
        string[] passes = ["Back", "Front"];
        for (int i = 0; i < passes.Length; i++)
        {
            var pass = GetNode<MeshInstance2D>(passes[i]);
            pass.Mesh = mesh;
            var material = (ShaderMaterial)pass.Material.Duplicate();
            pass.Material = material;
            _materials[i] = material;
            material.SetShaderParameter("back_pass", i == 0);
            material.SetShaderParameter("depth_blend", DepthBlend);
            material.SetShaderParameter("radius", Radius);
            material.SetShaderParameter("flattening", Flattening);
            material.SetShaderParameter("plane_rotation", PlaneRotation);
            material.SetShaderParameter("angular_speed", AngularSpeed);
            material.SetShaderParameter("phase", Phase);
            material.SetShaderParameter("arc_length", ArcLength);
            material.SetShaderParameter("width", Width);
            material.SetShaderParameter("tint", Tint);
        }
        _head = GetNode<Sprite2D>("Head");
        _headBack = GetNode<Sprite2D>("HeadBack");
    }

    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        foreach (var material in _materials)
        {
            material.SetShaderParameter("age", age);
            material.SetShaderParameter("release", release);
            material.SetShaderParameter("impact", impact);
            material.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
        }
        float angle = Phase + (reducedMotion ? 0 : age * AngularSpeed);
        float depth = Mathf.Sin(angle);
        _head.Position = new Vector2(Mathf.Cos(angle) * Radius, depth * Radius * Flattening).Rotated(PlaneRotation);
        var tangent = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle) * Flattening).Rotated(PlaneRotation)
            * Mathf.Sign(AngularSpeed);
        _head.Rotation = tangent.Angle();
        float front = Mathf.SmoothStep(0, 1, Mathf.Clamp((depth + DepthBlend) / (2 * DepthBlend), 0, 1));
        float envelope = Mathf.Min(age / .22f, 1) * (1 - release);
        // Two fixed layers crossfade with the band; never jump sprite Z at
        // the equator. A little transmitted light survives the shell texture.
        float opacity = envelope * Mathf.Lerp(.16f, .9f, front);
        var light = Tint.Lerp(Colors.White, .5f);
        _head.Modulate = new(light.R, light.G, light.B, opacity);
        _head.Scale = Vector2.One * HeadSize / _head.Texture.GetWidth();
        _headBack.Transform = _head.Transform;
        _headBack.Modulate = new(light.R, light.G, light.B, envelope * (1 - front) * .3f);
    }
}
