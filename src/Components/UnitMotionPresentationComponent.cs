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
    [Signal] public delegate void TravelWeightChangedEventHandler(float normalizedWeight);

    [Export(PropertyHint.Range, "0.01,0.5,0.005")] public float AuthoritySampleSecondsAtOneTimes { get; set; } = .125f;
    [Export] public float MaximumVisualLagSeconds { get; set; } = .25f;
    [Export(PropertyHint.Range, "0.001,0.1,0.001")] public float MaximumFrameDeltaSeconds { get; set; } = .05f;
    [Export(PropertyHint.Range, "1,64,1")] public int MaximumQueuedWaypoints { get; set; } = 12;
    [Export(PropertyHint.Range, "1,64,1")] public int MaximumSegmentsPerFrame { get; set; } = 16;

    private readonly List<QueuedWaypoint> _waypoints = [];
    private Node2D? _target;
    private Vector2 _segmentStart;
    private Vector2 _segmentTarget;
    private float _segmentElapsed;
    private float _segmentDuration;
    private float _settleElapsed;
    private float _settleDuration;
    private float _settleStartWeight;
    private float _activityElapsed;
    private float _travelWeight;
    private float _deltaCredit;
    private bool _segmentActive;
    private bool _settling;
    private bool _isMoving;
    private bool _deferFreshMotionDelta;
    private bool _requiresIntermediateFrame;
    private bool _paused;
    private bool _terminal;
    private bool _hasPlacement;
    private float _speedScale = 1f;

    public bool IsMoving => _isMoving;
    public bool IsPaused => _paused;
    public bool IsTerminal => _terminal;
    public bool HasPlacement => _hasPlacement;
    public int PendingWaypointCount => _waypoints.Count;
    public float PendingPlaybackSeconds => RemainingSegmentSeconds() +
                                           _waypoints.Sum(waypoint => waypoint.PlaybackSeconds) +
                                           (_settling ? Math.Max(0f, _settleDuration - _settleElapsed) : 0f);
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
    }

    public void QueueWaypoint(Vector2 destination)
    {
        if (_terminal || _target is null || !_hasPlacement) return;
        if (_segmentActive && _segmentTarget.IsEqualApprox(destination) && _waypoints.Count == 0) return;
        if (_waypoints.Count > 0 && _waypoints[^1].Position.IsEqualApprox(destination)) return;
        if (!_segmentActive)
        {
            BeginSegment(destination, EffectiveSampleSeconds(), emitState: !_isMoving);
            return;
        }

        var playbackSeconds = EffectiveSampleSeconds();
        var preferredLimit = Math.Clamp(MaximumQueuedWaypoints, 1, 64);
        if (_waypoints.Count >= preferredLimit && TryCoalesceTail(destination, playbackSeconds))
        {
            EnforceLagBudget();
            return;
        }
        // A corner is gameplay-significant geometry. The queue limit is therefore a soft memory
        // bound: under an exceptional render stall, retain an unmergeable corner instead of
        // drawing a shortcut through blocked terrain.
        _waypoints.Add(new QueuedWaypoint(destination, playbackSeconds));
        EnforceLagBudget();
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
        for (var index = 0; index < _waypoints.Count; index++)
            _waypoints[index] = _waypoints[index] with { Position = Remap(_waypoints[index].Position) };
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
        var durationScale = _speedScale / normalized;
        _speedScale = normalized;
        if (!_isMoving) return;

        _segmentElapsed *= durationScale;
        _segmentDuration *= durationScale;
        _settleElapsed *= durationScale;
        _settleDuration *= durationScale;
        for (var index = 0; index < _waypoints.Count; index++)
            _waypoints[index] = _waypoints[index] with
            {
                PlaybackSeconds = Math.Max(.001f, _waypoints[index].PlaybackSeconds * durationScale)
            };
        _deltaCredit = 0f;
        EnforceLagBudget();
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
        var segmentLimit = Math.Clamp(MaximumSegmentsPerFrame, 1, 64);
        var completedSegments = 0;
        while (_segmentActive && _deltaCredit > .000001f && completedSegments < segmentLimit)
        {
            if (!AdvanceSegment()) break;
            completedSegments++;
        }
        if (_settling && _deltaCredit > .000001f) AdvanceSettlement();
    }

    public override void _ExitTree()
    {
        ResetMotion();
        _target = null;
    }

    private bool AdvanceSegment()
    {
        var remaining = RemainingSegmentSeconds();
        if (_requiresIntermediateFrame && _deltaCredit + .000001f >= remaining)
        {
            _requiresIntermediateFrame = false;
            var endpointGuard = Math.Min(.001f, Math.Max(.000001f, _segmentDuration * .01f));
            var consumedBeforeEndpoint = Math.Max(0f, remaining - endpointGuard);
            _segmentElapsed += consumedBeforeEndpoint;
            _activityElapsed += consumedBeforeEndpoint;
            _deltaCredit = 0f;
            ApplyCurrentPosition();
            ApplyTravelWeight(Mathf.Clamp(_activityElapsed / Math.Min(.06f, EffectiveSampleSeconds()), 0f, 1f));
            return false;
        }
        _requiresIntermediateFrame = false;
        var consumed = Math.Min(_deltaCredit, remaining);
        _segmentElapsed += consumed;
        _activityElapsed += consumed;
        _deltaCredit = Math.Max(0f, _deltaCredit - consumed);
        ApplyCurrentPosition();
        ApplyTravelWeight(Mathf.Clamp(_activityElapsed / Math.Min(.06f, EffectiveSampleSeconds()), 0f, 1f));
        if (_segmentElapsed + .000001f < _segmentDuration) return false;

        _segmentElapsed = _segmentDuration;
        ApplyCurrentPosition();
        if (_waypoints.Count > 0)
        {
            var next = _waypoints[0];
            _waypoints.RemoveAt(0);
            BeginSegment(next.Position, next.PlaybackSeconds, emitState: false);
            return true;
        }
        BeginSettlement();
        return true;
    }

    private void AdvanceSettlement()
    {
        var remaining = Math.Max(0f, _settleDuration - _settleElapsed);
        var consumed = Math.Min(_deltaCredit, remaining);
        _settleElapsed += consumed;
        _deltaCredit = Math.Max(0f, _deltaCredit - consumed);
        var progress = _settleDuration <= 0f ? 1f : Mathf.Clamp(_settleElapsed / _settleDuration, 0f, 1f);
        ApplyTravelWeight(_settleStartWeight * (1f - progress));
        if (_settleElapsed + .000001f < _settleDuration) return;
        CompleteMotion();
    }

    private void BeginSegment(Vector2 destination, float playbackSeconds, bool emitState)
    {
        if (_target is null) return;
        _settling = false;
        _segmentStart = _target.Position;
        _segmentTarget = destination;
        _segmentElapsed = 0f;
        _segmentDuration = Math.Max(.001f, playbackSeconds);
        if (_segmentStart.IsEqualApprox(_segmentTarget))
        {
            if (_waypoints.Count > 0)
            {
                var next = _waypoints[0];
                _waypoints.RemoveAt(0);
                BeginSegment(next.Position, next.PlaybackSeconds, emitState);
            }
            else if (_isMoving) BeginSettlement();
            return;
        }

        var changed = !_isMoving;
        _segmentActive = true;
        _isMoving = true;
        if (changed)
        {
            _activityElapsed = 0f;
            _travelWeight = 0f;
            _deferFreshMotionDelta = true;
            _requiresIntermediateFrame = true;
            _deltaCredit = 0f;
            ApplyTravelWeight(0f);
        }
        SetProcess(true);
        var horizontalDelta = _segmentTarget.X - _segmentStart.X;
        if (Math.Abs(horizontalDelta) > .001f)
            EmitSignal(SignalName.HorizontalSegmentStarted, horizontalDelta);
        if (emitState && changed) EmitSignal(SignalName.MotionStateChanged, true);
        EnforceLagBudget();
    }

    private void BeginSettlement()
    {
        _segmentActive = false;
        _settling = true;
        _settleElapsed = 0f;
        _settleDuration = EffectiveSampleSeconds();
        _settleStartWeight = _travelWeight;
    }

    private bool TryCoalesceTail(Vector2 newestDestination, float playbackSeconds)
    {
        var tail = _waypoints[^1];
        var anchor = _waypoints.Count > 1 ? _waypoints[^2].Position : _segmentTarget;
        var first = tail.Position - anchor;
        var second = newestDestination - tail.Position;
        if (first.LengthSquared() <= .000001f || second.LengthSquared() <= .000001f) return false;
        var cross = Math.Abs(first.Cross(second));
        var collinearTolerance = .001f * Math.Max(1f, first.Length() * second.Length());
        if (cross > collinearTolerance || first.Dot(second) <= 0f) return false;
        _waypoints[^1] = new QueuedWaypoint(newestDestination, tail.PlaybackSeconds + playbackSeconds);
        return true;
    }

    private void EnforceLagBudget()
    {
        var currentRemaining = RemainingSegmentSeconds();
        var queuedRemaining = _waypoints.Sum(waypoint => waypoint.PlaybackSeconds);
        var totalRemaining = currentRemaining + queuedRemaining;
        var budget = Math.Max(.01f, MaximumVisualLagSeconds);
        if (totalRemaining <= budget + .000001f) return;
        var scale = budget / totalRemaining;

        if (_segmentActive && currentRemaining > .000001f)
        {
            var progress = SegmentProgress();
            var newRemaining = currentRemaining * scale;
            _segmentDuration = progress >= .999999f
                ? _segmentElapsed
                : Math.Max(.001f, newRemaining / Math.Max(.000001f, 1f - progress));
            _segmentElapsed = progress * _segmentDuration;
        }
        for (var index = 0; index < _waypoints.Count; index++)
            _waypoints[index] = _waypoints[index] with
            {
                PlaybackSeconds = Math.Max(.001f, _waypoints[index].PlaybackSeconds * scale)
            };
    }

    private float EffectiveSampleSeconds() => Math.Max(.005f, AuthoritySampleSecondsAtOneTimes / _speedScale);

    private float RemainingSegmentSeconds() => _segmentActive
        ? Math.Max(0f, _segmentDuration - _segmentElapsed)
        : 0f;

    private float SegmentProgress() => !_segmentActive || _segmentDuration <= 0f
        ? 0f
        : Mathf.Clamp(_segmentElapsed / _segmentDuration, 0f, 1f);

    private void ApplyCurrentPosition()
    {
        if (_target is null) return;
        _target.Position = _segmentStart.Lerp(_segmentTarget, SegmentProgress());
    }

    private void ApplyTravelWeight(float weight)
    {
        _travelWeight = Mathf.Clamp(weight, 0f, 1f);
        EmitSignal(SignalName.TravelWeightChanged, _travelWeight);
    }

    private void CompleteMotion()
    {
        _segmentActive = false;
        _settling = false;
        _isMoving = false;
        _segmentElapsed = 0f;
        _segmentDuration = 0f;
        _settleElapsed = 0f;
        _settleDuration = 0f;
        _activityElapsed = 0f;
        _deltaCredit = 0f;
        _deferFreshMotionDelta = false;
        _requiresIntermediateFrame = false;
        SetProcess(false);
        ApplyTravelWeight(0f);
        EmitSignal(SignalName.MotionStateChanged, false);
    }

    private void ClearActiveMotion()
    {
        var changed = _isMoving;
        _waypoints.Clear();
        _segmentActive = false;
        _settling = false;
        _isMoving = false;
        _segmentElapsed = 0f;
        _segmentDuration = 0f;
        _settleElapsed = 0f;
        _settleDuration = 0f;
        _activityElapsed = 0f;
        _deltaCredit = 0f;
        _deferFreshMotionDelta = false;
        _requiresIntermediateFrame = false;
        SetProcess(false);
        ApplyTravelWeight(0f);
        if (changed) EmitSignal(SignalName.MotionStateChanged, false);
    }

    private readonly record struct QueuedWaypoint(Vector2 Position, float PlaybackSeconds);
}
