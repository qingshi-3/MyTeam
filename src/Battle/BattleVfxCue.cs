namespace TowerAutobattler.Battle;

public enum BattleVfxPhase { Burst, ShieldActive, ShieldImpact, ShieldDepleted,
    SequenceStart, SequenceFired, SequenceProgress, SequenceImpact, SequenceEnd }
// A sequence id identifies one cast/projectile, not just its owner. Facts carry
// resolved presentation rates; the view never guesses them from attack speed.
public sealed record BattleVfxCue(BattleVfxPhase Phase, string EffectId, float Radius = 0,
    float CastSpeed = 1, float MotionSpeed = 1, float? FlightDuration = null,
    string InstanceId = "", float? TravelProgress = null);
