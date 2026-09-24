using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class UnitPortrait : Control
{
    [Export(PropertyHint.Range, "0.1,1.5,0.05")] public float UiPlaybackScale { get; set; } = .75f;
    [Export] public bool ContextMirrorHorizontal { get; set; }
    [Export] public bool FitVisibleArtwork { get; set; }

    // Resources are shared, but these derived layout caches neither edit them nor keep them alive.
    private static readonly ConditionalWeakTable<Texture2D, TextureArtworkBounds> TextureBounds = new();
    private static readonly ConditionalWeakTable<SpriteFrames, AnimationArtworkBounds> AnimationBounds = new();

    private AnimatedSprite2D _sprite = null!;
    private TextureRect _fallback = null!;
    private UnitPortraitDefinition? _definition;
    private Texture2D? _layoutTexture;
    private bool _hasPlayablePortrait;

    public UnitPortraitDefinition? Definition => _definition;
    public bool HasAuthoredPortrait => _definition?.ResolveTexture() is not null;
    public bool IsPortraitPlaying => _sprite?.IsPlaying() == true;
    public int CurrentFrame => _sprite?.Frame ?? 0;

    public override void _Ready()
    {
        CacheNodes();
        Resized += ApplyLayout;
        VisibilityChanged += SyncPlayback;
        ApplyLayout();
        SyncPlayback();
    }

    public override void _ExitTree()
    {
        Resized -= ApplyLayout;
        VisibilityChanged -= SyncPlayback;
        _sprite?.Pause();
    }

    public void Bind(UnitPortraitDefinition? definition, Texture2D? fallback = null)
    {
        CacheNodes();
        _definition = definition;
        _layoutTexture = definition?.ResolveTexture();
        _hasPlayablePortrait = _layoutTexture is not null && definition?.Frames is not null;
        _sprite.Stop();
        _sprite.Visible = _hasPlayablePortrait;
        _fallback.Texture = _hasPlayablePortrait ? null : fallback;
        var effectiveFlipHorizontal = (definition?.FlipHorizontal ?? false) ^ ContextMirrorHorizontal;
        _fallback.FlipH = effectiveFlipHorizontal;
        _fallback.Visible = _fallback.Texture is not null;
        if (_hasPlayablePortrait)
        {
            _sprite.SpriteFrames = definition!.Frames;
            _sprite.Animation = definition.AnimationName;
            _sprite.FlipH = effectiveFlipHorizontal;
            _sprite.SpeedScale = UiPlaybackScale;
            _sprite.Play(definition.AnimationName);
            _sprite.Frame = definition.FrameIndex;
            _sprite.FrameProgress = 0;
        }
        else
        {
            _layoutTexture = fallback;
        }
        ApplyLayout();
        SyncPlayback();
    }

    private void ApplyLayout()
    {
        if (_layoutTexture is null || Size.X <= 0 || Size.Y <= 0) return;
        Vector2 sourceSize = _layoutTexture.GetSize();
        if (sourceSize.X <= 0 || sourceSize.Y <= 0) return;
        if (FitVisibleArtwork)
        {
            var flip = (_definition?.FlipHorizontal ?? false) ^ ContextMirrorHorizontal;
            var bounds = ResolveVisibleArtworkBounds(flip);
            if (bounds.Size.X <= 0 || bounds.Size.Y <= 0) bounds = new Rect2(Vector2.Zero, sourceSize);
            // Full-art consumers intentionally replace the authored close-up zoom and offset.
            // One union for the entire idle animation prevents breathing/bobbing from pumping scale.
            var fitScale = Mathf.Min(Size.X / bounds.Size.X, Size.Y / bounds.Size.Y) * .96f;
            var fitPosition = (Size - bounds.Size * fitScale) * .5f - bounds.Position * fitScale;
            _sprite.Scale = Vector2.One * fitScale;
            _sprite.Position = fitPosition;
            _fallback.Size = sourceSize * fitScale;
            _fallback.Position = fitPosition;
            return;
        }
        var zoom = _definition?.Zoom ?? 1f;
        var scale = Mathf.Min(Size.X / sourceSize.X, Size.Y / sourceSize.Y) * zoom;
        var renderedSize = sourceSize * scale;
        var offset = _definition?.OffsetRatio ?? Vector2.Zero;
        var position = (Size - renderedSize) * .5f + new Vector2(Size.X * offset.X, Size.Y * offset.Y);
        _sprite.Scale = Vector2.One * scale;
        _sprite.Position = position;
        _fallback.Size = renderedSize;
        _fallback.Position = position;
    }

    private Rect2 ResolveVisibleArtworkBounds(bool flipped)
    {
        if (_hasPlayablePortrait && _definition?.Frames is { } frames)
        {
            var cache = AnimationBounds.GetValue(frames, source =>
            {
                var created = new AnimationArtworkBounds();
                source.Changed += created.Clear;
                return created;
            });
            var animation = _definition.AnimationName.ToString();
            if (!cache.ByAnimation.TryGetValue(animation, out var result))
            {
                Rect2 normal = default, mirrored = default;
                for (var frame = 0; frame < frames.GetFrameCount(_definition.AnimationName); frame++)
                {
                    var texture = frames.GetFrameTexture(_definition.AnimationName, frame);
                    if (texture is null) continue;
                    var used = ReadTextureBounds(texture);
                    if (used.Size.X <= 0 || used.Size.Y <= 0) continue;
                    normal = MergeNonempty(normal, used);
                    // Mirroring is relative to each complete texture, including transparent margins.
                    var reflected = new Rect2(new Vector2(texture.GetWidth() - used.End.X, used.Position.Y), used.Size);
                    mirrored = MergeNonempty(mirrored, reflected);
                }
                result = (normal, mirrored);
                cache.ByAnimation[animation] = result;
            }
            return flipped ? result.Mirrored : result.Normal;
        }
        if (_layoutTexture is null) return default;
        var fallbackBounds = ReadTextureBounds(_layoutTexture);
        return flipped ? new Rect2(new Vector2(_layoutTexture.GetWidth() - fallbackBounds.End.X,
            fallbackBounds.Position.Y), fallbackBounds.Size) : fallbackBounds;
    }

    private static Rect2 ReadTextureBounds(Texture2D texture)
    {
        var cache = TextureBounds.GetValue(texture, source =>
        {
            var created = new TextureArtworkBounds();
            source.Changed += created.Clear;
            return created;
        });
        if (cache.Valid) return cache.Bounds;
        // GetImage returns a copy (including an AtlasTexture's region), so decompression is local.
        using var image = texture.GetImage();
        if (image is null || image.IsEmpty() || (image.IsCompressed() && image.Decompress() != Error.Ok))
            cache.Bounds = new Rect2(Vector2.Zero, texture.GetSize());
        else
        {
            var used = image.GetUsedRect();
            cache.Bounds = new Rect2(used.Position, used.Size);
        }
        cache.Valid = true;
        return cache.Bounds;
    }

    private static Rect2 MergeNonempty(Rect2 current, Rect2 next) =>
        current.Size.X <= 0 || current.Size.Y <= 0 ? next : current.Merge(next);

    private sealed class TextureArtworkBounds
    {
        public Rect2 Bounds;
        public bool Valid;
        public void Clear() => Valid = false;
    }

    private sealed class AnimationArtworkBounds
    {
        public readonly Dictionary<string, (Rect2 Normal, Rect2 Mirrored)> ByAnimation = new();
        public void Clear() => ByAnimation.Clear();
    }

    private void SyncPlayback()
    {
        if (_sprite is null || !_hasPlayablePortrait) return;
        if (IsVisibleInTree()) _sprite.Play();
        else _sprite.Pause();
    }

    private void CacheNodes()
    {
        _sprite ??= GetNode<AnimatedSprite2D>("%PortraitSprite");
        _fallback ??= GetNode<TextureRect>("%PortraitFallback");
    }
}
