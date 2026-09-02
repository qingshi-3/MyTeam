namespace TowerAutobattler.Domain;

// Neutral fixed-step contract shared by authoring descriptions, battle runtime,
// presentation, and content adapters. No product compiler depends on BattleSimulation.
public static class BattleTiming
{
    public const float TickSeconds = 0.1f;
}
