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
    private readonly HashSet<Control> _relicScreens;
    private readonly ArmyOverviewController _armyOverview;
    private readonly Dictionary<Control, float> _authoredTopOffsets = [];
    private readonly Dictionary<Control, float> _authoredLeftOffsets = [];

    public ScreenRouter(
        IReadOnlyList<Control> screens,
        IEnumerable<Control> armyOverlayScreens,
        ArmyOverviewController armyOverview,
        IEnumerable<Control>? relicScreens = null)
    {
        _screens = screens ?? throw new ArgumentNullException(nameof(screens));
        _armyOverlayScreens = new HashSet<Control>(armyOverlayScreens ??
            throw new ArgumentNullException(nameof(armyOverlayScreens)));
        _armyOverview = armyOverview ?? throw new ArgumentNullException(nameof(armyOverview));
        _relicScreens = new HashSet<Control>(relicScreens ?? _armyOverlayScreens);
        foreach (var screen in _screens)
        {
            _authoredTopOffsets.Add(screen, screen.OffsetTop);
            _authoredLeftOffsets.Add(screen, screen.OffsetLeft);
        }
    }

    public Control? Current { get; private set; }

    public void Show(
        Control target,
        RunView? run,
        ContentRegistry? content,
        CompiledRunRules? rules,
        Control? retainedArtwork = null,
        bool focus = true)
    {
        ArgumentNullException.ThrowIfNull(target);
        foreach (var screen in _screens) screen.Visible = screen == target || screen == retainedArtwork;
        Current = target;
        var showArmy = run is not null && content is not null && rules is not null &&
            _armyOverlayScreens.Contains(target);
        var showRelics = run is not null && content is not null && rules is not null && _relicScreens.Contains(target);
        // The resource strip is a sibling overlay, not a child of the current
        // page's layout. Reserve its band centrally instead of stacking both
        // sets of controls at the viewport's top edge. Restore authored space
        // on screens without the strip; repeated navigation must not add insets.
        _armyOverview.SetRunHeaderContext(showArmy, showRelics);
        _armyOverview.Visible = showArmy || showRelics;
        if (!showArmy) _armyOverview.Close();
        if (showArmy || showRelics)
            _armyOverview.Bind(ArmyOverviewFactory.Build(run!, content!, rules!));
        target.OffsetTop = _authoredTopOffsets[target] + (showArmy || showRelics ? _armyOverview.HeaderReservedHeight : 0f);
        target.OffsetLeft = _authoredLeftOffsets[target] + (showRelics ? _armyOverview.TraitsReservedWidth : 0f);
        if (focus) FocusCurrent();
    }

    public void FocusCurrent()
    {
        if (Current is not { } target || !target.IsVisibleInTree()) return;
        if (target is ExpeditionScreenController expedition)
        {
            expedition.FocusParty();
            return;
        }
        foreach (var node in target.FindChildren("*", "Button", true, false))
            if (node is Button { Disabled: false } button && button.IsVisibleInTree())
            {
                button.GrabFocus();
                break;
            }
    }
}
