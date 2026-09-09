using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Vfx;

// Authored endpoints, instance-local branched mesh, one clock. The path stays
// fixed during a discharge; the material carries its moving current and decay.
public partial class VfxLightningTrack : MeshInstance2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public Vector2 From { get; set; } = new(8,-235);
    [Export] public Vector2 To { get; set; } = Vector2.Zero;
    [Export] public int Seed { get; set; } = 410;
    [Export] public int Branches { get; set; } = 7;
    [Export] public float Width { get; set; } = 22;
    [Export] public float Jaggedness { get; set; } = 40;
    [Export] public float Delay { get; set; }
    [Export] public float TravelDuration { get; set; } = .045f;
    [Export] public float TailDuration { get; set; } = .22f;
    [Export] public bool GroundFan { get; set; }
    private VfxPlaybackState? _playback;
    private ShaderMaterial _shader = null!;
    private bool _started;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback=playback;
    public override void _Ready()
    {
        using var rng = new RandomNumberGenerator { Seed=(ulong)Seed };
        var vertices=new List<Vector2>();var uvs=new List<Vector2>();
        var colors=new List<Color>();var indices=new List<int>();
        List<Vector2> Fracture(Vector2 from,Vector2 to,int depth,float roughness)
        {
            var points=new List<Vector2>{from,to};
            for(int level=0;level<depth;level++)
            {
                var next=new List<Vector2>{points[0]};
                for(int i=0;i<points.Count-1;i++)
                {
                    var delta=points[i+1]-points[i];var normal=new Vector2(-delta.Y,delta.X).Normalized();
                    next.Add((points[i]+points[i+1])*.5f+normal*rng.RandfRange(-roughness,roughness));
                    next.Add(points[i+1]);
                }
                points=next;roughness*=.57f;
            }
            return points;
        }
        void Strip(List<Vector2> points,float width,float startPhase,float strength)
        {
            int offset=vertices.Count;
            for(int i=0;i<points.Count;i++)
            {
                float u=i/(float)(points.Count-1);
                var tangent=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).Normalized();
                var normal=new Vector2(-tangent.Y,tangent.X);
                float taper=strength>=.99f?Mathf.Lerp(.9f,1.15f,u):Mathf.Lerp(1,.10f,u);
                for(int side=0;side<2;side++)
                {
                    vertices.Add(points[i]+normal*width*taper*(side-.5f));
                    uvs.Add(new(Mathf.Lerp(startPhase,1,u),side));
                    colors.Add(new(1,strength,1,1));
                }
                if(i==points.Count-1)continue;
                int v=offset+i*2;
                indices.AddRange([v,v+1,v+2,v+1,v+3,v+2]);
            }
        }
        if(GroundFan)
        {
            for(int i=0;i<Branches;i++)
            {
                var end=Vector2.FromAngle((i+.17f)*Mathf.Tau/Branches)*rng.RandfRange(65,105);
                Strip(Fracture(Vector2.Zero,end,4,Jaggedness),Width,0,rng.RandfRange(.5f,.8f));
            }
        }
        else
        {
            var trunk=Fracture(From,To,5,Jaggedness);
            Strip(trunk,Width,0,1);
            for(int i=0;i<Branches;i++)
            {
                int at=rng.RandiRange(5,trunk.Count-6);
                var end=trunk[at]+new Vector2((i%2==0?-1:1)*rng.RandfRange(30,70),rng.RandfRange(20,65));
                Strip(Fracture(trunk[at],end,3,Jaggedness*.30f),Width*.48f,at/(float)(trunk.Count-1),.65f);
            }
        }
        var arrays=new Godot.Collections.Array();arrays.Resize((int)Godot.Mesh.ArrayType.Max);
        arrays[(int)Godot.Mesh.ArrayType.Vertex]=vertices.ToArray();
        arrays[(int)Godot.Mesh.ArrayType.TexUV]=uvs.ToArray();
        arrays[(int)Godot.Mesh.ArrayType.Color]=colors.ToArray();
        arrays[(int)Godot.Mesh.ArrayType.Index]=indices.ToArray();
        var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arrays);Mesh=mesh;
        Material=_shader=(ShaderMaterial)Material.Duplicate();Visible=false;
    }
    public void Sample(float age,bool sustained,float release,float impact,bool reducedMotion)
    {
        float speed=_playback?.Parameters.CastSpeed??1;
        float t=age-Delay/speed;
        Visible=t>=0 && t<TravelDuration/speed+TailDuration && release<1 && (_started || _playback?.Cancelled!=true);
        if(!Visible)return;
        _started=true;
        _shader.SetShaderParameter("age",t);
        _shader.SetShaderParameter("travel_duration",TravelDuration/speed);
        _shader.SetShaderParameter("tail_duration",TailDuration);
        _shader.SetShaderParameter("release",release);
        _shader.SetShaderParameter("motion_amount",reducedMotion?0f:1f);
    }
}
