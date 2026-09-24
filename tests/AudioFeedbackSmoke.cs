using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Audio;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Rendered, isolated main-flow input and actual mixed-audio evidence. Master bus
// effects are upstream of the final volume/mute, so capture energy is deliberately
// NOT used to claim that the hardware output is audible while Master is muted.
public partial class AudioFeedbackSmoke : Node
{
    private readonly List<Vector2> _recording = [];
    private AudioEffectCapture? _capture;
    private int _master;
    private int _effectIndex;
    private GameRoot? _game;
    private SubViewport _viewport = null!;
    private int _rate;

    public override void _Process(double delta)
    {
        if (_capture is null) return;
        var available = _capture.GetFramesAvailable();
        if (available > 0) _recording.AddRange(_capture.GetBuffer(available));
    }

    public override async void _Ready()
    {
        var code = 0;
        _master = AudioServer.GetBusIndex("Master");
        var originalMute = AudioServer.IsBusMute(_master);
        var originalVolume = AudioServer.GetBusVolumeDb(_master);
        try
        {
            Require(DisplayServer.GetName() != "headless", "Run with a rendered window and an audio driver.");
            GetWindow().Size = new Vector2I(1600, 900);
            // The authored viewport renders the actual game while only accepting
            // test input. Desktop cursor movement must not contaminate hover QA.
            _viewport = GetNode<SubViewport>("TestViewport");
            _rate = (int)AudioServer.GetMixRate();
            _capture = new AudioEffectCapture { BufferLength = 2 };
            _effectIndex = AudioServer.GetBusEffectCount(_master);
            AudioServer.AddBusEffect(_master, _capture);
            await PoolAndRouting();

            _game = await Boot($"tests/audio-feedback/bootstrap-{Guid.NewGuid():N}");
            await HoverInput(_game);
            await InteriorInput(_game);
            var content = _game.Content!;
            var result = GameProjectCompiler.Compile(_game.ProjectDefinition!, content.Graph);
            var project = result.Project ?? throw new InvalidOperationException("Production project did not compile.");
            await RemoveGame();

            // These are labelled independent route fixtures, not a claimed full run.
            var shopNamespace = PrepareRoute(content, project, TowerNodeType.Shop);
            _game = await Boot(shopNamespace);
            var screens = _game.GetNode<AppScreenHost>(_game.ScreenHostPath);
            var ui = _game.GetNode<FeedbackAudio>("UiAudio");
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
            await Until(() => screens.Shop.IsVisibleInTree(), "continue to prepared shop");
            await Frames(4);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var shopImage = ProjectSettings.GlobalizePath("res://.godot/ui-review/item-audio-shop.png");
            Directory.CreateDirectory(Path.GetDirectoryName(shopImage)!);
            Require(_viewport.GetTexture().GetImage().SavePng(shopImage) == Error.Ok, "save real shop screenshot");
            var item = Descendants<RunOfferChoiceCard>(screens.Shop)
                .First(card => !card.ConfirmButton.Disabled && card.IsVisibleInTree());
            await Wait(.2);
            var selections = ui.PlayedCount;
            await MovePointer(item.DetailsButton.GetGlobalRect().GetCenter());
            await Wait(.15);
            Require(ui.PlayedCount == selections, "hover on a later-created internal button stays silent");
            await Click(item.DetailsButton);
            Require(ui.PlayedCount == selections + 1 && ui.LastPlayedCue == "ui_select" && item.DetailsExpanded,
                "click on a later-created internal button sounds once and opens details");
            await Wait(.3);
            selections = ui.PlayedCount;
            await Click(item.ConfirmButton);
            Require(ui.PlayedCount == selections + 1 && ui.LastPlayedCue == "purchase",
                "real accepted purchase produces only the outcome cue");
            await Wait(.65);
            await RemoveGame();

            var battleNamespace = PrepareRoute(content, project, TowerNodeType.Combat);
            _game = await Boot(battleNamespace);
            screens = _game.GetNode<AppScreenHost>(_game.ScreenHostPath);
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
            await Until(() => screens.Deployment.IsVisibleInTree(), "continue to prepared deployment");
            await Click(screens.Deployment.GetNode<Button>("%StartBattleButton"));
            await Until(() => screens.Battle.HasActiveBattle, "actual battle started");
            var battleAudio = screens.Battle.GetNode<FeedbackAudio>("BattleAudio");
            await Until(() => battleAudio.PlayedCount > 3, "actual battle emits audio facts", 600);
            await Wait(1.2);
            await Click(screens.Battle.GetNode<Button>("%PauseButton"));
            Require(screens.Battle.IsPaused, "pause via shipped button");
            var pausedCount = battleAudio.PlayedCount;
            var pausedTick = screens.Battle.TickIndex;
            await Wait(.4);
            Require(pausedCount == battleAudio.PlayedCount && pausedTick == screens.Battle.TickIndex,
                "paused battle produces no events or delayed audio queue");
            await Click(screens.Battle.GetNode<Button>("%PauseButton"));
            await Until(() => screens.Battle.TickIndex > pausedTick, "resume via shipped button");
            await Wait(1.4);
            Require(battleAudio.ActiveVoiceCount <= battleAudio.VoiceBudget, "busy combat stays within authored pool");
            screens.Battle.StopBattle();
            Require(battleAudio.ActiveVoiceCount == 0, "battle replacement stops every owned voice");
            await Wait(.2);
            _Process(0);
            var peak = _recording.Count == 0 ? 0 : _recording.Max(sample => Math.Max(Math.Abs(sample.X), Math.Abs(sample.Y)));
            Require(_recording.Count > _rate && peak > .001f, "real mixer emitted non-silent PCM, not only event counters");
            var path = ProjectSettings.GlobalizePath("res://.godot/ui-review/audio-feedback-main-flow.wav");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            SaveWave(path);
            GD.Print($"AUDIO_FEEDBACK_OK real-input=menu-hover,dwell,reenter,disabled,menu-focus,internal-hover-silent,internal-focus-silent,internal-mouse-and-keyboard-activation,details,purchase,battle,pause,resume mixer-frames={_recording.Count} peak={peak:0.0000} recording={path} subjective-listening=pending fixtures=isolated");
        }
        catch (Exception exception) { code = 1; GD.PrintErr("AUDIO_FEEDBACK_FAILED " + exception); }
        finally
        {
            await RemoveGame();
            if (_capture is not null) { AudioServer.RemoveBusEffect(_master, _effectIndex); _capture = null; }
            AudioServer.SetBusMute(_master, originalMute);
            AudioServer.SetBusVolumeDb(_master, originalVolume);
        }
        GetTree().Quit(code);
    }

    private async Task HoverInput(GameRoot game)
    {
        var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
        var ui = game.GetNode<FeedbackAudio>("UiAudio");
        var newRun = screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton");
        var disabled = screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton");
        Require(disabled.Disabled, "fresh isolated game has no Continue action");
        await MovePointer(new Vector2(24, 24));
        await Wait(.2);
        var pressed = 0;
        void CountPress() => pressed++;
        newRun.Pressed += CountPress;
        try
        {
            var before = ui.PlayedCount;
            var captureStart = _recording.Count;
            await MovePointer(newRun.GetGlobalRect().GetCenter());
            await Wait(.2);
            _Process(0);
            Require(ui.PlayedCount == before + 1 && ui.LastPlayedCue == "ui_select" && pressed == 0,
                "pointer entry sounds once before any press");
            var hoverSamples = _recording.Skip(captureStart).ToArray();
            Require(hoverSamples.Any(sample => Math.Abs(sample.X) > .001f || Math.Abs(sample.Y) > .001f),
                "hover itself produces non-silent mixer output");
            var capturePath = ProjectSettings.GlobalizePath("res://.godot/ui-review/ui-hover-input.wav");
            Directory.CreateDirectory(Path.GetDirectoryName(capturePath)!);
            SaveWave(capturePath, hoverSamples);

            before = ui.PlayedCount;
            await MovePointer(newRun.GetGlobalRect().GetCenter() + new Vector2(8, 0));
            await Wait(.2);
            Require(ui.PlayedCount == before, $"dwell and motion within a button do not repeat sound: before={before} after={ui.PlayedCount}");
            await MovePointer(disabled.GetGlobalRect().GetCenter());
            await Wait(.2);
            Require(ui.PlayedCount == before, "disabled button is silent on hover");
            await MovePointer(newRun.GetGlobalRect().GetCenter());
            await Wait(.2);
            Require(ui.PlayedCount == before + 1 && pressed == 0, "leaving and reentering sounds again without activation");

            await MovePointer(new Vector2(24, 24));
            newRun.GrabFocus(); // Establish the starting point; Tab below is real input.
            await Wait(.2);
            before = ui.PlayedCount;
            _viewport.PushInput(new InputEventKey { Keycode = Key.Tab, PhysicalKeycode = Key.Tab, Pressed = true }, true);
            _viewport.PushInput(new InputEventKey { Keycode = Key.Tab, PhysicalKeycode = Key.Tab, Pressed = false }, true);
            await Frames(4);
            if (disabled.HasFocus())
            {
                Require(ui.PlayedCount == before, "disabled keyboard focus stays silent too");
                _viewport.PushInput(new InputEventKey { Keycode = Key.Tab, PhysicalKeycode = Key.Tab, Pressed = true }, true);
                _viewport.PushInput(new InputEventKey { Keycode = Key.Tab, PhysicalKeycode = Key.Tab, Pressed = false }, true);
                await Frames(4);
            }
            Require(!newRun.HasFocus() && ui.PlayedCount == before + 1 && ui.LastPlayedCue == "ui_select",
                $"keyboard focus navigation also sounds once: focus={_viewport.GuiGetFocusOwner()?.GetPath()} before={before} after={ui.PlayedCount} last={ui.LastPlayedCue}");
        }
        finally { newRun.Pressed -= CountPress; }
    }

    private async Task InteriorInput(GameRoot game)
    {
        var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
        var ui = game.GetNode<FeedbackAudio>("UiAudio");
        var settings = screens.MainMenu.GetNode<Button>("Center/Panel/Menu/SettingsButton");
        await MovePointer(settings.GetGlobalRect().GetCenter());
        await Wait(.2);
        var before = ui.PlayedCount;
        await Click(settings);
        Require(screens.Settings.IsVisibleInTree() && ui.PlayedCount == before,
            "home menu hover followed by click does not duplicate selection");

        var save = screens.Settings.GetNode<Button>("Center/Panel/Layout/SaveButton");
        await MovePointer(save.GetGlobalRect().GetCenter());
        await Wait(.2);
        Require(ui.PlayedCount == before, "internal settings hover stays silent");
        var captureStart = _recording.Count;
        await Click(save);
        await Wait(.2);
        _Process(0);
        Require(screens.MainMenu.IsVisibleInTree() && ui.PlayedCount == before + 1 && ui.LastPlayedCue == "ui_select",
            "internal mouse activation sounds even when its handler hides the screen");
        var clickSamples = _recording.Skip(captureStart).ToArray();
        Require(clickSamples.Any(sample => Math.Abs(sample.X) > .001f || Math.Abs(sample.Y) > .001f),
            "internal click emits non-silent mixer output");
        SaveWave(ProjectSettings.GlobalizePath("res://.godot/ui-review/ui-internal-click-input.wav"), clickSamples);

        await MovePointer(settings.GetGlobalRect().GetCenter());
        await Wait(.2);
        await Click(settings);
        await MovePointer(new Vector2(24, 24));
        before = ui.PlayedCount;
        // Establish a starting point; Tab and Enter use the actual GUI input path.
        screens.Settings.GetNode<OptionButton>("Center/Panel/Layout/SpeedOption").GrabFocus();
        await Frames(3);
        await PressKey(Key.Tab);
        Require(save.HasFocus() && ui.PlayedCount == before, "internal keyboard focus navigation stays silent");
        await PressKey(Key.Enter);
        Require(screens.MainMenu.IsVisibleInTree() && ui.PlayedCount == before + 1 && ui.LastPlayedCue == "ui_select",
            "internal keyboard activation sounds once and performs the action");
    }

    private async Task PressKey(Key key)
    {
        _viewport.PushInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true }, true);
        await Frames(1);
        _viewport.PushInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false }, true);
        await Frames(3);
    }

    private async Task PoolAndRouting()
    {
        var audio = GD.Load<PackedScene>("res://scenes/audio/FeedbackAudio.tscn").Instantiate<FeedbackAudio>();
        AddChild(audio);
        Require(audio.Sounds.All(sound => sound.Stream?.GetLength() > .03), "authored clips decode with positive length");
        Require(BattleAudioFeedback.EventCue(Fact("attack_windup"), AttackDelivery.Projectile) == "", "bow windup is silent");
        Require(BattleAudioFeedback.EventCue(Fact("attack"), AttackDelivery.Projectile) == "", "attack declaration does not duplicate projectile sound");
        Require(BattleAudioFeedback.EventCue(Fact("projectile_spawn")) == "arrow_release", "arrow leaves on the authoritative spawn");
        Require(BattleAudioFeedback.EventCue(Fact("projectile_move")) == "", "flight samples are silent");
        Require(BattleAudioFeedback.EventCue(Fact("projectile_end")) == "", "miss/cleanup is not an impact");
        Require(BattleAudioFeedback.EventCue(Fact("projectile_impact")) == "arrow_hit", "impact has separate feedback");
        var routedBefore = audio.PlayedCount;
        BattleAudioFeedback.Present(audio, [Fact("attack"), Fact("projectile_spawn"), Fact("ability")],
            _ => AttackDelivery.Melee, (_, _) => true);
        Require(audio.PlayedCount == routedBefore, "authored animation owns action sounds without duplicate event playback");
        BattleAudioFeedback.Present(audio, [Fact("defeated")], _ => AttackDelivery.Melee,
            (id, cue) => id == "target" && cue == "defeated");
        Require(audio.PlayedCount == routedBefore, "death marker ownership belongs to victim, not attacker");
        BattleAudioFeedback.Present(audio, [Fact("projectile_impact")], _ => AttackDelivery.Projectile, (_, _) => true);
        Require(audio.PlayedCount == routedBefore + 1 && audio.LastPlayedCue == "arrow_hit", "animation markers preserve actual impact feedback");
        audio.Clear();
        Require(audio.Play("arrow_release"), "first release plays");
        Require(!audio.Play("arrow_release"), "same instant duplicate is dropped");
        audio.SetPaused(true);
        Require(!audio.Play("explosion"), "pause rejects new cues instead of queuing them");
        audio.SetPaused(false);
        audio.Clear();
        Require(audio.ActiveVoiceCount == 0, "clear stops paused/resumed clips");
        foreach (var sound in audio.Sounds) audio.Play(sound.Cue);
        Require(audio.ActiveVoiceCount <= audio.VoiceBudget, "pool stays bounded under simultaneous effects");
        audio.Clear();
        audio.RequestUi("purchase");
        audio.RequestUi("ui_select");
        await Frames(2);
        Require(audio.LastPlayedCue == "purchase", "semantic command outcome wins independently of signal ordering");
        await Wait(.6);
        audio.Clear();
        audio.QueueFree();
        await Frames(1);
    }

    private string PrepareRoute(ContentRegistry content, CompiledGameProject project, TowerNodeType type)
    {
        var space = $"tests/audio-feedback/{type}-{Guid.NewGuid():N}";
        var save = new SaveService(space);
        save.SaveSettings(new SettingsDto { MasterVolume = 0 });
        var app = new RunApplication(content, save, project);
        Require(AudioServer.IsBusMute(_master), "saved zero master volume actually mutes the bus");
        app.Settings.MasterVolume = .65f;
        app.SaveSettings();
        Require(!AudioServer.IsBusMute(_master), "positive volume restores the bus");
        for (ulong seed = 1; seed <= 64; seed++)
        {
            Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], seed), "prepare route with production unlocked hero");
            if (app.CurrentOptions().Any(option => option.Type == type)) break;
        }
        Require(app.SelectNode(type), "prepare legitimately offered route");
        Require(save.SaveActiveRun(app.ActiveRun!), "save only isolated prepared route");
        return space;
    }

    private async Task<GameRoot> Boot(string space)
    {
        var game = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
        game.SaveNamespace = space;
        _viewport.AddChild(game);
        await Until(() => game.Content is not null, "production bootstrap", 300);
        await Frames(4);
        return game;
    }
    private async Task RemoveGame() { _game?.QueueFree(); _game = null; await Frames(2); }
    private static BattleEvent Fact(string type) => new(0, type, "source", "target", 1, Vector2I.Zero, "");
    private async Task MovePointer(Vector2 point)
    {
        // Use the GUI input pipeline directly; mixing OS warps with synthetic
        // motion can deliver a late native exit between otherwise local moves.
        _viewport.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        await Frames(3);
    }
    private async Task Click(Control control)
    {
        Require(control.IsVisibleInTree(), "input target visible: " + control.GetPath());
        var point = control.GetGlobalRect().GetCenter();
        _viewport.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        _viewport.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }, true);
        _viewport.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }, true);
        await Frames(3);
    }
    private async Task Wait(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Until(Func<bool> predicate, string label, int frames = 240)
    {
        for (var i = 0; i < frames && !predicate(); i++) await Frames(1);
        Require(predicate(), label);
    }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T value) yield return value;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
    private void SaveWave(string path, IReadOnlyList<Vector2>? samples = null)
    {
        samples ??= _recording;
        using var file = new BinaryWriter(File.Create(path));
        var bytes = samples.Count * 4;
        file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); file.Write(bytes + 36);
        file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); file.Write(16);
        file.Write((short)1); file.Write((short)2); file.Write(_rate); file.Write(_rate * 4);
        file.Write((short)4); file.Write((short)16);
        file.Write(System.Text.Encoding.ASCII.GetBytes("data")); file.Write(bytes);
        foreach (var sample in samples)
        {
            file.Write((short)Math.Clamp(sample.X * 32767, -32768, 32767));
            file.Write((short)Math.Clamp(sample.Y * 32767, -32768, 32767));
        }
    }
}
