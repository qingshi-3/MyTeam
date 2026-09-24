using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Vfx;

// Bounded chips are reserved with the instance, then released once with the
// established ice shell. All motion fits the owner's real-time release window.
public partial class VfxFrozenReleaseTrack : Node2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public PackedScene ParticleScene { get; set; } = null!;
    [Export] public int Amount { get; set; } = 18;
    [Export] public int Seed { get; set; } = 1061;
    private VfxPlaybackState? _playback;
    private bool _formed;
    private readonly List<Chip> _chips = [];
    private sealed record Chip(Sprite2D Sprite, Vector2 Origin, Vector2 Velocity, float Size, float Spin);
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public override void _Ready()
    {
        using var random = new RandomNumberGenerator { Seed = (ulong)Seed };
        int count = Mathf.Clamp(Mathf.RoundToInt(Amount * (_playback?.DensityAtBirth ?? 1)), 1, 36);
        for (int i = 0; i < count; i++)
        {
            var sprite = ParticleScene.Instantiate<Sprite2D>();
            AddChild(sprite);
            sprite.Visible = false;
            var origin = new Vector2(random.RandfRange(-47, 47), random.RandfRange(-70, 15));
            var outward = new Vector2(origin.X, origin.Y + 25).Normalized();
            _chips.Add(new(sprite, origin, outward * random.RandfRange(45, 110) + new Vector2(0, -30),
                random.RandfRange(10, 22), random.RandfRange(-5, 5)));
        }
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        if (_playback?.Cancelled != true && (_playback?.Age ?? age) > .10f / (_playback?.Parameters.CastSpeed ?? 1))
            _formed = true;
        float t = release * .25f;
        foreach (var chip in _chips)
        {
            var sprite = chip.Sprite;
            sprite.Visible = _formed && release > 0 && release < 1;
            if (!sprite.Visible) continue;
            sprite.Position = chip.Origin + (reducedMotion ? Vector2.Zero : chip.Velocity * t + new Vector2(0, 150 * t * t));
            sprite.Rotation = reducedMotion ? 0 : chip.Spin * t;
            sprite.Scale = Vector2.One * chip.Size * (_playback?.ParticleScale ?? 1) / sprite.Texture.GetWidth();
            sprite.Modulate = new Color(.54f, .84f, 1, Mathf.Pow(1 - release, .75f));
        }
    }
}
