using System.Collections.Immutable;
namespace TowerAutobattler.Abilities;

// Shared configurations contain no cast progress, spawned bodies or hit history.
public abstract record CompiledEnemyAction : CompiledAbilityOperation
{
    public virtual ImmutableArray<string> ContentDependencies => [];
}
public sealed record CompiledConeBreath(float Range, float AngleDegrees, int WindupTicks,
    int PulseIntervalTicks, int PulseCount, int RecoveryTicks, float AttackMultiplier, string Vfx) : CompiledEnemyAction;
public sealed record CompiledReturningBlade(float Range, float Radius, float Speed, float AttackMultiplier, string Vfx) : CompiledEnemyAction;
public sealed record CompiledPositionSwap(float Range, int PrepareTicks, int RecoveryTicks, string Vfx) : CompiledEnemyAction;
public sealed record CompiledRampart(string WallContentId, int RaiseTicks, int IntervalTicks, int FissureWindupTicks,
    int RecoveryTicks, int WallLifetimeTicks, float Range, float FissureRadius, float AttackMultiplier, string Vfx) : CompiledEnemyAction
{
    public override ImmutableArray<string> ContentDependencies => [WallContentId];
}
public sealed record CompiledBroodPhase(string EggContentId, string LarvaContentId, float HealthThreshold,
    int BreakTicks, float SmallRadius, float RetreatDistance, float RangedReach, int HatchTicks,
    int SpawnCycleTicks, int MaximumLiving, int MaximumBatches, float SwipeMultiplier, string Vfx) : CompiledEnemyAction
{
    public override ImmutableArray<string> ContentDependencies => [EggContentId, LarvaContentId];
}
