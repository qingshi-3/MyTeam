using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;

namespace TowerAutobattler.Run;

// Ordinary relic supply is unique; equipment copies and explicitly repeatable
// relics retain their authored acquisition rule. Historical owned copies remain.
public static class RunItemSupplyPolicy
{
    public static bool CanAcquire(ActiveRunDto run, string contentId, ContentRegistry content) =>
        content.TryGet(contentId, out var entry) && entry.Definition is ItemDefinition item &&
        (item.ProductKind != ItemProductKind.Relic || item.AllowRepeatAcquisition ||
            !run.Items.Any(owned => owned.ContentId == contentId)) && HasFutureEffect(run, item, content);

    private static bool HasFutureEffect(ActiveRunDto run, ItemDefinition item, ContentRegistry content)
    {
        if (item.ProductKind != ItemProductKind.Relic) return true;
        var definition = content.Graph.ResolveRelic(item.Id);
        return definition.RunRules.IsDefaultOrEmpty || definition.RunRules.Any(rule =>
            rule.Kind != RelicRunRuleKind.VictoryInterest || run.BattleNumber < rule.Limit) ||
            !definition.AttributeBindings.IsEmpty || !definition.BattleStartEffects.IsEmpty || !definition.BattleModifiers.IsEmpty ||
            !definition.StatusGrants.IsDefaultOrEmpty || !definition.AbilityRepeats.IsDefaultOrEmpty || !definition.VictoryOutcomes.IsEmpty;
    }

    public static bool CanOffer(ActiveRunDto run, CompiledRunChoice choice, ContentRegistry content) =>
        (choice.SuccessChance <= 0 || BranchIsUnique(run, choice.Operations, content)) &&
        (choice.SuccessChance >= 1 || BranchIsUnique(run, choice.FailureOperations, content));

    public static PendingRunOffer VisibleOffer(ActiveRunDto run, PendingRunOffer offer, ContentRegistry content)
    {
        var choices = offer.Choices.Where(choice => CanOffer(run, choice, content)).ToImmutableArray();
        return choices.Length == offer.Choices.Length ? offer : offer with
        {
            Choices = choices,
            AllowSkip = offer.AllowSkip || choices.IsEmpty
        };
    }

    private static bool BranchIsUnique(ActiveRunDto run, ImmutableArray<CompiledRunOperation> operations,
        ContentRegistry content)
    {
        if (operations.IsDefaultOrEmpty) return true;
        var uniqueRelics = new HashSet<string>(StringComparer.Ordinal);
        foreach (var operation in operations.Where(value => value.Kind == RunOperationKind.GrantItem))
        {
            if (!content.TryGet(operation.ContentId, out var entry) || entry.Definition is not ItemDefinition item) return false;
            if (item.ProductKind != ItemProductKind.Relic || item.AllowRepeatAcquisition) continue;
            if (operation.Amount != 1 || !CanAcquire(run, operation.ContentId, content) || !uniqueRelics.Add(operation.ContentId)) return false;
        }
        return true;
    }
}
