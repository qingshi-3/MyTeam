using System;
using Godot;

namespace TowerAutobattler.Vfx;

// A bounded pool of an authored sprite scene. Birth positions are in stage space:
// movement of the owner cannot drag already emitted smoke along with it.
public partial class VfxMotePool : Node2D
{
    [Export] public PackedScene ParticleScene { get; set; } = null!;
    [Export] public int Capacity { get; set; } = 80;
    private sealed class Mote(Sprite2D sprite, ShaderMaterial material)
    {
        public readonly Sprite2D Sprite=sprite;
        public readonly ShaderMaterial Material=material;
        public float Born=-100, Life, Start, End, Angle, Spin;
        public Vector2 Position, Velocity, Acceleration, Aspect;
    }
    private Mote[] _motes=[];
    private int _next;
    public int BirthCount { get; private set; }
    public override void _Ready()
    {
        _motes=new Mote[Capacity];
        for(int i=0;i<Capacity;i++)
        {
            var sprite=ParticleScene.Instantiate<Sprite2D>();
            var material=(ShaderMaterial)sprite.Material.Duplicate();sprite.Material=material;
            AddChild(sprite);sprite.Visible=false;_motes[i]=new(sprite,material);
        }
    }
    public void Emit(float at, Vector2 position, Vector2 velocity, Vector2 acceleration,
        float life, float start, float end, float angle, float spin, Color tint,
        int kind=0, int cell=0, Vector2? aspect=null)
    {
        var p=_motes[_next++%Capacity]; BirthCount++;
        p.Born=at;p.Life=life;p.Start=start;p.End=end;p.Angle=angle;p.Spin=spin;
        p.Position=position;p.Velocity=velocity;p.Acceleration=acceleration;p.Aspect=aspect??Vector2.One;
        p.Material.SetShaderParameter("tint",tint);p.Material.SetShaderParameter("kind",kind);
        p.Material.SetShaderParameter("cell",cell);p.Material.SetShaderParameter("seed",(BirthCount%37)*.173f);
    }
    public void Render(float now, Transform2D inverse, bool reduced)
    {
        foreach(var p in _motes)
        {
            float t=now-p.Born,u=t/p.Life;
            p.Sprite.Visible=t>=0 && u<1;
            if(!p.Sprite.Visible)continue;
            var size=Mathf.Lerp(p.Start,p.End,1-Mathf.Pow(1-u,2));
            var position=p.Position+p.Velocity*t+p.Acceleration*t*t*.5f;
            p.Sprite.Transform=inverse*new Transform2D(p.Angle+(reduced?0:p.Spin*t),p.Aspect*size/512,0,position);
            p.Material.SetShaderParameter("progress",u);
            p.Material.SetShaderParameter("age",reduced?0:t);
        }
    }
    public static float Rand(int n) { var x=MathF.Sin(n*127.1f+311.7f)*43758.5453f;return x-MathF.Floor(x); }
}
