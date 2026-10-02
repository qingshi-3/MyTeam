using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Battle;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Content;

internal static class ContentTacticalCompiler
{
    internal static TacticalCommandBatchCompilationResult CompileTacticalCommandGraph(
        TacticalCommandAuthoredGraph graph,
        AbilityBatchCompilationResult abilities,
        ValidationReport report,
        bool requireProductionDirectories)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var authored = graph.Definitions.ToArray();
        var referenced = new List<TacticalCommandDefinition?>();
        var scenePaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scene in graph.Scenes)
        {
            if (scene is null)
            {
                report.Error("Tactical-command scene is missing.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(scene.ResourcePath) || !scenePaths.Add(scene.ResourcePath))
                report.Error($"Duplicate or pathless tactical-command scene: {scene.ResourcePath}");
            TacticalCommandContentRoot? root = null;
            try
            {
                root = scene.Instantiate<TacticalCommandContentRoot>();
                report.Merge(root.ValidateAuthoring());
                if (requireProductionDirectories && root.Definition is not null &&
                    !string.IsNullOrWhiteSpace(root.Definition.ResourcePath) &&
                    !root.Definition.ResourcePath.StartsWith(
                        "res://content/tactical-commands/definitions/",
                        StringComparison.Ordinal))
                    report.Error($"{scene.ResourcePath}: tactical-command definition is outside the production directory.");
                referenced.Add(root.Definition);
            }
            catch (Exception exception)
            {
                report.Error($"Tactical-command scene validation failed for {scene.ResourcePath}: {exception.Message}");
            }
            finally
            {
                if (GodotObject.IsInstanceValid(root)) root?.Free();
            }
        }

        var authoredByPath = authored.Where(definition => definition is not null)
            .Cast<TacticalCommandDefinition>()
            .GroupBy(ContentResourceDiscovery.ResourceLabel, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var referencedPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in referenced)
        {
            if (definition is null)
            {
                report.Error("Tactical-command scene has no definition.");
                continue;
            }
            var label = ContentResourceDiscovery.ResourceLabel(definition);
            if (!referencedPaths.Add(label))
                report.Error($"Tactical-command definition is referenced by multiple scenes: {label}");
            if (!authoredByPath.ContainsKey(label))
                report.Error($"Tactical-command scene references an unregistered definition: {label}");
        }
        foreach (var definition in authored.Where(definition => definition is not null).Cast<TacticalCommandDefinition>())
            if (!referencedPaths.Contains(ContentResourceDiscovery.ResourceLabel(definition)))
                report.Error($"Orphan tactical-command definition is not referenced by a concrete scene: {ContentResourceDiscovery.ResourceLabel(definition)}");

        CompiledAbilityLoadout? ResolveLoadout(AbilityLoadoutDefinition? loadout)
        {
            if (loadout is null) return null;
            foreach (var publication in abilities.Loadouts)
                if (ReferenceEquals(publication.Authored, loadout) ||
                    !string.IsNullOrWhiteSpace(loadout.ResourcePath) &&
                    publication.Authored.ResourcePath == loadout.ResourcePath)
                    return publication.Compiled;
            return null;
        }

        var compilation = TacticalCommandDefinitionCompiler.CompileBatch(authored, ResolveLoadout);
        report.Merge(compilation.Report);
        if (!report.HasCoreErrors &&
            (compilation.Definitions.Length != authored.Length ||
             compilation.Definitions.Length != referenced.Count))
            report.Error("Tactical-command publication did not compile exactly one definition per concrete scene.");
        return report.HasCoreErrors
            ? new TacticalCommandBatchCompilationResult([], report)
            : compilation;
    }

}
