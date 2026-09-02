using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public sealed record ArmyOverviewViewModel(
    float RosterHealthRatio,
    int Deployed,
    int CurrentPopulation,
    int EffectivePopulationCap,
    int Reserve,
    int ItemCount,
    int Gold,
    IReadOnlyList<ArmyOverviewRowViewModel> RosterHeroes,
    IReadOnlyList<ArmyOverviewRowViewModel> Items,
    IReadOnlyList<ArmyOverviewRowViewModel> TacticalCommands);

public sealed record ArmyOverviewRowViewModel(
    string Title,
    string Details,
    string Footer,
    UnitPortraitDefinition? Portrait = null,
    Texture2D? Icon = null,
    UnitRole? Role = null,
    bool IsHero = false,
    IReadOnlyList<SemanticFact>? Facts = null,
    int TacticalPointCost = 0,
    int GoldCost = 0);

public static class ArmyOverviewFactory
{
    public static ArmyOverviewViewModel Build(
        ActiveRunDto run,
        ContentRegistry content,
        CompiledRunRules rules)
    {
        var rosterHeroes = run.Roster.Select((instance, index) =>
        {
            var entry = Required(content, instance.ContentId);
            var definition = (UnitDefinition)entry.Definition;
            var slot = run.Deployment.IndexOf(instance.InstanceId);
            var state = slot >= 0
                ? $"已部署 · ({BattlefieldLayout.PlayerDeploymentCells[slot].X},{BattlefieldLayout.PlayerDeploymentCells[slot].Y})"
                : "后备";
            var detailsBase = index == 0
                ? StartingHeroCompatibility(entry, definition)
                : definition.Description;
            var equipmentNames = instance.Equipment
                .OrderBy(equipment => equipment.SlotIndex)
                .Select(equipment => ((ItemDefinition)Required(content, equipment.ContentId).Definition).DisplayName)
                .ToArray();
            var details = equipmentNames.Length == 0
                ? detailsBase
                : string.Join('\n', detailsBase, $"装备：{string.Join("、", equipmentNames)}");
            return new ArmyOverviewRowViewModel(
                definition.DisplayName,
                details,
                state,
                definition.Portrait, Role: definition.Role, IsHero: true,
                Facts: [UnitSemanticFacts.Health(instance.HealthRatio.ToString("P0")),
                    UnitSemanticFacts.Responsibility(definition.Role, includeLabel: false),
                    UnitSemanticFacts.Reach(definition.AttackRange, includeLabel: false)]);
        }).ToArray();

        var items = run.Items.Select(instance =>
        {
            var definition = (ItemDefinition)Required(content, instance.ContentId).Definition;
            return new ArmyOverviewRowViewModel(definition.DisplayName, definition.Description,
                $"{PlayerFacingText.DescribeItemRarity(definition.Rarity)} · 数量 {Math.Max(1, instance.Stacks)}",
                Icon: definition.Icon ?? SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Loot));
        }).ToArray();

        var tacticalCommands = run.EquippedTacticalCommandIds.Select((id, index) =>
        {
            var command = content.Graph.ResolveTacticalCommand(id);
            var runtimeRules = new List<string>();
            if (command.CooldownTicks > 0)
                runtimeRules.Add($"冷却 {command.CooldownTicks * BattleTiming.TickSeconds:0.0} 秒");
            if (command.MaxUses > 0) runtimeRules.Add($"每场最多 {command.MaxUses} 次");
            runtimeRules.Add("战术点每场重置");
            return new ArmyOverviewRowViewModel(
                $"槽位 {index + 1} · {command.DisplayName}",
                command.Description,
                string.Join(" · ", runtimeRules),
                Icon: SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.TacticalPoint),
                TacticalPointCost: command.TacticalPointCost,
                GoldCost: command.GoldCost);
        }).ToArray();

        var deployed = run.Deployment.Count(id => !string.IsNullOrEmpty(id));
        var reserve = Math.Max(0, run.Roster.Count - deployed);
        var population = RunPopulationPolicy.Evaluate(run, rules);
        var averageHealth = run.Roster.Count == 0 ? 0 : run.Roster.Average(hero => hero.HealthRatio);
        return new ArmyOverviewViewModel(averageHealth, deployed, population.CurrentPopulation,
            population.EffectivePopulationCap, reserve, run.Items.Count, run.Gold,
            rosterHeroes, items, tacticalCommands);
    }

    private static CatalogEntry Required(ContentRegistry content, string id) =>
        content.TryGet(id, out var entry) ? entry : throw new InvalidOperationException("Missing content: " + id);

    private static string StartingHeroCompatibility(
        CatalogEntry entry,
        UnitDefinition definition)
    {
        var root = entry.Scene.Instantiate<UnitContentRoot>();
        try
        {
            var rule = root.HeroRule;
            return string.Join('\n', new[]
            {
                definition.Description,
                rule is null ? string.Empty : $"{rule.RuleTitle}：{rule.RuleDescription}"
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
        }
        finally
        {
            root.Free();
        }
    }
}
