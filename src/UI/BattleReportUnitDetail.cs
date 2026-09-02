using System.Collections.Generic;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class BattleReportUnitDetail : PanelContainer
{
    private UnitPortrait _portrait = null!;
    private Label _name = null!;
    private Label _identity = null!;
    private SemanticChip _role = null!;
    private Label _awards = null!;
    private Label[] _facts = [];

    public string? RuntimeId { get; private set; }

    public override void _Ready()
    {
        _portrait = GetNode<UnitPortrait>("%UnitPortrait");
        _name = GetNode<Label>("%UnitName");
        _identity = GetNode<Label>("%UnitIdentity");
        _role = GetNode<SemanticChip>("%UnitRole");
        _awards = GetNode<Label>("%Awards");
        _facts =
        [
            GetNode<Label>("%Fact1"), GetNode<Label>("%Fact2"), GetNode<Label>("%Fact3"),
            GetNode<Label>("%Fact4"), GetNode<Label>("%Fact5"), GetNode<Label>("%Fact6"),
            GetNode<Label>("%Fact7"), GetNode<Label>("%Fact8"), GetNode<Label>("%Fact9"),
            GetNode<Label>("%Fact10"), GetNode<Label>("%Fact11"), GetNode<Label>("%Fact12")
        ];
    }

    public void Bind(
        BattleReportUnitViewModel model,
        UnitPortraitDefinition? portrait,
        Texture2D fallback)
    {
        RuntimeId = model.Unit.RuntimeId;
        var unit = model.Unit;
        _portrait.Bind(portrait, fallback);
        _name.Text = unit.DisplayName;
        _name.ThemeTypeVariation = unit.IsHero ? "HeroLabel" : unit.Team == 1 ? "EnemyLabel" : "ChoiceTitle";
        _identity.Text = $"{Identity(unit)} · {(unit.Alive ? "● 存活" : "✕ 阵亡")}";
        _identity.ThemeTypeVariation = unit.Alive ? "HealthValue" : "DangerValue";
        _role.Bind(UnitSemanticFacts.Responsibility(unit.Role));
        _awards.Text = Awards(model.Awards);
        _awards.Visible = _awards.Text.Length > 0;

        var facts = new[]
        {
            $"最终生命　{unit.FinalHealth:0} / {unit.MaxHealth:0}",
            $"有效伤害　{unit.DamageDealt:0}",
            $"有效承伤　{unit.DamageTaken:0}",
            $"护盾吸收　{unit.ShieldAbsorbed:0}",
            $"有效治疗　{unit.HealingDone:0}",
            $"击杀　{unit.Kills}",
            $"攻击行动　{unit.AttackActions}",
            $"治疗事件　{unit.EffectiveHealingEvents}",
            $"活跃时长　{model.ActiveLifetimeSeconds:0.0} 秒",
            $"输出占比　{model.DamageShare:P0}",
            $"承伤占比　{model.DamageTakenShare:P0}",
            $"治疗占比　{model.HealingShare:P0}"
        };
        for (var index = 0; index < _facts.Length; index++) _facts[index].Text = facts[index];
    }

    private static string Awards(BattleReportAwards awards)
    {
        var labels = new List<string>(3);
        if (awards.HasFlag(BattleReportAwards.DamageLeader)) labels.Add("◆ 输出领袖");
        if (awards.HasFlag(BattleReportAwards.DamageTakenLeader)) labels.Add("◆ 承伤领袖");
        if (awards.HasFlag(BattleReportAwards.HealingLeader)) labels.Add("◆ 治疗领袖");
        return string.Join("　", labels);
    }

    private static string Identity(BattleUnitReportSnapshot unit)
    {
        if (unit.IsTemporary) return "召唤物";
        if (unit.IsHero) return "英雄";
        if (unit.Role == UnitRole.Boss) return "首领";
        return unit.Team == 0 ? "士兵" : "敌方单位";
    }
}
