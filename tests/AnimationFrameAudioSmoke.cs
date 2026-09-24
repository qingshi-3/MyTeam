using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TowerAutobattler.Audio;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;

// Exercise the public animation commands and real AnimatedSprite2D progression.
// No mixer, simulation damage, save data or private timeline inspection is involved.
public partial class AnimationFrameAudioSmoke : Node2D
{
    private sealed record Heard(string Cue, int Frame, ulong At);
    private sealed record Rig(UnitAnimationComponent Animation, AnimatedSprite2D Sprite, List<Heard> Heard);

    public override async void _Ready()
    {
        try
        {
            ValidateAuthoring();
            ValidateProductionScenes();
            var rig = CreateRig();
            IdleRefresh(rig);
            await NaturalPlayback(rig);
            TimedPlayback(rig);
            QueuedPlayback(rig);
            await PausedPlayback(rig);
            InterruptedPlayback(rig);
            await AcceleratedPlayback(rig);
            await LoopPlayback(rig);
            rig.Animation.QueueFree();
            await Frames(2);
            await SingleFrameLoop();
            GD.Print("PASS AnimationFrameAudioSmoke: real frames, unequal duration, replay, queue, timed jumps, pause, speed, cancellation, loops and validation");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError($"FAIL AnimationFrameAudioSmoke: {error}");
            GetTree().Quit(1);
        }
    }

    private Rig CreateRig(bool singleIdle = false)
    {
        var animation = GD.Load<PackedScene>("res://scenes/components/UnitAnimationComponent.tscn")
            .Instantiate<UnitAnimationComponent>();
        animation.Frames = MakeFrames(singleIdle);
        animation.AttackActionWindowSeconds = 1.2f;
        animation.SkillActionWindowSeconds = 1.2f;
        animation.DefeatActionWindowSeconds = 1.2f;
        animation.FrameSounds =
        [
            Marker("attack", 1, "attack.start"),
            Marker("attack", 3, "attack.mid"),
            Marker("attack", 4, "attack.end"),
            Marker("skill_cast", 1, "skill.start"),
            Marker("skill_cast", 3, "skill.mid"),
            Marker("idle", 1, "idle.start"),
            Marker("defeated", 1, "death.start")
        ];
        animation.Position = new Vector2(220, 160);
        AddChild(animation);
        var sprite = animation.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        var heard = new List<Heard>();
        animation.AnimationSoundRequested += sound => heard.Add(new Heard(sound.Cue, sprite.Frame + 1, Time.GetTicksMsec()));
        return new Rig(animation, sprite, heard);
    }

    private static SpriteFrames MakeFrames(bool singleIdle = false)
    {
        var frames = new SpriteFrames();
        frames.RemoveAnimation("default");
        foreach (var cue in new[] { "idle", "move", "attack", "cast", "death" })
        {
            frames.AddAnimation(cue);
            frames.SetAnimationSpeed(cue, 6);
            frames.SetAnimationLoopMode(cue, cue is "idle" or "move" ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
            var count = singleIdle && cue == "idle" ? 1 : 4;
            for (var index = 0; index < count; index++)
            {
                var texture = new GradientTexture2D
                {
                    Width = 90,
                    Height = 90,
                    Gradient = new Gradient
                    {
                        Colors = [Color.FromHsv(index / 4f, .7f, 1), Color.FromHsv(index / 4f, .7f, .45f)]
                    }
                };
                // Unequal durations: frame starts at 0, 1/6, 4/6, 5/6 of an action.
                frames.AddFrame(cue, texture, index == 1 ? 3 : 1);
            }
        }
        return frames;
    }

    private static UnitAnimationSound Marker(string cue, int frame, string sound) => new()
    {
        Animation = cue,
        Frame = frame,
        Sound = new FeedbackSound { Cue = sound, Stream = new AudioStreamWav { Data = [0, 0, 0, 0] } }
    };

    private static void ValidateAuthoring()
    {
        var animation = new UnitAnimationComponent { Frames = MakeFrames() };
        try
        {
            animation.FrameSounds = [Marker("skill_cast", 4, "valid")];
            Require(!animation.ValidateFrameSounds().HasCoreErrors && animation.HasFrameSounds("skill_cast"),
                "pre-ready validation accepts logical skill_cast resolved to authored cast frames");
            animation.FrameSounds = [Marker("attack", 1, "")];
            Require(animation.ValidateFrameSounds().HasCoreErrors && !animation.HasFrameSounds("attack"),
                "empty sound cue is invalid and cannot suppress fallback audio");
            animation.FrameSounds =
            [
                Marker("typo", 1, "invalid"), Marker("attack", 0, "zero"),
                Marker("attack", 5, "tooFar"), Marker("attack", 1, ""), new UnitAnimationSound { Animation = "attack", Frame = 1 },
                new UnitAnimationSound { Animation = "attack", Frame = 1, Sound = new FeedbackSound() }
            ];
            Require(animation.ValidateFrameSounds().CoreErrors.Count >= 6 && !animation.HasFrameSounds("attack"),
                "unknown animation, zero, out-of-range, empty cue, null resource and missing stream rejected without clamping");
            animation.Frames = new SpriteFrames();
            animation.Frames.RemoveAnimation("default");
            Require(animation.ValidateFrameSounds().HasCoreErrors, "missing animation collection rejected before ready");
        }
        finally { animation.Free(); }
    }

    private static void ValidateProductionScenes()
    {
        var checkedUnits = 0;
        var disabledUnits = 0;
        foreach (var directory in new[] { "res://content/heroes", "res://content/enemies", "res://content/soldiers" })
        {
            var paths = DirAccess.GetFilesAt(directory).Where(file => file.EndsWith(".tscn", StringComparison.Ordinal)).ToArray();
            if (paths.Length == 0) throw new InvalidOperationException($"No production scenes found in {directory}");
            foreach (var file in paths)
            {
                var path = $"{directory}/{file}";
                var unit = GD.Load<PackedScene>(path).Instantiate<UnitContentRoot>();
                try
                {
                    var animation = unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
                    var report = animation.ValidateFrameSounds();
                    if (report.HasCoreErrors)
                        throw new InvalidOperationException($"{path}: {string.Join("; ", report.CoreErrors)}");
                    if (unit.Behavior?.DisableBasicAttacks == true)
                    {
                        disabledUnits++;
                        continue;
                    }
                    if (!animation.HasFrameSounds("attack"))
                        throw new InvalidOperationException($"{path}: attacking production unit has no valid authored attack-frame sound");
                    checkedUnits++;
                }
                finally { unit.Free(); }
            }
        }
        Require(checkedUnits > 0, $"validated {checkedUnits} production hero/enemy/soldier attack sound configurations; {disabledUnits} disabled attackers excluded");
    }

    private static void IdleRefresh(Rig rig)
    {
        rig.Animation.ResetPresentation();
        for (var index = 0; index < 10; index++) rig.Animation.PlayCue("idle");
        Require(rig.Heard.Count(heard => heard.Cue == "idle.start") == 1,
            "idle refresh leaves its current playback and first-frame sound intact");
    }

    private async Task NaturalPlayback(Rig rig)
    {
        rig.Heard.Clear();
        rig.Animation.PlayCue("attack");
        await Until(() => rig.Animation.ActiveLogicalCue == "idle", 2.5);
        var sounds = rig.Heard.Where(heard => heard.Cue.StartsWith("attack.")).ToArray();
        Require(sounds.Select(heard => heard.Cue).SequenceEqual(new[] { "attack.start", "attack.mid", "attack.end" }),
            "real AnimatedSprite2D plays every attack mark exactly once");
        Require(sounds.Select(heard => heard.Frame).SequenceEqual(new[] { 1, 3, 4 }),
            "ordinary playback emits at displayed configured frames");
        Require(sounds[1].At - sounds[0].At >= 600,
            "long second frame delays frame-three sound; timeline is not equal-frame interpolation");
    }

    private static void TimedPlayback(Rig rig)
    {
        rig.Heard.Clear();
        rig.Animation.BeginTimedAttack(new BattleAttackTiming(1.2f, .8f));
        rig.Animation.StepTimedAttack(.3f);
        Require(rig.Sprite.Frame == 1 && AttackCount(rig) == 1, "timed sampling respects long authored second frame");
        rig.Animation.StepTimedAttack(.6f);
        Require(rig.Sprite.Frame == 2 && AttackCount(rig) == 2, "jump reaches third frame and emits middle mark");
        rig.Animation.StepTimedAttack(.17f);
        rig.Animation.StepTimedAttack(.01f);
        Require(rig.Sprite.Frame == 3 && AttackCount(rig) == 3, "last-frame mark emits once while samples stay there");
        rig.Animation.BeginTimedAttack(new BattleAttackTiming(1.2f, .8f));
        Require(rig.Sprite.Frame == 0 && AttackCount(rig) == 4, "same animation restarts at first frame and emits anew");
        rig.Animation.CompleteTimedWindup(.95f);
        Require(rig.Sprite.Frame == 3 && AttackCount(rig) == 6, "windup completion jump preserves middle and last marks");
        rig.Animation.CompleteTimedWindup(.95f);
        Require(AttackCount(rig) == 6, "repeated authoritative windup sample cannot duplicate marks");
        rig.Animation.CancelTimedAttack();
    }

    private static void QueuedPlayback(Rig rig)
    {
        rig.Heard.Clear();
        rig.Animation.UseNeutralSkillFallback = true;
        rig.Animation.BeginTimedAttack(new BattleAttackTiming(1.2f, .8f));
        rig.Animation.PlayCue("skill_cast");
        Require(rig.Animation.PendingCue == "skill_cast" && rig.Heard.All(heard => !heard.Cue.StartsWith("skill.")),
            "queued skill never emits before its actual animation starts");
        rig.Animation.StepTimedAttack(1.3f);
        Require(rig.Animation.ActiveLogicalCue == "skill_cast" && rig.Animation.ActiveCue == "cast" &&
            rig.Heard.Count(heard => heard.Cue == "skill.start") == 1,
            "queued skill begins on first frame using logical cue through fallback");
        rig.Animation.ResetPresentation();
        rig.Animation.UseNeutralSkillFallback = false;
    }

    private async Task PausedPlayback(Rig rig)
    {
        rig.Heard.Clear();
        rig.Animation.SetPaused(true);
        rig.Animation.BeginTimedAttack(new BattleAttackTiming(1.2f, .8f));
        rig.Animation.StepTimedAttack(.9f);
        rig.Animation.CompleteTimedWindup(.95f);
        await Frames(4);
        Require(AttackCount(rig) == 0 && rig.Sprite.Frame == 0, "paused action starts and public samples cannot advance audio or sprite");
        rig.Animation.SetPaused(false);
        Require(AttackCount(rig) == 1, "resume emits deferred first-frame sound once");
        rig.Animation.SetPaused(true);
        await Frames(4);
        rig.Animation.SetPaused(false);
        Require(AttackCount(rig) == 1, "second pause/resume does not replay first frame");
        rig.Animation.CancelTimedAttack();
        rig.Heard.Clear();
        rig.Animation.PlayCue("attack");
        await Until(() => rig.Sprite.Frame == 1, 1);
        rig.Animation.SetPaused(true);
        var count = AttackCount(rig);
        await ToSignal(GetTree().CreateTimer(.25), SceneTreeTimer.SignalName.Timeout);
        Require(rig.Sprite.Frame == 1 && AttackCount(rig) == count, "real sprite progression pauses together with marker playback");
        rig.Animation.SetPaused(false);
        await Until(() => rig.Animation.ActiveLogicalCue == "idle", 2);
        Require(AttackCount(rig) == 3, "natural animation completes remaining marks once after resume");
    }

    private static void InterruptedPlayback(Rig rig)
    {
        rig.Heard.Clear();
        rig.Animation.BeginTimedAttack(new BattleAttackTiming(1.2f, .8f));
        rig.Animation.PlayCue("skill_cast");
        rig.Animation.CancelTimedAttack();
        rig.Animation.StepTimedAttack(2);
        Require(AttackCount(rig) == 1 && rig.Heard.All(heard => !heard.Cue.StartsWith("skill.")),
            "cancel drops unfinished marks and queued action");
        rig.Heard.Clear();
        rig.Animation.BeginTimedAttack(new BattleAttackTiming(1.2f, .8f));
        rig.Animation.PlayCue("defeated");
        rig.Animation.StepTimedAttack(2);
        rig.Animation.PlayCue("attack");
        Require(rig.Animation.IsTerminal && AttackCount(rig) == 1 && rig.Heard.Count(heard => heard.Cue == "death.start") == 1,
            "death interrupts remaining attack audio and rejects later attack cues");
        rig.Animation.ResetPresentation();
        rig.Heard.Clear();
        rig.Animation.BeginTimedAttack(new BattleAttackTiming(1.2f, .8f));
        Require(rig.Sprite.Frame == 0 && AttackCount(rig) == 1, "reset after death permits clean first action");
        rig.Animation.CancelTimedAttack();
    }

    private async Task AcceleratedPlayback(Rig rig)
    {
        rig.Heard.Clear();
        rig.Animation.SetCombatSpeed(4);
        rig.Animation.BeginTimedAttack(new BattleAttackTiming(1.2f, .8f));
        var began = Time.GetTicksMsec();
        await Until(() => rig.Animation.ActiveLogicalCue == "idle", 1.5);
        Require(AttackCount(rig) == 3 && Time.GetTicksMsec() - began < 950,
            "accelerated timed attack emits all marks in accelerated visual playback");
        rig.Animation.SetCombatSpeed(1);
    }

    private async Task LoopPlayback(Rig rig)
    {
        rig.Animation.ResetPresentation();
        rig.Heard.Clear();
        var loops = 0;
        void Looped() => loops++;
        rig.Sprite.AnimationLooped += Looped;
        await Until(() => loops >= 2, 3.5);
        rig.Sprite.AnimationLooped -= Looped;
        Require(rig.Heard.Count(heard => heard.Cue == "idle.start") == loops,
            "multi-frame loop emits its frame-one mark exactly once on each wrap");
    }

    private async Task SingleFrameLoop()
    {
        var rig = CreateRig(singleIdle: true);
        rig.Animation.ResetPresentation();
        rig.Heard.Clear();
        var loops = 0;
        void Looped() => loops++;
        rig.Sprite.AnimationLooped += Looped;
        await Until(() => loops >= 2, 1.5);
        rig.Sprite.AnimationLooped -= Looped;
        Require(rig.Heard.Count(heard => heard.Cue == "idle.start") == loops,
            "single-frame loop emits once per loop even without changing frame index");
        rig.Animation.QueueFree();
        await Frames(2);
    }

    private static int AttackCount(Rig rig) => rig.Heard.Count(heard => heard.Cue.StartsWith("attack."));
    private async Task Frames(int count) { for (var index = 0; index < count; index++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Until(Func<bool> condition, double timeout)
    {
        var deadline = Time.GetTicksMsec() + (ulong)(timeout * 1000);
        while (!condition() && Time.GetTicksMsec() < deadline) await Frames(1);
        if (!condition()) throw new InvalidOperationException("timed out waiting for real animation progression");
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        GD.Print($"PASS {message}");
    }
}
