using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Content;
using TowerAutobattler.Expedition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

namespace TowerAutobattler.App;

// Joins the published content catalogue with the already-redacted map read port.
// The facade remains the only authority for paths, costs and action eligibility.
internal static class ExpeditionScreenPresenter
{
    public static ExpeditionScreenModel Build(RunApplication app, SemanticIconCatalog icons, int selectedCellIndex)
    {
        if (app.Run is not { } run || app.Expedition is not { } map ||
            app.Project.Campaign.Progression is not CompiledExpedition definition)
            throw new InvalidOperationException("No active expedition presentation context.");
        var stage = definition.Stages[map.StageIndex];
        var boss = definition.Encounters[stage.BossProfileId];
        var selected = selectedCellIndex >= 0 && selectedCellIndex < map.Cells.Length
            ? selectedCellIndex : map.PositionIndex;
        var selectedCell = map.Cells.First(cell => cell.Index == selected);
        var selectedCanMove = selectedCell.Revealed && app.CanMoveExpedition(selected);
        var cells = map.Cells.Select(cell => Cell(app, icons, definition, cell, selected, selectedCanMove)).ToArray();
        return new()
        {
            MapId = $"{run.RunId}/{definition.StableId}",
            Title = "迷雾大陆",
            StageText = $"第 {map.StageIndex + 1} / {definition.Stages.Length} 阶段 · {stage.DisplayName}",
            Columns = map.Size,
            Rows = map.Size,
            Cells = cells,
            PlayerCellIndex = map.PositionIndex,
            SelectedCellIndex = selected,
            SelectedDetails = selectedCell.Revealed ? Details(app, icons, definition, selectedCell) : null,
            RemainingActions = map.RemainingActions,
            ActionBudget = stage.Actions,
            ExplorationHint = map.RemainingActions > 0
                ? "方向键 / WASD 移动 · 回车交互。移动不消耗行动，遇到守军可换路绕行。"
                : "本阶段行动已用尽。整理已有的军团与资源，准备迎战首领。",
            BossName = boss.Composition.DisplayName,
            BossSummary = map.RemainingActions > 0
                ? $"可继续探索，或提前迎战。提前迎战会放弃余下 {map.RemainingActions} 行动。"
                : "首领战已经到来；战败或超时将结束征程。",
            BossActionText = map.RemainingActions > 0 ? "提前迎战 · 查看敌军" : "迎战首领 · 查看敌军",
            CanChallengeBoss = app.CanChallengeExpeditionBoss,
            BossDisabledReason = app.CanChallengeExpeditionBoss ? string.Empty : "先完成当前待处理事项。",
            InteractionLocked = app.ExpeditionInteractionLocked,
            CanOpenArmy = !app.ExpeditionBattleCommitted,
            CanOpenMenu = !app.ExpeditionBattleCommitted
        };
    }

    private static ExpeditionCellModel Cell(RunApplication app, SemanticIconCatalog icons,
        CompiledExpedition definition, ExpeditionCellView cell, int selected, bool canMove)
    {
        if (!cell.Revealed) return new() { Index = cell.Index };
        var biome = definition.Biomes[cell.BiomeIndex];
        var site = definition.Sites.FirstOrDefault(site => site.StableId == cell.DefinitionId);
        var encounter = definition.Encounters.GetValueOrDefault(cell.EncounterProfileId);
        var appearance = Appearance(site?.Kind);
        var terrain = cell.Walkable ? cell.BiomeIndex switch
        {
            1 => ExpeditionTerrainAppearance.Forest,
            2 => ExpeditionTerrainAppearance.Ruins,
            _ => ExpeditionTerrainAppearance.Grassland
        } : ExpeditionTerrainAppearance.Mountain;
        return new()
        {
            Index = cell.Index,
            IsRevealed = true,
            Terrain = terrain,
            TerrainName = cell.Walkable ? "可通行地带" : "险峻地形",
            RegionName = biome.DisplayName,
            SiteAppearance = appearance,
            SiteId = cell.SiteId,
            SiteName = encounter?.Composition.DisplayName ?? site?.DisplayName ?? string.Empty,
            SiteIcon = site is null ? null : icons.ResolveIcon(SemanticIconKeys.TowerNodeSemantic(site.Kind)),
            IsCompleted = cell.Cleared || cell.Handled,
            IsBlocked = cell.Blocked,
            CanMove = cell.Index == selected && canMove,
            MoveBlockedReason = cell.Index != selected || canMove ? string.Empty : MovementReason(app, cell),
            StateText = cell.Cleared ? "已击败" : cell.Handled ? site?.Kind == TowerNodeType.Shop ? "已交易 · 可回访" : "已处理"
                : cell.Blocked && cell.Walkable ? "守军阻挡通路" : cell.Walkable ? "道路已发现" : "不可通行"
        };
    }

    private static ExpeditionSiteDetails? Details(RunApplication app, SemanticIconCatalog icons,
        CompiledExpedition definition, ExpeditionCellView cell)
    {
        var site = definition.Sites.FirstOrDefault(candidate => candidate.StableId == cell.DefinitionId);
        if (site is null) return null;
        var encounter = definition.Encounters.GetValueOrDefault(cell.EncounterProfileId);
        var actionable = app.CanInteractExpeditionSite(cell.SiteId);
        var offer = encounter is null ? app.PreviewExpeditionSiteOffer(cell.SiteId) : null;
        var description = encounter is not null
            ? DescribeEncounter(app, encounter)
            : offer is not null ? DescribeOffer(app, offer) : "此处的机会已经处理完毕。";
        return new()
        {
            CellIndex = cell.Index,
            SiteId = cell.SiteId,
            Title = encounter?.Composition.DisplayName ?? site.DisplayName,
            Subtitle = definition.Biomes[cell.BiomeIndex].DisplayName + " · " + site.DisplayName,
            Description = description,
            CostText = cell.Cleared || (cell.Handled && site.Kind != TowerNodeType.Shop) ? string.Empty
                : site.Kind == TowerNodeType.Shop
                    ? cell.Handled ? "后续购买不消耗行动；仍需支付商品费用。" : $"首次成功交易消耗 {site.ActionCost} 行动；仅查看不消耗。"
                    : $"确认{(encounter is null ? "处理" : "开战")}时消耗 {site.ActionCost} 行动。",
            RewardText = encounter is not null && !cell.Cleared ? DescribeReward(encounter) : string.Empty,
            RiskText = encounter is not null && !cell.Cleared ? RunHealthText.Risk(app.Rules, site.Kind) : string.Empty,
            AvailabilityText = actionable ? encounter is null ? "可查看完整选项，再决定是否处理。" : "可进入整备查看实际敌阵，也可以暂时离开。"
                : cell.Cleared ? "此处守军已被击败，道路已经开放。"
                : cell.Handled && site.Kind != TowerNodeType.Shop ? "此处已完成，可以继续探索。"
                : app.ExpeditionRemainingActions <= 0 ? "本阶段行动已用尽，请准备首领战。"
                : app.ExpeditionInteractionLocked ? "先完成当前待处理事项。"
                : "移动到此处或相邻地块后，再处理这里的遭遇。",
            InspectLabel = encounter is null ? site.Kind == TowerNodeType.Shop ? "查看商品" : "查看可选行动" : "查看敌军 · 进入整备",
            CanInspect = actionable,
            Icon = icons.ResolveIcon(SemanticIconKeys.TowerNodeSemantic(site.Kind))
        };
    }

    private static string DescribeEncounter(RunApplication app, CompiledExpeditionEncounter encounter)
    {
        var members = encounter.Composition.EnemyIds.GroupBy(id => id)
            .Select(group => $"{ContentName(app, group.Key)} × {group.Count()}");
        return (string.IsNullOrWhiteSpace(encounter.Composition.PressureSummary)
            ? string.Empty : encounter.Composition.PressureSummary + "\n\n") + "敌军\n" + string.Join("\n", members);
    }

    private static string DescribeOffer(RunApplication app, PendingRunOffer offer)
    {
        if (offer.Choices.Length == 0) return "这里暂时没有可取得的内容。";
        return string.Join("\n\n", offer.Choices.Select(choice =>
            choice.DisplayName + "\n" + RunDecisionText.Describe(choice, id => ContentName(app, id))));
    }

    private static string DescribeReward(CompiledExpeditionEncounter encounter)
    {
        var parts = new List<string>();
        var gold = encounter.Reward.Gold + encounter.Composition.RewardGold;
        if (gold > 0) parts.Add($"金币 {gold}");
        if (encounter.Composition.RewardResearch > 0) parts.Add($"研究 {encounter.Composition.RewardResearch}");
        if (encounter.Composition.RewardMaterials > 0) parts.Add($"材料 {encounter.Composition.RewardMaterials}");
        parts.AddRange(encounter.Reward.Groups.Select(group => group.RewardCategory == RunRewardCategory.Equipment ? "装备机会" : "遗物机会"));
        return parts.Count == 0 ? string.Empty : "胜利收益：" + string.Join(" · ", parts);
    }

    private static string ContentName(RunApplication app, string id) => app.Content.TryGet(id, out var entry)
        ? entry.Definition switch { UnitDefinition unit => unit.DisplayName, ItemDefinition item => item.DisplayName, _ => id }
        : id;

    private static string MovementReason(RunApplication app, ExpeditionCellView cell) => !cell.Walkable
        ? "此处地形无法通行。"
        : cell.Blocked ? "守军挡住通路；迎战或寻找其他道路。"
        : app.ExpeditionInteractionLocked ? "先完成当前待处理事项。"
        : app.ExpeditionRemainingActions <= 0 ? "本阶段探索已结束，请准备迎战首领。"
        : app.Expedition?.PositionIndex == cell.Index ? "军团当前所在位置。"
        : "尚无可到达这里的已知道路。";

    private static ExpeditionSiteAppearance Appearance(TowerNodeType? type) => type switch
    {
        TowerNodeType.Combat => ExpeditionSiteAppearance.Combat,
        TowerNodeType.Elite => ExpeditionSiteAppearance.Elite,
        TowerNodeType.Recruitment => ExpeditionSiteAppearance.Recruitment,
        TowerNodeType.Shop => ExpeditionSiteAppearance.Shop,
        TowerNodeType.Rest => ExpeditionSiteAppearance.Camp,
        TowerNodeType.Event => ExpeditionSiteAppearance.Ruins,
        _ => ExpeditionSiteAppearance.None
    };
}
