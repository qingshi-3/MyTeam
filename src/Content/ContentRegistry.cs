using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace TowerAutobattler.Content;

public sealed class ContentRegistry
{
    private readonly Dictionary<string, CatalogEntry> _entries;
    public ContentCatalog Catalog { get; }

    private ContentRegistry(ContentCatalog catalog, Dictionary<string, CatalogEntry> entries)
    {
        Catalog = catalog;
        _entries = entries;
    }

    public bool TryGet(string stableId, out CatalogEntry entry) => _entries.TryGetValue(stableId, out entry!);

    public static async Task<(ContentRegistry? Registry, ValidationReport Report)> CreateReadyAsync(
        Node treeOwner, ContentCatalog? catalog, IEnumerable<PackedScene>? additionalValidationScenes = null,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes = null)
    {
        var report = new ValidationReport();
        ContentRegistry? registry = null;
        var logger = new ContentReadyGateLogger();
        var loggerRegistrationAttempted = false;
        try
        {
            try
            {
                loggerRegistrationAttempted = true;
                Godot.OS.AddLogger(logger);
            }
            catch (Exception exception)
            {
                report.Error($"Content registry logger install failed: {exception.Message}");
            }

            if (!report.HasCoreErrors)
            {
                try { report.Merge(ContentValidator.Validate(catalog, additionalStructuralValidationScenes)); }
                catch (Exception exception) { report.Error($"Content structural validation failed: {exception.Message}"); }
            }

            if (!report.HasCoreErrors && !HasCapturedEngineErrors(logger, report, "after structural validation") && catalog is not null)
                await ContentValidator.ValidateReadyFrameAsync(treeOwner, catalog, report, additionalValidationScenes);

            if (!report.HasCoreErrors && !HasCapturedEngineErrors(logger, report, "before publication") && catalog is not null)
            {
                try
                {
                    var entries = new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
                    foreach (var entry in catalog.AllEntries()) entries.Add(entry.StableId, entry);
                    registry = new ContentRegistry(catalog, entries);
                }
                catch (Exception exception) { report.Error($"Content registry publication failed: {exception.Message}"); }
            }
        }
        finally
        {
            try
            {
                foreach (var error in logger.Errors)
                    report.Error("Content registry engine error: " + error);
            }
            catch (Exception exception) { report.Error($"Content registry logger read failed: {exception.Message}"); }

            if (loggerRegistrationAttempted)
            {
                try { Godot.OS.RemoveLogger(logger); }
                catch (Exception exception) { report.Error($"Content registry logger removal failed: {exception.Message}"); }
            }
            try { logger.Dispose(); }
            catch (Exception exception) { report.Error($"Content registry logger disposal failed: {exception.Message}"); }
        }

        return report.HasCoreErrors ? (null, report) : (registry, report);
    }

    private static bool HasCapturedEngineErrors(ContentReadyGateLogger logger, ValidationReport report, string stage)
    {
        try { return logger.Errors.Count > 0; }
        catch (Exception exception)
        {
            report.Error($"Content registry logger read failed {stage}: {exception.Message}");
            return true;
        }
    }
}
