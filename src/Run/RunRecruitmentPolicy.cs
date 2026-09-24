using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

// Ordinary recruitment is unique by content identity, including reserve members.
// Battle summons and an explicitly authored future copy operation are separate contracts.
public static class RunRecruitmentPolicy
{
    public static bool IsOwned(ActiveRunDto run, string contentId) =>
        run.Roster.Any(hero => string.Equals(hero.ContentId, contentId, StringComparison.Ordinal));

    public static bool CanOffer(ActiveRunDto run, CompiledRunChoice choice) =>
        (choice.SuccessChance <= 0 || BranchIsUnique(run, choice.Operations)) &&
        (choice.SuccessChance >= 1 || BranchIsUnique(run, choice.FailureOperations));

    private static bool BranchIsUnique(ActiveRunDto run, ImmutableArray<CompiledRunOperation> operations)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var operation in Recruits(operations))
            if (operation.Amount != 1 || IsOwned(run, operation.ContentId) || !seen.Add(operation.ContentId))
                return false;
        return true;
    }

    // Reserve every reachable recruited identity, not the card's presentation ContentId.
    // Alternative success/failure branches may each grant the same single hero.
    public static bool TrySelect(ActiveRunDto run, CompiledRunChoice choice, ISet<string> selected)
    {
        if (!CanOffer(run, choice)) return false;
        var ids = (choice.SuccessChance > 0 ? Recruits(choice.Operations) : [])
            .Concat(choice.SuccessChance < 1 ? Recruits(choice.FailureOperations) : [])
            .Select(operation => operation.ContentId).Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Any(selected.Contains)) return false;
        foreach (var id in ids) selected.Add(id);
        return true;
    }

    public static PendingRunOffer VisibleOffer(ActiveRunDto run, PendingRunOffer offer)
    {
        var selected = new HashSet<string>(StringComparer.Ordinal);
        var choices = offer.Choices.Where(choice => TrySelect(run, choice, selected)).ToImmutableArray();
        // Exhaustion is a valid empty opportunity: no duplicate filler, no new reward,
        // and no forced-choice softlock. Keep its identity and remaining choices on reload.
        return choices.Length == offer.Choices.Length ? offer : offer with
        {
            Choices = choices,
            AllowSkip = offer.AllowSkip || choices.IsEmpty
        };
    }

    private static IEnumerable<CompiledRunOperation> Recruits(ImmutableArray<CompiledRunOperation> operations) =>
        operations.IsDefaultOrEmpty ? [] : operations.Where(operation => operation.Kind == RunOperationKind.Recruit);
}
