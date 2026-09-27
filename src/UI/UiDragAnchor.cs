using Godot;

namespace TowerAutobattler.UI;

// Godot owns this empty native preview. Its lifetime ends on drop/Escape; the
// separately authored visual can finish its short flight in the same viewport.
public partial class UiDragAnchor : Control
{
    public UiDragVisual? Visual { get; set; }
    public override void _ExitTree()
    {
        if (IsInstanceValid(Visual)) Visual!.CallDeferred(nameof(UiDragVisual.Release));
    }
}
