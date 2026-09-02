using System;
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
        ThemeTypeVariation = selected ? "SelectedButton" : "SecondaryButton";
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

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (string.IsNullOrWhiteSpace(InstanceId)) return default;
        var preview = (Control)Duplicate();
        preview.CustomMinimumSize = new Vector2(250, 72);
        preview.MouseFilter = MouseFilterEnum.Ignore;
        SetDragPreview(preview);
        return new Godot.Collections.Dictionary { ["piece_id"] = InstanceId };
    }

    private void OnPressed() => UnitSelected?.Invoke(InstanceId);

    private static Texture2D? Fallback(UnitRole role) => SemanticIcons.Catalog.ResolveIcon(
        role is UnitRole.Ranged or UnitRole.Artillery ? SemanticIconKeys.Ranged : SemanticIconKeys.Melee);
}
