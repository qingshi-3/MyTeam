using System;
using Godot;

namespace TowerAutobattler.Vfx;

public partial class VfxEnemyActionTrack
{
    private float _previousCraftAge=-.001f;
    private Vector2 _previousPoint;
    private bool _hasPreviousPoint;
    private int _trailIndex;
    private bool _splashed;
    private float _splashAge;
    private float _craftStopAge=float.PositiveInfinity;
    private static float Ease(float u)=>1-Mathf.Pow(1-Mathf.Clamp(u,0,1),3);
    private Sprite2D Detail(int index)=>Details!.GetChild<Sprite2D>(index);
    private void Place(Sprite2D sprite,Vector2 point,Vector2 pixels,float angle=0,float alpha=1)
    {
        sprite.Visible=alpha>.001f;
        sprite.Transform=_inverse*new Transform2D(angle,pixels*_stage.UnitScale/sprite.Texture.GetSize(),0,point);
        sprite.Modulate=new Color(1,1,1,Mathf.Clamp(alpha,0,1));
    }
    private static void Parameter(Sprite2D sprite,string key,Variant value)
    { if(sprite.Material is ShaderMaterial material)material.SetShaderParameter(key,value); }
    private void DustBurst(float at,Vector2 point,int seed,int count,float strength=1,float spatialScale=1)
    {
        if(Motes is null)return;
        float s=_stage.UnitScale*spatialScale;
        for(int j=0;j<count;j++)
        {
            float u=VfxMotePool.Rand(seed+j*7),v=VfxMotePool.Rand(seed+j*7+3);
            Motes.Emit(at,point+new Vector2((u-.5f)*16,-v*3)*s,
                new Vector2((u-.5f)*96,-12-v*28)*s,Vector2.Up*8*s,
                .38f+v*.4f,(12+u*14)*s,(35+v*25)*s, u*6, (v-.5f)*.8f,
                new Color(.47f,.39f,.29f,.38f*strength),cell:j%4,aspect:new(1.3f,.75f));
        }
    }
    private bool SampleCrafted(float age,float release,float impact,bool reduced,bool active,Vector2 a,Vector2 b,Vector2 x,Vector2 y,float length)
    {
        float s=_stage.UnitScale,visualTime=reduced?.2f:age;
        var delta=b-a;var heading=delta.IsZeroApprox()?Vector2.Right:delta.Normalized();
        bool live=release<=0 && _playback?.Cancelled!=true;
        // Delayed rocks/chips already scheduled must not appear after interruption.
        if(!live)_craftStopAge=Math.Min(_craftStopAge,_previousCraftAge);
        switch(Kind)
        {
            case Shape.Blade:
                Place(Surface,b,new(58,58),visualTime*15,1-release);
                var ghost=Detail(0);Place(ghost,b-heading*3*s,new(59,59),visualTime*15-.38f,reduced?0:.22f*(1-release));
                var air=Detail(1);Place(air,b,new(83,62),0,.58f*(1-release));Parameter(air,"age",visualTime);
                Trail(age,b,heading,live && !reduced,false);
                break;
            case Shape.Acid:
                Place(Surface,b,new(36,24),heading.Angle(),release>0||_splashed?0:1);
                Parameter(Surface,"age",visualTime);
                Trail(age,b,heading,live && !reduced,true);
                if(impact>0 && !_splashed)
                {
                    _splashed=true;
                    _splashAge=age;
                    if(!reduced && Motes is not null)
                        for(int i=0;i<15;i++)
                        {
                            float u=VfxMotePool.Rand(770+i),v=VfxMotePool.Rand(950+i);
                            Motes.Emit(age,b,new Vector2((u-.5f)*170,-30-v*110)*s,Vector2.Down*290*s,
                                .25f+v*.24f,(5+u*8)*s,3*s,u*6,0,new(.56f,.8f,.1f,.9f));
                        }
                }
                // The contact burst stays at body height; the spreading liquid belongs to the floor.
                float splashTime=Math.Max(0,age-_splashAge),spread=reduced?1:Ease(splashTime/.18f);
                var splash=Detail(0);Place(splash,_stage.Project(_context.Target,true),new Vector2(132,56)*(.38f+.62f*spread),0,
                    _splashed?.86f*(1-Mathf.SmoothStep(.30f,.7f,splashTime)):0);
                Parameter(splash,"age",reduced?.2f:splashTime);
                break;
            case Shape.Fissure:
                // The inverse root transform cancels automatic radius scaling. Restore it
                // for every layer, independently of path length (which terrain may clip).
                float fissureScale=_playback?.RadiusScale??1;
                float fissureUnit=s*fissureScale;
                Surface.Transform=_inverse*new Transform2D(x*length/256,y*.6f*fissureScale/256,(a+b)/2);
                Parameter(Surface,"age",visualTime);Parameter(Surface,"active",active);
                for(int i=0;i<_pieces.Length;i++)
                {
                    var rock=(Sprite2D)_pieces[i];float p=(i+.5f)/_pieces.Length,start=i*.045f;
                    float t=age-start,height=36+VfxMotePool.Rand(910+i)*24,width=24+VfxMotePool.Rand(811+i)*14;
                    float ending=Mathf.SmoothStep(.37f,.83f,t),lift=reduced?1:Ease(t/.10f)*(1-ending*.65f);
                    var foot=a.Lerp(b,p)+new Vector2(0,Mathf.Sin(i*3.7f)*3*fissureUnit);
                    Place(rock,foot+Vector2.Up*height*fissureUnit*.5f,new Vector2(width,height)*fissureScale,0,active && t>=0 && start<=_craftStopAge?1-ending:0);
                    rock.FlipH=i%2==0;Parameter(rock,"lift",lift);Parameter(rock,"arrival",Mathf.Exp(-Mathf.Max(0,t-.1f)*12)*lift);
                    if(active && live && !reduced && _previousCraftAge<start && age>=start)DustBurst(start,foot,i*31,5,spatialScale:fissureScale);
                }
                Surface.Modulate=new Color(1,1,1,active?1-Mathf.SmoothStep(.5f,1.0f,age):.7f);
                Detail(0).Visible=false;
                break;
            case Shape.RockRise:
                Surface.Visible=false;
                if(Barrier is not null)
                {
                    Barrier.Visible=active && !_context.BodyProvidedByActor;
                    Barrier.Transform=_inverse*new Transform2D(0,Vector2.One*s,0,b);
                    Barrier.SetGeometry(y/s,x/s);
                    Barrier.Sample(Math.Min(age,_craftStopAge),reduced);
                }
                if(active && live && !reduced && _previousCraftAge<.10f && age>=.10f)DustBurst(.10f,b,992,17,1.2f);
                for(int i=0;i<Details!.GetChildCount();i++)
                {
                    var chip=Detail(i);float u=VfxMotePool.Rand(713+i),t=Math.Max(0,age-.1f);
                    var offset=new Vector2((u-.5f)*(30+t*100),-20-t*(65+u*40)+150*t*t)*s;
                    Place(chip,b+offset,new(8+u*7,12+u*10),t*(i%2==0?3:-3),active && !reduced && age>.10f && .10f<=_craftStopAge?1-Mathf.SmoothStep(.3f,.67f,t):0);
                }
                break;
            case Shape.Swap:
                Place(Surface,a,new(93,54),0,active?1-Mathf.SmoothStep(.1f,.6f,age):.7f);
                if(Other is not null)Place(Other,b,new(93,54),0,Surface.Modulate.A);
                Parameter(Surface,"age",visualTime);if(Other is not null)Parameter(Other,"age",visualTime+.23f);
                if(Link is not null)
                {
                    Link.Transform=_inverse;Link.Points=[a,b];Link.Width=s;
                    Link.Modulate=new Color(1,1,1,active?0:.16f+.09f*Mathf.Sin(visualTime*4));
                }
                for(int i=0;i<2;i++)
                {
                    var veil=Detail(i);var foot=i==0?a:b;
                    float envelope=active?1-Mathf.SmoothStep(.08f,.6f,age):.3f*Ease(age/.35f);
                    Place(veil,foot+Vector2.Up*35*s,new(active?76:42,92),0,envelope);
                    Parameter(veil,"age",visualTime);Parameter(veil,"active",active);
                    if(active && live && !reduced && _previousCraftAge<0)
                        for(int j=0;j<10;j++)
                        {
                            float u=VfxMotePool.Rand(312+i*33+j),v=VfxMotePool.Rand(71+j);
                            Motes?.Emit(0,foot+new Vector2((u-.5f)*38,-v*58)*s,new Vector2((u-.5f)*22,-25-v*20)*s,
                                Vector2.Zero,.35f+v*.35f,6*s,2*s,u*6,0,new(.64f,.65f,1,.65f));
                        }
                }
                break;
            default:return false;
        }
        if(Motes is not null){Motes.Visible=!reduced;Motes.Render(age,_inverse,reduced);}
        _previousPoint=b;_hasPreviousPoint=true;_previousCraftAge=age;
        return true;
    }
    private void Trail(float age,Vector2 point,Vector2 direction,bool emitting,bool acid)
    {
        if(Motes is null || !emitting)return;
        float s=_stage.UnitScale,interval=acid?.035f:.035f;
        while(_trailIndex*interval<=age)
        {
            float at=_trailIndex*interval;int n=_trailIndex++;float u=VfxMotePool.Rand(n+71);
            float fraction=age<=_previousCraftAge?1:Mathf.Clamp((at-_previousCraftAge)/(age-_previousCraftAge),0,1);
            var birth=_hasPreviousPoint?_previousPoint.Lerp(point,fraction):point;
            Motes.Emit(at,birth-direction*8*s,-direction*(acid?14:24)*s+Vector2.Up*(u-.5f)*9*s,
                acid?Vector2.Down*60*s:Vector2.Zero,acid?.27f:.20f,(acid?6+u*5:4+u*3)*s,acid?2*s:12*s,
                u*6,0,acid?new(.46f,.7f,.09f,.8f):new(.6f,.65f,.67f,.24f),cell:n%4);
        }
    }
}
