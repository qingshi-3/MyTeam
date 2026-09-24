using System.Collections.Generic;
using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Presentation;

public partial class BattleScreenController
{
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
    }
}
