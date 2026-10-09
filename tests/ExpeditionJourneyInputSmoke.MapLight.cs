using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class ExpeditionJourneyInputSmoke
{
    private bool _watchInspector;
    private float _maximumInspectorOffset;
    private float _minimumInspectorAlpha = 1;

    public override void _Process(double delta)
    {
        if (!_watchInspector) return;
        var panel = Screens.Expedition.GetNode<Control>("%DetailsPanel");
        _maximumInspectorOffset = Mathf.Max(_maximumInspectorOffset, panel.OffsetTransformPosition.Length());
        _minimumInspectorAlpha = Mathf.Min(_minimumInspectorAlpha, panel.Modulate.A);
    }

    private async Task MapLightJourney()
    {
        await OpenNewJourney();
        await EntranceSettled();
        await EntrancePause(.35);
        _step = "map light / real keyboard movement with stable inspector";
        var revealed = Map().Cells.Count(cell => cell.Revealed);
        _watchInspector = true;
        await EntranceClip("01-walk-reveal", async () =>
        {
            await EntrancePause(.25);
            var count = 0;
            while (count < 7)
            {
                var map = Map();
                var frontier = Frontier(map).FirstOrDefault(-1);
                Require(frontier >= 0, "当前地图有可达探索边缘。");
                foreach (var next in KnownRoutes(map)[frontier].Take(7 - count))
                {
                    await KeyboardStep(next);
                    count++;
                    await EntrancePause(.12);
                }
                if (count >= 4 && Map().Cells.Count(cell => cell.Revealed) > revealed) break;
            }
            await EntrancePause(.6);
        });
        _watchInspector = false;
        Require(_maximumInspectorOffset < .01f && _minimumInspectorAlpha > .999f,
            $"行走只更新详情内容，不重播面板入场。offset={_maximumInspectorOffset}, alpha={_minimumInspectorAlpha}");
        Require(Map().Cells.Count(cell => cell.Revealed) > revealed, "真实按键揭开新区域。");
        await Capture("map-after-walk.png");
        Check("walk-inspector-offset-zero-alpha-one-new-fog-real-input");

        _step = "map light / camp opening source and return";
        var camp = await KeyboardReachSite(TowerNodeType.Rest);
        await EntrancePause(.65);
        var scroll = Screens.Expedition.GetNode<ScrollContainer>("%MapScroll");
        // Deliberately inspect another tile before Enter. The keyboard target
        // must remain the current camp, including the transition's visual source.
        var other = Map().Cells.First(cell => cell.Revealed && cell.Index != camp.Index &&
            MapCell(cell.Index).GetGlobalRect().Intersects(scroll.GetGlobalRect()));
        await Click(MapCell(other.Index));
        await EntrancePause(.3);
        Require(MapCell(other.Index).GetNode<Control>("Selection").Visible &&
            !MapCell(camp.Index).GetNode<Control>("Selection").Visible, "回车前已实际查看另一个地块。");
        var camera = new Vector2I(scroll.ScrollHorizontal, scroll.ScrollVertical);
        var state = JsonSerializer.Serialize(State());
        await EntranceClip("02-site-open-return", async () =>
        {
            await EntrancePause(.2);
            await Key(Godot.Key.Enter);
            await Until(() => Screens.Reward.IsVisibleInTree(), "回车打开当前营地");
            Require(MapCell(camp.Index).GetNode<Control>("Selection").Visible,
                "地点转场使用实际打开的营地来源，不沿鼠标查看的远处格。");
            await EntranceSettled();
            await EntrancePause(.6);
            await Key(Godot.Key.D);
            Require(JsonSerializer.Serialize(State()) == state, "服务页移动键不穿透。");
            await Click(Screens.Reward.GetNode<Button>("Center/Panel/Layout/ContinueButton"));
            await EntranceSettled();
            await EntrancePause(.5);
        });
        Require(JsonSerializer.Serialize(State()) == state, "免费查看与返回不改变存档。");
        Require(new Vector2I(scroll.ScrollHorizontal, scroll.ScrollVertical) == camera, "服务返回保留地图视角。");
        await Capture("map-after-return.png");
        Check("enter-camp-after-remote-inspection-correct-source-return-save-camera");

        _step = "map light / reduced motion and hide cleanup";
        Screens.Expedition.ReduceUiMotion = () => true;
        foreach (var cell in Descendants<ExpeditionMapCell>(Screens.Expedition)) cell.ReduceUiMotion = () => true;
        await Frames(3);
        var light = Screens.Expedition.GetNode<ExpeditionPartyLight>("%PartyLight");
        Require(light.GetNode("Motes").GetChildren().OfType<Sprite2D>().All(mote => !mote.Visible),
            "减弱动效清掉路径光点。");
        var adjacent = Neighbors(Map().Size, Map().PositionIndex).First(index => Map().Cells[index] is
            { Revealed: true, Walkable: true, Blocked: false });
        await KeyboardStep(adjacent);
        await Frames(3);
        Require(light.GetNode("Motes").GetChildren().OfType<Sprite2D>().All(mote => !mote.Visible),
            "减弱动效真实移动不生成新粒子。");
        await Click(Screens.Expedition.GetNode<Button>("%MenuButton"));
        await EntranceSettled();
        Require(!light.Visible && !light.IsProcessing(), "离页清理地图光效。");
        Check("reduced-motion-static-lamp-no-new-trail-hidden-clear");
    }
}
