using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Presentation;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.UI;

public partial class SelectedUnitPanel : PanelContainer
{
    private Label _title = null!;
    private UnitPortrait _portrait = null!;
    private ProgressBar _healthBar = null!;
    private Label _healthText = null!;
    private SemanticChip _role = null!;
    private SemanticChip _reach = null!;
    private SemanticChip _health = null!;
    private Label _identity = null!;
    private Label _stats = null!;
    private Label _equipment = null!;
    private Label _traits = null!;
    private Label _action = null!;
    private Label _statuses = null!;
    private ProgressBar _mana = null!;
    private Label _manaText = null!;
    private Label _skillStates = null!;
    private SemanticChip _shield = null!;
    private CombatRichText _detail = null!;
    private Button _ability = null!;
    private Button _passive = null!;
    private CombatRichText _activeSummary = null!;
    private CombatRichText _passiveSummary = null!;
    private ScrollContainer _scroll = null!;
    private Button[] _attributes = [];
    private Action[] _attributeActions = [];
    private readonly string[] _attributeDetails = new string[6];
    private FoldableContainer _detailSection = null!;
    private string _activeDetails = "";
    private string _passiveDetails = "";
    private GridContainer _statusTiles = null!;
    private readonly Dictionary<string, CombatFactTile> _statusViews = new(StringComparer.Ordinal);
    private CombatFactTile[] _slots = [];
    private string _boundRuntimeId = "";
    private CombatFactTile? _detailTile;
    private int _detailMode = -1;
    [Export] public PackedScene FactTileScene { get; set; } = null!;

    public override void _Ready()
    {
        _title = GetNode<Label>("%UnitTitle");
        _portrait = GetNode<UnitPortrait>("%Portrait");
        _healthBar = GetNode<ProgressBar>("%HealthBar");
        _healthText = GetNode<Label>("%HealthText");
        _role = GetNode<SemanticChip>("%UnitRoleFact");
        _reach = GetNode<SemanticChip>("%UnitReachFact");
        _health = GetNode<SemanticChip>("%UnitHealthFact");
        _identity = GetNode<Label>("%UnitIdentity");
        _stats = GetNode<Label>("%UnitStats");
        _equipment = GetNode<Label>("%UnitEquipment");
        _traits = GetNode<Label>("%UnitTraits");
        _action = GetNode<Label>("%UnitAction");
        _statuses = GetNode<Label>("%UnitStatuses");
        _mana = GetNode<ProgressBar>("%ManaBar");
        _manaText = GetNode<Label>("%ManaText");
        _skillStates = GetNode<Label>("%SkillStates");
        _shield = GetNode<SemanticChip>("%UnitShieldFact");
        _detail = GetNode<CombatRichText>("%FactDetail");
        _ability = GetNode<Button>("%UnitAbility");
        _passive = GetNode<Button>("%UnitPassive");
        _activeSummary = GetNode<CombatRichText>("%ActiveSummary");
        _passiveSummary = GetNode<CombatRichText>("%PassiveSummary");
        _scroll = GetNode<ScrollContainer>("%Scroll");
        _detailSection = GetNode<FoldableContainer>("%FactDetailSection");
        _attributes = [GetNode<Button>("%StatAttack"), GetNode<Button>("%StatArmor"),
            GetNode<Button>("%StatSpeed"), GetNode<Button>("%StatReach"),
            GetNode<Button>("%StatResistance"), GetNode<Button>("%StatMaxHealth")];
        var icons = new[] { "attack", "armor", "time", "reach", "shield", "health" };
        for (var index = 0; index < _attributes.Length; index++)
        {
            _attributes[index].Icon = SemanticIcons.Catalog.ResolveIcon(icons[index]);
            _attributes[index].ExpandIcon = true;
            _attributes[index].AddThemeConstantOverride("icon_max_width", 18);
        }
        _attributeActions = new Action[_attributes.Length];
        for (var index = 0; index < _attributes.Length; index++)
        {
            var captured = index;
            _attributeActions[index] = () => ShowAttributeDetail(captured);
            _attributes[index].Pressed += _attributeActions[index];
        }
        _health.MouseFilter = _shield.MouseFilter = MouseFilterEnum.Pass;
        _ability.Pressed += ShowAbilityDetail;
        _passive.Pressed += ShowPassiveDetail;
        _statusTiles = GetNode<GridContainer>("%StatusTiles");
        _slots = [GetNode<CombatFactTile>("%SlotOne"), GetNode<CombatFactTile>("%SlotTwo"), GetNode<CombatFactTile>("%SlotThree")];
        foreach (var slot in _slots) slot.DetailRequested += ShowDetail;
    }

    public void Bind(BattleScreenRuntimeUnitSnapshot state, UnitPortraitDefinition? portrait = null)
    {
        Visible = true;
        _title.Text = state.DisplayName;
        if (_portrait.Definition != portrait) _portrait.Bind(portrait, SemanticIcons.Catalog.ResolveIcon(state.IsHero ? "hero" : "unit"));
        _healthBar.MaxValue = Math.Max(1, state.MaxHealth);
        _healthBar.Value = state.Health;
        _healthText.Text = $"{state.Health:0} / {state.MaxHealth:0}";
        _role.Bind(UnitSemanticFacts.Responsibility(state.Role, includeLabel: false));
        _reach.Bind(UnitSemanticFacts.Reach(state.Reach));
        _health.Bind(UnitSemanticFacts.Health($"{state.Health:0}/{state.MaxHealth:0}"));
        _health.Visible = _reach.Visible = false;
        _identity.Visible = true;
        _identity.Text = $"{(state.Team == 0 ? "我方" : "敌方")} · " +
            (state.IsTemporary ? "临时单位 · 仅限本场" : state.IsHero ? "英雄" : "小兵");
        if (_boundRuntimeId != state.RuntimeId)
        {
            _boundRuntimeId = state.RuntimeId;
            _detailTile = null;
            _detailMode = -1;
            _detail.Text = "点选装备或状态查看效果";
            _detailSection.Folded = true;
            // A newly selected identity starts with its attributes; combat refresh keeps the reading position.
            _scroll.ScrollVertical = 0;
        }
        BindAttribute(0, $"{state.Damage:0.#}", "攻击", "当前普攻的基础伤害。实际命中还会受技能倍率、防御与状态影响。");
        BindAttribute(1, $"{state.Armor:0.#}", "防御", "降低受到的普通伤害；真实伤害绕过防御。部分技能或反击也会读取防御。");
        BindAttribute(2, $"×{state.AttackSpeed:0.##}", "攻击速度倍率", "倍率越高，普攻间隔越短。移动、控制和施法仍会影响实际出手次数。");
        BindAttribute(3, $"{state.Reach:0.##}", "攻击距离（格）", "用于寻找能够攻击的位置。近战的接敌还会考虑双方身体边缘。");
        BindAttribute(4, $"控抗 {state.ControlResistance:P0}", "控制抗性", "降低控制持续时间；不同技能仍遵循各自的控制和位移规则。");
        BindAttribute(5, $"生命上限 {state.MaxHealth:0}", "最大生命", "当前生命容量。治疗只能补回已有的生命缺口，过量部分需明确能力才能转化。");
        _stats.Text = (state.HasAttackGrowth ? $"连击 {state.AttackHitStacks} 层" : "") +
            (!string.IsNullOrEmpty(state.BattleResources) ? "\n" + state.BattleResources : "");
        _stats.Text = _stats.Text.Trim();
        _stats.Visible = _stats.Text.Length > 0;
        _equipment.Text = "装备";
        var hasEquipmentSlots = state.IsPersistentRosterHero || !state.Equipment.IsDefaultOrEmpty;
        _equipment.Visible = hasEquipmentSlots;
        for (var index = 0; index < _slots.Length; index++)
        {
            _slots[index].Visible = hasEquipmentSlots;
            var item = state.Equipment.IsDefaultOrEmpty ? null : state.Equipment.FirstOrDefault(item => item.SlotIndex == index);
            _slots[index].Bind(item?.Icon ?? SemanticIcons.Catalog.ResolveIcon("loot"),
                item?.DisplayName ?? "空槽", $"装备 {index + 1}",
                item is null ? "这个装备槽为空。在备战或队伍中穿戴装备。" : $"{item.DisplayName}\n{item.Description}");
            BattleLabHoverHint.Bind(_slots[index], new BattleLabTooltipInfo(
                item?.DisplayName ?? "空装备槽", $"装备 {index + 1}",
                Abilities: item?.Description ?? "这个装备槽为空。在备战或队伍中穿戴装备。",
                Hint: "点击在下方“效果说明”查看完整内容"));
        }
        var teamTraits = state.TeamTraits.IsDefaultOrEmpty
            ? "无激活层级"
            : string.Join("、", state.TeamTraits.Select(item => item.Text));
        _traits.Text = $"团队层级：{teamTraits}";
        _traits.Visible = !state.TeamTraits.IsDefaultOrEmpty;
        _action.Text = DescribeAction(state);
        _shield.Visible = state.Shield > 0;
        _shield.Bind("shield", $"{state.Shield:0}");
        _shield.BindCaption("护盾", Colors.White);
        BattleLabHoverHint.Bind(_health, new BattleLabTooltipInfo("生命", Stats: $"当前生命 {state.Health:0}/{state.MaxHealth:0}"));
        BattleLabHoverHint.Bind(_healthBar, new BattleLabTooltipInfo("生命", Stats: $"当前生命 {state.Health:0}/{state.MaxHealth:0}"));
        BattleLabHoverHint.Bind(_shield, new BattleLabTooltipInfo("护盾", Stats: $"当前护盾 {state.Shield:0}",
            Abilities: "受到伤害时先由护盾吸收，护盾耗尽后才损失生命。"));
        var primary = state.Skills?.Primary ?? (state.MaxMana > 0 ? new BattleSkillProgress("", "技能",
            SkillResourceKind.Mana, state.CurrentMana, state.MaxMana, "法力", state.CurrentMana >= state.MaxMana
                ? SkillProgressState.Ready : SkillProgressState.Building) : null);
        _mana.Visible = _manaText.Visible = primary is not null;
        if (primary is not null)
        {
            SkillProgressPresentation.Bind(_mana, primary);
            _manaText.Text = SkillProgressPresentation.Summary(primary);
        }
        _skillStates.Text = state.Skills is null ? "" : string.Join("\n", state.Skills.Progress.Select(progress =>
            (progress == primary ? "" : SkillProgressPresentation.Summary(progress) + "\n") + progress.Detail).Where(text => text.Length > 0));
        _skillStates.Visible = _skillStates.Text.Length > 0;
        var abilities = state.Abilities.IsDefaultOrEmpty ? [] : state.Abilities.Where(item => item.IsDisplayedActiveSkill).ToArray();
        _ability.Visible = abilities.Length > 0;
        _activeDetails = BattleLabTooltipFormatter.AbilityGroups(abilities);
        _ability.Text = SkillTitle(_activeDetails, "主动");
        _activeSummary.Visible = _ability.Visible;
        _activeSummary.Text = SkillBody(_activeDetails);
        BattleLabHoverHint.Bind(_ability, new BattleLabTooltipInfo("主动技能", Abilities: _activeDetails,
            Hint: "按技能资源或触发条件自动施放；点击查看词条释义"));
        var passives = state.Abilities.IsDefaultOrEmpty ? [] : state.Abilities
            .Where(a => !a.IsDisplayedActiveSkill).ToArray();
        _passiveDetails = BattleLabTooltipFormatter.AbilityGroups(passives);
        if (state.AttackGrowth is { } growth)
            _passiveDetails += (_passiveDetails.Length > 0 ? "\n\n" : "") +
                $"被动 · 连续命中成长\n命中：攻速 +{growth.AttackSpeedPerHit:0.##}，无上限。\n" +
                (growth.ResetOnTargetChange ? "切换目标时清空层数。" : "切换目标保留层数。") +
                $"当前 {state.AttackHitStacks} 层。";
        _passive.Visible = _passiveDetails.Length > 0;
        _passive.Text = SkillTitle(_passiveDetails, "被动");
        _passiveSummary.Visible = _passive.Visible;
        _passiveSummary.Text = SkillBody(_passiveDetails);
        BattleLabHoverHint.Bind(_passive, new BattleLabTooltipInfo("被动能力", Abilities: _passiveDetails,
            Hint: "点击查看词条释义"));
        var manaInfo = new BattleLabTooltipInfo(primary?.ResourceName ?? "技能状态", Stats: _manaText.Text,
            Abilities: primary?.Detail ?? "");
        BattleLabHoverHint.Bind(_mana, manaInfo);
        BattleLabHoverHint.Bind(_manaText, manaInfo);
        BindStatuses(state);
        if (_detailTile is not null) _detail.Text = _detailTile.DetailText;
        else if (_detailMode == 6) _detail.Text = _activeDetails;
        else if (_detailMode == 7) _detail.Text = _passiveDetails;
        else if (_detailMode >= 0 && _detailMode < _attributeDetails.Length)
            _detail.Text = _attributeDetails[_detailMode];
    }

    private void ShowDetail(CombatFactTile tile)
    {
        _detailTile = tile;
        _detailMode = -1;
        _detail.Text = tile.DetailText;
        RevealDetail();
    }
    private void ShowAbilityDetail()
    {
        _detailTile = null;
        _detailMode = 6;
        _detail.Text = _activeDetails;
        RevealDetail();
    }

    private static string SkillTitle(string details, string kind)
    {
        var first = details.Split('\n')[0];
        var separator = first.IndexOf(" · ", StringComparison.Ordinal);
        return kind + "  " + (separator >= 0 ? first[(separator + 3)..] : "能力");
    }

    private static string SkillBody(string details)
    {
        var split = details.IndexOf('\n');
        return split < 0 ? details : details[(split + 1)..].TrimStart();
    }

    private void ShowPassiveDetail()
    {
        _detailTile = null;
        _detailMode = 7;
        _detail.Text = _passiveDetails;
        RevealDetail();
    }

    private void BindAttribute(int index, string text, string title, string description)
    {
        _attributes[index].Text = text;
        _attributeDetails[index] = $"{title}\n{text}\n{description}";
        BattleLabHoverHint.Bind(_attributes[index], new BattleLabTooltipInfo(title, Stats: text,
            Abilities: description, Hint: "点击在下方“效果说明”保留详情"));
    }

    private void ShowAttributeDetail(int index)
    {
        _detailTile = null;
        _detailMode = index;
        _detail.Text = _attributeDetails[index];
        RevealDetail();
    }

    private void RevealDetail()
    {
        _detailSection.Folded = false;
        // Wait for the fold's container layout, then reveal the start rather than the end of a long glossary.
        Callable.From(() =>
        {
            if (IsInsideTree()) _scroll.ScrollVertical = Mathf.RoundToInt(_detailSection.Position.Y);
        }).CallDeferred();
    }

    private void BindStatuses(BattleScreenRuntimeUnitSnapshot state)
    {
        var statuses = state.Statuses.IsDefaultOrEmpty ? [] : state.Statuses.ToArray();
        _statuses.Text = "当前状态";
        _statuses.Visible = statuses.Length > 0;
        var wanted = statuses.Select(item => item.InstanceId).ToHashSet(StringComparer.Ordinal);
        foreach (var key in _statusViews.Keys.Where(key => !wanted.Contains(key)).ToArray())
        {
            var tile = _statusViews[key];
            if (_detailTile == tile)
            {
                _detailTile = null;
                _detail.Text = "该状态已结束";
            }
            if (tile.HasFocus())
            {
                if (_ability.Visible) _ability.GrabFocus();
                else if (_slots[0].Visible) _slots[0].GrabFocus();
            }
            tile.DetailRequested -= ShowDetail;
            tile.Visible = false;
            tile.QueueFree();
            _statusViews.Remove(key);
        }
        for (var index = 0; index < statuses.Length; index++)
        {
            var status = statuses[index];
            if (!_statusViews.TryGetValue(status.InstanceId, out var tile))
            {
                tile = FactTileScene.Instantiate<CombatFactTile>();
                _statusTiles.AddChild(tile);
                tile.DetailRequested += ShowDetail;
                _statusViews.Add(status.InstanceId, tile);
            }
            _statusTiles.MoveChild(tile, index);
            tile.Bind(SemanticIcons.Catalog.ResolveIcon(StatusDisplayFacts.IconKey(status))
                    ?? SemanticIcons.Catalog.ResolveIcon("risk"),
                status.DisplayName, $"×{status.Stacks} · {StatusDisplayFacts.Duration(status)}",
                $"{status.DisplayName} ×{status.Stacks}\n{status.Description}\n剩余：{StatusDisplayFacts.Duration(status)}");
            BattleLabHoverHint.Bind(tile, new BattleLabTooltipInfo(status.DisplayName,
                Stats: $"{status.Stacks} 层 · 剩余 {StatusDisplayFacts.Duration(status)}",
                Abilities: status.Description, Hint: "点击在下方“效果说明”查看完整内容"));
        }
    }

    public override void _ExitTree()
    {
        _ability.Pressed -= ShowAbilityDetail;
        _passive.Pressed -= ShowPassiveDetail;
        for (var index = 0; index < _attributes.Length; index++)
            _attributes[index].Pressed -= _attributeActions[index];
        foreach (var slot in _slots) slot.DetailRequested -= ShowDetail;
        foreach (var tile in _statusViews.Values) tile.DetailRequested -= ShowDetail;
    }

    public static string DescribeAction(BattleUnitState state)
    {
        var target = string.IsNullOrWhiteSpace(state.ActionTargetName) ? string.Empty : $"：{state.ActionTargetName}";
        return state.Mode switch
        {
            BattleUnitMode.Seeking => $"行动：正在接敌{target}",
            BattleUnitMode.Moving => $"行动：正在移动{target}",
            BattleUnitMode.Waiting => $"行动：等待可用路线{target}",
            BattleUnitMode.Attacking => $"行动：正在攻击{target}",
            BattleUnitMode.Casting when state.LastActionKind == BattleActionKind.Ability => $"施法：{state.LastAbilityName}{target}",
            BattleUnitMode.Casting => $"行动：正在治疗{target}",
            BattleUnitMode.Recovering when state.LastActionKind == BattleActionKind.Heal =>
                $"行动：治疗冷却 {state.AttackCooldown * BattleTiming.TickSeconds:0.0} 秒{target}",
            BattleUnitMode.Recovering =>
                $"行动：攻击冷却 {state.AttackCooldown * BattleTiming.TickSeconds:0.0} 秒{target}",
            BattleUnitMode.Disabled => $"行动：被控制，剩余 {DisabledTicks(state) * BattleTiming.TickSeconds:0.0} 秒",
            BattleUnitMode.Defeated => "行动：已被击败",
            _ => "行动：正在判断战况"
        };
    }

    private static string DescribeAction(BattleScreenRuntimeUnitSnapshot state)
    {
        var target = string.IsNullOrWhiteSpace(state.ActionTargetName) ? string.Empty : $"：{state.ActionTargetName}";
        return state.Mode switch
        {
            BattleUnitMode.Seeking => $"行动：正在接敌{target}",
            BattleUnitMode.Moving => $"行动：正在移动{target}",
            BattleUnitMode.Waiting => $"行动：等待可用路线{target}",
            BattleUnitMode.Attacking => $"行动：正在攻击{target}",
            BattleUnitMode.Casting when state.LastActionKind == BattleActionKind.Ability => $"施法：{state.LastAbilityName}{target}",
            BattleUnitMode.Casting => $"行动：正在治疗{target}",
            BattleUnitMode.Recovering when state.LastActionKind == BattleActionKind.Heal =>
                $"行动：治疗冷却 {state.AttackCooldown * BattleTiming.TickSeconds:0.0} 秒{target}",
            BattleUnitMode.Recovering =>
                $"行动：攻击冷却 {state.AttackCooldown * BattleTiming.TickSeconds:0.0} 秒{target}",
            BattleUnitMode.Disabled => $"行动：被控制，剩余 {DisabledTicks(state) * BattleTiming.TickSeconds:0.0} 秒",
            BattleUnitMode.Defeated => "行动：已被击败",
            _ => "行动：正在判断战况"
        };
    }

    private static int DisabledTicks(BattleUnitState state) => Math.Max(
        state.DisabledTicks,
        state.Statuses.Where(status => status.GrantedTags.Contains(
                StatusDefinitionCompiler.ActionDisabledTag,
                System.StringComparer.Ordinal))
            .Select(status => status.RemainingTicks)
            .DefaultIfEmpty(0)
            .Max());

    private static int DisabledTicks(BattleScreenRuntimeUnitSnapshot state) => Math.Max(
        state.DisabledTicks,
        state.Statuses.Where(status => status.GrantedTags.Contains(
                StatusDefinitionCompiler.ActionDisabledTag,
                StringComparer.Ordinal))
            .Select(status => status.RemainingTicks)
            .DefaultIfEmpty(0)
            .Max());

}
