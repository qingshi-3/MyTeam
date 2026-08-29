using Godot;
using System;
using TowerAutobattler.Content;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class UnitAnimationComponent : Node2D
{
    [Signal] public delegate void DefeatFadeRequestedEventHandler(float duration);

    [Export] public SpriteFrames Frames { get; set; } = null!;
    [Export] public bool AuthoredFacingRight { get; set; } = true;
    [Export] public float VisualScale { get; set; } = 1.15f;
    [Export] public float AttackActionWindowSeconds { get; set; } = .62f;
    [Export] public float HitActionWindowSeconds { get; set; } = .28f;
    [Export] public float SkillActionWindowSeconds { get; set; } = .72f;
    [Export] public float DefeatActionWindowSeconds { get; set; } = .8f;
    [Export] public float DefeatHoldSeconds { get; set; } = .24f;
    [Export] public float DefeatFadeSeconds { get; set; } = .32f;
    [Export(PropertyHint.Range, "0,8,0.25")] public float StepLiftPixels { get; set; } = 3f;

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

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _readability = GetNode<UnitReadabilityComponent>("UnitReadabilityComponent");
        _sprite.SpriteFrames = Frames;
        _sprite.Scale = Vector2.One * VisualScale;
        _authoredSpritePosition = _sprite.Position;
        SetProcess(false);
    }

    public void BindReadability(UnitDefinition definition, int team)
    {
        _readability.Bind(definition, team);
        SetDefaultFacing(team);
    }

    public void ResetPresentation()
    {
        _state = PlaybackState.Looping;
        _baseCue = "idle";
        _activeLogicalCue = "idle";
        _pendingCue = string.Empty;
        _remaining = 0;
        _paused = false;
        _facingLocked = false;
        ActivePlaybackSeconds = 0;
        ActiveAuthoredSeconds = 0;
        ActiveFrameCount = 0;
        ResetMovementPresentation();
        Visible = true;
        SetProcess(false);
        PlayResolved("idle");
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
        else if (_state != PlaybackState.Hidden) _sprite.Play();
    }

    public void SetMovementProgress(float normalizedProgress)
    {
        if (_sprite is null) return;
        var progress = Mathf.Clamp(normalizedProgress, 0f, 1f);
        // Decorative weight belongs only to the character sprite. The unit root and sibling
        // readability components remain stable for path geometry, markers, and pointer input.
        var lift = progress <= .0001f || progress >= .9999f
            ? 0f
            : Math.Max(0f, StepLiftPixels) * Mathf.Sin(Mathf.Pi * progress);
        _sprite.Position = _authoredSpritePosition + Vector2.Up * lift;
    }

    public void ResetMovementPresentation()
    {
        _retainedMovePhase = 0f;
        if (_sprite is not null) _sprite.Position = _authoredSpritePosition;
    }

    public void PlayCue(string cue)
    {
        if (_sprite?.SpriteFrames is null) return;
        if (cue == "defeated")
        {
            BeginDefeat();
            return;
        }
        if (IsTerminal) return;
        if (PresentationCuePolicy.IsAction(cue))
        {
            if (_state == PlaybackState.OneShot)
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
        if (_state is not (PlaybackState.OneShot or PlaybackState.Defeated)) return;
        _remaining -= (float)delta;
        if (_remaining > 0) return;
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
        SetProcess(false);
        EmitSignal(SignalName.DefeatFadeRequested, DefeatFadeSeconds);
    }

    private void BeginDefeat()
    {
        if (IsTerminal) return;
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
        "hit" => HitActionWindowSeconds,
        "skill_cast" => SkillActionWindowSeconds,
        _ => AttackActionWindowSeconds
    };

    private void ConfigureActionPlayback(string cue, StringName resolved, float requestedWindow)
    {
        ActiveFrameCount = _sprite.SpriteFrames.GetFrameCount(resolved);
        ActiveAuthoredSeconds = AuthoredDuration(resolved);
        ActivePlaybackSeconds = Math.Max(.05f, requestedWindow);
        _sprite.SpeedScale = ActiveAuthoredSeconds <= 0 ? 1f : ActiveAuthoredSeconds / ActivePlaybackSeconds;
    }

    private void PlayResolved(string cue, bool restart = false) => PlayResolved(Resolve(cue), restart);

    private void PlayResolved(StringName resolved, bool restart = false)
    {
        if (_state == PlaybackState.Looping) _sprite.SpeedScale = 1f;
        var shouldStart = restart || _sprite.Animation != resolved || !_sprite.IsPlaying();
        if (shouldStart)
        {
            _sprite.Play(resolved);
            if (!restart && IsResolvedMove(resolved)) RestoreMovePhase(resolved);
        }
        if (_paused) _sprite.Pause();
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
        return _sprite.SpriteFrames.HasAnimation(requested) ? requested : ResolveFallback(cue);
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
            "defeated" => ["defeated", "death", "hit", "idle"],
            "skill_cast" => ["skill_cast", "cast", "breathing", "attack", "idle"],
            "move" => ["move", "run", "idle"],
            _ => [cue, "idle", "breathing"]
        };
        foreach (var candidate in candidates)
            if (_sprite.SpriteFrames.HasAnimation(candidate)) return candidate;
        return _sprite.SpriteFrames.GetAnimationNames()[0];
    }

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
