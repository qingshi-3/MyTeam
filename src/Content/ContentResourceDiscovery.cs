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

internal static class ContentResourceDiscovery
{
    private static readonly string[] ConcreteDirectories =
    [
        "res://content/heroes", "res://content/soldiers", "res://content/enemies", "res://content/items"
    ];
    internal static IReadOnlyList<T?> LoadResources<T>(
        IEnumerable<string> paths,
        string label,
        ValidationReport report) where T : Resource
    {
        var resources = new List<T?>();
        foreach (var path in paths.OrderBy(path => path, StringComparer.Ordinal))
        {
            try
            {
                var resource = GD.Load<T>(path);
                if (resource is null) report.Error($"Failed to load {label}: {path}");
                resources.Add(resource);
            }
            catch (Exception exception)
            {
                report.Error($"Failed to load {label} {path}: {exception.Message}");
                resources.Add(null);
            }
        }
        return resources;
    }

    internal static string ResourceLabel(Resource resource) =>
        string.IsNullOrWhiteSpace(resource.ResourcePath) ? $"<{resource.GetType().Name}>" : resource.ResourcePath;

    // Validates the authored entry graph without assuming production directory roots or sample totals.
    // Registry publication still uses Validate(), which adds bidirectional disk-completeness gates.

    internal static TacticalCommandAuthoredGraph LoadProductionTacticalCommandGraph(
        ValidationReport report)
    {
        var definitions = LoadResources<TacticalCommandDefinition>(
            DiscoverFiles("res://content/tactical-commands/definitions", ".tres", false, report),
            "tactical-command definition",
            report);
        var scenePaths = DiscoverFiles(
            "res://content/tactical-commands/commands",
            ".tscn",
            false,
            report);
        var scenes = new List<PackedScene?>();
        foreach (var path in scenePaths.OrderBy(path => path, StringComparer.Ordinal))
        {
            try
            {
                var scene = GD.Load<PackedScene>(path);
                if (scene is null) report.Error($"Failed to load tactical-command scene: {path}");
                scenes.Add(scene);
            }
            catch (Exception exception)
            {
                report.Error($"Failed to load tactical-command scene {path}: {exception.Message}");
                scenes.Add(null);
            }
        }
        return new TacticalCommandAuthoredGraph(definitions, scenes);
    }

    internal static IReadOnlyList<AbilityDefinition?> LoadOptionalAbilityDirectory(string folder, AbilityActivationKind kind, ValidationReport report)
    {
        var path = $"res://content/abilities/{folder}";
        if (!DirAccess.DirExistsAbsolute(path)) return [];
        var definitions = LoadResources<AbilityDefinition>(DiscoverFiles(path, ".tres", false, report), $"{folder} ability", report);
        foreach (var definition in definitions.OfType<AbilityDefinition>())
            if (definition.ActivationKind != kind) report.Error($"{ResourceLabel(definition)}: {folder} directory requires {kind} entry point.");
        return definitions;
    }

    internal static HashSet<string> DiscoverConcreteScenes(ValidationReport report)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var directoryPath in ConcreteDirectories)
        {
            using var directory = DirAccess.Open(directoryPath);
            if (directory is null) { report.Error($"Missing concrete content directory: {directoryPath}"); continue; }
            directory.ListDirBegin();
            for (var name = directory.GetNext(); !string.IsNullOrEmpty(name); name = directory.GetNext())
            {
                if (!directory.CurrentIsDir() && name.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
                    result.Add($"{directoryPath}/{name}");
            }
            directory.ListDirEnd();
        }
        return result;
    }

    internal static HashSet<string> DiscoverFiles(string rootPath, string extension, bool recursive, ValidationReport report)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        DiscoverInto(rootPath, extension, recursive, report, result);
        return result;
    }

    internal static void DiscoverInto(string path, string extension, bool recursive, ValidationReport report, HashSet<string> result)
    {
        using var directory = DirAccess.Open(path);
        if (directory is null) { report.Error($"Missing content directory: {path}"); return; }
        directory.ListDirBegin();
        for (var name = directory.GetNext(); !string.IsNullOrEmpty(name); name = directory.GetNext())
        {
            if (directory.CurrentIsDir())
            {
                if (recursive && name is not "." and not "..") DiscoverInto($"{path}/{name}", extension, true, report, result);
            }
            else if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) result.Add($"{path}/{name}");
        }
        directory.ListDirEnd();
    }

}
