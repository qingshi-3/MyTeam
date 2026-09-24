using System;
using System.Collections.Generic;
using TowerAutobattler.Content;
namespace TowerAutobattler.Abilities;
public static partial class AbilityDefinitionCompiler
{
    private static CompiledEnemyAction? CompileEnemyAction(AbilityOperationSpec authored, string label, ValidationReport report)
    {
        void Positive(float v, float max = 64) { if (!float.IsFinite(v) || v <= 0 || v > max) report.Error($"{label}: invalid enemy action magnitude."); }
        void Ticks(int v) { if (v < 1 || v > 600) report.Error($"{label}: invalid enemy action duration."); }
        void Vfx(string id) { if (string.IsNullOrWhiteSpace(id)) report.Error($"{label}: missing action VFX."); }
        switch (authored)
        {
            case ConeBreathAbilityOperationSpec x:
                Positive(x.Range); Positive(x.AngleDegrees, 180); Ticks(x.WindupTicks); Ticks(x.PulseIntervalTicks);
                Ticks(x.RecoveryTicks); Positive(x.AttackMultiplier); Positive(x.PulseCount, 8); Vfx(x.Vfx);
                return new CompiledConeBreath(x.Range,x.AngleDegrees,x.WindupTicks,x.PulseIntervalTicks,x.PulseCount,x.RecoveryTicks,x.AttackMultiplier,x.Vfx);
            case ReturningBladeAbilityOperationSpec x:
                Positive(x.Range); Positive(x.Radius, 1); Positive(x.Speed, 100); Positive(x.AttackMultiplier); Vfx(x.Vfx);
                return new CompiledReturningBlade(x.Range,x.Radius,x.Speed,x.AttackMultiplier,x.Vfx);
            case PositionSwapAbilityOperationSpec x:
                Positive(x.Range); Ticks(x.PrepareTicks); Ticks(x.RecoveryTicks); Vfx(x.Vfx);
                return new CompiledPositionSwap(x.Range,x.PrepareTicks,x.RecoveryTicks,x.Vfx);
            case RampartAbilityOperationSpec x:
                Vfx(x.WallContentId); Ticks(x.RaiseTicks); Ticks(x.IntervalTicks); Ticks(x.FissureWindupTicks);
                Ticks(x.RecoveryTicks); Ticks(x.WallLifetimeTicks); Positive(x.Range); Positive(x.FissureRadius,1); Positive(x.AttackMultiplier); Vfx(x.Vfx);
                return new CompiledRampart(x.WallContentId,x.RaiseTicks,x.IntervalTicks,x.FissureWindupTicks,x.RecoveryTicks,x.WallLifetimeTicks,x.Range,x.FissureRadius,x.AttackMultiplier,x.Vfx);
            case BroodPhaseAbilityOperationSpec x:
                Vfx(x.EggContentId); Vfx(x.LarvaContentId); Positive(x.HealthThreshold, .99f); Ticks(x.BreakTicks);
                Positive(x.SmallRadius, 1.5f); Positive(x.RetreatDistance); Positive(x.RangedReach); Ticks(x.HatchTicks);
                Ticks(x.SpawnCycleTicks); Positive(x.MaximumLiving,8); Positive(x.MaximumBatches,8); Positive(x.SwipeMultiplier); Vfx(x.Vfx);
                return new CompiledBroodPhase(x.EggContentId,x.LarvaContentId,x.HealthThreshold,x.BreakTicks,x.SmallRadius,
                    x.RetreatDistance,x.RangedReach,x.HatchTicks,x.SpawnCycleTicks,x.MaximumLiving,x.MaximumBatches,x.SwipeMultiplier,x.Vfx);
        }
        return null;
    }

    public static IEnumerable<string> EnemyActionDependencies(AbilityOperationSpec operation) => operation switch
    {
        RampartAbilityOperationSpec x => [x.WallContentId],
        BroodPhaseAbilityOperationSpec x => [x.EggContentId,x.LarvaContentId],
        _ => []
    };
}
