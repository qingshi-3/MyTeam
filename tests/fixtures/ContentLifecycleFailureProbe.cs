using Godot;

public partial class ContentLifecycleFailureProbe : Node
{
    public enum FailurePhase { Ready, Process, ExitTree, SceneInstantiated }

    [Export] public FailurePhase Phase { get; set; }
    [Export] public string Marker { get; set; } = string.Empty;
    private bool _reported;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated && Phase == FailurePhase.SceneInstantiated) Report();
    }

    public override void _Ready()
    {
        if (Phase == FailurePhase.Ready) Report();
    }

    public override void _Process(double delta)
    {
        if (Phase == FailurePhase.Process) Report();
    }

    public override void _ExitTree()
    {
        if (Phase == FailurePhase.ExitTree) Report();
    }

    private void Report()
    {
        if (_reported) return;
        _reported = true;
        GD.PushError(Marker);
    }
}
