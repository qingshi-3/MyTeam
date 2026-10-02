using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

// Browsing is read-only. The containing screen submits the stable option only
// after the player confirms; this component never changes a run or consumes spells.
public partial class OpponentChoicePanel : MarginContainer
{
    public event Action<string>? Confirmed;
    public event Action? BackRequested;

    private RunApplication? _application;
    private SemanticIconCatalog? _icons;
    private IReadOnlyList<OpponentOption> _options = [];
    private readonly Dictionary<string, OpponentPreview> _previews = new(StringComparer.Ordinal);
    private PackedScene _cardScene = null!;
    private Container _cards = null!;
    private Label _title = null!, _pressure = null!, _reward = null!, _risk = null!, _positions = null!;
    private Label _status = null!, _floorRule = null!;
    private OptionButton _unitPicker = null!;
    private UnitDetailView _unitDetails = null!;
    private Button _back = null!, _confirm = null!;
    private string _selectedId = string.Empty;
    private readonly List<string> _unitIds = [];

    public string SelectedOptionId => _selectedId;

    public override void _Ready()
    {
        _cardScene = GD.Load<PackedScene>("res://scenes/ui/components/OpponentChoiceCard.tscn");
        _cards = GetNode<Container>("%OpponentCards");
        _title = GetNode<Label>("%OpponentTitle");
        _pressure = GetNode<Label>("%OpponentPressure");
        _reward = GetNode<Label>("%OpponentReward");
        _risk = GetNode<Label>("%OpponentRisk");
        _positions = GetNode<Label>("%EnemyPositions");
        _status = GetNode<Label>("%OpponentStatus");
        _floorRule = GetNode<Label>("%OpponentFloorRule");
        _unitPicker = GetNode<OptionButton>("%EnemyPicker");
        _unitDetails = GetNode<UnitDetailView>("%EnemyDetails");
        _back = GetNode<Button>("%BackToRoute");
        _confirm = GetNode<Button>("%ConfirmOpponent");
        _unitPicker.ItemSelected += OnUnitSelected;
        _back.Pressed += OnBack;
        _confirm.Pressed += OnConfirm;
    }

    public override void _ExitTree()
    {
        _unitPicker.ItemSelected -= OnUnitSelected;
        _back.Pressed -= OnBack;
        _confirm.Pressed -= OnConfirm;
    }

    public void Bind(RunApplication app, IReadOnlyList<OpponentOption> options, SemanticIconCatalog icons,
        bool preserveSelection = false)
    {
        if (options.Count == 0) throw new ArgumentException("Opponent selection requires at least one offer.", nameof(options));
        _application = app;
        _icons = icons;
        _options = options;
        _previews.Clear();
        ShowMessage(string.Empty);
        if (!preserveSelection || !options.Any(option => option.OptionId == _selectedId))
            _selectedId = options[0].OptionId;
        var models = options.Select(option => new ChoiceCardViewModel(
            option.OptionId, option.Title,
            option.PressureSummary + "\n" + Composition(app, option.Encounter),
            RewardText(app, option),
            Icon: icons.ResolveIcon(SemanticIconKeys.TowerNodeSemantic(option.NodeType)))).ToArray();
        ChoiceCardListBinder.SyncChoices(_cards, models, _cardScene, SelectOpponent);
        RefreshSelected();
    }

    public void FocusSelection()
    {
        _cards.GetChildren().OfType<ChoiceCard>().FirstOrDefault(card => card.StableId == _selectedId)?.GrabFocus();
    }

    public void ShowMessage(string message)
    {
        _status.Text = message;
        _status.TooltipText = message;
        _status.Visible = !string.IsNullOrWhiteSpace(message);
    }

    private void SelectOpponent(string optionId)
    {
        if (!_options.Any(option => option.OptionId == optionId)) return;
        _selectedId = optionId;
        ShowMessage(string.Empty);
        RefreshSelected();
    }

    private void RefreshSelected()
    {
        var app = _application ?? throw new InvalidOperationException("Opponent panel is not bound.");
        var option = _options.Single(candidate => candidate.OptionId == _selectedId);
        foreach (var card in _cards.GetChildren().OfType<ChoiceCard>().Where(card => !card.IsQueuedForDeletion()))
        {
            var candidate = _options.Single(item => item.OptionId == card.StableId);
            var selected = card.StableId == _selectedId;
            card.Bind(candidate.OptionId, (selected ? "✓ " : string.Empty) + candidate.Title,
                candidate.PressureSummary + "\n" + Composition(app, candidate.Encounter), RewardText(app, candidate),
                _icons!.ResolveIcon(SemanticIconKeys.TowerNodeSemantic(candidate.NodeType)));
            card.ToggleMode = true;
            card.SetPressedNoSignal(selected);
            card.AccessibilityName = (selected ? "当前选择 · " : string.Empty) + candidate.Title;
        }
        _title.Text = option.Title;
        _pressure.Text = option.PressureSummary;
        _reward.Text = "固定胜利收益：" + RewardText(app, option);
        _risk.Text = RunHealthText.Risk(app.Rules, option.NodeType);
        _unitPicker.Clear();
        _unitIds.Clear();
        foreach (var group in option.Encounter.EnemyIds.GroupBy(id => id))
        {
            _unitIds.Add(group.Key);
            _unitPicker.AddItem($"{Definition(app, group.Key).DisplayName} ×{group.Count()}");
        }
        _unitPicker.Select(0);
        _confirm.Text = "挑战此对手 · 进入整备";
        _confirm.Disabled = false;
        RefreshUnit(0);
    }

    private void OnUnitSelected(long index) => RefreshUnit((int)index);

    private void RefreshUnit(int index)
    {
        if (_application is not { } app || index < 0 || index >= _unitIds.Count) return;
        var option = _options.Single(candidate => candidate.OptionId == _selectedId);
        if (!_previews.TryGetValue(option.OptionId, out var preview))
        {
            var config = app.BuildBattleConfig(option.Encounter, false);
            var details = new Dictionary<string, PreparedUnitDetails>(StringComparer.Ordinal);
            foreach (var unit in BattlePreparationProjection.Build(config).Units.Where(unit => unit.Team == 1))
                    details[unit.SourceInstanceId] = new PreparedUnitDetails(unit.Definition,
                        unit.Attributes.Values, unit.Health, unit.Shield,
                        "敌方 · 本场开战属性");
            preview = new OpponentPreview(config, details);
            _previews.Add(option.OptionId, preview);
        }
        var contentId = _unitIds[index];
        var units = preview.Config.Spawns.Where(spawn => spawn.Team == 1 && spawn.Unit.ContentId == contentId).ToArray();
        var first = units[0];
        _floorRule.Text = "战场：" + preview.Config.FloorRule.DisplayName;
        _floorRule.TooltipText = preview.Config.FloorRule.PreviewText;
        var entry = Required(app, contentId);
        var baseline = BattleSetupFactory.Snapshot(entry, app.Content);
        _unitDetails.Bind(new UnitInformation(option.OptionId + "/" + contentId, (UnitDefinition)entry.Definition,
            baseline, preview.Details[first.InstanceId]));
        _positions.Text = "敌阵位置：" + string.Join("、", units.Select(unit => $"({unit.Cell.X + 1},{unit.Cell.Y + 1})"));
        _positions.TooltipText = "横向列、纵向行，从 1 开始。具体阵型可在挑战后的整备战场查看。";
    }

    private void OnBack() => BackRequested?.Invoke();
    private void OnConfirm()
    {
        if (!_confirm.Disabled && _options.Any(option => option.OptionId == _selectedId))
            Confirmed?.Invoke(_selectedId);
    }

    private static string Composition(RunApplication app, EncounterPlan encounter) => string.Join("、",
        encounter.EnemyIds.GroupBy(id => id).Select(group => $"{Definition(app, group.Key).DisplayName}×{group.Count()}"));

    private static string RewardText(RunApplication app, OpponentOption option)
    {
        var baseGold = option.Encounter.IsBoss ? app.Rules.BossBattleGold :
            option.Encounter.IsElite ? app.Rules.EliteBattleGold : app.Rules.NormalBattleGold;
        var rewards = new List<string> { $"金币 {baseGold + option.RewardGold}" };
        if (option.RewardResearch > 0) rewards.Add($"研究 {option.RewardResearch}");
        if (option.RewardMaterials > 0) rewards.Add($"材料 {option.RewardMaterials}");
        return string.Join(" · ", rewards);
    }

    private static UnitDefinition Definition(RunApplication app, string id) => (UnitDefinition)Required(app, id).Definition;
    private static CatalogEntry Required(RunApplication app, string id) => app.Content.TryGet(id, out var entry)
        ? entry : throw new InvalidOperationException("Missing opponent content: " + id);

    private sealed record OpponentPreview(BattleConfig Config, IReadOnlyDictionary<string, PreparedUnitDetails> Details);
}
