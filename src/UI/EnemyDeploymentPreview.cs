using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class EnemyDeploymentPreview : Button
{
    public event System.Action<string>? Selected;
    public string InstanceId { get; private set; } = string.Empty;
    public float BodyRadius { get; private set; } = .32f;

    private UnitPortrait _portrait = null!;
    private TextureRect _enemyBadge = null!;
    private TextureRect _roleBadge = null!;
    private TextureRect _reachBadge = null!;

    public override void _Ready()
    {
        CacheNodes();
        Pressed += OnSelected;
    }

    public override void _ExitTree() => Pressed -= OnSelected;
    private void OnSelected() => Selected?.Invoke(InstanceId);

    public void Bind(EnemyDeploymentViewModel model)
    {
        CacheNodes();
        InstanceId = model.InstanceId;
        BodyRadius = model.BodyRadius;
        _portrait.FitVisibleArtwork = model.BodyRadius > .5f;
        _portrait.Bind(model.Portrait, SemanticIcons.Catalog.ResolveIcon(
            SemanticIconKeys.Responsibility(model.Role)));
        _enemyBadge.Texture = SemanticIcons.Catalog.ResolveIcon(
            model.IsBoss ? SemanticIconKeys.Responsibility(model.Role) : SemanticIconKeys.Deaths);
        _roleBadge.Texture = SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Responsibility(model.Role));
        _reachBadge.Texture = SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Reach);
        TooltipText = $"敌方 · {model.DisplayName} · {PlayerFacingText.DescribeUnitRole(model.Role)} · " +
                      $"{UnitRangeClassifier.Describe(model.AttackRange)} {model.AttackRange:0.#} 格\n点击查看属性与完整技能";
    }

    private void CacheNodes()
    {
        _portrait ??= GetNode<UnitPortrait>("%EnemyPortrait");
        _enemyBadge ??= GetNode<TextureRect>("%EnemyBadge");
        _roleBadge ??= GetNode<TextureRect>("%RoleBadge");
        _reachBadge ??= GetNode<TextureRect>("%ReachBadge");
    }

    public override bool _HasPoint(Vector2 point) => BodyRadius <= .5f
        ? new Rect2(Vector2.Zero, Size).HasPoint(point)
        : ((point - Size * .5f) / (Size * .5f)).LengthSquared() <= 1;
}
