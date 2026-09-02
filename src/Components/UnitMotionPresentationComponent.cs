using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class UnitMotionPresentationComponent : Node
{
    [Signal] public delegate void MotionStateChangedEventHandler(bool moving);
    [Signal] public delegate void HorizontalSegmentStartedEventHandler(float horizontalDelta);
    [Signal] public delegate void SegmentProgressChangedEventHandler(float normalizedProgress);

    [Export] public float OneTimesCellSeconds { get; set; } = .24f;
    [Export] public float TwoTimesCellSeconds { get; set; } = .14f;
    [Export] public float FourTimesCellSeconds { get; set; } = .09f;
    [Export] public float MaximumVisualLagSeconds { get; set; } = .25f;
    [Export(PropertyHint.Range, "0.001,0.1,0.001")] public float MaximumFrameDeltaSeconds { get; set; } = .05f;
    [Export(PropertyHint.Range, "1,12,1")] public int MaximumQueuedWaypoints { get; set; } = 12;

    private readonly Queue<Vector2> _waypoints = [];
    private Node2D? _target;
    private Vector2 _segmentStart;
    private Vector2 _segmentTarget;
    private float _segmentElapsed;
    private float _segmentDuration;
    private float _catchUpDurationLimit = float.PositiveInfinity;
    private float _deltaCredit;
    private bool _isMoving;
    private bool _deferFreshMotionDelta;
    private bool _paused;
    private bool _terminal;
    private bool _hasPlacement;
    private float _speedScale = 1f;

    public bool IsMoving => _isMoving;
    public bool IsPaused => _paused;
    public bool IsTerminal => _terminal;
    public bool HasPlacement => _hasPlacement;
    public int PendingWaypointCount => _waypoints.Count;
    public float SpeedScale => _speedScale;

    public override void _Ready() => SetProcess(false);

    public void BindTarget(Node2D target)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        ResetMotion();
    }

    public void SnapTo(Vector2 position)
    {
        if (_terminal || _target is null) return;
        ClearActiveMotion();
        _target.Position = position;
        _hasPlacement = true;
        EmitSignal(SignalName.SegmentProgressChanged, 0f);
    }

    public void QueueWaypoint(Vector2 destination)
    {
        if (_terminal || _target is null || !_hasPlacement) return;
        if (_isMoving && _segmentTarget.IsEqualApprox(destination) && _waypoints.Count == 0) return;
        if (_waypoints.Count > 0 && _waypoints.Last().IsEqualApprox(destination)) return;
        if (!_isMoving)
        {
            BeginSegment(destination);
            return;
        }
        if (_waypoints.Count >= Math.Clamp(MaximumQueuedWaypoints, 1, 12))
        {
            ReplaceQueuedTail(destination);
            TightenCatchUpBudget();
            return;
        }
        _waypoints.Enqueue(destination);
        TightenCatchUpBudget();
    }

    public void RemapCoordinates(Vector2 oldOrigin, Vector2 oldPitch, Vector2 newOrigin, Vector2 newPitch)
    {
        if (_target is null || !_hasPlacement || oldPitch.X <= 0f || oldPitch.Y <= 0f || newPitch.X <= 0f || newPitch.Y <= 0f) return;
        Vector2 Remap(Vector2 position)
        {
            var logical = new Vector2((position.X - oldOrigin.X) / oldPitch.X, (position.Y - oldOrigin.Y) / oldPitch.Y);
            return newOrigin + new Vector2(logical.X * newPitch.X, logical.Y * newPitch.Y);
        }

        _target.Position = Remap(_target.Position);
        _segmentStart = Remap(_segmentStart);
        _segmentTarget = Remap(_segmentTarget);
        var queued = _waypoints.Select(Remap).ToArray();
        _waypoints.Clear();
        foreach (var waypoint in queued) _waypoints.Enqueue(waypoint);
        EmitSignal(SignalName.SegmentProgressChanged, SegmentProgress());
    }

    public void SetPaused(bool paused)
    {
        if (_paused == paused) return;
        _paused = paused;
        _deltaCredit = 0f;
        if (!_paused && _isMoving) _deferFreshMotionDelta = true;
    }

    public void SetSpeedScale(float speedScale)
    {
        var normalized = speedScale >= 4f ? 4f : speedScale >= 2f ? 2f : 1f;
        if (Mathf.IsEqualApprox(_speedScale, normalized)) return;
        var progress = SegmentProgress();
        _speedScale = normalized;
        if (!_isMoving) return;
        _segmentDuration = EffectiveSegmentDuration();
        _segmentElapsed = progress * _segmentDuration;
        _deltaCredit = 0f;
        _deferFreshMotionDelta = true;
    }

    public void CancelForDefeat()
    {
        if (_terminal) return;
        _terminal = true;
        ClearActiveMotion();
    }

    public void ResetMotion()
    {
        _terminal = false;
        _paused = false;
        _hasPlacement = false;
        ClearActiveMotion();
    }

    public override void _Process(double delta)
    {
        if (_paused || _terminal || !_isMoving || _target is null) return;
        if (_deferFreshMotionDelta)
        {
            _deferFreshMotionDelta = false;
            _deltaCredit = 0f;
            return;
        }

        var frameCap = Math.Max(.001f, MaximumFrameDeltaSeconds);
        var creditCap = Math.Max(frameCap, MaximumVisualLagSeconds);
        _deltaCredit = Math.Min(creditCap, _deltaCredit + Math.Min(frameCap, Math.Max(0f, (float)delta)));
        var remainingSegment = Math.Max(0f, _segmentDuration - _segmentElapsed);
        if (_deltaCredit + .000001f < remainingSegment)
        {
            _segmentElapsed += _deltaCredit;
            _deltaCredit = 0f;
            ApplyCurrentPosition();
            return;
        }

        _segmentElapsed = _segmentDuration;
        ApplyCurrentPosition();
        _deltaCredit = Math.Max(0f, _deltaCredit - remainingSegment);
        if (_waypoints.Count > 0)
        {
            BeginSegment(_waypoints.Dequeue(), emitState: false);
            return;
        }
        CompleteMotion();
    }

    public override void _ExitTree()
    {
        ResetMotion();
        _target = null;
    }

    private void BeginSegment(Vector2 destination, bool emitState = true)
    {
        if (_target is null) return;
        _segmentStart = _target.Position;
        _segmentTarget = destination;
        _segmentElapsed = 0f;
        _segmentDuration = EffectiveSegmentDuration();
        EmitSignal(SignalName.SegmentProgressChanged, 0f);
        if (_segmentStart.IsEqualApprox(_segmentTarget))
        {
            if (_waypoints.Count > 0) BeginSegment(_waypoints.Dequeue(), emitState);
            return;
        }
        var changed = !_isMoving;
        _isMoving = true;
        if (changed)
        {
            _deferFreshMotionDelta = true;
            _deltaCredit = 0f;
        }
        SetProcess(true);
        var horizontalDelta = _segmentTarget.X - _segmentStart.X;
        if (Math.Abs(horizontalDelta) > .001f)
            EmitSignal(SignalName.HorizontalSegmentStarted, horizontalDelta);
        if (emitState && changed) EmitSignal(SignalName.MotionStateChanged, true);
    }

    private void TightenCatchUpBudget()
    {
        if (!_isMoving) return;
        var remainingWeight = Math.Max(.001f, 1f - SegmentProgress()) + _waypoints.Count;
        if (remainingWeight <= 1f) return;
        var budget = Math.Max(.01f, MaximumVisualLagSeconds) / remainingWeight;
        _catchUpDurationLimit = Math.Min(_catchUpDurationLimit, budget);
        var progress = SegmentProgress();
        _segmentDuration = EffectiveSegmentDuration();
        _segmentElapsed = progress * _segmentDuration;
    }

    private void ReplaceQueuedTail(Vector2 newestDestination)
    {
        var retained = _waypoints.Take(Math.Max(0, _waypoints.Count - 1)).ToArray();
        _waypoints.Clear();
        foreach (var waypoint in retained) _waypoints.Enqueue(waypoint);
        _waypoints.Enqueue(newestDestination);
    }

    private float EffectiveSegmentDuration() => Math.Max(.005f, Math.Min(DurationForSpeed(), _catchUpDurationLimit));

    private float DurationForSpeed() => _speedScale switch
    {
        >= 4f => Math.Max(.005f, FourTimesCellSeconds),
        >= 2f => Math.Max(.005f, TwoTimesCellSeconds),
        _ => Math.Max(.005f, OneTimesCellSeconds)
    };

    private float SegmentProgress() => !_isMoving || _segmentDuration <= 0f
        ? 0f
        : Mathf.Clamp(_segmentElapsed / _segmentDuration, 0f, 1f);

    private void ApplyCurrentPosition()
    {
        if (_target is null) return;
        var progress = SegmentProgress();
        // Smoothstep is monotonic and endpoint-exact, so authored grid segments gain weight
        // without bounce, overshoot, or any departure from their axis-aligned path.
        var easedProgress = progress * progress * (3f - 2f * progress);
        _target.Position = _segmentStart.Lerp(_segmentTarget, easedProgress);
        EmitSignal(SignalName.SegmentProgressChanged, progress);
    }

    private void CompleteMotion()
    {
        _isMoving = false;
        _segmentElapsed = 0f;
        _segmentDuration = 0f;
        _catchUpDurationLimit = float.PositiveInfinity;
        _deltaCredit = 0f;
        _deferFreshMotionDelta = false;
        SetProcess(false);
        EmitSignal(SignalName.SegmentProgressChanged, 0f);
        EmitSignal(SignalName.MotionStateChanged, false);
    }

    private void ClearActiveMotion()
    {
        var changed = _isMoving;
        _waypoints.Clear();
        _isMoving = false;
        _segmentElapsed = 0f;
        _segmentDuration = 0f;
        _catchUpDurationLimit = float.PositiveInfinity;
        _deltaCredit = 0f;
        _deferFreshMotionDelta = false;
        SetProcess(false);
        EmitSignal(SignalName.SegmentProgressChanged, 0f);
        if (changed) EmitSignal(SignalName.MotionStateChanged, false);
    }
}
