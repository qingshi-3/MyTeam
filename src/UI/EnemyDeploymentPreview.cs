using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class EnemyDeploymentPreview : Control
{
    public string InstanceId { get; private set; } = string.Empty;

    private UnitPortrait _portrait = null!;
    private TextureRect _enemyBadge = null!;
    private TextureRect _roleBadge = null!;
    private TextureRect _reachBadge = null!;

    public override void _Ready() => CacheNodes();

    public void Bind(EnemyDeploymentViewModel model)
    {
        CacheNodes();
        InstanceId = model.InstanceId;
        _portrait.Bind(model.Portrait, SemanticIcons.Catalog.ResolveIcon(
            SemanticIconKeys.Responsibility(model.Role)));
        _enemyBadge.Texture = SemanticIcons.Catalog.ResolveIcon(
            model.IsBoss ? SemanticIconKeys.Responsibility(model.Role) : SemanticIconKeys.Deaths);
        _roleBadge.Texture = SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Responsibility(model.Role));
        _reachBadge.Texture = SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Reach);
        TooltipText = $"敌方 · {model.DisplayName} · {PlayerFacingText.DescribeUnitRole(model.Role)} · " +
                      $"{UnitRangeClassifier.Describe(model.AttackRange)} {model.AttackRange:0.#} 格";
    }

    private void CacheNodes()
    {
        _portrait ??= GetNode<UnitPortrait>("%EnemyPortrait");
        _enemyBadge ??= GetNode<TextureRect>("%EnemyBadge");
        _roleBadge ??= GetNode<TextureRect>("%RoleBadge");
        _reachBadge ??= GetNode<TextureRect>("%ReachBadge");
    }
}
