using System;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

public enum FloorCellPreview { Normal, Blocked, Hazard, Objective }

public interface IBattleFloorRuleRuntime
{
    string Id { get; }
    string DisplayName { get; }
    string PreviewText { get; }
    bool CanOccupy(Vector2I cell);
    FloorCellPreview GetCellPreview(Vector2I cell);
    void OnBattleStarted(BattleRuleContext context);
    void OnTick(BattleRuleContext context);
    void OnBattleEnded(BattleRuleContext context, BattleOutcome outcome);
    float ModifyIncomingDamage(BattleRuleContext context, BattleUnitState target, float rawDamage);
}

public sealed class BattleRuleContext
{
    private readonly Func<int, IEnumerable<BattleUnitState>> _allies;
    private readonly Action<string, BattleUnitState, float> _damage;
    private readonly Action<BattleUnitState, float> _heal;
    private readonly Action<string, string, string, float, Vector2I, string> _emit;
    private readonly Func<int, bool> _beaconControlled;

    public int Tick { get; }
    public IReadOnlyList<BattleUnitState> Units { get; }

    public BattleRuleContext(
        int tick, IReadOnlyList<BattleUnitState> units,
        Func<int, IEnumerable<BattleUnitState>> allies,
        Action<string, BattleUnitState, float> damage,
        Action<BattleUnitState, float> heal,
        Action<string, string, string, float, Vector2I, string> emit,
        Func<int, bool> beaconControlled)
    {
        Tick = tick; Units = units; _allies = allies; _damage = damage; _heal = heal; _emit = emit; _beaconControlled = beaconControlled;
    }

    public IEnumerable<BattleUnitState> Allies(int team) => _allies(team);
    public void Damage(string source, BattleUnitState target, float amount) => _damage(source, target, amount);
    public void Heal(BattleUnitState target, float amount) => _heal(target, amount);
    public void Emit(string type, string source, string target, float value, Vector2I cell, string cue) => _emit(type, source, target, value, cell, cue);
    public bool BeaconControlled(int team) => _beaconControlled(team);
}

public class ClearFloorRuleRuntime(string id, string name, string preview) : IBattleFloorRuleRuntime
{
    public string Id => id; public string DisplayName => name; public string PreviewText => preview;
    public virtual bool CanOccupy(Vector2I cell) => true;
    public virtual FloorCellPreview GetCellPreview(Vector2I cell) => CanOccupy(cell) ? FloorCellPreview.Normal : FloorCellPreview.Blocked;
    public virtual void OnBattleStarted(BattleRuleContext context) { }
    public virtual void OnTick(BattleRuleContext context) { }
    public virtual void OnBattleEnded(BattleRuleContext context, BattleOutcome outcome) { }
    public virtual float ModifyIncomingDamage(BattleRuleContext context, BattleUnitState target, float rawDamage) => rawDamage;
}
