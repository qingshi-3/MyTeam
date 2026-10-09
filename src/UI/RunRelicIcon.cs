using Godot;

namespace TowerAutobattler.UI;

// Reading-only authored tile; activation never equips, removes or consumes a relic.
public partial class RunRelicIcon : Button
{
    public string ContentId { get; private set; } = "";
    public Control? HintAnchor { get; set; }

    public override void _GuiInput(InputEvent input)
    {
        // Pointer inspection should leave with the pointer; keyboard navigation
        // still retains focus so every relic remains readable without a mouse.
        if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) ReleaseFocus();
    }

    public void Bind(RunRelicViewModel model)
    {
        ContentId = model.ContentId;
        GetNode<TextureRect>("Artwork").Texture = model.Icon;
        var fallback = GetNode<Label>("NoIconLabel");
        fallback.Visible = model.Icon is null;
        fallback.Text = model.Hint.Title;
        var count = GetNode<Label>("Stacks");
        count.Visible = model.Stacks > 1;
        count.Text = $"×{model.Stacks}";
        AccessibilityName = model.Hint.PlainText;
        BattleLabHoverHint.Bind(this, model.Hint);
    }
}
