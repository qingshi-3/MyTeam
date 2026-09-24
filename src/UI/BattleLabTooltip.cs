using System;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

public partial class BattleLabTooltip : CanvasLayer
{
    private PanelContainer _panel = null!;

    public override void _Ready() => _panel = GetNode<PanelContainer>("%TooltipPanel");

    public void Present(BattleLabTooltipInfo info, Control target, bool keyboard)
    {
        _panel ??= GetNode<PanelContainer>("%TooltipPanel");
        // CanvasLayer crosses the visual tree only; inherit the caller's authored theme explicitly.
        for (Node? current = target; current is not null; current = current.GetParent())
            if (current is Control { Theme: { } theme }) { _panel.Theme = theme; break; }
        SetLabel("%Title", info.Title);
        SetLabel("%Subtitle", info.Subtitle);
        SetLabel("%Stats", info.Stats);
        SetLabel("%Abilities", info.Abilities);
        SetLabel("%Loadout", info.Loadout);
        SetLabel("%Hint", info.Hint);
        GetNode<TooltipStatGrid>("%IconStats").Bind(info.IconStats);
        var width = Mathf.Min(396, target.GetViewportRect().Size.X - 24);
        _panel.CustomMinimumSize = new Vector2(Mathf.Max(160, width), 0);
        _panel.Size = new Vector2(Mathf.Max(160, width), 0);
        FitContent(info, Mathf.Max(160, width) - 28, target.GetViewportRect().Size.Y - 24);
        Visible = true;
        Place(target, keyboard);
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(target) || !target.IsInsideTree() || !Visible) return;
            // RichText finishes wrapping after the container sort. Shrink the actual panel too:
            // changing only its minimum leaves the previous tall tooltip as an empty rectangle.
            _panel.ResetSize();
            Place(target, keyboard);
        }).CallDeferred();
    }

    private void SetLabel(string path, string text)
    {
        var control = GetNode<Control>(path);
        if (control is CombatRichText rich) rich.Text = text ?? string.Empty;
        else ((Label)control).Text = text ?? string.Empty;
        control.Visible = !string.IsNullOrWhiteSpace(text);
    }

    private void FitContent(BattleLabTooltipInfo info, float textWidth, float height)
    {
        var title = GetNode<Label>("%Title");
        var subtitle = GetNode<Label>("%Subtitle");
        var stats = GetNode<CombatRichText>("%Stats");
        var abilities = GetNode<CombatRichText>("%Abilities");
        var loadout = GetNode<CombatRichText>("%Loadout");
        var hint = GetNode<Label>("%Hint");
        title.MaxLinesVisible = 1;
        subtitle.MaxLinesVisible = 1;
        hint.MaxLinesVisible = 2;
        foreach (var label in new[] { title, subtitle, hint })
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        // Reserve only visible content, including the attribute grid; skills receive the remaining space.
        // Trim plain text first, then render tokens, so truncation can never break markup.
        var iconStats = GetNode<TooltipStatGrid>("%IconStats");
        var fixedHeight = 24 + 40 + LineHeight(title) + (subtitle.Visible ? LineHeight(subtitle) : 0)
            + (hint.Visible ? LineHeight(hint) * 2 : 0)
            + (iconStats.Visible ? iconStats.GetCombinedMinimumSize().Y + 8 : 0);
        var statsLines = height < 420 ? 2 : 4;
        var loadoutLines = loadout.Visible ? (height < 520 ? 2 : 4) : 0;
        // Attribute-only hovers can use the space otherwise reserved for skills.
        // Roster cards expose secondary attributes here, with no separate detail pane.
        if (!abilities.Visible)
            statsLines = Math.Max(statsLines, Mathf.FloorToInt(
                (height - fixedHeight - RichLineHeight(loadout) * loadoutLines) / RichLineHeight(stats)));
        var remaining = height - fixedHeight - (stats.Visible ? RichLineHeight(stats) * statsLines : 0)
            - RichLineHeight(loadout) * loadoutLines;
        var abilityLines = Math.Max(2, Mathf.FloorToInt(remaining / RichLineHeight(abilities)));
        stats.Text = Summarize(info.Stats, stats, textWidth, statsLines, out var cutStats);
        abilities.Text = Summarize(info.Abilities, abilities, textWidth, abilityLines, out var cutAbilities);
        loadout.Text = Summarize(info.Loadout, loadout, textWidth, loadoutLines, out var cutLoadout);
        if (cutStats || cutAbilities || cutLoadout)
        {
            hint.Visible = true;
            hint.Text = info.Subtitle.Contains("团队遗物", StringComparison.Ordinal)
                ? "摘要 · 展开右侧“遗物完整效果”查看全部规则。"
                : info.Subtitle.Contains("装备", StringComparison.Ordinal)
                    ? "摘要 · 展开右侧“装备完整效果”查看全部规则。"
                    : "摘要 · 点击对象查看完整说明与词条释义。";
        }
    }

    private static float LineHeight(Label label) =>
        label.GetThemeFont("font").GetHeight(label.GetThemeFontSize("font_size")) + 2;

    private static float RichLineHeight(RichTextLabel label) => Math.Max(
        label.GetThemeFont("normal_font").GetHeight(label.GetThemeFontSize("normal_font_size")),
        label.GetThemeFont("bold_font").GetHeight(label.GetThemeFontSize("bold_font_size"))) + 4;

    private static string Summarize(string text, RichTextLabel label, float width, int maxLines, out bool clipped)
    {
        clipped = false;
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var columns = Mathf.Max(8, Mathf.FloorToInt(width / label.GetThemeFontSize("normal_font_size")));
        var lineCount = text.Split('\n').Sum(line => Math.Max(1, (line.Length + columns - 1) / columns));
        if (lineCount <= maxLines) return text;
        clipped = true;
        var allParagraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var paragraphs = allParagraphs.Take(Math.Max(1, maxLines)).ToArray();
        var spacious = maxLines >= paragraphs.Length * 3 - 1;
        var linesPerBlock = Math.Max(1, (maxLines - (spacious ? paragraphs.Length - 1 : 0)) / paragraphs.Length);
        var summary = string.Join(spacious ? "\n\n" : "\n", paragraphs.Select(paragraph =>
        {
            var lines = paragraph.Split('\n');
            var budget = columns * linesPerBlock;
            if (lines.Sum(line => Math.Max(1, (line.Length + columns - 1) / columns)) <= linesPerBlock)
                return paragraph;
            if (lines.Length == 1 || linesPerBlock < 2)
                return paragraph.Replace('\n', ' ')[..Math.Min(paragraph.Length, Math.Max(1, budget - 1))] + "…";
            var body = string.Join(" ", lines.Skip(1));
            var take = Math.Min(body.Length, Math.Max(1, budget - columns - 1));
            var heading = lines[0].Length > columns ? lines[0][..(columns - 1)] + "…" : lines[0];
            return heading + "\n" + body[..take].TrimEnd() + "…";
        }));
        return allParagraphs.Length > paragraphs.Length ? summary.TrimEnd('…') + "…" : summary;
    }
    private void Place(Control target, bool keyboard)
    {
        var viewport = target.GetViewportRect();
        var targetRect = target.GetGlobalRect();
        var anchor = keyboard ? targetRect.Position + new Vector2(targetRect.Size.X, 0) : target.GetGlobalMousePosition();
        var size = _panel.Size;
        var x = anchor.X + 16;
        if (x + size.X > viewport.End.X - 12) x = keyboard ? targetRect.Position.X - size.X - 12 : anchor.X - size.X - 16;
        var y = anchor.Y + (keyboard ? 0 : 18);
        _panel.Position = new Vector2(Mathf.Clamp(x, 12, Mathf.Max(12, viewport.End.X - size.X - 12)),
            Mathf.Clamp(y, 12, Mathf.Max(12, viewport.End.Y - size.Y - 12)));
    }
}
