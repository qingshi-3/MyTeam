using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TowerAutobattler.Content;

public static class ProductionSourceGuard
{
    public static ImmutableArray<string> GuardedConcreteIdFamilies { get; } =
    [
        "hero",
        "soldier",
        "enemy",
        "item",
        "rule",
        "effect",
        "ability",
        "status",
        "relic",
        "equipment",
        "tactical",
        "trait",
        "encounter",
        "campaign",
        "project",
        "pool",
        "phase",
        "timeline",
        "region",
        "reward",
        "loadout"
    ];

    private static readonly Regex ConcreteId = new(
        $"\"(?<id>(?:{string.Join("|", GuardedConcreteIdFamilies.Select(Regex.Escape))})_[a-z0-9]+(?:_[a-z0-9]+)*)\"",
        RegexOptions.CultureInvariant);
    private static readonly Regex DonorAbsolutePath = new(
        @"(?<![a-z0-9_])[a-z]:(?:\\+|/+)godot(?:\\+|/+)rpg(?=$|\\|/|""|')",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private const string StableIdExpression =
        @"(?:(?:\b[A-Za-z_][A-Za-z0-9_]*\s*(?:\?\s*\.\s*|\.\s*))*(?:Id|StableId|ContentId)|\b(?:id|stableId|contentId))";
    private const string MemberAccess = @"(?:\?\s*\.\s*|\.\s*)";
    private const string CSharpStringStart = @"(?:(?:@|\$@|@\$|\$)?""|\$*""{3,})";
    private static readonly (string Name, Regex Pattern)[] StableIdInferencePatterns =
    [
        ("StartsWith", StableIdOperation(@"StartsWith\s*\(")),
        ("Substring", StableIdOperation(@"Substring\s*\(")),
        ("Split", StableIdOperation(@"Split\s*\(")),
        ("slice", new Regex(
            $@"{StableIdExpression}\s*\[\s*[^\]\r\n]*\.\.[^\]\r\n]*\]",
            RegexOptions.CultureInvariant)),
        ("IndexOf==0", new Regex(
            $@"(?:{StableIdExpression}\s*{MemberAccess}IndexOf\s*\([^;\r\n]*?\)\s*==\s*0\b|" +
            $@"\b0\s*==\s*{StableIdExpression}\s*{MemberAccess}IndexOf\s*\([^;\r\n]*?\))",
            RegexOptions.CultureInvariant)),
        ("anchored Regex", new Regex(
            $@"\bRegex\s*\.\s*(?:IsMatch|Match|Matches)\s*\(\s*{StableIdExpression}\s*,\s*{CSharpStringStart}\s*\^",
            RegexOptions.CultureInvariant)),
        ("anchored Regex", new Regex(
            $@"\bnew\s+Regex\s*\(\s*{CSharpStringStart}\s*\^[^\r\n;]*?\)\s*\.\s*(?:IsMatch|Match|Matches)\s*\(\s*{StableIdExpression}",
            RegexOptions.CultureInvariant)),
        ("named Regex", new Regex(
            $@"\b[A-Za-z_][A-Za-z0-9_]*(?:Regex|Pattern)\s*\.\s*(?:IsMatch|Match|Matches)\s*\(\s*{StableIdExpression}",
            RegexOptions.CultureInvariant))
    ];
    private static readonly HashSet<string> AllowedSemanticIds = new(StringComparer.Ordinal)
    {
        "tactical_command",
        "tactical_point"
    };
    private static readonly string[] ForbiddenGlobalDiscovery =
    [
        "/" + "root",
        "Current" + "Scene",
        "GetTree()." + "Root",
        "GetTree().Get" + "Root(",
        "SceneTree." + "Root",
        "GetFirstNodeIn" + "Group",
        "GetNodesIn" + "Group",
        "Call" + "Group",
        "Get" + "Groups",
        "AddTo" + "Group",
        "IsIn" + "Group"
    ];
    private static readonly string[] ForbiddenLocalTreeTraversal =
    [
        "Get" + "Parent(",
        ".." + "/",
        "NodePath(\"" + "..",
        "GetNode(\"" + ".."
    ];

    public static IReadOnlyList<string> FindIssues(
        string source,
        bool checkDiscovery,
        bool checkLocalTreeTraversal = false)
    {
        var issues = new List<string>();
        foreach (Match match in ConcreteId.Matches(source))
        {
            var id = match.Groups["id"].Value;
            if (!AllowedSemanticIds.Contains(id)) issues.Add($"concrete id '{id}'");
        }
        if (DonorAbsolutePath.IsMatch(source)) issues.Add("donor absolute path");

        if (checkDiscovery)
            foreach (var forbidden in ForbiddenGlobalDiscovery)
                if (source.Contains(forbidden, StringComparison.Ordinal)) issues.Add($"forbidden discovery '{forbidden}'");
        if (checkLocalTreeTraversal)
            foreach (var forbidden in ForbiddenLocalTreeTraversal)
                if (source.Contains(forbidden, StringComparison.Ordinal)) issues.Add($"forbidden local tree traversal '{forbidden}'");
        return issues;
    }

    public static IReadOnlyList<string> FindStableIdInferenceIssues(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return StableIdInferencePatterns
            .Where(candidate => candidate.Pattern.IsMatch(source))
            .Select(candidate => $"stable-id inference '{candidate.Name}'")
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static Regex StableIdOperation(string operation) => new(
        $@"{StableIdExpression}\s*{MemberAccess}{operation}",
        RegexOptions.CultureInvariant);
}
