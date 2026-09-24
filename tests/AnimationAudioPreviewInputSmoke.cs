using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Audio;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Components;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.UI;

// Isolated published lab session, real GUI input, animation-frame observations
// and mixer capture. No player save or authored resource is written.
public partial class AnimationAudioPreviewInputSmoke : Node
{
    private SubViewport _viewport = null!;
    private AudioEffectCapture? _capture;
    private readonly List<Vector2> _recording = [];
    private string _step = "boot";

    public override void _Process(double delta)
    {
        if (_capture is null) return;
        var available = _capture.GetFramesAvailable();
        if (available > 0) _recording.AddRange(_capture.GetBuffer(available));
    }

    public override async void _Ready()
    {
        var code = 0;
        var bus = AudioServer.GetBusIndex("Master");
        var effect = AudioServer.GetBusEffectCount(bus);
        BattleLabScreenController? lab = null;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered window and audio driver required");
            GetWindow().Size = new Vector2I(1600, 900);
            _viewport = GetNode<SubViewport>("TestViewport");
            _capture = new AudioEffectCapture { BufferLength = 2 };
            AudioServer.AddBusEffect(bus, _capture);
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var session = new BattleLabSession(index, 7, 81, BattleLabPlacementMode.FreeExperiment);
            var hero = index.PlayerUnits.Single(unit => unit.StableId == "hero_hc03_iron_guard");
            var added = session.AddAndPlace(hero.StableId, BattleLabSide.Player, new Vector2I(1, 2));
            Require(added.Succeeded, "isolated lab fixture");
            var digest = session.Freeze().CanonicalDigest;
            lab = GD.Load<PackedScene>("res://scenes/ui/BattleLabScreen.tscn").Instantiate<BattleLabScreenController>();
            _viewport.AddChild(lab); lab.Bind(index, session);
            await Frames(5);
            _step = "actual lab selection and entry";
            await Click(Descendants<BattleLabBoardCell>(lab).Single(cell => cell.InstanceId == added.InstanceId));
            await Click(lab.GetNode<Button>("%AudioPreviewButton"));
            var popup = lab.GetNode<UnitAnimationAudioPreview>("%AudioPreviewPopup");
            Require(popup.IsOpen, "lab button opens real preview");
            var unit = popup.GetNode<Node2D>("%PreviewActorHost").GetChild<UnitContentRoot>(0);
            var animation = unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
            var sprite = animation.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
            var original = animation.FrameSounds.First(mark => mark.Animation == "attack").Frame;
            var nextFrame = original == 2 ? 3 : 2;
            _step = "edit frame through SpinBox keyboard";
            var spin = popup.GetNode<SpinBox>("%MarkerFrame");
            await Click(spin.GetLineEdit());
            await KeyInput(Key.A, true);
            await KeyInput((Key)('0' + nextFrame), unicode: (uint)('0' + nextFrame));
            await KeyInput(Key.Enter);
            Require(animation.FrameSounds.First(mark => mark.Animation == "attack").Frame == nextFrame,
                "native spin edit changes local mark");
            Require(!sprite.IsPlaying(), "marker editing pauses current playback before mutating schedule");
            var frameWhilePaused = sprite.Frame;
            await Wait(.15);
            Require(sprite.Frame == frameWhilePaused, "edit remains paused");

            _step = "replay observes actual frame sound event";
            var emitted = new List<int>();
            void Observe(FeedbackSound _) { if (animation.ActiveLogicalCue == "attack") emitted.Add(sprite.Frame + 1); }
            animation.AnimationSoundRequested += Observe;
            var beforeAudio = popup.SoundPlayCount;
            _recording.Clear(); _capture.ClearBuffer();
            await Click(popup.GetNode<Button>("%PreviewPlay"));
            await Wait(1);
            Require(emitted.Count == 1 && emitted[0] == nextFrame,
                $"sound emitted exactly at edited frame {nextFrame}; observed={string.Join(',', emitted)}");
            Require(popup.SoundPlayCount == beforeAudio + 1, "shared audio player accepts animation sound once");
            animation.AnimationSoundRequested -= Observe;
            _Process(0);
            Require(_recording.Any(sample => Math.Abs(sample.X) > .001f || Math.Abs(sample.Y) > .001f),
                "real mix contains non-silent PCM");
            SaveWave();

            _step = "silent frame stepping and resume";
            beforeAudio = popup.SoundPlayCount;
            await Click(popup.GetNode<Button>("%PreviewNextFrame"));
            Require(!sprite.IsPlaying(), "frame step pauses sprite");
            var inspected = sprite.Frame;
            await Click(popup.GetNode<Button>("%PreviewNextFrame"));
            Require(sprite.Frame == Math.Min(inspected + 1, sprite.SpriteFrames.GetFrameCount(sprite.Animation) - 1),
                "next frame follows chosen attack, not idle playback");
            await Click(popup.GetNode<Button>("%PreviewPreviousFrame"));
            Require(sprite.Frame == inspected && popup.SoundPlayCount == beforeAudio, "inspection is silent in either direction");
            await Capture("animation-audio-preview");
            await Click(popup.GetNode<Button>("%PreviewPause"));
            await Wait(.85);
            Require(popup.SoundPlayCount == beforeAudio + 1, "continue restarts action and edited sound exactly once");

            _step = "close and reopen restore";
            await Click(popup.GetNode<Button>("%PreviewClose"));
            Require(!popup.IsOpen && popup.GetNode<Node2D>("%PreviewActorHost").GetChildCount() == 0,
                "closing releases preview actor");
            Require(popup.GetNode<FeedbackAudio>("%PreviewAudio").ActiveVoiceCount == 0, "closing stops sound");
            await Click(lab.GetNode<Button>("%AudioPreviewButton"));
            unit = popup.GetNode<Node2D>("%PreviewActorHost").GetChild<UnitContentRoot>(0);
            animation = unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
            Require(animation.FrameSounds.First(mark => mark.Animation == "attack").Frame == original,
                "reopen restores original resource mark");
            var detached = hero.Entry.Scene.Instantiate<UnitContentRoot>();
            Require(detached.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent").FrameSounds
                .First(mark => mark.Animation == "attack").Frame == original, "shared scene resource stayed unchanged");
            detached.Free();
            Require(session.Freeze().CanonicalDigest == digest, "preview does not mutate lab configuration");

            _step = "wide animation geometry fixture";
            // This archived content scene is a geometry fixture, not an assertion
            // that it is currently published in the player roster.
            popup.ShowUnit(GD.Load<PackedScene>("res://content/heroes/hero_hc36_cyclone_weaver.tscn"), "旋风动作 · 范围检查");
            await Frames(3);
            unit = popup.GetNode<Node2D>("%PreviewActorHost").GetChild<UnitContentRoot>(0);
            sprite = unit.GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D");
            await Click(popup.GetNode<Button>("%PreviewPreviousFrame"));
            while (sprite.Frame > 0) await Click(popup.GetNode<Button>("%PreviewPreviousFrame"));
            var count = sprite.SpriteFrames.GetFrameCount(sprite.Animation);
            var stage = popup.GetNode<Control>("%PreviewStage").GetGlobalRect();
            for (var frame = 0; frame < count; frame++)
            {
                var texture = sprite.SpriteFrames.GetFrameTexture(sprite.Animation, sprite.Frame);
                using var image = texture.GetImage();
                var used = image.GetUsedRect();
                var local = new Rect2((Vector2)used.Position - texture.GetSize() * .5f, used.Size);
                var transform = sprite.GetGlobalTransform();
                Require(stage.HasPoint(transform * local.Position) && stage.HasPoint(transform * local.End),
                    $"wide animation frame {frame + 1} is inside clipping stage");
                if (frame == count / 2) await Capture("animation-audio-preview-wide");
                await Click(popup.GetNode<Button>("%PreviewNextFrame"));
            }
            await KeyInput(Key.Escape);
            Require(!popup.IsOpen, "Esc closes preview");
            GD.Print("ANIMATION_AUDIO_PREVIEW_INPUT_OK lab-entry,spin-edit,frame-event-match,mixed-pcm,silent-steps,restart,close-reopen,resource-isolation,all-wide-frames-visible subjective-listening=pending");
        }
        catch (Exception exception)
        {
            code = 1; GD.PrintErr($"ANIMATION_AUDIO_PREVIEW_INPUT_FAILED step={_step}: {exception}");
            if (_viewport is not null) await Capture("animation-audio-preview-failed");
        }
        finally
        {
            lab?.QueueFree(); await Frames(2);
            if (_capture is not null) { AudioServer.RemoveBusEffect(bus, effect); _capture = null; }
        }
        GetTree().Quit(code);
    }

    private async Task Click(Control control)
    {
        Require(control.IsVisibleInTree(), "visible input target: " + control.Name);
        var point = control.GetGlobalRect().GetCenter();
        _viewport.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        _viewport.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }, true);
        _viewport.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }, true);
        await Frames(3);
    }
    private async Task KeyInput(Key key, bool ctrl = false, uint unicode = 0)
    {
        _viewport.PushInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, CtrlPressed = ctrl, Unicode = unicode, Pressed = true }, true);
        _viewport.PushInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, CtrlPressed = ctrl, Unicode = unicode, Pressed = false }, true);
        await Frames(2);
    }
    private async Task Capture(string name)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var directory = ProjectSettings.GlobalizePath("res://.godot/ui-review"); Directory.CreateDirectory(directory);
        Require(_viewport.GetTexture().GetImage().SavePng(Path.Combine(directory, name + ".png")) == Error.Ok, "screenshot saved");
    }
    private void SaveWave()
    {
        var directory = ProjectSettings.GlobalizePath("res://.godot/ui-review"); Directory.CreateDirectory(directory);
        using var file = new BinaryWriter(File.Create(Path.Combine(directory, "animation-audio-preview.wav")));
        var rate = (int)AudioServer.GetMixRate(); var bytes = _recording.Count * 4;
        file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); file.Write(bytes + 36);
        file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); file.Write(16);
        file.Write((short)1); file.Write((short)2); file.Write(rate); file.Write(rate * 4);
        file.Write((short)4); file.Write((short)16);
        file.Write(System.Text.Encoding.ASCII.GetBytes("data")); file.Write(bytes);
        foreach (var sample in _recording)
        { file.Write((short)Math.Clamp(sample.X * 32767, -32768, 32767)); file.Write((short)Math.Clamp(sample.Y * 32767, -32768, 32767)); }
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Wait(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        { if (child is T item) yield return item; foreach (var nested in Descendants<T>(child)) yield return nested; }
    }
}
