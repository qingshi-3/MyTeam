using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Composition;

namespace TowerAutobattler.Growth;

public partial class GrowthGameRoot : GameRoot
{
    // The composition root owns the authored rules for its entire lifetime,
    // including failed bootstraps and retries; compiled rules carry no Resource state.
    [Export] public GrowthRulesDefinition? RulesDefinition { get; set; }
    [Export] public GrowthRulesDefinition? LegacyRulesDefinition { get; set; }
    private AuthoredContentPackage? _authoredPackage;

    protected override Task<GamePackagePublicationResult> PublishPackageAsync() =>
        GamePackagePublisher.CreateAuthoredReadyAsync(this,
            _authoredPackage ??= GrowthContentPackage.LoadAuthored(RulesDefinition, LegacyRulesDefinition));

    public override void _ExitTree()
    {
        base._ExitTree();
        _authoredPackage = null;
    }
}
