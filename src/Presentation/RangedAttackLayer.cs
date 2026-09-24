using System.Collections.Generic;
using System;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Vfx;
using TowerAutobattler.Domain;
using TowerAutobattler.Statuses;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Presentation;

// Compatibility facade; all effect instances belong to the shared player.
public partial class RangedAttackLayer : Node2D, IVfxStage
{
    [Export] public Vector2 BodyOffset { get; set; } = new(0, -22);
    private VfxPlayer _player = null!;
    private BattleBoard? _board;
    private readonly Dictionary<int, ProjectileFlight> _projectiles = [];
    private bool _paused;
    private float _speed = 1;
    private readonly HashSet<string> _shields = [];
    private readonly Dictionary<string, VfxContext> _shieldContexts = [];
    private readonly Dictionary<string, (string OwnerId, VfxContext Context)> _castContexts = [];
    private readonly Dictionary<string, (string OwnerId, bool Ground, VfxContext Context)> _statusVfx = [];
    private Func<string, Vector2?>? _displayPosition;
    private Func<string, Vector2?>? _bodyDisplayPosition;
    private Func<string, VfxBodyVisual?>? _bodyVisual;
    private readonly Dictionary<string, HashSet<string>> _gatherMotions = [];
    private readonly List<DisplacementImpact> _displacementImpacts = [];
    private readonly Dictionary<string, (string Key, VfxContext Context)> _trampleRush = [];
    public int StatusVfxCount => _statusVfx.Count;
    public int ProjectileCount => _projectiles.Count;
    public int CastVfxCount => _castContexts.Count;
    public float UnitScale => _board?.CurrentProjection.UnitScale ?? 1;
    public Vector2 Project(Vector2 point, bool ground) =>
        (_board?.LogicalToLocal(point) ?? point) + (ground ? Vector2.Zero : BodyOffset * UnitScale);
    public float RadiusPixels(float radius) => _board is null ? radius :
        _board.LogicalToLocal(new Vector2(radius, 0)).DistanceTo(_board.LogicalToLocal(Vector2.Zero));
    public override void _Ready() => _player = GetNode<VfxPlayer>("Player");
    public void Bind(BattleBoard board) { _board = board; _player.Bind(this); }
    public void BindUnitPositions(Func<string, Vector2?> displayPosition) => _displayPosition = displayPosition;
    public void BindUnitBodyPositions(Func<string, Vector2?> bodyDisplayPosition) => _bodyDisplayPosition = bodyDisplayPosition;
    public void BindUnitBodyVisuals(Func<string, VfxBodyVisual?> bodyVisual) => _bodyVisual = bodyVisual;
    public void SetClock(bool paused, float speed)
    {
        _paused = paused;
        _speed = Math.Max(0, speed);
        _player.Paused = paused;
        _player.Speed = speed;
    }
    public override void _Process(double delta)
    {
        var seconds = _paused ? 0 : (float)delta * _speed;
        AdvanceFlights(seconds);
        AdvanceEnemyVisuals(seconds);
        AdvanceTechniqueVisuals(seconds);
        AdvanceDisplacementImpacts(seconds);
        foreach (var (owner, rush) in _trampleRush)
            _player.UpdateContext(rush.Key, WithDisplayPosition(owner, rush.Context, true));
        foreach (var (ownerId, context) in _shieldContexts)
            _player.UpdateContext("shield:" + ownerId, WithDisplayPosition(ownerId, context));
        foreach (var (key, status) in _statusVfx)
            _player.UpdateContext(key, WithDisplayPosition(status.OwnerId, status.Context, status.Ground));
        foreach (var (key, cast) in _castContexts.ToArray())
        {
            if (_player.PlaybackFor(key) is null) _castContexts.Remove(key);
            else _player.UpdateContext(key, CastContext(cast.OwnerId, cast.Context));
        }
    }

    private VfxContext CastContext(string ownerId, VfxContext context) =>
        WithDisplayPosition(ownerId, context) with { Body = _bodyVisual?.Invoke(ownerId) };

    private VfxContext WithDisplayPosition(string ownerId, VfxContext context, bool ground = false) => context with
    {
        // Ground effects retain the feet/selection anchor; body effects follow the
        // independently animated sprite, including leap elevation and ordinary travel.
        TargetDisplayPosition = ground ? _displayPosition?.Invoke(ownerId) :
            _bodyDisplayPosition?.Invoke(ownerId) ?? (_displayPosition?.Invoke(ownerId) is { } position
                ? position + BodyOffset * UnitScale : null)
    };

    // Kind selects only a shared visual action. Targets, center, radius and result
    // remain supplied by the simulation; no hero id or gameplay query belongs here.
    public void PresentDisplacement(BattleEvent fact, bool snap)
    {
        if (fact.Displacement is not { } motion) return;
        var started = motion.Progress <= 0f && !motion.Finished && !motion.Cancelled;
        if (motion.Kind == DisplacementKind.Blink)
        {
            var key = $"displacement:blink:{fact.TargetRuntimeId}:{motion.StartTick}";
            var context = new VfxContext(motion.Start, motion.End, Playback: new VfxPlaybackParameters(
                CastSpeed: Mathf.Clamp(_player.Catalog.Find("teleport").CastDuration / Math.Max(BattleTiming.TickSeconds,
                    motion.DurationTicks * BattleTiming.TickSeconds), .1f, 8f), Timing: VfxTimingMode.Events));
            if (started) _player.Play("teleport", context, key);
            if (motion.Cancelled) _player.End(key, VfxEndReason.ScopeEnded);
            else if (motion.Finished)
            {
                _player.UpdateContext(key, context with { Target = fact.Position });
                _player.Signal(key, VfxStartCue.Fired);
                // Teleport has no flying body, but the shared event player still needs
                // completion to release its independently authored endpoint aftermath.
                _player.Signal(key, VfxStartCue.Impact);
            }
            return;
        }
        if (motion.Kind == DisplacementKind.Gather)
        {
            var key = $"displacement:gather:{fact.SourceRuntimeId}:{motion.StartTick}";
            if (started)
            {
                if (!_gatherMotions.TryGetValue(key, out var members))
                {
                    members = new HashSet<string>(StringComparer.Ordinal);
                    _gatherMotions.Add(key, members);
                    _player.Play("wind_vortex", new(motion.EffectCenter, motion.EffectCenter, motion.EffectRadius), key);
                }
                members.Add(fact.TargetRuntimeId);
            }
            if (motion.Finished && _gatherMotions.TryGetValue(key, out var pending))
            {
                pending.Remove(fact.TargetRuntimeId);
                if (pending.Count == 0)
                {
                    _gatherMotions.Remove(key);
                    _player.End(key, VfxEndReason.Completed);
                }
            }
            return;
        }
        if (motion.Finished && !motion.Cancelled &&
            motion.Kind is DisplacementKind.Charge or DisplacementKind.Leap or DisplacementKind.Knockback)
        {
            // Ground travel deliberately retains one committed sample for smoothness.
            // Keep the contact flash with the visible arrival, including its last sample.
            var context = new VfxContext(fact.Position, fact.Position);
            if (snap) _player.Play("impact", context);
            else _displacementImpacts.Add(new(context, BattleTiming.TickSeconds));
        }
    }

    private void AdvanceDisplacementImpacts(float seconds)
    {
        for (var index = _displacementImpacts.Count - 1; index >= 0; index--)
        {
            var pending = _displacementImpacts[index];
            pending = pending with { RemainingSeconds = Math.Max(0f, pending.RemainingSeconds - seconds) };
            if (pending.RemainingSeconds > 0f) { _displacementImpacts[index] = pending; continue; }
            _displacementImpacts.RemoveAt(index);
            _player.Play("impact", pending.Context);
        }
    }

    public void AdvanceFlights(float simulationSeconds)
    {
        foreach (var (id, flight) in _projectiles.ToArray())
        {
            // Let the terminal point reach a rendered frame before removing it.
            // A paused flight also retains that final frame until time resumes.
            if (flight.Ending && flight.Elapsed >= BattleTiming.TickSeconds && simulationSeconds > 0)
            {
                _projectiles.Remove(id);
                _player.End("projectile:" + id, VfxEndReason.ScopeEnded);
                continue;
            }
            flight.Elapsed = Math.Min(BattleTiming.TickSeconds, flight.Elapsed + Math.Max(0, simulationSeconds));
            flight.Rendered = flight.From.Lerp(flight.To, flight.Elapsed / BattleTiming.TickSeconds);
            _player.UpdateContext("projectile:" + id, new(flight.From, flight.Rendered, Direction: flight.Direction));
            foreach (var hit in flight.Impacts.ToArray())
            {
                if ((flight.Rendered - hit.Contact).Dot(flight.Direction) < -.0001f) continue;
                _player.Play("impact", WithDisplayPosition(hit.TargetId, hit.Context));
                flight.Impacts.Remove(hit);
            }
        }
    }
    public void SynchronizeUnits(IEnumerable<BattleUnitState> units)
    {
        var snapshot = units.ToArray();
        var active = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in snapshot)
        {
            var key = "shield:" + unit.RuntimeId;
            if (unit.Alive && unit.Shield > 0)
            {
                _shields.Add(unit.RuntimeId);
                var context = new VfxContext(unit.Position, unit.Position);
                _shieldContexts[unit.RuntimeId] = context;
                _player.Play("shield", WithDisplayPosition(unit.RuntimeId, context), key);
            }
            else if (_shields.Remove(unit.RuntimeId))
            {
                _shieldContexts.Remove(unit.RuntimeId);
                _player.End(key, unit.Alive ? VfxEndReason.Depleted : VfxEndReason.OwnerDefeated);
            }

            if (!unit.Alive) continue;
            foreach (var status in unit.Statuses.Where(status => !string.IsNullOrWhiteSpace(status.PersistentVfx)))
            {
                // A taunt ceases to force targeting when its source is unavailable. Other
                // statuses retain their own authored lifetime, including source-independent ones.
                if (status.Behavior == StatusBehaviorKind.Taunt && !snapshot.Any(source =>
                        source.RuntimeId == status.SourceId && source.Alive && source.Team != unit.Team)) continue;
                var statusKey = $"status:{unit.RuntimeId}:{status.PersistentVfx}";
                if (!active.Add(statusKey)) continue;
                var context = new VfxContext(unit.Position, unit.Position);
                var ground = _player.Catalog.Find(status.PersistentVfx).Ground;
                _statusVfx[statusKey] = (unit.RuntimeId, ground, context);
                _player.Play(status.PersistentVfx, WithDisplayPosition(unit.RuntimeId, context, ground), statusKey);
            }
        }
        foreach (var key in _statusVfx.Keys.Where(key => !active.Contains(key)).ToArray())
        {
            _statusVfx.Remove(key);
            _player.End(key, VfxEndReason.ScopeEnded);
        }
    }
    public void Present(IReadOnlyList<BattleEvent> events, bool snap)
    {
        foreach (var fact in events)
        {
            PresentTechnique(fact, snap);
            PresentEnemyAction(fact,snap);
            var context = new VfxContext(fact.Origin, fact.Position, fact.Vfx?.Radius ?? 0);
            var projectile = "projectile:" + fact.EntityId;
            var shield = "shield:" + fact.TargetRuntimeId;
            if (fact.Vfx is { } cue)
            {
                if (cue.Phase == BattleVfxPhase.ShieldActive)
                {
                    _shields.Add(fact.TargetRuntimeId);
                    _shieldContexts[fact.TargetRuntimeId] = context;
                    context = WithDisplayPosition(fact.TargetRuntimeId, context);
                }
                if (cue.Phase == BattleVfxPhase.ShieldDepleted)
                {
                    _shields.Remove(fact.TargetRuntimeId);
                    _shieldContexts.Remove(fact.TargetRuntimeId);
                }
                VfxBindingResolver.Present(_player, cue, context, fact.TargetRuntimeId);
            }
            switch (fact.Type)
            {
                case "trample_charge" when fact.Trample is { } warning:
                    _player.Play(warning.WarningVfx, new(warning.Start, warning.End, warning.Radius,
                        new VfxPlaybackParameters(CastSpeed: 1.2f / (warning.ChargeTicks * BattleTiming.TickSeconds),
                            Timing: VfxTimingMode.Events)), $"trample:{fact.SourceRuntimeId}:{warning.StartTick}");
                    break;
                case "trample_rush" when fact.Trample is { } rush:
                    var warningKey = $"trample:{fact.SourceRuntimeId}:{rush.StartTick}";
                    _player.End(warningKey, VfxEndReason.ScopeEnded);
                    var rushKey = warningKey + ":rush";
                    var rushContext = new VfxContext(fact.Position, rush.End, rush.Radius,
                        Direction: (rush.End - rush.Start).Normalized());
                    _trampleRush[fact.SourceRuntimeId] = (rushKey, rushContext);
                    _player.Play(rush.RushVfx, WithDisplayPosition(fact.SourceRuntimeId, rushContext, true), rushKey);
                    break;
                case "trample_end" when fact.Trample is { } ended:
                    _player.End($"trample:{fact.SourceRuntimeId}:{ended.StartTick}", VfxEndReason.ScopeEnded);
                    if (_trampleRush.Remove(fact.SourceRuntimeId, out var stopped))
                        _player.End(stopped.Key, VfxEndReason.ScopeEnded);
                    break;
                case "line_charge" when fact.Line is { } charge:
                    var chargeKey = $"line:{fact.SourceRuntimeId}:{charge.StartTick}";
                    _player.Play(charge.ChargeVfx, new(fact.Origin, fact.Position, charge.Radius,
                        new VfxPlaybackParameters(CastSpeed: _player.Catalog.Find(charge.ChargeVfx).CastDuration / (charge.ChargeTicks * BattleTiming.TickSeconds),
                            Timing: VfxTimingMode.Events)), chargeKey);
                    break;
                case "line_cancel" when fact.Line is { } cancelled:
                    _player.End($"line:{fact.SourceRuntimeId}:{cancelled.StartTick}", VfxEndReason.ScopeEnded);
                    break;
                case "line_release" when fact.Line is { } released:
                    _player.End($"line:{fact.SourceRuntimeId}:{released.StartTick}", VfxEndReason.ScopeEnded);
                    if (released.Delivery == ChargedLineDelivery.Beam)
                        _player.Play(released.ReleaseVfx, new(fact.Origin, fact.Position, released.Radius));
                    break;
                case "line_impact": _player.Play("impact", WithDisplayPosition(fact.TargetRuntimeId, context)); break;
                case "ability":
                    // The successful mana-cast fact is emitted once per cast, even for
                    // multiple recipients. Basic heals and triggered passives do not emit it.
                    var castKey = $"cast:{fact.SourceRuntimeId}:{fact.Tick}";
                    var castContext = new VfxContext(fact.Position, fact.Position);
                    _castContexts[castKey] = (fact.SourceRuntimeId, castContext);
                    _player.Play(string.IsNullOrWhiteSpace(fact.SourceVfx) ? "spell_cast" : fact.SourceVfx,
                        CastContext(fact.SourceRuntimeId, castContext), castKey);
                    break;
                case "projectile_spawn":
                    // Spawn supplies a point ahead of the projectile; later move facts only
                    // carry position. Preserve the launch heading until this entity ends.
                    var direction = (fact.Origin - fact.Position).Normalized();
                    _projectiles[fact.EntityId] = new(fact.Position, direction,
                        finishSegmentOnEnd: fact.Line?.Delivery == ChargedLineDelivery.Projectile);
                    _player.Play(fact.Line?.ReleaseVfx ?? (string.IsNullOrEmpty(fact.SourceVfx) ? "projectile" : fact.SourceVfx), context with { Direction = direction }, projectile); break;
                case "projectile_move":
                    if (_projectiles.TryGetValue(fact.EntityId, out var flight))
                    {
                        flight.From = flight.Rendered;
                        flight.To = fact.Position;
                        flight.Elapsed = snap ? BattleTiming.TickSeconds : 0;
                        if (snap) flight.Rendered = flight.To;
                        _player.UpdateContext(projectile, context with { Target = flight.Rendered, Direction = flight.Direction });
                    }
                    break;
                case "projectile_impact":
                    // Piercing flight is interpolated behind fixed-step facts. Show
                    // the scratch when its visible front reaches the contact point.
                    if (!snap && _projectiles.TryGetValue(fact.EntityId, out var hitFlight) && hitFlight.FinishSegmentOnEnd)
                        hitFlight.Impacts.Add(new(fact.Position, fact.TargetRuntimeId, context));
                    else _player.Play("impact", WithDisplayPosition(fact.TargetRuntimeId, context));
                    break;
                case "projectile_end":
                    if (!snap && _projectiles.TryGetValue(fact.EntityId, out var ending) && ending.FinishSegmentOnEnd)
                    {
                        ending.From = ending.Rendered;
                        ending.To = fact.Position;
                        ending.Elapsed = 0;
                        ending.Ending = true;
                    }
                    else
                    {
                        _projectiles.Remove(fact.EntityId);
                        _player.End(projectile, VfxEndReason.ScopeEnded);
                    }
                    break;
                case "beam":
                    _player.Play("beam", context);
                    _player.Play("impact", WithDisplayPosition(fact.TargetRuntimeId, context)); break;
                case "heal": _player.Play("heal", context); break;
                case "defeated":
                    foreach (var key in _castContexts.Where(pair => pair.Value.OwnerId == fact.TargetRuntimeId).Select(pair => pair.Key).ToArray())
                    {
                        _castContexts.Remove(key);
                        _player.End(key, VfxEndReason.ScopeEnded);
                    }
                    _shields.Remove(fact.TargetRuntimeId);
                    _shieldContexts.Remove(fact.TargetRuntimeId);
                    _player.End(shield, VfxEndReason.OwnerDefeated); break;
                case "battle_finished": Clear(); break;
            }
        }
        if (snap)
        {
            AdvanceTechniqueVisuals(BattleTiming.TickSeconds);
            AdvanceEnemyVisuals(BattleTiming.TickSeconds);
            AdvanceDisplacementImpacts(BattleTiming.TickSeconds);
            _player.Advance(BattleTiming.TickSeconds);
        }
    }
    public void Clear()
    {
        _player?.Clear();
        ClearTechniqueVisuals();
        ClearEnemyVisuals();
        _projectiles.Clear();
        _shields.Clear();
        _shieldContexts.Clear();
        _castContexts.Clear();
        _trampleRush.Clear();
        _statusVfx.Clear();
        _gatherMotions.Clear();
        _displacementImpacts.Clear();
    }

    private sealed class ProjectileFlight(Vector2 position, Vector2 direction, bool finishSegmentOnEnd = false)
    {
        public Vector2 From = position, To = position, Rendered = position;
        public readonly Vector2 Direction = direction;
        public readonly bool FinishSegmentOnEnd = finishSegmentOnEnd;
        public bool Ending;
        public readonly List<ProjectileImpact> Impacts = [];
        public float Elapsed;
    }

    private sealed record DisplacementImpact(VfxContext Context, float RemainingSeconds);
    private sealed record ProjectileImpact(Vector2 Contact, string TargetId, VfxContext Context);
}

