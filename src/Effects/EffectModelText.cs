using System;
using System.Globalization;
using System.Linq;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Effects;

/// <summary>Shared identity and readable rule text; consumers must not silently omit new primitive fields.</summary>
public static class EffectModelText
{
    private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string Pack(params string[] parts) => string.Concat(parts.Select(part => $"{part.Length}:{part}"));
    public static string Target(CompiledEffectTargetQuery target) => target switch
    {
        CompiledOwnerTargetQuery => "owner",
        CompiledSourceTargetQuery => "source",
        CompiledExplicitTargetQuery => "explicit",
        CompiledRelativeTeamTargetQuery q => Pack("relative", q.Team.ToString(), q.IncludeDefeated.ToString(), q.RequiredTag),
        CompiledFilteredTargetQuery q => Pack("filtered", q.Team.ToString(), q.Anchor.ToString(), q.IncludeDefeated.ToString(),
            q.IncludeAnchor.ToString(), q.RequiredTag, F(q.Range), q.MaxTargets.ToString(CultureInfo.InvariantCulture), q.Order.ToString()),
        _ => throw new InvalidOperationException("Unsupported target identity.")
    };
    public static string Condition(CompiledEffectCondition condition) => condition switch
    {
        CompiledEntityAliveCondition c => Pack("alive", c.Entity.ToString(), c.ExpectedAlive.ToString()),
        CompiledHealthRatioCondition c => Pack("health", c.Entity.ToString(), c.Comparison.ToString(), F(c.Ratio)),
        CompiledEntityTagCondition c => Pack("tag", c.Entity.ToString(), c.Tag, c.ExpectedPresent.ToString()),
        _ => throw new InvalidOperationException("Unsupported condition identity.")
    };
    public static string BindingFingerprint(CompiledEffectBinding binding) => Pack(
        binding.StableId, binding.Priority.ToString(CultureInfo.InvariantCulture), binding.Trigger.Kind.ToString(), binding.Trigger.EventKind.ToString(),
        Pack(binding.Conditions.Select(Condition).ToArray()), Target(binding.TargetQuery),
        Pack(binding.Effects.Select(step => Pack(step.Kind.ToString(), step.DamageType.ToString(), step.AmountSource.ToString(), F(step.Amount),
            step.Magnitude is null ? "none" : AttributeMagnitudeSupport.Fingerprint(step.Magnitude))).ToArray()),
        binding.Limits.MaxUses.ToString(CultureInfo.InvariantCulture), binding.Limits.MinimumIntervalTicks.ToString(CultureInfo.InvariantCulture),
        binding.Limits.MaxDepth.ToString(CultureInfo.InvariantCulture), binding.Limits.MaxRepeatedEdges.ToString(CultureInfo.InvariantCulture),
        binding.Presentation is null ? "none" : Pack(binding.Presentation.DisplayName, binding.Presentation.ReportLabel, binding.Presentation.Cue));

    public static string DescribeTarget(CompiledEffectTargetQuery query) => query switch
    {
        CompiledOwnerTargetQuery => "拥有者",
        CompiledSourceTargetQuery => "来源单位",
        CompiledExplicitTargetQuery => "指定目标",
        CompiledRelativeTeamTargetQuery q => $"全体{Tag(q.RequiredTag)}{Team(q.Team)}{(q.IncludeDefeated ? "（含已阵亡）" : "（存活）")}",
        CompiledFilteredTargetQuery q => $"{Entity(q.Anchor)}{(q.Range >= 0 ? $"周围 {q.Range:0.##} 格内" : "所在战场的")}的" +
            $"{(q.Order == EffectTargetOrder.Nearest ? "最近" : q.Order == EffectTargetOrder.LowestHealthRatio ? "生命比例最低" : q.Order == EffectTargetOrder.HighestHealthRatio ? "生命比例最高" : "按稳定顺序选择")}的" +
            $"{(q.MaxTargets > 0 ? $"至多 {q.MaxTargets} 名" : "全部")}{Tag(q.RequiredTag)}{Team(q.Team)}" +
            $"{(q.IncludeDefeated ? "（含已阵亡）" : "（存活）")}{(!q.IncludeAnchor ? "，排除中心单位" : "")}",
        _ => throw new InvalidOperationException("Unsupported target description.")
    };

    public static string DescribeCondition(CompiledEffectCondition condition) => condition switch
    {
        CompiledEntityAliveCondition c => $"{Entity(c.Entity)}{(c.ExpectedAlive ? "存活" : "已阵亡")}",
        CompiledEntityTagCondition c => $"{Entity(c.Entity)}{(c.ExpectedPresent ? "具有" : "不具有")}「{c.Tag}」标签",
        CompiledHealthRatioCondition c => $"{Entity(c.Entity)}生命比例{Comparison(c.Comparison)} {c.Ratio * 100:0.##}%",
        _ => throw new InvalidOperationException("Unsupported condition description.")
    };

    public static string DescribeMagnitude(CompiledAttributeMagnitude magnitude) => magnitude switch
    {
        CompiledConstantMagnitude c => c.Value.ToString("0.##", CultureInfo.InvariantCulture),
        CompiledSourceAttributeMagnitude s => $"来源的{AttributeName(s.Attribute)}",
        CompiledTargetAttributeMagnitude t => $"目标的{AttributeName(t.Attribute)}",
        CompiledContextValueMagnitude c => c.Key == "InvocationValue" ? "本次触发值" : c.Key == "EventEffectiveValue" ? "事件实际结算量" : $"上下文「{c.Key}」",
        CompiledTeamCountMagnitude c => $"队伍 {c.Team} 的{c.CountKind switch { AttributeTeamCountKind.Persistent => "本战初始非临时单位数", AttributeTeamCountKind.Deployed => "本战初始单位数", _ => "当前存活单位数（含临时单位）" }}",
        CompiledTraitValueMagnitude t => $"队伍 {t.Team} 的「{t.TraitId}」羁绊值",
        CompiledCompositeMagnitude c => c.Operation switch
        {
            AttributeMagnitudeOperation.Add => $"（{string.Join(" + ", c.Operands.Select(DescribeMagnitude))}）",
            AttributeMagnitudeOperation.Multiply => $"（{string.Join(" × ", c.Operands.Select(DescribeMagnitude))}）",
            AttributeMagnitudeOperation.Minimum => $"最小值（{string.Join("、", c.Operands.Select(DescribeMagnitude))}）",
            _ => $"最大值（{string.Join("、", c.Operands.Select(DescribeMagnitude))}）"
        },
        _ => throw new InvalidOperationException("Unsupported magnitude description.")
    };

    public static string DescribeBinding(CompiledEffectBinding binding)
    {
        var condition = binding.Conditions.Length == 0 ? "" : $"当{string.Join("且", binding.Conditions.Select(DescribeCondition))}时，";
        var effects = binding.Effects.Select(step =>
        {
            var amount = step.Magnitude is not null ? DescribeMagnitude(step.Magnitude) : step.AmountSource switch
            {
                EffectAmountSource.InvocationValue => $"触发值 × {step.Amount:0.##}",
                EffectAmountSource.EventEffectiveValue => $"事件实际结算量 × {step.Amount:0.##}",
                _ => $"{step.Amount:0.##}"
            };
            return step.Kind switch { EffectKind.Damage => $"造成 {amount} {DamageTypeName(step.DamageType)}伤害", EffectKind.Heal => $"恢复 {amount} 生命", _ => $"提供 {amount} 护盾" };
        });
        return $"{condition}对{DescribeTarget(binding.TargetQuery)}{string.Join("，", effects)}" +
            (binding.Limits.MaxUses > 0 ? $"；最多触发 {binding.Limits.MaxUses} 次" : "") +
            (binding.Limits.MinimumIntervalTicks > 0 ? $"；间隔至少 {binding.Limits.MinimumIntervalTicks} tick" : "");
    }

    public static string AttributeName(CombatAttribute attribute) => attribute switch
    {
        CombatAttribute.MaxHealth => "最大生命", CombatAttribute.AttackDamage => "攻击力", CombatAttribute.SpellPower => "法强",
        CombatAttribute.AttackSpeed => "攻速", CombatAttribute.Armor => "护甲", CombatAttribute.MagicResistance => "魔抗",
        CombatAttribute.AttackRange => "攻击范围", CombatAttribute.MoveSpeed => "移动速度", CombatAttribute.CriticalChance => "暴击率",
        CombatAttribute.CriticalDamage => "暴击倍率", CombatAttribute.MaxMana => "法力上限", CombatAttribute.StartingMana => "初始法力",
        CombatAttribute.HealingPower => "治疗强度", CombatAttribute.LifeSteal => "吸血", CombatAttribute.ControlResistance => "控制抗性",
        CombatAttribute.ManaPerSecond => "每秒回蓝", CombatAttribute.ManaPerAttack => "攻击回蓝", CombatAttribute.ManaPerDamageRatio => "承伤回蓝倍率",
        CombatAttribute.ManaPerHitCap => "单次受击回蓝上限", _ => attribute.ToString()
    };
    public static string DamageTypeName(EffectDamageType damageType) => damageType switch
    {
        EffectDamageType.Physical => "物理", EffectDamageType.Magical => "魔法", EffectDamageType.True => "真实", _ => throw new InvalidOperationException("Invalid damage type.")
    };
    private static string Team(EffectRelativeTeam team) => team == EffectRelativeTeam.Allies ? "友军" : "敌军";
    private static string Tag(string tag) => string.IsNullOrEmpty(tag) ? "" : $"带「{tag}」标签的";
    private static string Entity(EffectEntityReference entity) => entity switch { EffectEntityReference.Owner => "拥有者", EffectEntityReference.Source => "来源单位", _ => "指定目标" };
    private static string Comparison(EffectComparison comparison) => comparison switch
    {
        EffectComparison.Less => "低于", EffectComparison.LessOrEqual => "不高于", EffectComparison.Equal => "等于", EffectComparison.GreaterOrEqual => "不低于", _ => "高于"
    };
}
