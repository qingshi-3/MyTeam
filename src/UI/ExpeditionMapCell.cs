using System;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

public partial class ExpeditionMapCell : Button, IUiMotionHost
{
    [Signal] public delegate void SelectedEventHandler(int cellIndex);

    [Export] public Texture2D? GrasslandTexture { get; set; }
    [Export] public Texture2D? ForestTexture { get; set; }
    [Export] public Texture2D? RuinsTexture { get; set; }
    [Export] public Texture2D? WaterTexture { get; set; }
    [Export] public Texture2D? MountainTexture { get; set; }
    [Export] public Texture2D? CombatIcon { get; set; }
    [Export] public Texture2D? EliteIcon { get; set; }
    [Export] public Texture2D? RecruitmentIcon { get; set; }
    [Export] public Texture2D? ShopIcon { get; set; }
    [Export] public Texture2D? CampIcon { get; set; }
    [Export] public Texture2D? RuinsIcon { get; set; }

    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    public bool UseExternalPartyMarker { get; set; }
    private Tween? _revealTween;
    private Tween? _discoveryTween;
    private Tween? _cueTween;
    private ShaderMaterial _fogMaterial = null!;
    private ShaderMaterial _siteLightMaterial = null!;
    private ShaderMaterial _discoveryMaterial = null!;
    private Control _siteLight = null!;
    private Control _discoveryMotes = null!;
    private Sprite2D[] _motes = [];
    private Vector2 _revealOrigin = new(.5f, .5f);
    private float _revealReach = 2.4f;
    private ColorRect _ground = null!;
    private TextureRect _terrain = null!;
    private Control _fog = null!;
    private Control _site = null!;
    private TextureRect _siteIcon = null!;
    private Label _siteKind = null!;
    private Label _complete = null!;
    private Label _blocked = null!;
    private Control _party = null!;
    private Control _selection = null!;
    private ExpeditionCellModel? _model;
    private bool _isParty;
    private bool _isSelected;
    private bool _ready;
    private bool _hovered;
    private bool _siteInteractionAvailable;
    private bool _pendingDiscovery;
    private float _lightTime;
    private float _cueStrength;

    public int CellIndex => _model?.Index ?? -1;
    public bool IsRevealed => _model?.IsRevealed == true;

    public override void _Ready()
    {
        _ground = GetNode<ColorRect>("Ground");
        _terrain = GetNode<TextureRect>("Terrain");
        _fog = GetNode<Control>("Fog");
        _fogMaterial = (ShaderMaterial)_fog.Material.Duplicate();
        _fog.Material = _fogMaterial;
        _siteLight = GetNode<Control>("SiteLight");
        _siteLightMaterial = (ShaderMaterial)_siteLight.Material.Duplicate();
        _siteLight.Material = _siteLightMaterial;
        _site = GetNode<Control>("Site");
        _siteIcon = GetNode<TextureRect>("Site/Icon");
        _discoveryMaterial = (ShaderMaterial)_siteIcon.Material.Duplicate();
        _siteIcon.Material = _discoveryMaterial;
        _discoveryMotes = GetNode<Control>("Site/DiscoveryMotes");
        _motes = _discoveryMotes.GetChildren().Cast<Sprite2D>().ToArray();
        _siteKind = GetNode<Label>("Site/Kind");
        _complete = GetNode<Label>("Completed");
        _blocked = GetNode<Label>("Blocked");
        _party = GetNode<Control>("Party");
        _selection = GetNode<Control>("Selection");
        Pressed += OnPressed;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        FocusEntered += RefreshLighting;
        FocusExited += RefreshLighting;
        VisibilityChanged += OnVisibilityChanged;
        Resized += RefreshLighting;
        _ready = true;
        SetProcess(false);
        Refresh();
    }

    public override void _ExitTree()
    {
        if (_ready)
        {
            Pressed -= OnPressed;
            MouseEntered -= OnMouseEntered;
            MouseExited -= OnMouseExited;
            FocusEntered -= RefreshLighting;
            FocusExited -= RefreshLighting;
            VisibilityChanged -= OnVisibilityChanged;
            Resized -= RefreshLighting;
            SettleReveal();
        }
        _ready = false;
    }

    public void Bind(ExpeditionCellModel model, bool isParty, bool isSelected)
    {
        var sameCell = _model?.Index == model.Index;
        var newlyRevealed = sameCell && _model is { IsRevealed: false } && model.IsRevealed;
        var changedSite = _model?.SiteId != model.SiteId || _model?.SiteAppearance != model.SiteAppearance;
        if ((!sameCell || changedSite) && _ready) SettleReveal();
        // Bind refreshes the whole snapshot; eligibility must be supplied again
        // afterwards so a formerly usable site's warm light cannot go stale.
        _siteInteractionAvailable = false;
        _model = model;
        _isParty = isParty;
        _isSelected = isSelected;
        if (_ready)
        {
            Refresh();
            if (newlyRevealed && IsVisibleInTree() && !ReduceUiMotion()) Reveal();
        }
    }

    public void SetRevealOrigin(Vector2 origin, float reach = 2.4f)
    {
        _revealOrigin = origin;
        _revealReach = reach;
    }

    // The screen supplies authoritative interaction eligibility. Movement and
    // reachability never stand in for the right to interact with a site.
    public void SetSiteInteractionAvailable(bool available)
    {
        _siteInteractionAvailable = available;
        if (_ready) RefreshLighting();
    }

    public void PulseSiteCue()
    {
        if (!_ready || !HasKnownSite || !IsVisibleInTree() || ReduceUiMotion()) return;
        _cueTween?.Kill();
        _cueStrength = 0;
        _cueTween = CreateTween().SetIgnoreTimeScale(true).SetPauseMode(Tween.TweenPauseMode.Process);
        _cueTween.TweenMethod(Callable.From<float>(value =>
        {
            _cueStrength = Mathf.Sin(value * Mathf.Pi) * .8f;
            RefreshLighting();
        }), 0f, 1f, .24);
        _cueTween.TweenCallback(Callable.From(ClearCue));
        SetProcess(true);
    }

    private void Reveal()
    {
        _revealTween?.Kill();
        ClearDiscovery();
        _pendingDiscovery = _model?.SiteAppearance != ExpeditionSiteAppearance.None;
        _site.Hide();
        _complete.Hide();
        _blocked.Hide();
        _siteLight.Hide();
        _fog.Visible = true;
        _fogMaterial.SetShaderParameter("reveal_origin", _revealOrigin);
        _fogMaterial.SetShaderParameter("reveal_reach", _revealReach);
        _fogMaterial.SetShaderParameter("reveal", 0f);
        _revealTween = CreateTween().SetIgnoreTimeScale(true).SetPauseMode(Tween.TweenPauseMode.Process);
        _revealTween.TweenMethod(Callable.From<float>(value => _fogMaterial.SetShaderParameter("reveal", value)),
            0f, 1f, .34).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _revealTween.TweenCallback(Callable.From(FinishReveal));
        SetProcess(true);
    }

    public void SettleReveal()
    {
        _revealTween?.Kill();
        _revealTween = null;
        _pendingDiscovery = false;
        ClearDiscovery();
        ClearCue();
        if (_ready)
        {
            _fog.Visible = _model?.IsRevealed != true;
            _fogMaterial.SetShaderParameter("reveal", 0f);
            _site.Visible = _model is { IsRevealed: true, SiteAppearance: not ExpeditionSiteAppearance.None };
            Refresh();
        }
    }

    private void FinishReveal()
    {
        var discover = _pendingDiscovery && !ReduceUiMotion() && IsVisibleInTree();
        _revealTween = null;
        _pendingDiscovery = false;
        _fog.Hide();
        _fogMaterial.SetShaderParameter("reveal", 0f);
        _site.Visible = _model is { IsRevealed: true, SiteAppearance: not ExpeditionSiteAppearance.None };
        Refresh();
        if (discover && _site.Visible) DiscoverSite();
        RefreshLighting();
    }

    private void DiscoverSite()
    {
        ClearDiscovery();
        _discoveryMotes.Show();
        _discoveryTween = CreateTween().SetIgnoreTimeScale(true).SetPauseMode(Tween.TweenPauseMode.Process);
        _discoveryTween.TweenMethod(Callable.From<float>(AdvanceDiscovery), 0f, 1f, .38)
            .SetTrans(Tween.TransitionType.Linear);
        _discoveryTween.TweenCallback(Callable.From(ClearDiscovery));
        AdvanceDiscovery(0);
        SetProcess(true);
    }

    private void AdvanceDiscovery(float progress)
    {
        _discoveryMaterial.SetShaderParameter("discovery", progress);
        var center = _siteIcon.Position + _siteIcon.Size * .5f;
        for (var index = 0; index < _motes.Length; index++)
        {
            var age = Mathf.Clamp((progress - index * .035f) / .75f, 0, 1);
            var outward = Vector2.FromAngle(index * Mathf.Tau / _motes.Length + .28f);
            var radius = _siteIcon.Size.X * (.36f + .10f * age);
            var sprite = _motes[index];
            sprite.Position = center + outward * radius;
            sprite.Scale = Vector2.One * (Mathf.Min(Size.X, Size.Y) / 56f) *
                (.040f + index % 2 * .013f) * (1 - age * .5f);
            sprite.Modulate = new Color(1, .89f, .62f, Mathf.Sin(age * Mathf.Pi) * .70f);
            sprite.Visible = age > 0 && age < 1;
        }
    }

    private void ClearDiscovery()
    {
        _discoveryTween?.Kill();
        _discoveryTween = null;
        if (!_ready) return;
        _discoveryMaterial.SetShaderParameter("discovery", 0f);
        _discoveryMotes.Hide();
        foreach (var sprite in _motes) sprite.Hide();
        UpdateProcessing();
    }

    private void ClearCue()
    {
        _cueTween?.Kill();
        _cueTween = null;
        _cueStrength = 0;
        if (_ready) RefreshLighting();
    }

    private bool HasKnownSite => _model is { IsRevealed: true, SiteAppearance: not ExpeditionSiteAppearance.None } && _revealTween is null;
    private bool HasWarmLight => HasKnownSite && _isParty && _siteInteractionAvailable;

    private void RefreshLighting()
    {
        if (!_ready) return;
        var visible = IsVisibleInTree();
        var focused = HasKnownSite && (_hovered || HasFocus());
        var warm = HasWarmLight;
        _siteLight.Visible = visible && HasKnownSite && (focused || warm || _cueStrength > 0);
        _siteLightMaterial.SetShaderParameter("cell_size", Size);
        _siteLightMaterial.SetShaderParameter("focus_strength", focused ? .88f : 0f);
        var ambientWarm = warm ? ReduceUiMotion() ? .66f : .66f + .09f * Mathf.Sin(_lightTime * 2.1f) : 0f;
        _siteLightMaterial.SetShaderParameter("warm_strength", Mathf.Max(ambientWarm, _cueStrength));
        UpdateProcessing();
    }

    private void UpdateProcessing() => SetProcess(_ready && IsVisibleInTree() &&
        (_revealTween is not null || _discoveryTween is not null || _cueTween is not null || HasWarmLight));

    private void OnMouseEntered() { _hovered = true; RefreshLighting(); }
    private void OnMouseExited() { _hovered = false; RefreshLighting(); }
    private void OnVisibilityChanged()
    {
        if (!_ready) return;
        if (!IsVisibleInTree()) { _hovered = false; _lightTime = 0; SettleReveal(); }
        else RefreshLighting();
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) { SettleReveal(); return; }
        if (ReduceUiMotion())
        {
            if (_revealTween is not null || _discoveryTween is not null || _cueTween is not null) SettleReveal();
            RefreshLighting();
            return;
        }
        if (Engine.TimeScale > .001) _lightTime += (float)(delta / Engine.TimeScale);
        RefreshLighting();
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (_ready) _selection.Visible = selected;
    }

    private void Refresh()
    {
        if (_model is not { } model) return;
        var known = model.IsRevealed;
        _fog.Visible = !known || _revealTween is not null;
        if (!known)
        {
            _revealTween?.Kill();
            _revealTween = null;
            _fogMaterial.SetShaderParameter("reveal", 0f);
            _pendingDiscovery = false;
            ClearDiscovery();
            ClearCue();
        }
        _ground.Visible = known;
        _terrain.Visible = known;
        _site.Visible = known && model.SiteAppearance != ExpeditionSiteAppearance.None && _revealTween is null;
        _complete.Visible = known && _revealTween is null && model.IsCompleted;
        _blocked.Visible = known && _revealTween is null && model.IsBlocked && !_site.Visible;
        _party.Visible = _isParty && !UseExternalPartyMarker;
        _selection.Visible = _isSelected;
        _terrain.Texture = model.Terrain switch
        {
            ExpeditionTerrainAppearance.Forest => ForestTexture,
            ExpeditionTerrainAppearance.Ruins => RuinsTexture,
            ExpeditionTerrainAppearance.Water => WaterTexture,
            ExpeditionTerrainAppearance.Mountain => MountainTexture,
            _ => GrasslandTexture
        };
        _ground.Color = model.Terrain switch
        {
            ExpeditionTerrainAppearance.Forest => new Color(.19f, .27f, .19f),
            ExpeditionTerrainAppearance.Ruins => new Color(.34f, .31f, .24f),
            ExpeditionTerrainAppearance.Water => new Color(.12f, .24f, .27f),
            ExpeditionTerrainAppearance.Mountain => new Color(.29f, .30f, .28f),
            _ => new Color(.34f, .38f, .24f)
        };
        _siteIcon.Texture = model.SiteIcon ?? ResolveSiteIcon(model.SiteAppearance);
        _siteIcon.Modulate = model.IsCompleted ? new Color(.69f, .71f, .60f, .75f) : new Color(.96f, .87f, .65f);
        _siteKind.Text = KindText(model.SiteAppearance);
        // Fog never leaks a hidden site's name, biome, icon or eligibility.
        var coordinate = model.Index >= 0 ? $"地块 {model.Index + 1}" : "地块";
        TooltipText = known
            ? string.Join("\n", model.SiteName, model.RegionName, model.TerrainName, model.StateText, model.MoveBlockedReason).Trim()
            : "战争迷雾\n靠近后揭示地貌与遭遇";
        AccessibilityName = known
            ? $"{coordinate}，{model.SiteName}，{model.TerrainName}，{model.StateText}{(_isParty ? "，军团当前位置" : string.Empty)}"
            : $"{coordinate}，战争迷雾";
        RefreshLighting();
    }

    public Texture2D? ResolveSiteIcon(ExpeditionSiteAppearance appearance) => appearance switch
    {
        ExpeditionSiteAppearance.Combat => CombatIcon,
        ExpeditionSiteAppearance.Elite => EliteIcon,
        ExpeditionSiteAppearance.Recruitment => RecruitmentIcon,
        ExpeditionSiteAppearance.Shop => ShopIcon,
        ExpeditionSiteAppearance.Camp => CampIcon,
        ExpeditionSiteAppearance.Ruins => RuinsIcon,
        _ => null
    };

    private static string KindText(ExpeditionSiteAppearance appearance) => appearance switch
    {
        ExpeditionSiteAppearance.Combat => "守军",
        ExpeditionSiteAppearance.Elite => "精英",
        ExpeditionSiteAppearance.Recruitment => "招募",
        ExpeditionSiteAppearance.Shop => "商店",
        ExpeditionSiteAppearance.Camp => "营地",
        ExpeditionSiteAppearance.Ruins => "遗迹",
        _ => string.Empty
    };

    private void OnPressed()
    {
        if (_model is { } model) EmitSignal(SignalName.Selected, model.Index);
    }
}
