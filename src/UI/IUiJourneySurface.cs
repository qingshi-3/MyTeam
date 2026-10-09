using Godot;

namespace TowerAutobattler.UI;

// A page exposes its authored visible surface independently of its full-screen
// layout and input bounds. Transition light follows that surface's transform.
public interface IUiJourneySurface
{
    Control? ResolveJourneySurface(Vector2 source);
}
