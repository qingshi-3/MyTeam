namespace TowerAutobattler.Battle;

public enum BattleVfxPhase { Burst, ShieldActive, ShieldImpact, ShieldDepleted }
public sealed record BattleVfxCue(BattleVfxPhase Phase, string EffectId, float Radius = 0);
