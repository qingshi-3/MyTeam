using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class BattleReportUnitRow : PanelContainer
{
    private UnitPortrait _portrait = null!;
    private Label _name = null!;
    private Label _identity = null!;
    private Label _status = null!;
    private SemanticChip _health = null!;
    private SemanticChip _damage = null!;
    private SemanticChip _taken = null!;
    private SemanticChip _healing = null!;
    private SemanticChip _kills = null!;

    public override void _Ready()
    {
        _portrait = GetNode<UnitPortrait>("%UnitPortrait");
        _name = GetNode<Label>("%UnitName");
        _identity = GetNode<Label>("%UnitIdentity");
        _status = GetNode<Label>("%UnitStatus");
        _health = GetNode<SemanticChip>("%UnitHealth");
        _damage = GetNode<SemanticChip>("%UnitDamage");
        _taken = GetNode<SemanticChip>("%UnitTaken");
        _healing = GetNode<SemanticChip>("%UnitHealing");
        _kills = GetNode<SemanticChip>("%UnitKills");
    }

    public void Bind(
        BattleUnitReportSnapshot unit,
        UnitPortraitDefinition? portrait,
        Texture2D fallback,
        bool topDamage,
        bool topTaken,
        bool topHealing)
    {
        _portrait.Bind(portrait, fallback);
        _name.Text = unit.DisplayName;
        _name.ThemeTypeVariation = unit.IsHero ? "HeroLabel" : unit.Team == 1 ? "EnemyLabel" : "PlayerLabel";
        _identity.Text = unit.IsTemporary ? "召唤物" : unit.IsHero ? "★ 英雄" : unit.Team == 1 ? "敌方单位" : "士兵";
        _identity.ThemeTypeVariation = unit.IsHero ? "HeroLabel" : "SecondaryLabel";
        _status.Text = unit.Alive ? "存活" : "阵亡";
        _status.ThemeTypeVariation = unit.Alive ? "HealthValue" : "DangerValue";
        _health.Bind(SemanticIconKeys.Health, $"生命 {unit.FinalHealth:0}/{unit.MaxHealth:0}", "HealthValue");
        _damage.Bind(SemanticIconKeys.Damage, $"伤害 {unit.DamageDealt:0}" + (topDamage && unit.DamageDealt > 0 ? "　最高" : string.Empty), "DamageValue");
        _taken.Bind(SemanticIconKeys.Damage, $"承伤 {unit.DamageTaken:0}" + (topTaken && unit.DamageTaken > 0 ? "　最高" : string.Empty), "DangerValue");
        _healing.Bind(SemanticIconKeys.Healing, $"治疗 {unit.HealingDone:0}" + (topHealing && unit.HealingDone > 0 ? "　最高" : string.Empty), "HealingValue");
        _kills.Bind(SemanticIconKeys.Kills, $"击杀 {unit.Kills}", "PlayerLabel");
    }
}
