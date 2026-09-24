using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.Audio;

// A view-owned pool, never a simulation clock or RNG consumer. The authored scene
// fixes the voice budget; crowded combat drops low-priority duplicates instead of
// building a delayed queue of obsolete impacts.
public partial class FeedbackAudio : Node
{
    [Export] public Godot.Collections.Array<FeedbackSound> Sounds { get; set; } = [];
    [Export] public Godot.Collections.Dictionary<string, string> AbilityCues { get; set; } = new();
    [Export] public float HighlightDuckDb { get; set; } = -7;
    [Export] public float HighlightHoldSeconds { get; set; } = .12f;
    [Export] public float DuckAttackSeconds { get; set; } = .025f;
    [Export] public float DuckReleaseSeconds { get; set; } = .2f;
    private readonly Dictionary<string, FeedbackSound> _sounds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ulong> _lastPlayed = new(StringComparer.Ordinal);
    private sealed class VoiceState(FeedbackSound sound)
    {
        public readonly FeedbackSound Sound = sound;
        public float Elapsed;
        public string Cue => Sound.Cue;
        public int Priority => Sound.Priority;
    }
    private readonly Dictionary<string, ulong> _lastGroupPlayed = new(StringComparer.Ordinal);
    private readonly Dictionary<AudioStreamPlayer, VoiceState> _owners = [];
    private float _highlightHold;
    private float _duckDb;
    private ulong _lastMixUpdate;
    private AudioStreamPlayer[] _voices = [];
    private bool _paused;
    private int _variation;
    private string _pendingUi = "";
    public int ActiveVoiceCount => _voices.Count(voice => voice.Playing);
    public int VoiceBudget => _voices.Length;
    public int PlayedCount { get; private set; }
    public string LastPlayedCue { get; private set; } = "";
    public string AbilityCue(string stableId) => AbilityCues.TryGetValue(stableId, out var cue) ? cue : "";

    public override void _Ready()
    {
        _voices = GetChildren().OfType<AudioStreamPlayer>().ToArray();
        _lastMixUpdate = Time.GetTicksUsec();
        foreach (var sound in Sounds)
            if (sound.Stream is not null && !string.IsNullOrWhiteSpace(sound.Cue)) _sounds.Add(sound.Cue, sound);
    }

    public bool Play(string cue) => _sounds.TryGetValue(cue, out var sound) && Play(sound);

    // Unit-authored animation markers use the same bounded mixer as event sounds.
    // A sound's cue is its concurrency group; the stream stays in the authored resource.
    public bool Play(FeedbackSound sound)
    {
        if (_paused || sound?.Stream is null || string.IsNullOrWhiteSpace(sound.Cue)) return false;
        AdvanceMix();
        var cue = sound.Cue;
        var now = Time.GetTicksMsec();
        if (_lastPlayed.TryGetValue(cue, out var last) && now - last < sound.MinimumInterval * 1000) return false;
        if (_voices.Count(voice => voice.Playing && _owners.TryGetValue(voice, out var owner) && owner.Cue == cue)
            >= sound.MaximumVoices) return false;
        var group = sound.MixGroup;
        AudioStreamPlayer? groupReplacement = null;
        if (group is not null)
        {
            if (_lastGroupPlayed.TryGetValue(group.Id, out var groupLast) && now - groupLast < group.MinimumInterval * 1000) return false;
            if (_voices.Count(candidate => candidate.Playing && _owners.TryGetValue(candidate, out var active)
                && active.Sound.MixGroup?.Id == group.Id) >= group.MaximumVoices)
            {
                groupReplacement = _voices.Where(candidate => candidate.Playing && _owners.TryGetValue(candidate, out var active)
                    && active.Sound.MixGroup?.Id == group.Id && active.Priority < sound.Priority)
                    .OrderBy(candidate => _owners[candidate].Priority).FirstOrDefault();
                if (groupReplacement is null) return false;
            }
        }
        var voice = groupReplacement ?? _voices.FirstOrDefault(candidate => !candidate.Playing) ??
            _voices.Where(candidate => _owners.TryGetValue(candidate, out var owner) && owner.Priority < sound.Priority)
                .OrderBy(candidate => _owners[candidate].Priority).FirstOrDefault();
        if (voice is null) return false;
        voice.Stop();
        voice.Stream = sound.Stream;
        voice.VolumeDb = sound.VolumeDb + (group?.DuckUnderHighlights == true ? _duckDb : 0);
        voice.PitchScale = sound.VaryPitch ? 1f + ((_variation++ % 5) - 2) * .018f : 1f;
        voice.StreamPaused = false;
        voice.Play();
        _owners[voice] = new VoiceState(sound);
        _lastPlayed[cue] = now;
        if (group is not null) _lastGroupPlayed[group.Id] = now;
        if (group?.IsHighlight == true) _highlightHold = Math.Max(_highlightHold, HighlightHoldSeconds);
        LastPlayedCue = cue;
        PlayedCount++;
        return true;
    }

    public override void _Process(double delta) => AdvanceMix();

    private void AdvanceMix()
    {
        var now = Time.GetTicksUsec();
        var seconds = (now - _lastMixUpdate) / 1_000_000f;
        _lastMixUpdate = now;
        if (_paused) return;
        // Match audio's real-time clock, including sounds started late within a
        // slow frame. Simulation speed and an earlier frame delta cannot trim them.
        var target = _highlightHold > 0 ? HighlightDuckDb : 0;
        _highlightHold = Math.Max(0, _highlightHold - seconds);
        var transition = target < _duckDb ? DuckAttackSeconds : DuckReleaseSeconds;
        _duckDb = Mathf.MoveToward(_duckDb, target, Math.Abs(HighlightDuckDb) * seconds / Math.Max(.001f, transition));
        foreach (var (voice, state) in _owners)
        {
            if (!voice.Playing) continue;
            state.Elapsed += seconds;
            var sound = state.Sound;
            var tailDb = 0f;
            if (sound.MaximumDuration > 0)
            {
                var remaining = sound.MaximumDuration - state.Elapsed;
                if (remaining <= 0) { voice.Stop(); continue; }
                var fade = Math.Min(.04f, sound.MaximumDuration * .3f);
                tailDb = Mathf.LinearToDb(Math.Min(1, remaining / fade));
            }
            voice.VolumeDb = sound.VolumeDb + tailDb + (sound.MixGroup?.DuckUnderHighlights == true ? _duckDb : 0);
        }
    }

    // Menu hover/focus or button activation can share a frame with a command result.
    // Resolve that input to its most informative cue independently of signal order.
    public void RequestUi(string cue)
    {
        if (!_sounds.TryGetValue(cue, out var sound)) return;
        var queued = !string.IsNullOrEmpty(_pendingUi);
        if (!queued || sound.Priority > _sounds[_pendingUi].Priority) _pendingUi = cue;
        if (!queued) Callable.From(FlushUi).CallDeferred();
    }

    private void FlushUi()
    {
        var cue = _pendingUi;
        _pendingUi = "";
        if (!string.IsNullOrEmpty(cue)) Play(cue);
    }

    public void SetPaused(bool paused)
    {
        AdvanceMix();
        _paused = paused;
        foreach (var voice in _voices) voice.StreamPaused = paused;
    }

    public void Clear()
    {
        foreach (var voice in _voices) { voice.Stop(); voice.StreamPaused = false; }
        _owners.Clear();
        _lastPlayed.Clear();
        _lastGroupPlayed.Clear();
        _highlightHold = 0;
        _duckDb = 0;
        _lastMixUpdate = Time.GetTicksUsec();
        _pendingUi = "";
        _paused = false;
    }

    public override void _ExitTree() => Clear();
}
