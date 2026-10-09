using System;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

// The outer scroll handles additional rows; each card keeps its own action outside its text scroll.
public partial class RunOfferChoiceGrid : GridContainer
{
    public bool RecruitmentPresentation { get; set; }
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
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        // Oversized rows must begin at the scroll origin. Centering them clips
        // their names above the scrollable area on short reward pages.
        SizeFlagsVertical = SizeFlags.ShrinkBegin;
        if (!compact && cards.Length > 0)
        {
            // Reserve the same comparison slot for one, two or three candidates.
            // Basing width on the actual count would turn a lone hero into a banner.
            var separation = GetThemeConstant("h_separation");
            // The orbit has two 108px reading columns plus frame/inner gutters.
            // Keep their hit regions apart; narrow windows use fewer columns.
            var minimumWidth = RecruitmentPresentation ? 324 : 350;
            var capacity = Math.Clamp((int)((width + separation) / (minimumWidth + separation)), 1, 3);
            Columns = Math.Min(capacity, cards.Length);
            // Recruitment reserves its real roster strip in the outer container.
            // Size the illustrated 2:3 frame from the remaining viewport, instead
            // of keeping the old reward page's 533px minimum under that strip.
            var availableHeight = Math.Max(RecruitmentPresentation ? 494 : 533, _viewport.Size.Y - 4);
            var heightLimitedWidth = (availableHeight - 116) * 2 / 3 + 72;
            var cardWidth = Math.Clamp(Math.Min((width - (capacity - 1) * separation) / capacity,
                heightLimitedWidth), minimumWidth, 400);
            // The frame has 36 px side gutters, a 28 px top and an 88 px action footer.
            var cardHeight = (cardWidth - 72) * 1.5f + 116;
            CustomMinimumSize = new Vector2(Columns * cardWidth + (Columns - 1) * separation, 0);
            foreach (var card in cards) card.SetAvailableSize(new Vector2(cardWidth, cardHeight));
            return;
        }
        var compactWidth = Math.Min(width, Math.Min(1120, cards.Length * 376 + Math.Max(0, cards.Length - 1) * 16));
        CustomMinimumSize = new Vector2(compact ? compactWidth : 0, 0);
        if (compact) width = compactWidth;
        Columns = Math.Clamp((int)((width + 16) / (compact ? 316 : 506)), 1, Math.Max(1, Math.Min(3, cards.Length)));
        var height = Math.Max(compact ? 240 : 360, _viewport.Size.Y - 4);
        if (compact) height = Math.Min(height, 440);
        foreach (var card in cards) card.SetAvailableHeight(height);
    }
}
