using Godot;

namespace TowerAutobattler.Vfx;

// Immutable 180-degree annular surface. UV.x is angular distance and UV.y
// radial width; the shader moves the blade front, not the entire texture.
public partial class VfxSectorTrack : MeshInstance2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public float SweepDuration { get; set; } = .3f;
    [Export] public float TailDuration { get; set; } = .24f;
    [Export] public float InnerRadiusRatio { get; set; } = .28f;
    private ShaderMaterial _shader = null!;
    private float _castSpeed = 1;
    public void ConfigurePlayback(VfxPlaybackState playback) => _castSpeed = playback.Parameters.CastSpeed;
    public override void _Ready()
    {
        const int angles = 96, radial = 6;
        var vertices = new Vector2[(angles + 1) * (radial + 1)];
        var uv = new Vector2[vertices.Length];
        var indices = new int[angles * radial * 6];
        for (int a = 0; a <= angles; a++)
        for (int r = 0; r <= radial; r++)
        {
            float u = a / (float)angles, v = r / (float)radial;
            int i = a * (radial + 1) + r;
            vertices[i] = Vector2.FromAngle(-Mathf.Pi / 2 + u * Mathf.Pi) * 128 * Mathf.Lerp(InnerRadiusRatio, 1, v);
            uv[i] = new(u, v);
            if (a == angles || r == radial) continue;
            int k = (a * radial + r) * 6;
            indices[k] = i; indices[k+1] = i+1; indices[k+2] = i+radial+1;
            indices[k+3] = i+1; indices[k+4] = i+radial+2; indices[k+5] = i+radial+1;
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Godot.Mesh.ArrayType.Max);
        arrays[(int)Godot.Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Godot.Mesh.ArrayType.TexUV] = uv;
        arrays[(int)Godot.Mesh.ArrayType.Index] = indices;
        var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
        Mesh = mesh;
        _shader = (ShaderMaterial)Material.Duplicate(); Material = _shader;
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        Visible = age < SweepDuration / _castSpeed + TailDuration && release < 1;
        _shader.SetShaderParameter("age", age);
        _shader.SetShaderParameter("sweep_duration", SweepDuration / _castSpeed);
        _shader.SetShaderParameter("tail_duration", TailDuration);
        _shader.SetShaderParameter("release", release);
        _shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
    }
}
