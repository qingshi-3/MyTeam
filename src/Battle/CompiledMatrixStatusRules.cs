using System;
using System.Globalization;
using TowerAutobattler.Content;
using TowerAutobattler.Project;

namespace TowerAutobattler.Battle;

public sealed record CompiledMatrixStatusRules(int PoisonDurationTicks, int PoisonIntervalTicks,
    float PoisonDamagePerLayer, int ChillDurationTicks, int ChillDefaultStackLimit,
    float ChillSlowPerLayer, int EmberDurationTicks, int EmberStackLimit)
{
    // Compatibility for programmatically constructed battle fixtures. Authored projects
    // carry their explicit compiled resource through both Run and laboratory preparation.
    public static CompiledMatrixStatusRules Default { get; } = new(120, 10, 2, 40, 2, .06f, 80, 5);
    public string Fingerprint => string.Create(CultureInfo.InvariantCulture,
        $"{PoisonDurationTicks}:{PoisonIntervalTicks}:{PoisonDamagePerLayer:R}:{ChillDurationTicks}:{ChillDefaultStackLimit}:{ChillSlowPerLayer:R}:{EmberDurationTicks}:{EmberStackLimit}");

    internal static CompiledMatrixStatusRules Compile(MatrixStatusRulesDefinition source,
        ValidationReport report, int maximumTraitChillStacks = 2)
    {
        var rules = new CompiledMatrixStatusRules(source.PoisonDurationTicks, source.PoisonIntervalTicks,
            source.PoisonDamagePerLayer, source.ChillDurationTicks, source.ChillDefaultStackLimit,
            source.ChillSlowPerLayer, source.EmberDurationTicks, source.EmberStackLimit);
        if (rules.PoisonDurationTicks is < 1 or > 3600 || rules.PoisonIntervalTicks is < 1 or > 3600 ||
            rules.PoisonIntervalTicks > rules.PoisonDurationTicks ||
            rules.ChillDurationTicks is < 1 or > 3600 || rules.EmberDurationTicks is < 1 or > 3600)
            report.Error("Matrix status rules contain invalid duration or periodic interval.");
        if (!float.IsFinite(rules.PoisonDamagePerLayer) || rules.PoisonDamagePerLayer is < 0 or > 100000 ||
            !float.IsFinite(rules.ChillSlowPerLayer) || rules.ChillSlowPerLayer < 0 ||
            rules.ChillSlowPerLayer * Math.Max(maximumTraitChillStacks, rules.ChillDefaultStackLimit) >= 1 ||
            rules.ChillDefaultStackLimit is < 1 or > 100 || rules.EmberStackLimit is < 1 or > 100)
            report.Error("Matrix status rules contain invalid damage, stack limit or cumulative slow.");
        return rules;
    }
}
