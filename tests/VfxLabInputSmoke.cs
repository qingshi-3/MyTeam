using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (OS.GetCmdlineUserArgs().Contains("--priest-only"))
            {
                await ReviewPriest(player);
                GetTree().Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--ranged-only"))
            {
                await ReviewRanged(player);
                GetTree().Quit();
                return;
            }
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
    private async Task ReviewRanged(VfxPlayer player)
    {
        var output = ProjectSettings.GlobalizePath("res://.godot/ranged-style-review/wind-preview");
        Directory.CreateDirectory(output);
        await Select("basic_wind");
        await Click("Pause");
        var instance = player.GetChild<VfxInstance>(0);
        var track = instance.GetNode<VfxBasicProjectileTrack>("Track");
        var point = instance.Context.Target;
        var rotation = track.Core.Rotation;
        await Frames(6);
        if (instance.Context.Target != point || track.Core.Rotation != rotation)
            throw new Exception("Wind preview pause lost position/spin.");
        await Click("Step");
        if (instance.Context.Target == point || track.Core.Rotation == rotation)
            throw new Exception("Wind preview step lost movement/spin.");
        await Click("Pause");
        await Click("Replay");
        var images = new List<Image>();
        var times = new List<ulong>();
        for (var i = 0; i < 90; i++)
        {
            await Frames(1);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            images.Add(_input.Viewport.GetTexture().GetImage());
            times.Add(Time.GetTicksMsec());
        }
        await Click("Reduced");
        await Click("Replay");
        instance = player.GetChild<VfxInstance>(0);
        track = instance.GetNode<VfxBasicProjectileTrack>("Track");
        rotation = track.Core.Rotation;
        await Frames(6);
        if (track.Core.Rotation != rotation || track.Trail.GetChildren().OfType<Sprite2D>().Any(s => s.Visible))
            throw new Exception("Reduced motion retained wind spin/trail.");
        await Click("End");
        await Frames(12);
        if (player.ActiveCount != 0) throw new Exception("Explicit end failed to clear ranged preview.");
        for (var i = 0; i < images.Count; i++)
        {
            if (images[i].SavePng(Path.Combine(output, $"{i:D4}.png")) != Error.Ok)
                throw new Exception("Wind preview capture failed.");
            images[i].Dispose();
        }
        File.WriteAllText(Path.Combine(output, "times.csv"), "frame,time_ms\n" +
            string.Join('\n', times.Select((t, i) => $"{i},{t-times[0]}")));
        GD.Print($"RANGED_PREVIEW_INPUT_OK real search/select/replay/pause/step/reduced/end frames={times.Count} span_ms={times[^1]-times[0]}");
    }
    private async Task ReviewPriest(VfxPlayer player)
    {
        await Select("basic_holy_strike");
        await Click("Pause");
        if (player.GetChildren().OfType<VfxInstance>().Any(v => v.SceneFilePath.EndsWith("basic_holy_pillar.tscn")))
            throw new Exception("Pillar appeared before the preview hit.");
        await Click("Impact");
        var pillar = player.GetChildren().OfType<VfxInstance>().Single(v => v.SceneFilePath.EndsWith("basic_holy_pillar.tscn"));
        var age = pillar.Playback.Age;
        await Frames(6);
        if (pillar.Playback.Age != age) throw new Exception("Paused light pillar kept advancing.");
        await Click("Step");
        if (pillar.Playback.Age <= age) throw new Exception("Light pillar single step did not advance.");
        await Click("Reduced");
        var material = (ShaderMaterial)pillar.GetNode<Sprite2D>("Column").Material;
        if ((float)material.GetShaderParameter("motion_amount") != 0)
            throw new Exception("Reduced motion did not reach the pillar material.");
        await Click("End");
        await Click("Pause");
        await Frames(20);
        if (player.ActiveCount != 0) throw new Exception("Light pillar end failed to clear the preview.");
        GD.Print("PRIEST_PREVIEW_INPUT_OK real search/select/pause/impact/step/reduced/end; no early pillar; transient cleanup");
    }
}
