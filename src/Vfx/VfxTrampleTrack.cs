using Godot;
using System.Linq;

namespace TowerAutobattler.Vfx;

// Geometry is projected from authoritative endpoints/body radius; all motion uses the player clock.
public partial class VfxTrampleTrack : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSpatialTrack
{
    [Export] public bool Rush { get; set; }
    [Export] public Sprite2D Surface { get; set; } = null!;
    [Export] public VfxMotePool? Dust { get; set; }
    [Export] public Node2D? Streaks { get; set; }
    [Export] public float FrontHeightPixels { get; set; } = 26;
    [Export] public float FrontAdvance { get; set; } = .65f;
    // A visual shoulder margin, applied across the heading only. Collision radius is unchanged.
    [Export(PropertyHint.Range, "0.5,2,0.05")] public float VisualWidthScale { get; set; } = 1;
    private ShaderMaterial _material = null!;
    private VfxPlaybackState _playback = null!;
    private VfxContext _context;
    private IVfxStage _stage = null!;
    private Transform2D _inverse;
    private Vector2? _previous;
    private float _previousAge;
    private int _serial;
    private Sprite2D[] _streaks=[];
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public override void _Ready()
    {
        _material = (ShaderMaterial)Surface.Material.Duplicate();
        Surface.Material = _material;
        if(!Rush) _material.SetShaderParameter("rush", false);
        _streaks=Streaks?.GetChildren().OfType<Sprite2D>().ToArray()??[];
    }
    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        _context=context;_stage=stage;_inverse=stageToInstance;
        if(Rush)return;
        var direction = context.Direction ?? (context.Target - context.Source).Normalized();
        if (direction.IsZeroApprox()) direction = Vector2.Right;
        var normal = new Vector2(-direction.Y, direction.X);
        var radius = context.Radius > 0 ? context.Radius : .9f;
        var anchor = stage.Project(context.Source, true);
        var along = stage.Project(context.Source + direction, true) - anchor;
        var across = stage.Project(context.Source + normal, true) - anchor;
        var length = context.Source.DistanceTo(context.Target);
        var center = (anchor + stage.Project(context.Target, true)) / 2;
        Surface.Transform = stageToInstance * new Transform2D(along * length / 256,
            across * radius * 2 / 256, center);
        _material.SetShaderParameter("length_ratio", length / (radius * 2));
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        if(Rush) { SampleRush(age,release,reducedMotion);return; }
        _material.SetShaderParameter("age", reducedMotion ? 0 : age);
        _material.SetShaderParameter("progress", Mathf.Clamp(_playback.Age / Mathf.Max(.01f, _playback.CastDuration), 0, 1));
        Modulate = new Color(1, 1, 1, 1 - Mathf.Clamp(release * 5, 0, 1));
    }
    private void SampleRush(float age,float release,bool reduced)
    {
        var d=_context.Direction??(_context.Target-_context.Source).Normalized();if(d.IsZeroApprox())d=Vector2.Right;
        var n=new Vector2(-d.Y,d.X);var origin=_stage.Project(_context.Source,true);
        var along=_stage.Project(_context.Source+d,true)-origin;var across=_stage.Project(_context.Source+n,true)-origin;
        float radius=_context.Radius>0?_context.Radius:.7f,size=_stage.UnitScale;
        float visualRadius=radius*Mathf.Max(.1f,VisualWidthScale);
        var body=_context.TargetDisplayPosition??origin;
        float strength=_playback.Cancelled?Mathf.Max(0,1-release*5):Mathf.Min(1,age*14);
        var front=body+Vector2.Up*FrontHeightPixels*size+along*radius*FrontAdvance;
        Surface.Transform=_inverse*new Transform2D(along*radius*2.5f/256,across*visualRadius*2.9f/256,front);
        _material.SetShaderParameter("age",reduced?0:age);_material.SetShaderParameter("strength",strength*.85f);
        for(int i=0;i<_streaks.Length;i++)
        {
            float side=i%2==0?-1:1,r=VfxMotePool.Rand(i+10);
            var pos=body-along*radius*(.65f+r*.4f)+across*side*visualRadius*(.5f+r*.35f)+Vector2.Up*(FrontHeightPixels-2)*size;
            _streaks[i].Transform=_inverse*new Transform2D(along*radius*(2.7f+r)/512,across*(.16f+r*.2f)*VisualWidthScale/512,pos);
            _streaks[i].Modulate=new Color(.88f,.93f,1,strength*(.32f+r*.25f));
        }
        if(!_playback.Cancelled && Dust is not null)
        {
            var previous=_previous??body;float distance=previous.DistanceTo(body),step=Mathf.Max(4,along.Length()*.14f);
            // Births are distributed on the actual traversed segment, never at the locked destination.
            if(_previous is null || distance>=step)
            {
                int count=Mathf.Clamp(Mathf.CeilToInt(distance/step),1,16);
                if(distance>along.Length()*4)count=1;
                for(int j=1;j<=count;j++)
                {
                    float t=(float)j/count;var pos=previous.Lerp(body,t);float born=Mathf.Lerp(_previousAge,age,t);
                    for(int side=-1;side<=1;side+=2)
                    {
                        int serial=++_serial;float r=VfxMotePool.Rand(serial);
                        Dust.Emit(born,pos-along*radius*(.35f+r*.28f)+across*side*visualRadius*(.4f+VfxMotePool.Rand(serial+79)*.32f),
                            -along*(.5f+r*.7f)+across*side*(.45f+r*.5f),Vector2.Up*22*size,
                            .5f+r*.28f,(14+r*12)*size,(36+r*27)*size,r*6,side*.55f,new Color(.65f,.57f,.46f,.42f),0,serial%4,new Vector2(1.1f,.75f));
                    }
                }
                _previous=body;_previousAge=age;
            }
        }
        Dust?.Render(age,_inverse,reduced);
    }
}
