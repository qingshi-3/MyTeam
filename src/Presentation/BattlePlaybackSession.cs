using System;
using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Presentation;

// Owns one simulation lifetime and its fixed-step clock, independently of any screen.
internal sealed class BattlePlaybackSession
{
    public const float NormalSimulationScale = .8f;
    private double _accumulator;
    public BattleSimulation? Simulation { get; private set; }
    public float Speed { get; private set; } = 1;
    public float SimulationScale => Speed * NormalSimulationScale;
    public bool Paused { get; private set; }
    public bool Ending { get; private set; }
    public BattleResult? Result { get; private set; }
    public bool CanAdvance => !Ending && !Paused && Simulation?.Outcome == BattleOutcome.Running;

    public void Start(BattleConfig config, float speed)
    {
        if (Simulation is not null) throw new InvalidOperationException("Stop the previous playback session before starting another.");
        Simulation = new BattleSimulation(config);
        _accumulator = 0;
        Speed = NormalizeSpeed(speed);
        Paused = false;
        ResetTerminal();
    }

    public void SetPaused(bool paused)
    {
        if (!Ending && Simulation?.Outcome == BattleOutcome.Running) Paused = paused;
    }

    public void SetSpeed(float speed) { if (!Ending) Speed = NormalizeSpeed(speed); }

    public void Advance(double delta, Action afterTick)
    {
        if (!CanAdvance) return;
        _accumulator += delta * SimulationScale;
        var steps = 0;
        while (_accumulator >= BattleTiming.TickSeconds && steps++ < 12 && Simulation!.Outcome == BattleOutcome.Running)
        {
            _accumulator -= BattleTiming.TickSeconds;
            Simulation.Step();
            afterTick();
        }
    }

    public bool BeginTerminal()
    {
        if (Ending || Simulation is null || Simulation.Outcome == BattleOutcome.Running) return false;
        Ending = true;
        _accumulator = 0;
        Result = Simulation.CreateResult();
        return true;
    }

    public void ResetTerminal() { Ending = false; Result = null; }

    public void Stop(bool replacement)
    {
        if (replacement)
            try { Simulation?.Replace(); }
            catch (Exception exception) { GD.PushError($"Battle replacement failed: {exception.Message}"); }
        try { Simulation?.Dispose(); }
        catch (Exception exception) { GD.PushError($"Battle abort failed: {exception.Message}"); }
        Simulation = null;
        _accumulator = 0;
    }

    private static float NormalizeSpeed(float speed) => speed >= 4f ? 4f : speed >= 2f ? 2f : 1f;
}
