using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Content;
namespace TowerAutobattler.Abilities;

public abstract record CompiledCombatTechniqueOperation : CompiledAbilityOperation;
public sealed record CompiledDuelOperation(int DurationTicks, float BreakDistance) : CompiledCombatTechniqueOperation;
public sealed record CompiledCounterattackOperation(int HitsRequired, float AttackRatio, float LifestealRatio) : CompiledCombatTechniqueOperation;
public sealed record CompiledGritStorageOperation(string CounterKey, int WindowTicks, float MaximumHealthRatio) : CompiledCombatTechniqueOperation;
public sealed record CompiledGritPunchOperation(string CounterKey, float LowHealthRatio, float CriticalHealthRatio,
    float Range, float Radius, int ChargeTicks, int RecoveryTicks, int ShieldTicks, float AttackRatio, float GritDamageRatio) : CompiledCombatTechniqueOperation;
public sealed record CompiledHookOperation(float Range, float Radius, float Speed, int WindupTicks, int ReturnTicks,
    float AttackRatio, bool ExcludeBoss) : CompiledCombatTechniqueOperation;

public static partial class AbilityDefinitionCompiler
{
    private static CompiledAbilityOperation? CompileTechnique(AbilityOperationSpec spec, string label, ValidationReport report)
    {
        bool B(float value, float min, float max) => float.IsFinite(value) && value >= min && value <= max;
        CompiledCombatTechniqueOperation? result = spec switch
        {
            DuelAbilityOperationSpec d when d.DurationTicks is > 0 and <= 150 && B(d.BreakDistance, 1, 20) =>
                new CompiledDuelOperation(d.DurationTicks, d.BreakDistance),
            CounterattackAbilityOperationSpec c when c.HitsRequired is > 0 and <= 20 && B(c.AttackRatio, 0, 10) && B(c.LifestealRatio, 0, 5) =>
                new CompiledCounterattackOperation(c.HitsRequired, c.AttackRatio, c.LifestealRatio),
            GritStorageAbilityOperationSpec g when !string.IsNullOrWhiteSpace(g.CounterKey) && g.WindowTicks is > 0 and <= 300 && B(g.MaximumHealthRatio, .01f, 1) =>
                new CompiledGritStorageOperation(g.CounterKey, g.WindowTicks, g.MaximumHealthRatio),
            GritPunchAbilityOperationSpec p when !string.IsNullOrWhiteSpace(p.CounterKey) &&
                B(p.CriticalHealthRatio, .01f, .99f) && B(p.LowHealthRatio, .01f, 1) && p.CriticalHealthRatio < p.LowHealthRatio &&
                B(p.Range, .1f, 10) && B(p.Radius, .1f, 3) && p.ChargeTicks is > 0 and <= 50 &&
                p.RecoveryTicks is > 0 and <= 50 &&
                p.ShieldTicks is > 0 and <= 100 && B(p.AttackRatio, 0, 10) && B(p.GritDamageRatio, 0, 5) =>
                new CompiledGritPunchOperation(p.CounterKey,p.LowHealthRatio,p.CriticalHealthRatio,p.Range,p.Radius,
                    p.ChargeTicks,p.RecoveryTicks,p.ShieldTicks,p.AttackRatio,p.GritDamageRatio),
            HookAbilityOperationSpec h when B(h.Range, .1f, 20) && B(h.Radius, .01f, 1) && B(h.Speed, 1, 30) &&
                h.WindupTicks is > 0 and <= 30 && h.ReturnTicks is > 0 and <= 30 && B(h.AttackRatio, 0, 10) =>
                new CompiledHookOperation(h.Range,h.Radius,h.Speed,h.WindupTicks,h.ReturnTicks,h.AttackRatio,h.ExcludeBoss),
            _ => null
        };
        if (result is null) report.Error($"{label}: combat technique parameters are missing, non-finite or outside supported bounds.");
        return result;
    }

    private static void ValidateTechniqueLoadout(IReadOnlyList<CompiledAbilityDefinition> abilities, ValidationReport report)
    {
        var storage = abilities.SelectMany(a => a.Operations).OfType<CompiledGritStorageOperation>().ToArray();
        var punches = abilities.SelectMany(a => a.Operations).OfType<CompiledGritPunchOperation>().ToArray();
        foreach (var punch in punches)
            if (storage.Count(s => s.CounterKey == punch.CounterKey) != 1 || punches.Count(p => p.CounterKey == punch.CounterKey) != 1)
                report.Error("A grit punch requires exactly one matching grit storage operation in its loadout.");
    }
}
