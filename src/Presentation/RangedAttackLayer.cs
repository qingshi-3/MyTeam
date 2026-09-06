using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Presentation;

public partial class RangedAttackLayer : Node2D
{
    [Export] public PackedScene ProjectileScene { get; set; } = null!;
    [Export] public PackedScene BeamScene { get; set; } = null!;
    [Export] public PackedScene ImpactScene { get; set; } = null!;
    private readonly Dictionary<int, RangedAttackVisual> _projectiles = [];
    private readonly List<RangedAttackVisual> _flashes = [];
    private BattleBoard? _board;
    private bool _paused;
    private float _speed = .8f;
    public int ProjectileCount => _projectiles.Count;

    public void Bind(BattleBoard board) => _board = board;
    public void SetClock(bool paused, float speed) { _paused = paused; _speed = speed; }

    public void Present(IReadOnlyList<BattleEvent> events, bool snap)
    {
        foreach (var fact in events)
        {
            switch (fact.Type)
            {
                case "projectile_spawn":
                    if (!_projectiles.ContainsKey(fact.EntityId))
                        _projectiles.Add(fact.EntityId, Spawn(ProjectileScene, fact.Origin, fact.Position));
                    break;
                case "projectile_move":
                    if (_projectiles.TryGetValue(fact.EntityId, out var moving)) moving.MoveTo(fact.Position, snap);
                    break;
                case "projectile_impact":
                    Flash(ImpactScene, fact.Position, fact.Position);
                    break;
                case "projectile_end":
                    if (_projectiles.Remove(fact.EntityId, out var ended)) ended.Free();
                    break;
                case "beam":
                    Flash(BeamScene, fact.Origin, fact.Position);
                    Flash(ImpactScene, fact.Position, fact.Position);
                    break;
                case "battle_finished":
                    ClearProjectiles();
                    break;
            }
        }
        Refresh(0);
    }

    private RangedAttackVisual Spawn(PackedScene scene, Vector2 origin, Vector2 position)
    {
        var visual = scene.Instantiate<RangedAttackVisual>();
        AddChild(visual);
        visual.Bind(origin, position);
        return visual;
    }

    private void Flash(PackedScene scene, Vector2 origin, Vector2 position)
    {
        // Only transient decoration is capped; every active gameplay projectile keeps its own node.
        if (_flashes.Count >= 96) { _flashes[0].Free(); _flashes.RemoveAt(0); }
        _flashes.Add(Spawn(scene, origin, position));
    }

    public override void _Process(double delta) => Refresh(_paused ? 0 : (float)delta * _speed);

    private void Refresh(float seconds)
    {
        if (_board is null) return;
        foreach (var projectile in _projectiles.Values) projectile.Advance(seconds, _board);
        foreach (var flash in _flashes.ToArray())
            if (!flash.Advance(seconds, _board)) { _flashes.Remove(flash); flash.Free(); }
    }

    private void ClearProjectiles()
    {
        foreach (var projectile in _projectiles.Values) projectile.Free();
        _projectiles.Clear();
    }

    public void Clear()
    {
        ClearProjectiles();
        foreach (var flash in _flashes) flash.Free();
        _flashes.Clear();
    }

    public override void _ExitTree() => Clear();
}
