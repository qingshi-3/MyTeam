using System;
using System.IO;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Components;
using TowerAutobattler.Presentation;
using TowerAutobattler.Battle;
using TowerAutobattler.Vfx;

// Isolated choreography of the production actors and effects; no run or save state.
public partial class SixActionsVfxReview : Node2D, IVfxStage
{
    public float UnitScale => 1.7f;
    private bool _rockStudy;
    private Vector2 Pitch => _rockStudy?BattlefieldLayout.BaseCellPitch*UnitScale:new(80,50);
    public Vector2 Project(Vector2 p,bool ground) => new Vector2(50,85)+p*Pitch+(ground?Vector2.Zero:Vector2.Up*39);
    public float RadiusPixels(float radius) => radius*Pitch.X;
    private static readonly string[] Ids=["returning_blade","mechanical_hook","ground_fissure","rock_raise","position_swap","acid_spit"];
    public override async void _Ready()
    {
        try
        {
            GetWindow().Size=new(1060,480);GetWindow().ContentScaleSize=new(1060,480);
            var args=OS.GetCmdlineUserArgs();
            var output=args.FirstOrDefault(a=>a.StartsWith("--capture="))?[10..];
            var selected=args.FirstOrDefault(a=>a.StartsWith("--effect="))?[9..];
            float fissureScale=float.Parse(args.FirstOrDefault(a=>a.StartsWith("--fissure-scale="))?[16..]??"1",System.Globalization.CultureInfo.InvariantCulture);
            bool fissureStudy=args.Contains("--fissure-study");
            var player=GetNode<VfxPlayer>("Player");player.Bind(this);player.SetProcess(false);player.ReducedMotion=args.Contains("--reduced");
            if(args.Contains("--light"))GetNode<ColorRect>("Background").Color=new("8d969d");
            CheckAcidContact();
            foreach(var id in Ids.Where(id=>selected is null || id==selected))
            {
                // A physical shield must use the production ratio of unit art to floor
                // distance. Enlarging only the actor turns a safe gap into a face overlap.
                _rockStudy=id=="rock_raise";
                var sourcePath=id switch {
                    "returning_blade"=>"enemies/enemy_ee08_return_blade",
                    "mechanical_hook"=>"heroes/hero_hc39_hook_machine",
                    "position_swap"=>"enemies/enemy_ee09_swap_envoy",
                    "acid_spit"=>"enemies/enemy_eb02_shell_matriarch",
                    _=>"enemies/enemy_eb01_rampart_warden"};
                var source=GD.Load<PackedScene>($"res://content/{sourcePath}.tscn").Instantiate<UnitContentRoot>();AddChild(source);
                var target=GD.Load<PackedScene>("res://content/heroes/hero_hc03_iron_guard.tscn").Instantiate<UnitContentRoot>();AddChild(target);
                source.Scale=target.Scale=Vector2.One*UnitScale;
                source.ZIndex=target.ZIndex=1;
                if(id=="acid_spit")source.GetNode<Node2D>("VisualRoot").Scale=Vector2.One*.56f;
                source.GetNode<CanvasItem>("HealthViewComponent").Visible=false;target.GetNode<CanvasItem>("HealthViewComponent").Visible=false;
                var a=new Vector2(2,5);var b=new Vector2(8.5f,5);
                if(id=="ground_fissure" && fissureStudy){a=new(1,6.5f);b=a+Vector2.Right*5.2f*fissureScale;}
                if(id=="rock_raise")
                {
                    a=new(2.5f,2);
                    var wallDefinition=GD.Load<UnitDefinition>("res://content/definitions/enemies/enemy_rampart_segment.tres");
                    b=a+Vector2.Right*(source.Definition.BodyRadius+wallDefinition.BodyRadius+.15f);
                }
                if(args.Contains("--left"))(a,b)=(b,a);
                if(args.Contains("--diagonal"))
                {
                    if(_rockStudy){a.Y+=.5f;b.Y-=.15f;}
                    else b+=new Vector2(0,-1.4f);
                }
                source.SnapPresentation(Project(a,true),100,100);target.SnapPresentation(Project(b,true),100,100);
                source.FaceToward(target.Position);target.FaceToward(source.Position);
                if(id=="ground_fissure")source.Visible=false;
                if(id=="rock_raise")target.Visible=false;
                var directory=output is null?null:Path.Combine(output,id);if(directory is not null)Directory.CreateDirectory(directory);
                var walls=new System.Collections.Generic.List<UnitContentRoot>();
                for(var f=0;f<240;f++)
                {
                    float t=(f-24)/60f;
                    var direction=(b-a).Normalized();
                    VfxContext Context(Vector2 end,float phase=1) => new(a,end,id=="ground_fissure"?(fissureStudy?.28f*fissureScale:.3f):id=="rock_raise"?.42f:0,
                        Direction:direction,TravelProgress:phase);
                    if(f==24)
                    {
                        var initial=id is "returning_blade" or "acid_spit" or "mechanical_hook"?a:b;
                        if(id=="rock_raise")
                        {
                            var normal=new Vector2(-direction.Y,direction.X);
                            for(int j=-1;j<=1;j++)
                            {
                                var foot=b+normal*(j*.84f);
                                var wall=GD.Load<PackedScene>("res://content/enemies/enemy_rampart_segment.tscn").Instantiate<UnitContentRoot>();AddChild(wall);
                                wall.Scale=Vector2.One*UnitScale;wall.ZIndex=1;wall.SnapPresentation(Project(foot,true),75,75);
                                wall.GetNode<CanvasItem>("HealthViewComponent").Visible=false;
                                wall.BarrierVisual!.SetProcess(false);
                                wall.BarrierVisual.SetGeometry((Project(foot+normal,true)-Project(foot,true))/UnitScale,
                                    (Project(foot+direction,true)-Project(foot,true))/UnitScale,j);
                                walls.Add(wall);
                                player.Play(id,Context(foot) with {BodyProvidedByActor=true},"wall"+j);
                            }
                        }
                        else player.Play(id,Context(initial,id is "position_swap" or "ground_fissure"?0:1),"action");
                    }
                    if(t>=0)
                    {
                        foreach(var wall in walls)
                        {
                            wall.BarrierVisual!.Sample(t,player.ReducedMotion);
                            wall.Modulate=new Color(1,1,1,1-Mathf.SmoothStep(2.5f,3.2f,t));
                        }
                        if(id=="rock_raise" && f==60)for(int j=-1;j<=1;j++)player.End("wall"+j,VfxEndReason.Completed);
                        if(id=="returning_blade" && t<=1.7f)
                        {
                            float u=t<.85f?t/.85f:1-(t-.85f)/.85f;var p=a.Lerp(b,u);
                            player.UpdateContext("action",Context(p) with {Source=p-direction*.1f});
                        }
                        if(id=="mechanical_hook" && t<=.85f)
                        {
                            float u=t<.325f?t/.325f:t<.4f?1:1-(t-.4f)/.45f;
                            var p=a.Lerp(b,u);player.UpdateContext("action",Context(p));
                            if(t>.4f)target.Position=Project(p,true);
                        }
                        if(id=="acid_spit" && t<=.8f)
                        {
                            var p=a.Lerp(b,t/.8f);player.UpdateContext("action",Context(p) with {Source=p-direction*.1f});
                        }
                    }
                    if((id=="position_swap" && f==90)||(id=="ground_fissure" && f==60))
                    {
                        player.End("action",VfxEndReason.ScopeEnded);player.Play(id,Context(b),"action");
                        if(id=="position_swap"){source.Position=Project(b,true);target.Position=Project(a,true);}
                    }
                    if(id=="acid_spit" && f==72){player.Impact("action");player.End("action",VfxEndReason.Completed);}
                    if((id=="returning_blade" && f==126)||(id=="mechanical_hook" && f==75)||(id is "ground_fissure" or "rock_raise" && f==120)||(id=="position_swap" && f==138))player.End("action",VfxEndReason.Completed);
                    player.Advance(1f/60);
                    if(_rockStudy && f==80 && !args.Contains("--diagonal"))CheckBarrierClearance(source,walls,(b-a).Normalized());
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    if(directory is not null && f%2==0 && DisplayServer.GetName()!="headless")
                    {
                        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                        GetViewport().GetTexture().GetImage().SavePng(Path.Combine(directory,$"{f/2:D4}.png"));
                    }
                }
                if(player.ActiveCount!=0)throw new Exception(id+" retained released instances");
                player.Play(id,new(a,b,.6f),"cancel");player.Advance(.1f);
                var ages=player.GetChildren().OfType<VfxInstance>().Select(v=>(v,v.Playback.Age)).ToArray();player.Advance(0);
                if(ages.Any(x=>x.v.Playback.Age!=x.Age))throw new Exception(id+" paused clock advanced");
                var pools=player.GetChildren().OfType<VfxInstance>().Select(v=>v.GetNodeOrNull<VfxEnemyActionTrack>("Track")?.Motes).Where(p=>p is not null).ToArray();
                var births=pools.Select(p=>p!.BirthCount).ToArray();
                player.End("cancel",VfxEndReason.OwnerDefeated);player.Advance(.1f);
                if(pools.Where((p,i)=>p!.BirthCount!=births[i]).Any())throw new Exception(id+" emitted after cancellation");
                player.Advance(2);
                if(player.ActiveCount!=0)throw new Exception(id+" cancel leaked");
                player.Clear();foreach(var wall in walls)wall.Free();source.Free();target.Free();GD.Print("SIX_ACTION_REVIEW_OK "+id);
            }
            if(selected is null or "ground_fissure")CheckFissureScaling();
            GetTree().Quit();
        }
        catch(Exception error){GD.PrintErr("SIX_ACTION_REVIEW_FAILED "+error);GetTree().Quit(1);}
    }
    private void CheckBarrierClearance(UnitContentRoot source,System.Collections.Generic.List<UnitContentRoot> walls,Vector2 direction)
    {
        var sprite=source.GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D");
        var texture=sprite.SpriteFrames.GetFrameTexture(sprite.Animation,sprite.Frame);
        var used=texture.GetImage().GetUsedRect();
        var offset=sprite.Offset-(sprite.Centered?texture.GetSize()*.5f:Vector2.Zero);
        float sign=Mathf.Sign(direction.X),actorFront=float.NegativeInfinity,wallRear=float.PositiveInfinity;
        foreach(var corner in new Vector2[]{used.Position,used.Position+new Vector2(used.Size.X,0),used.End,used.Position+new Vector2(0,used.Size.Y)})
        {
            var point=corner+offset;if(sprite.FlipH)point.X=-point.X;
            actorFront=Math.Max(actorFront,(sprite.GlobalTransform*point).X*sign);
        }
        foreach(var wall in walls)foreach(var pillar in wall.BarrierVisual!.Pillars)
        foreach(var face in pillar.Sides.Append(pillar.Crown).Where(p=>p.Visible))
        foreach(var point in face.Polygon)wallRear=Math.Min(wallRear,(face.GlobalTransform*point).X*sign);
        float gap=wallRear-actorFront;
        if(gap<12*UnitScale)throw new Exception($"wall overlaps caster silhouette or has no readable gap: {gap:0.0}px");
        GD.Print($"BARRIER_CLEARANCE_OK {(sign>0?"right":"left")} gap={gap:0.0}px");
    }
    private sealed class FissureStage(float zoom) : IVfxStage
    {
        public float UnitScale=>1.7f*zoom;
        public Vector2 Project(Vector2 p,bool ground)=>new Vector2(50,85)+new Vector2(p.X*80,p.Y*50)*zoom;
        public float RadiusPixels(float radius)=>radius*80*zoom;
    }
    private void CheckFissureScaling()
    {
        var definition=GD.Load<VfxDefinition>("res://content/vfx/ground_fissure.tres");
        foreach(var direction in new[]{Vector2.Right,Vector2.Left,new Vector2(1,-.6f).Normalized()})
        foreach(bool reduced in new[]{false,true})
        foreach(bool active in new[]{false,true})
        {
            var source=new Vector2(2,5);
            VfxInstance Create(float size,float zoom,float range=5.2f)
            {
                var instance=definition.Scene.Instantiate<VfxInstance>();
                instance.Bind(definition,new(source,source+direction*range*size,definition.ReferenceRadius*size,TravelProgress:active?1:0));
                AddChild(instance);instance.Advance(.2f,new FissureStage(zoom),reduced);
                return instance;
            }
            Node2D[] VisibleParts(VfxInstance instance)
            {
                var track=instance.GetNode<VfxEnemyActionTrack>("Track");
                return new Node2D[]{track.Surface}.Concat(track.Pieces!.GetChildren().OfType<Node2D>())
                    .Concat(track.Motes!.GetChildren().OfType<Node2D>()).Where(n=>n.IsVisibleInTree()).ToArray();
            }
            var baseline=Create(1,1);var baseAnchor=new FissureStage(1).Project(source,true);
            foreach(var (size,zoom) in new[]{(.5f,1f),(2f,1f),(1f,.75f),(2f,1.4f)})
            {
                var scaled=Create(size,zoom);var anchor=new FissureStage(zoom).Project(source,true);
                var first=VisibleParts(baseline);var second=VisibleParts(scaled);float factor=size*zoom;
                if(first.Length!=second.Length)throw new Exception("fissure scaling changed visible peak/dust count");
                for(int i=0;i<first.Length;i++)
                {
                    var expected=(first[i].GlobalPosition-baseAnchor)*factor;
                    var actual=second[i].GlobalPosition-anchor;
                    if(actual.DistanceTo(expected)>.03f ||
                        (second[i].GlobalTransform.X-first[i].GlobalTransform.X*factor).Length()>.001f ||
                        (second[i].GlobalTransform.Y-first[i].GlobalTransform.Y*factor).Length()>.001f)
                        throw new Exception($"fissure failed whole-effect scale: {first[i].Name}, size={size}, zoom={zoom}, active={active}, reduced={reduced}");
                }
                scaled.Free();
            }
            // Terrain can shorten the line without changing the size of its rock peaks.
            var clipped=Create(1,1,2.6f);
            var fullRock=baseline.GetNode<VfxEnemyActionTrack>("Track").Pieces!.GetChild<Node2D>(0);
            var shortRock=clipped.GetNode<VfxEnemyActionTrack>("Track").Pieces!.GetChild<Node2D>(0);
            if(!fullRock.GlobalTransform.X.IsEqualApprox(shortRock.GlobalTransform.X) || !fullRock.GlobalTransform.Y.IsEqualApprox(shortRock.GlobalTransform.Y))
                throw new Exception("clipped fissure incorrectly shrank its peaks");
            clipped.Free();baseline.Free();
        }
        GD.Print("FISSURE_SCALE_OK whole-effect 0.5x/2x stage-zoom left/diagonal prepare/release reduced clipped-line");
    }
    private void CheckAcidContact()
    {
        var layer=GD.Load<PackedScene>("res://scenes/effects/RangedAttackLayer.tscn").Instantiate<RangedAttackLayer>();AddChild(layer);
        layer.SetProcess(false);var player=layer.GetNode<VfxPlayer>("Player");player.SetProcess(false);player.Bind(layer);
        var start=new Vector2(1,2);var end=new Vector2(3,2);
        foreach(bool snap in new[]{false,true})foreach(bool hit in new[]{false,true})
        {
            layer.Present([new BattleEvent(1,"projectile_spawn","","",0,default,"",start,901,end,SourceVfx:"acid_spit")],snap);
            var instance=player.GetChildren().OfType<VfxInstance>().Single();var track=instance.GetNode<VfxEnemyActionTrack>("Track");
            if(hit)layer.Present([new BattleEvent(2,"projectile_impact","","",0,default,"",end,901)],snap);
            layer.Present([new BattleEvent(2,"projectile_end","","",0,default,"",end,901)],snap);
            if(!snap)
            {
                layer.AdvanceFlights(.05f);player.Advance(.05f);
                if(track.Details!.GetChild<Sprite2D>(0).Visible)throw new Exception("acid splashed before visible arrival");
                layer.AdvanceFlights(.05f);player.Advance(0);
                if(instance.Position.DistanceTo(layer.Project(end,false))>.01f)throw new Exception("acid terminal point drifted");
                layer.AdvanceFlights(.016f);player.Advance(.016f);
            }
            if(track.Details!.GetChild<Sprite2D>(0).Visible!=hit)throw new Exception("acid hit/miss aftermath mismatch");
            if(layer.ProjectileCount!=0 || player.ActiveCount!=1)throw new Exception("acid tail ownership mismatch");
            player.Advance(1);if(player.ActiveCount!=0)throw new Exception("acid tail leaked");
        }
        layer.Present([new BattleEvent(1,"projectile_spawn","","",0,default,"",start,902,end,SourceVfx:"acid_spit")],false);
        layer.Clear();if(player.ActiveCount!=0 || layer.ProjectileCount!=0)throw new Exception("acid scope clear leaked");
        layer.Free();GD.Print("ACID_CONTACT_OK hit miss terminal interpolation snap tail scope-clear");
    }
}
