using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Presentation;

// Owns transient cue nodes and animation lifetimes; callers supply only new facts and projections.
public partial class BattleFloatingCueLayer : Control
{
    public const int MaximumCueCount = 64;
    [Export] public PackedScene CueScene { get; set; } = null!;
    private readonly List<BattleFloatingCue> _cues = [];
    private long _sequence;
    public int ActiveCueCount => _cues.Count;
    public int ActiveTweenCount => _cues.Count(cue => cue.HasActiveTween);

    public override void _Ready()
    {
        if (CueScene is null)
            throw new InvalidOperationException("BattleFloatingCueLayer requires an authored cue scene.");
    }

    public override void _ExitTree() => Clear();

    public void Present(IReadOnlyList<BattleCombatEvent> combatEvents,
        IReadOnlyList<StatusPresentationCue> statusCues,
        Func<string, Vector2?> positionOf, Func<Vector2, Vector2> project)
    {
        var lanes = new Dictionary<string, int>(StringComparer.Ordinal);
        var combatFacts = combatEvents
            .Where(item => (item.Kind is BattleCombatEventKind.DamageResolved or
                            BattleCombatEventKind.HealingResolved) &&
                           item.EffectiveValue > 0 &&
                           !string.IsNullOrWhiteSpace(item.TargetRuntimeId))
            .GroupBy(item => (item.Tick, item.Kind, item.TargetRuntimeId))
            .Select(group => new
            {
                group.Key.Tick,
                group.Key.Kind,
                group.Key.TargetRuntimeId,
                Value = group.Sum(item => item.EffectiveValue),
                Position = group.OrderBy(item => item.Sequence).Last().Position,
                Sequence = group.Min(item => item.Sequence)
            })
            .OrderBy(item => item.Tick)
            .ThenBy(item => item.Sequence)
            .ToArray();
        foreach (var fact in combatFacts)
        {
            var kind = fact.Kind == BattleCombatEventKind.DamageResolved
                ? BattleFloatingCueKind.Damage
                : BattleFloatingCueKind.Healing;
            var sign = kind == BattleFloatingCueKind.Damage ? "-" : "+";
            SpawnFloatingCue(
                kind,
                sign + FormatCueValue(fact.Value),
                fact.TargetRuntimeId,
                fact.Tick,
                fact.Position,
                lanes, positionOf, project);
        }

        var onActive = statusCues
            .Where(cue => cue.Lifecycle == StatusPresentationCueLifecycle.OnActive)
            .Select(cue => (cue.Tick, cue.Status.OwnerId, cue.Status.StableId))
            .ToHashSet();
        var presented = new HashSet<(int Tick, string OwnerId, string StableId, BattleFloatingCueKind Kind)>();
        foreach (var cue in statusCues)
        {
            BattleFloatingCueKind? kind = cue.Lifecycle switch
            {
                StatusPresentationCueLifecycle.OnActive => BattleFloatingCueKind.StatusActive,
                StatusPresentationCueLifecycle.Executed when cue.Status.Stacks > 1 =>
                    BattleFloatingCueKind.StatusStack,
                StatusPresentationCueLifecycle.Executed when !onActive.Contains(
                    (cue.Tick, cue.Status.OwnerId, cue.Status.StableId)) => BattleFloatingCueKind.StatusActive,
                StatusPresentationCueLifecycle.Removed => BattleFloatingCueKind.StatusRemoved,
                _ => null
            };
            if (kind is null || !presented.Add(
                    (cue.Tick, cue.Status.OwnerId, cue.Status.StableId, kind.Value)))
                continue;
            var label = !string.IsNullOrWhiteSpace(cue.Status.ReportLabel)
                ? cue.Status.ReportLabel
                : !string.IsNullOrWhiteSpace(cue.Status.DisplayName)
                    ? cue.Status.DisplayName
                    : "状态";
            var text = kind.Value switch
            {
                BattleFloatingCueKind.StatusActive => label + " 生效",
                BattleFloatingCueKind.StatusStack => $"{label} ×{cue.Status.Stacks}",
                _ => label + " 消退"
            };
            SpawnFloatingCue(
                kind.Value,
                text,
                cue.Status.OwnerId,
                cue.Tick,
                default,
                lanes, positionOf, project);
        }
    }

    private void SpawnFloatingCue(
        BattleFloatingCueKind kind,
        string text,
        string targetRuntimeId,
        int tick,
        CombatPoint eventPosition,
        IDictionary<string, int> lanes,
        Func<string, Vector2?> positionOf, Func<Vector2, Vector2> project)
    {
        if (CueScene is null) return;
        while (_cues.Count >= MaximumCueCount)
            ReleaseFloatingCue(_cues[0], queueFree: false);

        lanes.TryGetValue(targetRuntimeId, out var sequence);
        lanes[targetRuntimeId] = sequence + 1;
        var lane = sequence % 4;
        var column = sequence / 4 % 3 - 1;
        var logicalPosition = eventPosition != default
            ? new Vector2(eventPosition.X, eventPosition.Y)
            : positionOf(targetRuntimeId) ?? Vector2.Zero;
        var cue = CueScene.Instantiate<BattleFloatingCue>();
        cue.Name = $"FloatingCue{++_sequence}";
        cue.Finished += OnFloatingCueFinished;
        AddChild(cue);
        _cues.Add(cue);
        cue.Play(kind, text, targetRuntimeId, tick, project(logicalPosition), lane, column);
    }

    private void OnFloatingCueFinished(BattleFloatingCue cue) =>
        ReleaseFloatingCue(cue, queueFree: true);

    public void Clear()
    {
        foreach (var cue in _cues.ToArray())
            ReleaseFloatingCue(cue, queueFree: false);
        _cues.Clear();
    }

    private void ReleaseFloatingCue(BattleFloatingCue cue, bool queueFree)
    {
        if (!_cues.Remove(cue)) return;
        cue.Finished -= OnFloatingCueFinished;
        cue.Stop();
        RemoveChild(cue);
        if (queueFree) cue.QueueFree();
        else cue.Free();
    }

    private static string FormatCueValue(float value) => value >= 10 || Math.Abs(value - MathF.Round(value)) < .01f
        ? MathF.Round(value).ToString("0")
        : value.ToString("0.#");
}
