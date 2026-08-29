using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public sealed record ArmyOverviewViewModel(
    string Summary,
    ArmyOverviewRowViewModel Hero,
    IReadOnlyList<ArmyOverviewRowViewModel> Soldiers,
    IReadOnlyList<ArmyOverviewRowViewModel> Items);

public sealed record ArmyOverviewRowViewModel(
    string Title,
    string Details,
    string Footer,
    UnitPortraitDefinition? Portrait = null,
    UnitRole? Role = null,
    bool IsHero = false,
    IReadOnlyList<SemanticFact>? Facts = null,
    int ManaCost = 0,
    int GoldCost = 0);

public static class ArmyOverviewFactory
{
    public static ArmyOverviewViewModel Build(ActiveRunDto run, ContentRegistry content)
    {
        var heroEntry = Required(content, run.HeroId);
        var heroDefinition = (UnitDefinition)heroEntry.Definition;
        var heroRoot = heroEntry.Scene.Instantiate<UnitContentRoot>();
        ArmyOverviewRowViewModel hero;
        try
        {
            var rule = heroRoot.HeroRule;
            var command = heroRoot.HeroCommand;
            var ruleText = rule is null ? heroDefinition.Description : $"军团规则·{rule.RuleTitle}：{rule.RuleDescription}";
            var commandText = command is null
                ? "无战场指令"
                : $"战场指令·{command.DisplayName}：{command.Description}";
            hero = new ArmyOverviewRowViewModel(
                $"★ {heroDefinition.DisplayName}",
                $"{ruleText}\n{commandText}", string.Empty, heroDefinition.Portrait, heroDefinition.Role, true,
                [UnitSemanticFacts.Health(run.HeroHealthRatio.ToString("P0")),
                    new SemanticFact(SemanticIconKeys.Mana, $"MP 上限 {rule?.MaxMana ?? 0}", "ManaValue")],
                command?.ManaCost ?? 0,
                command?.GoldCost ?? 0);
        }
        finally { heroRoot.Free(); }

        var soldiers = run.Roster.Select(instance =>
        {
            var definition = (UnitDefinition)Required(content, instance.ContentId).Definition;
            var slot = run.Deployment.IndexOf(instance.InstanceId);
            var state = slot >= 0 ? $"已部署 · 槽位 {slot + 1}" : "候命";
            return new ArmyOverviewRowViewModel(
                definition.DisplayName,
                definition.Description,
                state,
                definition.Portrait, definition.Role, false,
                [UnitSemanticFacts.Health(instance.HealthRatio.ToString("P0")),
                    UnitSemanticFacts.Responsibility(definition.Role, includeLabel: false),
                    UnitSemanticFacts.Reach(definition.AttackRange, includeLabel: false)]);
        }).ToArray();

        var items = run.Items.Select(instance =>
        {
            var definition = (ItemDefinition)Required(content, instance.ContentId).Definition;
            return new ArmyOverviewRowViewModel(definition.DisplayName, definition.Description,
                $"{PlayerFacingText.DescribeItemRarity(definition.Rarity)} · 数量 {Math.Max(1, instance.Stacks)}");
        }).ToArray();

        var deployed = run.Deployment.Count(id => !string.IsNullOrEmpty(id));
        var reserve = Math.Max(0, run.Roster.Count - deployed);
        var summary = $"英雄 {run.HeroHealthRatio:P0} · 部署 {deployed}/6 · 后备 {reserve} · 物品 {run.Items.Count} · 金币 {run.Gold}";
        return new ArmyOverviewViewModel(summary, hero, soldiers, items);
    }

    private static CatalogEntry Required(ContentRegistry content, string id) =>
        content.TryGet(id, out var entry) ? entry : throw new InvalidOperationException("Missing content: " + id);
}
