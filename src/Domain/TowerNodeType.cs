namespace TowerAutobattler.Domain;

// Shared authored/runtime identity. Project composition, Run progression, and
// Battle transition DTOs depend on this neutral contract instead of each other.
public enum TowerNodeType
{
    Combat,
    Elite,
    Recruitment,
    Shop,
    Event,
    Rest,
    Boss
}
