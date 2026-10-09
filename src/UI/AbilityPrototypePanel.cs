using System;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Traits;

namespace TowerAutobattler.UI;

public partial class AbilityPrototypePanel : Control
{
    [Signal] public delegate void StartRequestedEventHandler();
    [Signal] public delegate void BackRequestedEventHandler();
    private ContentRegistry? _content;
    private readonly OptionButton[,] _slots = new OptionButton[2,6];
    public override void _Ready()
    {
        GetNode<Button>("%PrototypeStart").Pressed += () => EmitSignal(SignalName.StartRequested);
        GetNode<Button>("%PrototypeBack").Pressed += () => EmitSignal(SignalName.BackRequested);
        for (var team = 0; team < 2; team++)
        {
            var side = team;
            var presets = GetNode<OptionButton>("%Preset" + team);
            foreach (var name in AbilityPrototypeContent.PresetNames) presets.AddItem(name);
            presets.ItemSelected += index => SelectPreset(side, (int)index);
            for (var slot = 0; slot < 6; slot++)
            {
                var pick = GetNode<OptionButton>($"%Unit{team}_{slot}");
                _slots[team,slot] = pick;
                pick.ItemSelected += _ => { Describe(pick.GetSelectedId()); Refresh(); };
                pick.FocusEntered += () => Describe(pick.GetSelectedId());
                pick.MouseEntered += () => Describe(pick.GetSelectedId());
            }
        }
    }
    internal void Bind(ContentRegistry content)
    {
        if (_content is not null) return;
        _content = content;
        foreach (var pick in _slots)
        {
            pick.AddItem("空位", 0);
            foreach (var number in AbilityPrototypeContent.UnitNumbers)
            {
                var unit = AbilityPrototypeContent.Snapshot(content, number);
                var names = unit.TraitContributions.Select(t => content.Graph.Traits.Single(d => d.StableId == t.TraitId).DisplayName);
                pick.AddItem($"{number:00} {unit.DisplayName} · {string.Join("／", names)}", number);
            }
        }
        SelectPreset(0,0); SelectPreset(1,1);
        GetNode<OptionButton>("%Preset1").Select(1);
        Describe(2);
    }
    private void SelectPreset(int team, int preset)
    {
        if (_content is null) return;
        for (var slot = 0; slot < 6; slot++) _slots[team,slot].Select(_slots[team,slot].GetItemIndex(AbilityPrototypeContent.Presets[preset][slot]));
        Refresh();
    }
    private int[] Selected(int team) => Enumerable.Range(0,6).Select(slot => _slots[team,slot].GetSelectedId()).ToArray();
    internal BattleConfig CreateBattle() => AbilityPrototypeContent.CreateBattle(_content!, Selected(0), Selected(1));
    private void Refresh()
    {
        try
        {
            var config = CreateBattle();
            var snapshot = TraitSnapshotBuilder.Build(config.Traits.Definitions, config.Traits.Contributions);
            for (var team = 0; team < 2; team++)
            {
                var values = snapshot.Values.Where(t => t.Team == team && t.Value > 0).ToArray();
                GetNode<Label>("%Traits" + team).Text = string.Join("  ", values.Select(t => $"{t.Presentation.DisplayName} {t.Value}" + (t.ActiveBreakpoint is null ? "（未激活）" : "（生效）")));
                GetNode<Label>("%Traits" + team).TooltipText = string.Join("\n\n", values.Where(t => t.ActiveBreakpoint is not null).Select(t => t.Presentation.DisplayName + "：" + TraitModelText.Describe(t.ActiveBreakpoint!)));
            }
            GetNode<Button>("%PrototypeStart").Disabled = false;
            GetNode<Label>("%PrototypeFeedback").Text = "前排靠近敌人；将单位换到其他位置即可调整站位。悬停羁绊查看完整效果。";
        }
        catch (Exception error)
        {
            GetNode<Button>("%PrototypeStart").Disabled = true;
            GetNode<Label>("%PrototypeFeedback").Text = error.Message;
        }
    }
    private void Describe(int number)
    {
        if (_content is null || number == 0) return;
        var unit = AbilityPrototypeContent.Snapshot(_content, number);
        GetNode<RichTextLabel>("%PrototypeDescription").Text = unit.DisplayName + $"  生命 {unit.MaxHealth:0}  攻击 {unit.Damage:0}  防御 {unit.Armor:0}\n" +
            string.Join("\n", unit.AbilityLoadout!.Abilities.Select(a => a.DisplayName + "：" + a.Description));
    }
}
