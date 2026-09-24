using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;
using TowerAutobattler.Vfx;

public partial class DuelGritHookVisualSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var args=OS.GetCmdlineUserArgs();
            var prefix=args.FirstOrDefault(a=>a.StartsWith("--hero="))?[7..]??"HC39";
            var capture=args.FirstOrDefault(a=>a.StartsWith("--capture="))?[10..];
            if(capture is not null)Directory.CreateDirectory(capture);
            var gate=await GamePackagePublisher.CreateReadyAsync(this,GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package=gate.Package??throw new InvalidOperationException(string.Join(';',gate.Report.CoreErrors));
            var store=new BattleLabPresetStore(GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres"));
            var preset=store.BuiltIns.Single(p=>p.Key.StartsWith(prefix,StringComparison.Ordinal));
            var config=new BattleLabPreparationAdapter(new BattleLabContentIndex(package)).Build(BattleLabPresetStore.ToSnapshot(preset.Value));
            var screen=GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
            AddChild(screen);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            screen.StartBattle(package.Content,config,preset.Key);screen.SetLabControlsVisible(true);screen.SetSpeed(1);screen.SetPaused(false);
            var layer=screen.GetNode<RangedAttackLayer>("%RangedAttackLayer");var player=layer.GetNode<VfxPlayer>("Player");
            var seen=new HashSet<string>();bool paused=false;var captured=0;
            for(var frame=0;frame<1200;frame++)
            {
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Require(screen.LastRuntimeFailure.Length==0,screen.LastRuntimeFailure);
                foreach(var v in player.GetChildren().OfType<VfxInstance>())seen.Add(Path.GetFileNameWithoutExtension(v.SceneFilePath));
                var active=player.GetChildren().OfType<VfxInstance>().Any(v=>v.SceneFilePath.Contains(prefix=="HC37"?"duel_mark":prefix=="HC38"?"grit_charge":"mechanical_hook"));
                if(!paused && active)
                {
                    await Click(screen.GetNode<Button>("%PauseButton"));Require(screen.IsPaused,"real pause button");
                    var tick=screen.TickIndex;var ages=player.GetChildren().OfType<VfxInstance>().Select(v=>(v,v.Playback.Age)).ToArray();
                    for(var j=0;j<5;j++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    Require(tick==screen.TickIndex && ages.All(p=>p.v.Playback.Age==p.Age),"pause freezes simulation and effects");
                    await Click(screen.GetNode<Button>("%PauseButton"));paused=true;
                }
                if(capture is not null && frame%4==0)
                {
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    Require(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(capture,$"{captured++:D4}.png"))==Error.Ok,"capture");
                }
                if(screen.TickIndex>=180)break;
            }
            Require(paused,"specific effect reached and paused");
            if(prefix=="HC38")Require(seen.Contains("grit_charge") && seen.Contains("grit_punch"),$"charge and released punch visible tick={screen.TickIndex} seen={string.Join(',',seen)}");
            screen.StopBattle();Require(player.ActiveCount==0 && layer.TechniqueVfxCount==0,"teardown clears tether and duel ownership");
            screen.QueueFree();GD.Print($"DUEL_GRIT_HOOK_VISUAL_OK {prefix} real-pause teardown rendered={string.Join(',',seen)} captures={captured}");GetTree().Quit();
        }
        catch(Exception error){GD.PrintErr("DUEL_GRIT_HOOK_VISUAL_FAILED "+error);GetTree().Quit(1);}
    }
    private async Task Click(Button button)
    {
        var p=button.GetGlobalTransformWithCanvas()* (button.Size/2);
        GetViewport().PushInput(new InputEventMouseMotion{Position=p,GlobalPosition=p},true);
        GetViewport().PushInput(new InputEventMouseButton{Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,ButtonMask=MouseButtonMask.Left,Pressed=true},true);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        GetViewport().PushInput(new InputEventMouseButton{Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,Pressed=false},true);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
