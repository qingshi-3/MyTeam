using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class UnitSkillSummary : PanelContainer
{
    public event Action<DetailExplainButton>? ExplanationRequested;
    public override void _Ready() => GetNode<DetailExplainButton>("%SkillHeader").ExplanationRequested += OnExplain;
    public override void _ExitTree() => GetNode<DetailExplainButton>("%SkillHeader").ExplanationRequested -= OnExplain;
    private void OnExplain(DetailExplainButton button) => ExplanationRequested?.Invoke(button);

    public void Bind(string category, string name, string body, string explanation)
    {
        var header = GetNode<DetailExplainButton>("%SkillHeader");
        header.Text = category + " · " + name;
        var tint = category == "主动" ? new Color("f2ca78") : new Color("92c4ed");
        header.AddThemeColorOverride("font_color", tint);
        header.AddThemeColorOverride("font_hover_color", tint.Lightened(.1f));
        header.AddThemeColorOverride("font_focus_color", tint);
        header.BindExplanation(name, explanation);
        GetNode<CombatRichText>("%SkillBody").Text = body;
    }
}
