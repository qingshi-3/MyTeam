using System.Collections.Generic;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Vfx;

namespace TowerAutobattler.Presentation;

// Compatibility facade; all effect instances belong to the shared player.
public partial class RangedAttackLayer : Node2D, IVfxStage
{
    [Export] public Vector2 BodyOffset { get; set; } = new(0, -22);
    private VfxPlayer _player = null!;
    private BattleBoard? _board;
    private readonly HashSet<int> _projectiles = [];
    private readonly HashSet<string> _shields = [];
    public int ProjectileCount => _projectiles.Count;
    public float UnitScale => _board?.CurrentProjection.UnitScale ?? 1;
    public Vector2 Project(Vector2 point, bool ground) =>
        (_board?.LogicalToLocal(point) ?? point) + (ground ? Vector2.Zero : BodyOffset * UnitScale);
    public float RadiusPixels(float radius) => _board is null ? radius :
        _board.LogicalToLocal(new Vector2(radius, 0)).DistanceTo(_board.LogicalToLocal(Vector2.Zero));
    public override void _Ready() => _player = GetNode<VfxPlayer>("Player");
    public void Bind(BattleBoard board) { _board = board; _player.Bind(this); }
    public void SetClock(bool paused, float speed) { _player.Paused = paused; _player.Speed = speed; }
    public void SynchronizeUnits(IEnumerable<BattleUnitState> units)
    {
        foreach (var unit in units)
        {
            var key = "shield:" + unit.RuntimeId;
            if (unit.Alive && unit.Shield > 0)
            {
                _shields.Add(unit.RuntimeId);
                _player.Play("shield", new(unit.Position, unit.Position), key);
            }
            else if (_shields.Remove(unit.RuntimeId))
                _player.End(key, unit.Alive ? VfxEndReason.Depleted : VfxEndReason.OwnerDefeated);
        }
    }
    public void Present(IReadOnlyList<BattleEvent> events, bool snap)
    {
        foreach (var fact in events)
        {
            var context = new VfxContext(fact.Origin, fact.Position, fact.Vfx?.Radius ?? 0);
            var projectile = "projectile:" + fact.EntityId;
            var shield = "shield:" + fact.TargetRuntimeId;
            if (fact.Vfx is { } cue)
            {
                if (cue.Phase == BattleVfxPhase.ShieldActive) _shields.Add(fact.TargetRuntimeId);
                if (cue.Phase == BattleVfxPhase.ShieldDepleted) _shields.Remove(fact.TargetRuntimeId);
                VfxBindingResolver.Present(_player, cue, context, fact.TargetRuntimeId);
            }
            switch (fact.Type)
            {
                case "projectile_spawn":
                    _projectiles.Add(fact.EntityId);
                    _player.Play("projectile", context, projectile); break;
                case "projectile_move": _player.UpdateContext(projectile, context); break;
                case "projectile_impact": _player.Play("impact", context); break;
                case "projectile_end":
                    _projectiles.Remove(fact.EntityId);
                    _player.End(projectile, VfxEndReason.ScopeEnded); break;
                case "beam":
                    _player.Play("beam", context);
                    _player.Play("impact", context); break;
                case "heal": _player.Play("heal", context); break;
                case "defeated":
                    _shields.Remove(fact.TargetRuntimeId);
                    _player.End(shield, VfxEndReason.OwnerDefeated); break;
                case "battle_finished": Clear(); break;
            }
        }
        if (snap) _player.Advance(TowerAutobattler.Domain.BattleTiming.TickSeconds);
    }
    public void Clear()
    {
        _player?.Clear();
        _projectiles.Clear();
        _shields.Clear();
    }
}

