using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

// One fixed attribute receipt; Run requires the complete same-tick AD/HP pair.
public sealed record BattleRememberedGain(
    string SourceInstanceId, string TargetInstanceId, string AbilityId, string OperationKey,
    string RememberAbilityId, string RememberOperationKey, CombatAttribute Attribute, float Amount,
    int GrantOrdinal, int GrantedTick);

public sealed record BattleOpeningAllyLink(string SourceRuntimeId, string SourceInstanceId,
    string TargetRuntimeId, string TargetInstanceId, string GrantKey);
