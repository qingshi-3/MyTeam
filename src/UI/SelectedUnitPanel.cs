using System;
using System.Linq;
using System.Collections.Generic;
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
    private ProgressBar _mana = null!;
    private Label _manaText = null!;
    private SemanticChip _shield = null!;
    private Label _detail = null!;
    private Button _ability = null!;
    private GridContainer _statusTiles = null!;
    private readonly Dictionary<string, CombatFactTile> _statusViews = new(StringComparer.Ordinal);
    private CombatFactTile[] _slots = [];
    private string _boundRuntimeId = "";
    private CombatFactTile? _detailTile;
    [Export] public PackedScene FactTileScene { get; set; } = null!;

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
        _mana = GetNode<ProgressBar>("%ManaBar");
        _manaText = GetNode<Label>("%ManaText");
        _shield = GetNode<SemanticChip>("%UnitShieldFact");
        _detail = GetNode<Label>("%FactDetail");
        _ability = GetNode<Button>("%UnitAbility");
        _ability.Pressed += ShowAbilityDetail;
        _statusTiles = GetNode<GridContainer>("%StatusTiles");
        _slots = [GetNode<CombatFactTile>("%SlotOne"), GetNode<CombatFactTile>("%SlotTwo"), GetNode<CombatFactTile>("%SlotThree")];
        foreach (var slot in _slots) slot.DetailRequested += ShowDetail;
    }

    public void Bind(BattleScreenRuntimeUnitSnapshot state)
    {
        Visible = true;
        var hero = state.IsPersistentRosterHero || state.IsHero ? "★ 英雄 · " : string.Empty;
        _title.Text = hero + state.DisplayName;
        _role.Bind(UnitSemanticFacts.Responsibility(state.Role, includeLabel: false));
        _reach.Bind(UnitSemanticFacts.Reach(state.Reach));
        _health.Bind(UnitSemanticFacts.Health($"{state.Health:0}/{state.MaxHealth:0}"));
        _identity.Visible = true;
        _identity.Text = state.IsTemporary ? "临时单位 · 仅限本场" : state.Team != 0 ? "敌方单位" : "我方英雄";
        if (_boundRuntimeId != state.RuntimeId)
        {
            _boundRuntimeId = state.RuntimeId;
            _detailTile = null;
            _detail.Text = "点选装备或状态查看效果";
        }
        _stats.Text = $"伤害 {state.Damage:0.#} · 攻速 {state.AttackSpeed:0.##} · 射程 {state.Reach:0.#} · 控抗 {state.ControlResistance:0.##}";
        _equipment.Text = "装备";
        _equipment.Visible = state.IsPersistentRosterHero;
        for (var index = 0; index < _slots.Length; index++)
        {
            _slots[index].Visible = state.IsPersistentRosterHero;
            var item = state.Equipment.IsDefaultOrEmpty ? null : state.Equipment.FirstOrDefault(item => item.SlotIndex == index);
            _slots[index].Bind(item?.Icon ?? SemanticIcons.Catalog.ResolveIcon("loot"),
                item?.DisplayName ?? "空槽", $"装备 {index + 1}",
                item is null ? "这个装备槽为空。在备战或队伍中穿戴装备。" : $"{item.DisplayName}\n{item.Description}");
        }
        var teamTraits = state.TeamTraits.IsDefaultOrEmpty
            ? "无激活层级"
            : string.Join("、", state.TeamTraits.Select(item => item.Text));
        _traits.Text = $"团队层级：{teamTraits}";
        _traits.Visible = !state.TeamTraits.IsDefaultOrEmpty;
        _action.Text = DescribeAction(state);
        _shield.Visible = state.Shield > 0;
        _shield.Bind("shield", $"护盾 {state.Shield:0}");
        _mana.Visible = _manaText.Visible = state.MaxMana > 0;
        _mana.MaxValue = Math.Max(1, state.MaxMana);
        _mana.Value = state.CurrentMana;
        _manaText.Text = state.Mode == BattleUnitMode.Casting && state.LastActionKind == BattleActionKind.Ability
            ? $"正在施放 · {state.LastAbilityName}"
            : $"法力 {state.CurrentMana:0}/{state.MaxMana:0} · " +
                (state.CurrentMana >= state.MaxMana ? "已就绪，等待施法条件" : "自动积蓄");
        var abilities = state.Abilities.IsDefaultOrEmpty ? [] : state.Abilities.Where(item => item.Trigger == TowerAutobattler.Abilities.AbilityTriggerKind.ManaFull).ToArray();
        _ability.Visible = abilities.Length > 0;
        _ability.Text = string.Join("\n", abilities.Select(item => $"{item.DisplayName} · 满蓝自动释放"));
        _ability.TooltipText = string.Join("\n\n", abilities.Select(item => $"{item.DisplayName}\n{item.Description}"));
        BindStatuses(state);
        if (_detailTile is not null) _detail.Text = _detailTile.DetailText;
    }

    private void ShowDetail(CombatFactTile tile)
    {
        _detailTile = tile;
        _detail.Text = tile.DetailText;
    }
    private void ShowAbilityDetail()
    {
        _detailTile = null;
        _detail.Text = _ability.TooltipText;
    }

    private void BindStatuses(BattleScreenRuntimeUnitSnapshot state)
    {
        var statuses = state.Statuses.IsDefaultOrEmpty ? [] : state.Statuses.ToArray();
        _statuses.Text = statuses.Length == 0 ? "暂无状态" : "当前状态";
        var wanted = statuses.Select(item => item.InstanceId).ToHashSet(StringComparer.Ordinal);
        foreach (var key in _statusViews.Keys.Where(key => !wanted.Contains(key)).ToArray())
        {
            var tile = _statusViews[key];
            if (_detailTile == tile)
            {
                _detailTile = null;
                _detail.Text = "该状态已结束";
            }
            if (tile.HasFocus())
            {
                if (_ability.Visible) _ability.GrabFocus();
                else if (_slots[0].Visible) _slots[0].GrabFocus();
            }
            tile.DetailRequested -= ShowDetail;
            tile.Visible = false;
            tile.QueueFree();
            _statusViews.Remove(key);
        }
        for (var index = 0; index < statuses.Length; index++)
        {
            var status = statuses[index];
            if (!_statusViews.TryGetValue(status.InstanceId, out var tile))
            {
                tile = FactTileScene.Instantiate<CombatFactTile>();
                _statusTiles.AddChild(tile);
                tile.DetailRequested += ShowDetail;
                _statusViews.Add(status.InstanceId, tile);
            }
            _statusTiles.MoveChild(tile, index);
            tile.Bind(SemanticIcons.Catalog.ResolveIcon(StatusDisplayFacts.IconKey(status))
                    ?? SemanticIcons.Catalog.ResolveIcon("risk"),
                status.DisplayName, $"×{status.Stacks} · {StatusDisplayFacts.Duration(status)}",
                $"{status.DisplayName} ×{status.Stacks}\n{status.Description}\n剩余：{StatusDisplayFacts.Duration(status)}");
        }
    }

    public override void _ExitTree()
    {
        _ability.Pressed -= ShowAbilityDetail;
        foreach (var slot in _slots) slot.DetailRequested -= ShowDetail;
        foreach (var tile in _statusViews.Values) tile.DetailRequested -= ShowDetail;
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
            BattleUnitMode.Casting when state.LastActionKind == BattleActionKind.Ability => $"施法：{state.LastAbilityName}{target}",
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
            BattleUnitMode.Casting when state.LastActionKind == BattleActionKind.Ability => $"施法：{state.LastAbilityName}{target}",
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

}
