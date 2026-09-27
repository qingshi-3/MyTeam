using Godot;

// Test-only stage. A SubViewport receives the same GUI input pipeline while its
// pointer state stays independent of the user's native desktop window.
internal sealed class UiInputStage
{
    public Viewport Viewport { get; }
    private readonly Node _parent;
    private readonly SubViewport? _isolated;

    public UiInputStage(Node owner, bool isolated)
    {
        if (isolated)
        {
            // The project's maximized startup mode can override CLI dimensions.
            // Keep the test host small; only the internal viewport uses 1600x900.
            owner.GetWindow().Mode = Window.ModeEnum.Windowed;
            owner.GetWindow().Size = new Vector2I(960, 540);
            _isolated = new SubViewport
            {
                Name = "InputStage",
                Size = new Vector2I(1600, 900),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                HandleInputLocally = true
            };
            owner.AddChild(_isolated);
            Viewport = _isolated;
            _parent = _isolated;
        }
        else
        {
            Viewport = owner.GetViewport();
            _parent = owner;
        }
    }

    public void AddChild(Node node) => _parent.AddChild(node);
    public void Resize(Vector2I size) { if (_isolated is not null) _isolated.Size = size; }
    public void WarpPointer(Vector2 point) { if (_isolated is null) Input.WarpMouse(point); }
    public void Push(InputEvent input)
    {
        if (_isolated is null) Input.ParseInputEvent(input);
        else _isolated.PushInput(input, inLocalCoords: true);
    }
}
