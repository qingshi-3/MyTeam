using Godot;
namespace TowerAutobattler.Vfx;

public partial class VfxBreathTrack : Node2D, IVfxTrack, IVfxSpatialTrack, IVfxPlaybackTrack
{
    [Export] public Sprite2D Fire {get;set;}=null!;
    [Export] public Sprite2D Glow {get;set;}=null!;
    [Export] public VfxMotePool Smoke {get;set;}=null!;
    [Export] public VfxMotePool Tongues {get;set;}=null!;
    private ShaderMaterial _fire=null!;
    private VfxPlaybackState _clock=null!;
    private VfxContext _context;
    private IVfxStage _stage=null!;
    private Transform2D _inverse;
    private float _next;
    private int _serial;
    private float? _stoppedAt;
    public void ConfigurePlayback(VfxPlaybackState p)=>_clock=p;
    public override void _Ready()
    {
        Fire.Material=_fire=(ShaderMaterial)Fire.Material.Duplicate();
    }
    public void SetSpatialContext(VfxContext c,IVfxStage s,Transform2D inverse){_context=c;_stage=s;_inverse=inverse;}
    public void Sample(float age,bool sustained,float release,float impact,bool reduced)
    {
        var direction=_context.Direction??(_context.Target-_context.Source).Normalized();
        if(direction.IsZeroApprox())direction=Vector2.Right;
        var normal=new Vector2(-direction.Y,direction.X);
        var source=_stage.Project(_context.Source,false);
        var along=_stage.Project(_context.Source+direction,false)-source;
        var across=_stage.Project(_context.Source+normal,false)-source;
        var range=_context.Source.DistanceTo(_context.Target);
        var half=range*Mathf.Tan(Mathf.DegToRad((_context.ConeAngleDegrees>0?_context.ConeAngleDegrees:70)*.5f));
        var active=(_context.TravelProgress??1)>.5f;
        // The throat stays on the actor; the broad flames occupy the actual locked cone.
        var mouth=source+along*.18f;
        Fire.Transform=_inverse*new Transform2D(along*(range-.18f)/256,across*half*2/256,mouth+along*(range-.18f)*.5f);
        if(_clock.Cancelled) _stoppedAt??=age;
        var stopAge=_stoppedAt is { } stopped?Mathf.Max(0,age-stopped):-1;
        var supply=stopAge>=0?Mathf.Max(0,1-stopAge/.10f):Mathf.Min(1,age/.06f);
        Fire.Visible=active;_fire.SetShaderParameter("age",age*(reduced?.6f:1));
        // Ending feed travels from the mouth toward the tip; already emitted fire keeps moving.
        _fire.SetShaderParameter("stop_age",stopAge);_fire.SetShaderParameter("supply",.8f);
        var charge=Mathf.Clamp(age,0,1);
        Glow.Transform=_inverse*new Transform2D(0,new Vector2(1,.65f)*_stage.UnitScale*(active?.32f:.13f+charge*.17f),0,mouth+along*.15f);
        Glow.Modulate=new Color(1,.25f+charge*.12f,.015f,(active?.48f:.13f+charge*.48f)*supply);
        if(active && !_clock.Cancelled)
        {
            // Limit catch-up after a large clock step; fixed birth times keep normal FPS independent.
            _next=Mathf.Max(_next,age-.8f);
            while(_next<=age)
            {
                int n=++_serial;float r=VfxMotePool.Rand(n),side=(VfxMotePool.Rand(n+91)-.5f)*1.8f;
                float speed=3.1f+r*1.25f, life=.58f+r*.17f;
                var vel=along*speed+across*side*half*.75f;
                Tongues.Emit(_next,mouth,vel,Vector2.Up*20*_stage.UnitScale,life,13*_stage.UnitScale,(40+r*24)*_stage.UnitScale,
                    side*.8f,side*.7f,new Color(1,1,1,.93f),1,n%4,new Vector2(1.25f,.82f));
                if(n%3==0)Smoke.Emit(_next,mouth+along*.2f,vel*.92f,Vector2.Up*50*_stage.UnitScale,
                    .9f,16*_stage.UnitScale,(57+r*28)*_stage.UnitScale,side,side*.3f,new Color(.31f,.19f,.15f,.38f),0,n%4);
                _next+=reduced?.033f:.018f;
            }
        }
        Smoke.Render(age,_inverse,reduced);Tongues.Render(age,_inverse,reduced);
    }
}
