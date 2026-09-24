using System.Threading.Tasks;
using TowerAutobattler.Composition;
using TowerAutobattler.Experience;

// Frozen regression package; never referenced by production scenes.
public partial class LegacyExperienceSliceRoot : ExperienceSliceRoot
{
    protected override Task<GamePackagePublicationResult> PublishPackageAsync() => TestProjectFixture.PublishAsync(this);
}
