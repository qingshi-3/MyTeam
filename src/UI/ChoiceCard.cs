using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class ChoiceCard : Button
{
    [Signal] public delegate void ChosenEventHandler(string stableId);

    public string StableId { get; private set; } = string.Empty;
    public string SearchText { get; private set; } = string.Empty;
    public string TitleText => _title?.Text ?? string.Empty;
    public string BodyText => _body?.Text ?? string.Empty;
    public string FooterText => _footer?.Text ?? string.Empty;
    private ChosenEventHandler? _chosenHandler;
    private TextureRect _icon = null!;
    private Label _title = null!;
    private Label _body = null!;
    private Label _footer = null!;
    private SemanticChip _semanticFooter = null!;

    public override void _Ready()
    {
        CacheNodes();
        Pressed += OnPressed;
    }
    public override void _ExitTree()
    {
        Pressed -= OnPressed;
        if (_chosenHandler is not null) Chosen -= _chosenHandler;
        _chosenHandler = null;
    }

    public void Bind(
        string stableId,
        string title,
        string description,
        string footer = "",
        Texture2D? icon = null,
        StringName? titleVariation = null,
        StringName? footerVariation = null,
        StringName? footerSemanticKey = null)
    {
        CacheNodes();
        StableId = stableId;
        Text = string.Empty;
        SearchText = string.IsNullOrWhiteSpace(footer) ? $"{title}\n{description}" : $"{title}\n{description}\n{footer}";
        _icon.Texture = icon;
        _icon.Visible = icon is not null;
        _title.Text = title;
        _title.ThemeTypeVariation = titleVariation ?? "ChoiceTitle";
        _body.Text = description;
        _footer.Text = footer;
        var semanticFooter = footerSemanticKey is { } key && !string.IsNullOrWhiteSpace(key.ToString());
        _footer.Visible = !semanticFooter && !string.IsNullOrWhiteSpace(footer);
        _footer.ThemeTypeVariation = footerVariation ?? "ChoiceFooter";
        _semanticFooter.Visible = semanticFooter && !string.IsNullOrWhiteSpace(footer);
        if (_semanticFooter.Visible) _semanticFooter.Bind(footerSemanticKey!, footer, footerVariation);
        TooltipText = description;
    }

    public void ConnectChosen(Action<string> handler)
    {
        if (_chosenHandler is not null) Chosen -= _chosenHandler;
        _chosenHandler = handler.Invoke;
        Chosen += _chosenHandler;
    }

    private void OnPressed() => EmitSignal(SignalName.Chosen, StableId);

    private void CacheNodes()
    {
        _icon ??= GetNode<TextureRect>("%ChoiceIcon");
        _title ??= GetNode<Label>("%ChoiceTitle");
        _body ??= GetNode<Label>("%ChoiceBody");
        _footer ??= GetNode<Label>("%ChoiceFooter");
        _semanticFooter ??= GetNode<SemanticChip>("%ChoiceSemanticFooter");
    }
}
