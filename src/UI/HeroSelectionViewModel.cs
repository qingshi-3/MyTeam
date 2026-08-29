using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public sealed record HeroSelectionViewModel(
    string StableId,
    UnitDefinition Definition,
    bool Unlocked,
    string RuleTitle,
    string RuleDescription,
    string CommandName,
    string CommandDescription,
    int ManaCost,
    int GoldCost);
