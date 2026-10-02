using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.UI;

// Authored inner-edge entries. No external gutters or aggregate reading panel.
public partial class UnitCardOrbit : Control
{
    [Export] public bool ShowEquipment { get; set; }
    [Export] public bool SplitMana { get; set; }
    [Export] public bool AlwaysVisible { get; set; }
    [Export] public PackedScene SkillScene { get; set; } = null!;
    private static readonly CombatAttribute[] LeftAttributes = [CombatAttribute.Armor, CombatAttribute.AttackSpeed,
        CombatAttribute.AttackRange, CombatAttribute.CriticalChance, CombatAttribute.CriticalDamage,
        CombatAttribute.LifeSteal];
    private static readonly CombatAttribute[] RightAttributes = [CombatAttribute.MoveSpeed, CombatAttribute.MaxMana,
        CombatAttribute.HealingPower, CombatAttribute.DodgeChance, CombatAttribute.ControlResistance];
    public static IReadOnlyList<CombatAttribute> DisplayedAttributes { get; } = Array.AsReadOnly(
        new[] { CombatAttribute.AttackDamage, CombatAttribute.SpellPower, CombatAttribute.MaxHealth }
            .Concat(LeftAttributes).Concat(RightAttributes).ToArray());
    private readonly List<Button> _skills = [];
    private string _skillKeys = "";
    public UiCardDisclosure Disclosure => GetNode<UiCardDisclosure>("Disclosure");
    public EquipmentSlotButton Slot(int index) => GetNode<EquipmentSlotButton>("TopDock/Equipment/Slot" + index);
    public CompactDetailFact Fact(CombatAttribute attribute) => GetNode<CompactDetailFact>(
        (LeftAttributes.Contains(attribute) ? "Left/" : "Right/") + attribute);

    public void Configure(Control owner, Control card, Button? toggle)
    {
        var equipment = GetNode<Control>("TopDock/Equipment");
        equipment.Visible = ShowEquipment;
        Control[] groups = [GetNode<Control>("Left"), GetNode<Control>("Right"), GetNode<Control>("TopDock/Skills")];
        Disclosure.Configure(owner, card, toggle, ShowEquipment ? [.. groups, equipment] : groups,
            GetNode<Control>("EdgeShade"), ShowEquipment ? "属性技能与装备" : "属性与技能", AlwaysVisible);
    }

    public void Bind(UnitInformation information, string rulesTitle = "", string rules = "", string traits = "")
    {
        var attributes = LeftAttributes.Concat(RightAttributes);
        if (SplitMana) attributes = attributes.Append(CombatAttribute.StartingMana);
        foreach (var attribute in attributes)
        {
            var fact = attribute == CombatAttribute.MaxMana && !SplitMana ? information.CardManaFact() : information.AttributeFact(attribute);
            var button = Fact(attribute);
            StringName icon = attribute switch
            {
                CombatAttribute.AttackSpeed => "keyword.attack-speed",
                CombatAttribute.MoveSpeed => "move-speed",
                CombatAttribute.DodgeChance => "keyword.dodge",
                CombatAttribute.StartingMana => "mana-start",
                _ => fact.Icon
            };
            button.Bind(icon, fact.Value, fact.Caption, fact.Explanation);
            button.AccessibilityName = fact.Caption + " " + fact.Value;
            BattleLabHoverHint.Bind(button, new(fact.Caption, Stats: fact.Explanation, SingleEntry: true));
        }
        var entries = information.Skills.Select(skill => (Key: skill.Category + skill.Name, Name: skill.Name,
            Kind: skill.Category, Body: skill.Body + "\n" + skill.Timing,
            Icon: skill.Category == "主动" ? new StringName("skill-active") : new StringName("skill-passive"))).ToList();
        BindSideHint("Left/Growth", SemanticIconKeys.Loot, rulesTitle, rules);
        BindSideHint("Right/Traits", SemanticIconKeys.Hero, "羁绊贡献", traits);
        var keys = string.Join('|', entries.Select(entry => entry.Key));
        if (keys != _skillKeys)
        {
            foreach (var button in _skills) { button.Hide(); button.QueueFree(); }
            _skills.Clear();
            foreach (var entry in entries)
            {
                var button = SkillScene.Instantiate<Button>();
                GetNode<Control>("TopDock/Skills").AddChild(button); _skills.Add(button);
            }
            _skillKeys = keys;
        }
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i]; var button = _skills[i];
            button.Icon = UnitCardIcons.Resolve(entry.Icon);
            button.AccessibilityName = entry.Kind + " · " + entry.Name;
            button.GetNode<Label>("Kind").Text = entry.Kind == "主动" ? "◆" : entry.Kind == "被动" ? "◇" : "＋";
            BattleLabHoverHint.Bind(button, new(entry.Name, entry.Kind, Abilities: entry.Body, SingleEntry: true));
        }
        Disclosure.RefreshItems();
    }

    private void BindSideHint(string path, StringName icon, string title, string explanation)
    {
        var button = GetNode<Button>(path);
        button.Visible = !string.IsNullOrWhiteSpace(explanation);
        button.SetMeta("card_entry_available", button.Visible);
        button.Icon = SemanticIcons.Catalog.ResolveIcon(icon);
        button.AccessibilityName = title;
        BattleLabHoverHint.Bind(button, new(title, Stats: explanation, SingleEntry: true));
    }

    public void BindEmptyEquipment()
    {
        if (!ShowEquipment) return;
        for (var i = 0; i < 3; i++)
        {
            var slot = Slot(i);
            slot.Bind("", i, null, false, false);
            slot.Disabled = false;
            BattleLabHoverHint.Bind(slot, new("装备位 " + (i + 1), Stats: "尚未穿戴装备。入队后可在英雄与装备界面配置。", SingleEntry: true));
        }
    }
}
