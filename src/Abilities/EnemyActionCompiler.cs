using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            case EnemySeatGuardSpec x:
                if (x.GuardCells.Count != 3 || x.GuardCells.Distinct().Count() != 3 ||
                    x.GuardCells.Any(cell => cell.X < 0 || cell.X >= 10 || cell.Y < 0 || cell.Y >= 6))
                    report.Error($"{label}: seat guard requires three distinct battlefield cells.");
                Positive(x.DamageMultiplier, 1);
                return new CompiledEnemySeatGuard(x.GuardCells.ToImmutableArray(), x.DamageMultiplier);
            case EnemyStoneQuakeSpec x:
                Positive(x.Radius); Ticks(x.WindupTicks); Ticks(x.PulseIntervalTicks); Ticks(x.RecoveryTicks);
                if (x.PulseCount != 3) report.Error($"{label}: stone quake requires exactly three pulses.");
                Positive(x.AttackMultiplier); Positive(x.ArmorMultiplier); Positive(x.CompletionArmor); Ticks(x.ArmorDurationTicks);
                return new CompiledEnemyStoneQuake(x.Radius, x.WindupTicks, x.PulseIntervalTicks, x.PulseCount,
                    x.RecoveryTicks, x.AttackMultiplier, x.ArmorMultiplier, x.CompletionArmor, x.ArmorDurationTicks);
            case EnemyOriginalAttackCycleSpec x:
                if (x.Every < 2 || x.Every > 32) report.Error($"{label}: original attack cycle requires a bounded interval of at least two attacks.");
                Ticks(x.WindupTicks); Positive(x.DamageMultiplier, 10);
                return new CompiledEnemyOriginalAttackCycle(x.Every, x.WindupTicks, x.DamageMultiplier);
            case ConeBreathAbilityOperationSpec x:
                Positive(x.Range); Positive(x.AngleDegrees, 180); Ticks(x.WindupTicks); Ticks(x.PulseIntervalTicks);
                Ticks(x.RecoveryTicks); Positive(x.AttackMultiplier); Positive(x.PulseCount, 8); Vfx(x.Vfx);
                if (x.AfterglowDurationTicks != 0)
                {
                    Ticks(x.AfterglowDurationTicks); Ticks(x.AfterglowIntervalTicks); Positive(x.AfterglowAttackMultiplier);
                    if (x.PulseCount != 3 || x.AfterglowDurationTicks % Math.Max(1, x.AfterglowIntervalTicks) != 0 ||
                        x.AfterglowDurationTicks / Math.Max(1, x.AfterglowIntervalTicks) > 12)
                        report.Error($"{label}: afterglow requires a three-pulse breath and a bounded whole number of ticks.");
                }
                else if (x.AfterglowAttackMultiplier != 0)
                    report.Error($"{label}: afterglow damage requires a duration.");
                return new CompiledConeBreath(x.Range,x.AngleDegrees,x.WindupTicks,x.PulseIntervalTicks,x.PulseCount,x.RecoveryTicks,x.AttackMultiplier,x.Vfx,
                    x.AfterglowDurationTicks,x.AfterglowIntervalTicks,x.AfterglowAttackMultiplier);
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
