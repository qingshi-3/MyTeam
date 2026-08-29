using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TowerAutobattler.Content;

public static class ProductionSourceGuard
{
    private static readonly Regex ConcreteId = new(
        "\"(?<id>(?:hero|soldier|enemy|item|rule)_[a-z0-9]+(?:_[a-z0-9]+)*)\"",
        RegexOptions.CultureInvariant);
    private static readonly HashSet<string> AllowedSemanticIds = new(StringComparer.Ordinal)
    {
        "hero_command"
    };
    private static readonly string[] ForbiddenDiscovery =
    [
        "/" + "root",
        "Current" + "Scene",
        "GetTree()." + "Root",
        "GetTree().Get" + "Root(",
        "SceneTree." + "Root",
        "Get" + "Parent(",
        "GetFirstNodeIn" + "Group",
        "GetNodesIn" + "Group",
        "Call" + "Group",
        "Get" + "Groups",
        "AddTo" + "Group",
        "IsIn" + "Group",
        ".." + "/",
        "NodePath(\"" + "..",
        "GetNode(\"" + ".."
    ];

    public static IReadOnlyList<string> FindIssues(string source, bool checkDiscovery)
    {
        var issues = new List<string>();
        foreach (Match match in ConcreteId.Matches(source))
        {
            var id = match.Groups["id"].Value;
            if (!AllowedSemanticIds.Contains(id)) issues.Add($"concrete id '{id}'");
        }

        if (!checkDiscovery) return issues;
        foreach (var forbidden in ForbiddenDiscovery)
            if (source.Contains(forbidden, StringComparison.Ordinal)) issues.Add($"forbidden discovery '{forbidden}'");
        return issues;
    }
}
