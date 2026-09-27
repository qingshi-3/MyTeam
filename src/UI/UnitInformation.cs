using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;

namespace TowerAutobattler.UI;

// A preparation projection is copied before its temporary simulation is disposed.
public sealed record PreparedUnitDetails(UnitSnapshot Snapshot,
    IReadOnlyDictionary<CombatAttribute, float> Attributes, float Health, float Shield, string? Context = null);

public sealed record UnitAttributeFact(StringName Icon, string Value, string Caption, string Explanation, Color? Tint = null);
public sealed record UnitSkillInfo(string Category, string Name, string Body, string Timing);

// Read-only presentation input. The views never receive a RunApplication or a live battle.
// Baseline and prepared values remain distinct, including when a prepared skill was upgraded.
public sealed class UnitInformation
{
    public string Identity { get; }
    public UnitDefinition Definition { get; }
    public UnitSnapshot Baseline { get; }
    public UnitSnapshot Snapshot { get; }
    public bool IsPrepared { get; }
    public string Context { get; }
    public string ContextCaption => Context.Split('·', '（')[0].Trim();
    public bool UsesMana => Snapshot.AbilityLoadout?.Abilities.Any(a => a.Trigger == AbilityTriggerKind.ManaFull) == true;
    public ImmutableArray<UnitSkillInfo> Skills { get; }
    private readonly ImmutableDictionary<CombatAttribute, float> _attributes;

    public UnitInformation(string identity, UnitDefinition definition, UnitSnapshot baseline,
        PreparedUnitDetails? prepared = null, string? context = null)
    {
        Identity = identity;
        Definition = definition;
        Baseline = baseline;
        Snapshot = prepared?.Snapshot ?? baseline;
        IsPrepared = prepared is not null;
        _attributes = prepared?.Attributes.ToImmutableDictionary() ?? ImmutableDictionary<CombatAttribute, float>.Empty;
        Context = context ?? prepared?.Context ?? (IsPrepared ? "准备属性 · 已计开战加成" : "基础属性 · 未计开战加成");
        var skills = new List<UnitSkillInfo>();
        foreach (var group in (Snapshot.AbilityLoadout?.Abilities ?? []).GroupBy(ability =>
                     (Active: ability.IsDisplayedActiveSkill, ability.DisplayName)).OrderByDescending(group => group.Key.Active))
        {
            var body = string.Join("\n", group.Select(ability => string.IsNullOrWhiteSpace(ability.AuthoredDescription)
                ? ability.Description : ability.AuthoredDescription).Distinct());
            var timing = group.Any(a => a.Trigger == AbilityTriggerKind.ManaFull) ? "法力满时自动施放。" :
                group.Key.Active ? "满足条件时自动施放，无需法力。" : "";
            var cooldown = group.Max(ability => ability.CooldownTicks);
            if (cooldown > 0) timing += $" 冷却 {cooldown * BattleTiming.TickSeconds:0.##} 秒。";
            skills.Add(new(group.Key.Active ? "主动" : "被动", group.Key.DisplayName, body, timing));
        }
        if (Snapshot.AttackHitGrowth is { } growth)
        {
            const string name = "连续命中成长";
            var body = $"每次命中，攻速增加 {growth.AttackSpeedPerHit * 100:0.##}%，可无限叠加。\n" +
                (growth.ResetOnTargetChange ? "更换目标时清空加成；" : "更换目标时保留加成；") + "战斗结束后清空。";
            var existing = skills.FindIndex(skill => skill.Category == "被动" && skill.Name == name);
            if (existing < 0) skills.Add(new("被动", name, body, ""));
            else skills[existing] = skills[existing] with { Body = skills[existing].Body + "\n" + body };
        }
        Skills = skills.ToImmutableArray();
    }

    public float Base(CombatAttribute attribute, float fallback = 0) =>
        Baseline.AttributeDefinition?.Attributes.FirstOrDefault(item => item.Attribute == attribute)?.BaseValue ?? fallback;
    public float Read(CombatAttribute attribute, float fallback = 0) => _attributes.TryGetValue(attribute, out var value) ? value : Base(attribute, fallback);

    public string Explain(CombatAttribute attribute, string rule, float fallback = 0, bool percent = false)
    {
        var baseline = Base(attribute, fallback);
        var current = Read(attribute, fallback);
        string Number(float value) => percent ? $"{value * 100:0.##}%" : $"{value:0.##}";
        return (IsPrepared ? $"{Context}\n当前值 {Number(current)} · 基础 {Number(baseline)}\n净变化 {(current - baseline >= 0 ? "+" : "")}{Number(current - baseline)}" :
            $"基础值 {Number(baseline)}\n未计装备、羁绊等开战加成。") + "\n" + rule;
    }

    public ImmutableArray<UnitAttributeFact> CoreStats()
    {
        var interval = Snapshot.AttackTicks * BattleTiming.TickSeconds / Math.Max(.01f, Read(CombatAttribute.AttackSpeed, 1));
        return [
            new(SemanticIconKeys.Attack, $"{Read(CombatAttribute.AttackDamage, Baseline.Damage):0.#}", "攻击",
                Explain(CombatAttribute.AttackDamage, "普攻与明确读取攻击的技能使用此值。最终伤害还受技能倍率、目标防护等影响。", Baseline.Damage)),
            new(SemanticIconKeys.Armor, $"{Read(CombatAttribute.Armor, Baseline.Armor):0.#}", "防御",
                Explain(CombatAttribute.Armor, "降低普通伤害，真实伤害绕过防御；只有明确读取防御的技能会将它转为输出。", Baseline.Armor)),
            new(SemanticIconKeys.Time, Snapshot.Behavior.DisableBasicAttacks ? "—" : $"{1 / interval:0.##}/秒", "普攻频率",
                Explain(CombatAttribute.AttackSpeed, Snapshot.Behavior.DisableBasicAttacks ? "该单位不进行普攻。" :
                    $"攻击间隔 {interval:0.##} 秒。\n基础攻击间隔 {Snapshot.AttackTicks * BattleTiming.TickSeconds:0.##} 秒 ÷ 当前攻速倍率。\n移动、控制和施法可能延迟实际出手。", 1)),
            new(SemanticIconKeys.Reach, $"{Read(CombatAttribute.AttackRange, Baseline.Range):0.##}", "射程 · 格",
                Explain(CombatAttribute.AttackRange, Snapshot.AttackDelivery == AttackDelivery.Melee ?
                    "从身体外沿计算近战触及距离，不是站位中心间距。" : "远程攻击的逻辑射程，以战场格为单位。", Baseline.Range))
        ];
    }

    // One fact source for the workbench summary and the complete 19-attribute sheet.
    public UnitAttributeFact AttributeFact(CombatAttribute attribute)
    {
        var (icon, caption, rule, fallback) = attribute switch
        {
            CombatAttribute.MaxHealth => (SemanticIconKeys.Health, "生命上限", "每战按此有效上限满血入场。", Baseline.MaxHealth),
            CombatAttribute.AttackDamage => (SemanticIconKeys.Attack, "攻击", "用于普攻及明确读取攻击的公式。", Baseline.Damage),
            CombatAttribute.SpellPower => (SemanticIconKeys.SpellPower, "法强", "仅影响明确读取法强的公式，不是所有技能的通用倍率。", Definition.SpellPower),
            CombatAttribute.AttackSpeed => (SemanticIconKeys.Time, "攻速倍率", "基础攻击间隔除以此倍率得到普攻间隔；移动、施法和控制会影响实际出手。", 1f),
            CombatAttribute.Armor => (SemanticIconKeys.Armor, "防御", "降低普通伤害；真实伤害绕过防御。", Baseline.Armor),
            CombatAttribute.AttackRange => (SemanticIconKeys.Reach, "射程", "普攻触及距离，单位为格；近战从身体外沿计算。", Baseline.Range),
            CombatAttribute.MoveSpeed => (SemanticIconKeys.Time, "移速倍率", "1 表示基础移动速度。", 1f),
            CombatAttribute.CriticalChance => (SemanticIconKeys.CriticalChance, "暴击率", "具有暴击资格的命中按此概率判定。", 0f),
            CombatAttribute.CriticalDamage => (SemanticIconKeys.CriticalDamage, "暴击倍率", "150% 表示暴击造成正常伤害的 1.5 倍。", 1.5f),
            CombatAttribute.MaxMana => (SemanticIconKeys.Mana, "法力上限", "满法力技能的释放阈值；无需法力的技能按自身条件触发。", 0f),
            CombatAttribute.StartingMana => (SemanticIconKeys.Mana, "初始法力", "每场战斗入场时的法力。", 0f),
            CombatAttribute.HealingPower => (SemanticIconKeys.Healing, "治疗强度", "仅影响明确读取治疗强度的恢复公式。", 0f),
            CombatAttribute.LifeSteal => (SemanticIconKeys.LifeSteal, "吸血", "符合吸血资格的伤害按此比例恢复生命。", 0f),
            CombatAttribute.ControlResistance => (SemanticIconKeys.ControlResistance, "控制抗性", "减轻可受控制抗性影响的控制。", 0f),
            CombatAttribute.ManaPerSecond => (SemanticIconKeys.Mana, "每秒回蓝", "每秒恢复的法力。", 0f),
            CombatAttribute.ManaPerAttack => (SemanticIconKeys.Mana, "普攻回蓝", "符合回蓝条件的普通攻击提供的法力。", 0f),
            CombatAttribute.ManaPerDamageRatio => (SemanticIconKeys.Mana, "承伤回蓝系数", "实际生命和护盾损失占生命上限的比例乘以此系数，再受单次上限约束；不是每次承伤固定获得的法力。", 0f),
            CombatAttribute.ManaPerHitCap => (SemanticIconKeys.Mana, "单次回蓝上限", "单次承伤事件能提供的法力上限。", 0f),
            CombatAttribute.DodgeChance => (SemanticIconKeys.Risk, "闪避率", "对可闪避的攻击按此概率判定。", 0f),
            _ => throw new ArgumentOutOfRangeException(nameof(attribute))
        };
        var percent = attribute is CombatAttribute.CriticalChance or CombatAttribute.CriticalDamage or CombatAttribute.LifeSteal
            or CombatAttribute.ControlResistance or CombatAttribute.DodgeChance;
        var value = Read(attribute, fallback);
        var number = percent ? $"{value * 100:0.#}%" : $"{value:0.##}";
        if (attribute is CombatAttribute.AttackSpeed or CombatAttribute.MoveSpeed) number = "×" + number;
        return new(icon, number, caption, Explain(attribute, rule, fallback, percent));
    }

    public string AllAttributesText() => string.Join("\n", Enum.GetValues<CombatAttribute>().Select(attribute =>
    {
        var fallback = attribute switch { CombatAttribute.MaxHealth => Baseline.MaxHealth,
            CombatAttribute.AttackDamage => Baseline.Damage, CombatAttribute.Armor => Baseline.Armor,
            CombatAttribute.AttackRange => Baseline.Range, CombatAttribute.AttackSpeed => 1, _ => 0 };
        var value = Read(attribute, fallback);
        var number = attribute is CombatAttribute.CriticalChance or CombatAttribute.LifeSteal or
            CombatAttribute.ControlResistance or CombatAttribute.DodgeChance or CombatAttribute.CriticalDamage
            ? $"{value * 100:0.##}%" : $"{value:0.##}";
        return $"{EffectModelText.AttributeName(attribute)} {number}";
    }).Chunk(2).Select(pair => string.Join("　", pair)));
}
