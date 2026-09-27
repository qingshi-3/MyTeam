using System.Threading.Tasks;
using TowerAutobattler.App;
using TowerAutobattler.Composition;

namespace TowerAutobattler.ValidationMatrix;

public partial class ValidationMatrixRoot : GameRoot
{
    protected override Task<GamePackagePublicationResult> PublishPackageAsync()=>ValidationMatrixPackage.CreateReadyAsync(this);
}
