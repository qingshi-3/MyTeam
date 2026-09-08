using TowerAutobattler.Battle;

namespace TowerAutobattler.Vfx;

public static class VfxBindingResolver
{
    public static void Present(VfxPlayer player, BattleVfxCue cue, VfxContext context, string owner)
    {
        var key = "shield:" + owner;
        switch (cue.Phase)
        {
            case BattleVfxPhase.Burst: player.Play(cue.EffectId, context); break;
            case BattleVfxPhase.ShieldActive: player.Play(cue.EffectId, context, key); break;
            case BattleVfxPhase.ShieldImpact: player.Impact(key); break;
            case BattleVfxPhase.ShieldDepleted: player.End(key, VfxEndReason.Depleted); break;
        }
    }
}
