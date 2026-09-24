using System;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class BattleStatRow : Button
{
    public event Action<string>? UnitSelected;
    private UnitPortrait _portrait = null!;
    private Label _name = null!;
    private Label _value = null!;
    private ProgressBar _bar = null!;
    private string _runtimeId = "";
    private string _contentId = "";

    public override void _Ready()
    {
        _portrait = GetNode<UnitPortrait>("%StatPortrait");
        _name = GetNode<Label>("%StatUnitName");
        _value = GetNode<Label>("%StatValue");
        _bar = GetNode<ProgressBar>("%StatBar");
        Pressed += OnSelected;
    }

    public void Bind(BattleUnitReportSnapshot unit, UnitPortraitDefinition? portrait, string metric, float value, float maximum)
    {
        _runtimeId = unit.RuntimeId;
        if (_contentId != unit.ContentId)
        {
            _contentId = unit.ContentId;
            _portrait.Bind(portrait, SemanticIcons.Catalog.ResolveIcon(unit.IsHero ? "hero" : "unit"));
        }
        _name.Text = unit.DisplayName + (unit.Alive ? "" : " · 阵亡");
        _value.Text = value.ToString("0");
        _bar.Value = maximum > 0 ? value / maximum * 100 : 0;
        BattleLabHoverHint.Bind(this, new BattleLabTooltipInfo(unit.DisplayName,
            Subtitle: unit.Alive ? "仍在战场" : "已阵亡 · 保留本场累计",
            Stats: $"本场{metric} {value:0}\n造成伤害 {unit.DamageDealt:0} · 承受伤害 {unit.DamageTaken:0}\n" +
                $"有效治疗 {unit.HealingDone:0} · 护盾吸收 {unit.ShieldAbsorbed:0}",
            Abilities: "伤害与承伤包含护盾吸收；治疗不含过疗。\n条长相对于本侧当前指标最高的单位。",
            Hint: "点击查看单位详情"));
    }

    private void OnSelected() => UnitSelected?.Invoke(_runtimeId);
    public override void _ExitTree() => Pressed -= OnSelected;
}
