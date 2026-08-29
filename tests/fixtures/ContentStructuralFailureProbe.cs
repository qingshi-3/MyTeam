using Godot;

public partial class ContentStructuralFailureProbe : Node
{
    private static bool _reported;

    public override void _Notification(int what)
    {
        if (what != NotificationSceneInstantiated || _reported) return;
        _reported = true;
        GD.PushError("CONTENT_GATE_STRUCTURAL_INSTANTIATE_FAILURE");
    }
}
