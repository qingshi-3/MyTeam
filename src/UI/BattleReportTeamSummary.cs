using Godot;

namespace TowerAutobattler.UI;

public partial class BattleReportTeamSummary : PanelContainer
{
    private Label _title = null!;
    private Label _healthValue = null!;
    private ProgressBar _healthBar = null!;
    private SemanticChip _survivors = null!;
    private SemanticChip _casualties = null!;
    private SemanticChip _kills = null!;
    private SemanticChip _damage = null!;
    private SemanticChip _healing = null!;
    private SemanticChip _environment = null!;

    public int Team { get; private set; }

    public override void _Ready()
    {
        _title = GetNode<Label>("%TeamTitle");
        _healthValue = GetNode<Label>("%RemainingHealthValue");
        _healthBar = GetNode<ProgressBar>("%RemainingHealthBar");
        _survivors = GetNode<SemanticChip>("%Survivors");
        _casualties = GetNode<SemanticChip>("%Casualties");
        _kills = GetNode<SemanticChip>("%Kills");
        _damage = GetNode<SemanticChip>("%Damage");
        _healing = GetNode<SemanticChip>("%Healing");
        _environment = GetNode<SemanticChip>("%EnvironmentDamage");
    }

    public void Bind(BattleReportTeamViewModel model)
    {
        Team = model.Team;
        ThemeTypeVariation = model.Team == 0 ? "ReportPlayerSummarySurface" : "ReportEnemySummarySurface";
        _title.Text = model.Title;
        _title.ThemeTypeVariation = model.Team == 0 ? "PlayerLabel" : "EnemyLabel";
        _healthValue.Text = $"剩余生命 {model.RemainingHealth:0} / {model.MaximumHealth:0}";
        _healthBar.Value = model.RemainingHealthRatio * 100;
        _survivors.Bind(SemanticIconKeys.Health, $"存活 {model.Survivors}", "HealthValue");
        _casualties.Bind(SemanticIconKeys.Deaths, $"伤亡 {model.Casualties}", "DangerValue");
        _kills.Bind(SemanticIconKeys.Kills, $"击杀 {model.Kills}", model.Team == 0 ? "PlayerLabel" : "EnemyLabel");
        _damage.Bind(SemanticIconKeys.Damage, $"伤害 {model.DamageDealt:0}", "DamageValue");
        _healing.Bind(SemanticIconKeys.Healing, $"治疗 {model.HealingDone:0}", "HealingValue");
        _environment.Visible = model.EnvironmentDamage > 0;
        if (_environment.Visible)
            _environment.Bind(SemanticIconKeys.Risk, $"环境承伤 {model.EnvironmentDamage:0}", "RiskValue");
    }
}
