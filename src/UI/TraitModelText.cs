using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

namespace TowerAutobattler.UI;

// Read compiled values, never infer a rule from a trait name or its stable id.
public static class TraitModelText
{
    public static string Describe(CompiledTraitBreakpoint tier)
    {
        var facts = new List<string> { tier.TargetPolicy == TraitTargetPolicy.Contributors ? "受益：贡献成员。" : "受益：全体友军。" };
        foreach (var modifier in tier.AttributeModifiers)
        {
            var amount = EffectModelText.DescribeMagnitude(modifier.Magnitude);
            if (modifier.Magnitude is CompiledConstantMagnitude constant && modifier.Operation != AttributeModifierOperation.Multiply &&
                modifier.Attribute is CombatAttribute.CriticalChance or CombatAttribute.LifeSteal or CombatAttribute.ControlResistance or CombatAttribute.DodgeChance)
                amount = Percent(constant.Value);
            facts.Add(EffectModelText.AttributeName(modifier.Attribute) + (modifier.Operation switch
            {
                AttributeModifierOperation.Add => "增加 ", AttributeModifierOperation.Multiply => "乘以 ", _ => "设为 "
            }) + amount + "。");
        }
        if (!tier.GrantedStatuses.IsDefaultOrEmpty)
            facts.AddRange(tier.GrantedStatuses.Select(status => status.DisplayName + "：" + StatusModelText.Describe(status)));
        if (!tier.Mechanics.IsDefaultOrEmpty) facts.AddRange(tier.Mechanics.Select(Mechanic));
        return string.Join("\n", facts);
    }

    private static string Percent(float value) => $"{value * 100:0.##}%";
    private static string Seconds(int ticks) => $"{ticks * BattleTiming.TickSeconds:0.##} 秒";
    private static string Cooldown(CompiledTraitMechanic m) => m.CooldownTicks > 0 ? $"冷却 {Seconds(m.CooldownTicks)}。" : "";

    private static string Mechanic(CompiledTraitMechanic m) => m.Kind switch
    {
        TraitMechanicKind.HealthThresholdRescue =>
            $"受到敌方伤害后仍存活且生命降至 {Percent(m.Threshold)} 或以下时，每场一次获得最大生命 {Percent(m.Amount)} 的护盾，持续 {Seconds(m.DurationTicks)}；最近的非成员常驻友军获得自身最大生命 {Percent(m.Secondary)} 的护盾，每名友军最多获得 {m.Limit} 次。",
        TraitMechanicKind.DamageTakenRamp =>
            $"累计每承受最大生命 {Percent(m.Threshold)} 的敌方实际伤害，伤害提高 {Percent(m.Amount)}，最多 {m.Limit} 层。" +
            (m.Secondary > 0 ? $"满层后普攻命中恢复最大生命 {Percent(m.Secondary)}。{Cooldown(m)}" : ""),
        TraitMechanicKind.RepeatedTargetAttack =>
            $"对同一目标每命中 {m.Count} 次普攻，追加攻击力 {Percent(m.Amount)} 的伤害；换目标清空计数。" +
            (m.Enabled ? "原目标死亡时，换目标可保留至多 2 次计数。" : ""),
        TraitMechanicKind.WoundedTargetReward =>
            $"对生命低于 {Percent(m.Threshold)} 的目标，伤害提高 {Percent(m.Amount)}。" +
            (m.Secondary > 0 ? $"击杀后获得最大生命 {Percent(m.Secondary)} 的护盾和 {Percent(m.Extra)} 移速加成，持续 {Seconds(m.DurationTicks)}。{Cooldown(m)}" : ""),
        TraitMechanicKind.ActiveDamageAndRefund =>
            $"主动技能伤害提高 {Percent(m.Amount)}。" +
            (m.Secondary > 0 ? $"首次及之后每 {m.Count} 次耗蓝施法返还所耗法力的 {Percent(m.Secondary)}，法力锁结束后到账。" : ""),
        TraitMechanicKind.SupportProtection =>
            $"治疗与护盾量提高 {Percent(m.Amount)}。" +
            (m.Secondary > 0 ? $"实际治疗或施盾后，目标获得 {Percent(m.Secondary)} 减伤，持续 {Seconds(m.DurationTicks)}。" : ""),
        TraitMechanicKind.OwnedSummonBonus =>
            $"所属临时召唤物的生命提高 {Percent(m.Amount)}，伤害提高 {Percent(m.Secondary)}。" +
            (m.Enabled ? "每名成员每场首次成功召唤额外生成 1 个。" : "") +
            (m.Extra > 0 ? $"所属召唤物正常阵亡时返还 {m.Extra:0.##} 法力，每 {Seconds(m.CooldownTicks)} 最多 {m.Limit} 法力；被吞噬及特殊产物不触发。" : ""),
        TraitMechanicKind.ChillOnAttack =>
            $"普攻命中施加 1 层寒意；每层降低 6% 攻速与移速，最多 {m.Count} 层，持续 4 秒。{Cooldown(m)}" +
            (m.Count >= 3 ? $"达到 3 层后消耗寒意并冻结 {Seconds(m.DurationTicks)}，同一目标冻结冷却 3 秒。" : "") +
            (m.Enabled ? "冻结时向附近 2 格内至多 2 名敌人扩散 1 层寒意。" : ""),
        TraitMechanicKind.PoisonOnAttack =>
            $"普攻命中施加 1 层毒，持续 12 秒，每秒造成 {2 * (1 + m.Amount):0.##} 伤害，毒层分别计时。{Cooldown(m)}" +
            (m.Extra > 0 ? $"敌人身上来自本队的毒达到 {m.Limit} 层时，其伤害降低 {Percent(m.Extra)}。" : ""),
        TraitMechanicKind.ActiveHitMarks =>
            $"主动技能命中施加 1 层烬印，同次技能对同一目标只加一次；主动伤害按目标每层烬印提高 {Percent(m.Amount)}。" +
            (m.Secondary > 0 ? $"命中已有 {m.Count} 层烬印的目标时，消耗全部烬印，造成攻击力与法强较高值 {Percent(m.Secondary)} 的伤害。{Cooldown(m)}" : "") +
            (m.Extra > 0 ? $"爆炸向 1.5 格内其他敌人造成 {Percent(m.Extra)} 的溅射伤害。" : ""),
        TraitMechanicKind.DeathResource =>
            $"成员或其所属召唤物正常阵亡提供 1 点魂能，每个单位每场一次；被吞噬和魂能产物不计。每 {m.Count} 点魂能治疗生命比例最低的 {m.Limit} 名存活成员，恢复各自最大生命 {Percent(m.Amount)}。{Cooldown(m)}" +
            (m.Enabled ? $"并召唤灵体，生命为受益者最大生命的 25%，攻击力为其 30%，持续 {Seconds(m.DurationTicks)}，每队同时最多 2 个。" : ""),
        TraitMechanicKind.BarrierRetaliation =>
            $"开战获得最大生命 {Percent(m.Amount)} 的护盾，持续 {Seconds(m.DurationTicks)}。" +
            (m.Secondary > 0 ? $"护盾被敌方耗尽时，对 1.5 格内敌人造成累计敌方吸收伤害 {Percent(m.Secondary)} 的反击，每次上限为基础攻击力的 120%；反击不自循环。{Cooldown(m)}" : "") +
            (m.Extra > 0 ? $"首次破盾后获得最大生命 {Percent(m.Extra)} 的修复盾，持续 4 秒。" : "") +
            (m.Enabled ? "由成员施加的限时盾延长 2 秒。" : ""),
        TraitMechanicKind.LowHealthLeech =>
            $"自身生命低于 {Percent(m.Threshold)} 时，伤害提高 {Percent(m.Amount)}，非持续伤害恢复实际生命损失的 {Percent(m.Secondary)}。" +
            (m.Extra > 0 ? $"生命低于 30% 时每场一次获得最大生命 {Percent(m.Extra)} 的护盾，持续 {Seconds(m.DurationTicks)}。" : ""),
        TraitMechanicKind.SharedCastResource =>
            $"成员累计每 {m.Count} 次实际耗蓝施法，为法力比例最低的 {m.Limit} 名存活成员补充 {m.Amount:0.##} 法力，法力锁结束后到账。{Cooldown(m)}" +
            (m.Secondary > 0 ? $"受益者的下一次耗蓝主动技能伤害、治疗与护盾提高 {Percent(m.Secondary)}。" : ""),
        _ => ""
    };
}
