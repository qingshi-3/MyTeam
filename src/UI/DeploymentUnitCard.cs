using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class DeploymentUnitCard : Button
{
    public event Action<string>? UnitSelected;
    public string InstanceId { get; private set; } = string.Empty;
    public UnitPortrait Portrait { get; private set; } = null!;

    private Label _name = null!;
    private SemanticChip _health = null!;
    private SemanticChip _role = null!;
    private SemanticChip _reach = null!;
    private Label _state = null!;
    private HBoxContainer _equipment = null!;
    public Func<Variant, EquipmentDropEvaluation>? EquipmentDropEvaluator { get; set; }
    public Action<Variant>? EquipmentReceiver { get; set; }
    public Func<Variant, bool>? RosterDropEvaluator { get; set; }
    public Action<Variant>? RosterReceiver { get; set; }
    private string _normalStyle = "SecondaryButton";

    public override Vector2 _GetMinimumSize() =>
        GetNodeOrNull<Control>("Margin")?.GetCombinedMinimumSize() ?? Vector2.Zero;

    public override void _Ready()
    {
        GetNode<Control>("Margin").MinimumSizeChanged += UpdateMinimumSize;
        Portrait = GetNode<UnitPortrait>("%UnitPortrait");
        _name = GetNode<Label>("%UnitName");
        _health = GetNode<SemanticChip>("%UnitHealthFact");
        _role = GetNode<SemanticChip>("%UnitRoleFact");
        _reach = GetNode<SemanticChip>("%UnitReachFact");
        _state = GetNode<Label>("%UnitState");
        _equipment = GetNode<HBoxContainer>("%EquipmentIcons");
        Pressed += OnPressed;
        MouseExited += ClearEquipmentHover;
    }
    public override void _ExitTree()
    {
        Pressed -= OnPressed;
        MouseExited -= ClearEquipmentHover;
    }

    public void Bind(DeploymentUnitViewModel model, bool selected)
    {
        InstanceId = model.InstanceId;
        Text = string.Empty;
        _normalStyle = selected ? "SelectedButton" : "SecondaryButton";
        ThemeTypeVariation = _normalStyle;
        Portrait.Bind(model.Portrait, Fallback(model.Role));
        _name.Text = $"{(model.IsHero ? "★ " : string.Empty)}{model.DisplayName}";
        _name.ThemeTypeVariation = model.IsHero ? "HeroIdentity" : "ChoiceTitle";
        _health.Bind(UnitSemanticFacts.Health(model.HealthRatio.ToString("P0"), includeLabel: false));
        _role.Bind(UnitSemanticFacts.Responsibility(model.Role, includeLabel: false));
        _reach.Bind(UnitSemanticFacts.Reach(model.AttackRange, includeLabel: false));
        _state.Text = model.Cell is { } cell
            ? model.IsHero
                ? $"英雄 · 第 {cell.X + 1} 列 / 第 {cell.Y + 1} 行"
                : $"已部署 · 第 {cell.X + 1} 列 / 第 {cell.Y + 1} 行"
            : "候命";
        TooltipText = model.Description;
    }

    public void BindEquipment(IReadOnlyList<(int SlotIndex, ItemDefinition Definition)> items)
    {
        _equipment.Visible = true;
        for (var index = 0; index < _equipment.GetChildCount(); index++)
        {
            var icon = _equipment.GetChild<TextureRect>(index);
            var definition = items.FirstOrDefault(item => item.SlotIndex == index).Definition;
            icon.Texture = definition?.Icon ?? SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Loot);
            icon.Modulate = definition is null ? new Color(1, 1, 1, .22f) : Colors.White;
            icon.TooltipText = definition is null ? $"装备槽 {index + 1} · 空" : $"{definition.DisplayName}\n{definition.Description}";
        }
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (string.IsNullOrWhiteSpace(InstanceId)) return default;
        var preview = (Control)Duplicate();
        preview.CustomMinimumSize = CustomMinimumSize;
        preview.MouseFilter = MouseFilterEnum.Ignore;
        SetDragPreview(preview);
        return new Godot.Collections.Dictionary { ["piece_id"] = InstanceId };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (IsRosterDrag(data)) return RosterDropEvaluator?.Invoke(data) == true;
        if (!EquipmentSlotButton.TryEquipmentId(data, out _)) return false;
        var evaluation = EquipmentDropEvaluator?.Invoke(data)
            ?? EquipmentDropEvaluation.Reject("当前不能更换装备。");
        ThemeTypeVariation = evaluation.Allowed ? "EquipmentSlotValid" : "EquipmentSlotInvalid";
        return evaluation.Allowed;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (_CanDropData(atPosition, data))
        {
            if (IsRosterDrag(data)) RosterReceiver?.Invoke(data);
            else EquipmentReceiver?.Invoke(data);
        }
        ClearEquipmentHover();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd) ClearEquipmentHover();
    }

    private void ClearEquipmentHover() => ThemeTypeVariation = _normalStyle;

    private void OnPressed() => UnitSelected?.Invoke(InstanceId);

    private static bool IsRosterDrag(Variant data) =>
        data.VariantType == Variant.Type.Dictionary && data.AsGodotDictionary().ContainsKey("piece_id");

    private static Texture2D? Fallback(UnitRole role) => SemanticIcons.Catalog.ResolveIcon(
        role is UnitRole.Ranged or UnitRole.Artillery ? SemanticIconKeys.Ranged : SemanticIconKeys.Melee);
}
