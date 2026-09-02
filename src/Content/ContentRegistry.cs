using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace TowerAutobattler.Content;

public sealed class ContentRegistry
{
    public const int CurrentPublicationVersion = 1;

    private readonly Dictionary<string, CatalogEntry> _entries;

    private ContentRegistry(
        ContentCatalog catalog,
        CompiledContentGraph graph,
        Dictionary<string, CatalogEntry> entries)
    {
        Catalog = catalog;
        Graph = graph;
        _entries = entries;
    }

    public ContentCatalog Catalog { get; }
    public CompiledContentGraph Graph { get; }
    public int PublicationVersion => CurrentPublicationVersion;

    public bool TryGet(string stableId, out CatalogEntry entry) => _entries.TryGetValue(stableId, out entry!);

    internal static async Task<ContentPublicationGateResult<T>> PublishReadyAsync<T>(
        Node treeOwner,
        Func<ContentPublicationPreparation<T>> prepare,
        IEnumerable<PackedScene>? additionalValidationScenes = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(treeOwner);
        ArgumentNullException.ThrowIfNull(prepare);
        var report = new ValidationReport();
        ContentPublicationPreparation<T>? prepared = null;
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
                try
                {
                    prepared = prepare();
                    report.Merge(prepared.Report);
                }
                catch (Exception exception)
                {
                    report.Error($"Game package prepublication failed: {exception.Message}");
                }
            }

            if (!report.HasCoreErrors &&
                (prepared?.Catalog is null || prepared.Graph is null || prepared.Payload is null))
                report.Error("Game package prepublication did not produce a complete catalog, compiled graph, and project payload.");

            if (!report.HasCoreErrors && !HasCapturedEngineErrors(logger, report, "after structural validation"))
                await ContentValidator.ValidateReadyFrameAsync(
                    treeOwner,
                    prepared!.Catalog!,
                    report,
                    additionalValidationScenes);

            if (!report.HasCoreErrors && !HasCapturedEngineErrors(logger, report, "before publication"))
            {
                try
                {
                    var entries = new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
                    foreach (var entry in prepared!.Catalog!.AllEntries()) entries.Add(entry.StableId, entry);
                    registry = new ContentRegistry(prepared.Catalog, prepared.Graph!, entries);
                }
                catch (Exception exception)
                {
                    report.Error($"Content registry publication failed: {exception.Message}");
                }
            }
        }
        finally
        {
            try
            {
                foreach (var error in logger.Errors)
                    report.Error("Content registry engine error: " + error);
            }
            catch (Exception exception)
            {
                report.Error($"Content registry logger read failed: {exception.Message}");
            }

            if (loggerRegistrationAttempted)
            {
                try { Godot.OS.RemoveLogger(logger); }
                catch (Exception exception) { report.Error($"Content registry logger removal failed: {exception.Message}"); }
            }
            try { logger.Dispose(); }
            catch (Exception exception) { report.Error($"Content registry logger disposal failed: {exception.Message}"); }
        }

        return report.HasCoreErrors || registry is null || prepared?.Payload is null
            ? new ContentPublicationGateResult<T>(null, null, report, 0)
            : new ContentPublicationGateResult<T>(
                registry,
                prepared.Payload,
                report,
                CurrentPublicationVersion);
    }

    private static bool HasCapturedEngineErrors(
        ContentReadyGateLogger logger,
        ValidationReport report,
        string stage)
    {
        try { return logger.Errors.Count > 0; }
        catch (Exception exception)
        {
            report.Error($"Content registry logger read failed {stage}: {exception.Message}");
            return true;
        }
    }
}

internal sealed record ContentPublicationPreparation<T>(
    ContentCatalog? Catalog,
    CompiledContentGraph? Graph,
    T? Payload,
    ValidationReport Report)
    where T : class;

internal sealed record ContentPublicationGateResult<T>(
    ContentRegistry? Registry,
    T? Payload,
    ValidationReport Report,
    int PublishedVersion)
    where T : class;
