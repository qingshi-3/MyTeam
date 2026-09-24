using Godot;

namespace TowerAutobattler.Vfx;

// A view-only sample. The effect never owns or changes the actor's sprite/material.
public sealed record VfxBodyVisual(Texture2D Texture, Transform2D Transform, Vector2 Offset,
    bool Centered, bool FlipH, bool FlipV)
{
    public static VfxBodyVisual? Capture(AnimatedSprite2D sprite, Transform2D worldToStage)
    {
        if (!sprite.IsVisibleInTree() || sprite.SpriteFrames is not { } frames ||
            !frames.HasAnimation(sprite.Animation) || frames.GetFrameCount(sprite.Animation) == 0) return null;
        return new(frames.GetFrameTexture(sprite.Animation, sprite.Frame), worldToStage * sprite.GlobalTransform,
            sprite.Offset, sprite.Centered, sprite.FlipH, sprite.FlipV);
    }
}
