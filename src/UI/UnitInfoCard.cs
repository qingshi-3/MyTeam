using System;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

// Information only. Selection and equipment policies belong to the embedding card.
public partial class UnitInfoCard : VBoxContainer
{
    public event Action? Activated;
    private UnitVitals _vitals = null!;
    private UnitCoreStats _stats = null!;
    private Button _active = null!, _passive = null!;
    public override void _Ready()
    {
        _vitals = GetNode<UnitVitals>("%Vitals");
        _stats = GetNode<UnitCoreStats>("%CoreStats");
        _active = GetNode<Button>("%Active");
        _passive = GetNode<Button>("%Passive");
        _vitals.ExplanationRequested += OnFact;
        _stats.ExplanationRequested += OnFact;
        _active.Pressed += OnActivated;
        _passive.Pressed += OnActivated;
    }
    public override void _ExitTree()
    {
        // Instanced scene roots can lose unique-name registration during teardown.
        if (IsInstanceValid(_vitals)) _vitals.ExplanationRequested -= OnFact;
        if (IsInstanceValid(_stats)) _stats.ExplanationRequested -= OnFact;
        if (IsInstanceValid(_active)) _active.Pressed -= OnActivated;
        if (IsInstanceValid(_passive)) _passive.Pressed -= OnActivated;
    }
    private void OnFact(DetailExplainButton _) => OnActivated();
    private void OnActivated() => Activated?.Invoke();

    public void ForwardDrag(Callable canDrop, Callable drop)
    {
        GetNode<UnitVitals>("%Vitals").ForwardDrag(canDrop, drop);
        GetNode<UnitCoreStats>("%CoreStats").ForwardDrag(canDrop, drop);
        foreach (var path in new[] { "%Active", "%Passive" })
            GetNode<Control>(path).SetDragForwarding(Callable.From<Vector2, Variant>(_ => default), canDrop, drop);
    }

    public void Bind(UnitInformation model, string context)
    {
        var portrait = GetNode<UnitPortrait>("%Portrait");
        if (portrait.Definition != model.Definition.Portrait) portrait.Bind(model.Definition.Portrait, model.Definition.Icon);
        GetNode<Label>("%HeroName").Text = model.Definition.DisplayName;
        GetNode<Label>("%HeroState").Text = context;
        GetNode<Label>("%AttributeContext").Text = model.Context;
        GetNode<UnitVitals>("%Vitals").Bind(model);
        GetNode<UnitCoreStats>("%CoreStats").Bind(model);
        foreach (var active in new[] { true, false })
        {
            var category = active ? "主动" : "被动";
            var skills = model.Skills.Where(skill => skill.Category == category).ToArray();
            var names = string.Join(" / ", skills.Select(skill => skill.Name));
            var target = GetNode<Button>(active ? "%Active" : "%Passive");
            target.Text = category + " · " + (skills.Length == 0 ? "无" : names);
            BattleLabHoverHint.Bind(target, new BattleLabTooltipInfo(names, category,
                Abilities: skills.Length == 0 ? "该单位没有此类能力。" :
                    string.Join("\n\n", skills.Select(skill => skill.Name + "\n" + skill.Body + "\n" + skill.Timing)),
                Hint: active ? "法力满时自动施放。" : "触发条件与作用范围见效果说明。"));
        }
    }
}
