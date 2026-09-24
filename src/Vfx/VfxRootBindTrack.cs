using Godot;

namespace TowerAutobattler.Vfx;

// A rooted vine reveals along an authored path once, then stays planted.
// Geometry and material belong to this instance; the shared curve is immutable.
public partial class VfxRootBindTrack : MeshInstance2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public Curve2D Path { get; set; } = null!;
    [Export] public float Width { get; set; } = 35;
    [Export] public float GrowDuration { get; set; } = .35f;
    [Export] public float Delay { get; set; }
    private VfxPlaybackState? _playback;
    private ShaderMaterial _shader = null!;
    private float _releasedGrowth = -1;
    private float _lastGrowth;

    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;

    public override void _Ready()
    {
        const int segments = 80;
        var vertices = new Vector2[(segments + 1) * 2];
        var uvs = new Vector2[vertices.Length];
        var indices = new int[segments * 6];
        float length = Path.GetBakedLength();
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
                // Asset is vertical, with its thick root at the bottom.
                uvs[index] = new(side, 1 - u);
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
        arrays[(int)Godot.Mesh.ArrayType.Index] = indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
        Mesh = mesh;
        _shader = (ShaderMaterial)Material.Duplicate();
        Material = _shader;
        Visible = false;
    }

    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        float clock = _playback?.Age ?? age;
        float castSpeed = _playback?.Parameters.CastSpeed ?? 1;
        float progress = Mathf.Clamp((clock * castSpeed - Delay) / Mathf.Max(.01f, GrowDuration), 0, 1);
        // An interrupted root cannot grow new segments during its release fade.
        if (release > 0 || _playback?.Cancelled == true)
        {
            if (_releasedGrowth < 0) _releasedGrowth = _lastGrowth;
            progress = _releasedGrowth * (1 - Mathf.SmoothStep(0, 1, release));
        }
        else _lastGrowth = progress;
        Visible = progress > 0 && release < 1;
        _shader.SetShaderParameter("growth", progress);
        _shader.SetShaderParameter("age", reducedMotion ? 0 : age);
        _shader.SetShaderParameter("release", release);
        _shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
    }
}
