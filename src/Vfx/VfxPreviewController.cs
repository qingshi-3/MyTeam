using System;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Vfx;

public partial class VfxPreviewController : Control, IVfxStage
{
    [Export] public AbilityDefinition RendAbility { get; set; } = null!;
    private VfxPlayer _player = null!;
    private ItemList _list = null!;
    private Control _stage = null!;
    private Label _details = null!;
    private readonly List<VfxDefinition> _visible = [];
    private string _selected = "rend";
    private float _elapsed;
    private float _radius = 1.5f;
    private float _distance = 3;
    private float _heading;
    private bool _loop = true;
    private float _castSpeed = 1, _motionSpeed = 1, _flightTime = .78f;
    private float _particleSize = 1, _density = 1, _width = 1, _progress;
    private bool _eventMode, _manuallyEnded;
    private SpriteFrames _defaultSourceFrames = null!;
    public float UnitScale => 1.7f;
    public Vector2 Project(Vector2 point, bool ground) => _stage.Size * .5f + point * 75 + (ground ? new Vector2(0, 35) : Vector2.Zero);
    public float RadiusPixels(float radius) => radius * 75;
    public override void _Ready()
    {
        DisplayServer.WindowSetTitle("军团登塔 · 特效预览室");
        _player = GetNode<VfxPlayer>("%Player");
        _stage = GetNode<Control>("%Stage");
        _list = GetNode<ItemList>("%Effects");
        _details = GetNode<Label>("%Details");
        _defaultSourceFrames = GetNode<AnimatedSprite2D>("%SourceUnit").SpriteFrames;
        _player.Bind(this);
        Filter("");
        GetNode<LineEdit>("%Search").TextChanged += Filter;
        _list.ItemSelected += index =>
        {
            _selected = _visible[(int)index].StableId;
            Replay();
            _list.EnsureCurrentIsVisible();
        };
        GetNode<Button>("%Replay").Pressed += Replay;
        GetNode<Button>("%Pause").Pressed += () =>
        {
            _player.Paused = !_player.Paused;
            GetNode<Button>("%Pause").Text = _player.Paused ? "继续" : "暂停";
        };
        GetNode<Button>("%Step").Pressed += () =>
        {
            _player.Paused = true;
            GetNode<Button>("%Pause").Text = "继续";
            AdvanceSource(1f / 30);
            _player.UpdateContext("preview", PreviewContext(_player.Catalog.Find(_selected)));
            _player.Advance(1f / 30);
            _elapsed += 1f / 30;
        };
        GetNode<Button>("%Impact").Pressed += () => _player.Impact("preview");
        GetNode<Button>("%End").Pressed += () => { _manuallyEnded = true; _player.End("preview", VfxEndReason.Completed); };
        GetNode<Button>("%Break").Pressed += () => { _manuallyEnded = true; _player.End("preview", VfxEndReason.Depleted); };
        GetNode<Button>("%Return").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/app/GameRoot.tscn");
        GetNode<CheckButton>("%Loop").Toggled += value => _loop = value;
        GetNode<CheckButton>("%Reduced").Toggled += value => _player.ReducedMotion = value;
        GetNode<CheckButton>("%Light").Toggled += value => GetNode<ColorRect>("%Backdrop").Color =
            value ? new Color("c9c6bf") : new Color("121c2b");
        GetNode<HSlider>("%Speed").ValueChanged += value => _player.Speed = (float)value;
        GetNode<HSlider>("%Radius").ValueChanged += value => { _radius = (float)value; Replay(); };
        GetNode<HSlider>("%Distance").ValueChanged += value => { _distance = (float)value; Replay(); };
        GetNode<HSlider>("%Heading").ValueChanged += value => { _heading = (float)value; Replay(); };
        GetNode<HSlider>("%CastSpeed").ValueChanged += value => { _castSpeed = (float)value; Replay(); };
        GetNode<HSlider>("%MotionSpeed").ValueChanged += value => { _motionSpeed = (float)value; Replay(); };
        GetNode<HSlider>("%FlightTime").ValueChanged += value => { _flightTime = (float)value; Replay(); };
        GetNode<HSlider>("%ParticleSize").ValueChanged += value => { _particleSize = (float)value; Replay(); };
        GetNode<HSlider>("%Density").ValueChanged += value => { _density = (float)value; Replay(); };
        GetNode<HSlider>("%Width").ValueChanged += value => { _width = (float)value; Replay(); };
        GetNode<CheckButton>("%EventMode").Toggled += value => { _eventMode = value; Replay(); };
        GetNode<Button>("%Fired").Pressed += () => _player.Signal("preview", VfxStartCue.Fired);
        GetNode<Button>("%ContactCue").Pressed += () => _player.Signal("preview", VfxStartCue.Impact);
        GetNode<HSlider>("%Progress").ValueChanged += value =>
        {
            _progress = (float)value;
            _player.UpdateContext("preview", PreviewContext(_player.Catalog.Find(_selected)));
        };
        GetNode<Button>("%Skill").Pressed += () =>
        {
            _selected = RendAbility.Presentation?.DamageVfx ?? throw new InvalidOperationException("Missing skill VFX binding.");
            Replay();
            _player.Clear();
            VfxBindingResolver.Present(_player,
                new TowerAutobattler.Battle.BattleVfxCue(TowerAutobattler.Battle.BattleVfxPhase.Burst, _selected,
                    CastSpeed: _castSpeed, MotionSpeed: _motionSpeed, InstanceId: "preview"),
                PreviewContext(_player.Catalog.Find(_selected)), "preview");
            _details.Text = RendAbility.DisplayName + " · 成功伤害的表现绑定\n" + _details.Text;
        };
        GetNode<Button>("%Reset").Pressed += () =>
        {
            GetNode<HSlider>("%Radius").Value = 1.5;
            GetNode<HSlider>("%Distance").Value = 3;
            GetNode<HSlider>("%Heading").Value = 0;
            GetNode<HSlider>("%Speed").Value = 1;
            foreach (var name in new[] { "CastSpeed", "MotionSpeed", "ParticleSize", "Density", "Width" })
                GetNode<HSlider>("%" + name).Value = 1;
            GetNode<HSlider>("%FlightTime").Value = .78;
            GetNode<CheckButton>("%EventMode").ButtonPressed = false;
        };
        _list.GrabFocus();
        Replay();
    }
    private void Filter(string query)
    {
        _visible.Clear(); _list.Clear();
        foreach (var definition in _player.Catalog.Effects)
            if ((definition.DisplayName + definition.StableId).Contains(query, StringComparison.OrdinalIgnoreCase))
            { _visible.Add(definition); _list.AddItem(definition.DisplayName); }
    }
    private void Replay()
    {
        _player.Clear(); _elapsed = 0;
        _manuallyEnded = false;
        _progress = 0;
        GetNode<HSlider>("%Progress").SetValueNoSignal(0);
        var definition = _player.Catalog.Find(_selected);
        var source = GetNode<AnimatedSprite2D>("%SourceUnit");
        source.SpriteFrames = definition.PreviewFrames ?? _defaultSourceFrames;
        source.Animation = "idle";
        source.Pause();
        source.SetFrameAndProgress(0, 0);
        PositionUnits();
        _player.Play(_selected, PreviewContext(definition), "preview");
        _details.Text = $"{definition.DisplayName}  ·  {(definition.Persistent ? "持续效果" : "瞬时效果")}\n半径 {_radius:0.0} 格 · 距离 {_distance:0.0} 格\n参数仅本次预览有效";
        if (!string.IsNullOrEmpty(definition.PreviewNote)) _details.Text += "\n" + definition.PreviewNote;
        GetNode<Button>("%End").Disabled = false;
        foreach (var name in new[] { "Impact", "Break" })
            GetNode<Button>("%" + name).Disabled = !definition.ReactsToImpact;
        bool sequence = definition.FlightDuration > 0 || definition.CastDuration > 0;
        GetNode<CheckButton>("%EventMode").Disabled = !sequence;
        GetNode<Button>("%Fired").Disabled = !sequence || !_eventMode;
        GetNode<Button>("%ContactCue").Disabled = !sequence || !_eventMode;
        GetNode<HSlider>("%Progress").Editable = sequence && _eventMode;
        GetNode<HSlider>("%FlightTime").Editable = definition.FlightDuration > 0;
        GetNode<HSlider>("%Width").Editable = definition.StretchBetween || definition.UsesWidth;
        GetNode<Label>("%CastSpeedLabel").Text = $"施法 {_castSpeed:0.00}×";
        GetNode<Label>("%MotionSpeedLabel").Text = $"流动 {_motionSpeed:0.00}×";
        GetNode<Label>("%FlightTimeLabel").Text = $"飞行 {_flightTime:0.00} 秒";
        GetNode<Label>("%HeadingLabel").Text = $"朝向 {_heading:0}°";
        PositionUnits();
    }
    private Vector2 PreviewDirection => Vector2.FromAngle(Mathf.DegToRad(_heading));
    private VfxContext PreviewContext(VfxDefinition definition) => new(-PreviewDirection * _distance / 2,
        (definition.AtSource ? -1 : 1) * PreviewDirection * _distance / 2,
        definition.Ground || definition.UsesRadius ? _radius : 0,
        new(CastSpeed: _castSpeed, MotionSpeed: _motionSpeed, FlightDuration: definition.FlightDuration > 0 ? _flightTime : null,
            ParticleScale: _particleSize, Density: _density, WidthScale: _width,
            Timing: _eventMode && (definition.CastDuration > 0 || definition.FlightDuration > 0) ? VfxTimingMode.Events : VfxTimingMode.Automatic),
        _progress, PreviewDirection, Body: definition.AtSource ? VfxBodyVisual.Capture(
            GetNode<AnimatedSprite2D>("%SourceUnit"), _player.GlobalTransform.AffineInverse()) : null);
    private void PositionUnits()
    {
        // These are centered body sprites, not full unit roots. The production
        // UnitAnimationComponent already offsets its sprite above the ground.
        GetNode<Node2D>("%SourceUnit").Position = Project(-PreviewDirection * _distance / 2, false);
        GetNode<Node2D>("%TargetUnit").Position = Project(PreviewDirection * _distance / 2, false);
    }
    public override void _Process(double delta)
    {
        if (!_player.Paused)
        {
            _elapsed += (float)delta * _player.Speed;
            AdvanceSource((float)delta * _player.Speed);
        }
        PositionUnits();
        // Sustained effects already animate continuously on their own clock.
        // Recreating them periodically cuts the orbit and repeats the entrance.
        // After explicit End/Break, only an explicit Replay should restore one.
        var definition = _player.Catalog.Find(_selected);
        if (definition.AtSource) _player.UpdateContext("preview", PreviewContext(definition));
        var parameters = PreviewContext(definition).Playback!;
        float duration = definition.Duration + (definition.CastDuration + definition.ActionSpan) * (1 / _castSpeed - 1)
            + (definition.FlightDuration > 0 ? _flightTime - definition.FlightDuration : 0);
        if (_loop && !_manuallyEnded && !definition.Persistent && parameters.Timing == VfxTimingMode.Automatic &&
            _player.ActiveCount == 0 && _elapsed > Mathf.Max(1.6f, duration + .35f)) Replay();
        GetNode<Label>("%Clock").Text = $"{_elapsed:0.00} 秒  ·  {_player.Speed:0.00}×  ·  {_player.ActiveCount} 实例";
    }

    private void AdvanceSource(float seconds)
    {
        // Sample the source idle on the same clock as its overlay, including
        // paused stepping. AnimatedSprite2D's internal clock stays paused.
        var source = GetNode<AnimatedSprite2D>("%SourceUnit");
        var frames = source.SpriteFrames;
        int count = frames.GetFrameCount(source.Animation);
        double fps = frames.GetAnimationSpeed(source.Animation);
        if (count == 0 || fps <= 0) return;
        double cycle = 0;
        for (int i = 0; i < count; i++) cycle += frames.GetFrameDuration(source.Animation, i);
        double time = source.FrameProgress * frames.GetFrameDuration(source.Animation, source.Frame) + seconds * fps;
        for (int i = 0; i < source.Frame; i++) time += frames.GetFrameDuration(source.Animation, i);
        time %= cycle;
        for (int i = 0; i < count; i++)
        {
            double duration = frames.GetFrameDuration(source.Animation, i);
            if (time < duration) { source.SetFrameAndProgress(i, (float)(time / duration)); return; }
            time -= duration;
        }
    }
}
