using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.UI;

// These are presentation snapshots. Navigation, visibility and action eligibility
// are supplied by the application; the screen never examines the mutable run.
public enum ExpeditionTerrainAppearance { Grassland, Forest, Ruins, Water, Mountain }
public enum ExpeditionSiteAppearance { None, Combat, Elite, Recruitment, Shop, Camp, Ruins }

public sealed record ExpeditionCellModel
{
    public int Index { get; init; }
    public bool IsRevealed { get; init; }
    public ExpeditionTerrainAppearance Terrain { get; init; }
    public string TerrainName { get; init; } = string.Empty;
    public string RegionName { get; init; } = string.Empty;
    public ExpeditionSiteAppearance SiteAppearance { get; init; }
    public string SiteId { get; init; } = string.Empty;
    public string SiteName { get; init; } = string.Empty;
    public Texture2D? SiteIcon { get; init; }
    public bool IsCompleted { get; init; }
    public bool IsBlocked { get; init; }
    public bool IsReachable { get; init; }
    public bool CanMove { get; init; }
    public string MoveBlockedReason { get; init; } = string.Empty;
    public string StateText { get; init; } = string.Empty;
}

public sealed record ExpeditionSiteDetails
{
    public int CellIndex { get; init; } = -1;
    public string SiteId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CostText { get; init; } = string.Empty;
    public string RewardText { get; init; } = string.Empty;
    public string RiskText { get; init; } = string.Empty;
    public string AvailabilityText { get; init; } = string.Empty;
    public string InspectLabel { get; init; } = "查看地点";
    public bool CanInspect { get; init; }
    public Texture2D? Icon { get; init; }
}

public sealed record ExpeditionScreenModel
{
    public string MapId { get; init; } = string.Empty;
    public string Title { get; init; } = "迷雾大陆";
    public string StageText { get; init; } = string.Empty;
    public int Columns { get; init; } = 16;
    public int Rows { get; init; } = 16;
    public IReadOnlyList<ExpeditionCellModel> Cells { get; init; } = [];
    public int PlayerCellIndex { get; init; } = -1;
    // -1 preserves the screen's local selection, falling back to the party.
    public int SelectedCellIndex { get; init; } = -1;
    public ExpeditionSiteDetails? SelectedDetails { get; init; }
    public int RemainingActions { get; init; }
    public int ActionBudget { get; init; }
    public string ExplorationHint { get; init; } = "方向键 / WASD 移动 · 回车交互。";
    public string BossName { get; init; } = string.Empty;
    public string BossSummary { get; init; } = string.Empty;
    public string BossActionText { get; init; } = "准备迎战";
    public bool CanChallengeBoss { get; init; }
    public string BossDisabledReason { get; init; } = string.Empty;
    public bool InteractionLocked { get; init; }
    public bool CanOpenArmy { get; init; } = true;
    public bool CanOpenMenu { get; init; } = true;
    public string StatusMessage { get; init; } = string.Empty;
}
