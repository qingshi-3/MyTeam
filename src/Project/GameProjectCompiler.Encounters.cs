using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.Project;

public static partial class GameProjectCompiler
{
    private static ImmutableArray<CompiledEncounterComposition> CompileEncounterCompositions(
        EncounterDefinition authored, CompilationContext context, int floorsPerRegion)
    {
        var source = Source(authored);
        foreach (var curve in new[] { authored.LocalHealthMultipliers, authored.LocalDamageMultipliers })
            if (curve is null || (curve.Length != 0 && curve.Length != floorsPerRegion) ||
                curve.Any(value => !float.IsFinite(value) || value <= 0 || value > 10))
                context.Report.Error($"{source}: local multipliers must be empty or cover every local floor with finite values in (0, 10].");
        if ((authored.LocalHealthMultipliers ?? []).Any(x => x * authored.EnemyHealthMultiplier > 100) ||
            (authored.LocalDamageMultipliers ?? []).Any(x => x * authored.EnemyDamageMultiplier > 100))
            context.Report.Error($"{source}: combined encounter/local multipliers cannot exceed 100.");
        var result = ImmutableArray.CreateBuilder<CompiledEncounterComposition>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var composition in authored.Compositions ?? [])
        {
            if (composition is null) { context.Report.Error($"{source}: null encounter composition."); continue; }
            context.RegisterStableId(composition.StableId, composition);
            if (!ids.Add(composition.StableId) || string.IsNullOrWhiteSpace(composition.DisplayName) ||
                composition.MinLocalFloor < 0 || composition.MaxLocalFloor >= floorsPerRegion ||
                composition.MaxLocalFloor < composition.MinLocalFloor)
                context.Report.Error($"{source}: composition requires unique identity, display name and a valid local floor range.");
            var enemies = composition.EnemyIds ?? [];
            var cells = composition.Cells?.ToArray() ?? [];
            if (enemies.Length is < 1 or > 8 || cells.Length != enemies.Length || cells.Distinct().Count() != cells.Length ||
                cells.Any(cell => !BattlefieldLayout.IsInBounds(cell) || cell.X < BattlefieldLayout.Width - BattlefieldLayout.PlayerDeploymentColumns))
                context.Report.Error($"{source}: composition must place 1–8 enemies at distinct enemy-side cells.");
            foreach (var enemy in enemies)
            {
                if (!context.IsContent(enemy, ContentPoolKind.Enemy))
                    context.Report.Error($"{source}: unknown composition enemy '{enemy}'.");
                else if (context.Entry(enemy).Definition is UnitDefinition { Role: UnitRole.Boss } &&
                    (authored.NodeType != TowerNodeType.Boss || enemy != authored.LeadEnemyId))
                    context.Report.Error($"{source}: composition contains an unexpected boss '{enemy}'.");
            }
            if (authored.NodeType == TowerNodeType.Boss &&
                (enemies.FirstOrDefault() != authored.LeadEnemyId || enemies.Count(id => id == authored.LeadEnemyId) != 1))
                context.Report.Error($"{source}: boss composition must start with exactly one encounter boss.");
            result.Add(new(composition.StableId, composition.DisplayName, composition.MinLocalFloor,
                composition.MaxLocalFloor, [.. enemies], [.. cells]));
        }
        if (result.Count > 0)
        {
            if (authored.AlternateLeadEnemyIds.Length > 0)
                context.Report.Error($"{source}: compositions cannot also randomize alternate leaders.");
            for (var floor = 0; floor < floorsPerRegion; floor++)
                if (!result.Any(entry => entry.MinLocalFloor <= floor && floor <= entry.MaxLocalFloor))
                    context.Report.Error($"{source}: no composition covers local floor {floor}.");
        }
        return result.ToImmutable();
    }
}
