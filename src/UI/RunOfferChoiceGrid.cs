using System;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

// The outer scroll handles additional rows; each card keeps its own action outside its text scroll.
public partial class RunOfferChoiceGrid : GridContainer
{
    private ScrollContainer _viewport = null!;

    public override void _Ready()
    {
        _viewport = GetParent<ScrollContainer>();
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
        Columns = Math.Clamp((int)((width + 16) / 316), 1, Math.Max(1, Math.Min(3, cards.Length)));
        var height = Math.Max(360, _viewport.Size.Y - 4);
        foreach (var card in cards) card.SetAvailableHeight(height);
    }
}
