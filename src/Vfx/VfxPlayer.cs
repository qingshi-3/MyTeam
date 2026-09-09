using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

public partial class VfxPlayer : Node2D
{
    [Export] public VfxCatalog Catalog { get; set; } = null!;
    public bool Paused { get; set; }
    public float Speed { get; set; } = 1;
    public bool ReducedMotion { get; set; }
    public int ActiveCount => _instances.Count;
    public VfxPlaybackState? PlaybackFor(string key) => _instances.TryGetValue(key, out var instance) ? instance.Playback : null;
    private readonly Dictionary<string, VfxInstance> _instances = [];
    private IVfxStage? _stage;
    private long _sequence;
    public void Bind(IVfxStage stage) { Catalog.Validate(); _stage = stage; }
    public string Play(string id, VfxContext context, string? key = null)
    {
        key ??= $"burst:{++_sequence}";
        if (_instances.TryGetValue(key, out var existing)) { existing.Context = context; return key; }
        var definition = Catalog.Find(id);
        // Bound transient work, but never drop an owned persistent effect.
        if (!definition.Persistent && _instances.Count >= 192) return key;
        var instance = definition.Scene.Instantiate<VfxInstance>();
        try { instance.Bind(definition, context); AddChild(instance); }
        catch { instance.Free(); throw; }
        _instances.Add(key, instance);
        if (_stage is not null) instance.Advance(0, _stage, ReducedMotion);
        return key;
    }
    public void UpdateContext(string key, VfxContext context)
    {
        if (_instances.TryGetValue(key, out var instance)) instance.Context = context;
    }
    public void Impact(string key) { if (_instances.TryGetValue(key, out var instance)) instance.Impact(); }
    public void Signal(string key, VfxStartCue cue)
    {
        if (_instances.TryGetValue(key, out var instance)) instance.Signal(cue);
    }
    public void End(string key, VfxEndReason reason)
    {
        if (_instances.Remove(key, out var instance))
        {
            if (reason == VfxEndReason.ScopeEnded) { instance.Free(); return; }
            instance.End(reason);
            _instances.Add($"release:{++_sequence}", instance);
        }
    }
    public override void _Process(double delta) => Advance(Paused ? 0 : (float)delta * Speed);
    public void Advance(float seconds)
    {
        if (!float.IsFinite(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (_stage is null) return;
        foreach (var pair in _instances.ToArray())
            if (!pair.Value.Advance(Math.Max(0, seconds), _stage, ReducedMotion))
            { _instances.Remove(pair.Key); pair.Value.Free(); }
    }
    public void Clear()
    {
        foreach (var instance in _instances.Values) instance.Free();
        _instances.Clear();
    }
    public override void _ExitTree() => Clear();
}
