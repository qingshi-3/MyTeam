using System;
using Godot;
using TowerAutobattler.Content;

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

    public override void _Ready()
    {
        Portrait = GetNode<UnitPortrait>("%UnitPortrait");
        _name = GetNode<Label>("%UnitName");
        _health = GetNode<SemanticChip>("%UnitHealthFact");
        _role = GetNode<SemanticChip>("%UnitRoleFact");
        _reach = GetNode<SemanticChip>("%UnitReachFact");
        _state = GetNode<Label>("%UnitState");
        Pressed += OnPressed;
    }
    public override void _ExitTree() => Pressed -= OnPressed;

    public void Bind(DeploymentUnitViewModel model, bool selected)
    {
        InstanceId = model.InstanceId;
        Text = string.Empty;
        Portrait.Bind(model.Portrait, Fallback(model.Role));
        _name.Text = $"{(selected ? "▶ " : string.Empty)}{model.DisplayName}";
        _health.Bind(UnitSemanticFacts.Health(model.HealthRatio.ToString("P0"), includeLabel: false));
        _role.Bind(UnitSemanticFacts.Responsibility(model.Role, includeLabel: false));
        _reach.Bind(UnitSemanticFacts.Reach(model.AttackRange, includeLabel: false));
        _state.Text = model.Slot >= 0 ? $"已部署 · 槽位 {model.Slot + 1}" : "候命";
        TooltipText = model.Description;
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (string.IsNullOrWhiteSpace(InstanceId)) return default;
        var preview = (Control)Duplicate();
        preview.CustomMinimumSize = new Vector2(250, 72);
        preview.MouseFilter = MouseFilterEnum.Ignore;
        SetDragPreview(preview);
        return new Godot.Collections.Dictionary { ["unit_id"] = InstanceId };
    }

    private void OnPressed() => UnitSelected?.Invoke(InstanceId);

    private static Texture2D? Fallback(UnitRole role) => SemanticIcons.Catalog.ResolveIcon(
        role is UnitRole.Ranged or UnitRole.Artillery ? SemanticIconKeys.Ranged : SemanticIconKeys.Melee);
}
