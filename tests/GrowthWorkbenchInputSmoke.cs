using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Growth;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class GrowthWorkbenchInputSmoke : Control
{
    private UiInputStage _stage = null!;
    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            _stage = new UiInputStage(this, true);
            var publication = await GrowthContentPackage.CreateReadyAsync(this);
            var package = publication.Package ?? throw new InvalidOperationException(string.Join(';', publication.Report.CoreErrors));
            var rules = GrowthContentPackage.LoadRules(package.Content);
            var save = new MemorySave();
            var app = new RunApplication(package.Content, save, package.Project, rules);
            var producers = rules.Heroes.Values.Where(hero => !hero.ProductionModes.IsEmpty).Select(hero => hero.ContentId).ToHashSet(StringComparer.Ordinal);
            var startingHero = producers
                .OrderBy(id => id == "hero_mx25" ? 0 : 1).ThenBy(id => id, StringComparer.Ordinal).First();
            if (!app.Meta.UnlockedHeroIds.Contains(startingHero)) app.Meta.UnlockedHeroIds.Add(startingHero);
            Require(app.StartNewRun(startingHero, 90427), "成长征程初始化");
            var run = app.ActiveRun!;
            run.Growth!.Materials = rules.AscensionCost;
            run.Growth.Research = rules.Spells.Values.Max(spell => spell.ResearchCost);

            var panel = GD.Load<PackedScene>("res://scenes/ui/components/GrowthWorkbenchPanel.tscn").Instantiate<GrowthWorkbenchPanel>();
            panel.Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres");
            panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stage.AddChild(panel);
            panel.Bind(app);
            await Frames(5);

            var producerId = run.Roster.First(hero => producers.Contains(hero.ContentId)).InstanceId;
            await Click(panel.GetNode<Button>("%ApplyAssignment"));
            run = app.ActiveRun!;
            var producer = run.Roster.Single(hero => hero.InstanceId == producerId);
            Require(producer.Growth!.ProductionTargetInstanceId == producer.InstanceId, "真实点击保存生产者、目标与模式");
            Require(panel.GetNode<Label>("%ProductionPreview").Text.Contains("下一次完成非终局节点"), "展示永久增量与产出时机");

            var ascend = panel.GetNode<Button>("%Ascend");
            var rosterBeforeDiscovery = app.ActiveRun!.Roster.Count;
            ascend.GrabFocus();
            await Key(Godot.Key.Enter);
            run = app.ActiveRun!;
            var growth = run.Growth!;
            Require(growth.PendingDiscovery?.CandidateIds.Count == 3, "键盘升阶后保留固定三选一");
            var choice = panel.GetNode<VBoxContainer>("%DiscoveryChoices").GetChildren().OfType<Button>().First();
            await Click(choice);
            run = app.ActiveRun!;
            growth = run.Growth!;
            Require(growth.PendingDiscovery is null && run.Roster.Count == rosterBeforeDiscovery + 1, "真实点击领取发现英雄");

            await Click(panel.GetNode<Button>("%CraftSpell"));
            run = app.ActiveRun!;
            growth = run.Growth!;
            var selectedSpell = rules.Spells.Keys.First(id => growth.SpellInventory.GetValueOrDefault(id) > 0);
            Select(panel.GetNode<OptionButton>("%Spell"), selectedSpell);
            await Click(panel.GetNode<Button>("%EquipSpell"));
            run = app.ActiveRun!;
            growth = run.Growth!;
            Require(growth.EquippedSpellId == selectedSpell && growth.SpellInventory[selectedSpell] == 1,
                "预设不提前消费法术库存");
            Require(panel.GetNode<Label>("%EquippedSpell").Text.Contains("真正开始战斗时消耗"), "消费前目标预设可见");
            Require(save.Writes >= 5, "命令均写入隔离存档");
            if (DisplayServer.GetName() != "headless")
            {
                const string output = "res://design-discussion/04-content-validation/artifacts/growth-route/runtime/growth-workbench.png";
                DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://design-discussion/04-content-validation/artifacts/growth-route/runtime"));
                await Frames(3);
                Require(_stage.Viewport.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath(output)) == Error.Ok, "保存成长工坊实际渲染截图");
            }
            GD.Print("GROWTH_WORKBENCH_INPUT_OK assignment ascension-discovery keyboard spell-preset");
        }
        catch (Exception error) { exit = 1; GD.PrintErr("GROWTH_WORKBENCH_INPUT_FAILED " + error); }
        GetTree().Quit(exit);
    }

    private async Task Click(Control target)
    {
        await Reveal(target);
        var point = target.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(4);
    }
    private async Task Key(Key key) { _stage.Push(new InputEventKey { Keycode = key, Pressed = true }); _stage.Push(new InputEventKey { Keycode = key, Pressed = false }); await Frames(4); }
    private async Task Reveal(Control target)
    {
        for (var ancestor = target.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
        {
            if (ancestor is not ScrollContainer scroll) continue;
            scroll.EnsureControlVisible(target); await Frames(3);
        }
        Require(target.IsVisibleInTree() && _stage.Viewport.GetVisibleRect().HasPoint(target.GetGlobalRect().GetCenter()), "输入目标可见：" + target.Name);
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static void Select(OptionButton menu, string id) { for (var i = 0; i < menu.ItemCount; i++) if (menu.GetItemMetadata(i).AsString() == id) { menu.Select(i); menu.EmitSignal(OptionButton.SignalName.ItemSelected, i); return; } }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private sealed class MemorySave : IRunSaveService
    {
        private string? _json;
        public int Writes { get; private set; }
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => _json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(_json);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { Writes++; _json = JsonSerializer.Serialize(value); return true; }
        public void DeleteActiveRun() => _json = null;
    }
}
