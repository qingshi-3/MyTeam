using System;
using Godot;

namespace TowerAutobattler.Vfx;

// One authored, transient child effect. This composes visual patterns only;
// it never selects combatants or turns an elapsed visual delay into a hit.
public partial class VfxSubEffectTrack : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSpatialTrack
{
    [Export] public VfxDefinition Definition { get; set; } = null!;
    [Export] public float Delay { get; set; }
    [Export] public bool DelayFollowsCast { get; set; } = true;
    [Export] public Vector2 TargetOffset { get; set; }
    [Export] public float ReferenceRadius { get; set; } = 1.5f;
    // Zero preserves the child's authored body size; positive values assign
    // a share of the parent's logical area to a child blast.
    [Export] public float RadiusRatio { get; set; }

    private VfxPlaybackState? _playback;
    private IVfxStage? _stage;
    private VfxContext _context;
    private VfxInstance? _instance;
    private float _sampledChildAge;
    private bool _finished;
    private bool _ending;

    public void ConfigurePlayback(VfxPlaybackState playback)
    {
        if (_playback is not null)
            throw new InvalidOperationException("A sub-effect track cannot be rebound.");
        if (playback.Parameters.Timing != VfxTimingMode.Automatic)
            throw new InvalidOperationException("Sub-effect patterns require automatic visual timing; actual hits need separate facts.");
        if (Definition?.Scene is null || Definition.Persistent ||
            !float.IsFinite(Delay) || Delay < 0 || !TargetOffset.IsFinite() ||
            !float.IsFinite(ReferenceRadius) || ReferenceRadius <= 0 ||
            !float.IsFinite(RadiusRatio) || RadiusRatio < 0 || RadiusRatio > 1)
            throw new InvalidOperationException("Invalid transient sub-effect definition, delay or logical area.");
        if (!float.IsFinite(Definition.Duration) || !float.IsFinite(Definition.Size) || Definition.Size <= 0 ||
            !float.IsFinite(Definition.ReferenceRadius) || Definition.ReferenceRadius <= 0 ||
            !float.IsFinite(Definition.CastDuration) || Definition.CastDuration < 0 ||
            !float.IsFinite(Definition.FlightDuration) || Definition.FlightDuration < 0 ||
            !float.IsFinite(Definition.ActionSpan) || Definition.ActionSpan < 0 ||
            Definition.Duration <= Definition.CastDuration + Definition.FlightDuration + Definition.ActionSpan)
            throw new InvalidOperationException("Invalid sub-effect size or timeline.");
        _playback = playback;
    }

    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        _context = context;
        _stage = stage;
        // VfxInstance consumes stage coordinates itself. Cancel the parent's
        // size/rotation/position once, instead of projecting a logical offset twice.
        Transform = stageToInstance;
    }

    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        if (_finished) return;
        var playback = _playback ?? throw new InvalidOperationException("Sub-effect playback was not injected.");
        var stage = _stage ?? throw new InvalidOperationException("Sub-effect stage was not injected.");
        if (sustained)
            throw new InvalidOperationException("Sub-effect patterns must have a finite parent lifetime.");

        float delay = Delay / (DelayFollowsCast ? playback.Parameters.CastSpeed : 1);
        float childAge = playback.Age - delay;
        bool cancelled = playback.Cancelled || release > 0;
        if (_instance is null)
        {
            if (cancelled) { _finished = true; return; }
            if (childAge < 0) return;
            StartChild(playback);
        }

        if (cancelled && !_ending)
        {
            _instance!.End(VfxEndReason.Completed);
            _ending = true;
        }
        float step = Mathf.Max(0, childAge - _sampledChildAge);
        _sampledChildAge = Mathf.Max(_sampledChildAge, childAge);
        if (!_instance!.Advance(step, stage, reducedMotion))
        {
            ClearChild();
            _finished = true;
        }
    }

    private void StartChild(VfxPlaybackState playback)
    {
        var root = Definition.Scene.Instantiate();
        try
        {
            if (root is not VfxInstance instance)
                throw new InvalidOperationException("A sub-effect scene must have a VfxInstance root.");
            // Inspect before Bind/AddChild, so a nested/cyclic definition cannot
            // start constructing more instances through lifecycle callbacks.
            RejectNestedTracks(root);
            float radius = _context.Radius > 0 ? _context.Radius : ReferenceRadius;
            var parameters = playback.Parameters with
            {
                Timing = VfxTimingMode.Automatic,
                FlightDuration = null,
            };
            // The source, center and pattern are birth snapshots. The stage can
            // reproject them, but a moving owner cannot drag an already-fired blast.
            var childContext = _context with
            {
                Target = _context.Target + TargetOffset * (radius / ReferenceRadius),
                Radius = radius * RadiusRatio,
                Playback = parameters,
                TravelProgress = null,
                TargetDisplayPosition = null,
            };
            instance.Bind(Definition, childContext);
            AddChild(instance);
            _instance = instance;
        }
        catch
        {
            root.Free();
            throw;
        }
    }

    private static void RejectNestedTracks(Node node)
    {
        if (node is VfxSubEffectTrack)
            throw new InvalidOperationException("Nested sub-effect patterns are unsupported; reference a leaf effect scene.");
        foreach (Node child in node.GetChildren()) RejectNestedTracks(child);
    }

    private void ClearChild()
    {
        if (_instance is not null && GodotObject.IsInstanceValid(_instance)) _instance.Free();
        _instance = null;
    }

    public override void _ExitTree() => ClearChild();
}
