using System;
using System.Collections.Generic;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;

namespace TowerAutobattler.Presentation;

public static class BattlePresentationCueArbiter
{
    public static IReadOnlyDictionary<string, string> Select(IReadOnlyList<BattleEvent> events)
    {
        var selected = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var battleEvent in events)
        {
            var runtimeId = battleEvent.Type is "damage" or "defeated"
                ? battleEvent.TargetRuntimeId
                : battleEvent.SourceRuntimeId;
            if (string.IsNullOrWhiteSpace(runtimeId) || string.IsNullOrWhiteSpace(battleEvent.Cue)) continue;
            selected[runtimeId] = selected.TryGetValue(runtimeId, out var current)
                ? PresentationCuePolicy.Prefer(current, battleEvent.Cue)
                : battleEvent.Cue;
        }
        return selected;
    }
}
