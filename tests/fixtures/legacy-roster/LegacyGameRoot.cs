using System.Threading.Tasks;
using TowerAutobattler.Composition;
using TowerAutobattler.App;

// Frozen regression package; never referenced by production scenes.
public partial class LegacyGameRoot : GameRoot
{
    protected override Task<GamePackagePublicationResult> PublishPackageAsync() => TestProjectFixture.PublishAsync(this);
}
