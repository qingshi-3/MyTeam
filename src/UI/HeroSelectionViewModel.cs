using TowerAutobattler.Content;
using TowerAutobattler.Battle;

namespace TowerAutobattler.UI;

public sealed record HeroSelectionViewModel(
    string StableId,
    UnitDefinition Definition,
    bool Unlocked,
    string RuleTitle,
    string RuleDescription,
    UnitSnapshot? Snapshot = null,
    int RecruitmentTier = 0);
