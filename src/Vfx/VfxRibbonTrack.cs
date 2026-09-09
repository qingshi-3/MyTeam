using Godot;

namespace TowerAutobattler.Vfx;

// Immutable authored path, instance-owned mesh/material. UV.x measures stroke
// distance; vertex color carries its tangent for the shader's trailing motion.
public partial class VfxRibbonTrack : MeshInstance2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public Curve2D Path { get; set; } = null!;
    [Export] public float Width { get; set; } = 42;
    [Export] public float Delay { get; set; }
    [Export] public float SweepDuration { get; set; } = .17f;
    [Export] public float TailDuration { get; set; } = .24f;
    private ShaderMaterial _shader = null!;
    private float _castSpeed = 1;
    public void ConfigurePlayback(VfxPlaybackState playback) => _castSpeed = playback.Parameters.CastSpeed;

    public override void _Ready()
    {
        const int segments = 64;
        var vertices = new Vector2[(segments + 1) * 2];
        var uvs = new Vector2[vertices.Length];
        var colors = new Color[vertices.Length];
        var indices = new int[segments * 6];
        var length = Path.GetBakedLength();
        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            var center = Path.SampleBaked(length * u, true);
            var tangent = (Path.SampleBaked(Mathf.Min(length, length * u + .5f), true)
                - Path.SampleBaked(Mathf.Max(0, length * u - .5f), true)).Normalized();
            var normal = new Vector2(-tangent.Y, tangent.X);
            for (int side = 0; side < 2; side++)
            {
                int index = i * 2 + side;
                vertices[index] = center + normal * Width * (side - .5f);
                uvs[index] = new(u, side);
                colors[index] = new(tangent.X * .5f + .5f, tangent.Y * .5f + .5f, 1, 1);
            }
            if (i == segments) continue;
            int v = i * 2, k = i * 6;
            indices[k] = v; indices[k + 1] = v + 1; indices[k + 2] = v + 2;
            indices[k + 3] = v + 1; indices[k + 4] = v + 3; indices[k + 5] = v + 2;
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Godot.Mesh.ArrayType.Max);
        arrays[(int)Godot.Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Godot.Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Godot.Mesh.ArrayType.Color] = colors;
        arrays[(int)Godot.Mesh.ArrayType.Index] = indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
        Mesh = mesh;
        _shader = (ShaderMaterial)Material.Duplicate();
        Material = _shader;
        _shader.SetShaderParameter("sweep_duration", SweepDuration);
        _shader.SetShaderParameter("tail_duration", TailDuration);
        Visible = false;
    }

    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        float localAge = age - Delay / _castSpeed;
        // Only the moving blade accelerates. Already-painted tail segments
        // keep their authored real-time fade, including at slow cast speeds.
        _shader.SetShaderParameter("sweep_duration", SweepDuration / _castSpeed);
        Visible = localAge >= 0 && localAge < SweepDuration / _castSpeed + TailDuration && release < 1;
        _shader.SetShaderParameter("age", Mathf.Max(0, localAge));
        _shader.SetShaderParameter("release", release);
        _shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
    }
}
