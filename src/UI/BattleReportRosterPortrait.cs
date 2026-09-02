using Godot;

namespace TowerAutobattler.UI;

public partial class BattleReportRosterPortrait : VBoxContainer
{
    private UnitPortrait _portrait = null!;
    private Label _status = null!;

    public override void _Ready()
    {
        _portrait = GetNode<UnitPortrait>("%UnitPortrait");
        _status = GetNode<Label>("%UnitStatus");
    }

    public void Bind(BattleReportRosterPortraitModel model)
    {
        _portrait.Bind(model.Portrait, model.Fallback);
        _status.Text = model.Unit.Alive ? "●" : "✕";
        _status.ThemeTypeVariation = model.Unit.Alive ? "HealthValue" : "DangerValue";
        TooltipText = $"{model.Unit.DisplayName} · {(model.Unit.Alive ? "存活" : "阵亡")}";
    }
}
