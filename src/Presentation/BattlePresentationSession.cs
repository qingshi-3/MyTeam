using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.UI;
using TowerAutobattler.Audio;

namespace TowerAutobattler.Presentation;

internal sealed class BattlePresentationSession(
    BattlePlaybackSession _playback, BattleBoard _board, BattleEnvironment _environment, Node2D _unitsRoot,
    RangedAttackLayer _rangedAttackLayer, FeedbackAudio _audio, BattleFloatingCueLayer _floatingCueLayer,
    Func<CatalogEntry, UnitContentRoot> _factory, Action<string> _selected)
{
    private readonly Dictionary<string, UnitContentRoot> _presenters = new(StringComparer.Ordinal);
    private ContentRegistry? _content;
    private int _combatCueCursor, _statusCueCursor;
    private BattleSimulation? _simulation => _playback.Simulation;
    private bool _paused => _playback.Paused;
    private float _speedScale => _playback.Speed;
    private const float NormalSimulationScale = BattlePlaybackSession.NormalSimulationScale;
    public IEnumerable<UnitContentRoot> Actors => _presenters.Values;
    private void OnActorSelected(string id) => _selected(id);
    public void Bind(ContentRegistry content) => _content = content;
    public void BindRangedPositions()
    {
        _rangedAttackLayer.BindUnitPositions(id => _presenters.TryGetValue(id, out var unit) ? unit.Position : null);
        _rangedAttackLayer.BindUnitBodyPositions(id => _presenters.TryGetValue(id, out var unit)
            ? _unitsRoot.ToLocal(unit.BodyGlobalPosition) : null);
        _rangedAttackLayer.BindUnitBodyVisuals(id => _presenters.TryGetValue(id, out var unit)
            ? unit.CaptureBodyVisual(_rangedAttackLayer.GlobalTransform.AffineInverse()) : null);
    }
    public void HoldTerminalPositions()
    {
        if (_simulation is null) return;
        foreach (var state in _simulation.UnitViews)
            if (_presenters.TryGetValue(state.RuntimeId, out var presenter))
                presenter.SnapPresentation(_board.LogicalToLocal(state.Position), state.Health, state.MaxHealth);
    }
    public void Clear()
    {
        _audio.Clear();
        _rangedAttackLayer.Clear();
        _floatingCueLayer.Clear();
        foreach (var presenter in _presenters.Values)
        {
            presenter.ActorSelected -= OnActorSelected;
            presenter.AnimationSoundRequested -= OnAnimationSoundRequested;
            presenter.Deactivate();
            presenter.GetParent()?.RemoveChild(presenter);
            presenter.Free();
        }
        _presenters.Clear();
        _content = null;
        _combatCueCursor = 0;
        _statusCueCursor = 0;
        _environment.ResetClock();
    }
    public void OnUnitHitPresented(string runtimeId)
    {
        if (_presenters.TryGetValue(runtimeId, out var actor)) actor.PresentHitFeedback();
    }

    public void PresentEvents(IReadOnlyList<BattleEvent> events)
    {
        foreach (var change in events.Where(e => e.Type is "revived" or "allegiance"))
        {
            var id = change.TargetRuntimeId;
            if (_presenters.Remove(id,out var previous))
            {
                previous.ActorSelected -= OnActorSelected;
                previous.AnimationSoundRequested -= OnAnimationSoundRequested;
                previous.Deactivate(); previous.GetParent()?.RemoveChild(previous); previous.Free();
            }
            EnsurePresenter(id);
        }
        BattleAudioFeedback.Present(_audio, events,
            id => FindState(id)?.AttackDelivery ?? AttackDelivery.Melee,
            (id, cue) =>
            {
                EnsurePresenter(id);
                return _presenters.TryGetValue(id, out var actor) && actor.HasAnimationSounds(cue);
            });
        PresentDisplacements(events);
        var displacedOwners = events.Where(fact => fact.Displacement is not null)
            .Select(fact => fact.TargetRuntimeId).ToHashSet(StringComparer.Ordinal);
        var timedAttackOwners = new HashSet<string>(StringComparer.Ordinal);
        foreach (var fact in events)
        {
            var released = fact.Type is "attack" or "skill_arrow" && FindState(fact.SourceRuntimeId)?.Definition.ProjectileWindupSeconds > 0;
            if (fact.AttackTiming is null && fact.Type is not ("attack_cancel" or "line_cancel" or "line_release" or "blade_release") && !released) continue;
            EnsurePresenter(fact.SourceRuntimeId);
            if (!_presenters.TryGetValue(fact.SourceRuntimeId, out var actor)) continue;
            if (fact.AttackTiming is { } timing)
            {
                actor.FaceToward(_board.LogicalToLocal(fact.Position));
                actor.BeginAttackPresentation(timing, fact.Line?.ReleaseVfx ?? "attack");
                timedAttackOwners.Add(fact.SourceRuntimeId);
            }
            else if (fact.Type == "line_release") actor.CompleteAttackWindup(fact.Line?.ReleaseProgress ?? .8f);
            else if (fact.Type == "blade_release") actor.CompleteAttackWindup(FindState(fact.SourceRuntimeId)!.Definition.AttackReleaseProgress);
            else if (released) actor.CompleteAttackWindup(FindState(fact.SourceRuntimeId)!.Definition.AttackReleaseProgress);
            else actor.CancelAttackPresentation();
        }
        foreach(var group in events.Where(e=>e.Type=="wall_raised").GroupBy(e=>e.SourceRuntimeId))
        {
            var walls=group.ToArray();
            var tangent=walls.Length>1?(walls[^1].Position-walls[0].Position).Normalized():Vector2.Down;
            var forward=new Vector2(tangent.Y,-tangent.X);
            for(int i=0;i<walls.Length;i++)
            {
                var fact=walls[i];EnsurePresenter(fact.TargetRuntimeId);
                if(!_presenters.TryGetValue(fact.TargetRuntimeId,out var actor) || actor.BarrierVisual is null)continue;
                var foot=_board.LogicalToLocal(fact.Position);
                actor.BarrierVisual.Begin((_board.LogicalToLocal(fact.Position+tangent)-foot)/actor.Scale.X,
                    (_board.LogicalToLocal(fact.Position+forward)-foot)/actor.Scale.X,i-(walls.Length-1)*.5f,_paused);
            }
        }
        _rangedAttackLayer.Present(events, _paused);
        if (_simulation is not null && !events.Any(e => e.Type == "battle_finished"))
            _rangedAttackLayer.SynchronizeUnits(_simulation.UnitViews, _simulation.FrontalGuards, _simulation.GroundStorms);
        foreach (var battleEvent in events)
        {
            if (battleEvent.Type != "move" || string.IsNullOrWhiteSpace(battleEvent.SourceRuntimeId) ||
                displacedOwners.Contains(battleEvent.SourceRuntimeId)) continue;
            EnsurePresenter(battleEvent.SourceRuntimeId);
            if (_presenters.TryGetValue(battleEvent.SourceRuntimeId, out var mover))
            {
                var logicalPosition = battleEvent.Position != default
                    ? battleEvent.Position
                    : BattlefieldSpace.CellCenter(battleEvent.Cell);
                mover.QueueMovement(_board.LogicalToLocal(logicalPosition));
            }
        }

        foreach (var battleEvent in events.Where(battleEvent => battleEvent.Type is "attack" or "skill_arrow" or "heal" or "ability"))
        {
            EnsurePresenter(battleEvent.SourceRuntimeId);
            EnsurePresenter(battleEvent.TargetRuntimeId);
            if (_presenters.TryGetValue(battleEvent.SourceRuntimeId, out var source) &&
                _presenters.TryGetValue(battleEvent.TargetRuntimeId, out var target))
                source.FaceToward(target.Position);
            if (battleEvent.Type == "ability" && _presenters.TryGetValue(battleEvent.SourceRuntimeId, out var caster))
                caster.PresentAbility(FindState(battleEvent.SourceRuntimeId)?.LastAbilityName ?? "技能");
        }
        foreach (var fact in events.Where(fact => fact.Type == "line_charge"))
            if (_presenters.TryGetValue(fact.SourceRuntimeId, out var caster))
                caster.PresentAbility(FindState(fact.SourceRuntimeId)?.LastAbilityName ?? "蓄力");

        var presented = new HashSet<string>(StringComparer.Ordinal);
        foreach (var selection in BattlePresentationCueArbiter.Select(events))
        {
            var presentationId = selection.Key;
            EnsurePresenter(presentationId);
            if (_presenters.TryGetValue(presentationId, out var presenter))
            {
                var state = FindState(presentationId);
                if (state is not null)
                {
                    presenter.RefreshPresentation(state.Alive && timedAttackOwners.Contains(presentationId) ? "" : selection.Value, state.Health, state.MaxHealth);
                    presented.Add(presentationId);
                }
            }
        }
        SyncPresenters("idle", presented);
    }

    public void PresentResolvedFacts()
    {
        if (_simulation is null) return;
        var combatStart = Math.Min(_combatCueCursor, _simulation.CombatEvents.Count);
        var combatFacts = _simulation.CombatEvents.Skip(combatStart).ToArray();
        var statusStart = Math.Min(_statusCueCursor, _simulation.StatusPresentationCues.Count);
        var statusCues = _simulation.StatusPresentationCues.Skip(statusStart).ToArray();
        foreach (var fact in combatFacts)
        {
            _audio.Play(BattleAudioFeedback.CombatCue(fact,
                FindState(fact.SourceRuntimeId)?.AttackDelivery ?? AttackDelivery.Melee));
            if (fact.Kind == BattleCombatEventKind.AbilityResolved &&
                !(_presenters.TryGetValue(fact.SourceRuntimeId, out var caster) && caster.HasAnimationSounds("skill_cast")))
                _audio.Play(_audio.AbilityCue(fact.SubjectStableId));
        }
        foreach (var cue in statusCues) _audio.Play(BattleAudioFeedback.StatusCue(cue));
        _floatingCueLayer.Present(combatFacts, statusCues,
            id => FindState(id)?.Position, _board.LogicalToLocal);
        _combatCueCursor = _simulation.CombatEvents.Count;
        _statusCueCursor = _simulation.StatusPresentationCues.Count;
    }

    private void ClearFloatingCues() => _floatingCueLayer?.Clear();

    public void SyncPresenters(string cue, IReadOnlySet<string>? preserveCue = null)
    {
        if (_simulation is null) return;
        var guards = _simulation.FrontalGuards.ToDictionary(value => value.OwnerRuntimeId, StringComparer.Ordinal);
        foreach (var state in _simulation.UnitViews)
        {
            EnsurePresenter(state.RuntimeId);
            if (_presenters.TryGetValue(state.RuntimeId, out var presenter))
            {
                presenter.RefreshPresentation(state.Alive && preserveCue?.Contains(state.RuntimeId) == true ? "" : state.Alive ? cue : "defeated",
                    state.Health, state.MaxHealth);
                presenter.RefreshBattleForm(state.BodyRadius);
                if (state.Alive && guards.TryGetValue(state.RuntimeId, out var guard))
                    presenter.FaceToward(_board.LogicalToLocal(state.Position + guard.Direction));
                presenter.RefreshCombatResources(state.CurrentMana, state.MaxMana, state.Shield, state.Statuses, state.Alive,
                    _simulation.ReadUnitSkills(state.RuntimeId).Primary);
            }
        }
    }

    private void EnsurePresenter(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) || _presenters.ContainsKey(runtimeId) || _content is null) return;
        var state = FindState(runtimeId);
        if (state is null || !_content.TryGet(state.Definition.ContentId, out var entry)) return;
        UnitContentRoot? presenter = null;
        var subscribed = false;
        try
        {
            presenter = _factory(entry);
            _unitsRoot.AddChild(presenter);
            presenter.Bind(state.RuntimeId, state.Team, state.Health, state.MaxHealth);
            presenter.ActorSelected += OnActorSelected;
            presenter.AnimationSoundRequested += OnAnimationSoundRequested;
            subscribed = true;
            presenter.Activate(new UnitBindingContext(
                new DeterministicRandom(1), NullEvents.Instance, NullCommands.Instance));
            presenter.SetPresentationSpeed(_speedScale, _speedScale * NormalSimulationScale);
            presenter.SetPresentationPaused(_paused);
            presenter.Scale = Vector2.One * _board.CurrentProjection.UnitScale;
            presenter.SnapPresentation(_board.LogicalToLocal(state.Position), state.Health, state.MaxHealth);
            presenter.Readability?.BindEnvironmentLighting();
            presenter.Readability?.SetEnvironmentLighting(_environment.CanvasToTexture(presenter.GlobalPosition), _environment.ElapsedSeconds);
            _presenters.Add(runtimeId, presenter);
        }
        catch
        {
            if (presenter is not null)
            {
                if (subscribed)
                {
                    presenter.ActorSelected -= OnActorSelected;
                    presenter.AnimationSoundRequested -= OnAnimationSoundRequested;
                }
                try { presenter.Deactivate(); }
                catch { }
                presenter.GetParent()?.RemoveChild(presenter);
                presenter.Free();
            }
            throw;
        }
    }

    public BattleUnitView? FindState(string runtimeId) => _simulation?.UnitViews.FirstOrDefault(unit => unit.RuntimeId == runtimeId);
    private void OnAnimationSoundRequested(FeedbackSound sound)
    {
        if (!_playback.Ending) _audio.Play(sound);
    }

    public void SnapPresentersToAuthority()
    {
        if (_simulation is null) return;
        var guards = _simulation.FrontalGuards.ToDictionary(value => value.OwnerRuntimeId, StringComparer.Ordinal);
        foreach (var state in _simulation.UnitViews)
        {
            EnsurePresenter(state.RuntimeId);
            if (_presenters.TryGetValue(state.RuntimeId, out var presenter))
                presenter.SnapSpatialPresentation(_board.LogicalToLocal(state.Position));
        }
    }

    public void RefreshEnvironmentLighting()
    {
        _environment.RefreshLayout();
        foreach (var presenter in _presenters.Values)
            presenter.Readability?.SetEnvironmentLighting(_environment.CanvasToTexture(presenter.GlobalPosition), _environment.ElapsedSeconds);
    }

    // Called after same-identity presenter replacement and before the normal move queue.
    // The payload supplies the path; no target lookup is allowed to retarget a visible jump.
    private void PresentDisplacements(IReadOnlyList<BattleEvent> events)
    {
        foreach (var fact in events)
        {
            if (fact.Displacement is not { } displacement || string.IsNullOrWhiteSpace(fact.TargetRuntimeId))
                continue;
            EnsurePresenter(fact.TargetRuntimeId);
            if (!_presenters.TryGetValue(fact.TargetRuntimeId, out var presenter)) continue;
            presenter.PresentDisplacement(displacement,
                _board.LogicalToLocal(fact.Position),
                _board.LogicalToLocal(displacement.Start),
                // Height is sprite-local; the content root already has projection scale.
                displacement.ArcHeight * 64f,
                _paused);
            _rangedAttackLayer.PresentDisplacement(fact, _paused);
        }
    }    private sealed class NullEvents : ISemanticBattleEventSink
    {
        public static readonly NullEvents Instance = new();
        public void Publish(SemanticBattleEvent battleEvent) { }
    }

    private sealed class NullCommands : IBattleCommandGateway
    {
        public static readonly NullCommands Instance = new();
        public bool Submit(BattleCommandRequest command) => false;
    }}
