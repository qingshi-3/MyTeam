using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

namespace TowerAutobattler.UI;

public partial class CombatRichText : RichTextLabel
{
    [Export] public CombatKeywordCatalog Vocabulary { get; set; } = null!;
    [Export] public bool ShowGlossary { get; set; }
    private string _plainText = "";

    // Callers always bind plain text. Only this renderer creates markup, escaping each token.
    public new string Text
    {
        get => _plainText;
        set
        {
            value ??= "";
            if (_plainText == value && base.Text.Length > 0) return;
            _plainText = value;
            Render();
        }
    }

    public override void _Ready()
    {
        if (_plainText.Length == 0) _plainText = base.Text;
        BbcodeEnabled = true;
        Render();
    }

    private void Render()
    {
        if (Vocabulary is null) { base.Text = Escape(_plainText); return; }
        var terms = Vocabulary.Terms.Where(term => !string.IsNullOrEmpty(term.Word))
            .OrderByDescending(term => term.Word.Length).ToArray();
        var used = new HashSet<CombatKeyword>();
        var iconSize = GetThemeFontSize("normal_font_size") + 2;
        var result = new StringBuilder();
        foreach (var line in _plainText.Split('\n'))
        {
            if (result.Length > 0) result.Append('\n');
            if (line.StartsWith("主动 · ", StringComparison.Ordinal) || line.StartsWith("被动 · ", StringComparison.Ordinal))
                result.Append(line.StartsWith("主动", StringComparison.Ordinal) ? "[b][color=#f2ca78]" : "[b][color=#92c4ed]")
                    .Append(Escape(line)).Append("[/color][/b]");
            else result.Append(CombatTextMarkup.Line(line, terms, iconSize, used));
        }
        if (ShowGlossary)
        {
            // Read in the fixed detail pane; the transient tooltip remains input-transparent.
            if (used.Count > 0)
            {
                result.Append("\n\n[b][color=#94a8b7]词条释义[/color][/b]");
                foreach (var term in terms.Where(used.Contains))
                    result.Append("\n[b][color=#").Append(term.Tint.ToHtml(false)).Append(']')
                        .Append(Escape(term.Word)).Append("[/color][/b]").Append(CombatTextMarkup.Icon(term, iconSize)).Append("  [color=#aab9c5]")
                        .Append(Escape(term.Explanation)).Append("[/color]");
            }
        }
        base.Text = result.ToString();
    }

    private static string Escape(string value) => value.Replace("[", "[lb]", StringComparison.Ordinal);
}
