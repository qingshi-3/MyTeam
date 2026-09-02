using System;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Presentation;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.UI;

public partial class SelectedUnitPanel : PanelContainer
{
    private Label _title = null!;
    private SemanticChip _role = null!;
    private SemanticChip _reach = null!;
    private SemanticChip _health = null!;
    private Label _identity = null!;
    private Label _stats = null!;
    private Label _equipment = null!;
    private Label _traits = null!;
    private Label _action = null!;
    private Label _statuses = null!;

    public override void _Ready()
    {
        _title = GetNode<Label>("%UnitTitle");
        _role = GetNode<SemanticChip>("%UnitRoleFact");
        _reach = GetNode<SemanticChip>("%UnitReachFact");
        _health = GetNode<SemanticChip>("%UnitHealthFact");
        _identity = GetNode<Label>("%UnitIdentity");
        _stats = GetNode<Label>("%UnitStats");
        _equipment = GetNode<Label>("%UnitEquipment");
        _traits = GetNode<Label>("%UnitTraits");
        _action = GetNode<Label>("%UnitAction");
        _statuses = GetNode<Label>("%UnitStatuses");
    }

    public void Bind(BattleScreenRuntimeUnitSnapshot state)
    {
        Visible = true;
        var hero = state.IsHero ? "★ 英雄 · " : string.Empty;
        _title.Text = hero + state.DisplayName;
        _role.Bind(UnitSemanticFacts.Responsibility(state.Role, includeLabel: false));
        _reach.Bind(UnitSemanticFacts.Reach(state.Reach));
        _health.Bind(UnitSemanticFacts.Health($"{state.Health:0}/{state.MaxHealth:0}"));
        _identity.Text = $"内容：{state.ContentId}\n来源：{DisplayIdentity(state.SourceInstanceId)} · 运行：{state.RuntimeId}";
        _stats.Text = $"伤害 {state.Damage:0.#} · 攻速 {state.AttackSpeed:0.##} · 射程 {state.Reach:0.#} · 控抗 {state.ControlResistance:0.##}";
        _equipment.Text = state.Team != 0
            ? "装备：不适用（PvE）"
            : state.Equipment.IsDefaultOrEmpty
                ? "装备：无"
                : "装备：" + string.Join(" ｜ ", state.Equipment.Select(item =>
                    $"槽{item.SlotIndex + 1} {item.ContentId} [{item.InstanceId}]"));
        var unitTraits = state.TraitContributions.IsDefaultOrEmpty
            ? "无"
            : string.Join("、", state.TraitContributions.Select(item =>
                $"{item.TraitId}+{item.Value}({item.SourceInstanceId})"));
        var teamTraits = state.TeamTraits.IsDefaultOrEmpty
            ? "无激活层级"
            : string.Join("、", state.TeamTraits.Select(item => item.Text));
        _traits.Text = $"单位特质：{unitTraits}\n团队层级：{teamTraits}";
        _action.Text = DescribeAction(state);
        _statuses.Text = DescribeStatuses(state.Statuses);
    }

    private static string DisplayIdentity(string sourceInstanceId) =>
        string.IsNullOrWhiteSpace(sourceInstanceId) ? "临时单位" : sourceInstanceId;

    public static string DescribeAction(BattleUnitState state)
    {
        var target = string.IsNullOrWhiteSpace(state.ActionTargetName) ? string.Empty : $"：{state.ActionTargetName}";
        return state.Mode switch
        {
            BattleUnitMode.Seeking => $"行动：正在接敌{target}",
            BattleUnitMode.Moving => $"行动：正在移动{target}",
            BattleUnitMode.Waiting => $"行动：等待可用路线{target}",
            BattleUnitMode.Attacking => $"行动：正在攻击{target}",
            BattleUnitMode.Casting => $"行动：正在治疗{target}",
            BattleUnitMode.Recovering when state.LastActionKind == BattleActionKind.Heal =>
                $"行动：治疗冷却 {state.AttackCooldown * BattleTiming.TickSeconds:0.0} 秒{target}",
            BattleUnitMode.Recovering =>
                $"行动：攻击冷却 {state.AttackCooldown * BattleTiming.TickSeconds:0.0} 秒{target}",
            BattleUnitMode.Disabled => $"行动：被控制，剩余 {DisabledTicks(state) * BattleTiming.TickSeconds:0.0} 秒",
            BattleUnitMode.Defeated => "行动：已被击败",
            _ => "行动：正在判断战况"
        };
    }

    private static string DescribeAction(BattleScreenRuntimeUnitSnapshot state)
    {
        var target = string.IsNullOrWhiteSpace(state.ActionTargetName) ? string.Empty : $"：{state.ActionTargetName}";
        return state.Mode switch
        {
            BattleUnitMode.Seeking => $"行动：正在接敌{target}",
            BattleUnitMode.Moving => $"行动：正在移动{target}",
            BattleUnitMode.Waiting => $"行动：等待可用路线{target}",
            BattleUnitMode.Attacking => $"行动：正在攻击{target}",
            BattleUnitMode.Casting => $"行动：正在治疗{target}",
            BattleUnitMode.Recovering when state.LastActionKind == BattleActionKind.Heal =>
                $"行动：治疗冷却 {state.AttackCooldown * BattleTiming.TickSeconds:0.0} 秒{target}",
            BattleUnitMode.Recovering =>
                $"行动：攻击冷却 {state.AttackCooldown * BattleTiming.TickSeconds:0.0} 秒{target}",
            BattleUnitMode.Disabled => $"行动：被控制，剩余 {DisabledTicks(state) * BattleTiming.TickSeconds:0.0} 秒",
            BattleUnitMode.Defeated => "行动：已被击败",
            _ => "行动：正在判断战况"
        };
    }

    private static int DisabledTicks(BattleUnitState state) => Math.Max(
        state.DisabledTicks,
        state.Statuses.Where(status => status.GrantedTags.Contains(
                StatusDefinitionCompiler.ActionDisabledTag,
                System.StringComparer.Ordinal))
            .Select(status => status.RemainingTicks)
            .DefaultIfEmpty(0)
            .Max());

    private static int DisabledTicks(BattleScreenRuntimeUnitSnapshot state) => Math.Max(
        state.DisabledTicks,
        state.Statuses.Where(status => status.GrantedTags.Contains(
                StatusDefinitionCompiler.ActionDisabledTag,
                StringComparer.Ordinal))
            .Select(status => status.RemainingTicks)
            .DefaultIfEmpty(0)
            .Max());

    private static string DescribeStatuses(System.Collections.Immutable.ImmutableArray<StatusRuntimeSnapshot> statuses)
    {
        if (statuses.IsDefaultOrEmpty) return "状态：无";
        var facts = statuses.Select(status =>
        {
            var stacks = status.Stacks > 1 ? $" ×{status.Stacks}" : string.Empty;
            var duration = status.Permanent
                ? "本场战斗"
                : $"{status.RemainingTicks * BattleTiming.TickSeconds:0.0} 秒";
            var contributions = status.SourceContributions.IsDefaultOrEmpty
                ? status.SourceId
                : string.Join(',', status.SourceContributions.Select(source =>
                    $"{source.SourceId}×{source.Stacks}"));
            return $"{status.DisplayName}{stacks} · {duration} · 来源 {contributions}";
        });
        return "状态：" + string.Join(" ｜ ", facts);
    }
}
