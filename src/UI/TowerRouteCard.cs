using System;
using Godot;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

// A route decision keeps the existing exact-once choice command. This scene
// separates its identity, opportunity and failure cost for reading.
public partial class TowerRouteCard : ChoiceCard, IUiMotionHost
{
    public Func<bool> ReduceUiMotion { get; set; } = () => false;

    public void BindRoute(TowerNodeType type, RunApplication app)
    {
        var action = type switch
        {
            TowerNodeType.Combat or TowerNodeType.Boss => "进入战斗",
            TowerNodeType.Elite => app.CurrentOpponentOptions(type).Count > 0 ? "查看挑战" : "进入战斗",
            TowerNodeType.Shop => "前往商店", TowerNodeType.Recruitment => "查看招募",
            TowerNodeType.Event => "探访异象", TowerNodeType.Rest => "进入营地", _ => "前往节点"
        };
        GetNode<Label>("%RouteAction").Text = action + "  ›";
        var consequence = type switch
        {
            TowerNodeType.Combat or TowerNodeType.Elite =>
                $"战败 / 超时 −{RunHealthPolicy.DefeatLoss(app.Rules, type)} 生命\n无战利品 · 生命尚余可继续",
            TowerNodeType.Boss => "失败 / 超时即结束征程\n不受剩余生命影响",
            TowerNodeType.Shop => "查看物品效果后再购买",
            TowerNodeType.Recruitment => "为军团增添英雄",
            TowerNodeType.Rest => "恢复征程生命或获得金币",
            _ => "查看事件后选择行动"
        };
        GetNode<Label>("%RouteConsequence").Text = consequence;
        var tint = type switch
        {
            TowerNodeType.Combat => new Color(.83f, .65f, .45f),
            TowerNodeType.Elite or TowerNodeType.Boss => new Color(.86f, .52f, .39f),
            TowerNodeType.Event => new Color(.68f, .67f, .83f),
            TowerNodeType.Rest => new Color(.60f, .77f, .63f),
            _ => new Color(.85f, .75f, .52f)
        };
        GetNode<TextureRect>("%ChoiceIcon").Modulate = tint;
        var risk = RunHealthText.Risk(app.Rules, type);
        // Everything needed for this choice is already on the face. Avoid a
        // delayed native tooltip covering the action when the card is hovered.
        TooltipText = string.Empty;
        AccessibilityName = string.Join("\n", TitleText, BodyText, risk, consequence, FooterText, action);
    }
}
