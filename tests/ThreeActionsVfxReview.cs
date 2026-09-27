using System;
using System.IO;
using System.Linq;
using Godot;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Battle;
using TowerAutobattler.Abilities;
using TowerAutobattler.Vfx;

// Authored actors and the production catalog/player; no simulation or saved state.
public partial class ThreeActionsVfxReview : Node2D, IVfxStage
{
    private float _zoom=1;
    private Vector2 _offset;
    public float UnitScale => 1.5f*_zoom;
    public Vector2 Project(Vector2 p, bool ground) => p * new Vector2(76, 46)*_zoom+_offset + (ground ? Vector2.Zero : new Vector2(0, -33)*_zoom);
    public float RadiusPixels(float r) => r * 76*_zoom;
    public override async void _Ready()
    {
        try
        {
            GetWindow().Size = new(1060, 790); GetWindow().ContentScaleSize = new(1060, 790);
            var args = OS.GetCmdlineUserArgs();
            var solo=args.FirstOrDefault(x=>x.StartsWith("--solo="))?[7..];
            if(solo is not null)
            {
                _zoom=solo=="rush"?1:1.75f;
                var row=solo=="fire"?4:solo=="punch"?9:14;
                _offset=new Vector2(160,340)-new Vector2(152,row*46)*_zoom;
                GetWindow().Size=new(1060,480);GetWindow().ContentScaleSize=new(1060,480);
                foreach(var child in GetChildren())
                    if(child is Label || child is ColorRect && child.Name.ToString().StartsWith("Floor")) ((CanvasItem)child).Visible=false;
            }
            var capture = args.FirstOrDefault(x => x.StartsWith("--capture="))?[10..];
            if (capture is not null) Directory.CreateDirectory(capture);
            var player = GetNode<VfxPlayer>("Player"); player.Bind(this); player.SetProcess(false);
            player.ReducedMotion = args.Contains("--reduced");
            if(args.Contains("--light")) GetNode<ColorRect>("Background").Color = new("818b91");
            var left = args.Contains("--left"); var diagonal=args.Contains("--diagonal");
            Vector2 Heading() => diagonal ? new Vector2(.9f,.43589f) : left ? Vector2.Left : Vector2.Right;
            Vector2 Start(float y) => new(left ? 10 : 2, y);
            VfxContext Context(float y, float radius, float? progress = null) => new(Start(y),Start(y)+Heading()*3,radius,
                Direction:Heading(),TravelProgress:progress,ConeAngleDegrees:70);
            foreach (var (name,y) in new[]{("Breather",4f),("Brawler",9f),("Brute",14f)})
            {
                var unit=GetNode<UnitContentRoot>(name); unit.SnapPresentation(Project(Start(y),true),100,100);
                unit.SetPresentationSpeed(1,1);
                unit.Scale=Vector2.One*UnitScale;
                if(solo is not null)
                {
                    unit.Visible=name==(solo=="fire"?"Breather":solo=="punch"?"Brawler":"Brute");
                    unit.GetNode<CanvasItem>("HealthViewComponent").Visible=false;
                }
                unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent").FaceHorizontal(left?-1:1);
            }
            var brawler=GetNode<UnitContentRoot>("Brawler");
            var punchAnimation=brawler.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
            var brute=GetNode<UnitContentRoot>("Brute");
            var rushAnimation=brute.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
            var punchTiming=new BattleAttackTiming(1,.6f);
            var runFrames=new System.Collections.Generic.HashSet<int>();
            for(var f=0; f<264; f++)
            {
                // Use the production actor API, with one shared fixed-step clock for
                // this isolated recording. Gameplay owns these same phase boundaries.
                if(f>36 && f<=96) brawler.StepAttackPresentation(1f/60);
                if(f==24) { player.Play("cone_breath",Context(4,3,0),"breath"); player.Play("trample_warning",new(Start(14),Start(14)+Heading()*8,.7f,Direction:Heading()),"warning"); }
                if(f==36) { player.Play("grit_charge",Context(9,.8f),"charge"); brawler.BeginAttackPresentation(punchTiming,"grit_punch");punchAnimation.SetProcess(false); }
                if(f==72) { brawler.CompleteAttackWindup(.6f); player.End("charge",VfxEndReason.ScopeEnded);player.Play("grit_punch",Context(9,.8f),"punch"); }
                if(f==84) { player.End("breath",VfxEndReason.ScopeEnded); player.Play("cone_breath",Context(4,3,1),"breath"); }
                if(f==96) { player.End("warning",VfxEndReason.ScopeEnded); player.Play("trample_rush",new(Start(14),Start(14)+Heading()*8,.7f,Direction:Heading(),TargetDisplayPosition:Project(Start(14),true)),"rush"); }
                if(f>=96 && f<=192)
                {
                    var p=Start(14)+Heading()*Mathf.Min(8,(f-96)/60f*5);
                    brute.PresentDisplacement(new(DisplacementKind.Charge,Start(14),Start(14)+Heading()*8,
                        16,16,Mathf.Clamp((f-96)/96f,0,1),f==192,false,0,Vector2.Zero,0),
                        Project(p,true),Project(Start(14),true),0,true);
                    player.UpdateContext("rush",new(Start(14),Start(14)+Heading()*8,.7f,Direction:Heading(),TargetDisplayPosition:brute.Position));
                    if(f<192)
                    {
                        if(rushAnimation.ActiveCue is not ("move" or "run"))throw new Exception("Charge failed to own running pose");
                        runFrames.Add(rushAnimation.GetNode<AnimatedSprite2D>("AnimatedSprite2D").Frame);
                    }
                }
                if(f==192) { player.End("breath",VfxEndReason.Completed); player.End("rush",VfxEndReason.Completed); }
                player.Advance(1f/60);
                if(solo is not null)
                    foreach(var visual in player.GetChildren().OfType<VfxInstance>())
                        visual.Visible=solo switch { "fire"=>visual.SceneFilePath.Contains("cone_breath"),
                            "punch"=>visual.SceneFilePath.Contains("grit_"),_=>visual.SceneFilePath.Contains("trample_") };
                if(f==115)
                {
                    var ages=player.GetChildren().OfType<VfxInstance>().Select(v=>(v,v.Playback.Age)).ToArray();
                    player.Advance(0);
                    if(ages.Any(x=>x.v.Playback.Age!=x.Age))throw new Exception("Paused clock advanced");
                }
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if(capture is not null && f%2==0 && DisplayServer.GetName()!="headless")
                {
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng(Path.Combine(capture,$"{f/2:D4}.png"));
                }
            }
            if(runFrames.Count<6)throw new Exception("Charge is sliding or restarting its run clip");
            CheckPunchTiming(brawler);
            if(player.ActiveCount!=0) throw new Exception("Action tails failed to expire");
            player.Play("cone_breath",Context(4,3,1),"cancel"); player.Advance(.2f);
            var breath=player.GetChildren().OfType<VfxInstance>().Single();
            var tongues=breath.GetNode<VfxMotePool>("Track/Tongues");int births=tongues.BirthCount;
            player.End("cancel",VfxEndReason.OwnerDefeated);player.Advance(.2f);
            if(tongues.BirthCount!=births || player.ActiveCount!=1)throw new Exception("Cancellation must stop supply and retain living particles");
            player.Advance(1); if(player.ActiveCount!=0)throw new Exception("Cancelled breath leaked");
            var rc=new VfxContext(Start(14),Start(14)+Heading()*8,.7f,Direction:Heading(),TargetDisplayPosition:Project(Start(14),true));
            player.Play("trample_rush",rc,"trail");player.Advance(.1f);
            var rush=player.GetChildren().OfType<VfxInstance>().Single();var pool=rush.GetNode<VfxMotePool>("Layers/Dust");
            var mote=pool.GetChildren().OfType<Sprite2D>().First();var before=mote.GlobalPosition;
            // Updating the owner without advancing the clock must leave previously born dust still.
            player.UpdateContext("trail",rc with{TargetDisplayPosition=Project(Start(14)+Heading()*2,true)});player.Advance(0);
            if(!mote.GlobalPosition.IsEqualApprox(before))throw new Exception("Dust dragged by owner movement");
            player.End("trail",VfxEndReason.Completed);int count=pool.BirthCount;player.Advance(.2f);
            if(pool.BirthCount!=count)throw new Exception("Dust emitted after stop");
            player.Clear(); GD.Print("THREE_ACTIONS_REVIEW_OK shared-scenes clock tails cancel cleanup"); GetTree().Quit();
        }
        catch(Exception e) { GD.PrintErr("THREE_ACTIONS_REVIEW_FAILED "+e); GetTree().Quit(1); }
    }

    private static void CheckPunchTiming(UnitContentRoot actor)
    {
        var animation=actor.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
        var sprite=animation.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        void Require(bool value,string message) { if(!value)throw new Exception(message); }
        animation.ResetPresentation();
        actor.BeginAttackPresentation(new(1,.6f));actor.StepAttackPresentation(.3f);
        Require(animation.ActiveCue=="attack","basic attack clip unchanged");
        actor.BeginAttackPresentation(new(1,.6f),"grit_punch");
        actor.StepAttackPresentation(.45f);
        Require(animation.ActiveCue=="grit_punch" && sprite.Frame is >=4 and <=6,"heavy anticipation has distinct timing and clip");
        actor.SetPresentationPaused(true);var frame=sprite.Frame;actor.StepAttackPresentation(.5f);
        Require(sprite.Frame==frame,"pause freezes charged character pose");actor.SetPresentationPaused(false);
        actor.StepAttackPresentation(2);
        Require(sprite.Frame==8,"windup waits before strike for authoritative release");
        animation.PlayCue("skill_cast");animation.PlayCue("attack");
        Require(animation.ActiveCue=="grit_punch" && animation.PendingCue=="","decorative cues cannot replace or queue behind charged action");
        actor.CompleteAttackWindup(.6f);
        Require(sprite.Frame==9,"strike frame lands exactly with release");
        actor.StepAttackPresentation(.15f);
        Require(sprite.Frame>9,"released pose follows through");
        actor.StepAttackPresentation(.26f);
        Require(animation.ActiveCue=="idle","recovery finishes within gameplay action window");
        actor.BeginAttackPresentation(new(1,.6f),"grit_punch");actor.StepAttackPresentation(.2f);actor.CancelAttackPresentation();
        Require(animation.ActiveCue=="idle","cancel clears charging pose");
        actor.BeginAttackPresentation(new(1,.6f),"grit_punch");animation.SetDisplacementCue("move");actor.CompleteAttackWindup(.6f);
        Require(animation.ActiveCue=="move","displacement owns pose after interrupt");animation.SetDisplacementCue("");
        actor.BeginAttackPresentation(new(1,.6f),"grit_punch");animation.PlayCue("defeated");actor.CompleteAttackWindup(.6f);
        Require(animation.IsTerminal,"late release cannot revive defeated pose");
        animation.ResetPresentation();actor.SetPresentationSpeed(2,2);
        actor.BeginAttackPresentation(new(1,.6f),"grit_punch");animation._Process(0);animation._Process(.225);
        Require(sprite.Frame is >=4 and <=6,"combat speed scales anticipation clock");
        animation.ResetPresentation();actor.SetPresentationSpeed(1,1);
    }
}
