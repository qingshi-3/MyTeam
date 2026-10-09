using System;
using System.Collections.Generic;
using TowerAutobattler.Composition;

namespace TowerAutobattler.Run;

// Profile routing precedes construction of progression, economy, and Meta services.
// A successful route does not validate or migrate the untrusted save body.
public static class RunSessionFactory
{
    public static RunSessionResolution ResolveProfile(
        CompiledGamePackage package,
        IRunSaveService save,
        string saveNamespace,
        IReadOnlyDictionary<string, string>? legacyNamespaceMappings = null)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(save);
        ActiveRunDto? loaded;
        string? readError;
        try
        {
            loaded = save.LoadActiveRun();
            readError = save.ActiveRunReadError;
        }
        catch (Exception exception)
        {
            loaded = null;
            readError = $"无法读取活动征程，原文件应保留：{exception.Message}";
        }

        if (readError is not null)
            return Rejected(save, loaded, new(ActiveRunLoadFailureKind.ReadFailed, readError));
        if (loaded is null)
            return new(package.DefaultProfile, null, null, new LoadedRunSaveService(save, null, null));

        var header = new RunProfileHeader(loaded.Version, loaded.ProfileId, loaded.ContentFingerprint,
            loaded.ProfileCompatibilityVersion, loaded.ProgressionProtocolVersion,
            loaded.RewardProtocolVersion, loaded.IdentityProtocolVersion);
        if (!RunProfileResolver.TryResolve(package, saveNamespace, header, out var profile, out var error,
                legacyNamespaceMappings))
            return Rejected(save, loaded, new(
                loaded.Version < RunProfileResolver.FirstProfileSchemaVersion
                    ? ActiveRunLoadFailureKind.MigrationRejected
                    : ActiveRunLoadFailureKind.ValidationRejected,
                error));

        return new(profile, loaded, null, new LoadedRunSaveService(save, loaded, null));
    }

    private static RunSessionResolution Rejected(IRunSaveService save, ActiveRunDto? loaded,
        ActiveRunLoadDiagnostic diagnostic) =>
        new(null, loaded, diagnostic, new LoadedRunSaveService(save, loaded, diagnostic.Message));
}

public sealed class RunSessionResolution
{
    internal RunSessionResolution(CompiledRunProfile? profile, ActiveRunDto? loadedRun,
        ActiveRunLoadDiagnostic? diagnostic, IRunSaveService saveService)
    {
        Profile = profile;
        LoadedRun = loadedRun;
        Diagnostic = diagnostic;
        SaveService = saveService;
    }

    public CompiledRunProfile? Profile { get; }
    // This is the original read result. The persistence boundary must clone it
    // before structural migration or validation, as it does for direct reads.
    public ActiveRunDto? LoadedRun { get; }
    public ActiveRunLoadDiagnostic? Diagnostic { get; }
    public bool IsSuccess => Profile is not null && Diagnostic is null;
    // Use once when constructing the selected session. This adapter prevents a
    // second disk read between routing and persistence; it performs no implicit writes.
    public IRunSaveService SaveService { get; }
}

// Replays exactly one load, then resumes normal storage behavior. All persistence
// commands stay explicit and are delegated to the existing transactional owner.
internal sealed class LoadedRunSaveService : IRunSaveService
{
    private readonly IRunSaveService _inner;
    private ActiveRunDto? _loadedRun;
    private bool _hasUnreadSnapshot = true;

    public LoadedRunSaveService(IRunSaveService inner, ActiveRunDto? loadedRun, string? readError)
    {
        _inner = inner;
        _loadedRun = loadedRun;
        ActiveRunReadError = readError;
    }

    public string? ActiveRunReadError { get; private set; }
    public MetaProgressDto LoadMeta() => _inner.LoadMeta();
    public SettingsDto LoadSettings() => _inner.LoadSettings();

    public ActiveRunDto? LoadActiveRun()
    {
        if (_hasUnreadSnapshot)
        {
            _hasUnreadSnapshot = false;
            var result = _loadedRun;
            _loadedRun = null;
            return result;
        }

        var loaded = _inner.LoadActiveRun();
        ActiveRunReadError = _inner.ActiveRunReadError;
        return loaded;
    }

    public bool SaveMeta(MetaProgressDto value) => _inner.SaveMeta(value);
    public bool SaveSettings(SettingsDto value) => _inner.SaveSettings(value);

    public bool SaveActiveRun(ActiveRunDto value)
    {
        if (!_inner.SaveActiveRun(value)) return false;
        DiscardSnapshot();
        return true;
    }

    public void DeleteActiveRun()
    {
        _inner.DeleteActiveRun();
        DiscardSnapshot();
    }

    private void DiscardSnapshot()
    {
        _hasUnreadSnapshot = false;
        _loadedRun = null;
        ActiveRunReadError = null;
    }
}
