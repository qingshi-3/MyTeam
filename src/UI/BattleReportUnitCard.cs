using System.Collections.Generic;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class BattleReportUnitCard : PanelContainer
{
    private UnitPortrait _portrait = null!;
    private Label _name = null!;
    private Label _identity = null!;
    private Label _status = null!;
    private SemanticChip _role = null!;
    private Label _award = null!;
    private TextureRect _primaryIcon = null!;
    private Label _primaryLabel = null!;
    private Label _primaryValue = null!;
    private SemanticChip[] _supportFacts = [];
    private Label _contributionLabel = null!;
    private ProgressBar _contributionBar = null!;

    public BattleReportUnitViewModel? Model { get; private set; }
    public BattleReportDimension Dimension { get; private set; }

    public override void _Ready()
    {
        _portrait = GetNode<UnitPortrait>("%UnitPortrait");
        _name = GetNode<Label>("%UnitName");
        _identity = GetNode<Label>("%UnitIdentity");
        _status = GetNode<Label>("%UnitStatus");
        _role = GetNode<SemanticChip>("%UnitRole");
        _award = GetNode<Label>("%Award");
        _primaryIcon = GetNode<TextureRect>("%PrimaryIcon");
        _primaryLabel = GetNode<Label>("%PrimaryLabel");
        _primaryValue = GetNode<Label>("%PrimaryValue");
        _supportFacts =
        [
            GetNode<SemanticChip>("%SupportFact1"),
            GetNode<SemanticChip>("%SupportFact2"),
            GetNode<SemanticChip>("%SupportFact3"),
            GetNode<SemanticChip>("%SupportFact4")
        ];
        _contributionLabel = GetNode<Label>("%ContributionLabel");
        _contributionBar = GetNode<ProgressBar>("%ContributionBar");
    }

    public void Bind(
        BattleReportUnitViewModel model,
        BattleReportDimension dimension,
        UnitPortraitDefinition? portrait,
        Texture2D fallback)
    {
        Model = model;
        Dimension = dimension;
        var unit = model.Unit;
        _portrait.Bind(portrait, fallback);
        _name.Text = unit.DisplayName;
        _name.ThemeTypeVariation = unit.IsHero ? "HeroLabel" : unit.Team == 1 ? "EnemyLabel" : "ChoiceTitle";
        _identity.Text = Identity(unit);
        _identity.ThemeTypeVariation = unit.IsHero ? "HeroIdentity" : unit.Alive ? "TraitIdentity" : "DangerValue";
        _status.Text = unit.Alive ? "● 存活" : "✕ 阵亡";
        _status.ThemeTypeVariation = unit.Alive ? "HealthValue" : "DangerValue";
        _role.Bind(UnitSemanticFacts.Responsibility(unit.Role));
        ThemeTypeVariation = !unit.Alive ? "ReportDefeatedCardSurface" : unit.IsHero ? "ReportHeroCardSurface" : "ReportUnitCardSurface";
        BindAwards(model.Awards);

        switch (dimension)
        {
            case BattleReportDimension.Offense:
                BindOffense(model);
                break;
            case BattleReportDimension.Survival:
                BindSurvival(model);
                break;
            case BattleReportDimension.Healing:
                BindHealing(model);
                break;
            default:
                BindOverview(model);
                break;
        }
    }

    private void BindOverview(BattleReportUnitViewModel model)
    {
        var unit = model.Unit;
        SetPrimary(SemanticIconKeys.Health, "最终生命", $"{unit.FinalHealth:0} / {unit.MaxHealth:0}", "HealthValue");
        BindSupport(0, SemanticIconKeys.Damage, $"伤害 {unit.DamageDealt:0}", "DamageValue");
        BindSupport(1, SemanticIconKeys.Damage, $"承伤 {unit.DamageTaken:0}", "DangerValue");
        BindSupport(2, SemanticIconKeys.Healing, $"治疗 {unit.HealingDone:0}", "HealingValue");
        BindSupport(3, SemanticIconKeys.Kills, $"击杀 {unit.Kills}", unit.Team == 0 ? "PlayerLabel" : "EnemyLabel");
        SetContribution("剩余生命比例", model.FinalHealthRatio);
    }

    private void BindOffense(BattleReportUnitViewModel model)
    {
        var unit = model.Unit;
        SetPrimary(SemanticIconKeys.Damage, "有效伤害", unit.DamageDealt.ToString("0"), "DamageValue");
        BindSupport(0, SemanticIconKeys.Damage, $"团队占比 {model.DamageShare:P0}", "DamageValue");
        BindSupport(1, SemanticIconKeys.Damage, $"每秒伤害 {model.DamagePerSecond:0.0}", "DamageValue");
        BindSupport(2, SemanticIconKeys.Kills, $"击杀 {unit.Kills}", unit.Team == 0 ? "PlayerLabel" : "EnemyLabel");
        BindSupport(3, SemanticIconKeys.Time, $"攻击行动 {unit.AttackActions}", "SecondaryLabel");
        SetContribution("团队输出贡献", model.DamageShare);
    }

    private void BindSurvival(BattleReportUnitViewModel model)
    {
        var unit = model.Unit;
        SetPrimary(SemanticIconKeys.Damage, "有效承伤", unit.DamageTaken.ToString("0"), "DangerValue");
        BindSupport(0, SemanticIconKeys.Shield, $"护盾吸收 {unit.ShieldAbsorbed:0}", "ShieldValue");
        BindSupport(1, SemanticIconKeys.Health, $"最终生命 {unit.FinalHealth:0}/{unit.MaxHealth:0}", "HealthValue");
        BindSupport(2, SemanticIconKeys.Time, $"活跃 {model.ActiveLifetimeSeconds:0.0} 秒", "SecondaryLabel");
        BindSupport(3, SemanticIconKeys.Health, $"剩余比例 {model.FinalHealthRatio:P0}", "HealthValue");
        SetContribution("团队承伤占比", model.DamageTakenShare);
    }

    private void BindHealing(BattleReportUnitViewModel model)
    {
        var unit = model.Unit;
        SetPrimary(SemanticIconKeys.Healing, "有效治疗", unit.HealingDone.ToString("0"), "HealingValue");
        BindSupport(0, SemanticIconKeys.Healing, $"团队占比 {model.HealingShare:P0}", "HealingValue");
        BindSupport(1, SemanticIconKeys.Healing, $"每秒治疗 {model.HealingPerSecond:0.0}", "HealingValue");
        BindSupport(2, SemanticIconKeys.Healing, $"有效治疗 {unit.EffectiveHealingEvents} 次", "HealingValue");
        BindSupport(3, SemanticIconKeys.Time, $"活跃 {model.ActiveLifetimeSeconds:0.0} 秒", "SecondaryLabel");
        SetContribution("团队治疗贡献", model.HealingShare);
    }

    private void SetPrimary(StringName key, string label, string value, StringName variation)
    {
        _primaryIcon.Texture = SemanticIcons.Catalog.ResolveIcon(key);
        _primaryLabel.Text = label;
        _primaryValue.Text = value;
        _primaryValue.ThemeTypeVariation = variation;
        _primaryIcon.Modulate = _primaryValue.GetThemeColor("font_color");
    }

    private void BindSupport(int index, StringName key, string text, StringName variation)
        => _supportFacts[index].Bind(key, text, variation);

    private void SetContribution(string label, float ratio)
    {
        _contributionLabel.Text = label;
        _contributionBar.Value = Mathf.Clamp(ratio, 0, 1) * 100;
    }

    private void BindAwards(BattleReportAwards awards)
    {
        var labels = new List<string>(3);
        if (awards.HasFlag(BattleReportAwards.DamageLeader)) labels.Add("◆ 输出之冠");
        if (awards.HasFlag(BattleReportAwards.DamageTakenLeader)) labels.Add("◆ 铁壁");
        if (awards.HasFlag(BattleReportAwards.HealingLeader)) labels.Add("◆ 治疗之星");
        _award.Text = string.Join("　", labels);
        _award.Visible = labels.Count > 0;
    }

    private static string Identity(Battle.BattleUnitReportSnapshot unit)
    {
        if (unit.IsTemporary) return "◇ 召唤物";
        if (unit.IsHero) return "★ 英雄";
        if (unit.Role == UnitRole.Boss) return "◆ 首领";
        return unit.Team == 0 ? "士兵" : "敌方单位";
    }
}
