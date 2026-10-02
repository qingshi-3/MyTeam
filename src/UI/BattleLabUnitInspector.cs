using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using TowerAutobattler.Traits;

namespace TowerAutobattler.UI;

// Binds the authored unit detail page to prepared facts, with no edit authority.
internal sealed class BattleLabUnitInspector
{
    private readonly Label _unitTitle, _inspectorDetails;
    private readonly CombatRichText _inspector, _inspectorStats, _inspectorGlossary;
    private readonly StatBlock _healthStat, _attackStat, _armorStat, _speedStat;
    private readonly Control _statsPanel;
    private readonly CheckButton _retentionUpgrade;
    public BattleLabUnitInspector(Control page, CheckButton retentionUpgrade)
    {
        _unitTitle = page.GetNode<Label>("UnitBox/UnitTitle");
        _inspector = page.GetNode<CombatRichText>("UnitBox/Inspector");
        _inspectorStats = page.GetNode<CombatRichText>("UnitBox/InspectorStats");
        _inspectorGlossary = page.GetNode<CombatRichText>("UnitBox/GlossaryFold/InspectorGlossary");
        _inspectorDetails = page.GetNode<Label>("UnitBox/TechnicalFold/InspectorDetails");
        _statsPanel = page.GetNode<Control>("UnitBox/StatsPanel");
        _healthStat = _statsPanel.GetNode<StatBlock>("LabHealth");
        _attackStat = _statsPanel.GetNode<StatBlock>("LabAttack");
        _armorStat = _statsPanel.GetNode<StatBlock>("LabArmor");
        _speedStat = _statsPanel.GetNode<StatBlock>("LabSpeed");
        _retentionUpgrade = retentionUpgrade;
    }
    private static string SideName(BattleLabSide side) => side == BattleLabSide.Player ? "A 队" : "B 队";
    private static string EquipmentName(BattleLabEditorSession editor, string id) =>
        editor.Content.Equipment.FirstOrDefault(item => item.StableId == id)?.DisplayName ?? id;
    public void Refresh(BattleLabEditorSession editor, BattleLabSelection selection, BattleLabDerivedProjection derived)
    {
        _retentionUpgrade.Visible = false; _inspectorDetails.Text = string.Empty;
        _statsPanel.Visible = false;
        _inspectorStats.Text = string.Empty;
        _inspectorGlossary.Text = string.Empty;
        if (editor.Session is null) return;
        var selected = !string.IsNullOrEmpty(selection.InstanceId) && editor.Session.TryGet(selection.InstanceId, out var instance) ? instance : null;
        var contentId = selected?.ContentId ?? selection.Prototype;
        if (string.IsNullOrEmpty(contentId) || editor.Content?.TryGetUnit(contentId, out var content) != true)
        { _unitTitle.Text = "选择一个单位"; _inspector.Text = "点击左侧单位了解能力；点击场上单位调整装备与升阶。"; return; }
        _unitTitle.Text = content.DisplayName + (selected is null ? " · 单位预览" : " · " + SideName(selected.Side));
        var identity = $"内容：{contentId}";
        if (selected is not null)
        {
            identity += $"\n实例：{selected.InstanceId}\n坐标：({selected.Cell.X}, {selected.Cell.Y})";
            _retentionUpgrade.Visible = editor.UnitSnapshotFor(content).AttackHitGrowth?.RetentionUpgradeAvailable == true;
            _retentionUpgrade.SetPressedNoSignal(selected.RetainAttackStacks);
        }
        var definition = content.Definition;
        var snapshot = editor.UnitSnapshotFor(content);
        float Read(CombatAttribute attribute, float fallback) => snapshot.AttributeDefinition?.Attributes
            .FirstOrDefault(item => item.Attribute == attribute)?.BaseValue ?? fallback;
        var health = definition.MaxHealth;
        var damage = definition.AttackDamage;
        var armor = definition.Armor;
        var speed = Read(CombatAttribute.AttackSpeed, 1);
        var reach = definition.AttackRange;
        var resistance = definition.BaseControlResistance;
        if (selected is not null && derived.Units.TryGetValue(selected.InstanceId, out var prepared))
        {
            health = prepared.Health; damage = prepared.Damage; armor = prepared.Armor;
            speed = prepared.AttackSpeed; reach = prepared.Reach; resistance = prepared.ControlResistance;
            var contributions = prepared.TraitContributions.Select(item =>
                $"{item.TraitId} +{item.Value}（{DescribeTraitSource(item.SourceKind)} · {item.SourceInstanceId} · {item.ContentIdentity}）");
            identity += "\n\n羁绊贡献来源\n" + string.Join("\n", contributions);
        }
        var equipment = selected is not null ? "\n\n装备：" +
            (selected.Equipment.IsDefaultOrEmpty ? "无" : string.Join("、", selected.Equipment.OrderBy(item => item.SlotIndex)
                .Select(item => $"{item.SlotIndex + 1}号槽 {EquipmentName(editor, item.ContentId)}"))) : string.Empty;
        var abilities = BattleLabTooltipFormatter.Abilities(snapshot, content.Definition.Description,
            selected?.RetainAttackStacks == true);
        var interval = snapshot.AttackTicks * BattleTiming.TickSeconds / Math.Max(.01f, speed);
        _statsPanel.Visible = true;
        _healthStat.Bind(SemanticIconKeys.Health, $"{health:0.#}", "生命", "HealthValue");
        _attackStat.Bind(SemanticIconKeys.Attack, $"{damage:0.#}", "攻击", "DamageValue");
        _armorStat.Bind(SemanticIconKeys.Armor, $"{armor:0.#}", "防御", "ArmorValue");
        _speedStat.Bind(SemanticIconKeys.Time, snapshot.Behavior.DisableBasicAttacks ? "—" : $"{1 / Math.Max(.01f, interval):0.##}/秒", "普攻频率", "RangeValue");
        var attack = snapshot.Behavior.DisableBasicAttacks ? "不进行普攻" : $"普攻间隔 {interval:0.##} 秒 · 攻速倍率 ×{speed:0.##}";
        var range = snapshot.AttackDelivery == AttackDelivery.Melee ? "近战触及" : "远程射程";
        var maxMana = Read(CombatAttribute.MaxMana, definition.MaxMana);
        _inspectorStats.Text = (selected is null ? "基础属性" : "当前配置属性") + $"\n{attack}\n{range} {reach:0.##} 格 · 控制抗性 {resistance:P0}" +
            (maxMana > 0 ? $"\n基础法力 {Read(CombatAttribute.StartingMana, definition.StartingMana):0.#}/{maxMana:0.#} · 满蓝施法\n基础回蓝 {Read(CombatAttribute.ManaPerSecond, definition.ManaPerSecond):0.#}/秒" : "") + equipment;
        _inspector.Text = abilities.Length == 0 ? definition.Description : abilities;
        _inspectorGlossary.Text = string.Join("\n\n", _inspector.Vocabulary.Terms
            .Where(term => _inspector.Text.Contains(term.Word, StringComparison.Ordinal))
            .Select(term => term.Word + " · " + term.Explanation));
        _inspectorDetails.Text = identity + (abilities.Length == 0 ? string.Empty : "\n\n内容说明\n" + definition.Description);
    }
    private static string DescribeTraitSource(TraitContributionSourceKind sourceKind) => sourceKind switch
    {
        TraitContributionSourceKind.Hero => "英雄", TraitContributionSourceKind.Equipment => "装备",
        TraitContributionSourceKind.ExplicitExtra => "额外", _ => sourceKind.ToString()
    };
}
