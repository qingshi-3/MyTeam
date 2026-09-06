using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.Project;

public static partial class GameProjectCompiler
{
    private static ImmutableDictionary<RunOfferKind, CompiledRunOffer> CompileRunOffers(
        CampaignDefinition authored, CompilationContext context)
    {
        var result = ImmutableDictionary.CreateBuilder<RunOfferKind, CompiledRunOffer>();
        foreach (var offer in authored.RunOffers ?? [])
        {
            if (offer is null) { context.Report.Error($"{Source(authored)}: null RunOffer."); continue; }
            var source = Source(offer);
            context.RegisterStableId(offer.StableId, offer);
            if (!Enum.IsDefined(offer.Kind) || !Enum.IsDefined(offer.PoolAction) || string.IsNullOrWhiteSpace(offer.DisplayName) ||
                offer.Repeatable && offer.Kind != RunOfferKind.Shop || offer.Kind == RunOfferKind.Shop && !offer.AllowSkip)
                context.Report.Error($"{source}: invalid offer kind/title/repeat policy.");
            CompiledContentPool? pool = null;
            if (offer.Pool is not null)
            {
                pool = context.CompilePool(offer.Pool, offer.PoolAction == RunPoolAction.Recruit ? ContentPoolKind.Soldier : ContentPoolKind.Item, source);
                if (offer.PoolChoiceCount <= 0) context.Report.Error($"{source}: PoolChoiceCount must be positive.");
            }
            var choices = ImmutableArray.CreateBuilder<CompiledRunChoice>();
            foreach (var choice in offer.Choices ?? [])
            {
                if (choice is null) { context.Report.Error($"{source}: null choice."); continue; }
                var choiceSource = Source(choice);
                context.RegisterStableId(choice.StableId, choice);
                if (string.IsNullOrWhiteSpace(choice.DisplayName) || !float.IsFinite(choice.SuccessChance) ||
                    choice.SuccessChance < 0 || choice.SuccessChance > 1)
                    context.Report.Error($"{choiceSource}: invalid title/chance.");
                var conditions = (choice.Conditions ?? []).Select((condition, index) =>
                {
                    if (condition is null) { context.Report.Error($"{choiceSource}: condition[{index}] is null."); return null!; }
                    var compiled = new CompiledRunCondition(condition.Kind, condition.Amount, condition.Ratio, condition.ContentId);
                    if (!RunDecisionValidation.ValidCondition(compiled, id => context.HasContent(id)))
                        context.Report.Error($"{choiceSource}: condition[{index}] is invalid or references missing content.");
                    return compiled;
                }).ToImmutableArray();
                ImmutableArray<CompiledRunOperation> Operations(RunOperationDefinition[]? values, bool cost, string label) =>
                    (values ?? []).Select((operation, index) =>
                    {
                        if (operation is null) { context.Report.Error($"{choiceSource}: {label}[{index}] is null."); return null!; }
                        var compiled = new CompiledRunOperation(operation.Kind, operation.Amount, operation.Ratio,
                            operation.ContentId, operation.SourceId, operation.Target, operation.MinimumHealthRatio);
                        if (!RunDecisionValidation.ValidOperation(compiled, context.IsRecruitable, context.IsItem, cost))
                            context.Report.Error($"{choiceSource}: {label}[{index}] is invalid or unsupported.");
                        return compiled;
                    }).ToImmutableArray();
                choices.Add(new CompiledRunChoice(choice.StableId, choice.DisplayName, choice.Description, conditions,
                    Operations(choice.Costs, true, "cost"), Operations(choice.Operations, false, "operation"), choice.SuccessChance,
                    Operations(choice.FailureOperations, false, "failure")));
            }
            var poolChoices = pool is null ? [] : CompilePoolChoices(pool, offer.PoolAction, context);
            if (choices.Count == 0 && poolChoices.Length == 0) context.Report.Error($"{source}: offer has no executable choices.");
            if (choices.Select(choice => choice.StableId).Concat(poolChoices.Select(choice => choice.StableId)).Distinct().Count() != choices.Count + poolChoices.Length)
                context.Report.Error($"{source}: duplicate choice identity.");
            var compiledOffer = new CompiledRunOffer(offer.StableId, offer.Kind, offer.DisplayName, offer.AllowSkip,
                offer.Repeatable, pool, offer.PoolAction, offer.PoolChoiceCount, choices.ToImmutable()) { PoolChoices = poolChoices };
            if (result.ContainsKey(offer.Kind)) context.Report.Error($"{source}: duplicate offer binding for {offer.Kind}.");
            else result.Add(offer.Kind, compiledOffer);
        }
        return result.ToImmutable();
    }

    private static ImmutableArray<CompiledRunChoice> CompilePoolChoices(CompiledContentPool pool, RunPoolAction action, CompilationContext context) =>
        pool.ContentIds.Select(id =>
        {
            var definition = context.Entry(id).Definition;
            var name = definition is UnitDefinition unit ? unit.DisplayName : ((ItemDefinition)definition).DisplayName;
            var description = definition is UnitDefinition unitDefinition ? unitDefinition.Description : ((ItemDefinition)definition).Description;
            var costs = action == RunPoolAction.BuyItem
                ? ImmutableArray.Create(new CompiledRunOperation(RunOperationKind.SpendGold, ((ItemDefinition)definition).Price))
                : ImmutableArray<CompiledRunOperation>.Empty;
            return new CompiledRunChoice(id, name, description, [], costs,
                [new CompiledRunOperation(action == RunPoolAction.Recruit ? RunOperationKind.Recruit : RunOperationKind.GrantItem, 1, ContentId: id)],
                FailureOperations: [], ContentId: id);
        }).ToImmutableArray();
}
