using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// Authored sprites projected from the same endpoints/radius as combat. No game state here.
public partial class VfxChargedLineTrack : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSpatialTrack
{
    [Export] public bool Beam { get; set; }
    [Export] public Sprite2D Strip { get; set; } = null!;
    [Export] public Sprite2D SourceGlow { get; set; } = null!;
    [Export] public Node2D Motes { get; set; } = null!;
    [Export] public Color Tint { get; set; } = new(1, .55f, .16f);
    private VfxPlaybackState _playback = null!;
    private Sprite2D[] _motes = [];
    private ShaderMaterial _stripMaterial = null!;
    private Transform2D _toLocal;
    private Vector2 _source;
    private float _unit;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public override void _Ready()
    {
        _stripMaterial = (ShaderMaterial)Strip.Material.Duplicate();
        Strip.Material = _stripMaterial;
        _stripMaterial.SetShaderParameter("tint", Tint);
        _stripMaterial.SetShaderParameter("beam", Beam);
        _motes = Motes.GetChildren().OfType<Sprite2D>().ToArray();
        SourceGlow.Modulate = Tint;
        foreach (var mote in _motes) mote.Modulate = Tint;
    }
    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        _toLocal = stageToInstance;
        _unit = stage.UnitScale;
        var direction = (context.Target - context.Source).Normalized();
        if (direction.IsZeroApprox()) direction = Vector2.Right;
        var normal = new Vector2(-direction.Y, direction.X);
        var radius = context.Radius > 0 ? context.Radius : .18f;
        var start = stage.Project(context.Source, !Beam);
        var end = stage.Project(context.Target, !Beam);
        var width = (stage.Project(context.Source + normal * radius, !Beam) - start) * 2;
        Strip.Transform = stageToInstance * new Transform2D((end - start) / 256, width * 1.8f / 256, (start + end) / 2);
        _stripMaterial.SetShaderParameter("length_pixels", start.DistanceTo(end) / Mathf.Max(.001f, _unit));
        _source = stage.Project(context.Source, false);
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        var p = Mathf.Clamp(_playback.Age / Mathf.Max(.01f, _playback.CastDuration), 0, 1);
        Visible = release <= 0 && (Beam || _playback.FiredAt is null);
        if (!Visible) return;
        _stripMaterial.SetShaderParameter("progress", Beam ? Mathf.Clamp(age / .35f, 0, 1) : p);
        _stripMaterial.SetShaderParameter("age", age);
        _stripMaterial.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
        var glow = Beam ? Mathf.Pow(Mathf.Max(0, 1 - age / .3f), 2) : .18f + .82f * p * p;
        SourceGlow.Transform = _toLocal * new Transform2D(0, Vector2.One * _unit * (Beam ? .55f : .24f + .32f * p), 0, _source);
        SourceGlow.Modulate = new Color(Tint.R, Tint.G, Tint.B, glow);
        for (var i = 0; i < _motes.Length; i++)
        {
            var mote = _motes[i];
            mote.Visible = !Beam && !reducedMotion && p > .15f && p < .99f;
            var cycle = Mathf.PosMod(p * 1.8f + i / (float)_motes.Length, 1);
            var offset = Vector2.FromAngle(i * Mathf.Tau / _motes.Length + .25f) * (1 - cycle) * 42 * _unit;
            mote.Transform = _toLocal * new Transform2D(0, Vector2.One * _unit * .13f, 0, _source + offset);
            mote.Modulate = new Color(Tint.R, Tint.G, Tint.B, Mathf.Sin(cycle * Mathf.Pi) * p);
        }
    }
}
