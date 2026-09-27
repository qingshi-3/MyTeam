using System;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

// The outer scroll handles additional rows; each card keeps its own action outside its text scroll.
public partial class RunOfferChoiceGrid : GridContainer
{
    [Export] public ScrollContainer ScrollOwner { get; set; } = null!;
    private ScrollContainer _viewport = null!;

    public override void _Ready()
    {
        _viewport = ScrollOwner;
        _viewport.Resized += RefreshLayout;
        ChildOrderChanged += RefreshLayout;
        RefreshLayout();
    }

    public override void _ExitTree()
    {
        _viewport.Resized -= RefreshLayout;
        ChildOrderChanged -= RefreshLayout;
    }

    public void RefreshLayout()
    {
        if (_viewport is null) return;
        var cards = GetChildren().OfType<RunOfferChoiceCard>().Where(card => !card.IsQueuedForDeletion()).ToArray();
        var width = _viewport.Size.X;
        // Short item offers keep the scale of a card instead of stretching into full-height panels.
        // Mixed offers retain equal-sized cards so comparison does not shift when one is a unit.
        var compact = cards.Length > 0 && cards.All(card => !card.IsUnit);
        SizeFlagsHorizontal = compact ? SizeFlags.ShrinkCenter : SizeFlags.ExpandFill;
        SizeFlagsVertical = compact ? SizeFlags.ShrinkCenter : SizeFlags.ExpandFill;
        var compactWidth = Math.Min(width, Math.Min(1120, cards.Length * 376 + Math.Max(0, cards.Length - 1) * 16));
        CustomMinimumSize = new Vector2(compact ? compactWidth : 0, 0);
        if (compact) width = compactWidth;
        Columns = Math.Clamp((int)((width + 16) / 316), 1, Math.Max(1, Math.Min(3, cards.Length)));
        var height = Math.Max(360, _viewport.Size.Y - 4);
        if (compact) height = Math.Min(height, 540);
        foreach (var card in cards) card.SetAvailableHeight(height);
    }
}
