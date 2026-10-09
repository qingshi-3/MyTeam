using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;

namespace TowerAutobattler.Run;

public sealed partial class RunRelicRules
{
    public static List<RelicRunRuleStateDto> InitialRunRuleStates(ActiveRunDto run, CompiledRelicDefinition definition) =>
        (definition.RunRules.IsDefault ? [] : definition.RunRules)
        .Where(rule => rule.Kind == RelicRunRuleKind.VictoryNativeKillReward)
        .Select(rule => new RelicRunRuleStateDto { RuleId = rule.RuleId,
            GrantedAtSequence = run.Action?.Source.Sequence ?? run.NextActionSequence - 1 }).ToList();

    private static int BountyDraws(ActiveRunDto run, ItemInstanceDto item, CompiledRelicRunRule rule,
        BattleResult result, EncounterPlan encounter)
    {
        if (result.Outcome != BattleOutcome.PlayerVictory || run.Expedition is not { } map || encounter.IsBoss ||
            encounter.ExpeditionBoss || encounter.NodeType is not (TowerNodeType.Combat or TowerNodeType.Elite)) return 0;
        if (run.Action is not { Phase: RunActionPhase.Battle, IsStageBoss: false } action || rule.Limit <= 0 ||
            action.Source.SiteId != encounter.ExpeditionSiteId || item.RunRuleStates is null) return -1;
        var state = item.RunRuleStates.SingleOrDefault(value => value.RuleId == rule.RuleId);
        if (state is null) return -1;
        // Acquisition during this clear cannot turn its own kills into retroactive rewards.
        if (action.Source.Sequence <= state.GrantedAtSequence || state.ClearedSites.Any(site => site.SiteId == action.Source.SiteId)) return 0;
        var site = map.Sites.SingleOrDefault(value => value.SiteId == action.Source.SiteId);
        if (site is null || site.Cleared || result.Units.IsDefault || result.Units.Any(unit => unit is null)) return -1;
        var kills = 0;
        for (var index = 0; index < encounter.EnemyIds.Count; index++)
        {
            // Only identities in the frozen original composition can earn credit.
            // Summons, copies and later entrants never inherit an enemy-N identity.
            var reports = result.Units.Where(unit => unit.SourceInstanceId == $"enemy-{index}").ToArray();
            if (reports.Length != 1 || reports[0].Team != 1 || reports[0].IsTemporary || reports[0].JoinTick != 0 ||
                reports[0].ContentId != encounter.EnemyIds[index]) return -1;
            if (!reports[0].Alive && reports[0].DefeatTick is { } tick && tick >= 0 && tick <= result.Ticks) kills++;
        }
        var total = checked(state.Remainder + kills);
        var draws = checked(total / rule.Limit * (int)rule.Amount * item.Stacks);
        state.Remainder = total % rule.Limit;
        state.ClearedSites.Add(new RelicClearedSiteDto { SiteId = site.SiteId, ActionId = action.Source.ActionId,
            Sequence = action.Source.Sequence, NativeKills = kills });
        return draws;
    }

    public static bool ValidateRunRuleStates(ActiveRunDto run, ItemInstanceDto item, CompiledRelicDefinition definition,
        CompiledGameProject project)
    {
        var rules = (definition.RunRules.IsDefault ? [] : definition.RunRules)
            .Where(rule => rule.Kind == RelicRunRuleKind.VictoryNativeKillReward).ToArray();
        if (item.RunRuleStates is null || item.RunRuleStates.Count != rules.Length ||
            item.RunRuleStates.Any(state => state is null || state.RuleId is null) ||
            item.RunRuleStates.Select(state => state.RuleId).Distinct(StringComparer.Ordinal).Count() != rules.Length) return false;
        foreach (var state in item.RunRuleStates)
        {
            var rule = rules.SingleOrDefault(value => value.RuleId == state.RuleId);
            if (rule is null || rule.Limit <= 0 || state.GrantedAtSequence < 0 || state.GrantedAtSequence >= run.NextActionSequence ||
                state.Remainder < 0 || state.Remainder >= rule.Limit || state.ClearedSites is null ||
                state.ClearedSites.Any(site => site is null) ||
                state.ClearedSites.Select(site => site.SiteId).Distinct(StringComparer.Ordinal).Count() != state.ClearedSites.Count) return false;
            long sum = 0;
            var previous = state.GrantedAtSequence;
            foreach (var receipt in state.ClearedSites)
            {
                if (run.Expedition is not { } map || project.Campaign.Progression is not CompiledExpedition expedition ||
                    receipt.Sequence <= previous || receipt.Sequence >= run.NextActionSequence ||
                    string.IsNullOrWhiteSpace(receipt.ActionId) || receipt.ActionId != $"{run.RunId}:action:{receipt.Sequence}" ||
                    !run.CompletedActionIds.Contains(receipt.ActionId, StringComparer.Ordinal) && run.Action?.Source.ActionId != receipt.ActionId)
                    return false;
                var site = map.Sites.SingleOrDefault(value => value.SiteId == receipt.SiteId);
                if (site is not { Cleared: true } || !expedition.Encounters.TryGetValue(site.EncounterProfileId, out var profile) ||
                    profile.Encounter.NodeType is not (TowerNodeType.Combat or TowerNodeType.Elite) ||
                    receipt.NativeKills < 0 || receipt.NativeKills > profile.Composition.EnemyIds.Length) return false;
                sum += receipt.NativeKills;
                previous = receipt.Sequence;
            }
            if (state.Remainder != sum % rule.Limit) return false;
        }
        return true;
    }
}
