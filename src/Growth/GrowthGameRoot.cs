using System.Threading.Tasks;
using TowerAutobattler.App;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;

namespace TowerAutobattler.Growth;

public partial class GrowthGameRoot : GameRoot
{
    protected override Task<GamePackagePublicationResult> PublishPackageAsync() =>
        GrowthContentPackage.CreateReadyAsync(this);

    protected override CompiledGrowthRules? CreateGrowthRules(ContentRegistry content) =>
        GrowthContentPackage.LoadRules(content);
}
