using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Content;

namespace TowerAutobattler.Abilities;

public static partial class AbilityDefinitionCompiler
{
    private static void ValidateRememberedAbility(AbilityDefinition authored,
        IEnumerable<CompiledAbilityOperation> operations, string label, ValidationReport report)
    {
        var roots = operations.OfType<CompiledMatrixOperation>().ToArray();
        var all = roots.SelectMany(MatrixAbilityCompiler.Flatten).ToArray();
        var grants = all.Where(op => op.Kind == MatrixOperationKind.GrantRememberableAttribute).ToArray();
        foreach(var group in grants.GroupBy(op => op.Key, StringComparer.Ordinal))
        {
            var pair = group.ToArray();
            var transforms = all.Where(op => op.Kind == MatrixOperationKind.Transform &&
                op.Effects.Any(child => child.Kind == MatrixOperationKind.GrantRememberableAttribute && child.Key == group.Key)).ToArray();
            if(pair.Length != 2 || all.Count(op => op.Key == group.Key) != 2 || pair.Count(op => op.Attribute == CombatAttribute.AttackDamage) != 1 ||
                pair.Count(op => op.Attribute == CombatAttribute.MaxHealth) != 1 || transforms.Length != 1 ||
                transforms[0].Effects.Count(child => child.Kind == MatrixOperationKind.GrantRememberableAttribute && child.Key == group.Key) != 2)
                report.Error($"{label}: remembered group '{group.Key}' requires exactly one fixed AD/HP pair under one Transform.");
        }
        if(grants.Length > 0 && (authored.ActivationKind != AbilityActivationKind.Automatic ||
            authored.Trigger != AbilityTriggerKind.ManaFull || authored.MaxUses != 1 || authored.Echoable))
            report.Error($"{label}: remembered grants require a non-echoed, once-per-battle mana-full transform.");
        var metadata = all.Where(op => op.Kind is MatrixOperationKind.RememberAllyFixedGains or MatrixOperationKind.BindOpeningAllies).ToArray();
        if(metadata.Length > 0 && (authored.ActivationKind != AbilityActivationKind.Triggered ||
            authored.Trigger != AbilityTriggerKind.OwnerEnteredBattle || authored.MaxUses != 1 || authored.Echoable ||
            metadata.Length != roots.Length || all.Length != roots.Length))
            report.Error($"{label}: remembered metadata requires sole root entry operations, once per body and no echo.");
        if(metadata.GroupBy(op => op.Key,StringComparer.Ordinal).Any(group => group.Count() != 1))
            report.Error($"{label}: remembered metadata keys must be unique in an ability.");
    }

    private static void ValidateRememberedLoadout(IEnumerable<CompiledAbilityDefinition> abilities, ValidationReport report)
    {
        var all = abilities.SelectMany(ability => ability.Operations.OfType<CompiledMatrixOperation>()
            .SelectMany(MatrixAbilityCompiler.Flatten)).ToArray();
        if(all.Count(op => op.Kind == MatrixOperationKind.RememberAllyFixedGains) > 1)
            report.Error("A loadout may declare only one fixed-ally-gain remember marker.");
        var bindings = all.Where(op => op.Kind == MatrixOperationKind.BindOpeningAllies).ToArray();
        if(bindings.GroupBy(op => op.Key,StringComparer.Ordinal).Any(group => group.Count() != 1))
            report.Error("Opening-ally binding keys must be unique in a loadout.");
        foreach(var key in all.Where(op => op.Kind == MatrixOperationKind.GrantRememberableAttribute).Select(op => op.Key).Distinct(StringComparer.Ordinal))
            if(bindings.Count(op => op.Key == key) != 1)
                report.Error($"Remembered grant '{key}' requires its matching opening-ally binding.");
    }
}
