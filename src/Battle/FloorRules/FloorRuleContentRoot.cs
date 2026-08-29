using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

[GlobalClass]
public partial class FloorRuleContentRoot : Node
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string PreviewText { get; set; } = string.Empty;
    [Export] public float PulseInterval { get; set; } = 4f;
    [Export] public float PulseAmount { get; set; } = 10f;

    public virtual IBattleFloorRuleRuntime CreateRuntime() => new ClearFloorRuleRuntime(Id, DisplayName, PreviewText);

    public ValidationReport ValidateAuthoring()
    {
        var report = new ValidationReport();
        if (string.IsNullOrWhiteSpace(Id)) report.Error($"{SceneFilePath}: floor rule has empty id");
        if (string.IsNullOrWhiteSpace(PreviewText)) report.Error($"{SceneFilePath}: floor rule has no preview");
        return report;
    }
}
