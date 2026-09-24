using System;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

namespace TowerAutobattler.App;

// Owns screen visibility, army-overlay eligibility, and focus handoff.
public sealed class ScreenRouter
{
    private readonly IReadOnlyList<Control> _screens;
    private readonly HashSet<Control> _armyOverlayScreens;
    private readonly ArmyOverviewController _armyOverview;
    private readonly Dictionary<Control, float> _authoredTopOffsets = [];

    public ScreenRouter(
        IReadOnlyList<Control> screens,
        IEnumerable<Control> armyOverlayScreens,
        ArmyOverviewController armyOverview)
    {
        _screens = screens ?? throw new ArgumentNullException(nameof(screens));
        _armyOverlayScreens = new HashSet<Control>(armyOverlayScreens ??
            throw new ArgumentNullException(nameof(armyOverlayScreens)));
        _armyOverview = armyOverview ?? throw new ArgumentNullException(nameof(armyOverview));
        foreach (var screen in _screens) _authoredTopOffsets.Add(screen, screen.OffsetTop);
    }

    public Control? Current { get; private set; }

    public void Show(
        Control target,
        ActiveRunDto? run,
        ContentRegistry? content,
        CompiledRunRules? rules)
    {
        ArgumentNullException.ThrowIfNull(target);
        foreach (var screen in _screens) screen.Visible = screen == target;
        Current = target;
        var showArmy = run is not null && content is not null && rules is not null &&
            _armyOverlayScreens.Contains(target);
        // The resource strip is a sibling overlay, not a child of the current
        // page's layout. Reserve its band centrally instead of stacking both
        // sets of controls at the viewport's top edge. Restore authored space
        // on screens without the strip; repeated navigation must not add insets.
        target.OffsetTop = _authoredTopOffsets[target] + (showArmy ? _armyOverview.HeaderReservedHeight : 0f);
        _armyOverview.Visible = showArmy;
        if (showArmy)
            _armyOverview.Bind(ArmyOverviewFactory.Build(run!, content!, rules!));
        else
            _armyOverview.Close();
        foreach (var node in target.FindChildren("*", "Button", true, false))
            if (node is Button { Disabled: false } button && button.IsVisibleInTree())
            {
                button.GrabFocus();
                break;
            }
    }
}
