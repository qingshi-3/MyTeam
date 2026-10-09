using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Project;
using TowerAutobattler.UI;

public partial class ExpeditionJourneyInputSmoke
{
    private async Task ItemsJourney()
    {
        await OpenNewJourney();
        var site = await ReachSite(TowerNodeType.Shop);
        await SelectCell(site.Index);
        var before = State();
        var saved = JsonSerializer.Serialize(before);
        await Click(Screens.Expedition.GetNode<Button>("%InspectButton"));
        await Until(() => Screens.Shop.IsVisibleInTree(), "实际商店商品");
        var cards = Descendants<RunOfferChoiceCard>(Screens.Shop).Where(c => c.IsVisibleInTree()).ToArray();
        Require(cards.Length == 3, "当前商店冻结三个真实商品");
        await Capture("items-shop-before.png");
        await Click(Screens.Shop.GetNode<Button>("Margin/Layout/LeaveButton"));
        await Until(() => Screens.Expedition.IsVisibleInTree(), "商店预览返回");
        Require(JsonSerializer.Serialize(State()) == saved, "商店预览不消费、不刷新库存");
        await SelectCell(site.Index);
        await Click(Screens.Expedition.GetNode<Button>("%InspectButton"));
        await Until(() => Screens.Shop.IsVisibleInTree(), "重新查看已冻结商品");
        for(var purchase=0;purchase<2;purchase++)
        {
            _step="items / actual purchase " + (purchase+1);
            var previous=State();
            var card=Descendants<RunOfferChoiceCard>(Screens.Shop).FirstOrDefault(c=>c.IsVisibleInTree()&&!c.ConfirmButton.Disabled);
            Require(card is not null,"实际金币可以买到该件商品");
            await Click(card!.ConfirmButton);
            await Frames(20);
            var after=State();
            Require(after.EquipmentInventory.Count==previous.EquipmentInventory.Count+1&&after.Gold==previous.Gold-8,
                "一次真实购买扣八金币并获得独立装备实例");
            Require(after.Expedition!.RemainingActions==previous.Expedition!.RemainingActions-(purchase==0?1:0),
                "首买消耗一次行动，同店第二件不重复生产");
        }
        await Click(Screens.Shop.GetNode<Button>("Margin/Layout/LeaveButton"));
        await Until(()=>Screens.Expedition.IsVisibleInTree(),"购买后返回地图");
        var purchased=State().EquipmentInventory.TakeLast(2).Select(i=>i.InstanceId).ToArray();
        var heroes=State().Roster.Take(2).Select(h=>h.InstanceId).ToArray();
        await Click(Screens.Expedition.GetNode<Button>("%ArmyButton"));
        var army=_game!.GetNode<ArmyOverviewController>("ArmyOverview");
        await Until(()=>army.IsOpen,"打开正式装备管理");
        var view=army.GetNode<RosterLoadoutView>("%ArmyEquipmentPanel");
        for(var i=0;i<3&&!view.IsVisibleInTree();i++)await Click(army.GetNode<Button>("%PageToggle"));
        await Click(ItemSelector(view,heroes[0]));
        foreach(var item in purchased)
        {
            var tile=InventoryTile(view,item);
            Require(tile.GetNode<Label>("NoIconLabel").IsVisibleInTree()&&!string.IsNullOrWhiteSpace(tile.GetNode<Label>("NoIconLabel").Text),
                "无图标装备有可见名称，不是空白槽");
        }
        await Capture("items-inventory-names.png");
        _step="items / equip and replace";
        await ItemDrag(InventoryTile(view,purchased[0]),HeroSlot(view,0));
        Require(HeroHasItem(heroes[0],purchased[0]),"原生拖放穿戴并保存");
        await ItemDrag(InventoryTile(view,purchased[1]),HeroSlot(view,0));
        Require(HeroHasItem(heroes[0],purchased[1])&&State().EquipmentInventory.Any(i=>i.InstanceId==purchased[0]),
            "替换时旧装备完整回包");
        await Capture("items-equipped.png");
        _step="items / transfer and cancel";
        await ItemDrag(HeroSlot(view,0),ItemSelector(view,heroes[1]));
        Require(HeroHasItem(heroes[1],purchased[1])&&view.SelectedHeroId==heroes[1],"拖到另一英雄头像完成转移");
        saved=JsonSerializer.Serialize(State());
        await ItemDrag(HeroSlot(view,0),view.GetNode<Control>("%RosterReturnZone"),cancel:true);
        Require(JsonSerializer.Serialize(State())==saved,"Escape取消拖放不提交装备变更");
        await ItemDrag(HeroSlot(view,0),view.GetNode<Control>("%RosterReturnZone"));
        Require(State().EquipmentInventory.Any(i=>i.InstanceId==purchased[1]),"卸下装备回真实背包");
        await ItemDrag(InventoryTile(view,purchased[0]),HeroSlot(view,0));
        await Capture("items-transferred.png");
        await Key(Godot.Key.Escape);
        await Until(()=>!army.IsOpen,"关闭装备管理");
        await VerifyResume();
        Require(HeroHasItem(heroes[1],purchased[0])&&State().EquipmentInventory.Any(i=>i.InstanceId==purchased[1]),
            "冷启动继续恢复两件装备准确归属");
        Check("actual-shop-two-items-equip-replace-transfer-cancel-return-persistent");
    }

    private bool HeroHasItem(string hero,string item)=>State().Roster.Single(h=>h.InstanceId==hero).Equipment.Any(e=>e.InstanceId==item);
    private static RosterHeroSelector ItemSelector(RosterLoadoutView view,string id)=>Descendants<RosterHeroSelector>(view).Single(s=>s.HeroId==id);
    private static EquipmentSlotButton InventoryTile(RosterLoadoutView view,string id)=>Descendants<EquipmentSlotButton>(view.GetNode<Control>("%RosterInventory")).Single(s=>s.InstanceId==id);
    private static EquipmentSlotButton HeroSlot(RosterLoadoutView view,int slot)=>view.GetNode<RosterHeroCard>("%SelectedCard").Slot(slot);
    private async Task ItemDrag(Control source,Control target,bool cancel=false)
    {
        for(var parent=source.GetParent();parent is not null;parent=parent.GetParent())
            if(parent is ScrollContainer scroll){scroll.EnsureControlVisible(source);await Frames(3);}
        await Frames(3);
        var start=source.GetGlobalRect().GetCenter();var end=target.GetGlobalRect().GetCenter();
        Require(source.IsVisibleInTree()&&target.IsVisibleInTree()&&_stage.Viewport.GetVisibleRect().HasPoint(start)&&
            _stage.Viewport.GetVisibleRect().HasPoint(end),"拖放两端真实可见");
        _inputs.Add(new{kind="equipment-drag",from=source.GetPath().ToString(),to=target.GetPath().ToString(),cancel});
        _stage.Push(new InputEventMouseMotion{Position=start,GlobalPosition=start});
        _stage.Push(new InputEventMouseButton{Position=start,GlobalPosition=start,ButtonIndex=MouseButton.Left,ButtonMask=MouseButtonMask.Left,Pressed=true});
        for(var i=1;i<=20;i++)
        {
            var point=start.Lerp(end,i/20f);
            _stage.Push(new InputEventMouseMotion{Position=point,GlobalPosition=point,Relative=(end-start)/20,ButtonMask=MouseButtonMask.Left});
            await Frames(2);
        }
        Require(_stage.Viewport.GuiIsDragging(),"Godot原生拖放已开始");
        if(cancel)await Key(Godot.Key.Escape);
        _stage.Push(new InputEventMouseButton{Position=end,GlobalPosition=end,ButtonIndex=MouseButton.Left,Pressed=false});
        await Frames(24);
    }
}
