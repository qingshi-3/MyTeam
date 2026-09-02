using System;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.TacticalCommands;

namespace TowerAutobattler.UI;

public partial class TacticalCommandSlot : Button
{
    public event Action<int>? UseRequested;

    private Label _name = null!;
    private Label _effect = null!;
    private ResourceCostBadge _tacticalPointCost = null!;
    private ResourceCostBadge _goldCost = null!;
    private Label _state = null!;

    public int SlotIndex { get; private set; } = -1;
    public string CommandId { get; private set; } = string.Empty;

    public override void _Ready()
    {
        CacheNodes();
        Pressed += OnPressed;
    }

    public override void _ExitTree() => Pressed -= OnPressed;

    public void Bind(TacticalCommandSlotSnapshot slot, bool battleRunning)
    {
        CacheNodes();
        SlotIndex = slot.SlotIndex;
        CommandId = slot.StableId;
        _name.Text = $"{slot.SlotIndex + 1}. {slot.DisplayName}";
        _effect.Text = slot.Description;
        _tacticalPointCost.BindTacticalPoint(slot.TacticalPointCost);
        _goldCost.BindGold(slot.GoldCost);
        _state.Text = DescribeState(slot, battleRunning);
        _state.ThemeTypeVariation = slot.CanAttempt && battleRunning ? "HealingValue" : "SecondaryLabel";
        Disabled = !battleRunning;
        TooltipText = string.Join('\n', new[] { slot.Description, _state.Text });
    }

    public void BindUnavailable(int slotIndex)
    {
        CacheNodes();
        SlotIndex = slotIndex;
        CommandId = string.Empty;
        _name.Text = $"{slotIndex + 1}. 未配置";
        _effect.Text = "该战术指令槽位尚未就绪。";
        _tacticalPointCost.BindTacticalPoint(0);
        _goldCost.BindGold(0);
        _state.Text = "不可用";
        _state.ThemeTypeVariation = "SecondaryLabel";
        Disabled = true;
        TooltipText = _effect.Text;
    }

    private static string DescribeState(TacticalCommandSlotSnapshot slot, bool battleRunning)
    {
        if (!battleRunning) return "战斗已结束";
        if (slot.CooldownRemainingTicks > 0)
            return $"冷却 {slot.CooldownRemainingTicks * BattleTiming.TickSeconds:0.0} 秒";
        if (slot.MaxUses > 0 && slot.Uses >= slot.MaxUses)
            return $"次数 {slot.Uses}/{slot.MaxUses} · 已耗尽";
        if (slot.MaxUses > 0) return $"次数 {slot.Uses}/{slot.MaxUses} · 可尝试";
        return slot.CanAttempt ? "可尝试" : "资源不足 · 可查看原因";
    }

    private void OnPressed()
    {
        if (SlotIndex >= 0 && !string.IsNullOrWhiteSpace(CommandId)) UseRequested?.Invoke(SlotIndex);
    }

    private void CacheNodes()
    {
        _name ??= GetNode<Label>("%CommandName");
        _effect ??= GetNode<Label>("%CommandEffect");
        _tacticalPointCost ??= GetNode<ResourceCostBadge>("%TacticalPointCostBadge");
        _goldCost ??= GetNode<ResourceCostBadge>("%GoldCostBadge");
        _state ??= GetNode<Label>("%CommandState");
    }
}
