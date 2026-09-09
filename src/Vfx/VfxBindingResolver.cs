using TowerAutobattler.Battle;
using System;

namespace TowerAutobattler.Vfx;

public static class VfxBindingResolver
{
    public static void Present(VfxPlayer player, BattleVfxCue cue, VfxContext context, string owner)
    {
        var key = "shield:" + owner;
        bool sequence = cue.Phase >= BattleVfxPhase.SequenceStart;
        if (sequence && string.IsNullOrWhiteSpace(cue.InstanceId))
            throw new ArgumentException("A sequence cue requires a unique cast/projectile instance id.", nameof(cue));
        var sequenceKey = "sequence:" + cue.InstanceId;
        var parameters = context.Playback ?? VfxPlaybackParameters.Default;
        context = context with { Playback = parameters with { CastSpeed = cue.CastSpeed, MotionSpeed = cue.MotionSpeed,
            FlightDuration = cue.FlightDuration, Timing = sequence ? VfxTimingMode.Events : VfxTimingMode.Automatic },
            TravelProgress = cue.TravelProgress };
        switch (cue.Phase)
        {
            case BattleVfxPhase.Burst:
                player.Play(cue.EffectId, context, string.IsNullOrEmpty(cue.InstanceId) ? null : cue.InstanceId); break;
            case BattleVfxPhase.ShieldActive: player.Play(cue.EffectId, context, key); break;
            case BattleVfxPhase.ShieldImpact: player.Impact(key); break;
            case BattleVfxPhase.ShieldDepleted: player.End(key, VfxEndReason.Depleted); break;
            case BattleVfxPhase.SequenceStart: player.Play(cue.EffectId, context, sequenceKey); break;
            case BattleVfxPhase.SequenceFired:
                player.UpdateContext(sequenceKey, context); player.Signal(sequenceKey, VfxStartCue.Fired); break;
            case BattleVfxPhase.SequenceProgress: player.UpdateContext(sequenceKey, context); break;
            case BattleVfxPhase.SequenceImpact:
                player.UpdateContext(sequenceKey, context); player.Signal(sequenceKey, VfxStartCue.Impact); break;
            case BattleVfxPhase.SequenceEnd: player.End(sequenceKey, VfxEndReason.Completed); break;
        }
    }
}
