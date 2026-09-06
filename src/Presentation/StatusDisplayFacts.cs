using System;
using System.Linq;
using TowerAutobattler.Statuses;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Presentation;

// Presentation meanings, never equipment or status-id dispatch. Unknown authored meanings
// retain a disposition icon and their Chinese name rather than losing the status entirely.
public static class StatusDisplayFacts
{
    public static string IconKey(StatusRuntimeSnapshot status) => string.IsNullOrWhiteSpace(status.SemanticIcon)
        ? status.Disposition == StatusDisposition.Harmful ? "risk" : "shield"
        : status.SemanticIcon;

    public static bool Frozen(StatusRuntimeSnapshot status) =>
        status.GrantedTags.Contains("state.frozen", StringComparer.Ordinal);

    public static bool DisablesActions(StatusRuntimeSnapshot status) =>
        status.GrantedTags.Contains(StatusDefinitionCompiler.ActionDisabledTag, StringComparer.Ordinal);

    public static string Duration(StatusRuntimeSnapshot status) => status.Permanent
        ? "本场" : $"{status.RemainingTicks * BattleTiming.TickSeconds:0.0}s";
}
