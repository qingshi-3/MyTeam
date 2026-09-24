using Godot;

namespace TowerAutobattler.Vfx;

// Full circular weapon path; the containing SweepPlane applies world projection.
// Each angular segment owns its real-time tail after the blade passes it.
public partial class VfxWhirlwindTrack : MeshInstance2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public float SweepDuration { get; set; } = .66f;
    [Export] public float TailDuration { get; set; } = .20f;
    [Export] public float InnerRadiusRatio { get; set; } = .65f;
    private VfxPlaybackState? _playback;
    private ShaderMaterial _shader = null!;
    private float _previousAge;
    private float _cutoff = -1;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public override void _Ready()
    {
        const int angles = 160, radial = 6;
        var vertices = new Vector2[(angles + 1) * (radial + 1)];
        var uvs = new Vector2[vertices.Length];
        var indices = new int[angles * radial * 6];
        for (int a = 0; a <= angles; a++)
        for (int r = 0; r <= radial; r++)
        {
            float u = a / (float)angles, v = r / (float)radial;
            int i = a * (radial + 1) + r;
            vertices[i] = Vector2.FromAngle(-Mathf.Pi / 2 + u * Mathf.Tau)
                * 128 * Mathf.Lerp(InnerRadiusRatio, 1, v);
            uvs[i] = new(u, v);
            if (a == angles || r == radial) continue;
            int k = (a * radial + r) * 6;
            indices[k] = i; indices[k + 1] = i + 1; indices[k + 2] = i + radial + 1;
            indices[k + 3] = i + 1; indices[k + 4] = i + radial + 2; indices[k + 5] = i + radial + 1;
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
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        float speed = _playback?.Parameters.CastSpeed ?? 1;
        if (_playback?.Cancelled == true && _cutoff < 0) _cutoff = _previousAge;
        _previousAge = age;
        Visible = age < SweepDuration / speed + TailDuration && release < 1 && _cutoff != 0;
        if (!Visible) return;
        _shader.SetShaderParameter("age", age);
        _shader.SetShaderParameter("sweep_duration", SweepDuration / speed);
        _shader.SetShaderParameter("tail_duration", TailDuration);
        _shader.SetShaderParameter("cutoff_time", _cutoff);
        _shader.SetShaderParameter("release", release);
        _shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
    }
}
