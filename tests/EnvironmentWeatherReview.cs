using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Presentation;
using TowerAutobattler.UI;

// Real GUI events are confined to the established 960x540 SubViewport.
public partial class EnvironmentWeatherReview : Node
{
    private UiInputStage _stage = null!;
    private string _output = "";

    public override async void _Ready()
    {
        try
        {
            Engine.MaxFps = 30;
            _output = ProjectSettings.GlobalizePath("res://outputs/environment-weather-20261001");
            Directory.CreateDirectory(_output);
            _stage = new UiInputStage(this, true);
            _stage.Resize(new(960, 540));
            await CheckMenu();
            await CheckPrefabs();
            var preview = GD.Load<PackedScene>("res://scenes/app/EnvironmentPreview.tscn")
                .Instantiate<EnvironmentPreviewController>();
            _stage.AddChild(preview);
            await Frame();
            var environment = preview.Environment;
            var surface = (ShaderMaterial)environment.GetNode<TextureRect>("Ground").Material;
            var weather = (ShaderMaterial)environment.GetNode<ColorRect>("Weather").Material;
            Require(environment.GetGlobalRect() == _stage.Viewport.GetVisibleRect(), "full viewport background");
            Require(!environment.GetNode<Control>("Weather").Visible, "clear default preserves sunny backdrop");
            await Capture("clear.png");
            var styles = new[] { "Rain", "Snow", "Storm" };
            foreach (var style in styles)
            {
                await Click(preview.GetNode<Button>("%" + style));
                var clock = environment.ElapsedSeconds;
                Require(preview.GetNode<Button>("%" + style).ButtonPressed, style + " selected by real input");
                var frames = new List<Image>();
                var times = new List<ulong>();
                for (var i = 0; i < 270; i++)
                {
                    await Frame();
                    frames.Add(_stage.Viewport.GetTexture().GetImage());
                    times.Add(Time.GetTicksMsec());
                    Require(surface.GetShaderParameter("environment_elapsed").AsSingle() ==
                        weather.GetShaderParameter("environment_elapsed").AsSingle(), "weather and surface share clock");
                }
                Require(environment.ElapsedSeconds > clock + 7.0f, "normal-speed animation advances");
                await Click(preview.GetNode<Button>("%Pause"));
                clock = environment.ElapsedSeconds;
                for (var i = 0; i < 8; i++) await Frame();
                Require(environment.ElapsedSeconds == clock, "real pause freezes " + style);
                await Capture(style.ToLowerInvariant() + ".png");
                await Click(preview.GetNode<CheckButton>("%ReducedMotion"));
                Require(weather.GetShaderParameter("reduced_motion").AsBool() &&
                    surface.GetShaderParameter("reduced_motion").AsBool(), "reduced motion reaches flash and precipitation");
                await Capture(style.ToLowerInvariant() + "-reduced.png");
                await Click(preview.GetNode<Button>("%Pause"));
                for (var i = 0; i < 8; i++) await Frame();
                Require(environment.ElapsedSeconds == clock, "reduced motion freezes presentation time");
                await Click(preview.GetNode<CheckButton>("%ReducedMotion"));
                await Frame();
                Require(environment.ElapsedSeconds > clock, "real resume restores motion");
                // Saving is outside the recorded interval; it cannot slow the movie.
                environment.SetProcess(false);
                var directory = Path.Combine(_output, style.ToLowerInvariant());
                Directory.CreateDirectory(Path.Combine(directory, "after-frames"));
                for (var i = 0; i < frames.Count; i++)
                {
                    frames[i].SavePng(Path.Combine(directory, $"after-frames/{i:0000}.png"));
                    frames[i].Dispose();
                }
                File.WriteAllText(Path.Combine(directory, "after-frame-times.json"),
                    System.Text.Json.JsonSerializer.Serialize(times));
                GD.Print($"WEATHER_CAPTURE {style} frames={frames.Count} wall_ms={times[^1] - times[0]} clock={clock}");
                environment.SetProcess(true);
            }
            await Click(preview.GetNode<Button>("%Clear"));
            Require(!environment.GetNode<Control>("Weather").Visible &&
                surface.GetShaderParameter("weather_overcast").AsSingle() == 0,
                "switching back clears storm and restores original light");
            preview.QueueFree();
            await Frame(); await Frame();
            GD.Print("ENVIRONMENT_WEATHER_OK menu request; three authored styles; real selection/pause/resume/reduced motion; private materials; no domain or desktop input");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError("ENVIRONMENT_WEATHER_FAILED " + error);
            GetTree().Quit(1);
        }
    }

    private async Task CheckMenu()
    {
        var menu = GD.Load<PackedScene>("res://scenes/ui/MainMenuScreen.tscn").Instantiate<MainMenuScreenController>();
        menu.Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres");
        var requested = false;
        menu.EnvironmentPreviewRequested += () => requested = true;
        _stage.AddChild(menu);
        menu.Bind(false);
        menu.BindJourney(true);
        await Frame();
        await Click(menu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
        await Capture("menu-entry.png");
        var button = menu.GetNode<Button>("Center/Panel/Menu/EnvironmentPreviewButton");
        await Click(button);
        Require(requested, "authored main-menu request uses real GUI input");
        menu.QueueFree();
        await Frame();
    }

    private async Task CheckPrefabs()
    {
        foreach (var name in new[] { "RainyCourtyard", "SnowyCourtyard", "StormCourtyard" })
        {
            var scene = GD.Load<PackedScene>("res://scenes/environments/" + name + ".tscn");
            var first = scene.Instantiate<BattleEnvironment>();
            var second = scene.Instantiate<BattleEnvironment>();
            first.AutoAnimate = false;
            second.AutoAnimate = false;
            _stage.AddChild(first); _stage.AddChild(second);
            await Frame();
            var one = (ShaderMaterial)first.GetNode<ColorRect>("Weather").Material;
            var two = (ShaderMaterial)second.GetNode<ColorRect>("Weather").Material;
            first.Advance(1.25f);
            Require(one != two && first.ElapsedSeconds == 1.25f && second.ElapsedSeconds == 0,
                "independent material clocks: " + name);
            var sharedStyle = first.WeatherStyle;
            first.SetWeatherStyle(null);
            Require(second.WeatherStyle == sharedStyle && second.GetNode<Control>("Weather").Visible,
                "switching cannot mutate shared recipe: " + name);
            first.QueueFree(); second.QueueFree();
            await Frame();
        }
    }

    private async Task Click(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        Require(_stage.Viewport.GetVisibleRect().HasPoint(point), "visible input target: " + control.Name);
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frame();
        foreach (var pressed in new[] { true, false })
        {
            _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, ButtonMask = pressed ? MouseButtonMask.Left : 0, Pressed = pressed });
            await Frame();
        }
        // Keep the pointer clear of scenery without touching the OS pointer.
        _stage.Push(new InputEventMouseMotion { Position = new(940, 100), GlobalPosition = new(940, 100) });
    }
    private Task Capture(string path)
    {
        using var image = _stage.Viewport.GetTexture().GetImage();
        Require(image.SavePng(Path.Combine(_output, path)) == Error.Ok, "capture " + path);
        return Task.CompletedTask;
    }
    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw(false);
    }
    private static void Require(bool value, string label)
    { if (!value) throw new InvalidOperationException(label); }
}
