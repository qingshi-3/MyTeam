using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Relics;

namespace TowerAutobattler.Run;

public sealed record RunItemBinding(
    CatalogEntry Entry,
    ItemInstanceState State,
    CompiledRelicDefinition Definition);

public sealed record RunRelicPreparation(
    ModifierSnapshot Modifiers,
    RelicBattlePreparation BattlePreparation);

// Owns the Run-side item scene lifecycle and the only bridge that may prepare or
// validate/apply Battle relic transitions. Battle receives value snapshots only.
public sealed class RunRelicService
{
    public RunRelicService(ContentRegistry content) =>
        ArgumentNullException.ThrowIfNull(content);

    public RunRelicPreparation PrepareBattle(
        RelicRunKey runKey,
        IEnumerable<RunItemBinding> itemBindings)
    {
        ArgumentNullException.ThrowIfNull(itemBindings);
        using var scope = new RelicRunScope(runKey);
        var activeRoots = Activate(scope, itemBindings);
        try
        {
            var preparation = scope.PrepareBattle();
            var relics = preparation.Modifiers;
            return new RunRelicPreparation(
                new ModifierSnapshot(
                    relics.ArmyHealthMultiplier,
                    relics.ArmyDamageMultiplier,
                    relics.HeroHealthMultiplier,
                    relics.HeroDamageMultiplier,
                    relics.ArmyLifeStealBonus,
                    relics.HeroLifeStealBonus,
                    relics.StartBattleShield,
                    relics.EmptySlotPower,
                    relics.SummonToken,
                    0,
                    relics.FormationAdjacentArmor,
                    relics.FormationAdjacentDamageMultiplier,
                    relics.SummonContentId),
                preparation);
        }
        finally
        {
            Deactivate(activeRoots);
            scope.Complete(RelicRunCompletionReason.BattlePrepared);
        }
    }

    public RelicRunApplyResult ValidateTransition(
        RelicRunKey runKey,
        IEnumerable<RunItemBinding> itemBindings,
        RelicBattleTransitionResult transition,
        RelicBattleCompletionReason expectedReason) =>
        WithScope(runKey, itemBindings, scope => scope.Validate(transition, expectedReason),
            RelicRunCompletionReason.BattlePrepared);

    public RelicRunApplyResult ApplyTransition(
        RelicRunKey runKey,
        IEnumerable<RunItemBinding> itemBindings,
        RelicBattleTransitionResult transition) =>
        WithScope(runKey, itemBindings, scope => scope.Apply(transition),
            RelicRunCompletionReason.TransitionApplied);

    private static RelicRunApplyResult WithScope(
        RelicRunKey runKey,
        IEnumerable<RunItemBinding> itemBindings,
        Func<RelicRunScope, RelicRunApplyResult> action,
        RelicRunCompletionReason successReason)
    {
        ArgumentNullException.ThrowIfNull(itemBindings);
        ArgumentNullException.ThrowIfNull(action);
        using var scope = new RelicRunScope(runKey);
        var roots = Activate(scope, itemBindings);
        var completion = RelicRunCompletionReason.Abort;
        try
        {
            var result = action(scope);
            if (result.Succeeded) completion = successReason;
            return result;
        }
        catch
        {
            completion = RelicRunCompletionReason.Exception;
            throw;
        }
        finally
        {
            Deactivate(roots);
            scope.Complete(completion);
        }
    }

    private static List<ItemContentRoot> Activate(
        RelicRunScope scope,
        IEnumerable<RunItemBinding> itemBindings)
    {
        var roots = new List<ItemContentRoot>();
        try
        {
            foreach (var binding in itemBindings)
            {
                var root = binding.Entry.Scene.Instantiate<ItemContentRoot>();
                roots.Add(root);
                root.Bind(binding.State);
                root.Activate(new ItemBindingContext(scope, binding.Definition));
            }
            return roots;
        }
        catch
        {
            Deactivate(roots);
            throw;
        }
    }

    private static void Deactivate(IEnumerable<ItemContentRoot> roots)
    {
        foreach (var root in roots)
        {
            root.Deactivate();
            root.Free();
        }
    }

}
