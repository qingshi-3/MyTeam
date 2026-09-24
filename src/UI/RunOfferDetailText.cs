using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

// Prepared once per offer binding from the published catalog. Inspection never simulates a battle.
public static class RunOfferDetailText
{
    public static string UnitSummary(UnitDefinition definition)
    {
        var text = Regex.Replace(definition.Description, @"^\s*HC\d+(?:[／/]BC\d+)?\s*测试稿[：。.]?\s*", "");
        return Regex.Replace(text, @"\s*数值(?:均)?为测试暂值[。.]?", "").Trim();
    }

    public static RunOfferChoiceViewModel Card(RunApplication app, string contentId, string id,
        string title, string action, string rules, string notice, bool disabled, Texture2D? fallback = null)
    {
        if (!app.Content.TryGet(contentId, out var entry))
            return new(id, title, "机会", null, fallback, false, "", "", "", "", "", "", rules, "", action, notice, disabled);
        if (entry.Definition is ItemDefinition item)
            return new(id, item.DisplayName,
                $"{(item.ProductKind == ItemProductKind.Equipment ? "英雄装备" : "团队遗物")} · {PlayerFacingText.DescribeItemRarity(item.Rarity)}",
                null, item.Icon ?? fallback, false, "", "", "", "", "", "", item.Description, ItemRules(app, item) + (string.IsNullOrWhiteSpace(rules) ? "" : "\n\n" + rules), action, notice, disabled);
        if (entry.Definition is not UnitDefinition definition)
            return new(id, title, "机会", null, fallback, false, "", "", "", "", "", "", rules, "", action, notice, disabled);

        var snapshot = BattleSetupFactory.Snapshot(entry, app.Content);
        float Read(CombatAttribute attribute, float fallbackValue) => snapshot.AttributeDefinition?.Attributes
            .FirstOrDefault(value => value.Attribute == attribute)?.BaseValue ?? fallbackValue;
        var speed = Read(CombatAttribute.AttackSpeed, 1);
        var interval = snapshot.AttackTicks * BattleTiming.TickSeconds / Math.Max(.01f, speed);
        var reach = snapshot.AttackDelivery == AttackDelivery.Melee ? "近战触及（身体外）" : "远程射程";
        var active = BattleLabTooltipFormatter.AbilityGroups((snapshot.AbilityLoadout?.Abilities ?? [])
            .Where(ability => ability.IsDisplayedActiveSkill));
        var passive = BattleLabTooltipFormatter.AbilityGroups((snapshot.AbilityLoadout?.Abilities ?? [])
            .Where(ability => !ability.IsDisplayedActiveSkill));
        // Component-backed HC01 growth uses the same formatter as the official unit inspector.
        var componentPassive = BattleLabTooltipFormatter.Abilities(snapshot with { AbilityLoadout = null }, "", false);
        if (!string.IsNullOrWhiteSpace(componentPassive))
            passive += (string.IsNullOrWhiteSpace(passive) ? "" : "\n\n") + componentPassive;
        var extra = $"{reach} {Read(CombatAttribute.AttackRange, definition.AttackRange):0.##} 格" +
            $"\n法强 {Read(CombatAttribute.SpellPower, definition.SpellPower):0.#}" +
            $"\n治疗强度 {Read(CombatAttribute.HealingPower, definition.HealPower):0.#}　控制抗性 {Read(CombatAttribute.ControlResistance, definition.BaseControlResistance):P0}" +
            $"\n暴击率 {Read(CombatAttribute.CriticalChance, 0):P0}　暴击伤害 {Read(CombatAttribute.CriticalDamage, 1.5f):P0}" +
            $"\n吸血 {Read(CombatAttribute.LifeSteal, definition.LifeSteal):P0}" +
            (snapshot.Behavior.DisableBasicAttacks ? "\n不进行普攻" : $"\n攻击间隔 {interval:0.##} 秒 · 攻速倍率 ×{speed:0.##}");
        var mana = Read(CombatAttribute.MaxMana, definition.MaxMana);
        if (mana > 0 && snapshot.AbilityLoadout?.Abilities.Any(a => a.Trigger == AbilityTriggerKind.ManaFull) == true)
            extra += $"\n初始法力 {Read(CombatAttribute.StartingMana, definition.StartingMana):0.#} / {mana:0.#} · 满蓝自动施法" +
                $"\n每秒回蓝 {Read(CombatAttribute.ManaPerSecond, definition.ManaPerSecond):0.#}　普攻回蓝 {Read(CombatAttribute.ManaPerAttack, definition.ManaPerAttack):0.#}";
        if (!string.IsNullOrWhiteSpace(rules)) extra += "\n\n" + rules;
        var role = PlayerFacingText.DescribeUnitRole(definition.Role);
        var delivery = snapshot.AttackDelivery == AttackDelivery.Melee ? "近战" : "远程";
        var tier = app.Project.Campaign.RecruitmentSupply?.TierOf(contentId) ?? 0;
        return new(id, definition.DisplayName,
            (tier > 0 ? $"{tier} 阶 · " : "") + (role == delivery ? role : role + " · " + delivery),
            definition.Portrait, definition.Icon ?? fallback, true,
            Read(CombatAttribute.MaxHealth, definition.MaxHealth).ToString("0.#"),
            Read(CombatAttribute.AttackDamage, definition.AttackDamage).ToString("0.#"),
            Read(CombatAttribute.Armor, definition.Armor).ToString("0.#"),
            snapshot.Behavior.DisableBasicAttacks ? "—" : (1 / interval).ToString("0.##"),
            active, passive, string.IsNullOrWhiteSpace(active + passive) ? UnitSummary(definition) : "", extra, action, notice, disabled);
    }

    private static string ItemRules(RunApplication app, ItemDefinition item)
    {
        var graph = app.Content.Graph;
        string ContentName(string id) => app.Content.TryGet(id, out var entry) && entry.Definition is UnitDefinition unit
            ? unit.DisplayName : "临时单位";
        var facts = new List<string>();
        if (item.ProductKind == ItemProductKind.Equipment)
        {
            var equipment = graph.ResolveEquipment(item.Id);
            facts.Add("穿戴后作用于持有者，取下后移除装备贡献。");
            facts.AddRange(equipment.AttributeModifiers.Select(Modifier));
            if (!equipment.GrantedStatuses.IsDefaultOrEmpty)
                facts.AddRange(equipment.GrantedStatuses.Select(status => $"穿戴时获得「{status.DisplayName}」：{StatusModelText.Describe(status)}"));
            facts.AddRange(equipment.ReactiveStatusBindings.Select(binding =>
                $"拥有者作为{(binding.OwnerRole == StatusReactiveOwnerRole.OwnerIsSource ? "发起方" : "承受方")}发生{Event(binding.EventKind)}时，" +
                $"以{(binding.Source == EquipmentReactiveStatusSource.Owner ? "持有者" : "该装备")}为来源，向{(binding.Target == EquipmentReactiveStatusTarget.Owner ? "自身" : binding.Target == EquipmentReactiveStatusTarget.EventSource ? "事件发起者" : "事件承受者")}施加「{binding.Status.DisplayName}」：{StatusModelText.Describe(binding.Status)}"));
            if (!equipment.TraitContributions.IsDefaultOrEmpty)
                facts.Add("羁绊贡献：" + string.Join("、", equipment.TraitContributions.Select(trait =>
                    $"{(graph.TryGetTrait(trait.TraitId, out var named) ? named.DisplayName : "羁绊")} +{trait.Value}")));
        }
        else if (item.ProductKind == ItemProductKind.Relic)
        {
            var relic = graph.ResolveRelic(item.Id);
            facts.Add($"当前持有 {app.ActiveRun?.Items.Where(entry => entry.ContentId == item.Id).Sum(entry => entry.Stacks) ?? 0} 层（本候选尚未领取）。");
            foreach (var binding in relic.AttributeBindings)
            {
                var stackText = binding.StackPolicy == RelicAttributeStackPolicy.PerStack
                    ? "每层独立应用"
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
                    ? "开场每层执行一次" : "同类遗物每场仅执行一次";
                if (effect is CompiledRelicBattleStartShield shield)
                    facts.Add($"{repeat}：为我方初始非临时单位各提供 {shield.Amount} 护盾。" +
                        (shield.Effect.Conditions.Length == 0 ? "" : "条件：" + string.Join("且", shield.Effect.Conditions.Select(EffectModelText.DescribeCondition))));
                else if (effect is CompiledRelicBattleStartSummon summon)
                    facts.Add($"{repeat}：召唤{ContentName(summon.ContentId)}，" +
                        $"生命倍率 {summon.HealthMultiplier:0.##}，攻击倍率 {summon.DamageMultiplier:0.##}。");
            }
            if (!relic.StatusGrants.IsDefaultOrEmpty)
                facts.AddRange(relic.StatusGrants.Select(grant =>
                    $"每层向{RelicTarget(grant.Target)}授予「{grant.Status.DisplayName}」：{StatusModelText.Describe(grant.Status)}"));
            foreach (var counter in relic.ReactiveCounters)
            {
                var team = counter.Team == 0 ? "我方" : "敌方";
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
                    _ => counter.TargetTeam == 0 ? "我方首个存活单位" : "敌方首个存活单位"
                };
                facts.Add($"{input}（{temporary}）。达到 {counter.Threshold} 次时消耗 {counter.Consumption} 次，目标为{target}：" +
                    EffectModelText.DescribeBinding(counter.ThresholdEffect) +
                    $"；{(counter.Scope == RelicCounterScope.Battle ? "本场计数" : "本局计数")}，{(counter.ResetPolicy == RelicCounterResetPolicy.BattleEnd ? "战后清零" : "本局结束清零")}。");
            }
            if (!relic.VictoryOutcomes.IsDefaultOrEmpty)
                facts.AddRange(relic.VictoryOutcomes.Select(outcome =>
                    $"正式流程胜利结算：每层额外 {outcome.Amount} 金币。"));
        }
        var text = string.Join("\n\n", facts);
        foreach (var trait in graph.Traits)
            text = text.Replace(trait.StableId, trait.DisplayName, StringComparison.Ordinal);
        return text;
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
        CompiledRelicPlayerArmyTarget => "我方初始非英雄单位（不含临时召唤）",
        CompiledRelicPlayerSummonsTarget => "我方临时召唤物（含战斗中后续生成）",
        CompiledRelicPlayerHeroesTarget or CompiledRelicPlayerEmptySlotHeroesTarget => "我方初始英雄（不含临时单位）",
        CompiledRelicPlayerFormationAdjacentTarget => "我方身边 1.5 格身体间距内有存活友军的单位",
        _ => "合格我方单位"
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

}
