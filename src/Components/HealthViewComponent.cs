using Godot;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Statuses;
using TowerAutobattler.Presentation;
using TowerAutobattler.UI;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class HealthViewComponent : Node2D
{
    [Export] public ProgressBar Bar { get; set; } = null!;
    [Export] public ProgressBar ManaBar { get; set; } = null!;
    [Export] public ProgressBar ShieldBar { get; set; } = null!;
    [Export] public Label ReadyMarker { get; set; } = null!;
    [Export] public Label StatusText { get; set; } = null!;
    [Export] public TextureRect StatusIcon { get; set; } = null!;
    [Export] public Line2D ControlOutline { get; set; } = null!;
    [Export] public Line2D ShieldOutline { get; set; } = null!;
    [Export] public Line2D CastOutline { get; set; } = null!;
    [Export] public Label CastName { get; set; } = null!;
    private Tween? _castTween;
    private bool _paused;

    public void SetHealth(float current, float maximum)
    {
        if (Bar is null) return;
        Bar.MaxValue = maximum;
        Bar.Value = Mathf.Clamp(current, 0, maximum);
    }

    public void SetCombatResources(float mana, float maximumMana, float shield,
        ImmutableArray<StatusRuntimeSnapshot> statuses, bool alive)
    {
        ManaBar.Visible = alive && maximumMana > 0;
        ManaBar.MaxValue = Mathf.Max(1, maximumMana);
        ManaBar.Value = Mathf.Clamp(mana, 0, maximumMana);
        ReadyMarker.Visible = ManaBar.Visible && mana >= maximumMana;
        ShieldBar.Visible = alive && shield > 0;
        ShieldBar.MaxValue = Mathf.Max((float)Bar.MaxValue, shield);
        ShieldBar.Value = shield;
        ShieldOutline.Visible = ShieldBar.Visible;
        var visibleStatuses = statuses.IsDefaultOrEmpty ? [] : statuses
            .OrderByDescending(StatusDisplayFacts.DisablesActions)
            .ThenByDescending(item => item.Disposition == StatusDisposition.Harmful)
            .ThenBy(item => item.ApplicationSequence).ToArray();
        var primary = visibleStatuses.FirstOrDefault();
        ControlOutline.Visible = alive && visibleStatuses.Any(StatusDisplayFacts.DisablesActions);
        ControlOutline.DefaultColor = visibleStatuses.Any(StatusDisplayFacts.Frozen)
            ? new Color(.48f, .85f, 1f, .9f) : new Color(1f, .72f, .32f, .9f);
        StatusText.Visible = alive && primary is not null;
        StatusIcon.Visible = StatusText.Visible;
        if (primary is not null)
        {
            StatusIcon.Texture = SemanticIcons.Catalog.ResolveIcon(StatusDisplayFacts.IconKey(primary))
                ?? SemanticIcons.Catalog.ResolveIcon("risk");
            StatusText.Text = $"{primary.DisplayName} ×{primary.Stacks}" +
                (visibleStatuses.Length > 1 ? $" +{visibleStatuses.Length - 1}" : "");
            StatusText.TooltipText = string.Join("\n", visibleStatuses.Select(item =>
                $"{item.DisplayName} ×{item.Stacks} · {StatusDisplayFacts.Duration(item)}"));
        }
        if (!alive) ResetCast();
    }

    // Called only for a committed ability fact. Resource changes never fabricate a cast.
    public void PresentAbility(string name)
    {
        _castTween?.Kill();
        CastName.Text = string.IsNullOrWhiteSpace(name) ? "施法" : name;
        CastName.Modulate = Colors.White;
        CastName.Visible = true;
        CastOutline.Visible = true;
        CastOutline.Modulate = Colors.White;
        CastOutline.Scale = Vector2.One;
        _castTween = CreateTween().SetParallel();
        _castTween.TweenProperty(CastOutline, "scale", Vector2.One * 1.45f, .42);
        _castTween.TweenProperty(CastOutline, "modulate:a", 0f, .42);
        _castTween.TweenProperty(CastName, "modulate:a", 0f, .7).SetDelay(.25);
        _castTween.Chain().TweenCallback(Callable.From(ResetCast));
        if (_paused) _castTween.Pause();
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        if (_castTween is null) return;
        if (paused) _castTween.Pause();
        else _castTween.Play();
    }

    private void ResetCast()
    {
        _castTween?.Kill();
        _castTween = null;
        CastName.Visible = false;
        CastOutline.Visible = false;
    }

    public override void _ExitTree() => _castTween?.Kill();
}
