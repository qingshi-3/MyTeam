using System;
using Godot;

namespace TowerAutobattler.Content;

[GlobalClass]
public partial class UnitPortraitDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public SpriteFrames? Frames { get; set; }
    [Export] public StringName AnimationName { get; set; } = "idle";
    [Export(PropertyHint.Range, "0,128,1")] public int FrameIndex { get; set; }
    [Export(PropertyHint.Range, "0.5,4,0.05")] public float Zoom { get; set; } = 2f;
    [Export] public Vector2 OffsetRatio { get; set; } = Vector2.Zero;
    [Export] public bool FlipHorizontal { get; set; }

    public Texture2D? ResolveTexture()
    {
        if (Frames is null || string.IsNullOrWhiteSpace(AnimationName.ToString()) ||
            !Frames.HasAnimation(AnimationName) || FrameIndex < 0 || FrameIndex >= Frames.GetFrameCount(AnimationName))
            return null;
        return Frames.GetFrameTexture(AnimationName, FrameIndex);
    }

    public ValidationReport Validate(string expectedStableId)
    {
        var report = new ValidationReport();
        var label = string.IsNullOrWhiteSpace(ResourcePath) ? expectedStableId : ResourcePath;
        if (!string.Equals(StableId, expectedStableId, StringComparison.Ordinal))
            report.Error($"{label}: portrait stable id '{StableId}' does not match '{expectedStableId}'.");
        if (string.IsNullOrWhiteSpace(ResourcePath)) report.Error($"{expectedStableId}: portrait must be an external resource.");
        if (Frames is null) report.Error($"{label}: portrait has no SpriteFrames source.");
        else if (!Frames.HasAnimation(AnimationName)) report.Error($"{label}: portrait animation '{AnimationName}' is missing.");
        else if (FrameIndex < 0 || FrameIndex >= Frames.GetFrameCount(AnimationName))
            report.Error($"{label}: portrait frame {FrameIndex} is outside animation '{AnimationName}'.");
        else if (Frames.GetFrameTexture(AnimationName, FrameIndex) is null)
            report.Error($"{label}: portrait frame texture is empty.");
        if (Zoom is < .5f or > 4f) report.Error($"{label}: portrait zoom must be between 0.5 and 4.");
        if (Math.Abs(OffsetRatio.X) > 1 || Math.Abs(OffsetRatio.Y) > 1)
            report.Error($"{label}: portrait offset ratio must stay within -1..1.");
        return report;
    }
}
