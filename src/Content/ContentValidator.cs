using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Content;

public static class ContentValidator
{
    private static readonly string[] ConcreteDirectories =
    [
        "res://content/heroes", "res://content/soldiers", "res://content/enemies", "res://content/items"
    ];
    private static readonly Regex StableIdPattern = new("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant);

    public static ValidationReport Validate(
        ContentCatalog? catalog, IEnumerable<PackedScene>? additionalStructuralValidationScenes = null)
    {
        var report = new ValidationReport();
        if (catalog is null)
        {
            report.Error("Content catalog failed to load.");
            return report;
        }

        if (additionalStructuralValidationScenes is not null)
            foreach (var scene in additionalStructuralValidationScenes)
                ValidateStructuralProbe(scene, report);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var scenes = new HashSet<string>(StringComparer.Ordinal);
        var definitions = new HashSet<string>(StringComparer.Ordinal);
        var portraits = new HashSet<string>(StringComparer.Ordinal);
        if (catalog.Heroes.Count != 8 || catalog.Soldiers.Count != 24 || catalog.Enemies.Count != 13)
            report.Error($"Production unit catalog must contain 8 heroes, 24 soldiers, and 13 enemies; got {catalog.Heroes.Count}, {catalog.Soldiers.Count}, {catalog.Enemies.Count}.");
        ValidateGroup(catalog.Heroes, ContentCategory.Hero, report, ids, scenes, definitions, portraits);
        ValidateGroup(catalog.Soldiers, ContentCategory.Soldier, report, ids, scenes, definitions, portraits);
        ValidateGroup(catalog.Enemies, ContentCategory.Enemy, report, ids, scenes, definitions, portraits);
        ValidateGroup(catalog.Items, ContentCategory.Item, report, ids, scenes, definitions, portraits);

        var diskScenes = DiscoverConcreteScenes(report);
        foreach (var scenePath in scenes)
            if (!diskScenes.Contains(scenePath)) report.Error($"Catalog scene does not exist in concrete directories: {scenePath}");
        foreach (var scenePath in diskScenes)
            if (!scenes.Contains(scenePath)) report.Error($"Concrete scene is missing from catalog: {scenePath}");

        var diskDefinitions = DiscoverFiles("res://content/definitions", ".tres", true, report);
        foreach (var definitionPath in definitions)
            if (!diskDefinitions.Contains(definitionPath)) report.Error($"Catalog definition does not exist: {definitionPath}");
        foreach (var definitionPath in diskDefinitions)
            if (!definitions.Contains(definitionPath)) report.Error($"Definition is missing from catalog: {definitionPath}");

        var diskPortraits = DiscoverFiles("res://content/portraits", ".tres", true, report);
        foreach (var portraitPath in portraits)
            if (!diskPortraits.Contains(portraitPath)) report.Error($"Catalog portrait does not exist: {portraitPath}");
        foreach (var portraitPath in diskPortraits)
            if (!portraits.Contains(portraitPath)) report.Error($"Portrait resource is missing from the production unit catalog: {portraitPath}");
        if (portraits.Count != 45 || diskPortraits.Count != 45)
            report.Error($"Production portrait coverage must be exactly 45/45; catalog={portraits.Count}, disk={diskPortraits.Count}.");

        var floorScenePaths = new HashSet<string>(StringComparer.Ordinal);
        var floorIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var floorRule in catalog.FloorRules)
        {
            if (floorRule is null || string.IsNullOrWhiteSpace(floorRule.ResourcePath))
                report.Error("Floor-rule catalog contains an empty scene.");
            else if (!floorScenePaths.Add(floorRule.ResourcePath))
                report.Error($"Duplicate floor-rule scene: {floorRule.ResourcePath}");
            else
            {
                try
                {
                    var root = floorRule.Instantiate<FloorRuleContentRoot>();
                    try
                    {
                        report.Merge(root.ValidateAuthoring());
                        if (!StableIdPattern.IsMatch(root.Id)) report.Error($"Invalid floor-rule id: {root.Id}");
                        if (!floorIds.Add(root.Id)) report.Error($"Duplicate floor-rule id: {root.Id}");
                    }
                    finally { root.Free(); }
                }
                catch (Exception exception)
                {
                    report.Error($"Floor-rule instantiate failed for {floorRule.ResourcePath}: {exception.Message}");
                }
            }
        }
        var diskFloorScenes = DiscoverFiles("res://content/floor-rules", ".tscn", false, report);
        foreach (var scenePath in floorScenePaths)
            if (!diskFloorScenes.Contains(scenePath)) report.Error($"Catalog floor-rule scene does not exist: {scenePath}");
        foreach (var scenePath in diskFloorScenes)
            if (!floorScenePaths.Contains(scenePath)) report.Error($"Floor-rule scene is missing from catalog: {scenePath}");
        return report;
    }

    public static async Task ValidateReadyFrameAsync(
        Node treeOwner, ContentCatalog catalog, ValidationReport report,
        IEnumerable<PackedScene>? additionalValidationScenes = null)
    {
        if (!treeOwner.IsInsideTree())
        {
            report.Error("Ready-frame validation requires a node inside the active scene tree.");
            return;
        }

        var host = new Node2D
        {
            Name = "ContentReadyValidationHost",
            Visible = false
        };
        var attached = false;
        try
        {
            try
            {
                foreach (var entry in catalog.AllEntries())
                    TryAddValidationInstance(host, entry.Scene, entry.Scene.ResourcePath, report);
                foreach (var scene in catalog.FloorRules)
                    TryAddValidationInstance(host, scene, scene?.ResourcePath ?? "<empty floor rule>", report);
                if (additionalValidationScenes is not null)
                    foreach (var scene in additionalValidationScenes)
                        TryAddValidationInstance(host, scene, scene?.ResourcePath ?? "<additional validation scene>", report);
            }
            catch (Exception exception)
            {
                report.Error($"Content ready-frame staging failed: {exception.Message}");
            }

            if (report.HasCoreErrors) return;
            try
            {
                treeOwner.AddChild(host);
                attached = true;
            }
            catch (Exception exception)
            {
                report.Error($"Content ready-frame attach failed: {exception.Message}");
            }

            if (!attached) return;
            try
            {
                await treeOwner.ToSignal(treeOwner.GetTree(), SceneTree.SignalName.ProcessFrame);
                // ProcessFrame is emitted before node _Process callbacks. The second signal proves
                // that every enabled validation node completed at least one actual process cycle.
                await treeOwner.ToSignal(treeOwner.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            catch (Exception exception)
            {
                report.Error($"Content ready-frame process failed: {exception.Message}");
            }
        }
        finally
        {
            try
            {
                if (attached && GodotObject.IsInstanceValid(treeOwner) && GodotObject.IsInstanceValid(host))
                    treeOwner.RemoveChild(host);
            }
            catch (Exception exception) { report.Error($"Content ready-frame detach failed: {exception.Message}"); }
            try
            {
                if (GodotObject.IsInstanceValid(host)) host.Free();
            }
            catch (Exception exception) { report.Error($"Content ready-frame free failed: {exception.Message}"); }
        }
    }

    private static void ValidateStructuralProbe(PackedScene? scene, ValidationReport report)
    {
        if (scene is null)
        {
            report.Error("Structural validation probe scene is empty.");
            return;
        }

        Node? instance = null;
        try { instance = scene.Instantiate(); }
        catch (Exception exception) { report.Error($"Structural validation probe instantiate failed: {exception.Message}"); }
        finally
        {
            try
            {
                if (GodotObject.IsInstanceValid(instance)) instance?.Free();
            }
            catch (Exception exception) { report.Error($"Structural validation probe free failed: {exception.Message}"); }
        }
    }

    private static void TryAddValidationInstance(Node host, PackedScene? scene, string label, ValidationReport report)
    {
        if (scene is null)
        {
            report.Error($"Ready-frame validation scene is empty: {label}");
            return;
        }
        Node? instance;
        try { instance = scene.Instantiate(); }
        catch (Exception exception)
        {
            report.Error($"Ready-frame instantiate failed for {label}: {exception.Message}");
            return;
        }

        try { host.AddChild(instance); }
        catch (Exception exception)
        {
            report.Error($"Ready-frame validation-host attach failed for {label}: {exception.Message}");
            try
            {
                if (GodotObject.IsInstanceValid(instance)) instance.Free();
            }
            catch (Exception freeException) { report.Error($"Ready-frame failed-instance free failed for {label}: {freeException.Message}"); }
        }
    }

    private static void ValidateGroup(
        Godot.Collections.Array<CatalogEntry> entries, ContentCategory category, ValidationReport report,
        HashSet<string> ids, HashSet<string> scenes, HashSet<string> definitions, HashSet<string> portraits)
    {
        foreach (var entry in entries) ValidateEntry(entry, category, report, ids, scenes, definitions, portraits);
    }

    private static void ValidateEntry(
        CatalogEntry? entry,
        ContentCategory category,
        ValidationReport report,
        HashSet<string> ids,
        HashSet<string> scenes,
        HashSet<string> definitions,
        HashSet<string> portraits)
    {
        if (entry is null) { report.Error("Catalog contains a null entry."); return; }
        if (entry.Scene is null) { report.Error("Catalog entry has no PackedScene."); return; }
        if (entry.Definition is null) { report.Error($"{entry.Scene.ResourcePath}: catalog entry has no definition."); return; }
        if (string.IsNullOrWhiteSpace(entry.StableId)) report.Error($"{entry.Scene.ResourcePath}: definition has no stable id.");
        else if (!StableIdPattern.IsMatch(entry.StableId)) report.Error($"Invalid stable id: {entry.StableId}");
        else if (!ids.Add(entry.StableId)) report.Error($"Duplicate content id: {entry.StableId}");
        if (!scenes.Add(entry.Scene.ResourcePath)) report.Error($"Duplicate scene reference: {entry.Scene.ResourcePath}");
        if (string.IsNullOrWhiteSpace(entry.Definition.ResourcePath)) report.Error($"{entry.StableId}: definition is not an external resource.");
        else if (!definitions.Add(entry.Definition.ResourcePath)) report.Error($"Duplicate definition reference: {entry.Definition.ResourcePath}");

        try
        {
            var instance = entry.Scene.Instantiate();
            try
            {
                switch (instance)
                {
                    case UnitContentRoot unit:
                        report.Merge(unit.ValidateAuthoring());
                        if (!ReferenceEquals(unit.Definition, entry.Definition))
                            report.Error($"{entry.StableId}: scene root and catalog do not reference the same definition.");
                        ValidateUnitCategory(unit.Definition, category, entry.Scene.ResourcePath, report, portraits);
                        break;
                    case ItemContentRoot item:
                        report.Merge(item.ValidateAuthoring());
                        if (!ReferenceEquals(item.Definition, entry.Definition))
                            report.Error($"{entry.StableId}: scene root and catalog do not reference the same definition.");
                        if (category != ContentCategory.Item) report.Error($"{entry.StableId}: item appears in {category} catalog.");
                        break;
                    default:
                        report.Error($"{entry.Scene.ResourcePath}: root must be UnitContentRoot or ItemContentRoot.");
                        break;
                }
            }
            finally { instance.Free(); }
        }
        catch (Exception exception)
        {
            report.Error($"Scene instantiate failed for {entry.Scene.ResourcePath}: {exception.Message}");
        }
    }

    private static void ValidateUnitCategory(
        UnitDefinition definition, ContentCategory category, string scenePath, ValidationReport report,
        HashSet<string> portraits)
    {
        var valid = category switch
        {
            ContentCategory.Hero => definition.IsHero && !definition.IsEnemy,
            ContentCategory.Soldier => !definition.IsHero && !definition.IsEnemy,
            ContentCategory.Enemy => !definition.IsHero && definition.IsEnemy,
            _ => false
        };
        if (!valid) report.Error($"{definition.Id}: category flags do not match {category}.");
        var expectedFolder = category switch
        {
            ContentCategory.Hero => "heroes",
            ContentCategory.Soldier => "soldiers",
            ContentCategory.Enemy => "enemies",
            _ => "items"
        };
        if (!scenePath.StartsWith($"res://content/{expectedFolder}/", StringComparison.Ordinal))
            report.Error($"{definition.Id}: scene is outside its {category} directory.");
        var requiredTag = category.ToString().ToLowerInvariant();
        if (!definition.Tags.Contains(requiredTag)) report.Error($"{definition.Id}: missing required tag '{requiredTag}'.");
        if (definition.Portrait is null)
        {
            report.Error($"{definition.Id}: missing production portrait resource.");
            return;
        }
        report.Merge(definition.Portrait.Validate(definition.Id));
        if (string.IsNullOrWhiteSpace(definition.Portrait.ResourcePath)) return;
        if (!portraits.Add(definition.Portrait.ResourcePath))
            report.Error($"{definition.Id}: duplicate portrait resource reference {definition.Portrait.ResourcePath}.");
        if (!definition.Portrait.ResourcePath.StartsWith($"res://content/portraits/{expectedFolder}/", StringComparison.Ordinal))
            report.Error($"{definition.Id}: portrait is outside its {category} directory.");
    }

    private static HashSet<string> DiscoverConcreteScenes(ValidationReport report)
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

    private static HashSet<string> DiscoverFiles(string rootPath, string extension, bool recursive, ValidationReport report)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        DiscoverInto(rootPath, extension, recursive, report, result);
        return result;
    }

    private static void DiscoverInto(string path, string extension, bool recursive, ValidationReport report, HashSet<string> result)
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

    private enum ContentCategory { Hero, Soldier, Enemy, Item }
}

internal sealed partial class ContentReadyGateLogger : Logger
{
    private readonly object _lock = new();
    private readonly List<string> _errors = [];
    public IReadOnlyList<string> Errors
    {
        get { lock (_lock) return _errors.ToArray(); }
    }

    public override void _LogError(
        string function, string file, int line, string code, string rationale,
        bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (errorType == (int)ErrorType.Warning) return;
        var message = string.IsNullOrWhiteSpace(rationale) ? code : rationale;
        lock (_lock) _errors.Add($"{message} ({file}:{line} {function})");
    }
}
