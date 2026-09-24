using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;
using TowerAutobattler.Vfx;

public partial class EnemyTrampleVisualSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var capture=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--capture="))?[10..];
            if(capture is not null)Directory.CreateDirectory(capture);
            var gate=await GamePackagePublisher.CreateReadyAsync(this,GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package=gate.Package??throw new InvalidOperationException(string.Join(';',gate.Report.CoreErrors));
            var store=new BattleLabPresetStore(GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres"));
            var preset=store.BuiltIns.Single(p=>p.Key.StartsWith("EE03 ·",StringComparison.Ordinal));
            var config=new BattleLabPreparationAdapter(new BattleLabContentIndex(package)).Build(BattleLabPresetStore.ToSnapshot(preset.Value));
            // Isolate the action in the production preset; these overrides never touch a run/save.
            for(var i=0;i<config.Spawns.Count;i++)
                config.Spawns[i]=config.Spawns[i] with {Unit=config.Spawns[i].Unit with
                    {Behavior=new(Stationary:true,DisableBasicAttacks:true),MaxHealth=2000}};
            var screen=GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
            AddChild(screen);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            screen.StartBattle(package.Content,config,"EE03 · 破阵巨兽 · 分边冲锋");
            screen.SetLabControlsVisible(true);screen.SetSpeed(1);screen.SetPaused(false);
            var player=screen.GetNode<RangedAttackLayer>("%RangedAttackLayer").GetNode<VfxPlayer>("Player");
            bool warning=false,rush=false,paused=false;var captured=0;
            for(var frame=0;frame<410;frame++)
            {
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Require(screen.LastRuntimeFailure.Length==0,screen.LastRuntimeFailure);
                warning|=player.GetChildren().OfType<VfxInstance>().Any(v=>v.GetNodeOrNull<VfxTrampleTrack>("Layers") is {Rush:false});
                rush|=player.GetChildren().OfType<VfxInstance>().Any(v=>v.GetNodeOrNull<VfxTrampleTrack>("Layers") is {Rush:true});
                if(!paused && screen.TickIndex>=14)
                {
                    await Click(screen.GetNode<Button>("%PauseButton"));
                    Require(screen.IsPaused,"real pause input");var tick=screen.TickIndex;
                    var ages=player.GetChildren().OfType<VfxInstance>().Select(v=>(v,v.Playback.Age)).ToArray();
                    for(var j=0;j<4;j++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    Require(screen.TickIndex==tick && ages.All(p=>p.v.Playback.Age==p.Age),"paused warning and simulation");
                    await Click(screen.GetNode<Button>("%PauseButton"));paused=true;
                }
                if(capture is not null && frame%4==0)
                {
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    Require(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(capture,$"{captured++:D4}.png"))==Error.Ok,"capture");
                }
            }
            Require(warning && rush,"shared warning and rush resources visible in formal battle");
            screen.StopBattle();Require(player.ActiveCount==0,"clear phase effects at battle teardown");
            screen.QueueFree();
            GD.Print("ENEMY_TRAMPLE_VISUAL_OK formal-screen warning rush pause-input teardown");GetTree().Quit();
        }
        catch(Exception e){GD.PrintErr("ENEMY_TRAMPLE_VISUAL_FAILED "+e);GetTree().Quit(1);}
    }
    private async Task Click(Button button)
    {
        var p=button.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion{Position=p,GlobalPosition=p},true);
        GetViewport().PushInput(new InputEventMouseButton{Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,ButtonMask=MouseButtonMask.Left,Pressed=true},true);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        GetViewport().PushInput(new InputEventMouseButton{Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,Pressed=false},true);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
