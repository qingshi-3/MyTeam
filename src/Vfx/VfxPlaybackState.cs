using System;

namespace TowerAutobattler.Vfx;

public enum VfxTimingMode { Automatic, Events }
public enum VfxStartCue { Play, Fired, Impact }
public enum VfxClockDomain { Realtime, Cast, Motion }

// Immutable invocation data. Hero action speed is a snapshot for this cast;
// it never changes a shared Resource or the owning player's global clock.
public sealed record VfxPlaybackParameters(float CastSpeed = 1, float MotionSpeed = 1,
    float? FlightDuration = null, float ParticleScale = 1, float Density = 1,
    float WidthScale = 1, VfxTimingMode Timing = VfxTimingMode.Automatic)
{
    public static VfxPlaybackParameters Default { get; } = new();
    public void Validate()
    {
        static void Check(float n, float low, float high)
        { if (!float.IsFinite(n) || n < low || n > high) throw new ArgumentOutOfRangeException(nameof(n), "Invalid VFX playback parameter."); }
        Check(CastSpeed, .1f, 8); Check(MotionSpeed, .1f, 8);
        Check(ParticleScale, .25f, 4); Check(Density, .25f, 4); Check(WidthScale, .25f, 4);
        if (FlightDuration is { } duration) Check(duration, .02f, 60);
        if (!Enum.IsDefined(Timing)) throw new ArgumentOutOfRangeException(nameof(Timing));
    }
}

public sealed class VfxPlaybackState
{
    private readonly VfxDefinition _definition;
    public VfxPlaybackParameters Parameters { get; }
    public float Age { get; private set; }
    public float? FiredAt { get; private set; }
    public float? ImpactAt { get; private set; }
    public bool Cancelled { get; private set; }
    public float RadiusScale { get; private set; }
    public float DensityAtBirth { get; }
    public float? TravelProgress { get; private set; }
    public float FlightDuration => Parameters.FlightDuration ?? _definition.FlightDuration;
    public float CastDuration => _definition.CastDuration / Parameters.CastSpeed;
    public float ExpectedDuration => _definition.Duration
        + (_definition.CastDuration + _definition.ActionSpan) * (1 / Parameters.CastSpeed - 1)
        + (_definition.FlightDuration > 0 ? FlightDuration - _definition.FlightDuration : 0);
    public float ParticleScale => Parameters.ParticleScale / RadiusScale;
    public bool HasSequence => _definition.CastDuration > 0 || _definition.FlightDuration > 0;
    public VfxPlaybackState(VfxDefinition definition, VfxContext context)
    {
        _definition = definition;
        Parameters = context.Playback ?? VfxPlaybackParameters.Default;
        Parameters.Validate();
        UpdateContext(context);
        DensityAtBirth = Parameters.Density * RadiusScale * RadiusScale;
        if (Parameters.Timing == VfxTimingMode.Events && !HasSequence)
            throw new ArgumentException("Event timing requires an authored cast/flight sequence.");
        Advance(0);
    }
    public void UpdateContext(VfxContext context)
    {
        if (!float.IsFinite(context.Radius) || context.Radius < 0 ||
            context.TravelProgress is { } p && (!float.IsFinite(p) || p < 0 || p > 1))
            throw new ArgumentOutOfRangeException(nameof(context));
        if (!context.Source.IsFinite() || !context.Target.IsFinite() ||
            context.Direction is { } direction && (!direction.IsFinite() || direction.LengthSquared() < .000001f))
            throw new ArgumentOutOfRangeException(nameof(context));
        RadiusScale = (_definition.Ground || _definition.UsesRadius) && context.Radius > 0 ? context.Radius / _definition.ReferenceRadius : 1;
        TravelProgress = context.TravelProgress;
    }
    public void Advance(float seconds)
    {
        Age += seconds;
        if (Cancelled || Parameters.Timing == VfxTimingMode.Events) return;
        if (Age >= CastDuration) FiredAt ??= CastDuration;
        if (Age >= CastDuration + FlightDuration) ImpactAt ??= CastDuration + FlightDuration;
    }
    public void Signal(VfxStartCue cue)
    {
        if (Cancelled || Parameters.Timing != VfxTimingMode.Events) return;
        if (cue == VfxStartCue.Fired && ImpactAt is null) FiredAt ??= Age;
        if (cue == VfxStartCue.Impact) { FiredAt ??= Age; ImpactAt ??= Age; }
    }
    public void Cancel() => Cancelled = true;
    public float Since(VfxStartCue cue) => cue switch
    {
        VfxStartCue.Play => Age,
        VfxStartCue.Fired => FiredAt is { } fired ? Age - fired : -1,
        VfxStartCue.Impact => ImpactAt is { } impact ? Age - impact : -1,
        _ => throw new ArgumentOutOfRangeException(nameof(cue))
    };
    public bool Finished => !_definition.Persistent && (Parameters.Timing == VfxTimingMode.Events
        ? ImpactAt is { } impact && Age - impact >= _definition.Duration - _definition.CastDuration - _definition.FlightDuration
        : Age >= ExpectedDuration);
}

public interface IVfxPlaybackTrack
{
    void ConfigurePlayback(VfxPlaybackState playback);
}
