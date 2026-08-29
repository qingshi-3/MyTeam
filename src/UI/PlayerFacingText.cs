using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public static class PlayerFacingText
{
    private static readonly IReadOnlyDictionary<string, string> GameplayTags = new Dictionary<string, string>
    {
        ["order"] = "秩序",
        ["desert"] = "沙海",
        ["undead"] = "亡灵",
        ["beast"] = "野兽",
        ["machine"] = "机械",
        ["frost"] = "霜寒"
    };

    public static string DescribeUnitFaction(UnitFaction faction) => faction switch
    {
        UnitFaction.Order => "秩序",
        UnitFaction.Desert => "沙海",
        UnitFaction.Undead => "亡灵",
        UnitFaction.Beast => "野兽",
        UnitFaction.Machine => "机械",
        UnitFaction.Frost => "霜寒",
        UnitFaction.Neutral => "中立",
        UnitFaction.Enemy => "敌军",
        _ => "未知阵营"
    };

    public static string DescribeUnitRole(UnitRole role) => role switch
    {
        UnitRole.Vanguard => "前卫",
        UnitRole.Fighter => "战士",
        UnitRole.Ranged => "远程",
        UnitRole.Support => "辅助",
        UnitRole.Assassin => "刺客",
        UnitRole.Summoner => "召唤",
        UnitRole.Artillery => "炮手",
        UnitRole.Boss => "首领",
        _ => "未知职责"
    };

    public static string DescribeUnitTraits(UnitFaction faction, IEnumerable<StringName> tags)
    {
        var labels = new List<string> { DescribeUnitFaction(faction) };
        foreach (var tag in tags.Select(value => value.ToString()))
            if (GameplayTags.TryGetValue(tag, out var label) && !labels.Contains(label)) labels.Add(label);
        return string.Join("、", labels);
    }

    public static string DescribeItemRarity(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => "普通",
        ItemRarity.Uncommon => "优良",
        ItemRarity.Rare => "稀有",
        ItemRarity.Legendary => "传奇",
        _ => "未知品质"
    };

    public static string DescribeBattleOutcome(BattleOutcome outcome) => outcome switch
    {
        BattleOutcome.Running => "进行中",
        BattleOutcome.PlayerVictory => "我方胜利",
        BattleOutcome.PlayerDefeat => "我方战败",
        BattleOutcome.Timeout => "战斗超时",
        _ => "未知结果"
    };
}
