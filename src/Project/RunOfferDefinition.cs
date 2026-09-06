using Godot;

namespace TowerAutobattler.Project;

public enum RunOfferKind { Recruitment, Shop, CombatReward, Event, Rest }
public enum RunPoolAction { Recruit, GrantItem, BuyItem }

[GlobalClass]
public partial class RunOfferDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public RunOfferKind Kind { get; set; }
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public bool AllowSkip { get; set; } = true;
    [Export] public bool Repeatable { get; set; }
    [Export] public ContentPoolDefinition? Pool { get; set; }
    [Export] public RunPoolAction PoolAction { get; set; }
    [Export] public int PoolChoiceCount { get; set; } = 3;
    [Export] public RunChoiceDefinition[] Choices { get; set; } = [];
}
