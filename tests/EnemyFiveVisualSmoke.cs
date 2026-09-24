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

public partial class EnemyFiveVisualSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var capture=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--capture="))?[10..];
            var only=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--case="))?[7..];
            var gate=await GamePackagePublisher.CreateReadyAsync(this,GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package=gate.Package??throw new InvalidOperationException(string.Join(';',gate.Report.CoreErrors));
            var store=new BattleLabPresetStore(GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres"));
            foreach(var prefix in new[]{"EE07","EE08","EE09","EB01","EB02"}.Where(p=>only is null || p==only))
            {
                var directory=capture is null?null:Path.Combine(capture,prefix);if(directory is not null)Directory.CreateDirectory(directory);
                var preset=store.BuiltIns.Single(p=>p.Key.StartsWith(prefix+" ·",StringComparison.Ordinal));
                var config=new BattleLabPreparationAdapter(new BattleLabContentIndex(package)).Build(BattleLabPresetStore.ToSnapshot(preset.Value));
                for(var i=0;i<config.Spawns.Count;i++)
                {
                    var spawn=config.Spawns[i];
                    config.Spawns[i]=spawn with{Unit=spawn.Unit with{Behavior=new(Stationary:true,DisableBasicAttacks:true),MaxHealth=2000},
                        HealthRatio=prefix=="EB02" && spawn.Team==1?.49f:1};
                }
                var screen=GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
                AddChild(screen);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                screen.StartBattle(package.Content,config,preset.Key);screen.SetLabControlsVisible(true);screen.SetSpeed(1);screen.SetPaused(false);
                var player=screen.GetNode<RangedAttackLayer>("%RangedAttackLayer").GetNode<VfxPlayer>("Player");
                var seen=new System.Collections.Generic.HashSet<VfxEnemyActionTrack.Shape>();bool paused=false;int captured=0;
                for(int frame=0;frame<880 && screen.TickIndex<105;frame++)
                {
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    Require(screen.LastRuntimeFailure.Length==0,screen.LastRuntimeFailure);
                    foreach(var visual in player.GetChildren().OfType<VfxInstance>())
                        if(visual.GetNodeOrNull<VfxEnemyActionTrack>("Track") is {} track)seen.Add(track.Kind);
                    if(!paused && screen.TickIndex>=(prefix=="EB01"?65:prefix=="EB02"?5:14))
                    {
                        await Click(screen.GetNode<Button>("%PauseButton"));Require(screen.IsPaused,"pause input "+prefix);
                        var tick=screen.TickIndex;var ages=player.GetChildren().OfType<VfxInstance>().Select(v=>(v,v.Playback.Age)).ToArray();
                        for(int j=0;j<4;j++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                        Require(screen.TickIndex==tick && ages.All(p=>p.v.Playback.Age==p.Age),"pause freezes gameplay and VFX "+prefix);
                        await Click(screen.GetNode<Button>("%PauseButton"));paused=true;
                    }
                    if(directory is not null && frame%4==0)
                    {
                        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                        Require(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(directory,$"{captured++:D4}.png"))==Error.Ok,"frame capture");
                    }
                }
                var expected=prefix switch{"EE07"=>VfxEnemyActionTrack.Shape.Cone,"EE08"=>VfxEnemyActionTrack.Shape.Blade,
                    "EE09"=>VfxEnemyActionTrack.Shape.Swap,"EB01"=>VfxEnemyActionTrack.Shape.Fissure,_=>VfxEnemyActionTrack.Shape.Shell};
                Require(seen.Contains(expected),prefix+" authored production VFX reached");
                screen.StopBattle();Require(player.ActiveCount==0,"scope teardown "+prefix);screen.QueueFree();
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                GD.Print("ENEMY_FIVE_VISUAL_CASE_OK "+prefix+" "+string.Join(',',seen));
            }
            GD.Print("ENEMY_FIVE_VISUAL_OK real pause-input; shared battle resources; teardown");GetTree().Quit();
        }
        catch(Exception e){GD.PrintErr("ENEMY_FIVE_VISUAL_FAILED "+e);GetTree().Quit(1);}
    }
    private async Task Click(Button button)
    {
        var p=button.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion{Position=p,GlobalPosition=p},true);
        GetViewport().PushInput(new InputEventMouseButton{Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,ButtonMask=MouseButtonMask.Left,Pressed=true},true);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        GetViewport().PushInput(new InputEventMouseButton{Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,Pressed=false},true);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }
    private static void Require(bool b,string text){if(!b)throw new InvalidOperationException(text);}
}
