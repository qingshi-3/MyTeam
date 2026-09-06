using System.Text.Json;
using TowerAutobattler.Run;

namespace TowerAutobattler.Experience;

// No path, filesystem service, or production save fallback exists in an experiment session.
public sealed class ExperienceMemoryStore : IRunSaveService
{
    private string? _run;
    private string _meta = JsonSerializer.Serialize(new MetaProgressDto());
    private string _settings = JsonSerializer.Serialize(new SettingsDto());
    public MetaProgressDto LoadMeta() => JsonSerializer.Deserialize<MetaProgressDto>(_meta)!;
    public SettingsDto LoadSettings() => JsonSerializer.Deserialize<SettingsDto>(_settings)!;
    public ActiveRunDto? LoadActiveRun() => _run is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(_run);
    public bool SaveMeta(MetaProgressDto value) { _meta = JsonSerializer.Serialize(value); return true; }
    public bool SaveSettings(SettingsDto value) { _settings = JsonSerializer.Serialize(value); return true; }
    public bool SaveActiveRun(ActiveRunDto value) { _run = JsonSerializer.Serialize(value); return true; }
    public void DeleteActiveRun() => _run = null;
    public static ActiveRunDto Clone(ActiveRunDto run) => JsonSerializer.Deserialize<ActiveRunDto>(JsonSerializer.Serialize(run))!;
}
