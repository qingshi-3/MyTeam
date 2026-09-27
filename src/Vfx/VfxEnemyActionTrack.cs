using System;
using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// Authored nodes provide shapes; this track only maps supplied battle geometry and clock.
public partial class VfxEnemyActionTrack : Node2D, IVfxTrack, IVfxSpatialTrack, IVfxPlaybackTrack
{
    public enum Shape { Cone, Blade, Swap, Fissure, RockRise, Shell, Claw, Acid }
    [Export] public Shape Kind { get; set; }
    [Export] public Sprite2D Surface { get; set; } = null!;
    [Export] public Sprite2D? Other { get; set; }
    [Export] public Line2D? Link { get; set; }
    [Export] public Node2D? Pieces { get; set; }
    [Export] public Node2D? Details { get; set; }
    [Export] public VfxMotePool? Motes { get; set; }
    [Export] public RockBarrierVisual? Barrier { get; set; }
    private Node2D[] _pieces = [];
    private ShaderMaterial? _material;
    private VfxContext _context;
    private IVfxStage _stage = null!;
    private Transform2D _inverse;
    private float _age;
    private float _previousShellAge = -.001f, _shellStoppedAt = float.PositiveInfinity;
    private VfxPlaybackState? _playback;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback=playback;
    public override void _Ready()
    {
        if (Surface.Material is ShaderMaterial material) Surface.Material = _material = (ShaderMaterial)material.Duplicate();
        _pieces = Pieces?.GetChildren().OfType<Node2D>().ToArray() ?? [];
        foreach(var sprite in _pieces.OfType<Sprite2D>().Concat(Details?.GetChildren().OfType<Sprite2D>() ?? []))
            if(sprite.Material is ShaderMaterial shader)sprite.Material=(ShaderMaterial)shader.Duplicate();
        if(Other?.Material is ShaderMaterial other)Other.Material=(ShaderMaterial)other.Duplicate();
    }
    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    { _context=context; _stage=stage; _inverse=stageToInstance; }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        _age=age;
        var active = (_context.TravelProgress ?? 1) > .5f;
        var direction = _context.Direction ?? (_context.Target-_context.Source).Normalized();
        if (direction.IsZeroApprox()) direction=Vector2.Right;
        var normal = new Vector2(-direction.Y,direction.X);
        var ground = Kind is Shape.Swap or Shape.Fissure or Shape.RockRise;
        var a = _stage.Project(_context.Source,ground);
        var b = _context.TargetDisplayPosition ?? _stage.Project(_context.Target,ground);
        var x = _stage.Project(_context.Source+direction,ground)-a;
        var y = _stage.Project(_context.Source+normal,ground)-a;
        var length = _context.Source.DistanceTo(_context.Target);
        Modulate = new Color(1,1,1,1-release);
        _material?.SetShaderParameter("age",reducedMotion ? .12f : age);
        _material?.SetShaderParameter("active",active);
        if(Details is not null && SampleCrafted(age,release,impact,reducedMotion,active,a,b,x,y,length))return;
        switch (Kind)
        {
            case Shape.Cone:
                var slope = Mathf.Tan(Mathf.DegToRad((_context.ConeAngleDegrees > 0 ? _context.ConeAngleDegrees : 70)*.5f));
                _material?.SetShaderParameter("cone_slope",slope);
                Surface.Transform = _inverse*new Transform2D(x*length/256,y*length*slope*2/256,(a+b)/2);
                break;
            case Shape.Blade:
                Surface.Transform = _inverse*new Transform2D(reducedMotion?0:age*15,Vector2.One*_stage.UnitScale*.19f,0,b);
                break;
            case Shape.Acid:
                Surface.Transform = _inverse*new Transform2D((b-a).Angle(),Vector2.One*_stage.UnitScale*.095f,0,b);
                break;
            case Shape.Swap:
                Surface.Transform = _inverse*new Transform2D(0,new Vector2(.25f,.16f)*_stage.UnitScale,0,a);
                if (Other is not null) Other.Transform = _inverse*new Transform2D(0,new Vector2(.25f,.16f)*_stage.UnitScale,0,b);
                if (Link is not null) { Link.Transform=_inverse;Link.Points=[a,b];Link.Width=1.5f*_stage.UnitScale; }
                break;
            case Shape.Fissure:
                float fissureScale=_playback?.RadiusScale??1;
                float fissureUnit=_stage.UnitScale*fissureScale;
                Surface.Transform = _inverse*new Transform2D(x*length/256,y*.6f*fissureScale/256,(a+b)/2);
                for(var i=0;i<_pieces.Length;i++)
                {
                    var p=(i+.5f)/_pieces.Length;
                    var t=Mathf.Clamp((age-p*.24f)*8,0,1);
                    _pieces[i].Visible=active;
                    var textureWidth = (_pieces[i] as Sprite2D)?.Texture?.GetWidth() ?? 256;
                    _pieces[i].Transform=_inverse*new Transform2D(0,Vector2.One*fissureUnit*.12f*256/textureWidth,0,a.Lerp(b,p)+Vector2.Up*20*fissureUnit*Mathf.Sin(t*Mathf.Pi*.65f));
                    _pieces[i].Modulate=new Color(1,1,1,active?1-Mathf.Clamp((age-.35f)*4,0,1):0);
                }
                break;
            case Shape.RockRise:
                Surface.Visible=active;
                Surface.Transform=_inverse*new Transform2D(0,Vector2.One*_stage.UnitScale*.23f*256/Surface.Texture.GetWidth(),0,b-Vector2.Up*Mathf.Clamp(age*6,0,1)*16*_stage.UnitScale);
                Surface.Modulate=new Color(1,1,1,active?1-Mathf.Clamp(age*2,0,1):.18f);
                break;
            case Shape.Shell:
                if (_playback?.Cancelled == true) _shellStoppedAt = Mathf.Min(_shellStoppedAt, _previousShellAge);
                float shellScale = _stage.UnitScale * (_playback?.RadiusScale ?? 1);
                Surface.Transform=_inverse*new Transform2D(0,Vector2.One*shellScale*.36f,0,b);
                Surface.Visible=!active;
                for(var i=0;i<_pieces.Length;i++)
                {
                    float random=VfxMotePool.Rand(340+i),delay=i*.012f;
                    float t=Mathf.Max(0,age-delay);
                    var heading=Vector2.FromAngle(i*Mathf.Tau/_pieces.Length+(random-.5f)*.65f);
                    float size=(.095f+random*.07f)*shellScale;
                    float lift=reducedMotion?0:-(65+random*80)*t+210*t*t;
                    var offset=heading*(9+t*(45+random*70))*shellScale+Vector2.Down*lift*shellScale;
                    _pieces[i].Visible=active && age>=delay && delay<=_shellStoppedAt && age<.82f;
                    _pieces[i].Transform=_inverse*new Transform2D(heading.Angle()+(reducedMotion?0:t*(i%2==0?3:-2)),
                        new Vector2(size,size*(.6f+random*.35f)),0,b+offset);
                    _pieces[i].Modulate=new Color(1,1,1,1-Mathf.SmoothStep(.34f,.75f,t));
                }
                _previousShellAge=age;
                break;
            case Shape.Claw:
                float clawScale=_stage.UnitScale*(_playback?.RadiusScale??1);
                float sweep=reducedMotion?1:Ease(age/.16f);
                var sweepPoint=a.Lerp(b,.72f)+new Vector2(0,(-8+16*sweep)*clawScale);
                Surface.Transform=_inverse*new Transform2D((b-a).Angle()-.12f+.20f*sweep,
                    Vector2.One*clawScale*.36f,0,sweepPoint);
                Surface.Modulate=new Color(1,1,1,active?1-Mathf.SmoothStep(.30f,.53f,age):0);
                // Reduced motion keeps the cut visible without following the sweep.
                _material?.SetShaderParameter("age",reducedMotion?.16f:age);
                break;
        }
    }
}
