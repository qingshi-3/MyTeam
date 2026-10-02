using System;
using System.Linq;
using Godot;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class TowerScreenController : Control
{
    public event Action<TowerNodeType>? NodeSelected;
    public event Action<TowerNodeType, string>? OpponentSelected;
    public event Action? AbandonRequested;

    private Label _title = null!;
    private Label _runInfo = null!;
    private Label _status = null!;
    private Container _choices = null!;
    private Button _abandon = null!;
    private Control _route = null!;
    private OpponentChoicePanel _opponents = null!;
    private RunApplication? _application;
    private SemanticIconCatalog? _icons;
    private TowerNodeType _opponentNodeType;
    private string _contextKey = string.Empty;
    private PackedScene _routeCardScene = null!;

    public override void _Ready()
    {
        _title = GetNode<Label>("%RouteTitle");
        _runInfo = GetNode<Label>("%RunInfo");
        _status = GetNode<Label>("Margin/Layout/Status");
        _choices = GetNode<Container>("Margin/Layout/Choices");
        _abandon = GetNode<Button>("Margin/Layout/AbandonButton");
        _route = GetNode<Control>("Margin");
        _opponents = GetNode<OpponentChoicePanel>("OpponentChoicePanel");
        _routeCardScene = GD.Load<PackedScene>("res://scenes/ui/components/TowerRouteCard.tscn");
        _abandon.Pressed += OnAbandon;
        _opponents.BackRequested += OnBackToRoute;
        _opponents.Confirmed += OnOpponentConfirmed;
    }

    public override void _ExitTree()
    {
        _abandon.Pressed -= OnAbandon;
        _opponents.BackRequested -= OnBackToRoute;
        _opponents.Confirmed -= OnOpponentConfirmed;
    }

    public void Bind(
        RunApplication app,
        PackedScene choiceTemplate,
        SemanticIconCatalog icons)
    {
        var run = app.Run ?? throw new InvalidOperationException("No active run for tower screen.");
        _application = app;
        _icons = icons;
        _status.Visible = false;
        var contextKey = $"{run.Seed}/{run.FloorIndex}";
        var preserveOpponentSelection = _opponents.Visible && _contextKey == contextKey;
        _contextKey = contextKey;
        var region = app.Tower.RegionFor(run.FloorIndex);
        _title.Text = region.DisplayName;
        _runInfo.Text = $"第 {run.FloorIndex + 1}/{app.Project.Campaign.TotalFloors} 层";
        var models = app.CurrentOptions().Select(option =>
        {
            return new ChoiceCardViewModel(
                option.Type.ToString(),
                option.Title,
                option.Description,
                option.Risk > 0 ? $"风险 {option.Risk}" : string.Empty,
                Icon: icons.ResolveIcon(SemanticIconKeys.TowerNodeSemantic(option.Type)),
                TitleVariation: "RouteCardTitle",
                FooterVariation: "WarningLabel",
                FooterSemanticKey: option.Risk > 0 ? SemanticIconKeys.Risk : default,
                Template: _routeCardScene);
        }).ToArray();
        ChoiceCardListBinder.SyncChoices(_choices, models, choiceTemplate, OnChoice);
        foreach (var card in _choices.GetChildren().OfType<TowerRouteCard>().Where(card => !card.IsQueuedForDeletion()))
            card.BindRoute(Enum.Parse<TowerNodeType>(card.StableId), app);
        GetNode<ProgressBar>("%RouteProgress").MaxValue = app.Project.Campaign.TotalFloors;
        GetNode<ProgressBar>("%RouteProgress").Value = run.FloorIndex + 1;
        if (preserveOpponentSelection)
        {
            var opponents = app.CurrentOpponentOptions(_opponentNodeType);
            if (opponents.Count > 0) _opponents.Bind(app, opponents, icons, true);
            else OnBackToRoute();
        }
        else OnBackToRoute();
    }

    private void OnChoice(string stableId)
    {
        if (!Enum.TryParse<TowerNodeType>(stableId, out var type)) return;
        if (_application is { } app && _icons is { } icons &&
            type is TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss)
        {
            var opponents = app.CurrentOpponentOptions(type);
            if (opponents.Count > 0)
            {
                _opponentNodeType = type;
                _opponents.Bind(app, opponents, icons);
                _route.Visible = false;
                _opponents.Visible = true;
                _opponents.FocusSelection();
                return;
            }
        }
        NodeSelected?.Invoke(type);
    }

    private void OnBackToRoute()
    {
        _opponents.Visible = false;
        _route.Visible = true;
        if (IsVisibleInTree())
            _choices.GetChildren().OfType<ChoiceCard>()
                .FirstOrDefault(card => card.StableId == _opponentNodeType.ToString() && !card.IsQueuedForDeletion())?.GrabFocus();
    }

    public void ShowMessage(string message)
    {
        if (_opponents.Visible) _opponents.ShowMessage(message);
        else
        {
            _status.Text = message;
            _status.Visible = !string.IsNullOrWhiteSpace(message);
        }
    }

    private void OnOpponentConfirmed(string optionId) => OpponentSelected?.Invoke(_opponentNodeType, optionId);

    private void OnAbandon() => AbandonRequested?.Invoke();
}
