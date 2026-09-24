using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Vfx;

// A bent funnel is rendered as two authored surface passes. Texture circulation,
// the silhouette and the orbiting particles share one owner clock.
public partial class VfxVortexTrack : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSpatialTrack
{
    [Export] public float Height { get; set; } = 178;
    [Export] public float Radius { get; set; } = 100;
    [Export] public PackedScene MoteScene { get; set; } = null!;
    [Export] public int MoteCount { get; set; } = 28;
    private VfxPlaybackState? _playback;
    private ShaderMaterial[] _materials = [];
    private Sprite2D _ground = null!;
    private ShaderMaterial _groundMaterial = null!;
    private readonly List<(Sprite2D Sprite, float Phase, float Size)> _motes = [];
    private Vector2 _groundX = Vector2.Right;
    private Vector2 _groundY = new(0, .6f);
    private bool _started;
    private float _previousMotionTime;
    private float? _cancelMotionTime;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;

    public override void _Ready()
    {
        const int columns = 32, rows = 40;
        var vertices = new Vector2[(columns + 1) * (rows + 1)];
        var uvs = new Vector2[vertices.Length];
        var indices = new int[columns * rows * 6];
        for (int y = 0; y <= rows; y++)
        for (int x = 0; x <= columns; x++)
        {
            int v = y * (columns + 1) + x;
            uvs[v] = new(x / (float)columns, y / (float)rows);
            // Conservative bounds include the projected cap, bend and floating dust.
            vertices[v] = new((uvs[v].X * 2 - 1) * (Radius + 30),
                Mathf.Lerp(-Height - Radius, Radius, uvs[v].Y));
            if (x == columns || y == rows) continue;
            int k = (y * columns + x) * 6;
            indices[k] = v; indices[k + 1] = v + 1; indices[k + 2] = v + columns + 1;
            indices[k + 3] = v + 1; indices[k + 4] = v + columns + 2; indices[k + 5] = v + columns + 1;
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        var back = GetNode<MeshInstance2D>("Back");
        var front = GetNode<MeshInstance2D>("Front");
        back.Mesh = front.Mesh = mesh;
        back.Material = (Material)back.Material.Duplicate();
        front.Material = (Material)front.Material.Duplicate();
        _materials = [(ShaderMaterial)back.Material, (ShaderMaterial)front.Material];
        _ground = GetNode<Sprite2D>("GroundSkirt");
        _ground.Material = _groundMaterial = (ShaderMaterial)_ground.Material.Duplicate();
        using var rng = new RandomNumberGenerator { Seed = 90217 };
        int count = Mathf.Clamp(Mathf.RoundToInt(MoteCount * (_playback?.DensityAtBirth ?? 1)), 8, 48);
        for (int i = 0; i < count; i++)
        {
            var sprite = MoteScene.Instantiate<Sprite2D>();
            AddChild(sprite);
            sprite.Visible = false;
            _motes.Add((sprite, (i + rng.RandfRange(.1f, .8f)) / count, rng.RandfRange(7, 16)));
        }
    }

    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        var origin = stage.Project(context.Target, true);
        float unit = Mathf.Max(.001f, stage.RadiusPixels(1));
        _groundX = (stage.Project(context.Target + Vector2.Right, true) - origin) / unit;
        _groundY = (stage.Project(context.Target + Vector2.Down, true) - origin) / unit;
        _ground.Transform = new Transform2D(_groundX * .8f, _groundY * .8f, Vector2.Zero);
    }

    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        if (!_started && _playback?.Cancelled == true)
        {
            Visible = false;
            return;
        }
        _started |= age > 0;
        float realAge = _playback?.Age ?? age;
        float cast = _playback?.Parameters.CastSpeed ?? 1;
        float entry = Mathf.SmoothStep(0, .32f / cast, realAge);
        float motionTime = reducedMotion ? .7f : age;
        if (_playback?.Cancelled == true) _cancelMotionTime ??= _previousMotionTime;
        _previousMotionTime = motionTime;
        _groundMaterial.SetShaderParameter("age", motionTime);
        _groundMaterial.SetShaderParameter("entry", entry);
        _groundMaterial.SetShaderParameter("release", release);
        _groundMaterial.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
        foreach (var shader in _materials)
        {
            shader.SetShaderParameter("age", motionTime);
            shader.SetShaderParameter("entry", entry);
            shader.SetShaderParameter("release", release);
            shader.SetShaderParameter("height", Height);
            shader.SetShaderParameter("radius", Radius);
            shader.SetShaderParameter("ground_x", _groundX);
            shader.SetShaderParameter("ground_y", _groundY);
            shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
        }
        foreach (var (sprite, phase, size) in _motes)
        {
            // A wrap is a new birth. Release may finish the current revolution,
            // but a fast motion clock must not repopulate the funnel's foot.
            if (_cancelMotionTime is { } stopped &&
                Mathf.Floor(motionTime * .42f + phase) > Mathf.Floor(stopped * .42f + phase))
            {
                sprite.Visible = false;
                continue;
            }
            float climb = Mathf.PosMod(motionTime * .42f + phase, 1);
            float angle = phase * Mathf.Tau + motionTime * 5.6f + climb * 4;
            float radius = Mathf.Lerp(28, Radius + 7, Mathf.Pow(climb, 1.25f));
            var bend = new Vector2(Mathf.Sin(climb * 5.4f + motionTime * 1.1f) * 12 * climb, 0);
            sprite.Position = (_groundX * Mathf.Cos(angle) + _groundY * Mathf.Sin(angle)) * radius
                + new Vector2(0, -Height * climb) + bend;
            float envelope = Mathf.Sin(climb * Mathf.Pi) * entry * (1 - release);
            sprite.Visible = envelope > .001f;
            sprite.Scale = Vector2.One * size / sprite.Texture.GetWidth() * (_playback?.ParticleScale ?? 1);
            sprite.Modulate = new Color(.55f, .88f, .91f, envelope * .7f);
            // Dust is low contrast; a soft depth weight avoids a hard mid-orbit switch.
            sprite.Modulate *= new Color(1, 1, 1, .65f + .35f * Mathf.Sin(angle));
        }
    }
}
