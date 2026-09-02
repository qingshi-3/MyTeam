using System.Collections.Generic;
using System.Text.Json.Serialization;
using TowerAutobattler.Equipment;

namespace TowerAutobattler.Run;

public sealed class RosterHeroInstanceDto
{
    public string InstanceId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public float HealthRatio { get; set; } = 1f;
    public int Rank { get; set; } = 1;
    public List<EquipmentInstanceState> Equipment { get; set; } = [];
}

public sealed class ItemInstanceDto
{
    public string InstanceId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public int Stacks { get; set; } = 1;
    public int Charges { get; set; }
    public int Roll { get; set; }
    public List<RelicCounterStateDto> Counters { get; set; } = [];
}

public sealed class RelicCounterStateDto
{
    public string CounterId { get; set; } = string.Empty;
    public int Value { get; set; }
}

public sealed class PopulationCapSourceDto
{
    public string SourceId { get; set; } = string.Empty;
    public int Amount { get; set; }
}

public sealed class ActiveRunDto
{
    public int Version { get; set; } = ActiveRunFormationSchema.CurrentVersion;
    public ulong Seed { get; set; }
    public List<RosterHeroInstanceDto> Roster { get; set; } = [];
    public int CurrentPopulation { get; set; } = 1;
    public List<PopulationCapSourceDto> PopulationCapSources { get; set; } = [];
    public List<string> Deployment { get; set; } = ActiveRunFormationSchema.EmptyDeployment();
    public List<ItemInstanceDto> Items { get; set; } = [];
    public List<string> EquippedTacticalCommandIds { get; set; } = [];
    public int Gold { get; set; } = 16;
    public int FloorIndex { get; set; }
    public int BattleNumber { get; set; }
    public bool PendingNode { get; set; }
    public TowerNodeType SelectedNode { get; set; }

    // Schema-v3 compatibility input only. A successful migration clears these
    // fields so schema-v4 saves publish only the unified roster/formation model.
    [JsonPropertyName("HeroId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyHeroId { get; set; }

    [JsonPropertyName("HeroHealthRatio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float LegacyHeroHealthRatio { get; set; }

    [JsonPropertyName("HeroCell")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FormationCellDto? LegacyHeroCell { get; set; }

    [JsonPropertyName("DeploymentCells")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FormationCellDto>? LegacyDeploymentCells { get; set; }
}

public sealed class MetaProgressDto
{
    public int Version { get; set; } = 1;
    public List<string> UnlockedHeroIds { get; set; } = [];
    public int Victories { get; set; }
    public int HighestRegion { get; set; }
}

public sealed class SettingsDto
{
    public int Version { get; set; } = 1;
    public float MasterVolume { get; set; } = .8f;
    public float DefaultBattleSpeed { get; set; } = 1f;
    public bool ShowDamageNumbers { get; set; } = true;
}

public enum ActiveRunLoadFailureKind
{
    MigrationRejected,
    ValidationRejected,
    MigrationPublicationFailed
}

public sealed record ActiveRunLoadDiagnostic(ActiveRunLoadFailureKind Kind, string Message);

public sealed record TowerNodeOption(TowerNodeType Type, string Title, string Description, int Risk);
public sealed record EncounterPlan(
    string Title,
    string FloorRuleId,
    IReadOnlyList<string> EnemyIds,
    bool IsBoss,
    bool IsElite,
    string EncounterId = "",
    TowerNodeType NodeType = TowerNodeType.Combat);
