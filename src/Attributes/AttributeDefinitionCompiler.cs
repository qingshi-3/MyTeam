using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Content;

namespace TowerAutobattler.Attributes;

public sealed record AttributeSetCompilationResult(CompiledAttributeSetDefinition? Definition, ValidationReport Report);
public sealed record AttributeModifierCompilationResult(CompiledAttributeModifier? Modifier, ValidationReport Report);

public static class AttributeDefinitionCompiler
{
    public static AttributeSetCompilationResult Compile(AttributeSetDefinition? authored)
    {
        var report = new ValidationReport();
        if (authored is null)
        {
            report.Error("Attribute set definition is missing.");
            return new AttributeSetCompilationResult(null, report);
        }
        var compiled = ImmutableArray.CreateBuilder<CompiledAttributeDefinition>();
        var ids = new HashSet<CombatAttribute>();
        foreach (var item in authored.Attributes)
        {
            if (item is null)
            {
                report.Error("Attribute definition entry is missing.");
                continue;
            }
            if (!Enum.IsDefined(item.Attribute)) report.Error("Attribute id is invalid.");
            if (!ids.Add(item.Attribute)) report.Error($"Duplicate attribute: {item.Attribute}.");
            if (!float.IsFinite(item.BaseValue) || !float.IsFinite(item.Minimum) || !float.IsFinite(item.Maximum))
                report.Error($"Attribute '{item.Attribute}' values must be finite.");
            if (item.Minimum > item.Maximum)
                report.Error($"Attribute '{item.Attribute}' minimum exceeds maximum.");
            if (item.BaseValue < item.Minimum || item.BaseValue > item.Maximum)
                report.Error($"Attribute '{item.Attribute}' base value is outside its clamp.");
            compiled.Add(new CompiledAttributeDefinition(item.Attribute, item.BaseValue, item.Minimum, item.Maximum));
        }
        foreach (var required in Enum.GetValues<CombatAttribute>())
            if (!ids.Contains(required)) report.Error($"Attribute set is missing required attribute: {required}.");
        if (compiled.Count == 0) report.Error("Attribute set must define at least one attribute.");
        if (report.HasCoreErrors) return new AttributeSetCompilationResult(null, report);
        var ordered = compiled.OrderBy(item => item.Attribute).ToImmutableArray();
        return new AttributeSetCompilationResult(new CompiledAttributeSetDefinition(ordered, Fingerprint(ordered)), report);
    }

    public static AttributeModifierCompilationResult Compile(AttributeModifierSpec? authored)
    {
        var report = new ValidationReport();
        if (authored is null)
        {
            report.Error("Attribute modifier is missing.");
            return new AttributeModifierCompilationResult(null, report);
        }
        if (!Enum.IsDefined(authored.Attribute)) report.Error("Modifier attribute is invalid.");
        if (!Enum.IsDefined(authored.Operation)) report.Error("Modifier operation is invalid.");
        if (string.IsNullOrWhiteSpace(authored.SlotId)) report.Error("Modifier slot id is required.");
        var magnitude = CompileMagnitude(authored.Magnitude, report);
        return report.HasCoreErrors || magnitude is null
            ? new AttributeModifierCompilationResult(null, report)
            : new AttributeModifierCompilationResult(
                new CompiledAttributeModifier(authored.Attribute, authored.Operation, magnitude, authored.Priority, authored.SlotId), report);
    }

    public static CompiledAttributeSetDefinition Legacy(IReadOnlyDictionary<CombatAttribute, float> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var definitions = Enum.GetValues<CombatAttribute>().Select(attribute =>
        {
            var value = values.GetValueOrDefault(attribute, Default(attribute));
            var (minimum, maximum) = LegacyClamp(attribute);
            return new CompiledAttributeDefinition(attribute, Math.Clamp(value, minimum, maximum), minimum, maximum);
        }).ToImmutableArray();
        return new CompiledAttributeSetDefinition(definitions, Fingerprint(definitions));
    }

    public static CompiledAttributeSetDefinition WithBaseValues(
        CompiledAttributeSetDefinition definition,
        IReadOnlyDictionary<CombatAttribute, float> overrides)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(overrides);
        var attributes = definition.Attributes.Select(item =>
        {
            var value = overrides.GetValueOrDefault(item.Attribute, item.BaseValue);
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(overrides), "Attribute base values must be finite.");
            return item with { BaseValue = Math.Clamp(value, item.Minimum, item.Maximum) };
        }).ToImmutableArray();
        return new CompiledAttributeSetDefinition(attributes, Fingerprint(attributes));
    }

    public static CompiledAttributeMagnitude? CompileMagnitude(AttributeMagnitudeSpec? authored, ValidationReport report)
    {
        var remaining = 128;
        return CompileMagnitude(authored, report, new HashSet<AttributeMagnitudeSpec>(ReferenceEqualityComparer.Instance), 0, ref remaining);
    }

    private static CompiledAttributeMagnitude? CompileMagnitude(AttributeMagnitudeSpec? authored, ValidationReport report,
        HashSet<AttributeMagnitudeSpec> ancestors, int depth, ref int remaining)
    {
        if (authored is null)
        {
            report.Error("Modifier magnitude is missing.");
            return null;
        }
        if (!Enum.IsDefined(authored.CaptureMode)) report.Error("Magnitude capture mode is invalid.");
        if (--remaining < 0 || depth > 8 || ancestors.Contains(authored))
        {
            report.Error("Magnitude expression is cyclic or exceeds depth 8 / 128 nodes.");
            return null;
        }
        switch (authored)
        {
            case CompositeAttributeMagnitudeSpec composite:
                if (!Enum.IsDefined(composite.Operation) || composite.Operands is null || composite.Operands.Count is < 2 or > 8)
                {
                    report.Error("Composite magnitude requires a valid operation and 2–8 operands.");
                    return null;
                }
                ancestors.Add(authored);
                var children = ImmutableArray.CreateBuilder<CompiledAttributeMagnitude>();
                foreach (var operand in composite.Operands)
                {
                    if (remaining <= 0) { report.Error("Magnitude expression exceeds 128 nodes."); break; }
                    var child = CompileMagnitude(operand, report, ancestors, depth + 1, ref remaining);
                    if (child is not null) children.Add(child);
                }
                ancestors.Remove(authored);
                return report.HasCoreErrors ? null : new CompiledCompositeMagnitude(composite.Operation, children.ToImmutable(), composite.CaptureMode);
            case ConstantAttributeMagnitudeSpec constant when float.IsFinite(constant.Value):
                return new CompiledConstantMagnitude(constant.Value, constant.CaptureMode);
            case ConstantAttributeMagnitudeSpec:
                report.Error("Constant magnitude must be finite.");
                return null;
            case SourceAttributeMagnitudeSpec source when Enum.IsDefined(source.Attribute):
                return new CompiledSourceAttributeMagnitude(source.Attribute, source.CaptureMode);
            case SourceAttributeMagnitudeSpec:
                report.Error("Source-attribute magnitude has an invalid attribute id.");
                return null;
            case TargetAttributeMagnitudeSpec target when Enum.IsDefined(target.Attribute):
                return new CompiledTargetAttributeMagnitude(target.Attribute, target.CaptureMode);
            case TargetAttributeMagnitudeSpec:
                report.Error("Target-attribute magnitude has an invalid attribute id.");
                return null;
            case ContextAttributeMagnitudeSpec context when !string.IsNullOrWhiteSpace(context.Key):
                return new CompiledContextValueMagnitude(context.Key, context.CaptureMode);
            case ContextAttributeMagnitudeSpec:
                report.Error("Context magnitude key is required.");
                return null;
            case TeamCountAttributeMagnitudeSpec count when !Enum.IsDefined(count.CountKind):
                report.Error("Team-count magnitude has an invalid count kind.");
                return null;
            case TeamCountAttributeMagnitudeSpec count when count.Team is < 0 or > 1:
                report.Error("Team-count magnitude team must be zero or one.");
                return null;
            case TeamCountAttributeMagnitudeSpec count:
                return new CompiledTeamCountMagnitude(count.CountKind, count.Team, count.CaptureMode);
            case TraitValueAttributeMagnitudeSpec trait when !string.IsNullOrWhiteSpace(trait.TraitId) &&
                                                             trait.Team is >= 0 and <= 1:
                return new CompiledTraitValueMagnitude(trait.TraitId, trait.Team, trait.CaptureMode);
            case TraitValueAttributeMagnitudeSpec trait when !string.IsNullOrWhiteSpace(trait.TraitId):
                report.Error("Trait magnitude team must be zero or one.");
                return null;
            case TraitValueAttributeMagnitudeSpec:
                report.Error("Trait magnitude id is required.");
                return null;
            default:
                report.Error($"Unsupported magnitude type: {authored.GetType().Name}.");
                return null;
        }
    }

    private static float Default(CombatAttribute attribute) => attribute switch
    {
        CombatAttribute.AttackSpeed or CombatAttribute.MoveSpeed => 1f,
        CombatAttribute.CriticalDamage => 1.5f,
        _ => 0f
    };

    private static (float Minimum, float Maximum) LegacyClamp(CombatAttribute attribute) => attribute switch
    {
        CombatAttribute.AttackSpeed => (.01f, float.MaxValue),
        CombatAttribute.MoveSpeed => (.01f, 1000f),
        CombatAttribute.AttackRange => (0, 1000f),
        CombatAttribute.CriticalChance or CombatAttribute.DodgeChance or CombatAttribute.LifeSteal or CombatAttribute.ControlResistance => (0, 1f),
        CombatAttribute.MaxMana or CombatAttribute.StartingMana or CombatAttribute.ManaPerSecond or
            CombatAttribute.ManaPerAttack or CombatAttribute.ManaPerDamageRatio or CombatAttribute.ManaPerHitCap => (0, 1_000_000f),
        _ => (-1_000_000_000f, 1_000_000_000f)
    };

    private static string Fingerprint(IEnumerable<CompiledAttributeDefinition> definitions)
    {
        var canonical = string.Join("|", definitions.OrderBy(item => item.Attribute).Select(item =>
            $"{item.Attribute}:{item.BaseValue.ToString("R", CultureInfo.InvariantCulture)}:" +
            $"{item.Minimum.ToString("R", CultureInfo.InvariantCulture)}:{item.Maximum.ToString("R", CultureInfo.InvariantCulture)}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
