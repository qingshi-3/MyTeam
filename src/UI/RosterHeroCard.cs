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
    public string TraitContributionSummary { get; private set; } = "";
    public event Action<string>? Selected;
    public event Action<string, int, string>? EquipmentDropped;
    public event Action<EquipmentSlotButton, string>? EquipmentInspected;
    public event Action<EquipmentSlotButton, bool, string>? KeyboardEquipment;
    public Func<Variant, EquipmentDropEvaluation>? EvaluateDrop { get; set; }
    public Func<string, bool>? CanReceiveItem { get; set; }
    public UnitCardOrbit Orbit => GetNode<UnitCardOrbit>("Orbit");
    public EquipmentSlotButton Slot(int index) => Orbit.Slot(index);
    private string _normalStyle = "OrbitCard";
    private Control _margin = null!;
    private WorkbenchHeroSummary _information = null!;
    public override void _Ready()
    {
        _margin = GetNode<Control>("Margin");
        _margin.MinimumSizeChanged += RefreshMinimumSize;
        Pressed += SelectCard;
        MouseExited += ClearHighlight;
        _information = GetNode<WorkbenchHeroSummary>("%Information");
        Orbit.Configure(this, GetNode<Control>("CardFrame"), null);
        _information.ForwardDrag(Callable.From<Vector2, Variant, bool>((p, data) => _CanDropData(p, data)),
            Callable.From<Vector2, Variant>((p, data) => _DropData(p, data)));
        for (var index = 0; index < 3; index++)
        {
            var slot = Slot(index);
            slot.EquipmentDropped += (id, at) => EquipmentDropped?.Invoke(HeroId, at, id);
            slot.Chosen += tile => EquipmentInspected?.Invoke(tile, HeroId);
            slot.KeyboardActionRequested += (tile, remove) => KeyboardEquipment?.Invoke(tile, remove, HeroId);
        }
    }
    public void Bind(RunApplication app, RosterHeroView hero, UnitDefinition definition,
        UnitInformation model, string selectedEquipmentId)
    {
        HeroId = hero.InstanceId;
        _normalStyle = "OrbitCard";
        ThemeTypeVariation = _normalStyle;
        var deployed = app.Run!.Deployment.Contains(hero.InstanceId);
        _information.Bind(model);
        GetNode<Label>("%HeroRank").Text = FormatRank(hero.Rank);
        GetNode<TextureRect>("%DeployedIcon").Visible = deployed;
        GetNode<TextureRect>("%ReserveIcon").Visible = !deployed;
        AccessibilityName = $"{definition.DisplayName} · {hero.Rank} 阶 · {(deployed ? "出战" : "后备")}";
        for (var index = 0; index < 3; index++)
        {
            var slot = Slot(index);
            var item = hero.Equipment.SingleOrDefault(equipment => equipment.SlotIndex == index);
            var itemDefinition = item is not null && app.Content.TryGet(item.ContentId, out var entry) ? (ItemDefinition)entry.Definition : null;
            slot.Bind(item?.InstanceId ?? "", index, itemDefinition, item?.InstanceId == selectedEquipmentId, !app.EquipmentEditingLocked);
            slot.DragContext = RunEquipmentDropRules.ContextFor(app.Run);
            slot.CanReceive = id => CanReceiveItem?.Invoke(id) == true;
            if (itemDefinition is not null)
                BattleLabHoverHint.Bind(slot, RosterLoadoutView.EquipmentHint(app, itemDefinition, "已装备",
                    "拖到另一英雄或装备槽转交；拖回背包卸下。Delete 卸下。") with { SingleEntry = true });
            else BattleLabHoverHint.Bind(slot, new("装备位 " + (index + 1), Stats: "空槽。拖入装备即可穿戴，替换下来的装备会回到背包。", SingleEntry: true));
        }
        var contributions = (model.Baseline.TraitContributions.IsDefault ? [] : model.Baseline.TraitContributions)
            .Concat(hero.Equipment.SelectMany(item => app.Content.Graph.ResolveEquipment(item.ContentId).TraitContributions))
            .GroupBy(trait => trait.TraitId).Select(group =>
                $"{(app.Content.Graph.TryGetTrait(group.Key, out var trait) ? trait.DisplayName : group.Key)} +{group.Sum(value => value.Value)}").ToArray();
        TraitContributionSummary = contributions.Length == 0 ? "" : "羁绊贡献（含装备）\n" + string.Join(" · ", contributions);
        // Facts and equipment remain visible; only their individual explanations
        // use hover. The card itself does not need another tooltip.
        BattleLabHoverHint.Bind(this, null);
        Orbit.Bind(model, traits: TraitContributionSummary);
        RefreshMinimumSize();
    }
    internal static string FormatRank(int rank) => rank switch
    {
        1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V",
        _ => rank.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
    private void RefreshMinimumSize()
    {
        // Button does not derive its native minimum from child containers. Publish the
        // composed content's minimum explicitly, including the authored frame insets.
        var content = _margin.GetCombinedMinimumSize();
        CustomMinimumSize = new Vector2(
            Math.Max(620, content.X + _margin.OffsetLeft - _margin.OffsetRight),
            Math.Max(660, content.Y + _margin.OffsetTop - _margin.OffsetBottom));
    }
    public override void _ExitTree()
    {
        if (IsInstanceValid(_margin)) _margin.MinimumSizeChanged -= RefreshMinimumSize;
        Pressed -= SelectCard;
        MouseExited -= ClearHighlight;
    }
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (!EquipmentSlotButton.TryEquipmentId(data, out _)) return false;
        var evaluation = EvaluateDrop?.Invoke(data) ?? EquipmentDropEvaluation.Reject("当前不可更换。");
        GetNode<Panel>("CardFrame").Modulate = evaluation.Allowed ? new Color(0.88f, 1f, 0.86f) : new Color(1f, 0.7f, 0.65f);
        var hint = GetNode<Label>("DropHint");
        hint.Text = evaluation.Allowed ? "放入装备" : "不可装备";
        hint.Visible = true;
        UiDragVisual.Aim(data, this, evaluation.Allowed);
        return evaluation.Allowed;
    }
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var evaluation = EvaluateDrop?.Invoke(data) ?? EquipmentDropEvaluation.Reject("当前不可更换。");
        if (evaluation.Allowed) EquipmentDropped?.Invoke(HeroId, evaluation.SlotIndex, evaluation.ItemId);
        ClearHighlight();
    }
    public override void _Notification(int what) { if (what == NotificationDragEnd) ClearHighlight(); }
    private void ClearHighlight()
    {
        GetNode<Panel>("CardFrame").Modulate = Colors.White;
        GetNode<Label>("DropHint").Hide();
    }
    private void SelectCard() => Selected?.Invoke(HeroId);
}
