using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

// Player-facing rules come from the frozen executable choice, not authored flavor text.
public static class RunDecisionText
{
    public static string Describe(CompiledRunChoice choice, Func<string, string> contentName)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(choice.Description)) lines.Add(choice.Description);
        if (!choice.Conditions.IsDefaultOrEmpty)
            lines.Add("需要：" + string.Join("；", choice.Conditions.Select(condition => Condition(condition, contentName))));
        if (!choice.Costs.IsDefaultOrEmpty)
            lines.Add((choice.SuccessChance < 1 ? "代价（无论成败）：" : "代价：") + Costs(choice, contentName));
        if (choice.SuccessChance >= 1)
            lines.Add("结果：" + Operations(choice.Operations, contentName));
        else
        {
            lines.Add($"成功 {Percent(choice.SuccessChance)}：" + Operations(choice.Operations, contentName));
            lines.Add($"失败 {Percent(1 - choice.SuccessChance)}：" + Operations(choice.FailureOperations, contentName));
        }
        return string.Join("\n", lines);
    }

    public static string Costs(CompiledRunChoice choice, Func<string, string> contentName) =>
        choice.Costs.IsDefaultOrEmpty ? "无代价" :
            string.Join("；", choice.Costs.Select(operation => Operation(operation, contentName, true)));

    public static string Condition(CompiledRunCondition condition, Func<string, string> contentName) => condition.Kind switch
    {
        RunConditionKind.GoldAtLeast => $"持有至少 {condition.Amount} 金币",
        RunConditionKind.RosterHealthAtLeast => $"全队每名英雄生命不低于 {Percent(condition.Ratio)}",
        RunConditionKind.HasContent => $"拥有 {contentName(condition.ContentId)} × {condition.Amount}（含已穿戴）",
        RunConditionKind.PopulationBelowCap => "当前人口低于人口上限",
        RunConditionKind.StartingHeroIs => $"初始英雄为 {contentName(condition.ContentId)}",
        _ => throw new ArgumentOutOfRangeException(nameof(condition), "Unsupported Run condition.")
    };

    public static string Operation(CompiledRunOperation operation, Func<string, string> contentName, bool cost = false) => operation.Kind switch
    {
        RunOperationKind.GainGold => $"获得 {operation.Amount} 金币",
        RunOperationKind.SpendGold => $"支付 {operation.Amount} 金币",
        RunOperationKind.GrantItem => $"获得 {contentName(operation.ContentId)} × {operation.Amount}",
        RunOperationKind.Recruit => $"{contentName(operation.ContentId)} × {operation.Amount} 加入队伍",
        RunOperationKind.RecoverRoster => Health(operation, cost),
        RunOperationKind.GrantPopulation => $"人口增加 {operation.Amount}（不可超过上限）",
        RunOperationKind.IncreasePopulationCap => $"人口上限增加 {operation.Amount}（同一来源仅一次，不超过部署容量）",
        _ => throw new ArgumentOutOfRangeException(nameof(operation), "Unsupported Run operation.")
    };

    private static string Operations(System.Collections.Immutable.ImmutableArray<CompiledRunOperation> operations,
        Func<string, string> contentName) => operations.IsDefaultOrEmpty ? "无额外变化" :
        string.Join("；", operations.Select(operation => Operation(operation, contentName)));

    private static string Health(CompiledRunOperation operation, bool cost)
    {
        var target = operation.Target switch
        {
            RunRosterTarget.All => "全队每名英雄",
            RunRosterTarget.StartingHero => "初始英雄",
            RunRosterTarget.OtherHeroes => "除初始英雄外每名英雄",
            _ => throw new ArgumentOutOfRangeException(nameof(operation), "Unsupported Run health target.")
        };
        var change = operation.Ratio < 0 ? "失去" : "恢复";
        var boundary = cost
            ? $"扣除后须保有至少 {Percent(operation.MinimumHealthRatio)} 生命"
            : $"结算后生命限制在 {Percent(operation.MinimumHealthRatio)}–100%";
        return $"{target}{change}最大生命的 {Percent(Math.Abs(operation.Ratio))}（{boundary}）";
    }

    private static string Percent(float value) => (value * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%";
}
