using System;
using System.Linq;
using System.Text;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Presentation;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.UI;

// Formatting happens only for the selected report, never in the simulation hot path.
internal static class BattleStatisticsText
{
    internal static string Build(BattleUnitReportSnapshot unit, int battleTicks,
        BattleScreenRuntimeUnitSnapshot? current,
        Func<CombatSourceKind, string, string> sourceName, Func<string, string> unitName)
    {
        var text = new StringBuilder(1800);
        text.AppendLine($"[b]{Escape(unit.DisplayName)}[/b]　{(unit.Team == 0 ? "我方" : "敌方")}{(unit.IsTemporary ? " · 召唤物" : "")}　{(unit.Alive ? "存活" : "阵亡")}");
        text.AppendLine($"生命 {unit.FinalHealth:0}/{unit.MaxHealth:0}　护盾 {unit.FinalShield:0}");
        if (current is not null && unit.Alive)
        {
            var active = current.Statuses.Where(status => status.Permanent || status.RemainingTicks > 0)
                .OrderByDescending(status => status.GrantedTags.Contains(StatusDefinitionCompiler.ActionDisabledTag)).Take(12).ToArray();
            text.AppendLine(active.Length == 0 ? "当前状态：无" : "当前状态：" + string.Join("、", active.Select(status =>
                Escape(status.DisplayName) + (status.Permanent ? "（本场）" : $"（{status.RemainingTicks * BattleTiming.TickSeconds:0.0}秒）"))));
            if (current.DisabledTicks > 0) text.AppendLine($"动作锁定剩余 {current.DisabledTicks * BattleTiming.TickSeconds:0.0} 秒（可能包含自身蓄势）");
        }
        if (unit.DetailedStatistics is not { } detail)
        {
            text.AppendLine($"\n有效伤害 {unit.DamageDealt:0}　有效承伤 {unit.DamageTaken:0}\n护盾吸收 {unit.ShieldAbsorbed:0}　有效治疗 {unit.HealingDone:0}");
            text.AppendLine("\n详细来源与控制统计仅在能力实验中采集。");
            return text.ToString();
        }

        var seconds = battleTicks * BattleTiming.TickSeconds;
        var alive = detail.AliveTicks * BattleTiming.TickSeconds;
        var control = detail.HardControlTicksTaken * BattleTiming.TickSeconds;
        text.AppendLine($"\n[b]控制[/b]\n受到硬控 {control:0.0} 秒（重叠只计一次）");
        text.AppendLine($"造成硬控 {detail.HardControlTicksDealt * BattleTiming.TickSeconds:0.0} 目标秒　其他动作锁定 {detail.LegacyActionLockTicks * BattleTiming.TickSeconds:0.0} 秒");
        text.AppendLine("[color=#bbb4a4]按实际行动时点累计，精度0.1秒；不含减速／嘲讽。目标秒按受控目标累加；来源可能重叠，不代表额外延长。其他锁定含自身蓄势。[/color]");
        if (detail.ControlSources.Length > 0)
        {
            text.AppendLine("[table=3][cell][b]受到的控制[/b][/cell][cell][b]来源单位[/b][/cell][cell][b]有效覆盖[/b][/cell]");
            foreach (var source in detail.ControlSources.OrderByDescending(s => s.ActiveTicks))
                text.AppendLine($"[cell]{Escape(source.IsOverflow ? "其余控制（合并）" : source.DisplayName)}[/cell][cell]{Escape(source.IsOverflow ? "多来源" : unitName(source.SourceRuntimeId))}[/cell][cell]{source.ActiveTicks * BattleTiming.TickSeconds:0.0} 秒[/cell]");
            text.AppendLine("[/table]");
        }
        else text.AppendLine("尚未受到硬控。");


        text.AppendLine($"\n[b]输出与生存[/b]　存活 {alive:0.0} 秒 · 击杀 {unit.Kills}");
        text.AppendLine($"有效伤害 {unit.DamageDealt:0.0} = 扣血 {detail.HealthDamageDealt:0.0} + 破盾 {detail.ShieldDamageDealt:0.0}");
        text.AppendLine($"溢出伤害 {detail.OverkillDamageDealt:0.0}　承受扣血 {detail.HealthDamageTaken:0.0}　自身护盾吸收 {unit.ShieldAbsorbed:0.0}");
        text.AppendLine($"全场秒伤 {(seconds > 0 ? unit.DamageDealt / seconds : 0):0.0}　存活秒伤 {(alive > 0 ? unit.DamageDealt / alive : 0):0.0}");
        text.AppendLine($"攻击行动 {unit.AttackActions}　命中 {detail.LandedAttacks}　被闪避 {detail.DodgedAttacks}　暴击 {detail.CriticalHits}");
        text.AppendLine($"法力施法 {detail.ManaCasts}　能力结算 {detail.AbilityResolutions}（含被动／触发）");
        text.AppendLine($"\n[b]治疗与护盾[/b]\n有效治疗 {unit.HealingDone:0.0}　过量治疗 {detail.HealingWasted:0.0}　收到治疗 {detail.HealingReceived:0.0}\n授予护盾 {detail.ShieldGranted:0.0}（授予量不等于实际吸收）");
        text.AppendLine("\n[b]伤害组成[/b]　均为减伤后数值，溢出不计有效伤害");
        text.AppendLine("[table=6][cell][b]来源[/b][/cell][cell][b]分类[/b][/cell][cell][b]扣血[/b][/cell][cell][b]破盾[/b][/cell][cell][b]溢出[/b][/cell][cell][b]次数[/b][/cell]");
        foreach (var source in detail.DamageSources.OrderByDescending(s => s.HealthDamage + s.ShieldDamage))
        {
            var name = source.IsOverflow ? "其他来源（合并）" : source.SourceKind == CombatSourceKind.Unit && source.DamageClass == CombatDamageClass.BasicAttack ? "普通攻击" : sourceName(source.SourceKind, source.StableId);
            var kind = source.IsOverflow ? "混合" : DamageClass(source.DamageClass) + (source.DamageType == EffectDamageType.True ? "·真实" : "");
            text.AppendLine($"[cell]{Escape(name)}[/cell][cell]{kind}[/cell][cell]{source.HealthDamage:0.0}[/cell][cell]{source.ShieldDamage:0.0}[/cell][cell]{source.OverkillDamage:0.0}[/cell][cell]{source.Hits}[/cell]");
        }
        text.AppendLine("[/table]");
        if (detail.DamageSources.IsEmpty) text.AppendLine("尚未造成伤害。");
        if (detail.HealingSources.Length > 0)
        {
            text.AppendLine("\n[b]治疗组成[/b]\n[table=4][cell][b]来源[/b][/cell][cell][b]有效[/b][/cell][cell][b]过量[/b][/cell][cell][b]次数[/b][/cell]");
            foreach (var source in detail.HealingSources.OrderByDescending(s => s.EffectiveHealing))
                text.AppendLine($"[cell]{Escape(source.IsOverflow ? "其他来源（合并）" : sourceName(source.SourceKind, source.StableId))}[/cell][cell]{source.EffectiveHealing:0.0}[/cell][cell]{source.WastedHealing:0.0}[/cell][cell]{source.Events}[/cell]");
            text.AppendLine("[/table]");
        }
        return text.ToString();
    }

    private static string DamageClass(CombatDamageClass kind) => kind switch
    {
        CombatDamageClass.BasicAttack => "普攻", CombatDamageClass.ActiveSkill => "主动技能",
        CombatDamageClass.Periodic => "持续/状态", CombatDamageClass.Counterattack => "反击",
        CombatDamageClass.Derived => "触发/派生", _ => "其他"
    };
    private static string Escape(string value) => value.Replace("[", "[lb]");
}
