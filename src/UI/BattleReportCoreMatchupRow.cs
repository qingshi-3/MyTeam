using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

public partial class BattleReportCoreMatchupRow : PanelContainer
{
    private Control _matchupContent = null!;
    private TextureRect _categoryIcon = null!;
    private Label _category = null!;
    private Label _playerLeaderNames = null!;
    private Label _playerLeaderValueShare = null!;
    private Label _enemyLeaderNames = null!;
    private Label _enemyLeaderValueShare = null!;
    private Label _bothZeroState = null!;

    public override void _Ready()
    {
        _matchupContent = GetNode<Control>("%MatchupContent");
        _categoryIcon = GetNode<TextureRect>("%CategoryIcon");
        _category = GetNode<Label>("%Category");
        _playerLeaderNames = GetNode<Label>("%PlayerLeaderNames");
        _playerLeaderValueShare = GetNode<Label>("%PlayerLeaderValueShare");
        _enemyLeaderNames = GetNode<Label>("%EnemyLeaderNames");
        _enemyLeaderValueShare = GetNode<Label>("%EnemyLeaderValueShare");
        _bothZeroState = GetNode<Label>("%BothZeroState");
    }

    public void Bind(BattleReportCoreMatchupViewModel model)
    {
        var (title, iconKey, valueVariation, zeroText) = Category(model.Dimension);
        _category.Text = title;
        _categoryIcon.Texture = SemanticIcons.Catalog.ResolveIcon(iconKey);
        _playerLeaderValueShare.ThemeTypeVariation = valueVariation;
        _enemyLeaderValueShare.ThemeTypeVariation = valueVariation;
        _categoryIcon.Modulate = _playerLeaderValueShare.GetThemeColor("font_color");

        _matchupContent.Visible = !model.BothSidesZero;
        _bothZeroState.Visible = model.BothSidesZero;
        if (model.BothSidesZero)
        {
            _bothZeroState.Text = model.Dimension == BattleReportDimension.Healing
                ? "治疗核心 · 双方均无有效治疗"
                : $"{title} · 双方均无正值";
            return;
        }

        BindSide(model.Dimension, model.PlayerLeaders, _playerLeaderNames, _playerLeaderValueShare, zeroText);
        BindSide(model.Dimension, model.EnemyLeaders, _enemyLeaderNames, _enemyLeaderValueShare, zeroText);
    }

    private static void BindSide(
        BattleReportDimension dimension,
        IReadOnlyList<BattleReportUnitViewModel> leaders,
        Label names,
        Label valueShare,
        string zeroText)
    {
        if (leaders.Count == 0)
        {
            names.Text = zeroText;
            names.TooltipText = zeroText;
            valueShare.Text = "—";
            valueShare.TooltipText = zeroText;
            return;
        }

        var joinedNames = string.Join("、", leaders.Select(leader => leader.Unit.DisplayName));
        var value = Value(dimension, leaders[0]);
        var share = Share(dimension, leaders[0]);
        var tiedPrefix = leaders.Count > 1 ? "各 " : string.Empty;
        var sharePrefix = leaders.Count > 1 ? "各占 " : "占比 ";
        names.Text = joinedNames;
        names.TooltipText = joinedNames;
        valueShare.Text = $"{tiedPrefix}{value:0} · {sharePrefix}{share * 100:0.#}%";
        valueShare.TooltipText = $"权威值 {value:0}，本队贡献占比 {share * 100:0.#}%";
    }

    private static float Value(BattleReportDimension dimension, BattleReportUnitViewModel leader) => dimension switch
    {
        BattleReportDimension.Offense => leader.Unit.DamageDealt,
        BattleReportDimension.Survival => leader.Unit.DamageTaken,
        BattleReportDimension.Healing => leader.Unit.HealingDone,
        _ => throw new ArgumentOutOfRangeException(nameof(dimension))
    };

    private static float Share(BattleReportDimension dimension, BattleReportUnitViewModel leader) => dimension switch
    {
        BattleReportDimension.Offense => leader.DamageShare,
        BattleReportDimension.Survival => leader.DamageTakenShare,
        BattleReportDimension.Healing => leader.HealingShare,
        _ => throw new ArgumentOutOfRangeException(nameof(dimension))
    };

    private static (string Title, StringName IconKey, StringName ValueVariation, string ZeroText) Category(
        BattleReportDimension dimension) => dimension switch
    {
        BattleReportDimension.Offense => ("输出核心", SemanticIconKeys.Damage, "DamageValue", "无有效输出"),
        BattleReportDimension.Survival => ("承伤核心", SemanticIconKeys.Shield, "ShieldValue", "无有效承伤"),
        BattleReportDimension.Healing => ("治疗核心", SemanticIconKeys.Healing, "HealingValue", "无有效治疗"),
        _ => throw new ArgumentOutOfRangeException(nameof(dimension))
    };
}
