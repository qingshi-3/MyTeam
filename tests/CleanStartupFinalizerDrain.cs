using System;
using Godot;

// Godot 4.7's --quit-after path tears native containers down before late managed
// array finalizers unless the startup probe drains them while the engine is live.
public partial class CleanStartupFinalizerDrain : Node
{
    private int _frames;

    public override void _Process(double delta)
    {
        if (++_frames != 3) return;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        SetProcess(false);
    }
}
