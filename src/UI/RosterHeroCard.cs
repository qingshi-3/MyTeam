using System;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

// The card owns presentation and input only; the view supplies validated run commands.
public partial class RosterHeroCard : Button
{
    public string HeroId { get; private set; } = "";
    public event Action<string>? Selected;
    public event Action<string, int, string>? EquipmentDropped;
    public event Action<EquipmentSlotButton, string>? EquipmentInspected;
    public event Action<EquipmentSlotButton, bool, string>? KeyboardEquipment;
    public Func<Variant, EquipmentDropEvaluation>? EvaluateDrop { get; set; }
    public Func<string, bool>? CanReceiveItem { get; set; }
    private string _normalStyle = "SecondaryButton";
    private Control _margin = null!;
    private UnitInfoCard _information = null!;
    public override void _Ready()
    {
        _margin = GetNode<Control>("Margin");
        _margin.MinimumSizeChanged += RefreshMinimumSize;
        Pressed += SelectCard;
        MouseExited += ClearHighlight;
        _information = GetNode<UnitInfoCard>("%Information");
        _information.Activated += SelectCard;
        _information.ForwardDrag(Callable.From<Vector2, Variant, bool>((p, data) => _CanDropData(p, data)),
            Callable.From<Vector2, Variant>((p, data) => _DropData(p, data)));
        for (var index = 0; index < 3; index++)
        {
            var slot = GetNode<EquipmentSlotButton>("%Slot" + index);
            slot.EquipmentDropped += (id, at) => EquipmentDropped?.Invoke(HeroId, at, id);
            slot.Chosen += tile => EquipmentInspected?.Invoke(tile, HeroId);
            slot.KeyboardActionRequested += (tile, remove) => KeyboardEquipment?.Invoke(tile, remove, HeroId);
        }
    }
    public void Bind(RunApplication app, RosterHeroInstanceDto hero, UnitDefinition definition,
        UnitSnapshot baseline, PreparedUnitDetails? prepared, bool selected, string selectedEquipmentId)
    {
        HeroId = hero.InstanceId;
        _normalStyle = selected ? "SelectedButton" : "SecondaryButton";
        ThemeTypeVariation = _normalStyle;
        var model = new UnitInformation(hero.InstanceId, definition, baseline, prepared, hero.HealthRatio);
        var deployed = app.ActiveRun!.Deployment.Contains(hero.InstanceId);
        GetNode<UnitInfoCard>("%Information").Bind(model, $"{(deployed ? "出战" : "后备")} · {hero.Rank} 阶");
        for (var index = 0; index < 3; index++)
        {
            var slot = GetNode<EquipmentSlotButton>("%Slot" + index);
            var item = hero.Equipment.SingleOrDefault(equipment => equipment.SlotIndex == index);
            var itemDefinition = item is not null && app.Content.TryGet(item.ContentId, out var entry) ? (ItemDefinition)entry.Definition : null;
            slot.Bind(item?.InstanceId ?? "", index, itemDefinition, item?.InstanceId == selectedEquipmentId, !app.EquipmentEditingLocked);
            slot.DragContext = RunEquipmentDropRules.ContextFor(app.ActiveRun);
            slot.CanReceive = id => CanReceiveItem?.Invoke(id) == true;
            if (itemDefinition is not null)
                BattleLabHoverHint.Bind(slot, RosterLoadoutView.EquipmentHint(app, itemDefinition, "已装备",
                    "拖到另一英雄或装备槽转交；拖回背包卸下。Delete 卸下。"));
            else BattleLabHoverHint.Bind(slot, null);
        }
        var contributions = (baseline.TraitContributions.IsDefault ? [] : baseline.TraitContributions)
            .Concat(hero.Equipment.SelectMany(item => app.Content.Graph.ResolveEquipment(item.ContentId).TraitContributions))
            .GroupBy(trait => trait.TraitId).Select(group =>
                $"{(app.Content.Graph.TryGetTrait(group.Key, out var trait) ? trait.DisplayName : group.Key)} +{group.Sum(value => value.Value)}").ToArray();
        BattleLabHoverHint.Bind(this, new BattleLabTooltipInfo(definition.DisplayName,
            $"{(deployed ? "出战" : "后备")} · {hero.Rank} 阶 · " + (prepared is null ? "基础属性（未计装备与开战加成）" : "准备属性（已计开战加成）"),
            Stats: model.AllAttributesText(),
            Loadout: contributions.Length == 0 ? "羁绊：无" : "羁绊贡献（含装备）\n" + string.Join("、", contributions),
            Hint: "整卡可选择；拖入装备装入首个空槽。装备满时拖到具体槽位替换。技能与装备悬停查看各自完整效果。"));
        RefreshMinimumSize();
    }
    private void RefreshMinimumSize()
    {
        // Button does not derive its native minimum from child containers. Publish the
        // composed content's minimum explicitly, including the authored 12px insets.
        var content = _margin.GetCombinedMinimumSize();
        CustomMinimumSize = new Vector2(Math.Max(274, content.X + 24), Math.Max(466, content.Y + 24));
    }
    public override void _ExitTree()
    {
        if (IsInstanceValid(_margin)) _margin.MinimumSizeChanged -= RefreshMinimumSize;
        if (IsInstanceValid(_information)) _information.Activated -= SelectCard;
        Pressed -= SelectCard;
        MouseExited -= ClearHighlight;
    }
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (!EquipmentSlotButton.TryEquipmentId(data, out _)) return false;
        var evaluation = EvaluateDrop?.Invoke(data) ?? EquipmentDropEvaluation.Reject("当前不可更换。");
        ThemeTypeVariation = evaluation.Allowed ? "EquipmentSlotValid" : "EquipmentSlotInvalid";
        return evaluation.Allowed;
    }
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var evaluation = EvaluateDrop?.Invoke(data) ?? EquipmentDropEvaluation.Reject("当前不可更换。");
        if (evaluation.Allowed) EquipmentDropped?.Invoke(HeroId, evaluation.SlotIndex, evaluation.ItemId);
        ClearHighlight();
    }
    public override void _Notification(int what) { if (what == NotificationDragEnd) ClearHighlight(); }
    private void ClearHighlight() => ThemeTypeVariation = _normalStyle;
    private void SelectCard() => Selected?.Invoke(HeroId);
}
