using Godot;

namespace TowerAutobattler.UI;

public partial class BattleReportLeaderboardHeader : PanelContainer
{
    private Label _primary = null!;
    private Label[] _secondary = [];

    public override void _Ready()
    {
        _primary = GetNode<Label>("%PrimaryHeader");
        _secondary =
        [
            GetNode<Label>("%SecondaryHeader1"),
            GetNode<Label>("%SecondaryHeader2"),
            GetNode<Label>("%SecondaryHeader3"),
            GetNode<Label>("%SecondaryHeader4")
        ];
    }

    public void Bind(BattleReportDimension dimension)
    {
        var labels = dimension switch
        {
            BattleReportDimension.Offense => new[] { "有效伤害", "团队占比", "DPS", "击杀", "攻击行动" },
            BattleReportDimension.Survival => new[] { "有效承伤", "护盾吸收", "最终生命", "活跃时长", "剩余比例" },
            BattleReportDimension.Healing => new[] { "有效治疗", "团队占比", "HPS", "治疗事件", "" },
            _ => new[] { "核心指标", "数据一", "数据二", "数据三", "数据四" }
        };
        _primary.Text = labels[0];
        for (var index = 0; index < _secondary.Length; index++)
        {
            _secondary[index].Text = labels[index + 1];
            ((Control)_secondary[index].GetParent()).Visible = labels[index + 1].Length > 0;
        }
    }
}
