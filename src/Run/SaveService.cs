using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace TowerAutobattler.Run;

public interface IRunSaveService
{
    MetaProgressDto LoadMeta();
    SettingsDto LoadSettings();
    ActiveRunDto? LoadActiveRun();
    bool SaveMeta(MetaProgressDto value);
    bool SaveSettings(SettingsDto value);
    bool SaveActiveRun(ActiveRunDto value);
    void DeleteActiveRun();
}

public sealed class SaveService : IRunSaveService
{
    private readonly string _metaPath;
    private readonly string _settingsPath;
    private readonly string _runPath;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public SaveService(string isolatedNamespace = "")
    {
        var prefix = string.IsNullOrWhiteSpace(isolatedNamespace) ? "user://" : $"user://{isolatedNamespace.Trim('/')}/";
        _metaPath = prefix + "meta.json";
        _settingsPath = prefix + "settings.json";
        _runPath = prefix + "active_run.json";
    }

    public MetaProgressDto LoadMeta() => Load(_metaPath, new MetaProgressDto());
    public SettingsDto LoadSettings() => Load(_settingsPath, new SettingsDto());
    public ActiveRunDto? LoadActiveRun() => Load<ActiveRunDto?>(_runPath, null);
    public bool SaveMeta(MetaProgressDto value) => Save(_metaPath, value);
    public bool SaveSettings(SettingsDto value) => Save(_settingsPath, value);
    public bool SaveActiveRun(ActiveRunDto value) => Save(_runPath, value);

    public void DeleteActiveRun()
    {
        var path = ProjectSettings.GlobalizePath(_runPath);
        if (File.Exists(path)) File.Delete(path);
    }

    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, _options);
    public T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _options);

    private T Load<T>(string resourcePath, T fallback)
    {
        try
        {
            var path = ProjectSettings.GlobalizePath(resourcePath);
            if (!File.Exists(path)) return fallback;
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), _options) ?? fallback;
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Save load failed for {resourcePath}: {exception.Message}");
            return fallback;
        }
    }

    private bool Save<T>(string resourcePath, T value)
    {
        try
        {
            var path = ProjectSettings.GlobalizePath(resourcePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(value, _options));
            File.Move(temporary, path, true);
            return true;
        }
        catch (Exception exception)
        {
            GD.PushError($"Save failed for {resourcePath}: {exception.Message}");
            return false;
        }
    }
}
