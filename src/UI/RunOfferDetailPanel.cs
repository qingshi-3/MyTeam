using Godot;

namespace TowerAutobattler.UI;

public partial class RunOfferDetailPanel : PanelContainer
{
    private string? _text;

    public void Bind(string text)
    {
        if (_text == text) return;
        _text = text;
        var split = text.IndexOf('\n');
        GetNode<Label>("Layout/Title").Text = split < 0 ? text : text[..split];
        GetNode<CombatRichText>("Layout/Scroll/Copy").Text = split < 0 ? string.Empty : text[(split + 1)..];
        GetNode<ScrollContainer>("Layout/Scroll").ScrollVertical = 0;
    }
}
