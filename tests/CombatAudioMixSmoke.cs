using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using TowerAutobattler.Audio;

// Real voice admission, playback and time-based envelopes; no private-state access.
// Synthetic tones live only in this fixture and are intentionally quiet.
public partial class CombatAudioMixSmoke : Node
{
    private FeedbackAudio _mixer = null!;

    public override async void _Ready()
    {
        try
        {
            _mixer = GetNode<FeedbackAudio>("Mixer");
            Require(_mixer.VoiceBudget >= 6, "authored mixer has enough independent voices for envelope checks");
            await SharedGroupAdmission();
            await HighlightDucking();
            await PauseFreezesEnvelopesAndTail();
            await TailFadeAndClear();
            await CorpseExplosionCadence();
            _mixer.Clear();
            GD.Print("PASS CombatAudioMixSmoke: shared group admission, live ducking, pause, capped tails, clear and no delayed combat queue");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            _mixer?.Clear();
            GD.PushError($"FAIL CombatAudioMixSmoke: {error}");
            GetTree().Quit(1);
        }
    }

    private async Task SharedGroupAdmission()
    {
        _mixer.Clear();
        var first = Sound("blade.a", new FeedbackMixGroup { Id = "melee", MinimumInterval = .12f, MaximumVoices = 2 });
        // Separate resource instances with the same semantic id must share budget.
        var second = Sound("blade.b", new FeedbackMixGroup { Id = "melee", MinimumInterval = .12f, MaximumVoices = 2 });
        var third = Sound("blade.c", second.MixGroup);
        Require(_mixer.Play(first) && !_mixer.Play(second), "different attack cues share their group's minimum interval");
        await Wait(.16);
        Require(_mixer.Play(second), "second cue is admitted once shared interval expires");
        await Wait(.16);
        Require(!_mixer.Play(third) && _mixer.ActiveVoiceCount == 2, "different cues share group concurrency even after interval expires");
        third.Priority = 5;
        Require(_mixer.Play(third) && _mixer.ActiveVoiceCount == 2 && Voice(third).Playing,
            "higher-priority event can replace a lower-priority voice within the full group without increasing its budget");
        var urgent = Sound("blade.urgent", second.MixGroup);
        urgent.Priority = 6;
        Require(!_mixer.Play(urgent), "priority replacement still respects shared minimum interval");
        var independent = Sound("spell.a", new FeedbackMixGroup { Id = "spell", MaximumVoices = 2 });
        Require(_mixer.Play(independent) && _mixer.ActiveVoiceCount == 3, "other groups retain their independent voice budget");
        _mixer.Clear();
        Require(_mixer.Play(third), "Clear resets shared group admission history");
    }

    private async Task HighlightDucking()
    {
        _mixer.Clear();
        var ordinary = new FeedbackMixGroup { Id = "ordinary", MaximumVoices = 4, DuckUnderHighlights = true };
        var priority = new FeedbackMixGroup { Id = "highlight", MaximumVoices = 2, IsHighlight = true };
        var oldAttack = Sound("attack.old", ordinary);
        var newAttack = Sound("attack.new", ordinary);
        var ui = Sound("ui.ungrouped");
        var impact = Sound("highlight.hit", priority, .1f);
        Require(_mixer.Play(oldAttack) && _mixer.Play(ui), "ordinary combat voice and ungrouped UI voice begin together");
        Require(_mixer.Play(impact), "highlight is admitted immediately");
        await Until(() => Voice(oldAttack).VolumeDb < oldAttack.VolumeDb - 4, .2);
        Require(Near(Voice(ui).VolumeDb, ui.VolumeDb), "highlight leaves ungrouped UI volume unchanged");
        Require(_mixer.Play(newAttack) && Voice(newAttack).VolumeDb < newAttack.VolumeDb - 4,
            "ordinary voice starting during duck inherits the current envelope immediately");
        await Until(() => Near(Voice(oldAttack).VolumeDb, oldAttack.VolumeDb) && Near(Voice(newAttack).VolumeDb, newAttack.VolumeDb), .8);
        Require(Voice(oldAttack).Playing && Voice(newAttack).Playing && Near(Voice(ui).VolumeDb, ui.VolumeDb),
            "old and new combat voices recover while UI remains unchanged");
    }

    private async Task PauseFreezesEnvelopesAndTail()
    {
        _mixer.Clear();
        var ordinary = Sound("pause.ordinary", new FeedbackMixGroup { Id = "pause.normal", MaximumVoices = 2, DuckUnderHighlights = true });
        var capped = Sound("pause.capped", null, .4f);
        var highlight = Sound("pause.highlight", new FeedbackMixGroup { Id = "pause.highlight", MaximumVoices = 2, IsHighlight = true }, .1f);
        Require(_mixer.Play(ordinary) && _mixer.Play(capped) && _mixer.Play(highlight), "pause fixture starts ordinary, capped and highlight voices");
        await Until(() => Voice(ordinary).VolumeDb < ordinary.VolumeDb - 4, .2);
        var ordinaryVoice = Voice(ordinary);
        var cappedVoice = Voice(capped);
        _mixer.SetPaused(true);
        var beforeVolume = ordinaryVoice.VolumeDb;
        var beforePosition = cappedVoice.GetPlaybackPosition();
        await Wait(.5);
        Require(ordinaryVoice.StreamPaused && cappedVoice.StreamPaused && Near(ordinaryVoice.VolumeDb, beforeVolume),
            "pause freezes duck envelope instead of consuming its hold or release time");
        Require(Math.Abs(cappedVoice.GetPlaybackPosition() - beforePosition) < .06,
            "pause freezes real stream playback despite wall time exceeding capped duration");
        Require(!_mixer.Play(Sound("paused.reject")), "paused mixer rejects new sound requests");
        _mixer.SetPaused(false);
        await Wait(.06);
        Require(cappedVoice.Playing && !cappedVoice.StreamPaused, "resume preserves capped voice's remaining playback duration");
        await Until(() => !cappedVoice.Playing && Near(ordinaryVoice.VolumeDb, ordinary.VolumeDb), .8);
        Require(ordinaryVoice.Playing, "resumed capped voice ends and ordinary envelope recovers independently");
    }

    private async Task TailFadeAndClear()
    {
        _mixer.Clear();
        var capped = Sound("tail.short", null, .25f);
        Require(_mixer.Play(capped), "capped source starts through real audio player");
        var voice = Voice(capped);
        await Until(() => voice.Playing && voice.VolumeDb < capped.VolumeDb - 1, .4);
        Require(voice.Playing, "maximum-duration tail fades audibly before voice stops");
        await Until(() => !voice.Playing, .2);
        Require(_mixer.ActiveVoiceCount == 0, "maximum duration releases voice instead of playing the full three-second source");

        var ordinary = Sound("clear.ordinary", new FeedbackMixGroup { Id = "clear.normal", MaximumVoices = 2, DuckUnderHighlights = true });
        var highlight = Sound("clear.highlight", new FeedbackMixGroup { Id = "clear.highlight", MaximumVoices = 2, IsHighlight = true });
        Require(_mixer.Play(ordinary) && _mixer.Play(highlight), "clear fixture has active ducking voices");
        await Until(() => Voice(ordinary).VolumeDb < ordinary.VolumeDb - 4, .2);
        _mixer.SetPaused(true);
        _mixer.Clear();
        Require(_mixer.ActiveVoiceCount == 0 && Players().All(player => !player.StreamPaused),
            "Clear stops every voice and removes paused residue");
        Require(_mixer.Play(ordinary) && Near(Voice(ordinary).VolumeDb, ordinary.VolumeDb),
            "Clear removes old duck envelope and permits immediate clean playback");
        _mixer.Clear();
    }

    private async Task CorpseExplosionCadence()
    {
        _mixer.Clear();
        var group = new FeedbackMixGroup { Id = "corpse.explosion", MinimumInterval = .11f, MaximumVoices = 1, IsHighlight = true };
        var sounds = Enumerable.Range(0, 4).Select(index => Sound($"corpse.{index}", group, .08f)).ToArray();
        var startCount = _mixer.PlayedCount;
        var accepted = 0;
        for (var index = 0; index < 30; index++)
            if (_mixer.Play(sounds[index % sounds.Length])) accepted++;
        Require(accepted == 1 && _mixer.PlayedCount - startCount == 1, "simultaneous corpse burst drops excess requests immediately");
        await Wait(.2);
        Require(_mixer.ActiveVoiceCount == 0 && _mixer.PlayedCount - startCount == 1,
            "rejected burst does not become delayed explosions after its voice ends");
        for (var index = 0; index < 12; index++)
        {
            var before = _mixer.PlayedCount;
            var played = _mixer.Play(sounds[index % sounds.Length]);
            Require(_mixer.PlayedCount == before + (played ? 1 : 0), "cadence admission resolves synchronously with each death event");
            await Wait(.03);
        }
        var finalCount = _mixer.PlayedCount;
        await Wait(.35);
        Require(finalCount > startCount + 1 && _mixer.PlayedCount == finalCount && _mixer.ActiveVoiceCount == 0,
            "sustained corpse cadence admits new events without leaving a trailing queue");
    }

    private static FeedbackSound Sound(string cue, FeedbackMixGroup? group = null, float maximumDuration = 0) => new()
    {
        Cue = cue,
        Stream = Tone(),
        VolumeDb = -28,
        MinimumInterval = 0,
        MaximumVoices = 8,
        VaryPitch = false,
        MixGroup = group,
        MaximumDuration = maximumDuration
    };

    private static AudioStreamWav Tone()
    {
        const int sampleRate = 22050;
        var data = new byte[sampleRate * 3 * 2];
        for (var sample = 0; sample < data.Length / 2; sample++)
        {
            var value = (short)(Math.Sin(2 * Math.PI * 220 * sample / sampleRate) * 5000);
            data[sample * 2] = (byte)(value & 255);
            data[sample * 2 + 1] = (byte)((value >> 8) & 255);
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = sampleRate, Data = data };
    }

    private AudioStreamPlayer[] Players() => _mixer.GetChildren().OfType<AudioStreamPlayer>().ToArray();
    private AudioStreamPlayer Voice(FeedbackSound sound) => Players().Single(player => player.Stream == sound.Stream);
    private static bool Near(float left, float right) => Math.Abs(left - right) < .2f;
    private async Task Wait(double seconds)
    {
        // Admission uses monotonic wall time. A startup frame's large delta can
        // exhaust a new SceneTreeTimer before this much real time has elapsed.
        var deadline = Time.GetTicksMsec() + (ulong)(seconds * 1000);
        while (Time.GetTicksMsec() < deadline) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task Until(Func<bool> condition, double seconds)
    {
        var deadline = Time.GetTicksMsec() + (ulong)(seconds * 1000);
        while (!condition() && Time.GetTicksMsec() < deadline) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!condition()) throw new InvalidOperationException("Timed out waiting for real voice/envelope state");
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        GD.Print($"PASS {message}");
    }
}
