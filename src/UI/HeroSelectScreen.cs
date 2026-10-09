using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Run;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;
using TowerAutobattler.Growth;

namespace TowerAutobattler.UI;

public partial class HeroSelectScreen : Control
{
    [Signal] public delegate void HeroChosenEventHandler(string stableId);
    public event Action? BackRequested;
    public event Action<string>? OpeningHeroToggled;
    public event Action? OpeningConfirmed;
    [Export] public PackedScene HeroLibraryTileScene { get; set; } = null!;
    [Export] public PackedScene OpeningHeroLibraryTileScene { get; set; } = null!;
    [Export] public StyleBox OpeningLibraryStyle { get; set; } = null!;
    [Export] public Theme OpeningTheme { get; set; } = null!;

    private GridContainer _library = null!;
    private ScrollContainer _libraryScroll = null!;
    private PanelContainer _libraryPanel = null!;
    private HeroDetailPanel _detail = null!;
    private Button _back = null!;
    private Button _confirm = null!;
    private Label _status = null!;
    private Label _hint = null!;
    private OpeningHeroSeat _firstSeat = null!;
    private OpeningHeroSeat _secondSeat = null!;
    private OpeningRecruitmentDto? _opening;
    private readonly Dictionary<string, HeroSelectionViewModel> _models = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HeroLibraryTile> _tiles = new(StringComparer.Ordinal);
    private string _previewId = string.Empty;

    public string PreviewStableId => _previewId;

    // The journey host owns the departure after the opening transaction succeeds.
    public Control[] DeparturePortraits => new[] { _firstSeat, _secondSeat }
        .Where(seat => seat.StableId.Length > 0).Select(seat => seat.Portrait).ToArray();
    public Control[] DepartureSurfaces => new Control[]
    {
        GetNode<Control>("OpeningBackdrop"), GetNode<Control>("Margin/Layout/Title"),
        _library, _back, _confirm, _status,
        _firstSeat.GetNode<Control>("Name"), _secondSeat.GetNode<Control>("Name"),
        _firstSeat.GetNode<Control>("EmptyRing"), _secondSeat.GetNode<Control>("EmptyRing"),
        _firstSeat.GetNode<Control>("Ordinal"), _secondSeat.GetNode<Control>("Ordinal")
    };

    public void PrepareDeparture()
    {
        _firstSeat.CancelArrival();
        _secondSeat.CancelArrival();
        foreach (var tile in _tiles.Values) tile.Disclosure.Reset();
    }

    public override void _Ready()
    {
        CacheNodes();
        Resized += UpdateResponsiveColumns;
        _libraryScroll.Resized += UpdateResponsiveColumns;
        _detail.DeployRequested += OnDeployRequested;
        _back.Pressed += OnBack;
        _confirm.Pressed += OnConfirm;
        _firstSeat.Pressed += OnFirstSeat;
        _secondSeat.Pressed += OnSecondSeat;
        UpdateResponsiveColumns();
    }

    public override void _ExitTree()
    {
        Resized -= UpdateResponsiveColumns;
        _libraryScroll.Resized -= UpdateResponsiveColumns;
        _detail.DeployRequested -= OnDeployRequested;
        _back.Pressed -= OnBack;
        _confirm.Pressed -= OnConfirm;
        _firstSeat.Pressed -= OnFirstSeat;
        _secondSeat.Pressed -= OnSecondSeat;
    }

    public void Bind(ContentRegistry content, MetaProgressDto meta)
        => BindContent(content, meta.UnlockedHeroIds, null, null);

    public void BindOpening(RunApplication app)
    {
        var opening = app.Run?.OpeningRecruitment ?? throw new InvalidOperationException("Missing opening draft.");
        BindContent(app.Content, opening.CandidateIds, app.Project.Campaign.RecruitmentSupply, app.GrowthRules);
        RefreshOpening(opening);
    }

    // A presentation snapshot entry also lets the authored screen be previewed
    // while campaign content is being rebuilt. It never creates a run or draft.
    public void BindOpening(IReadOnlyList<HeroSelectionViewModel> candidates, OpeningRecruitmentDto opening)
    {
        Bind(candidates, true);
        RefreshOpening(opening);
    }

    private void BindContent(ContentRegistry content, IReadOnlyCollection<string> eligible, CompiledRecruitmentSupply? supply,
        CompiledGrowthRules? growthRules)
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
                var snapshot = BattleSetupFactory.Snapshot(definition, root.Behavior,
                    root.AbilityLoadout?.Resolve(content.Graph), content.Graph) with
                    {
                        AttackHitGrowth = root.AttackHitGrowth?.Snapshot()
                    };
                var growth = GrowthPresentation.Project(entry.StableId, snapshot, growthRules);
                var ruleTitle = growth.HasGrowthPlan ? growth.Title : rule?.RuleTitle ?? string.Empty;
                var legacyRule = snapshot.AbilityLoadout is null && snapshot.AttackHitGrowth is null
                    ? rule?.RuleDescription ?? string.Empty : string.Empty;
                var ruleDescription = string.Join("\n\n", new[] { growth.Description, legacyRule }
                    .Where(text => !string.IsNullOrWhiteSpace(text)));
                heroes.Add(new HeroSelectionViewModel(
                    entry.StableId,
                    definition,
                    eligible.Contains(entry.StableId),
                    ruleTitle,
                    ruleDescription,
                    growth.Snapshot, supply?.TierOf(entry.StableId) ?? 0)
                { AlwaysShowRule = growth.HasGrowthPlan });
            }
            finally { root.Free(); }
        }
        Bind(heroes, supply is not null);
    }

    public void Bind(IEnumerable<HeroSelectionViewModel> heroes) => Bind(heroes, false);

    private void Bind(IEnumerable<HeroSelectionViewModel> heroes, bool opening)
    {
        CacheNodes();
        _opening = null;
        Theme = opening ? OpeningTheme : null;
        GetNode<Control>("OpeningBackdrop").Visible = opening;
        var title = GetNode<Label>("Margin/Layout/Title");
        title.Text = opening ? "选择同行的两位英雄" : "选择英雄";
        title.ThemeTypeVariation = opening ? "OpeningTitle" : "PageTitle";
        if (opening) title.AddThemeFontSizeOverride("font_size", 32);
        else title.RemoveThemeFontSizeOverride("font_size");
        title.HorizontalAlignment = opening ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        _back.ThemeTypeVariation = opening ? "OpeningBack" : "SecondaryButton";
        var margin = GetNode<MarginContainer>("Margin");
        margin.AddThemeConstantOverride("margin_top", opening ? 18 : 20);
        margin.AddThemeConstantOverride("margin_bottom", opening ? 12 : 20);
        GetNode<Control>("Margin/Layout/Footer").CustomMinimumSize = new Vector2(0, opening ? 106 : 50);
        _firstSeat.Visible = _secondSeat.Visible = opening;
        _firstSeat.Bind(null); _secondSeat.Bind(null);
        _detail.Visible = !opening;
        if (opening) _libraryPanel.AddThemeStyleboxOverride("panel", OpeningLibraryStyle);
        else _libraryPanel.RemoveThemeStyleboxOverride("panel");
        _library.AddThemeConstantOverride("h_separation", opening ? 30 : 24);
        _library.AddThemeConstantOverride("v_separation", opening ? 18 : 10);
        _libraryScroll.HorizontalScrollMode = opening ? ScrollContainer.ScrollMode.Disabled : ScrollContainer.ScrollMode.ShowNever;
        _libraryScroll.VerticalScrollMode = opening ? ScrollContainer.ScrollMode.Disabled : ScrollContainer.ScrollMode.Auto;
        _library.GetParent<Control>().SizeFlagsVertical = opening ? SizeFlags.ExpandFill : SizeFlags.Fill;
        _confirm.Visible = false;
        _status.Text = string.Empty;
        _status.Hide();
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
            var tile = (opening ? OpeningHeroLibraryTileScene : HeroLibraryTileScene).Instantiate<HeroLibraryTile>();
            _library.AddChild(tile);
            tile.Bind(model);
            tile.SelectionRequested += OnTilePressed;
            _tiles.Add(model.StableId, tile);
        }
        var initial = _models.Values.FirstOrDefault(model => model.Unlocked) ?? _models.Values.FirstOrDefault();
        if (initial is not null)
        {
            Preview(initial.StableId);
            var initialTile = _tiles[initial.StableId];
            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(initialTile) || !initialTile.IsInsideTree()) return;
                initialTile.GrabFocus();
                // Focus can run before the opening page finishes reflowing.
                // Start at the card's upper edge, not a stale scroll offset;
                // subsequent keyboard navigation still uses follow_focus.
                if (_opening is not null)
                    _libraryScroll.SetDeferred(ScrollContainer.PropertyName.ScrollVertical, 0);
            }).CallDeferred();
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
        var animateSelection = _opening is not null && IsVisibleInTree();
        var seatedSources = new[] { _firstSeat, _secondSeat }
            .Where(seat => seat.StableId.Length > 0)
            .ToDictionary(seat => seat.StableId, seat => seat.Portrait.GetGlobalRect().GetCenter());
        _opening = opening;
        _detail.Hide();
        UpdateResponsiveColumns();
        _confirm.Visible = true;
        _confirm.Disabled = opening.SelectedIds.Length != CompiledRecruitmentSupply.OpeningSelectionCount;
        _status.Text = string.Empty;
        _status.Hide();
        _status.ThemeTypeVariation = "SecondaryLabel";
        _hint.Text = string.Empty;
        _hint.Visible = false;
        RefreshSelectionState();
        BindSeat(_firstSeat, opening.SelectedIds.Length > 0 ? opening.SelectedIds[0] : null, animateSelection, seatedSources);
        BindSeat(_secondSeat, opening.SelectedIds.Length > 1 ? opening.SelectedIds[1] : null, animateSelection, seatedSources);
    }

    private void BindSeat(OpeningHeroSeat seat, string? stableId, bool animate, IReadOnlyDictionary<string, Vector2> seatedSources)
    {
        var changed = seat.StableId != (stableId ?? string.Empty);
        seat.Bind(stableId is not null ? _models[stableId] : null);
        if (animate && changed && stableId is not null && _tiles.TryGetValue(stableId, out var source))
            seat.ArriveFrom(seatedSources.TryGetValue(stableId, out var previousSeat)
                ? previousSeat : source.GetNode<Control>("%HeroPortrait").GetGlobalRect().GetCenter());
    }

    public void ShowOpeningError(string message)
    {
        // Keep feedback in the fixed footer so it cannot displace the candidates.
        _status.Text = message;
        _status.ThemeTypeVariation = "DangerValue";
        _status.Show();
    }

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
        { ShowOpeningError("已选满两名，请先取消一名。"); return; }
        OpeningHeroToggled?.Invoke(stableId);
    }

    private void OnDeployRequested(string stableId)
    {
        if (_opening is not null) { OpeningHeroToggled?.Invoke(stableId); return; }
        if (_models.TryGetValue(stableId, out var model) && model.Unlocked)
            EmitSignal(SignalName.HeroChosen, stableId);
    }

    private void OnBack() => BackRequested?.Invoke();
    private void OnFirstSeat() { if (_firstSeat.StableId.Length > 0) OpeningHeroToggled?.Invoke(_firstSeat.StableId); }
    private void OnSecondSeat() { if (_secondSeat.StableId.Length > 0) OpeningHeroToggled?.Invoke(_secondSeat.StableId); }
    private void OnConfirm() { if (!_confirm.Disabled) OpeningConfirmed?.Invoke(); }

    private void UpdateResponsiveColumns()
    {
        if (_libraryScroll is null) return;
        var gap = _library.GetThemeConstant("h_separation");
        var opening = _opening is not null;
        _library.Columns = opening ? Math.Min(3, Math.Max(1, _tiles.Count))
            : Math.Clamp((int)((_libraryScroll.Size.X + gap) / (470 + gap)), 1, 3);
        _library.SizeFlagsHorizontal = opening ? SizeFlags.ShrinkCenter : SizeFlags.ExpandFill;
        if (!opening || _tiles.Count == 0) return;
        // Reserve side air for the entire card's lift. Keep the irregular stone
        // frame at its authored 2:3 ratio; it is never stretched as a nine-patch.
        var rows = Mathf.CeilToInt(_tiles.Count / 3f);
        var rowGap = _library.GetThemeConstant("v_separation");
        var widthByColumns = (_libraryScroll.Size.X - 48 - 2 * gap) / 3f;
        var widthByRows = (_libraryScroll.Size.Y - 20 - (rows - 1) * rowGap) / (rows * 1.5f);
        var width = Math.Clamp(Mathf.Floor(Mathf.Min(widthByColumns, widthByRows)), 180, 290);
        var height = width * 1.5f;
        foreach (var tile in _tiles.Values) tile.CustomMinimumSize = new Vector2(width, height);
    }

    private void CacheNodes()
    {
        _library ??= GetNode<GridContainer>("%HeroLibrary");
        _libraryScroll ??= GetNode<ScrollContainer>("%LibraryScroll");
        _libraryPanel ??= GetNode<PanelContainer>("%LibraryPanel");
        _detail ??= GetNode<HeroDetailPanel>("%HeroDetailPanel");
        _back ??= GetNode<Button>("%BackButton");
        _confirm ??= GetNode<Button>("%ConfirmOpening");
        _status ??= GetNode<Label>("%OpeningStatus");
        _hint ??= GetNode<Label>("%OpeningHint");
        _firstSeat ??= GetNode<OpeningHeroSeat>("%FirstSeat");
        _secondSeat ??= GetNode<OpeningHeroSeat>("%SecondSeat");
    }
}
