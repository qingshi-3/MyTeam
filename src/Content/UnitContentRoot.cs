using Godot;
using TowerAutobattler.Components;
using System.Collections.Immutable;
using TowerAutobattler.Statuses;
using TowerAutobattler.Battle;
using TowerAutobattler.Audio;

namespace TowerAutobattler.Content;

[GlobalClass]
public partial class UnitContentRoot : Node2D
{
    [Signal] public delegate void ActorSelectedEventHandler(string runtimeId);
    public event System.Action<FeedbackSound>? AnimationSoundRequested;

    [Export] public UnitDefinition Definition { get; set; } = null!;

    public string RuntimeId { get; private set; } = string.Empty;
    public int Team { get; private set; }
    public bool IsActive { get; private set; }
    public ContentLifecycleState LifecycleState { get; private set; }
    public Vector2 BodyGlobalPosition => _animation?.BodyGlobalPosition ?? GlobalPosition;
    public TowerAutobattler.Vfx.VfxBodyVisual? CaptureBodyVisual(Transform2D worldToStage) =>
        _animation?.CaptureBodyVisual(worldToStage);
    public HeroRuleComponent? HeroRule => GetNodeOrNull<HeroRuleComponent>("HeroRuleComponent");
    public UnitBehaviorComponent? Behavior => GetNodeOrNull<UnitBehaviorComponent>("UnitBehaviorComponent");
    public AttackHitGrowthComponent? AttackHitGrowth => GetNodeOrNull<AttackHitGrowthComponent>("AttackHitGrowthComponent");
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
        if (_animation is not null)
        {
            _animation.DefeatFadeRequested += OnDefeatFadeRequested;
            _animation.AnimationSoundRequested += OnAnimationSoundRequested;
        }
        if (_motion is not null)
        {
            _motion.MotionStateChanged += OnMotionStateChanged;
            _motion.HorizontalSegmentStarted += OnHorizontalSegmentStarted;
            _motion.TravelWeightChanged += OnTravelWeightChanged;
            _motion.DisplacementPoseChanged += OnDisplacementPoseChanged;
            _motion.DisplacementElevationChanged += OnDisplacementElevationChanged;
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
        else report.Merge(GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent").ValidateFrameSounds());
        if (GetNodeOrNull<UnitMotionPresentationComponent>("UnitMotionPresentationComponent") is null)
            report.Error($"{SceneFilePath}: missing motion presentation component");
        if (GetNodeOrNull<HealthViewComponent>("HealthViewComponent") is null)
            report.Error($"{SceneFilePath}: missing health view component");
        if (Behavior is null) report.Error($"{SceneFilePath}: missing unit behavior component");
        if (AttackHitGrowth is not null) report.Merge(AttackHitGrowth.ValidateAuthoring());
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

    public void PresentDisplacement(BattleDisplacementCue cue, Vector2 position, Vector2 start,
        float arcHeightPixels, bool snap) =>
        _motion?.PresentDisplacement(cue, position, start, arcHeightPixels, snap);

    public void SnapSpatialPresentation(Vector2 worldPosition) => _motion?.SnapAuthorityPosition(worldPosition);

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
        _healthView?.SetPaused(paused);
        if (_defeatTween is null) return;
        if (paused) _defeatTween.Pause();
        else _defeatTween.Play();
    }

    public void RefreshCombatResources(float mana, float maximumMana, float shield,
        ImmutableArray<StatusRuntimeSnapshot> statuses, bool alive, TowerAutobattler.Battle.BattleSkillProgress? skill = null) =>
        _healthView?.SetCombatResources(mana, maximumMana, shield, statuses, alive, skill);

    public void RefreshBattleForm(float radius)
    {
        if (Definition is null) return;
        var visual = GetNodeOrNull<Node2D>("VisualRoot");
        if (visual is not null) visual.Scale = Vector2.One * (radius / Definition.BodyRadius);
    }

    public void PresentAbility(string name) => _healthView?.PresentAbility(name);
    public bool HasAnimationSounds(string cue) => _animation?.HasFrameSounds(cue) == true;
    private void OnAnimationSoundRequested(FeedbackSound sound)
    {
        if (IsActive) AnimationSoundRequested?.Invoke(sound);
    }
    public void BeginAttackPresentation(BattleAttackTiming timing) => _animation?.BeginTimedAttack(timing);
    public void CancelAttackPresentation() => _animation?.CancelTimedAttack();
    public void StepAttackPresentation(float seconds) => _animation?.StepTimedAttack(seconds);
    public void CompleteAttackWindup(float releaseProgress) => _animation?.CompleteTimedWindup(releaseProgress);

    public void SetPresentationSpeed(float speedScale, float simulationSpeed = 1)
    {
        _motion?.SetSpeedScale(speedScale);
        _motion?.SetSimulationSpeed(simulationSpeed);
        _animation?.SetCombatSpeed(simulationSpeed);
    }

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

    private void OnTravelWeightChanged(float normalizedWeight) =>
        _animation?.SetMovementWeight(normalizedWeight);

    private void OnDisplacementPoseChanged(string cue) => _animation?.SetDisplacementCue(cue);
    private void OnDisplacementElevationChanged(float pixels) => _animation?.SetDisplacementElevation(pixels);

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
            if (_animation is not null)
            {
                _animation.DefeatFadeRequested -= OnDefeatFadeRequested;
                _animation.AnimationSoundRequested -= OnAnimationSoundRequested;
            }
            if (_motion is not null)
            {
                _motion.MotionStateChanged -= OnMotionStateChanged;
                _motion.HorizontalSegmentStarted -= OnHorizontalSegmentStarted;
                _motion.TravelWeightChanged -= OnTravelWeightChanged;
                _motion.DisplacementPoseChanged -= OnDisplacementPoseChanged;
                _motion.DisplacementElevationChanged -= OnDisplacementElevationChanged;
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
