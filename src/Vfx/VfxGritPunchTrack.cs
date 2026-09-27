using System.Linq;
using Godot;
namespace TowerAutobattler.Vfx;

public partial class VfxGritPunchTrack : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSpatialTrack
{
    [Export] public Sprite2D GroundStrip {get;set;}=null!;
    [Export] public Sprite2D BodyStrip {get;set;}=null!;
    [Export] public Sprite2D Glow {get;set;}=null!;
    [Export] public Sprite2D Flash {get;set;}=null!;
    [Export] public Sprite2D Core {get;set;}=null!;
    [Export] public Node2D Streaks {get;set;}=null!;
    [Export] public VfxMotePool Dust {get;set;}=null!;
    [Export] public bool Released {get;set;}
    private VfxPlaybackState _clock=null!;
    private ShaderMaterial _ground=null!,_body=null!;
    private Sprite2D[] _streaks=[];
    private VfxContext _context;
    private IVfxStage _stage=null!;
    private Transform2D _inverse;
    private bool _emitted;
    public void ConfigurePlayback(VfxPlaybackState p)=>_clock=p;
    public override void _Ready()
    {
        GroundStrip.Material=_ground=(ShaderMaterial)GroundStrip.Material.Duplicate();
        BodyStrip.Material=_body=(ShaderMaterial)BodyStrip.Material.Duplicate();
        _streaks=Streaks.GetChildren().OfType<Sprite2D>().ToArray();
    }
    public void SetSpatialContext(VfxContext c,IVfxStage s,Transform2D inverse){_context=c;_stage=s;_inverse=inverse;}
    public void Sample(float age,bool sustained,float release,float impact,bool reduced)
    {
        var d=_context.Direction??(_context.Target-_context.Source).Normalized();if(d.IsZeroApprox())d=Vector2.Right;
        var n=new Vector2(-d.Y,d.X);var a=_stage.Project(_context.Source,false);var end=_stage.Project(_context.Target,false);
        var along=_stage.Project(_context.Source+d,false)-a;var across=_stage.Project(_context.Source+n,false)-a;
        var length=_context.Source.DistanceTo(_context.Target);var radius=_context.Radius>0?_context.Radius:.8f;
        var ground=_stage.Project(_context.Source,true);var size=_stage.UnitScale;
        var fist=a+along*.22f;
        float p=Mathf.Clamp(_clock.Age/Mathf.Max(.01f,_clock.CastDuration),0,1);
        GroundStrip.Visible=!Released && !_clock.Cancelled;
        GroundStrip.Transform=_inverse*new Transform2D(along*length/256,across*radius*2/256,ground+along*length*.5f);
        _ground.SetShaderParameter("progress",p);_ground.SetShaderParameter("released",false);
        if(!Released)
        {
            Core.Visible=false;
            BodyStrip.Visible=false;
            Glow.Transform=_inverse*new Transform2D(0,Vector2.One*size*(.16f+p*p*.29f),0,fist);
            Glow.Modulate=new Color(1,.42f,.025f,(.12f+p*.48f)*(1-release));
            Flash.Transform=_inverse*new Transform2D(0,Vector2.One*size*(.035f+p*p*.1f),0,fist);
            Flash.Modulate=new Color(1,.85f,.32f,p*p*(1-release));
            for(int i=0;i<_streaks.Length;i++)
            {
                float phase=Mathf.PosMod(age*1.8f+i*.137f,1),ang=i*2.4f;
                var dir=Vector2.FromAngle(ang);var r=(1-phase)*35*size;
                var sprite=_streaks[i];sprite.Visible=!_clock.Cancelled;
                sprite.Transform=_inverse*new Transform2D(ang,new Vector2((10+phase*8)*size/512,4*size/512),0,fist+dir*r);
                sprite.Modulate=new Color(1,.64f,.13f,Mathf.Sin(phase*Mathf.Pi)*p*.8f);
            }
        }
        else
        {
            // The whole damaging lane is visible at release. Only its opening/falloff is slower;
            // this is an impact body, not a projectile whose arrival could imply later damage.
            float flash=Mathf.Max(0,1-age/.10f),body=1-Mathf.SmoothStep(0,1,Mathf.Clamp((age-.07f)/.39f,0,1));
            float expand=1-Mathf.Pow(Mathf.Max(0,1-age/.25f),3);
            Core.Visible=true;
            Core.Transform=_inverse*new Transform2D(along*length*1.1f/512,across*radius*(6.5f+expand*2)/512,a+along*length*.54f);
            Core.Modulate=new Color(1,.83f,.43f,.85f*(1-Mathf.SmoothStep(0,1,Mathf.Clamp((age-.035f)/.235f,0,1))));
            BodyStrip.Visible=true;
            BodyStrip.Transform=_inverse*new Transform2D(along*length*.55f/256,across*radius*(2.05f+expand*.24f)/256,end-along*.55f);
            _body.SetShaderParameter("age",age);_body.SetShaderParameter("strength",body);
            Glow.Transform=_inverse*new Transform2D(along*length*1.05f/256,across*radius*2.3f/256,a+along*length*.55f);
            Glow.Modulate=new Color(1,.35f,.035f,body*.45f);
            Flash.Transform=_inverse*new Transform2D(0,new Vector2(.95f,.75f)*size*.38f,0,fist+along*.3f);
            Flash.Modulate=new Color(1,.96f,.71f,flash);
            for(int i=0;i<_streaks.Length;i++)
            {
                float r=VfxMotePool.Rand(i+17),side=(i-5.5f)/6;
                var sprite=_streaks[i];sprite.Visible=true;
                float width=length*(.62f+r*.35f),fade=Mathf.Pow(Mathf.Max(0,1-age/(.20f+r*.16f)),1.1f);
                var pos=a+along*(length*.54f+age*.5f)+across*radius*side*(.48f+expand*.42f);
                sprite.Transform=_inverse*new Transform2D((along*width+across*side*.18f)/512,across*(.48f+r*.85f)/512,pos);
                sprite.Modulate=new Color(1,.70f+r*.27f,.28f+r*.51f,fade);
            }
            if(!_emitted)
            {
                _emitted=true;
                for(int i=0;i<16;i++)
                {
                    float r=VfxMotePool.Rand(i+11),side=i%2==0?-1:1;
                    float dist=(i/2+.15f+VfxMotePool.Rand(i+51)*.8f)/8*length;
                    var lateral=radius*side*(.64f+VfxMotePool.Rand(i+83)*.24f);
                    Dust.Emit(.015f+VfxMotePool.Rand(i+21)*.025f,ground+along*dist+across*lateral,
                        along*(.3f+r*.65f)+across*side*(.35f+r*.7f),Vector2.Up*size*15,
                        .45f+r*.4f,(10+r*11)*size,(23+r*22)*size,r*6,side*.3f,new Color(.61f,.5f,.36f,.3f),0,i%4,new Vector2(1.2f,.75f));
                }
            }
        }
        Dust.Render(age,_inverse,reduced);
        Modulate=new Color(1,1,1,1-release);
    }
}
