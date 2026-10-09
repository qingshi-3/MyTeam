using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.UI;

public partial class ExpeditionJourneyInputSmoke
{
    // Supplements the successful interaction trace with settled surfaces only;
    // does not replay or fabricate a battle or repeat the opening journey.
    private async Task RestorationCaptures()
    {
        await Capture("main-menu.png");
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "已通过输入验证的隔离存档");
        await Capture("map-restored.png");
        var site = await ReachSite(TowerAutobattler.Domain.TowerNodeType.Combat);
        await SelectCell(site.Index);
        await Click(Screens.Expedition.GetNode<Button>("%InspectButton"));
        await Until(() => Screens.Deployment.IsVisibleInTree(), "敌阵预览");
        await Capture("deployment-preview.png");
        await Click(Screens.Deployment.GetNode<Button>("%BackButton"));
        await Click(Screens.Expedition.GetNode<Button>("%MenuButton"));
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
        await Capture("tools-entry.png");
        Check("settled-surfaces-after-original-transitions");
    }

    private async Task RestorationJourney()
    {
        Require(ProjectSettings.GetSetting("application/run/main_scene").AsString() == "res://scenes/app/GrowthGameRoot.tscn",
            "默认入口必须是正式游戏");
        await Capture("main-menu.png");
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/SettingsButton"));
        await Until(() => Screens.Settings.IsVisibleInTree(), "设置页");
        await Click(Screens.Settings.GetNode<Button>("Center/Panel/Layout/SaveButton"));
        await Until(() => Screens.MainMenu.IsVisibleInTree(), "设置返回主菜单");
        await VerifyAbilityTools();
        Require(_save.LoadActiveRun() is null, "实验不能创建征程存档");
        await OpenNewJourney();
        await AssertInitialPartyVisible();
        await Capture("map-overview.png");
        await Click(Screens.Expedition.GetNode<Button>("%ArmyButton"));
        Require(_game!.GetNode<ArmyOverviewController>("ArmyOverview").IsOpen, "军团页面已恢复");
        await Key(Godot.Key.Escape);
        await Frames(20);
        await PreviewAndBattle();
        await VerifyResume();
        var before = JsonSerializer.Serialize(State());
        await Click(Screens.Expedition.GetNode<Button>("%MenuButton"));
        await VerifyAbilityTools();
        Require(JsonSerializer.Serialize(State()) == before, "已有征程不受能力实验影响");
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ContinueButton"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "实验返回后继续征程");
        Require(JsonSerializer.Serialize(State()) == before, "实验返回后继续不重抽内容或结算");
        Check("restored-main-ui-with-independent-ability-entry");
    }

    private async Task VerifyAbilityTools()
    {
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/AbilityPrototypeButton"));
        var tool = _game!.GetChildren().OfType<ContentRebuildRoot>().Single();
        await Until(() => tool.ContentReady, "独立能力实验载入");
        await Capture("ability-tools.png");
        await Click(tool.GetNode<Button>("%Prototype"));
        var panel = tool.GetNode<Control>("PrototypePanel");
        Require(panel.IsVisibleInTree() && !Screens.MainMenu.IsVisibleInTree(), "实验有独立页面");
        await Click(panel.GetNode<Button>("%PrototypeStart"));
        var battle = tool.GetNode<TowerAutobattler.Presentation.BattleScreenController>("Battle");
        await Until(() => battle.TickIndex >= 10, "实验实际开战");
        await Click(battle.GetNode<Button>("%ReturnConfigurationButton"));
        await Click(panel.GetNode<Button>("%PrototypeBack"));
        await Click(tool.GetNode<Button>("%BackToGame"));
        await Frames(4);
        Require(Screens.MainMenu.IsVisibleInTree() && !_game.GetChildren().OfType<ContentRebuildRoot>().Any(), "实验退出清理并恢复原主菜单");
        await Click(Screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
    }
}
