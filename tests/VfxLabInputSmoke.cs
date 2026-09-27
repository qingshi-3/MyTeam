using System;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Vfx;

public partial class VfxLabInputSmoke : Node
{
    private UiInputStage _input = null!;
    private VfxPreviewController _preview = null!;
    private async Task Frames(int count = 3)
    { for (int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private async Task Click(Vector2 point)
    {
        _input.Push(new InputEventMouseMotion { Position=point,GlobalPosition=point });
        _input.Push(new InputEventMouseButton { Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=true });
        _input.Push(new InputEventMouseButton { Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=false });
        await Frames();
    }
    private Task Click(string name) => Click(_preview.GetNode<Control>("%"+name).GetGlobalRect().GetCenter());
    private async Task Select(string id)
    {
        await Click("Search");
        _input.Push(new InputEventKey { Keycode=Key.A,CtrlPressed=true,Pressed=true });
        _input.Push(new InputEventKey { Keycode=Key.A,CtrlPressed=true,Pressed=false });
        _input.Push(new InputEventKey { Keycode=Key.Backspace,Pressed=true });
        _input.Push(new InputEventKey { Keycode=Key.Backspace,Pressed=false });
        foreach(char c in id)
        {
            _input.Push(new InputEventKey { Unicode=c,Pressed=true });
            _input.Push(new InputEventKey { Unicode=c,Pressed=false });
        }
        await Frames();
        var list=_preview.GetNode<ItemList>("%Effects");
        if(list.ItemCount!=1)throw new Exception("Search input failed: "+id);
        await Click(list.GlobalPosition+list.GetItemRect(0).GetCenter());
    }
    public override async void _Ready()
    {
        try
        {
            Engine.MaxFps=30;
            _input=new(this,true);
            _preview=GD.Load<PackedScene>("res://scenes/app/VfxPreview.tscn").Instantiate<VfxPreviewController>();
            _input.AddChild(_preview);
            await Frames(6);
            var player=_preview.GetNode<VfxPlayer>("%Player");
            await Select("ground_fissure");
            if(Math.Abs(_preview.GetNode<HSlider>("%Radius").Value-.28)>.001)throw new Exception("Fissure radius not restored through input.");
            await Click("Reset");
            if(Math.Abs(_preview.GetNode<HSlider>("%Radius").Value-.28)>.001)throw new Exception("Reset lost selected effect defaults.");
            await Click("EventMode");
            if(!_preview.GetNode<HSlider>("%Progress").Editable)throw new Exception("External action phase control disabled.");
            await Frames(20);
            var progress=_preview.GetNode<HSlider>("%Progress").GetGlobalRect();
            await Click(new Vector2(progress.End.X-3,progress.GetCenter().Y));
            var released=player.GetChild<VfxInstance>(0);
            if(released.Context.TravelProgress<=.5f || released.Playback.Age>.3f)
                throw new Exception("Manual release retained the preparation clock.");
            await Click("EventMode");
            await Select("projectile");
            await Click("Pause");
            var paused=player.GetChild<VfxInstance>(0).Context.Target;
            await Frames(6);
            if(player.GetChild<VfxInstance>(0).Context.Target!=paused)throw new Exception("Pause input did not freeze projectile.");
            await Click("Step");
            if(player.GetChild<VfxInstance>(0).Context.Target==paused)throw new Exception("Step input did not advance projectile.");
            await Click("Pause");
            await Select("rock_raise");
            if(_preview.GetNode<AnimatedSprite2D>("%TargetUnit").Visible || !_preview.GetNode<AnimatedSprite2D>("%SourceUnit").Visible)
                throw new Exception("Wall preview places a target actor inside the wall.");
            await Select("shield");
            await Click("Reduced");await Click("Light");
            await Frames(10);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            _input.Viewport.GetTexture().GetImage().SavePng(".godot/library-polish/lab-input.png");
            GD.Print("VFX_LAB_INPUT_OK search/select/reset/phase/pause/step/light/reduced");
            GetTree().Quit();
        }
        catch(Exception error){GD.PrintErr(error);GetTree().Quit(1);}
    }
}
