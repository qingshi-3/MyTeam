using System;
using System.Collections.Immutable;
using System.Linq;

namespace TowerAutobattler.UI;

// Text and typed attribute facts shared by hover and keyboard focus; no caller-authored markup.
public sealed record BattleLabTooltipInfo(
    string Title,
    string Subtitle = "",
    string Stats = "",
    string Abilities = "",
    string Loadout = "",
    string Hint = "",
    ImmutableArray<UnitAttributeFact> IconStats = default)
{
    public string PlainText => string.Join("\n\n", new[] { Title, Subtitle, Stats,
        IconStats.IsDefaultOrEmpty ? "" : string.Join("\n", IconStats.Select(fact => fact.Caption + " " + fact.Value)),
        Abilities, Loadout, Hint }
        .Where(value => !string.IsNullOrWhiteSpace(value)));
}
