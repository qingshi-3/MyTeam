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
    private bool _loop = true;
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
        _player.Bind(this);
        Filter("");
        GetNode<LineEdit>("%Search").TextChanged += Filter;
        _list.ItemSelected += index => { _selected = _visible[(int)index].StableId; Replay(); };
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
            _player.Advance(1f / 30);
            _elapsed += 1f / 30;
        };
        GetNode<Button>("%Impact").Pressed += () => _player.Impact("preview");
        GetNode<Button>("%End").Pressed += () => _player.End("preview", VfxEndReason.Completed);
        GetNode<Button>("%Break").Pressed += () => _player.End("preview", VfxEndReason.Depleted);
        GetNode<Button>("%Return").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/app/GameRoot.tscn");
        GetNode<CheckButton>("%Loop").Toggled += value => _loop = value;
        GetNode<CheckButton>("%Reduced").Toggled += value => _player.ReducedMotion = value;
        GetNode<CheckButton>("%Light").Toggled += value => GetNode<ColorRect>("%Backdrop").Color =
            value ? new Color("c9c6bf") : new Color("121c2b");
        GetNode<HSlider>("%Speed").ValueChanged += value => _player.Speed = (float)value;
        GetNode<HSlider>("%Radius").ValueChanged += value => { _radius = (float)value; Replay(); };
        GetNode<HSlider>("%Distance").ValueChanged += value => { _distance = (float)value; Replay(); };
        GetNode<Button>("%Skill").Pressed += () =>
        {
            _selected = RendAbility.Presentation?.DamageVfx ?? throw new InvalidOperationException("Missing skill VFX binding.");
            Replay();
            _player.Clear();
            VfxBindingResolver.Present(_player,
                new TowerAutobattler.Battle.BattleVfxCue(TowerAutobattler.Battle.BattleVfxPhase.Burst, _selected),
                new(new(-_distance / 2, 0), new(_distance / 2, 0)), "preview");
            _details.Text = RendAbility.DisplayName + " · 成功伤害的表现绑定\n" + _details.Text;
        };
        GetNode<Button>("%Reset").Pressed += () =>
        {
            GetNode<HSlider>("%Radius").Value = 1.5;
            GetNode<HSlider>("%Distance").Value = 3;
            GetNode<HSlider>("%Speed").Value = 1;
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
        var definition = _player.Catalog.Find(_selected);
        _player.Play(_selected, new(new(-_distance / 2, 0), new(_distance / 2, 0),
            definition.Ground ? _radius : 0), "preview");
        _details.Text = $"{definition.DisplayName}  ·  {(definition.Persistent ? "持续效果" : "瞬时效果")}\n半径 {_radius:0.0} 格 · 距离 {_distance:0.0} 格\n参数仅本次预览有效";
        foreach (var name in new[] { "Impact", "End", "Break" })
            GetNode<Button>("%" + name).Disabled = !definition.Persistent;
        PositionUnits();
    }
    private void PositionUnits()
    {
        // These are centered body sprites, not full unit roots. The production
        // UnitAnimationComponent already offsets its sprite above the ground.
        GetNode<Node2D>("%SourceUnit").Position = Project(new(-_distance / 2, 0), false);
        GetNode<Node2D>("%TargetUnit").Position = Project(new(_distance / 2, 0), false);
    }
    public override void _Process(double delta)
    {
        if (!_player.Paused) _elapsed += (float)delta * _player.Speed;
        PositionUnits();
        // Sustained effects already animate continuously on their own clock.
        // Recreating them periodically cuts the orbit and repeats the entrance.
        // After explicit End/Break, only an explicit Replay should restore one.
        var definition = _player.Catalog.Find(_selected);
        if (_loop && !definition.Persistent && _elapsed > Mathf.Max(1.6f, definition.Duration + .35f)) Replay();
        GetNode<Label>("%Clock").Text = $"{_elapsed:0.00} 秒  ·  {_player.Speed:0.00}×  ·  {_player.ActiveCount} 实例";
    }
}
