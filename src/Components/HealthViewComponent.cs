using Godot;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class HealthViewComponent : Node2D
{
    [Export] public ProgressBar Bar { get; set; } = null!;

    public void SetHealth(float current, float maximum)
    {
        if (Bar is null) return;
        Bar.MaxValue = maximum;
        Bar.Value = Mathf.Clamp(current, 0, maximum);
    }
}
