using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.UI;

public partial class BattleReportRosterStrip : PanelContainer
{
    [Export] public PackedScene PortraitScene { get; set; } = null!;

    private Label _title = null!;
    private HFlowContainer _portraits = null!;

    public override void _Ready()
    {
        _title = GetNode<Label>("%RosterTitle");
        _portraits = GetNode<HFlowContainer>("%RosterPortraits");
    }

    public void Bind(string title, IReadOnlyList<BattleReportRosterPortraitModel> models)
    {
        _title.Text = title;
        foreach (var child in _portraits.GetChildren())
        {
            _portraits.RemoveChild(child);
            child.QueueFree();
        }
        foreach (var model in models)
        {
            var portrait = PortraitScene.Instantiate<BattleReportRosterPortrait>();
            _portraits.AddChild(portrait);
            portrait.Bind(model);
        }
    }
}
