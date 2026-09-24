using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// Two explicit spatial meanings share the same textured energy transport:
// a real target-to-source link, or a single owner's surrounding energy gather.
// The bounded motes are visual only and cannot restore stats or deal damage.
public partial class VfxInwardFlowTrack : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSpatialTrack
{
    public enum FlowKind { TargetToSource, LocalGather }
    [Export] public FlowKind Mode { get; set; }
    [Export] public PackedScene MoteScene { get; set; } = null!;
    [Export] public int Amount { get; set; } = 18;
    [Export] public float FlowDuration { get; set; } = .68f;
    [Export] public float GatherRadius { get; set; } = 58;
    [Export] public float RibbonWidth { get; set; } = 11;
    [Export] public Color Tint { get; set; } = new(.72f, .13f, .35f);
    private VfxPlaybackState? _playback;
    private MeshInstance2D[] _strands = [];
    private ShaderMaterial[] _materials = [];
    private Sprite2D _destination = null!;
    private Sprite2D _origin = null!;
    private readonly List<Sprite2D> _motes = [];
    private Vector2 _start;
    private Vector2 _end;
    private float _unit = 1;
    private bool _spatialReady;
    private bool _hasLength;
    private float _cancelAge = -1;
    private float _lastClock;

    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;

    public override void _Ready()
    {
        _strands = GetChildren().OfType<MeshInstance2D>().ToArray();
        _materials = _strands.Select(strand =>
        {
            var shader = (ShaderMaterial)strand.Material.Duplicate();
            strand.Material = shader;
            strand.Mesh = new ArrayMesh();
            return shader;
        }).ToArray();
        _destination = GetNode<Sprite2D>("DestinationGlow");
        _origin = GetNode<Sprite2D>("OriginGlow");
        int count = Mathf.Clamp(Mathf.RoundToInt(Amount * (_playback?.DensityAtBirth ?? 1)), 4, 36);
        for (int i = 0; i < count; i++)
        {
            var mote = MoteScene.Instantiate<Sprite2D>();
            AddChild(mote);
            mote.Visible = false;
            _motes.Add(mote);
        }
    }

    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        var target = stageToInstance * (context.TargetDisplayPosition ?? stage.Project(context.Target, false));
        var start = target;
        var end = Mode == FlowKind.TargetToSource ? stageToInstance * stage.Project(context.Source, false) : target;
        float unit = stageToInstance.BasisXform(Vector2.Right * stage.UnitScale).Length();
        if (_spatialReady && start.IsEqualApprox(_start) && end.IsEqualApprox(_end) && Mathf.IsEqualApprox(unit, _unit)) return;
        _spatialReady = true;
        _start = start; _end = end; _unit = unit;
        _hasLength = Mode == FlowKind.LocalGather || _start.DistanceSquaredTo(_end) > .01f;
        for (int strand = 0; strand < _strands.Length; strand++) BuildStrand(strand);
    }

    private Vector2 PositionAt(float u, int strand)
    {
        if (Mode == FlowKind.LocalGather)
        {
            float angle = Mathf.Tau * strand / _strands.Length - .45f;
            var radial = Vector2.FromAngle(angle);
            var tangent = new Vector2(-radial.Y, radial.X);
            var relative = radial * GatherRadius * (1 - u)
                + tangent * (Mathf.Sin(u * Mathf.Pi) * 22 * (1 - u));
            return _end + relative * new Vector2(1, .72f) * _unit;
        }
        var direction = _end - _start;
        var normal = new Vector2(-direction.Y, direction.X).Normalized();
        float bend = (strand - (_strands.Length - 1) * .5f) * 10 * _unit;
        return _start.Lerp(_end, u) + normal * Mathf.Sin(Mathf.Pi * u) * bend;
    }

    private void BuildStrand(int strand)
    {
        const int segments = 40;
        var vertices = new Vector2[(segments + 1) * 2];
        var uvs = new Vector2[vertices.Length];
        var indices = new int[segments * 6];
        float width = RibbonWidth * _unit * (_playback?.Parameters.WidthScale ?? 1);
        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            var center = PositionAt(u, strand);
            var tangent = (PositionAt(Mathf.Min(1, u + .005f), strand) - PositionAt(Mathf.Max(0, u - .005f), strand)).Normalized();
            var normal = new Vector2(-tangent.Y, tangent.X);
            for (int side = 0; side < 2; side++)
            {
                int index = i * 2 + side;
                vertices[index] = center + normal * width * (side - .5f);
                uvs[index] = new Vector2(u, side);
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
        var mesh = (ArrayMesh)_strands[strand].Mesh;
        mesh.ClearSurfaces();
        mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
    }

    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        float sinceFired = _playback?.Since(VfxStartCue.Fired) ?? age;
        bool local = Mode == FlowKind.LocalGather;
        // Continuous transport may change speed without changing ownership;
        // a one-shot gather retains the authored arrival-to-glow schedule.
        float clock = sinceFired * (local ? 1 : _playback?.Parameters.MotionSpeed ?? 1);
        bool active = sinceFired >= 0 && _spatialReady && _hasLength && release < 1;
        if ((_playback?.Cancelled == true || release > 0) && _cancelAge < 0) _cancelAge = _lastClock;
        if (_cancelAge >= 0 && _lastClock <= 0) active = false;
        Visible = active;
        if (!active) return;
        float duration = Mathf.Max(.05f, FlowDuration);
        float intro = Mathf.SmoothStep(0, 1, Mathf.Clamp(sinceFired / .12f, 0, 1));
        float naturalFade = local ? 1 - Mathf.SmoothStep(.60f, 1.0f, sinceFired) : 1;
        for (int i = 0; i < _strands.Length; i++)
        {
            _materials[i].SetShaderParameter("age", clock);
            _materials[i].SetShaderParameter("flow_duration", duration);
            _materials[i].SetShaderParameter("release", release);
            _materials[i].SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
            _materials[i].SetShaderParameter("local_gather", local);
            _materials[i].SetShaderParameter("strand_phase", i * .13f);
            _materials[i].SetShaderParameter("tint", Tint);
        }
        for (int i = 0; i < _motes.Count; i++)
        {
            var mote = _motes[i];
            if (_cancelAge >= 0 && !mote.Visible) continue;
            float phase = (i + .3f) / _motes.Count;
            float p = local ? (clock - phase * .15f) / duration : Mathf.PosMod(clock / duration + phase, 1);
            if (!local && _cancelAge >= 0)
                p = Mathf.PosMod(_cancelAge / duration + phase, 1) + (clock - _cancelAge) / duration;
            mote.Visible = !reducedMotion && p >= 0 && p < 1;
            if (!mote.Visible) continue;
            int strand = i % _strands.Length;
            mote.Position = PositionAt(p, strand);
            mote.Rotation = (PositionAt(Mathf.Min(1, p + .015f), strand) - PositionAt(Mathf.Max(0, p - .015f), strand)).Angle();
            float size = (14 + (i % 4) * 3) * _unit * (_playback?.ParticleScale ?? 1);
            mote.Scale = Vector2.One * size / mote.Texture.GetWidth();
            float edge = Mathf.SmoothStep(0, .08f, p) * (1 - Mathf.SmoothStep(.83f, 1, p));
            mote.Modulate = new Color(Tint.R, Tint.G, Tint.B, edge * intro * (1 - release) * .85f);
        }
        _destination.Position = _end;
        _origin.Position = _start;
        _origin.Visible = !local;
        float pulse = reducedMotion ? .7f : .72f + .28f * Mathf.Sin(clock * 8);
        float peak = local ? Mathf.Exp(-Mathf.Pow((sinceFired - .73f) / .17f, 2)) : pulse;
        _destination.Scale = Vector2.One * (local ? 65 : 48) * _unit / _destination.Texture.GetWidth();
        _destination.Modulate = new Color(Tint.R, Tint.G, Tint.B, peak * intro * (1 - release) * naturalFade);
        _origin.Scale = Vector2.One * 34 * _unit / _origin.Texture.GetWidth();
        _origin.Modulate = new Color(Tint.R, Tint.G, Tint.B, intro * .38f * (1 - release));
        _lastClock = clock;
    }
}
