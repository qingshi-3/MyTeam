using Godot;
using TowerAutobattler.Components;

namespace TowerAutobattler.Content;

[GlobalClass]
public partial class UnitContentRoot : Node2D
{
    [Signal] public delegate void ActorSelectedEventHandler(string runtimeId);

    [Export] public UnitDefinition Definition { get; set; } = null!;

    public string RuntimeId { get; private set; } = string.Empty;
    public int Team { get; private set; }
    public bool IsActive { get; private set; }
    public ContentLifecycleState LifecycleState { get; private set; }
    public HeroRuleComponent? HeroRule => GetNodeOrNull<HeroRuleComponent>("HeroRuleComponent");
    public UnitBehaviorComponent? Behavior => GetNodeOrNull<UnitBehaviorComponent>("UnitBehaviorComponent");
    public UnitAbilityLoadoutComponent? AbilityLoadout => GetNodeOrNull<UnitAbilityLoadoutComponent>("UnitAbilityLoadoutComponent");

    private UnitAnimationComponent? _animation;
    private UnitMotionPresentationComponent? _motion;
    private HealthViewComponent? _healthView;
    private UnitBindingContext? _context;
    private Tween? _defeatTween;

    public override void _Ready()
    {
        _animation = GetNodeOrNull<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
        _motion = GetNodeOrNull<UnitMotionPresentationComponent>("UnitMotionPresentationComponent");
        _healthView = GetNodeOrNull<HealthViewComponent>("HealthViewComponent");
        if (_animation is not null) _animation.DefeatFadeRequested += OnDefeatFadeRequested;
        if (_motion is not null)
        {
            _motion.MotionStateChanged += OnMotionStateChanged;
            _motion.HorizontalSegmentStarted += OnHorizontalSegmentStarted;
            _motion.SegmentProgressChanged += OnSegmentProgressChanged;
            _motion.BindTarget(this);
        }
        _animation?.PlayCue("idle");
    }

    public ValidationReport ValidateAuthoring()
    {
        var report = new ValidationReport();
        if (Definition is null) report.Error($"{SceneFilePath}: missing UnitDefinition");
        else if (string.IsNullOrWhiteSpace(Definition.Id)) report.Error($"{SceneFilePath}: empty stable id");
        else if (Definition.Portrait is null) report.Error($"{SceneFilePath}: missing UnitPortraitDefinition");
        else report.Merge(Definition.Portrait.Validate(Definition.Id));
        if (GetNodeOrNull<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent") is null)
            report.Error($"{SceneFilePath}: missing animation component");
        if (GetNodeOrNull<UnitMotionPresentationComponent>("UnitMotionPresentationComponent") is null)
            report.Error($"{SceneFilePath}: missing motion presentation component");
        if (GetNodeOrNull<HealthViewComponent>("HealthViewComponent") is null)
            report.Error($"{SceneFilePath}: missing health view component");
        if (Behavior is null) report.Error($"{SceneFilePath}: missing unit behavior component");
        if (AbilityLoadout is not null) report.Merge(AbilityLoadout.ValidateAuthoring());
        if (Definition?.IsHero == true && HeroRule is null)
            report.Error($"{SceneFilePath}: hero missing rule component");
        return report;
    }

    public void Bind(string runtimeId, int team, float currentHealth, float maxHealth)
    {
        if (LifecycleState == ContentLifecycleState.Active) throw new System.InvalidOperationException("Active unit cannot be rebound.");
        if (string.IsNullOrWhiteSpace(runtimeId)) throw new System.ArgumentException("Runtime id is required.", nameof(runtimeId));
        RuntimeId = runtimeId;
        Team = team;
        Modulate = team == 0 ? Colors.White : new Color(1f, 0.72f, 0.72f);
        Visible = true;
        _defeatTween?.Kill();
        _defeatTween = null;
        _motion?.BindTarget(this);
        _animation?.ResetPresentation();
        if (Definition is not null) _animation?.BindReadability(Definition, team);
        _healthView?.SetHealth(currentHealth, maxHealth);
        LifecycleState = ContentLifecycleState.Bound;
    }

    public void Activate(UnitBindingContext context)
    {
        if (LifecycleState != ContentLifecycleState.Bound || string.IsNullOrWhiteSpace(RuntimeId))
            throw new System.InvalidOperationException("Unit must be bound before activation.");
        if (context is null) throw new System.ArgumentNullException(nameof(context));
        _context = context;
        IsActive = true;
        LifecycleState = ContentLifecycleState.Active;
        try
        {
            _context.Events.Publish(new SemanticBattleEvent(SemanticBattleEventType.Activated, RuntimeId, string.Empty, Team));
        }
        catch
        {
            IsActive = false;
            LifecycleState = ContentLifecycleState.Bound;
            _context = null;
            throw;
        }
    }

    public void Deactivate()
    {
        var context = _context;
        var wasActive = LifecycleState == ContentLifecycleState.Active;
        _motion?.ResetMotion();
        _animation?.ResetMovementPresentation();
        IsActive = false;
        if (wasActive) LifecycleState = ContentLifecycleState.Bound;
        _context = null;
        if (wasActive)
            context?.Events.Publish(new SemanticBattleEvent(SemanticBattleEventType.Deactivated, RuntimeId, string.Empty, Team));
    }

    public void RequestSelection()
    {
        if (!IsActive) return;
        EmitSignal(SignalName.ActorSelected, RuntimeId);
        _context?.Events.Publish(new SemanticBattleEvent(SemanticBattleEventType.Selected, RuntimeId, string.Empty, 0));
    }

    public void SnapPresentation(Vector2 worldPosition, float currentHealth, float maxHealth)
    {
        _animation?.ResetMovementPresentation();
        _motion?.SnapTo(worldPosition);
        _healthView?.SetHealth(currentHealth, maxHealth);
    }

    public void QueueMovement(Vector2 worldPosition) => _motion?.QueueWaypoint(worldPosition);

    public void RemapPresentationCoordinates(Vector2 oldOrigin, Vector2 oldPitch, Vector2 newOrigin, Vector2 newPitch) =>
        _motion?.RemapCoordinates(oldOrigin, oldPitch, newOrigin, newPitch);

    public void FaceToward(Vector2 worldPosition) => _animation?.FaceHorizontal(worldPosition.X - Position.X);

    public void RefreshPresentation(string cue, float currentHealth, float maxHealth)
    {
        _healthView?.SetHealth(currentHealth, maxHealth);
        if (cue == "defeated") _motion?.CancelForDefeat();
        if (cue == "idle" && _motion?.IsMoving == true) cue = "move";
        if (!string.IsNullOrWhiteSpace(cue)) _animation?.PlayCue(cue);
    }

    public void SetPresentationPaused(bool paused)
    {
        _motion?.SetPaused(paused);
        _animation?.SetPaused(paused);
        if (_defeatTween is null) return;
        if (paused) _defeatTween.Pause();
        else _defeatTween.Play();
    }

    public void SetPresentationSpeed(float speedScale) => _motion?.SetSpeedScale(speedScale);

    // Compatibility entry point for focused animation probes. Production composition uses the
    // explicit snap, queue, and non-positional refresh APIs above.
    public void ApplyPresentation(string cue, Vector2 worldPosition, float currentHealth, float maxHealth)
    {
        if (_motion?.HasPlacement != true) SnapPresentation(worldPosition, currentHealth, maxHealth);
        else if (cue == "move") QueueMovement(worldPosition);
        RefreshPresentation(cue, currentHealth, maxHealth);
    }

    private void OnMotionStateChanged(bool moving) => _animation?.PlayCue(moving ? "move" : "idle");

    private void OnHorizontalSegmentStarted(float horizontalDelta) => _animation?.FaceHorizontal(horizontalDelta);

    private void OnSegmentProgressChanged(float normalizedProgress) =>
        _animation?.SetMovementProgress(normalizedProgress);

    private void OnDefeatFadeRequested(float duration)
    {
        if (!Visible) return;
        _defeatTween?.Kill();
        _defeatTween = CreateTween();
        _defeatTween.TweenProperty(this, "modulate:a", 0f, duration);
        _defeatTween.TweenCallback(Callable.From(() => Visible = false));
    }

    public override void _ExitTree()
    {
        try
        {
            if (_animation is not null) _animation.DefeatFadeRequested -= OnDefeatFadeRequested;
            if (_motion is not null)
            {
                _motion.MotionStateChanged -= OnMotionStateChanged;
                _motion.HorizontalSegmentStarted -= OnHorizontalSegmentStarted;
                _motion.SegmentProgressChanged -= OnSegmentProgressChanged;
                _motion.ResetMotion();
            }
            _animation?.ResetMovementPresentation();
            _defeatTween?.Kill();
            Deactivate();
        }
        finally
        {
            IsActive = false;
            _context = null;
            RuntimeId = string.Empty;
            LifecycleState = ContentLifecycleState.Unbound;
        }
    }
}
