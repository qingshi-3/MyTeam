using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.UI;

// Consumes already published/prepared data. Hover never instantiates content or simulates a battle.
public static class BattleLabTooltipFormatter
{
    public static BattleLabTooltipInfo Prototype(BattleLabPublishedUnit unit, UnitSnapshot snapshot)
    {
        var definition = unit.Definition;
        return new(unit.DisplayName, Abilities: Abilities(snapshot, definition.Description, false),
            IconStats: AttributeFacts(snapshot, definition.MaxHealth, definition.AttackDamage, definition.Armor,
                BaseAttribute(snapshot, CombatAttribute.AttackSpeed, 1), definition.AttackRange,
                definition.BaseControlResistance,
                BaseAttribute(snapshot, CombatAttribute.StartingMana, definition.StartingMana),
                BaseAttribute(snapshot, CombatAttribute.MaxMana, definition.MaxMana),
                BaseAttribute(snapshot, CombatAttribute.ManaPerSecond, definition.ManaPerSecond)));
    }

    public static BattleLabTooltipInfo Instance(BattleLabPublishedUnit unit, UnitSnapshot snapshot,
        BattleLabUnitConfiguration instance, BattleLabPreparedUnitProjection? prepared,
        BattleLabDerivedProjection? derived, BattleLabContentIndex content)
    {
        var stats = prepared is null
            ? "当前配置属性暂不可用；基础能力见下方。"
            : "";
        var loadout = new List<string>();
        if (!instance.Equipment.IsDefaultOrEmpty)
        {
            var equipment = instance.Equipment.OrderBy(item => item.SlotIndex).Select(item =>
                $"{item.SlotIndex + 1}号槽：{content.Equipment.FirstOrDefault(candidate => candidate.StableId == item.ContentId)?.DisplayName ?? "未识别装备"}");
            loadout.Add("装备\n" + string.Join("\n", equipment));
        }
        if (prepared is not null && !prepared.TraitContributions.IsDefaultOrEmpty)
        {
            var traits = prepared.TraitContributions.GroupBy(item => item.TraitId).Select(group =>
            {
                var current = derived?.Traits.FirstOrDefault(trait => trait.TraitId == group.Key && trait.Team == (int)instance.Side);
                var name = current?.DisplayName ?? content.Package.Content.Graph.Traits.FirstOrDefault(trait => trait.StableId == group.Key)?.DisplayName ?? "羁绊";
                var team = current is null ? "" : $"（全队 {current.Value}，{(current.ActiveMinValue.HasValue ? "已激活" : "未激活")}）";
                return $"{name} +{group.Sum(item => item.Value)}{team}";
            });
            loadout.Add("羁绊贡献：" + string.Join("、", traits));
        }
        if (snapshot.AttackHitGrowth is { RetentionUpgradeAvailable: true })
            loadout.Add(instance.RetainAttackStacks ? "升阶：换目标保留攻速层数" : "升阶：基础版，换目标清层");
        if (prepared is not null && !prepared.Statuses.IsDefaultOrEmpty)
            loadout.Add("开场状态：" + string.Join("、", prepared.Statuses.Select(status => status.DisplayName).Distinct()));
        return new(unit.DisplayName, Stats: stats,
            Abilities: Abilities(snapshot, unit.Definition.Description, instance.RetainAttackStacks), Loadout: string.Join("\n\n", loadout),
            IconStats: prepared is null ? [] : AttributeFacts(snapshot, prepared.Health, prepared.Damage,
                prepared.Armor, prepared.AttackSpeed, prepared.Reach, prepared.ControlResistance,
                prepared.Mana, prepared.MaxMana, prepared.ManaPerSecond));
    }

    public static BattleLabTooltipInfo Item(ItemDefinition definition, int stacks = 1) => new(
        definition.DisplayName,
        $"{(definition.ProductKind == ItemProductKind.Equipment ? "装备" : "团队遗物")} · {PlayerFacingText.DescribeItemRarity(definition.Rarity)}" +
        (stacks > 1 ? $" · 当前 {stacks} 层" : ""),
        Abilities: definition.Description,
        Hint: definition.ProductKind == ItemProductKind.Equipment ? "装入所选单位的装备槽。" : "配置后作用于A 队团队。");

    public static BattleLabTooltipInfo Item(BattleLabPublishedItem item, BattleLabContentIndex content, int stacks = 1)
    {
        var result = Item(item.Definition, stacks);
        var graph = content.Package.Content.Graph;
        var facts = new List<string>();
        if (item.Definition.ProductKind == ItemProductKind.Equipment && graph.TryGetEquipment(item.StableId, out var equipment))
        {
            facts.AddRange(equipment.AttributeModifiers.Select(Modifier));
            if (!equipment.GrantedStatuses.IsDefaultOrEmpty)
                facts.AddRange(equipment.GrantedStatuses.Select(status => $"穿戴时获得「{status.DisplayName}」：{StatusModelText.Describe(status)}"));
            facts.AddRange(equipment.ReactiveStatusBindings.Select(binding =>
                $"拥有者作为{(binding.OwnerRole == StatusReactiveOwnerRole.OwnerIsSource ? "发起方" : "承受方")}发生{Event(binding.EventKind)}时，" +
                $"向{(binding.Target == EquipmentReactiveStatusTarget.Owner ? "自身" : binding.Target == EquipmentReactiveStatusTarget.EventSource ? "事件发起者" : "事件承受者")}施加「{binding.Status.DisplayName}」：{StatusModelText.Describe(binding.Status)}"));
            if (!equipment.TraitContributions.IsDefaultOrEmpty)
                facts.Add("羁绊贡献：" + string.Join("、", equipment.TraitContributions.Select(trait =>
                    $"{(graph.TryGetTrait(trait.TraitId, out var named) ? named.DisplayName : "羁绊")} +{trait.Value}")));
        }
        else if (item.Definition.ProductKind == ItemProductKind.Relic && graph.TryGetRelic(item.StableId, out var relic))
        {
            foreach (var binding in relic.AttributeBindings)
            {
                var stackText = binding.StackPolicy == RelicAttributeStackPolicy.PerStack
                    ? $"每层独立应用（当前 {Math.Max(1, stacks)} 层）"
                    : "按空部署位数与同类遗物总层数累计";
                var effect = Modifier(binding.Modifier);
                if (binding.StackPolicy == RelicAttributeStackPolicy.LinearAcrossStacksAndInstances &&
                    binding.Modifier.Magnitude is CompiledConstantMagnitude amount)
                    effect = binding.Modifier.Operation == AttributeModifierOperation.Multiply
                        ? $"每个空部署位、每层使{EffectModelText.AttributeName(binding.Modifier.Attribute)}增加 {(amount.Value - 1) * 100:0.##}%（增幅相加）"
                        : $"每个空部署位、每层使{EffectModelText.AttributeName(binding.Modifier.Attribute)}增加 {amount.Value:0.##}";
                facts.Add($"{RelicTarget(binding.Target)}：{effect}；{stackText}。");
            }
            foreach (var effect in relic.BattleStartEffects)
            {
                var repeat = effect.RepeatPolicy == RelicBattleStartRepeatPolicy.PerStack
                    ? $"开场按层数执行（当前 {Math.Max(1, stacks)} 次）" : "同类遗物每场仅执行一次";
                if (effect is CompiledRelicBattleStartShield shield)
                    facts.Add($"{repeat}：为A 队初始非临时单位各提供 {shield.Amount} 护盾。" +
                        (shield.Effect.Conditions.Length == 0 ? "" : "条件：" + string.Join("且", shield.Effect.Conditions.Select(EffectModelText.DescribeCondition))));
                else if (effect is CompiledRelicBattleStartSummon summon)
                    facts.Add($"{repeat}：召唤{(content.TryGetUnit(summon.ContentId, out var summoned) ? summoned.DisplayName : "临时单位")}，" +
                        $"生命倍率 {summon.HealthMultiplier:0.##}，攻击倍率 {summon.DamageMultiplier:0.##}。");
            }
            if (!relic.StatusGrants.IsDefaultOrEmpty)
                facts.AddRange(relic.StatusGrants.Select(grant =>
                    $"每层向{RelicTarget(grant.Target)}授予「{grant.Status.DisplayName}」：{StatusModelText.Describe(grant.Status)}"));
            foreach (var counter in relic.ReactiveCounters)
            {
                var team = counter.Team == 0 ? "A 队" : "B 队";
                var temporary = counter.IncludeTemporary ? "含临时单位" : "不含临时单位";
                var input = counter.Source switch
                {
                    RelicCounterSourceKind.Population => $"发生{Event(counter.EventKind)}时，按{team}初始单位数增加计数",
                    RelicCounterSourceKind.Alive => $"发生{Event(counter.EventKind)}时，按{team}存活人数增加计数",
                    RelicCounterSourceKind.Attack => $"{team}单位发生{Event(counter.EventKind)}时增加 1 次计数",
                    _ => $"{team}单位阵亡时增加 1 次计数"
                };
                var target = counter.Target switch
                {
                    RelicThresholdTargetKind.EventSource => "触发事件的发起者",
                    RelicThresholdTargetKind.EventTarget => "触发事件的承受者",
                    _ => counter.TargetTeam == 0 ? "A 队首个存活单位" : "B 队首个存活单位"
                };
                facts.Add($"{input}（{temporary}）。达到 {counter.Threshold} 次时消耗 {counter.Consumption} 次，目标为{target}：" +
                    EffectModelText.DescribeBinding(counter.ThresholdEffect) +
                    $"；{(counter.ResetPolicy == RelicCounterResetPolicy.BattleEnd ? "战后清零" : "本局结束清零")}。");
            }
            if (!relic.VictoryOutcomes.IsDefaultOrEmpty)
                facts.AddRange(relic.VictoryOutcomes.Select(outcome =>
                    $"正式流程胜利结算：每层额外 {outcome.Amount} 金币；实验室不发放这项局内收益。"));
        }
        if (facts.Count == 0) return result;
        var text = string.Join("\n\n", facts);
        // Human-readable helpers may refer to a known trait in a value expression; show its name.
        foreach (var trait in graph.Traits) text = text.Replace(trait.StableId, trait.DisplayName, StringComparison.Ordinal);
        return result with { Abilities = text };
    }

    private static string Modifier(CompiledAttributeModifier modifier)
    {
        var name = modifier.Attribute == CombatAttribute.DodgeChance ? "闪避率" : EffectModelText.AttributeName(modifier.Attribute);
        var amount = EffectModelText.DescribeMagnitude(modifier.Magnitude);
        if (modifier.Magnitude is CompiledConstantMagnitude constant && modifier.Operation != AttributeModifierOperation.Multiply &&
            modifier.Attribute is CombatAttribute.CriticalChance or CombatAttribute.LifeSteal or CombatAttribute.ControlResistance or CombatAttribute.DodgeChance)
            amount = $"{constant.Value * 100:0.##}%";
        return name + (modifier.Operation switch
        {
            AttributeModifierOperation.Add => "增加 ", AttributeModifierOperation.Multiply => "乘以 ", _ => "设为 "
        }) + amount;
    }

    private static string RelicTarget(CompiledRelicUnitTarget target) => target switch
    {
        CompiledRelicPlayerArmyTarget => "A 队初始非英雄单位（不含临时召唤）",
        CompiledRelicPlayerSummonsTarget => "A 队临时召唤物（含战斗中后续生成）",
        CompiledRelicPlayerHeroesTarget or CompiledRelicPlayerEmptySlotHeroesTarget => "A 队初始英雄（不含临时单位）",
        CompiledRelicPlayerFormationAdjacentTarget => "A 队身边 1.5 格身体间距内有存活友军的单位",
        _ => "合格A 队单位"
    };

    private static string Event(BattleCombatEventKind kind) => kind switch
    {
        BattleCombatEventKind.BattleStarted => "战斗开始", BattleCombatEventKind.UnitMoved => "移动",
        BattleCombatEventKind.UnitSummoned => "召唤", BattleCombatEventKind.CriticalHit => "普攻暴击",
        BattleCombatEventKind.AttackDodged => "闪避普攻", BattleCombatEventKind.HealthLost => "实际生命受损",
        BattleCombatEventKind.ManaSkillResolved => "满蓝技能施放", BattleCombatEventKind.ControlApplied => "成功施加控制",
        BattleCombatEventKind.UnitRevived => "返场", BattleCombatEventKind.AllegianceChanged => "阵营变化",
        BattleCombatEventKind.BattleCompleted => "战斗结束", BattleCombatEventKind.StatusApplied => "状态施加",
        BattleCombatEventKind.StatusStackChanged => "状态层数变化", BattleCombatEventKind.StatusRemoved => "状态移除",
        _ => StatusModelText.EventName(kind)
    };

    private static ImmutableArray<UnitAttributeFact> AttributeFacts(UnitSnapshot snapshot, float health,
        float damage, float armor, float speed, float range, float resistance, float mana, float maxMana, float manaPerSecond)
    {
        var interval = snapshot.AttackTicks * BattleTiming.TickSeconds / Math.Max(.01f, speed);
        var facts = ImmutableArray.CreateBuilder<UnitAttributeFact>();
        facts.Add(new(SemanticIconKeys.Health, $"{health:0.#}", "生命", ""));
        facts.Add(new(SemanticIconKeys.Attack, $"{damage:0.#}", "攻击", ""));
        facts.Add(new(SemanticIconKeys.Armor, $"{armor:0.#}", "防御", ""));
        facts.Add(new(SemanticIconKeys.Time, snapshot.Behavior.DisableBasicAttacks ? "—" : $"{interval:0.##}s", "间隔", "普攻间隔"));
        facts.Add(new(snapshot.AttackDelivery == AttackDelivery.Melee ? SemanticIconKeys.Melee : SemanticIconKeys.Ranged,
            snapshot.Behavior.DisableBasicAttacks ? "—" : $"{range:0.##}", "射程", "以格为单位"));
        if (maxMana > 0)
        {
            facts.Add(new(SemanticIconKeys.Mana, $"{mana:0.#}/{maxMana:0.#}", "法力", "初始／上限法力"));
            facts.Add(new(SemanticIconKeys.Mana, $"+{manaPerSecond:0.#}/s", "回蓝", "每秒法力回复"));
        }
        if (resistance > 0) facts.Add(new(SemanticIconKeys.ControlResistance, $"{resistance:P0}", "抗控", "控制抗性"));
        return facts.ToImmutable();
    }

    public static string AbilityGroups(IEnumerable<CompiledAbilityDefinition> abilities) => string.Join("\n\n",
        abilities.GroupBy(ability => (Active: ability.IsDisplayedActiveSkill, ability.DisplayName))
            .Select(group => $"{(group.Key.Active ? "主动" : "被动")} · {group.Key.DisplayName}\n" +
                string.Join("\n", group.Select(ability => string.IsNullOrWhiteSpace(ability.AuthoredDescription)
                    ? ability.Description : ability.AuthoredDescription).Distinct())));

    public static string Abilities(UnitSnapshot snapshot, string fallback, bool retain)
    {
        var rows = new List<string>();
        var groups = AbilityGroups(snapshot.AbilityLoadout?.Abilities ?? []);
        if (!string.IsNullOrWhiteSpace(groups)) rows.Add(groups);
        if (snapshot.AttackHitGrowth is { } growth)
            rows.Add($"被动 · 连续命中成长\n命中：攻速 +{growth.AttackSpeedPerHit:0.##}，无上限。\n" +
                ((!growth.ResetOnTargetChange || retain) ? "换目标保层；" : "换目标清层；") + "战后清空。");
        return rows.Count == 0 ? fallback : string.Join("\n\n", rows);
    }

    private static float BaseAttribute(UnitSnapshot snapshot, CombatAttribute attribute, float fallback) =>
        snapshot.AttributeDefinition?.Attributes.FirstOrDefault(item => item.Attribute == attribute)?.BaseValue ?? fallback;
}
