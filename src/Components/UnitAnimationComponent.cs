using Godot;
using System;
using TowerAutobattler.Content;
using TowerAutobattler.Battle;
using TowerAutobattler.Audio;
using System.Collections.Generic;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class UnitAnimationComponent : Node2D
{
    [Signal] public delegate void DefeatFadeRequestedEventHandler(float duration);

    [Export] public SpriteFrames Frames { get; set; } = null!;
    [Export] public bool AuthoredFacingRight { get; set; } = true;
    [Export] public float VisualScale { get; set; } = 1.15f;
    [Export] public float AttackActionWindowSeconds { get; set; } = .62f;
    [Export] public float SkillActionWindowSeconds { get; set; } = .72f;
    // Content without a cast clip can use its neutral pose alongside source VFX.
    // This pose yields to the next real action instead of queuing a second cast.
    [Export] public bool UseNeutralSkillFallback { get; set; }
    [Export] public float DefeatActionWindowSeconds { get; set; } = .8f;
    [Export] public float DefeatHoldSeconds { get; set; } = .24f;
    [Export] public float DefeatFadeSeconds { get; set; } = .32f;
    [Export(PropertyHint.Range, "0,8,0.25")] public float StepLiftPixels { get; set; } = 3f;
    [Export] public Godot.Collections.Array<UnitAnimationSound> FrameSounds { get; set; } = [];

    public event Action<FeedbackSound>? AnimationSoundRequested;

    private AnimatedSprite2D _sprite = null!;
    private UnitReadabilityComponent _readability = null!;
    private PlaybackState _state;
    private string _baseCue = "idle";
    private string _activeLogicalCue = "idle";
    private string _pendingCue = string.Empty;
    private float _remaining;
    private bool _paused;
    private bool _facingLocked;
    private Vector2 _authoredSpritePosition;
    private float _retainedMovePhase;
    private bool _timedAttack;
    private bool _freshTimedAttack;
    private float _combatSpeed = 1;
    private string _displacementCue = string.Empty;
    private float _displacementElevation;
    private float _movementLift;
    private readonly List<UnitAnimationSound> _activeFrameSounds = [];
    private string _soundLogicalCue = string.Empty;
    private int _nextFrameSound;
    private int _lastSoundFrame = -1;
    private bool _settingAnimationFrame;

    public bool IsTerminal => _state is PlaybackState.Defeated or PlaybackState.Hidden;
    public string ActiveCue => _sprite?.Animation.ToString() ?? string.Empty;
    public string ActiveLogicalCue => _activeLogicalCue;
    public string PendingCue => _pendingCue;
    public float ActivePlaybackSeconds { get; private set; }
    public float ActiveAuthoredSeconds { get; private set; }
    public int ActiveFrameCount { get; private set; }
    public float PlaybackSpeedScale => _sprite?.SpeedScale ?? 1f;
    public bool FacingRight { get; private set; } = true;
    public float RetainedMovePhase => _retainedMovePhase;
    public Vector2 BodyGlobalPosition => _sprite?.GlobalPosition ?? GlobalPosition;
    public TowerAutobattler.Vfx.VfxBodyVisual? CaptureBodyVisual(Transform2D worldToStage) =>
        _sprite is null || IsTerminal ? null : TowerAutobattler.Vfx.VfxBodyVisual.Capture(_sprite, worldToStage);

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _readability = GetNode<UnitReadabilityComponent>("UnitReadabilityComponent");
        _sprite.SpriteFrames = Frames;
        _sprite.FrameChanged += OnAnimationFrameChanged;
        _sprite.AnimationLooped += OnAnimationLooped;
        _sprite.Scale = Vector2.One * VisualScale;
        _authoredSpritePosition = _sprite.Position;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_sprite is not null)
        {
            _sprite.FrameChanged -= OnAnimationFrameChanged;
            _sprite.AnimationLooped -= OnAnimationLooped;
        }
        ClearFrameSoundPlayback();
    }

    public bool HasFrameSounds(string logicalCue)
    {
        foreach (var marker in FrameSounds)
            if (marker is not null && marker.Animation == logicalCue && IsValidFrameSound(marker)) return true;
        return false;
    }

    public ValidationReport ValidateFrameSounds()
    {
        var report = new ValidationReport();
        for (var index = 0; index < FrameSounds.Count; index++)
        {
            var marker = FrameSounds[index];
            var path = $"{SceneFilePath}/{Name}.FrameSounds[{index}]";
            if (marker is null) { report.Error($"{path}: missing animation sound marker"); continue; }
            if (!IsKnownLogicalCue(marker.Animation))
                report.Error($"{path}: unknown logical animation '{marker.Animation}'");
            else if (Frames is null || Frames.GetAnimationNames().Length == 0)
                report.Error($"{path}: missing SpriteFrames animations");
            else
            {
                var resolved = Resolve(marker.Animation);
                var count = Frames.GetFrameCount(resolved);
                if (marker.Frame < 1 || marker.Frame > count)
                    report.Error($"{path}: frame {marker.Frame} is outside '{resolved}' frames 1..{count}");
            }
            if (marker.Sound?.Stream is null) report.Error($"{path}: missing FeedbackSound or audio stream");
            else if (string.IsNullOrWhiteSpace(marker.Sound.Cue)) report.Error($"{path}: missing sound concurrency cue");
        }
        return report;
    }

    public void BindReadability(UnitDefinition definition, int team)
    {
        _readability.Bind(definition, team);
        SetDefaultFacing(team);
    }

    public void ResetPresentation()
    {
        ClearFrameSoundPlayback();
        _state = PlaybackState.Looping;
        _baseCue = "idle";
        _activeLogicalCue = "idle";
        _pendingCue = string.Empty;
        _remaining = 0;
        _timedAttack = false;
        _paused = false;
        _facingLocked = false;
        ActivePlaybackSeconds = 0;
        ActiveAuthoredSeconds = 0;
        ActiveFrameCount = 0;
        ResetMovementPresentation();
        Visible = true;
        SetProcess(false);
        PlayResolved("idle", restart: true);
    }

    public void FaceHorizontal(float horizontalDelta)
    {
        if (_facingLocked || Math.Abs(horizontalDelta) <= .001f) return;
        SetFacing(horizontalDelta > 0);
    }

    public void SetPaused(bool paused)
    {
        if (_paused == paused || _sprite is null) return;
        _paused = paused;
        if (_paused) _sprite.Pause();
        else if (_state != PlaybackState.Hidden)
        {
            if (!_timedAttack) _sprite.Play();
            DispatchFrameSounds(_sprite.Frame);
        }
    }

    public void SetCombatSpeed(float speed)
    {
        _combatSpeed = Math.Max(.001f, speed);
        if (!string.IsNullOrEmpty(_displacementCue) && _sprite is not null)
            _sprite.SpeedScale = _combatSpeed;
    }

    // A release-driven attack replaces queued decorative actions; queuing it behind
    // an old one-shot would separate the bow pose from the authoritative launch again.
    public void BeginTimedAttack(BattleAttackTiming timing)
    {
        if (IsTerminal || _sprite?.SpriteFrames is null || !string.IsNullOrEmpty(_displacementCue)) return;
        CaptureMovePhase();
        _timedAttack = true;
        _freshTimedAttack = true;
        _state = PlaybackState.OneShot;
        _pendingCue = string.Empty;
        _activeLogicalCue = "attack";
        var resolved = Resolve("attack");
        ConfigureActionPlayback("attack", resolved, timing.PlaybackSeconds);
        _remaining = ActivePlaybackSeconds;
        PlayResolved(resolved, restart: true);
        SampleTimedAttack();
        SetProcess(true);
    }

    public void CancelTimedAttack()
    {
        if (!_timedAttack) return;
        _timedAttack = false;
        _remaining = 0;
        _pendingCue = string.Empty;
        _state = PlaybackState.Looping;
        _activeLogicalCue = _baseCue;
        PlayResolved(_baseCue);
        SetProcess(false);
    }

    public void StepTimedAttack(float seconds)
    {
        if (!_timedAttack || _paused) return;
        _freshTimedAttack = false;
        AdvanceAction(seconds);
    }

    public void CompleteTimedWindup(float releaseProgress)
    {
        if (!_timedAttack || _paused) return;
        _remaining = Math.Min(_remaining, ActivePlaybackSeconds * (1 - releaseProgress));
        _freshTimedAttack = false;
        SampleTimedAttack();
    }

    private void SampleTimedAttack()
    {
        var progress = Mathf.Clamp(1 - _remaining / ActivePlaybackSeconds, 0, .999999f);
        var animation = _sprite.Animation;
        var frameUnits = progress * ActiveAuthoredSeconds * (float)_sprite.SpriteFrames.GetAnimationSpeed(animation) + .00001f;
        for (var frame = 0; frame < ActiveFrameCount; frame++)
        {
            var duration = _sprite.SpriteFrames.GetFrameDuration(animation, frame);
            if (frameUnits < duration || frame == ActiveFrameCount - 1)
            {
                _settingAnimationFrame = true;
                _sprite.SetFrameAndProgress(frame, Mathf.Clamp(frameUnits / duration, 0, .999999f));
                _settingAnimationFrame = false;
                DispatchFrameSounds(frame);
                break;
            }
            frameUnits -= duration;
        }
    }

    public void SetMovementWeight(float normalizedWeight)
    {
        if (_sprite is null) return;
        // This is one sustained travel weight, not per-sample progress. It prevents every 10 Hz
        // authority sample from restarting a decorative hop while the root follows its polyline.
        _movementLift = Math.Max(0f, StepLiftPixels) * Mathf.Clamp(normalizedWeight, 0f, 1f);
        ApplySpriteLift();
    }

    // A forced move owns the pose until its authoritative finish/cancel fact. Ordinary
    // idle refreshes must not enqueue attacks which then play after the displacement.
    public void SetDisplacementCue(string cue)
    {
        if (IsTerminal || _sprite?.SpriteFrames is null) return;
        if (cue == "hit") cue = "displaced";
        _displacementCue = cue;
        _displacementElevation = 0f;
        _movementLift = 0f;
        _timedAttack = false;
        _freshTimedAttack = false;
        _pendingCue = string.Empty;
        _remaining = 0f;
        _baseCue = "idle";
        _state = PlaybackState.Looping;
        _facingLocked = cue == "displaced";
        _activeLogicalCue = string.IsNullOrEmpty(cue) ? "idle" : cue;
        PlayResolved(Resolve(_activeLogicalCue), restart: true);
        _sprite.SpeedScale = string.IsNullOrEmpty(cue) ? 1f : _combatSpeed;
        ApplySpriteLift();
        SetProcess(false);
    }

    public void SetDisplacementElevation(float pixels)
    {
        _displacementElevation = Math.Max(0f, pixels);
        ApplySpriteLift();
    }

    private void ApplySpriteLift()
    {
        if (_sprite is null) return;
        var lift = string.IsNullOrEmpty(_displacementCue) ? _movementLift : _displacementElevation;
        _sprite.Position = _authoredSpritePosition + Vector2.Up * lift;
    }

    public void ResetMovementPresentation()
    {
        _retainedMovePhase = 0f;
        _displacementCue = string.Empty;
        _displacementElevation = 0f;
        _movementLift = 0f;
        if (_sprite is not null) _sprite.Position = _authoredSpritePosition;
    }

    public void PlayCue(string cue)
    {
        // Legacy callers may still send a damage cue. Ignore it without restarting
        // locomotion, replacing an attack, or adding a delayed reaction to the queue.
        if (cue == "hit") return;
        if (_sprite?.SpriteFrames is null) return;
        if (cue == "defeated")
        {
            BeginDefeat();
            return;
        }
        if (IsTerminal) return;
        if (!string.IsNullOrEmpty(_displacementCue)) return;
        if (PresentationCuePolicy.IsAction(cue))
        {
            if (_state == PlaybackState.OneShot && !UsesNeutralSkillPose(cue) && !UsesNeutralSkillPose(_activeLogicalCue))
            {
                _pendingCue = string.IsNullOrEmpty(_pendingCue) ? cue : PresentationCuePolicy.Prefer(_pendingCue, cue);
                return;
            }
            BeginAction(cue);
            return;
        }
        _baseCue = cue is "move" ? "move" : "idle";
        if (_state == PlaybackState.OneShot) return;
        if (_baseCue == "idle") CaptureMovePhase();
        _state = PlaybackState.Looping;
        _activeLogicalCue = _baseCue;
        PlayResolved(_baseCue);
    }

    public override void _Process(double delta)
    {
        if (_paused) return;
        if (_timedAttack && _freshTimedAttack) { _freshTimedAttack = false; return; }
        AdvanceAction((float)delta * (_timedAttack ? _combatSpeed : 1));
    }

    private void AdvanceAction(float seconds)
    {
        if (_state is not (PlaybackState.OneShot or PlaybackState.Defeated)) return;
        _remaining -= seconds;
        if (_timedAttack) SampleTimedAttack();
        if (_remaining > 0) return;
        _timedAttack = false;
        if (_state == PlaybackState.OneShot)
        {
            if (!string.IsNullOrEmpty(_pendingCue))
            {
                var pending = _pendingCue;
                _pendingCue = string.Empty;
                BeginAction(pending);
                return;
            }
            _state = PlaybackState.Looping;
            _activeLogicalCue = _baseCue;
            PlayResolved(_baseCue);
            SetProcess(false);
            return;
        }
        _state = PlaybackState.Hidden;
        ClearFrameSoundPlayback();
        SetProcess(false);
        EmitSignal(SignalName.DefeatFadeRequested, DefeatFadeSeconds);
    }

    private void BeginDefeat()
    {
        if (IsTerminal) return;
        _timedAttack = false;
        ResetMovementPresentation();
        _facingLocked = true;
        _state = PlaybackState.Defeated;
        _activeLogicalCue = "defeated";
        _pendingCue = string.Empty;
        var resolved = Resolve("defeated");
        ConfigureActionPlayback("defeated", resolved, DefeatActionWindowSeconds);
        _remaining = ActivePlaybackSeconds + Math.Max(0, DefeatHoldSeconds);
        PlayResolved(resolved, restart: true);
        SetProcess(true);
    }

    private void BeginAction(string cue)
    {
        _timedAttack = false;
        _pendingCue = string.Empty;
        CaptureMovePhase();
        _state = PlaybackState.OneShot;
        _activeLogicalCue = cue;
        var resolved = Resolve(cue);
        ConfigureActionPlayback(cue, resolved, ActionWindow(cue));
        _remaining = ActivePlaybackSeconds;
        PlayResolved(resolved, restart: true);
        SetProcess(true);
    }

    private float ActionWindow(string cue) => cue switch
    {
        "skill_cast" => SkillActionWindowSeconds,
        _ => AttackActionWindowSeconds
    };

    private void ConfigureActionPlayback(string cue, StringName resolved, float requestedWindow)
    {
        ActiveFrameCount = _sprite.SpriteFrames.GetFrameCount(resolved);
        ActiveAuthoredSeconds = AuthoredDuration(resolved);
        ActivePlaybackSeconds = Math.Max(.05f, requestedWindow);
        _sprite.SpeedScale = UsesNeutralSkillPose(cue) || ActiveAuthoredSeconds <= 0 ? 1f : ActiveAuthoredSeconds / ActivePlaybackSeconds;
    }

    private void PlayResolved(string cue, bool restart = false) => PlayResolved(Resolve(cue), restart);

    private void PlayResolved(StringName resolved, bool restart = false)
    {
        if (_state == PlaybackState.Looping) _sprite.SpeedScale = 1f;
        var shouldStart = restart || _sprite.Animation != resolved || _soundLogicalCue != _activeLogicalCue;
        if (shouldStart)
        {
            // Play(same_animation) resumes its existing frame. Reset explicitly while
            // suppressing callbacks, then start a fresh sound timeline for this action.
            _settingAnimationFrame = true;
            _sprite.Stop();
            _sprite.Play(resolved);
            _sprite.SetFrameAndProgress(0, 0);
            if (!restart && IsResolvedMove(resolved)) RestoreMovePhase(resolved);
            _settingAnimationFrame = false;
            BeginFrameSoundPlayback();
            // A restored walk phase starts here: don't backfill footsteps before it.
            _lastSoundFrame = _sprite.Frame - 1;
            while (_nextFrameSound < _activeFrameSounds.Count && _activeFrameSounds[_nextFrameSound].Frame - 1 < _sprite.Frame)
                _nextFrameSound++;
            DispatchFrameSounds(_sprite.Frame);
        }
        if (_paused || _timedAttack) _sprite.Pause();
    }

    private static bool IsKnownLogicalCue(string cue) => cue is "idle" or "move" or "attack" or "skill_cast" or "defeated" or "displaced";

    private bool IsValidFrameSound(UnitAnimationSound marker)
    {
        if (!IsKnownLogicalCue(marker.Animation) || Frames is null || Frames.GetAnimationNames().Length == 0 || marker.Sound?.Stream is null || string.IsNullOrWhiteSpace(marker.Sound.Cue))
            return false;
        return marker.Frame >= 1 && marker.Frame <= Frames.GetFrameCount(Resolve(marker.Animation));
    }

    private void BeginFrameSoundPlayback()
    {
        ClearFrameSoundPlayback();
        _soundLogicalCue = _activeLogicalCue;
        foreach (var marker in FrameSounds)
            if (marker is not null && marker.Animation == _soundLogicalCue && IsValidFrameSound(marker)) _activeFrameSounds.Add(marker);
        _activeFrameSounds.Sort((left, right) => left.Frame.CompareTo(right.Frame));
    }

    private void ClearFrameSoundPlayback()
    {
        _activeFrameSounds.Clear();
        _soundLogicalCue = string.Empty;
        _nextFrameSound = 0;
        _lastSoundFrame = -1;
    }

    private void OnAnimationFrameChanged()
    {
        if (_settingAnimationFrame || _paused || _timedAttack || _state == PlaybackState.Hidden) return;
        // AnimatedSprite emits frame_changed for each advanced frame, including loop
        // wrap. Sound marks repeat once per loop, not on repeated idle refresh calls.
        if (_sprite.Frame < _lastSoundFrame && _sprite.SpriteFrames.GetAnimationLoopMode(_sprite.Animation) == SpriteFrames.LoopMode.Linear)
        {
            _nextFrameSound = 0;
            _lastSoundFrame = -1;
        }
        DispatchFrameSounds(_sprite.Frame);
    }

    private void OnAnimationLooped()
    {
        if (_settingAnimationFrame || _paused || _timedAttack || _state == PlaybackState.Hidden) return;
        // A single-frame loop has no decreasing frame index to detect its wrap.
        if (_sprite.SpriteFrames.GetFrameCount(_sprite.Animation) != 1) return;
        _nextFrameSound = 0;
        _lastSoundFrame = -1;
        DispatchFrameSounds(0);
    }

    private void DispatchFrameSounds(int frame)
    {
        if (_paused || _state == PlaybackState.Hidden || frame < _lastSoundFrame) return;
        _lastSoundFrame = frame;
        // Timed attacks may skip display frames at high speed or when the authoritative
        // windup catches up. Every crossed mark still emits once, in authored order.
        while (_nextFrameSound < _activeFrameSounds.Count && _activeFrameSounds[_nextFrameSound].Frame - 1 <= frame)
        {
            var marker = _activeFrameSounds[_nextFrameSound++];
            AnimationSoundRequested?.Invoke(marker.Sound);
        }
    }

    private void CaptureMovePhase()
    {
        if (_sprite?.SpriteFrames is null || !IsResolvedMove(_sprite.Animation)) return;
        var frameCount = _sprite.SpriteFrames.GetFrameCount(_sprite.Animation);
        if (frameCount <= 1) return;
        // Keep only one normalized loop phase; repeated steps cannot grow callback or frame state.
        var phase = (_sprite.Frame + _sprite.FrameProgress) / frameCount;
        _retainedMovePhase = Mathf.PosMod(phase, 1f);
    }

    private void RestoreMovePhase(StringName resolved)
    {
        var frameCount = _sprite.SpriteFrames.GetFrameCount(resolved);
        if (frameCount <= 1 || _retainedMovePhase <= .0001f) return;
        var framePosition = _retainedMovePhase * frameCount;
        var frame = Math.Clamp(Mathf.FloorToInt(framePosition), 0, frameCount - 1);
        _sprite.Frame = frame;
        _sprite.FrameProgress = Mathf.Clamp(framePosition - frame, 0f, .9999f);
    }

    private bool IsResolvedMove(StringName animation) => animation == Resolve("move");

    private StringName Resolve(string cue)
    {
        var requested = new StringName(cue);
        return Frames.HasAnimation(requested) ? requested : ResolveFallback(cue);
    }

    private float AuthoredDuration(StringName animation)
    {
        var speed = Math.Max(.01, _sprite.SpriteFrames.GetAnimationSpeed(animation));
        var duration = 0f;
        for (var frame = 0; frame < _sprite.SpriteFrames.GetFrameCount(animation); frame++)
            duration += _sprite.SpriteFrames.GetFrameDuration(animation, frame);
        return (float)(duration / speed);
    }

    private StringName ResolveFallback(string cue)
    {
        string[] candidates = cue switch
        {
            "defeated" => ["defeated", "death", "idle"],
            "displaced" => ["idle", "breathing"],
            "skill_cast" => UseNeutralSkillFallback ? ["skill_cast", "cast", "idle"] : ["skill_cast", "cast", "attack", "idle"],
            "move" => ["move", "run", "idle"],
            _ => [cue, "idle", "breathing"]
        };
        foreach (var candidate in candidates)
            if (Frames.HasAnimation(candidate)) return candidate;
        return Frames.GetAnimationNames()[0];
    }

    private bool UsesNeutralSkillPose(string cue) => cue == "skill_cast" && UseNeutralSkillFallback &&
        !Frames.HasAnimation("skill_cast") && !Frames.HasAnimation("cast");

    private void SetDefaultFacing(int team)
    {
        _facingLocked = false;
        SetFacing(team == 0);
    }

    private void SetFacing(bool facingRight)
    {
        FacingRight = facingRight;
        if (_sprite is not null) _sprite.FlipH = AuthoredFacingRight != facingRight;
    }

    private enum PlaybackState { Looping, OneShot, Defeated, Hidden }
}
