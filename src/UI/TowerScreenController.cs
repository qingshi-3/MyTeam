using System;
using System.Linq;
using Godot;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class TowerScreenController : Control
{
    public event Action<TowerNodeType>? NodeSelected;
    public event Action? AbandonRequested;

    private Label _title = null!;
    private Label _runInfo = null!;
    private Container _choices = null!;
    private Button _abandon = null!;

    public override void _Ready()
    {
        _title = GetNode<Label>("Margin/Layout/Title");
        _runInfo = GetNode<Label>("Margin/Layout/RunInfo");
        _choices = GetNode<Container>("Margin/Layout/Choices");
        _abandon = GetNode<Button>("Margin/Layout/AbandonButton");
        _abandon.Pressed += OnAbandon;
    }

    public override void _ExitTree() => _abandon.Pressed -= OnAbandon;

    public void Bind(
        RunApplication app,
        PackedScene choiceTemplate,
        SemanticIconCatalog icons)
    {
        var run = app.ActiveRun ?? throw new InvalidOperationException("No active run for tower screen.");
        var region = app.Tower.RegionFor(run.FloorIndex);
        _title.Text = region.DisplayName;
        _runInfo.Text = $"第 {run.FloorIndex + 1}/{app.Project.Campaign.TotalFloors} 层";
        var models = app.CurrentOptions().Select(option =>
        {
            var riskRule = RunHealthText.Risk(app.Rules, option.Type);
            return new ChoiceCardViewModel(
                option.Type.ToString(),
                option.Title,
                string.IsNullOrWhiteSpace(riskRule) ? option.Description : option.Description + "\n" + riskRule,
                option.Risk > 0 ? $"风险 {option.Risk}" : string.Empty,
                Icon: icons.ResolveIcon(SemanticIconKeys.TowerNodeSemantic(option.Type)),
                FooterVariation: "WarningLabel",
                FooterSemanticKey: option.Risk > 0 ? SemanticIconKeys.Risk : default);
        }).ToArray();
        ChoiceCardListBinder.SyncChoices(_choices, models, choiceTemplate, OnChoice);
    }

    private void OnChoice(string stableId)
    {
        if (Enum.TryParse<TowerNodeType>(stableId, out var type)) NodeSelected?.Invoke(type);
    }

    private void OnAbandon() => AbandonRequested?.Invoke();
}
