using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class UnitPortrait : Control
{
    [Export(PropertyHint.Range, "0.1,1.5,0.05")] public float UiPlaybackScale { get; set; } = .75f;
    [Export] public bool ContextMirrorHorizontal { get; set; }

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
