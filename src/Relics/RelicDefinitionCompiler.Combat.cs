using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using TowerAutobattler.Content;

namespace TowerAutobattler.Relics;

public static partial class RelicDefinitionCompiler
{
    private static ImmutableArray<CompiledRelicCombatRule> CompileCombatRules(RelicCombatRuleSpec[]? rules,
        ISet<string> ids, string label, ValidationReport report)
    {
        var result = ImmutableArray.CreateBuilder<CompiledRelicCombatRule>();
        if (rules is null) { report.Error($"{label}: combat rule collection is missing."); return []; }
        foreach (var rule in rules)
        {
            if (rule is null || string.IsNullOrWhiteSpace(rule.RuleId) || !StableIdPattern.IsMatch(rule.RuleId) ||
                !ids.Add(rule.RuleId) || !Enum.IsDefined(rule.Kind) || !float.IsFinite(rule.Amount) ||
                !float.IsFinite(rule.Threshold) || rule.Amount <= 0 || rule.Amount > 10000)
            { report.Error($"{label}: invalid or duplicate combat rule."); continue; }
            var valid = rule.Kind switch
            {
                RelicCombatRuleKind.DamageAboveHealthRatio => rule.Amount <= 10 && rule.Threshold is >= 0 and < 1 && rule.Tick == 0,
                RelicCombatRuleKind.DamageWithinBodyDistance => rule.Amount <= 10 && rule.Threshold is >= 0 and <= 30 && rule.Tick == 0,
                RelicCombatRuleKind.DamageAgainstBoss => rule.Amount <= 10 && rule.Threshold == 0 && rule.Tick == 0,
                RelicCombatRuleKind.HealOnAttackCritical => rule.Threshold == 0 && rule.Tick == 0,
                RelicCombatRuleKind.HealMissingHealthAtTick => rule.Amount <= 1 && rule.Threshold == 0 && rule.Tick is > 0 and <= 3600,
                _ => false
            };
            if (!valid) report.Error($"{label}: invalid parameters for combat rule '{rule.RuleId}'.");
            else result.Add(new(rule.RuleId, rule.Kind, rule.Amount, rule.Threshold, rule.Tick));
        }
        return result.ToImmutable();
    }
}
