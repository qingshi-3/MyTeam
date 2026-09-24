namespace TowerAutobattler.Abilities;

public sealed record CompiledTrampleOperation(float Range, float Distance, float Speed,
    int ChargeTicks, int RecoveryTicks, float SideDistance, float AttackMultiplier,
    string WarningVfx, string RushVfx) : CompiledAbilityOperation;
