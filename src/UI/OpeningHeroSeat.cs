using System;
using Godot;

namespace TowerAutobattler.UI;

// Bound only from the saved opening draft. Clicking requests the same toggle
// as the source card; failed commands leave both the seat and card unchanged.
public partial class OpeningHeroSeat : Button, IUiMotionHost
{
    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    public string StableId { get; private set; } = string.Empty;
    private UnitPortrait _portrait = null!;
    private UiPopupMotion _motion = null!;

    public override void _Ready()
    {
        _portrait = GetNode<UnitPortrait>("Portrait");
        _motion = new UiPopupMotion(_portrait, null, () => ReduceUiMotion());
        VisibilityChanged += Visibility;
    }

    public void Bind(HeroSelectionViewModel? model)
    {
        var changed = StableId != (model?.StableId ?? string.Empty);
        StableId = model?.StableId ?? string.Empty;
        Disabled = model is null;
        GetNode<Control>("EmptyRing").Visible = model is null;
        GetNode<Control>("Ordinal").Visible = model is null;
        _portrait.Visible = model is not null;
        GetNode<Label>("Name").Text = model?.Definition.DisplayName ?? string.Empty;
        AccessibilityName = model is null ? "空英雄席位" : model.Definition.DisplayName + "，取消选择";
        if (model is not null) _portrait.Bind(model.Definition.Portrait, model.Definition.Icon);
        if (changed && model is not null && IsVisibleInTree()) _motion.Open(new Vector2(0, -18));
        else if (model is null) _motion.Reset();
    }

    private void Visibility() { if (!IsVisibleInTree()) _motion.Reset(); }
    public override void _ExitTree() { VisibilityChanged -= Visibility; _motion.Dispose(); }
}
