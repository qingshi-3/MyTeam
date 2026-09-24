using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Run;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;

namespace TowerAutobattler.UI;

public partial class HeroSelectScreen : Control
{
    [Signal] public delegate void HeroChosenEventHandler(string stableId);
    public event Action? BackRequested;
    public event Action<string>? OpeningHeroToggled;
    public event Action? OpeningConfirmed;
    [Export] public PackedScene HeroLibraryTileScene { get; set; } = null!;

    private GridContainer _library = null!;
    private ScrollContainer _libraryScroll = null!;
    private HeroDetailPanel _detail = null!;
    private Button _back = null!;
    private Button _confirm = null!;
    private Label _status = null!;
    private Label _hint = null!;
    private OpeningRecruitmentDto? _opening;
    private readonly Dictionary<string, HeroSelectionViewModel> _models = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HeroLibraryTile> _tiles = new(StringComparer.Ordinal);
    private string _previewId = string.Empty;

    public string PreviewStableId => _previewId;

    public override void _Ready()
    {
        CacheNodes();
        Resized += UpdateResponsiveColumns;
        _libraryScroll.Resized += UpdateResponsiveColumns;
        _detail.DeployRequested += OnDeployRequested;
        _back.Pressed += OnBack;
        _confirm.Pressed += OnConfirm;
        UpdateResponsiveColumns();
    }

    public override void _ExitTree()
    {
        Resized -= UpdateResponsiveColumns;
        _libraryScroll.Resized -= UpdateResponsiveColumns;
        _detail.DeployRequested -= OnDeployRequested;
        _back.Pressed -= OnBack;
        _confirm.Pressed -= OnConfirm;
    }

    public void Bind(ContentRegistry content, MetaProgressDto meta)
        => BindContent(content, meta.UnlockedHeroIds, null);

    public void BindOpening(RunApplication app)
    {
        var opening = app.ActiveRun?.OpeningRecruitment ?? throw new InvalidOperationException("Missing opening draft.");
        BindContent(app.Content, opening.CandidateIds, app.Project.Campaign.RecruitmentSupply);
        RefreshOpening(opening);
    }

    private void BindContent(ContentRegistry content, IReadOnlyCollection<string> eligible, CompiledRecruitmentSupply? supply)
    {
        var heroes = new List<HeroSelectionViewModel>();
        var entries = supply is null ? content.Catalog.Heroes.AsEnumerable() : eligible.Select(id =>
            content.TryGet(id, out var entry) ? entry : throw new InvalidOperationException("Missing opening hero: " + id));
        foreach (var entry in entries)
        {
            var definition = (UnitDefinition)entry.Definition;
            var root = entry.Scene.Instantiate<UnitContentRoot>();
            try
            {
                var rule = root.HeroRule;
                heroes.Add(new HeroSelectionViewModel(
                    entry.StableId,
                    definition,
                    eligible.Contains(entry.StableId),
                    rule?.RuleTitle ?? string.Empty,
                    rule?.RuleDescription ?? string.Empty,
                    BattleSetupFactory.Snapshot(definition, root.Behavior,
                        root.AbilityLoadout?.Resolve(content.Graph), content.Graph) with
                    {
                        AttackHitGrowth = root.AttackHitGrowth?.Snapshot()
                    }, supply?.TierOf(entry.StableId) ?? 0));
            }
            finally { root.Free(); }
        }
        Bind(heroes);
    }

    public void Bind(IEnumerable<HeroSelectionViewModel> heroes)
    {
        CacheNodes();
        _opening = null;
        _confirm.Visible = false;
        _status.Text = string.Empty;
        foreach (var child in _library.GetChildren())
        {
            if (child is HeroLibraryTile existing) existing.SelectionRequested -= OnTilePressed;
            _library.RemoveChild(child);
            child.Free();
        }
        _models.Clear();
        _tiles.Clear();
        _previewId = string.Empty;
        foreach (var model in heroes)
        {
            _models.Add(model.StableId, model);
            var tile = HeroLibraryTileScene.Instantiate<HeroLibraryTile>();
            _library.AddChild(tile);
            tile.Bind(model);
            tile.SelectionRequested += OnTilePressed;
            _tiles.Add(model.StableId, tile);
        }
        var initial = _models.Values.FirstOrDefault(model => model.Unlocked) ?? _models.Values.FirstOrDefault();
        if (initial is not null)
        {
            Preview(initial.StableId);
            _tiles[initial.StableId].CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    public void Preview(string stableId)
    {
        if (!_models.TryGetValue(stableId, out var model)) return;
        _previewId = stableId;
        foreach (var pair in _tiles) pair.Value.SetPreviewed(pair.Key == stableId);
        _detail.Bind(model);
        RefreshSelectionState();
    }

    public void RefreshOpening(OpeningRecruitmentDto opening)
    {
        _opening = opening;
        _confirm.Visible = true;
        _confirm.Disabled = opening.SelectedIds.Length != CompiledRecruitmentSupply.OpeningSelectionCount;
        _status.Text = $"已选 {opening.SelectedIds.Length} / 2" + (opening.SelectedIds.IsEmpty ? "" :
            "　" + string.Join("、", opening.SelectedIds.Select(id => _models[id].Definition.DisplayName)));
        _hint.Text = "从这六名英雄中选择两名。再次点击可取消，确认后进入首战备战。";
        RefreshSelectionState();
    }

    public void ShowOpeningError(string message) => _hint.Text = message;

    private void RefreshSelectionState()
    {
        if (_opening is not { } opening) return;
        foreach (var pair in _tiles)
            pair.Value.SetOpeningState(_models[pair.Key].RecruitmentTier, opening.SelectedIds.Contains(pair.Key));
        if (_models.TryGetValue(_previewId, out var model))
            _detail.SetOpeningAction(model.RecruitmentTier, opening.SelectedIds.Contains(_previewId), opening.SelectedIds.Length >= 2);
    }

    private void OnTilePressed(string stableId)
    {
        Preview(stableId);
        if (_opening is null) return;
        if (_opening.SelectedIds.Length >= 2 && !_opening.SelectedIds.Contains(stableId))
        { ShowOpeningError("已选满两名；先取消其中一名，再选择新的英雄。右侧仍可查看技能。"); return; }
        OpeningHeroToggled?.Invoke(stableId);
    }

    private void OnDeployRequested(string stableId)
    {
        if (_opening is not null) { OpeningHeroToggled?.Invoke(stableId); return; }
        if (_models.TryGetValue(stableId, out var model) && model.Unlocked)
            EmitSignal(SignalName.HeroChosen, stableId);
    }

    private void OnBack() => BackRequested?.Invoke();
    private void OnConfirm() { if (!_confirm.Disabled) OpeningConfirmed?.Invoke(); }

    private void UpdateResponsiveColumns()
    {
        // The allocated library width, rather than window width, determines whether tiles fit.
        if (_libraryScroll is not null)
            _library.Columns = Math.Clamp((int)((_libraryScroll.Size.X - 16 + 8) / (190 + 8)), 1, 3);
    }

    private void CacheNodes()
    {
        _library ??= GetNode<GridContainer>("%HeroLibrary");
        _libraryScroll ??= GetNode<ScrollContainer>("%LibraryScroll");
        _detail ??= GetNode<HeroDetailPanel>("%HeroDetailPanel");
        _back ??= GetNode<Button>("%BackButton");
        _confirm ??= GetNode<Button>("%ConfirmOpening");
        _status ??= GetNode<Label>("%OpeningStatus");
        _hint ??= GetNode<Label>("%OpeningHint");
    }
}
