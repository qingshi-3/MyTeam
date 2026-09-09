using Godot;

namespace TowerAutobattler.Vfx;

// One authored crystal grows from its base, then locally fractures. A separate
// emitter at that same base owns the flying chips; this sprite never slides out.
public partial class VfxCrystalTrack : Sprite2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public Vector2 PixelSize { get; set; } = new(60,110);
    [Export] public float Delay { get; set; }
    [Export] public float GrowthDuration { get; set; } = .15f;
    [Export] public float FractureAt { get; set; } = .28f;
    [Export] public float TailDuration { get; set; } = .23f;
    private VfxPlaybackState? _playback;
    private ShaderMaterial _shader=null!;
    private bool _started;
    public void ConfigurePlayback(VfxPlaybackState playback)=>_playback=playback;
    public override void _Ready()
    {
        Offset=new(0,-Texture.GetHeight()*.5f);
        Material=_shader=(ShaderMaterial)Material.Duplicate();Visible=false;
    }
    public void Sample(float age,bool sustained,float release,float impact,bool reducedMotion)
    {
        float speed=_playback?.Parameters.CastSpeed??1;
        float t=age-Delay/speed;
        float crackAge=t-FractureAt/speed;
        Visible=t>=0 && crackAge<TailDuration && release<1 && (_started || _playback?.Cancelled!=true);
        if(!Visible)return;
        _started=true;
        float growth=Mathf.SmoothStep(0,GrowthDuration/speed,t);
        Scale=PixelSize/Texture.GetSize()*(_playback?.ParticleScale??1)
            *(reducedMotion?Vector2.One:new Vector2(.65f+.35f*growth,Mathf.Max(.01f,growth)));
        _shader.SetShaderParameter("growth",growth);
        _shader.SetShaderParameter("fracture",Mathf.Clamp(crackAge/TailDuration,0,1));
        _shader.SetShaderParameter("release",release);
        _shader.SetShaderParameter("motion_amount",reducedMotion?0f:1f);
    }
}
