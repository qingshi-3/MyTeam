using System.Collections.Generic;

namespace TowerAutobattler.Run;

public enum TowerNodeType { Combat, Elite, Recruitment, Shop, Event, Rest, Boss }

public sealed class UnitInstanceDto
{
    public string InstanceId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public float HealthRatio { get; set; } = 1f;
    public int Rank { get; set; } = 1;
}

public sealed class ItemInstanceDto
{
    public string InstanceId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public int Stacks { get; set; } = 1;
    public int Charges { get; set; }
    public int Roll { get; set; }
}

public sealed class ActiveRunDto
{
    public int Version { get; set; } = 2;
    public ulong Seed { get; set; }
    public string HeroId { get; set; } = string.Empty;
    public float HeroHealthRatio { get; set; } = 1f;
    public List<UnitInstanceDto> Roster { get; set; } = [];
    public List<string> Deployment { get; set; } = ["", "", "", "", "", ""];
    public List<ItemInstanceDto> Items { get; set; } = [];
    public int Gold { get; set; } = 16;
    public int FloorIndex { get; set; }
    public int BattleNumber { get; set; }
    public bool PendingNode { get; set; }
    public TowerNodeType SelectedNode { get; set; }
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

public sealed record TowerNodeOption(TowerNodeType Type, string Title, string Description, int Risk);
public sealed record EncounterPlan(string Title, string FloorRuleId, IReadOnlyList<string> EnemyIds, bool IsBoss, bool IsElite);
