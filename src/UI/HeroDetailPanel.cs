using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class HeroDetailPanel : PanelContainer
{
    [Signal] public delegate void DeployRequestedEventHandler(string stableId);
    [Export] public PackedScene TraitBadgeScene { get; set; } = null!;

    private UnitPortrait _portrait = null!;
    private Label _name = null!;
    private Container _traits = null!;
    private StatBlock _health = null!;
    private StatBlock _damage = null!;
    private StatBlock _reach = null!;
    private Label _description = null!;
    private Label _ruleName = null!;
    private Label _ruleCopy = null!;
    private HeroAbilityPanel _ability = null!;
    private Label _availability = null!;
    private Button _deploy = null!;
    private string _stableId = string.Empty;

    public override void _Ready()
    {
        CacheNodes();
        _deploy.Pressed += OnDeployPressed;
    }

    public override void _ExitTree() => _deploy.Pressed -= OnDeployPressed;

    public void Bind(HeroSelectionViewModel model)
    {
        CacheNodes();
        _stableId = model.StableId;
        _portrait.Bind(model.Definition.Portrait, model.Definition.Icon);
        _name.Text = model.Definition.DisplayName;
        _description.Text = model.Definition.Description;
        _health.Bind(SemanticIconKeys.Health, model.Definition.MaxHealth.ToString("0"), "生命", "HealthValue");
        _damage.Bind(SemanticIconKeys.Damage, model.Definition.AttackDamage.ToString("0"), "伤害", "DamageValue");
        _reach.Bind(SemanticIconKeys.Reach, model.Definition.AttackRange.ToString("0.#"), "攻击距离", "RangeValue");
        _ruleName.Text = string.IsNullOrWhiteSpace(model.RuleTitle) ? "军团规则" : model.RuleTitle;
        _ruleCopy.Text = model.RuleDescription;
        _ability.Bind(model.CommandName, model.CommandDescription, model.ManaCost, model.GoldCost);
        _availability.Text = model.Unlocked ? "已解锁 · 可以出征" : "未解锁 · 仅可预览";
        _availability.ThemeTypeVariation = model.Unlocked ? "HealingValue" : "DangerValue";
        _deploy.Disabled = !model.Unlocked;
        _deploy.Text = model.Unlocked ? "以该英雄出征" : "尚未解锁";
        BindTraits(model);
        TooltipText = model.Definition.Description;
    }

    private void BindTraits(HeroSelectionViewModel model)
    {
        foreach (var child in _traits.GetChildren())
        {
            _traits.RemoveChild(child);
            child.Free();
        }
        var role = TraitBadgeScene.Instantiate<TraitBadge>();
        _traits.AddChild(role);
        role.Bind(UnitSemanticFacts.Responsibility(model.Definition.Role));
        foreach (var fact in UnitSemanticFacts.Traits(model.Definition.Faction, model.Definition.Tags))
        {
            var badge = TraitBadgeScene.Instantiate<TraitBadge>();
            _traits.AddChild(badge);
            badge.Bind(fact);
        }
    }

    private void OnDeployPressed()
    {
        if (!_deploy.Disabled && !string.IsNullOrWhiteSpace(_stableId))
            EmitSignal(SignalName.DeployRequested, _stableId);
    }

    private void CacheNodes()
    {
        _portrait ??= GetNode<UnitPortrait>("%DetailPortrait");
        _name ??= GetNode<Label>("%DetailName");
        _traits ??= GetNode<Container>("%DetailTraits");
        _health ??= GetNode<StatBlock>("%HealthStat");
        _damage ??= GetNode<StatBlock>("%DamageStat");
        _reach ??= GetNode<StatBlock>("%ReachStat");
        _description ??= GetNode<Label>("%HeroDescription");
        _ruleName ??= GetNode<Label>("%RuleName");
        _ruleCopy ??= GetNode<Label>("%RuleCopy");
        _ability ??= GetNode<HeroAbilityPanel>("%HeroAbilityPanel");
        _availability ??= GetNode<Label>("%Availability");
        _deploy ??= GetNode<Button>("%DeployButton");
    }
}
