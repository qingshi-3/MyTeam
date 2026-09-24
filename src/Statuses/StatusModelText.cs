using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Statuses;

public static class StatusModelText
{
    public static string Describe(CompiledStatusDefinition status)
    {
        var facts = new List<string>();
        if (status.GrantedTags.Contains(StatusDefinitionCompiler.ActionDisabledTag)) facts.Add("无法行动");
        foreach (var modifier in status.AttributeModifiers)
        {
            var operation = modifier.Operation switch { AttributeModifierOperation.Add => "增加", AttributeModifierOperation.Multiply => "乘以", _ => "设为" };
            facts.Add($"每层{EffectModelText.AttributeName(modifier.Attribute)}{operation}{EffectModelText.DescribeMagnitude(modifier.Magnitude)}" +
                (modifier.Magnitude.CaptureMode == AttributeCaptureMode.Live ? "（实时读取）" : "（施加时取值）"));
        }
        facts.Add(status.DurationKind switch
        {
            StatusDurationKind.Permanent => "持续至移除或本场结束",
            StatusDurationKind.Instant => "立即结算",
            _ => $"持续 {status.DurationTicks * BattleTiming.TickSeconds:0.##} 秒"
        });
        if (status.StackLimit != 1) facts.Add(status.StackLimit == 0 ? "层数无上限" : $"最多 {status.StackLimit} 层");
        if (status.OverflowTransition is { } transition)
            facts.Add($"达到 {status.StackLimit} 层时消耗{(transition.ConsumeStacks == 0 ? "全部" : transition.ConsumeStacks.ToString())}层，施加「{transition.Target.DisplayName}」");
        else if (status.StackLimit > 0 && status.DurationKind != StatusDurationKind.Instant)
            facts.Add(status.OverflowPolicy == StatusOverflowPolicy.RefreshDuration ? "满层再次施加会刷新持续时间" : "满层不再增加层数");
        facts.Add(status.AggregationPolicy switch
        {
            StatusAggregationPolicy.ByTarget => "同一目标的多个来源合并叠层",
            StatusAggregationPolicy.BySource => "不同来源独立叠层",
            _ => "每次施加独立存在"
        });
        if (status.DurationKind == StatusDurationKind.TimedTicks)
            facts.Add(status.DurationRefreshPolicy switch
            {
                StatusDurationRefreshPolicy.Reset => "再次施加重置持续时间",
                StatusDurationRefreshPolicy.KeepLonger => "再次施加保留较长剩余时间",
                StatusDurationRefreshPolicy.Extend => "再次施加延长持续时间",
                _ => "再次施加不延长持续时间"
            });
        if (status.PeriodicEffect is not null)
            facts.Add($"每 {status.PeriodicIntervalTicks * BattleTiming.TickSeconds:0.##} 秒：{EffectModelText.DescribeBinding(status.PeriodicEffect)}" +
                (status.PeriodicResetPolicy == StatusPeriodicResetPolicy.ResetOnApplication ? "；再次施加重置周期" : "；再次施加保留周期"));
        foreach (var binding in status.LifecycleBindings)
            facts.Add($"{binding.Trigger switch { StatusLifecycleTriggerKind.Applied => "施加时", StatusLifecycleTriggerKind.StackChanged => "层数变化时", _ => "移除时（战斗结束清理除外）" }}：{EffectModelText.DescribeBinding(binding.Binding)}");
        foreach (var binding in status.CombatReactiveBindings)
            facts.Add($"当拥有者作为{(binding.OwnerRole == StatusReactiveOwnerRole.OwnerIsSource ? "事件发起方" : "事件承受方")}发生{EventName(binding.EventKind)}" +
                (binding.SourceKind == CombatSourceKind.None ? "" : $"（仅{SourceName(binding.SourceKind)}来源）") +
                (binding.FilterDamageType ? $"（仅{EffectModelText.DamageTypeName(binding.DamageType)}伤害）" : "") +
                $"：{EffectModelText.DescribeBinding(binding.Binding)}");
        facts.Add(status.DispelCategory switch { StatusDispelCategory.NonDispellable => "不可驱散", StatusDispelCategory.StrongOnly => "仅强驱散可移除", _ => "可驱散" });
        if (status.Behavior == StatusBehaviorKind.Taunt) facts.Add("普攻目标改为嘲讽来源；来源阵亡后恢复寻敌");
        if (status.ControlDurationRule == StatusControlDurationRule.LinearResistanceCeiling) facts.Add("持续时间受控制抗性缩短，至少保留一个逻辑帧");
        if (status.DeathPolicy == StatusDeathPolicy.Persist) facts.Add("拥有者阵亡后保留");
        return string.Join("；", facts) + "。";
    }

    public static string EventName(BattleCombatEventKind kind) => kind switch
    {
        BattleCombatEventKind.AttackDeclared => "普通攻击开始", BattleCombatEventKind.AttackLanded => "普通攻击命中",
        BattleCombatEventKind.SkillHitLanded => "技能命中",
        BattleCombatEventKind.AbilityResolved => "技能结算", BattleCombatEventKind.DamageResolved => "伤害结算",
        BattleCombatEventKind.HealingResolved => "治疗结算", BattleCombatEventKind.ShieldResolved => "护盾结算",
        BattleCombatEventKind.UnitDefeated => "单位阵亡", BattleCombatEventKind.UnitKilled => "击杀单位",
        _ => kind.ToString()
    };

    private static string SourceName(CombatSourceKind kind) => kind switch
    {
        CombatSourceKind.Unit => "单位", CombatSourceKind.Ability => "技能", CombatSourceKind.Status => "状态",
        CombatSourceKind.Equipment => "装备", CombatSourceKind.Trait => "羁绊", CombatSourceKind.Relic => "遗物",
        CombatSourceKind.TacticalCommand => "战术指令", CombatSourceKind.FloorRule => "楼层规则",
        CombatSourceKind.System => "系统", _ => "任意"
    };
}
