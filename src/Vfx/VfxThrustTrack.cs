using Godot;

namespace TowerAutobattler.Vfx;

// The owner plane supplies source and locked heading. Shader geometry advances
// a narrow spear head while already-traversed axial segments fade independently.
public partial class VfxThrustTrack : MeshInstance2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public float Delay { get; set; } = .045f;
    [Export] public float SweepDuration { get; set; } = .18f;
    [Export] public float TailDuration { get; set; } = .36f;
    [Export] public float HalfWidth { get; set; } = 24;
    private VfxPlaybackState? _playback;
    private ShaderMaterial _shader = null!;
    private float _previousTime = -1;
    private float _cutoff = -1;
    private bool _started;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public override void _Ready()
    {
        const int segments = 64;
        var vertices = new Vector2[(segments + 1) * 2];
        var uvs = new Vector2[vertices.Length];
        var indices = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            vertices[i * 2] = new Vector2(u * 128, -HalfWidth);
            vertices[i * 2 + 1] = new Vector2(u * 128, HalfWidth);
            uvs[i * 2] = new Vector2(u, 0);
            uvs[i * 2 + 1] = new Vector2(u, 1);
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
        Material = _shader = (ShaderMaterial)Material.Duplicate();
        Visible = false;
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        float speed = _playback?.Parameters.CastSpeed ?? 1;
        float time = age - Delay / speed;
        if (_playback?.Cancelled == true && _cutoff < 0) _cutoff = Mathf.Max(0, _previousTime);
        _previousTime = time;
        Visible = time >= 0 && time < SweepDuration / speed + TailDuration && release < 1
            && (_started || _playback?.Cancelled != true);
        if (!Visible) return;
        _started = true;
        _shader.SetShaderParameter("age", time);
        _shader.SetShaderParameter("sweep_duration", SweepDuration / speed);
        _shader.SetShaderParameter("tail_duration", TailDuration);
        _shader.SetShaderParameter("cutoff_time", _cutoff);
        _shader.SetShaderParameter("release", release);
        _shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
    }
}
