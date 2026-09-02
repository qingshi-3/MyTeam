using Godot;
using TowerAutobattler.TacticalCommands;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class RunRulesDefinition : Resource
{
    [Export] public int OrdinaryPopulationCap { get; set; } = 10;
    [Export] public int PhysicalDeploymentCeiling { get; set; } = 18;
    [Export] public int ReserveCapacity { get; set; } = 3;
    [Export] public int StarterRosterHeroCount { get; set; } = 3;
    [Export(PropertyHint.Range, "1,10,1")] public int InitialPopulation { get; set; } = 7;
    [Export(PropertyHint.Range, "1,6,1")] public int EquipmentSlotCapacity { get; set; } = 3;
    [Export] public TacticalCommandDefinition[] StarterTacticalCommands { get; set; } = [];
    [Export] public LegacyHeroTacticalCommandMapping[] LegacyHeroTacticalCommandMappings { get; set; } = [];
    [Export] public int RecruitmentChoiceCount { get; set; } = 3;
    [Export] public int ItemChoiceCount { get; set; } = 3;
    [Export] public int StartingGold { get; set; } = 16;
    [Export] public int NormalBattleGold { get; set; } = 7;
    [Export] public int EliteBattleGold { get; set; } = 12;
    [Export] public int BossBattleGold { get; set; } = 18;
    [Export(PropertyHint.Range, "0,1,0.01")] public float VictoryHeroRecovery { get; set; } = .12f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float VictorySoldierRecovery { get; set; } = .15f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float MinimumVictoryHeroHealth { get; set; } = .15f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float MinimumLivingSoldierHealth { get; set; } = .1f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float DefeatedSoldierHealth { get; set; } = .25f;
    [Export] public int RiskyEventSuccessGold { get; set; } = 18;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RiskyEventSuccessChance { get; set; } = .65f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RiskyEventHealthLoss { get; set; } = .25f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RiskyEventMinimumHealth { get; set; } = .25f;
    [Export] public int SafeEventGold { get; set; } = 6;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RestHeroHealing { get; set; } = .35f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RestSoldierHealing { get; set; } = .45f;
    [Export] public int RestGold { get; set; } = 8;
    [Export] public int InitialUnlockedHeroCount { get; set; } = 3;
}
