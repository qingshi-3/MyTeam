using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TowerAutobattler.UI;

// Presentation grammar only. Numbers bind through explicit adjacent attribute/value phrases,
// never through the nearest keyword across a sentence or a distance/time qualifier.
internal static class CombatTextMarkup
{
    private static readonly Regex AfterTerm = new(@"^\s*(?:(?:[:：×]|力|率|值|倍率|层数|上限|的|每层|减速|增加|提高|降低|减少|回复|恢复|乘以|设为|为|提升|至|达到|至少|最多|持续|从|低于|高于|等于|[，,]\s*最多额外)\s*)*$");
    private static readonly Regex BeforeTerm = new(@"^\s*(?:(?:层|点|秒)\s*)?$");
    private static readonly Regex ThresholdContinuation = new(@"^\s*(?:及)?以上跌至\s*$");
    private sealed record Token(Match Match, CombatKeyword? Term);

    public static string Line(string line, CombatKeyword[] terms, int iconSize, ISet<CombatKeyword> used)
    {
        var names = terms.SelectMany(term => term.Aliases.Prepend(term.Word)
            .Where(word => !string.IsNullOrWhiteSpace(word)).Select(word => (word, term)))
            .GroupBy(item => item.word).ToDictionary(group => group.Key, group => group.First().term);
        var pattern = string.Join("|", names.Keys.OrderByDescending(word => word.Length).Select(Regex.Escape)
            .Append(@"[+−\-]?\d+(?:\.\d+)?[%％]?(?:\s*/\s*\d+(?:\.\d+)?)?"));
        var tokens = Regex.Matches(line, pattern).Select(match => new Token(match, names.GetValueOrDefault(match.Value))).ToArray();
        var output = new StringBuilder();
        var cursor = 0;
        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            output.Append(Escape(line[cursor..token.Match.Index]));
            var owner = token.Term ?? ValueOwner(line, tokens, i);
            var tint = owner?.Tint.ToHtml(false) ?? "f4eee0";
            output.Append("[b][color=#").Append(tint).Append(']').Append(Escape(token.Match.Value)).Append("[/color][/b]");
            if (token.Term is { } term)
            {
                used.Add(term);
                output.Append(Icon(term, iconSize));
            }
            cursor = token.Match.Index + token.Match.Length;
        }
        return output.Append(Escape(line[cursor..])).ToString();
    }

    private static CombatKeyword? ValueOwner(string line, Token[] tokens, int index)
    {
        var number = tokens[index].Match;
        var left = index > 0 ? tokens[index - 1] : null;
        var right = index + 1 < tokens.Length ? tokens[index + 1] : null;
        var leftGap = left is null ? "" : line[(left.Match.Index + left.Match.Length)..number.Index];
        var rightGap = right is null ? "" : line[(number.Index + number.Length)..right.Match.Index];
        var before = left?.Term is not null && AfterTerm.IsMatch(leftGap) ? left.Term : null;
        var after = right?.Term is not null && BeforeTerm.IsMatch(rightGap) ? right.Term : null;
        if (before is null && after is null && left is { Term: null } && ThresholdContinuation.IsMatch(leftGap))
            return ValueOwner(line, tokens, index - 1);
        // In "闪避恢复 20 生命", 20 describes healing, not the dodge chance.
        if (after is not null && (before is null || leftGap.Contains("恢复", StringComparison.Ordinal) || leftGap.Contains("回复", StringComparison.Ordinal)))
            return after;
        return before ?? after;
    }

    public static string Icon(CombatKeyword term, int size)
    {
        var icon = SemanticIcons.Catalog.ResolveIcon(term.SemanticIcon);
        return icon is null ? "" : $" [img width={size} height={size} color=#{term.Tint.ToHtml(false)}]{icon.ResourcePath}[/img]";
    }

    public static string Escape(string value) => value.Replace("[", "[lb]", StringComparison.Ordinal);
}
