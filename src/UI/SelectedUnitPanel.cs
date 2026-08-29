using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class SelectedUnitPanel : PanelContainer
{
    private Label _title = null!;
    private SemanticChip _role = null!;
    private SemanticChip _reach = null!;
    private SemanticChip _health = null!;
    private Label _action = null!;

    public override void _Ready()
    {
        _title = GetNode<Label>("%UnitTitle");
        _role = GetNode<SemanticChip>("%UnitRoleFact");
        _reach = GetNode<SemanticChip>("%UnitReachFact");
        _health = GetNode<SemanticChip>("%UnitHealthFact");
        _action = GetNode<Label>("%UnitAction");
    }

    public void Bind(BattleUnitState state)
    {
        Visible = true;
        var hero = state.Definition.IsHero ? "★ 英雄 · " : string.Empty;
        _title.Text = hero + state.Definition.DisplayName;
        _role.Bind(UnitSemanticFacts.Responsibility(state.Definition.Role, includeLabel: false));
        _reach.Bind(UnitSemanticFacts.Reach(state.Definition.Range));
        _health.Bind(UnitSemanticFacts.Health($"{state.Health:0}/{state.MaxHealth:0}"));
        _action.Text = DescribeAction(state);
    }

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
                $"行动：治疗冷却 {state.AttackCooldown * BattleSimulation.TickSeconds:0.0} 秒{target}",
            BattleUnitMode.Recovering =>
                $"行动：攻击冷却 {state.AttackCooldown * BattleSimulation.TickSeconds:0.0} 秒{target}",
            BattleUnitMode.Disabled => $"行动：被控制，剩余 {state.DisabledTicks * BattleSimulation.TickSeconds:0.0} 秒",
            BattleUnitMode.Defeated => "行动：已被击败",
            _ => "行动：正在判断战况"
        };
    }
}
