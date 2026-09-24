using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Vfx;

namespace TowerAutobattler.Presentation;

public partial class RangedAttackLayer
{
    private readonly Dictionary<string, HookVisual> _hooks = [];
    private readonly Dictionary<string, (string Owner, VfxContext Context)> _duelMarks = [];
    public int TechniqueVfxCount => _hooks.Count + _duelMarks.Count;
    private sealed class HookVisual(Vector2 source)
    {
        public Vector2 Source = source, From = source, To = source, Rendered = source;
        public string Caught = "";
        public float Elapsed;
        public bool Ending;
    }
    private void AdvanceTechniqueVisuals(float seconds)
    {
        foreach (var (id, hook) in _hooks.ToArray())
        {
            var key = "hook:" + id;
            if (hook.Ending && hook.Elapsed >= BattleTiming.TickSeconds && seconds > 0)
            { _hooks.Remove(id); _player.End(key, VfxEndReason.Completed); continue; }
            hook.Elapsed = Mathf.Min(BattleTiming.TickSeconds, hook.Elapsed + seconds);
            hook.Rendered = hook.From.Lerp(hook.To, hook.Elapsed / BattleTiming.TickSeconds);
            Vector2? display = hook.Caught.Length == 0 ? null : _bodyDisplayPosition?.Invoke(hook.Caught);
            _player.UpdateContext(key, new(hook.Source, hook.Rendered, TargetDisplayPosition: display));
        }
        foreach (var (key, mark) in _duelMarks)
            _player.UpdateContext(key, WithDisplayPosition(mark.Owner, mark.Context, true));
    }
    private void PresentTechnique(BattleEvent fact, bool snap)
    {
        var key = "hook:" + fact.SourceRuntimeId;
        switch (fact.Type)
        {
            case "hook_move": case "hook_return":
                if (!_hooks.TryGetValue(fact.SourceRuntimeId, out var hook))
                {
                    hook = new(fact.Origin); _hooks.Add(fact.SourceRuntimeId, hook);
                    _player.Play("mechanical_hook", new(fact.Origin, fact.Origin), key);
                }
                hook.From = hook.Rendered; hook.To = fact.Position; hook.Source = fact.Origin;
                hook.Elapsed = snap ? BattleTiming.TickSeconds : 0; hook.Caught = fact.TargetRuntimeId;
                break;
            case "hook_caught":
                _player.Play("impact", WithDisplayPosition(fact.TargetRuntimeId, new(fact.Origin, fact.Position)));
                break;
            case "hook_end":
                if (_hooks.TryGetValue(fact.SourceRuntimeId, out var ending))
                { ending.From = ending.Rendered; ending.To = fact.Position; ending.Elapsed = 0; ending.Ending = true; }
                break;
            case "duel_begin":
                foreach (var (owner, point) in new[] { (fact.SourceRuntimeId, fact.Origin), (fact.TargetRuntimeId, fact.Position) })
                {
                    var markKey = $"duel:{fact.SourceRuntimeId}:{owner}";
                    var context = new VfxContext(point, point);
                    _duelMarks[markKey] = (owner, context);
                    _player.Play("duel_mark", WithDisplayPosition(owner, context, true), markKey);
                }
                break;
            case "duel_end":
                foreach (var owner in new[] { fact.SourceRuntimeId, fact.TargetRuntimeId })
                {
                    var markKey = $"duel:{fact.SourceRuntimeId}:{owner}";
                    _duelMarks.Remove(markKey); _player.End(markKey, VfxEndReason.Completed);
                }
                break;
            case "counter_strike":
                _player.Play("impact", WithDisplayPosition(fact.TargetRuntimeId, new(fact.Origin, fact.Position)));
                break;
        }
    }
    private void ClearTechniqueVisuals() { _hooks.Clear(); _duelMarks.Clear(); }
}
