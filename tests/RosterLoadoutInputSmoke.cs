using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Attributes;
using TowerAutobattler.Composition;
using TowerAutobattler.Effects;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// A composed GameRoot check: real pointer/key input, native drag/drop and rendered
// frames. Ownership writes go only to MemorySave; root startup has a unique test namespace.
public partial class RosterLoadoutInputSmoke : Control
{
    private string _step = "startup";

    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered window required");
            GetWindow().Size = new Vector2I(1600, 900);
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var fixture = System.IO.File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/fixtures/first-boss-ranged-stall.json"));
            var save = new MemorySave(fixture);
            var app = new RunApplication(package.Content, save, package.Project);
            var run = app.ActiveRun ?? throw new InvalidOperationException("memory fixture rejected");
            // Fill only the legal reserve capacity through production recruitment.
            // This exercises multiple card rows without an invalid oversized roster.
            var originals = run.Roster.ToArray();
            var targetCount = run.Deployment.Count(id => !string.IsNullOrEmpty(id)) + app.Rules.ReserveCapacity;
            while (run.Roster.Count < targetCount)
            {
                Require(app.Recruit(package.Project.Campaign.RecruitmentPool.ContentIds.First(id =>
                    run.Roster.All(hero => hero.ContentId != id))), "legal reserve recruitment fixture");
            }
            Require(app.GrantItem("equipment_rimebrand") && app.GrantItem("equipment_vanguard_insignia"), "equipment fixture");
            var heroA = originals[0].InstanceId;
            var heroB = originals[1].InstanceId;
            var blade = run.EquipmentInventory.Single(item => item.ContentId == "equipment_rimebrand").InstanceId;
            var armor = run.EquipmentInventory.Single(item => item.ContentId == "equipment_vanguard_insignia").InstanceId;

            var game = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
            game.SaveNamespace = $"tests/roster-loadout/{Guid.NewGuid():N}";
            AddChild(game);
            for (var frame = 0; frame < 180 && game.Content is null; frame++) await Frames(1);
            Require(game.Content is not null, "production root ready in isolated namespace");
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var army = game.GetNode<ArmyOverviewController>("ArmyOverview");
            screens.BindEquipmentManagement(app);
            screens.Deployment.Bind(app, app.CurrentEncounter());
            screens.Show(AppScreenId.Deployment, run, app.Content, app.Rules);
            await Frames(4);

            _step = "fullscreen composed popup";
            var opener = screens.Deployment.GetNode<Button>("%EquipmentButton");
            var popup = screens.Deployment.GetNode<ContextPopup>("%EquipmentPopup");
            await Click(opener);
            Require(popup.IsOpen && popup.Blocking, "equipment entry opens blocking roster manager");
            var view = screens.Deployment.GetNode<RosterLoadoutView>("%EquipmentLoadoutPanel");
            var panel = popup.GetNode<Control>("Panel");
            Fullscreen(panel);
            Require(Cards(view).Count() == run.Roster.Count, "all owned heroes, including reserves, have cards");
            FirstRowFits(view);
            var resource = army.GetNode<Button>("%SummaryButton");
            Require(panel.GetGlobalRect().HasPoint(resource.GetGlobalRect().GetCenter()), "manager covers global resource strip");
            await Click(resource);
            Require(popup.IsOpen && !army.IsOpen, "covered resource entry cannot open a second window through manager");
            await Capture("fullscreen");

            _step = "whole portrait selects hero";
            await Click(Card(view, heroA).GetNode<UnitInfoCard>("%Information").GetNode<Control>("%Portrait"));
            await Click(Card(view, heroB).GetNode<UnitInfoCard>("%Information").GetNode<Control>("%Portrait"));
            Require(view.SelectedHeroId == heroB && screens.Deployment.SelectedPieceId == heroB,
                "hero artwork selects the entire card without clicking the name");
            var before = JsonSerializer.Serialize(run);
            await Click(Item(view, blade));
            await Click(Slot(view, heroA, 0));
            Require(JsonSerializer.Serialize(run) == before, "inspection clicks do not equip");

            _step = "inventory equips and replaces";
            await Drag(Item(view, blade), Slot(view, heroA, 0));
            Require(Holds(run, heroA, blade, 0), "native inventory drag equips selected slot");
            await Drag(Item(view, armor), Slot(view, heroA, 0));
            Require(Holds(run, heroA, armor, 0) && run.EquipmentInventory.Any(item => item.InstanceId == blade),
                "occupied slot replacement preserves old item in inventory");
            FirstRowFits(view);
            await Capture("replacement");

            _step = "card transfer and inventory return";
            await Drag(Slot(view, heroA, 0), Card(view, heroB).GetNode<UnitInfoCard>("%Information").GetNode<Control>("%Portrait"));
            Require(!run.Roster.Single(hero => hero.InstanceId == heroA).Equipment.Any(item => item.InstanceId == armor)
                && Holds(run, heroB, armor, 0), "card artwork accepts direct transfer between heroes");
            await Drag(Slot(view, heroB, 0), Item(view, blade));
            Require(run.EquipmentInventory.Any(item => item.InstanceId == armor)
                && !run.Roster.Single(hero => hero.InstanceId == heroB).Equipment.Any(item => item.InstanceId == armor),
                "returning onto an existing inventory tile unequips");
            await Drag(Item(view, armor), Slot(view, heroB, 0));
            var returnZone = view.GetNode<Control>("%RosterReturnZone");
            await Drag(Slot(view, heroB, 0), returnZone.GetGlobalRect().End - new Vector2(16, 16));
            Require(run.EquipmentInventory.Any(item => item.InstanceId == armor), "inventory blank area receives unequip");

            _step = "whole portrait hover exposes all attributes";
            await Hover(Card(view, heroA).GetNode<UnitInfoCard>("%Information").GetNode<Control>("%Portrait"));
            var portraitTooltip = VisibleTooltip(view);
            var stats = portraitTooltip.GetNode<CombatRichText>("%Stats").Text;
            foreach (var attribute in Enum.GetValues<CombatAttribute>())
                Require(stats.Contains(EffectModelText.AttributeName(attribute), StringComparison.Ordinal),
                    "whole-card hover must expose attribute " + attribute);
            Require(!string.IsNullOrWhiteSpace(portraitTooltip.GetNode<CombatRichText>("%Loadout").Text),
                "whole-card hover explains trait contributions");
            await Capture("portrait-hover");

            _step = "equipment hover exposes published reactive rules";
            await Hover(Item(view, blade));
            var equipmentTooltip = VisibleTooltip(view);
            var itemRules = equipmentTooltip.GetNode<CombatRichText>("%Abilities").Text;
            var compiledBlade = app.Content.Graph.ResolveEquipment("equipment_rimebrand");
            Require(!compiledBlade.ReactiveStatusBindings.IsEmpty, "fixture has published reactive equipment rules");
            foreach (var binding in compiledBlade.ReactiveStatusBindings)
                Require(itemRules.Contains(binding.Status.DisplayName, StringComparison.Ordinal),
                    "equipment hover must expose compiled status " + binding.Status.DisplayName);
            foreach (var contribution in compiledBlade.TraitContributions)
                Require(app.Content.Graph.TryGetTrait(contribution.TraitId, out var trait)
                    && itemRules.Contains(trait.DisplayName, StringComparison.Ordinal),
                    "equipment hover must expose compiled trait contribution " + contribution.TraitId);
            await Capture("equipment-hover");

            _step = "attribute and ability hover";
            await Hover(Card(view, heroA).GetNode<UnitInfoCard>("%Information").GetNode<UnitCoreStats>("%CoreStats").GetNode<Control>("%DamageFact"));
            Require(!string.IsNullOrWhiteSpace(VisibleTooltip(view).GetNode<CombatRichText>("%Stats").Text),
                "attribute hover opens populated detail tooltip");
            Require(!Descendants<Control>(GetTree().Root).Any(control => control.Name == "DetailExplanationTooltip" && control.IsVisibleInTree()),
                "compact facts must not show native and focus tooltips together");
            await Capture("attribute-hover");
            await Hover(Card(view, heroA).GetNode<UnitInfoCard>("%Information").GetNode<Control>("%Active"));
            Require(!string.IsNullOrWhiteSpace(VisibleTooltip(view).GetNode<CombatRichText>("%Abilities").Text),
                "skill hover opens populated detail tooltip");
            await Capture("skill-hover");

            _step = "reserve scrolling and independent inventory";
            var last = Card(view, run.Roster[^1].InstanceId);
            var scroll = view.GetNode<ScrollContainer>("%RosterHeroScroll");
            Require(scroll.GetVScrollBar().MaxValue > scroll.GetVScrollBar().Page, "many heroes create scrollable content");
            await Reveal(last);
            await Click(last.GetNode<UnitInfoCard>("%Information").GetNode<Control>("%Portrait"));
            Require(view.SelectedHeroId == run.Roster[^1].InstanceId, "last reserve can be reached and selected with real scrolling");
            Require(GetViewport().GetVisibleRect().Encloses(returnZone.GetGlobalRect()), "inventory stays on screen while hero grid scrolls");
            await Drag(Item(view, blade), last.GetNode<EquipmentSlotButton>("%Slot0"));
            Require(Holds(run, last.HeroId, blade, 0), "visible inventory equips final reserve without leaving screen");
            await Capture("reserve-scroll");

            _step = "keyboard focus and close";
            for (var i = 0; i < 6; i++)
            {
                await KeyPress(Key.Tab);
                var focus = GetViewport().GuiGetFocusOwner();
                Require(focus is not null && popup.IsAncestorOf(focus), "modal Tab focus stays in manager");
            }
            await KeyPress(Key.Escape);
            Require(!popup.IsOpen && !army.IsOpen && GetViewport().GuiGetFocusOwner() == opener,
                "Escape closes manager and restores entry focus");
            await Click(opener);
            await Click(popup.GetNode<Button>("Panel/Layout/Header/Close"));
            Require(!popup.IsOpen, "explicit close works");

            _step = "global army shares full roster manager";
            await Click(resource);
            Require(army.IsOpen, "uncovered resource entry opens global army");
            Fullscreen(army.GetNode<Control>("%Drawer"));
            var globalView = army.GetNode<RosterLoadoutView>("%ArmyEquipmentPanel");
            Require(globalView.IsVisibleInTree() && Cards(globalView).Count() == run.Roster.Count,
                "global army opens all heroes and equipment together");
            await Capture("global-army");
            await KeyPress(Key.Escape);
            Require(!army.IsOpen && !popup.IsOpen, "global manager closes without leaving another popup");
            Require(save.Writes > 0, "equipment commands used isolated persistence");
            GD.Print("ROSTER_LOADOUT_INPUT_OK composed-fullscreen,card-content-contained,header-blocked,portrait-select,inspect-only,equip,replace,transfer,tile-return,blank-return,portrait-all-attributes,equipment-compiled-rules,attribute-hover,skill-hover,reserve-scroll,reserve-equip,modal-focus,escape,global-army");
        }
        catch (Exception exception)
        {
            exit = 1;
            GD.PrintErr($"ROSTER_LOADOUT_INPUT_FAILED step={_step}: {exception}");
            if (DisplayServer.GetName() != "headless") await Capture("failure");
        }
        GetTree().Quit(exit);
    }

    private void FirstRowFits(RosterLoadoutView view)
    {
        var columns = view.GetNode<GridContainer>("%RosterHeroGrid").Columns;
        foreach (var card in Cards(view).Take(columns))
        foreach (var element in Descendants<Control>(card).Where(control => control.IsVisibleInTree() &&
                     control is UnitPortrait or DetailExplainButton or EquipmentSlotButton ||
                     control.IsVisibleInTree() && control.Name.ToString() is "HeroName" or "HeroState" or "AttributeContext" or "Active" or "Passive"))
        {
            Require(card.GetGlobalRect().Grow(.5f).Encloses(element.GetGlobalRect()),
                $"card content outside boundary: hero={card.HeroId}, node={element.Name}, card={card.GetGlobalRect()}, content={element.GetGlobalRect()}");
        }
    }
    private BattleLabTooltip VisibleTooltip(RosterLoadoutView view)
    {
        var visible = Descendants<BattleLabTooltip>(view).Where(hint => hint.Visible).ToArray();
        Require(visible.Length == 1, "hover must show exactly one real detail tooltip");
        var tooltip = visible[0];
        Require(GetViewport().GetVisibleRect().Grow(2).Encloses(tooltip.GetNode<Control>("%TooltipPanel").GetGlobalRect()),
            "hover details must fit inside the viewport");
        return tooltip;
    }
    private void Fullscreen(Control panel)
    {
        var viewport = GetViewport().GetVisibleRect();
        var rect = panel.GetGlobalRect();
        Require(panel.IsVisibleInTree() && viewport.Grow(2).Encloses(rect)
            && rect.Size.X >= viewport.Size.X * .95f && rect.Size.Y >= viewport.Size.Y * .9f,
            $"manager should fill viewport without clipping: viewport={viewport}, panel={rect}");
    }
    private static bool Holds(ActiveRunDto run, string hero, string item, int slot) =>
        run.Roster.Single(value => value.InstanceId == hero).Equipment.Any(value => value.InstanceId == item && value.SlotIndex == slot);
    private static IEnumerable<RosterHeroCard> Cards(RosterLoadoutView view) => Descendants<RosterHeroCard>(view);
    private static RosterHeroCard Card(RosterLoadoutView view, string id) => Cards(view).Single(card => card.HeroId == id);
    private static EquipmentSlotButton Slot(RosterLoadoutView view, string hero, int index) => Card(view, hero).GetNode<EquipmentSlotButton>("%Slot" + index);
    private static EquipmentSlotButton Item(RosterLoadoutView view, string id) =>
        Descendants<EquipmentSlotButton>(view.GetNode<Control>("%RosterInventory")).Single(tile => tile.InstanceId == id);

    private async Task Click(Control target) { await Reveal(target); await ClickPoint(target.GetGlobalRect().GetCenter()); }
    private async Task ClickPoint(Vector2 point)
    {
        Input.WarpMouse(point); await Frames(1);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }
    private async Task Hover(Control target)
    {
        await Reveal(target);
        var point = target.GetGlobalRect().GetCenter();
        Input.WarpMouse(point);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await ToSignal(GetTree().CreateTimer(.6), SceneTreeTimer.SignalName.Timeout);
        await Frames(3);
    }
    private async Task Drag(Control source, Control target)
    {
        await Reveal(target); await Reveal(source);
        await Drag(source, target.GetGlobalRect().GetCenter());
    }
    private async Task Drag(Control source, Vector2 destination)
    {
        await Reveal(source);
        var start = source.GetGlobalRect().GetCenter();
        Require(GetViewport().GetVisibleRect().HasPoint(start) && GetViewport().GetVisibleRect().HasPoint(destination), "drag endpoints must be visible");
        Input.WarpMouse(start); await Frames(1);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = start, GlobalPosition = start });
        Input.ParseInputEvent(new InputEventMouseButton { Position = start, GlobalPosition = start, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        for (var i = 1; i <= 8; i++)
        {
            var point = start.Lerp(destination, i / 8f);
            Input.WarpMouse(point);
            Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = (destination - start) / 8, ButtonMask = MouseButtonMask.Left });
            await Frames(1);
        }
        GD.Print($"ROSTER_DRAG step={_step} source={source.GetPath()} target={destination} native={GetViewport().GuiIsDragging()} hover={GetViewport().GuiGetHoveredControl()?.GetPath()}");
        // Windows cursor refresh can race a rendered frame. Reassert the target
        // motion immediately before release, through the same real input path.
        Input.ParseInputEvent(new InputEventMouseMotion { Position = destination, GlobalPosition = destination, ButtonMask = MouseButtonMask.Left });
        Input.ParseInputEvent(new InputEventMouseButton { Position = destination, GlobalPosition = destination, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(4);
    }
    private async Task Reveal(Control target)
    {
        Require(target.IsVisibleInTree(), "target hidden: " + target.GetPath());
        for (var ancestor = target.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
        {
            if (ancestor is not ScrollContainer scroll) continue;
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var rect = target.GetGlobalRect(); var clip = scroll.GetGlobalRect();
                if (clip.Grow(2).Encloses(rect)) break;
                var point = clip.GetCenter();
                var direction = rect.Position.Y < clip.Position.Y ? MouseButton.WheelUp : MouseButton.WheelDown;
                Input.WarpMouse(point); await Frames(1);
                Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point });
                Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = direction, Pressed = true });
                Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = direction, Pressed = false });
                await Frames(2);
            }
            Require(scroll.GetGlobalRect().HasPoint(target.GetGlobalRect().GetCenter()), "target cannot be scrolled into view: " + target.GetPath());
        }
    }
    private async Task KeyPress(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = false });
        await Frames(3);
    }
    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
    private async Task Capture(string name)
    {
        await Frames(2);
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://.godot/ui-review"));
        GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://.godot/ui-review/roster-{name}.png"));
    }
    private static IEnumerable<T> Descendants<T>(Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T item) yield return item;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class MemorySave : IRunSaveService
    {
        private string? _json;
        public MemorySave(string json) => _json = json;
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
