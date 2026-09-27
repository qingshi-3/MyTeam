using System;
using System.IO;
using System.Linq;
using Godot;
using TowerAutobattler.Vfx;

// Catalog-wide visual evidence, with the same production scenes and clocks.
public partial class VfxLibraryReview : Node2D, IVfxStage
{
    public float UnitScale=>1.25f;
    public Vector2 Project(Vector2 p,bool ground)=>new Vector2(480,300)+p*new Vector2(88,68)*UnitScale+(ground?Vector2.Zero:Vector2.Up*45);
    public float RadiusPixels(float r)=>r*88*UnitScale;
    public override async void _Ready()
    {
        try
        {
            GetWindow().Mode=Window.ModeEnum.Windowed;
            GetWindow().Size=new(960,540);GetWindow().ContentScaleSize=new(960,540);
            Engine.MaxFps=30;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var args=OS.GetCmdlineUserArgs();
            var output=args.FirstOrDefault(a=>a.StartsWith("--capture="))?[10..]??".godot/library-polish/before";
            var ids=args.FirstOrDefault(a=>a.StartsWith("--effects="))?[10..]?.Split(',');
            bool reduced=args.Contains("--reduced");
            if(args.Contains("--light"))GetNode<ColorRect>("Background").Color=new("a5aeb7");
            var player=GetNode<VfxPlayer>("Player");player.Bind(this);player.SetProcess(false);player.ReducedMotion=reduced;
            var source=GetNode<AnimatedSprite2D>("Source");var target=GetNode<AnimatedSprite2D>("Target");
            source.Pause();target.Pause();source.Scale=target.Scale=Vector2.One*1.4f;
            foreach(var def in player.Catalog.Effects.Where(d=>ids is null||ids.Contains(d.StableId)))
            {
                var id=def.StableId;
                float distance=VfxPreviewSample.Distance(def),flight=VfxPreviewSample.Flight(id);
                float radius=VfxPreviewSample.Radius(def);
                source.Visible=VfxPreviewSample.SelfCentered(def)||VfxPreviewSample.Directional(def);
                target.Visible=VfxPreviewSample.ShowTarget(def);
                source.SpriteFrames=def.PreviewFrames??GD.Load<SpriteFrames>("res://assets/donor-units/f6_general/frames.tres");
                source.Animation="idle";source.SetFrameAndProgress(0,0);
                VfxContext Context(float age=0)
                {
                    var g=VfxPreviewSample.Geometry(def,Vector2.Right,distance,age,flight);
                    source.Position=Project(g.SourceUnit,false);target.Position=Project(g.TargetUnit,false);
                    return new(g.Source,g.Target,radius,Direction:Vector2.Right,TravelProgress:null,
                        Body:def.AtSource?VfxBodyVisual.Capture(source,player.GlobalTransform.AffineInverse()):null);
                }
                string dir=Path.Combine(output,id);Directory.CreateDirectory(dir);
                GetNode<Label>("Name").Text=def.DisplayName;
                float endAt=Mathf.Min(def.Persistent?2.1f:def.Duration,VfxPreviewSample.EndAt(def,flight));
                int frames=(int)Math.Ceiling(Math.Max(2.6f,endAt+def.ReleaseDuration+.25f)*30);
                player.Play(id,Context(),"sample");
                for(int f=0;f<frames;f++)
                {
                    float age=f/30f;
                    player.UpdateContext("sample",Context(Math.Min(age,endAt)));
                    if(id=="acid_spit"&&f==(int)(endAt*30))player.Impact("sample");
                    if(id=="shield"&&f==30)player.Impact("sample");
                    if(def.Persistent&&f==(int)(endAt*30))player.End("sample",VfxEndReason.Completed);
                    player.Advance(f==0?0:1f/30);
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    if(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(dir,$"{f:D4}.png"))!=Error.Ok)throw new Exception("capture failed");
                }
                player.Clear();GD.Print("LIBRARY_REVIEW_OK "+id+" frames="+frames);
            }
            GetTree().Quit();
        }
        catch(Exception e){GD.PrintErr(e);GetTree().Quit(1);}
    }
}
