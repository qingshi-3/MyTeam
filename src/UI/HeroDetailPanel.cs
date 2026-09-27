using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.UI;

public partial class HeroDetailPanel : PanelContainer
{
    [Signal] public delegate void DeployRequestedEventHandler(string stableId);

    private CombatRichText _ruleCopy = null!;
    private FoldableContainer _rulePanel = null!;
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
        var identityChanged = _stableId != model.StableId;
        _stableId = model.StableId;
        var snapshot = model.Snapshot ?? BattleSetupFactory.Snapshot(model.Definition);
        GetNode<UnitDetailView>("%UnitDetails").Bind(new UnitInformation(model.StableId, model.Definition, snapshot));
        _rulePanel.Title = string.IsNullOrWhiteSpace(model.RuleTitle) ? "军团规则" : model.RuleTitle;
        if (identityChanged) _rulePanel.Folded = true;
        _ruleCopy.Text = model.RuleDescription;
        // Legacy rule prose often repeats a skill and may lag behind the published loadout.
        // Show it only for older units that have no compiled or component ability to inspect.
        _rulePanel.Visible = !string.IsNullOrWhiteSpace(model.RuleDescription) &&
            (model.AlwaysShowRule || snapshot.AbilityLoadout is null && snapshot.AttackHitGrowth is null);
        _availability.Text = model.Unlocked ? "已解锁 · 可以出征" : "未解锁 · 仅可预览";
        _availability.Visible = !model.Unlocked;
        _availability.ThemeTypeVariation = model.Unlocked ? "HealingValue" : "DangerValue";
        _deploy.Disabled = !model.Unlocked;
        _deploy.Text = model.Unlocked ? "以该英雄出征" : "尚未解锁";
    }

    public void SetOpeningAction(int tier, bool selected, bool full)
    {
        _deploy.ThemeTypeVariation = "SecondaryButton";
        _availability.Visible = false;
        _deploy.Disabled = full && !selected;
        _deploy.Text = selected ? "取消选择" : full ? "已选满两名" : "选择这名英雄";
    }

    private void OnDeployPressed()
    {
        if (!_deploy.Disabled && !string.IsNullOrWhiteSpace(_stableId))
            EmitSignal(SignalName.DeployRequested, _stableId);
    }

    private void CacheNodes()
    {
        _ruleCopy ??= GetNode<CombatRichText>("%RuleCopy");
        _rulePanel ??= GetNode<FoldableContainer>("%RulePanel");
        _availability ??= GetNode<Label>("%Availability");
        _deploy ??= GetNode<Button>("%DeployButton");
    }
}
