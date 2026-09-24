using System;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Audio;
using TowerAutobattler.Components;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

// Uses an isolated instance of the real content scene. Frame timing and sound
// dispatch stay in the same animation component that production battle uses.
public partial class UnitAnimationAudioPreview : ContextPopup
{
    private UnitContentRoot? _unit;
    private UnitAnimationComponent? _animation;
    private AnimatedSprite2D? _sprite;
    private PackedScene? _source;
    private int _selectedFrameCount;
    private string _selectedAnimation = "";
    private readonly Dictionary<string, Rect2> _actionBounds = [];
    private readonly List<string> _actions = [];
    private readonly List<int> _markers = [];
    private Node2D _holder = null!;
    private Control _stage = null!;
    private FeedbackAudio _audio = null!;
    private OptionButton _action = null!;
    private ItemList _markerList = null!;
    private SpinBox _markerFrame = null!;
    private Label _title = null!, _frame = null!, _sourceLabel = null!, _feedback = null!;
    private Button _pause = null!, _playSound = null!;
    private bool _paused;
    private bool _mustReplay;
    public int CurrentFrame => (_sprite?.Frame ?? 0) + 1;
    public int SoundPlayCount => _audio.PlayedCount;

    public override void _Ready()
    {
        base._Ready();
        _holder = GetNode<Node2D>("%PreviewActorHost");
        _stage = GetNode<Control>("%PreviewStage");
        _audio = GetNode<FeedbackAudio>("%PreviewAudio");
        _action = GetNode<OptionButton>("%PreviewAction");
        _markerList = GetNode<ItemList>("%SoundMarkers");
        _markerFrame = GetNode<SpinBox>("%MarkerFrame");
        _title = GetNode<Label>("%PreviewTitle");
        _frame = GetNode<Label>("%PreviewFrame");
        _sourceLabel = GetNode<Label>("%PreviewSource");
        _feedback = GetNode<Label>("%PreviewFeedback");
        _pause = GetNode<Button>("%PreviewPause");
        _playSound = GetNode<Button>("%PlaySound");
        GetNode<Button>("%PreviewPlay").Pressed += Replay;
        _pause.Pressed += TogglePaused;
        GetNode<Button>("%PreviewPreviousFrame").Pressed += () => StepFrame(-1);
        GetNode<Button>("%PreviewNextFrame").Pressed += () => StepFrame(1);
        _playSound.Pressed += Audition;
        GetNode<Button>("%ResetSounds").Pressed += ResetSounds;
        _action.ItemSelected += ActionSelected;
        _markerList.ItemSelected += MarkerSelected;
        _markerFrame.ValueChanged += FrameChanged;
        _stage.Resized += FitActor;
        Closed += ReleaseActor;
        SetProcess(false);
    }

    public void ShowUnit(PackedScene scene, string displayName, Control? opener = null)
    {
        ReleaseActor();
        _source = scene;
        _title.Text = displayName + " · 动画音效";
        _sourceLabel.Text = "永久配置：" + scene.ResourcePath + "\nVisualRoot / UnitAnimationComponent → FrameSounds";
        Open(opener);
        CreateActor();
    }

    private void CreateActor()
    {
        if (_source is null) return;
        _unit = _source.Instantiate<UnitContentRoot>();
        _animation = _unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
        // Duplicate the marker and nested sound resources before exposing edits.
        var isolated = new Godot.Collections.Array<UnitAnimationSound>();
        foreach (var marker in _animation.FrameSounds)
            isolated.Add((UnitAnimationSound)marker.Duplicate(true));
        _animation.FrameSounds = isolated;
        _holder.AddChild(_unit);
        _unit.GetNode<CanvasItem>("HealthViewComponent").Hide();
        _sprite = _animation.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _animation.AnimationSoundRequested += OnAnimationSound;
        _actions.Clear(); _action.Clear();
        AddAction("attack", "普攻");
        AddAction("skill_cast", "施法");
        if (_animation.Frames.HasAnimation("move") || _animation.Frames.HasAnimation("run")) AddAction("move", "移动");
        AddAction("idle", "待机");
        _action.Select(0);
        _feedback.Text = "调整仅用于本次试听；关闭后还原，不修改英雄资源。";
        Replay();
        SetProcess(true);
    }

    private void AddAction(string cue, string label) { _actions.Add(cue); _action.AddItem(label); }
    private void ActionSelected(long _) => Replay();

    private void Replay()
    {
        if (_animation is null || _actions.Count == 0) return;
        _audio.Clear(); _paused = false; _mustReplay = false; _pause.Text = "暂停";
        _unit!.Modulate = Colors.White; _unit.Show();
        _animation.ResetPresentation();
        _animation.SetCombatSpeed(1);
        _animation.PlayCue(_actions[_action.Selected]);
        _selectedFrameCount = _sprite!.SpriteFrames.GetFrameCount(_sprite.Animation);
        _selectedAnimation = _sprite.Animation;
        RefreshMarkers(); FitActor();
    }

    private void TogglePaused()
    {
        if (_animation is null) return;
        if (_paused && _mustReplay) { Replay(); return; }
        _paused = !_paused;
        _audio.SetPaused(_paused); _animation.SetPaused(_paused);
        _pause.Text = _paused ? "继续" : "暂停";
    }

    private void PauseForEditing()
    {
        _paused = true; _mustReplay = true;
        _animation?.SetPaused(true); _audio.Clear();
        _pause.Text = "重播继续";
    }

    private void StepFrame(int direction)
    {
        if (_sprite is null || _animation is null) return;
        var current = _sprite.Animation == _selectedAnimation ? _sprite.Frame : 0;
        PauseForEditing();
        // Paused animation callbacks stay silent. Resume must restart the component
        // clock because this visual inspection deliberately bypasses action time.
        _sprite.Animation = _selectedAnimation;
        _sprite.SetFrameAndProgress(Math.Clamp(current + direction, 0, _selectedFrameCount - 1), 0);
    }

    public override void _Process(double delta)
    {
        if (_animation is null || _sprite is null) return;
        var count = _sprite.SpriteFrames.GetFrameCount(_sprite.Animation);
        var cue = _animation.ActiveLogicalCue == _animation.ActiveCue ? _animation.ActiveCue
            : $"{_animation.ActiveLogicalCue} → {_animation.ActiveCue}";
        if (_mustReplay) cue = _actions[_action.Selected] + " · 逐帧查看";
        _frame.Text = $"{cue}  ·  第 {CurrentFrame} / {count} 帧" + (_paused ? "  ·  已暂停" : "");
    }

    private void RefreshMarkers()
    {
        _markers.Clear(); _markerList.Clear();
        if (_animation is null) return;
        var cue = _actions[_action.Selected];
        for (var index = 0; index < _animation.FrameSounds.Count; index++)
        {
            var marker = _animation.FrameSounds[index];
            if (marker.Animation != cue) continue;
            _markers.Add(index);
            _markerList.AddItem($"第 {marker.Frame} 帧  ·  {marker.Sound?.Cue ?? "未配置声音"}");
        }
        _markerFrame.Editable = _markers.Count > 0;
        _playSound.Disabled = _markers.Count == 0;
        if (_markers.Count > 0) { _markerList.Select(0); MarkerSelected(0); }
        else _feedback.Text = "此动作没有音效标记。可在英雄场景 FrameSounds 中新增。";
    }

    private UnitAnimationSound? SelectedMarker()
    {
        var selected = _markerList.GetSelectedItems();
        return _animation is not null && selected.Length > 0 ? _animation.FrameSounds[_markers[selected[0]]] : null;
    }

    private void MarkerSelected(long index)
    {
        var marker = SelectedMarker();
        if (marker is null || _sprite is null) return;
        _markerFrame.SetBlockSignals(true);
        _markerFrame.MaxValue = Math.Max(marker.Frame, _selectedFrameCount);
        _markerFrame.Value = marker.Frame;
        _markerFrame.SetBlockSignals(false);
        _feedback.Text = marker.Sound?.Stream?.ResourcePath ?? "标记未配置音源。";
    }

    private void FrameChanged(double frame)
    {
        var marker = SelectedMarker();
        if (marker is null) return;
        PauseForEditing();
        marker.Frame = (int)frame;
        var selected = _markerList.GetSelectedItems()[0];
        _markerList.SetItemText(selected, $"第 {marker.Frame} 帧  ·  {marker.Sound?.Cue ?? "未配置声音"}");
        _feedback.Text = $"临时改为第 {marker.Frame} 帧；点击“重播”听实际时机。关闭后还原。";
    }

    private void Audition()
    {
        var sound = SelectedMarker()?.Sound;
        if (sound is null) return;
        _audio.Clear(); _audio.Play(sound);
    }

    private void OnAnimationSound(FeedbackSound sound) => _audio.Play(sound);
    private void ResetSounds() { ReleaseActor(); CreateActor(); }

    private void FitActor()
    {
        if (_unit is null || _sprite is null || _stage.Size.X <= 0) return;
        var action = _selectedAnimation;
        if (string.IsNullOrEmpty(action)) return;
        if (!_actionBounds.TryGetValue(action, out var bounds))
        {
            // Fit the union, not an initial crouched pose: attacks such as a wide
            // cyclone can occupy very different space in later frames.
            var hasBounds = false;
            for (var frame = 0; frame < _sprite.SpriteFrames.GetFrameCount(action); frame++)
            {
                var texture = _sprite.SpriteFrames.GetFrameTexture(action, frame);
                if (texture is null) continue;
                using var image = texture.GetImage();
                var used = image?.GetUsedRect() ?? new Rect2I(Vector2I.Zero, (Vector2I)texture.GetSize());
                if (used.Size.X <= 0 || used.Size.Y <= 0) continue;
                var local = new Rect2((Vector2)used.Position - texture.GetSize() * .5f, used.Size);
                bounds = hasBounds ? bounds.Merge(local) : local;
                hasBounds = true;
            }
            if (!hasBounds) return;
            _actionBounds[action] = bounds;
        }
        var spriteScale = _sprite.Scale.X;
        var scale = Mathf.Min(_stage.Size.X / bounds.Size.X, _stage.Size.Y / bounds.Size.Y) * .82f / spriteScale;
        _unit.Scale = Vector2.One * scale;
        var localCenter = bounds.GetCenter();
        _unit.Position = _stage.Size * .5f - (_sprite.Position + localCenter * spriteScale) * scale;
    }

    private void ReleaseActor()
    {
        SetProcess(false); _audio.Clear();
        if (_animation is not null) { _animation.AnimationSoundRequested -= OnAnimationSound; _animation.SetPaused(true); }
        if (_unit is not null) { _holder.RemoveChild(_unit); _unit.QueueFree(); }
        _unit = null; _animation = null; _sprite = null;
        _selectedAnimation = "";
        _actionBounds.Clear();
    }

    public override void _ExitTree()
    {
        ReleaseActor();
        Closed -= ReleaseActor;
        _stage.Resized -= FitActor;
        base._ExitTree();
    }
}
