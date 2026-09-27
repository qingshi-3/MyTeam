using System;
using Godot;

namespace TowerAutobattler.Vfx;

// The temporary eruption and the durable wall share this authored assembly.
// Battle ownership, pause and speed are injected; the component never reads the board.
[GlobalClass]
public partial class RockBarrierVisual : Node2D
{
    [Export] public RockBarrierPillar[] Pillars { get; set; } = [];
    [Export] public float ArcDepth { get; set; } = .18f;
    [Export] public float ForwardOffset { get; set; } = .18f;
    public float Age { get; private set; }
    public bool Paused { get; set; }
    public float PlaybackSpeed { get; set; } = 1;
    private Vector2 _tangent=new(0,30),_forward=new(48,0);
    private float _slot;
    private bool _playing;
    public override void _Ready()
    {
        Sample(1,false);
    }
    public void Begin(Vector2 tangent,Vector2 forward,float slot,bool snap=false)
    {
        _tangent=tangent;_forward=forward;_slot=slot;Age=snap?1:0;_playing=true;
        Sample(Age,snap);
    }
    public void SetGeometry(Vector2 tangent,Vector2 forward,float slot=0)
    { _tangent=tangent;_forward=forward;_slot=slot; }
    public override void _Process(double delta)
    {
        if(!_playing || Paused || Age>=.6f)return;
        Advance((float)delta*PlaybackSpeed);
    }
    public void Advance(float seconds)
    { if(!_playing)return;Age+=seconds;Sample(Age,false); }
    public void Sample(float age,bool reduced)
    {
        for(int i=0;i<Pillars.Length;i++)
        {
            float across=(i-1)*.27f;
            // Bow the center outward without pulling the wings into the caster.
            var foot=_tangent*across+_forward*(ForwardOffset-ArcDepth*Mathf.Pow(_slot+across,2));
            float height=(i==0?86:i==1?111:95)*(1-.22f*Mathf.Abs(_slot));
            float delay=Mathf.Abs(_slot)*.035f+(i==1?0:i==0?.045f:.075f);
            float t=Math.Max(0,age-delay),u=Mathf.Clamp(t/.27f,0,1);
            float lift=reduced?1:1-Mathf.Pow(1-u,3);
            var pillar=Pillars[i];
            pillar.Position=foot;
            // Each column turns along the shallow arc; no independent sprite mirroring.
            float slope=2*ArcDepth*(_slot+across),normalizer=Mathf.Sqrt(1+slope*slope);
            var tangent=(_tangent-_forward*slope)/normalizer;
            var forward=(_forward+_tangent*slope)/normalizer;
            pillar.ZIndex=(int)Mathf.Round((_slot+across)*Mathf.Sign(_tangent.Y)*3)+5;
            pillar.Sample(tangent,forward,height,i==1?.34f:.31f,.62f,lift,(int)(_slot+1)*3+i);
        }
    }
}
