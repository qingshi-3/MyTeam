using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Presentation;

public partial class AbilityPrototypeInputSmoke : Node
{
    private UiInputStage _stage=null!;
    private const string Output="res://outputs/ability-prototype-20261004";
    public override async void _Ready()
    {
        try
        {
            _stage=new UiInputStage(this,isolated:true);
            _stage.Viewport.GuiEmbedSubwindows=true;
            var root=GD.Load<PackedScene>("res://scenes/app/ContentRebuildRoot.tscn").Instantiate<ContentRebuildRoot>();
            _stage.AddChild(root);
            await Until(()=>root.ContentReady,"content ready");
            await Click(root.GetNode<Button>("%Prototype"));
            var panel=root.GetNode<Control>("PrototypePanel");
            Require(panel.IsVisibleInTree(),"prototype menu entered");
            for(var team=0;team<2;team++)for(var slot=0;slot<6;slot++)
                Require(_stage.Viewport.GetVisibleRect().Encloses(panel.GetNode<Control>($"%Unit{team}_{slot}").GetGlobalRect()),"visible roster choice");
            Require(_stage.Viewport.GetVisibleRect().Encloses(panel.GetNode<Control>("Panel").GetGlobalRect()),"panel fits viewport");
            await Capture("configuration");
            var preset=panel.GetNode<OptionButton>("%Preset0");
            await SelectThroughKeys(preset,1);
            Require(preset.Selected==1,"keyboard changes actual preset to forest transform");
            var pick=panel.GetNode<OptionButton>("%Unit0_0");
            await SelectThroughKeys(pick,0);
            Require(pick.GetSelectedId()==0,"unit selector can leave a slot blank");
            await SelectThroughKeys(preset,0);
            Require(preset.Selected==0 && pick.GetSelectedId()==2,"preset restores formation");
            await Click(panel.GetNode<Button>("%PrototypeStart"));
            var battle=root.GetNode<BattleScreenController>("Battle");
            await Until(()=>battle.TickIndex>=5,"real battle advances");
            Directory.CreateDirectory(ProjectSettings.GlobalizePath(Output+"/frames"));
            for(var frame=0;frame<100;frame++)
            {
                await ToSignal(GetTree().CreateTimer(.1),SceneTreeTimer.SignalName.Timeout);
                await Capture($"frames/{frame:000}");
                Require(battle.LastRuntimeFailure.Length==0,"no production battle error");
            }
            Require(battle.TickIndex>=90,"normal-speed combat progresses through lightning");
            await Click(battle.GetNode<Button>("%PauseButton"));
            Require(battle.IsPaused,"pause through real button");
            var tick=battle.TickIndex;
            await ToSignal(GetTree().CreateTimer(.2),SceneTreeTimer.SignalName.Timeout);
            Require(battle.TickIndex==tick,"paused state holds");
            await Click(battle.GetNode<Button>("%ResetBattleButton"));
            Require(battle.TickIndex<tick && !battle.IsPaused,"reset rebuilds same experiment");
            await Click(battle.GetNode<Button>("%ReturnConfigurationButton"));
            Require(panel.IsVisibleInTree() && !battle.HasActiveBattle,"return stops battle and retains roster");
            await Click(panel.GetNode<Button>("%PrototypeBack"));
            Require(root.GetNode<Control>("Menu").IsVisibleInTree(),"menu restored");
            root.QueueFree();await Draw();
            GD.Print("ABILITY_PROTOTYPE_INPUT_OK 960x540 isolated preset-keyboard unit-selection start normal-speed pause reset return");
            GetTree().Quit();
        }
        catch(Exception error) {GD.PrintErr("ABILITY_PROTOTYPE_INPUT_FAILED "+error);GetTree().Quit(1);}
    }
    private async Task SelectThroughKeys(OptionButton pick,int index)
    {
        pick.GrabFocus(); await Draw();
        var current=pick.Selected;
        await PressKey(Key.Space);
        for(var i=current;i>index;i--)await PressKey(Key.Up);
        for(var i=current;i<index;i++)await PressKey(Key.Down);
        await PressKey(Key.Enter);
    }
    private async Task PressKey(Key key)
    {
        _stage.Push(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=true});
        _stage.Push(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=false});
        for(var i=0;i<4;i++)await Draw();
    }
    private async Task Click(Button button)
    {
        await Draw();var point=button.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion{Position=point,GlobalPosition=point});
        _stage.Push(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,ButtonMask=MouseButtonMask.Left,Pressed=true});await Draw();
        _stage.Push(new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=false});await Draw();
    }
    private async Task Draw(){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);}
    private async Task Until(Func<bool> condition,string label)
    {
        var end=Time.GetTicksMsec()+20000;
        while(!condition()){if(Time.GetTicksMsec()>end)throw new InvalidOperationException("Timeout: "+label);await Draw();}
    }
    private async Task Capture(string name)
    {
        await Draw();Directory.CreateDirectory(ProjectSettings.GlobalizePath(Output));
        Require(_stage.Viewport.GetTexture().GetImage().SavePng(Output+"/"+name+".png")==Error.Ok,"capture");
    }
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
