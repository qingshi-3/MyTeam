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
    private readonly UiDragMotionRecorder _motion = new();

    // Isolated mode renders and drives a dedicated viewport, without native mouse state.
    private UiInputStage _stage = null!;
    private bool IsolatedPointer => Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--isolated-pointer");
    private void WarpPointer(Vector2 point) => _stage.WarpPointer(point);

    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered window required");
            GetWindow().Size = new Vector2I(1600, 900);
            // An off-screen native host may stop presenting frames on macOS.
            // Input still goes only to the isolated SubViewport.
            if (IsolatedPointer) GetWindow().Position = new Vector2I(80, 80);
            _stage = new UiInputStage(this, IsolatedPointer);
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
            _stage.AddChild(game);
            for (var frame = 0; frame < 180 && game.Content is null; frame++) await Frames(1);
            Require(game.Content is not null, "production root ready in isolated namespace");
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var army = game.GetNode<ArmyOverviewController>("ArmyOverview");
            screens.BindEquipmentManagement(app);
            screens.Deployment.Bind(app, app.CurrentEncounter());
            screens.Show(AppScreenId.Deployment, RunView.For(run), app.Content, app.Rules);
            await Frames(4);

            _step = "composed workbench";
            var opener = screens.Deployment.GetNode<Button>("%EquipmentButton");
            var popup = screens.Deployment.GetNode<ContextPopup>("%EquipmentPopup");
            var bench = screens.Deployment.GetNode<Control>("%ReserveBench");
            var board = screens.Deployment.GetNode<Control>("%DeploymentBoard");
            var boardRect = board.GetGlobalRect();
            var benchTint = bench.Modulate;
            await Click(opener);
            Require(popup.IsOpen && popup.Blocking, "equipment entry opens blocking roster manager");
            var view = screens.Deployment.GetNode<RosterLoadoutView>("%EquipmentLoadoutPanel");
            var panel = popup.GetNode<Control>("Panel");
            Fullscreen(panel);
            Require(Selectors(view).Count() == run.Roster.Count && Cards(view).Count() == 1,
                "all heroes have selectors and only selected hero has a large card");
            Require(popup.GetNode<ColorRect>("Backdrop").Color.A == 0, "live scene remains visible through outer backdrop");
            Require(bench.Modulate.A == 0 && board.Modulate.A > 0 && board.GetGlobalRect() == boardRect,
                "inspection hides reserve HUD without hiding or moving the live board");
            foreach (var selector in Selectors(view))
            {
                var portrait = selector.GetNode<Control>("%Portrait");
                Require(Mathf.IsEqualApprox(portrait.Size.X, portrait.Size.Y),
                    "roster avatar and frame remain circular: " + selector.HeroId);
            }
            FirstRowFits(view);
            var resource = army.GetNode<Button>("%SummaryButton");
            await Click(resource);
            Require(popup.IsOpen && !army.IsOpen, "covered resource entry cannot open a second window through manager");
            await Capture("fullscreen");
            var orbit = view.GetNode<RosterHeroCard>("%SelectedCard").Orbit;
            var information = view.GetNode<RosterHeroCard>("%SelectedCard").GetNode<WorkbenchHeroSummary>("%Information");
            Require(!view.GetNode<RosterHeroCard>("%SelectedCard").HasNode("DetailsButton") && orbit.AlwaysVisible &&
                view.Disclosure.IsOpen && view.Disclosure.Groups.All(group => group.IsVisibleInTree()),
                "opening the manager immediately shows attributes, skills and slots without a details button");
            await Hover(popup.GetNode<Button>("Panel/Layout/Header/Close"));
            Require(view.Disclosure.Groups.All(group => group.IsVisibleInTree()) &&
                information.GetNode<Control>("%Portrait").IsVisibleInTree(), "leaving the hero card never hides its information");
            var core = information.GetNode<Control>("Artwork/CoreStats").GetGlobalRect();
            var attackLeft = Fact(view, CombatAttribute.AttackDamage).GetNode<Control>("%FactIcon").GetGlobalRect().Position.X;
            var healthRight = Fact(view, CombatAttribute.MaxHealth).GetNode<Control>("%FactValue").GetGlobalRect().End.X;
            var spell = Fact(view, CombatAttribute.SpellPower);
            var spellCenter = (spell.GetNode<Control>("%FactIcon").GetGlobalRect().Position.X +
                spell.GetNode<Control>("%FactValue").GetGlobalRect().End.X) / 2;
            Require(Math.Abs((attackLeft - core.Position.X) - (core.End.X - healthRight)) <= 2 &&
                Math.Abs(spellCenter - core.GetCenter().X) <= 2, "attack and health have symmetric outer margins with spell power centered");
            Require(orbit.GetNode<Control>("Right").GetChildren().OfType<CompactDetailFact>().All(fact =>
                fact.GetNode<Control>("%FactValue").GetGlobalRect().End.X <= fact.GetNode<Control>("%FactIcon").GetGlobalRect().Position.X),
                "right attributes place values before edge-aligned icons");
            var frameCenter = view.GetNode<RosterHeroCard>("%SelectedCard").GetGlobalRect().GetCenter().X;
            Require(orbit.GetNode<Control>("Left").GetGlobalRect().End.X < frameCenter &&
                orbit.GetNode<Control>("Right").GetGlobalRect().Position.X > frameCenter,
                "mirroring the right entries does not move their column across the card");
            await Capture("always-visible");
            var edgeShade = orbit.GetNode<Control>("EdgeShade");
            Require(orbit.ShowEquipment && view.Disclosure.Groups.Count == 4 &&
                Enumerable.Range(0, 3).All(index => orbit.Slot(index).IsVisibleInTree()),
                "equipment management explicitly enables all three real equipment slots");
            Require(edgeShade.ZAsRelative && orbit.ZIndex + edgeShade.ZIndex == 1 &&
                information.GetNode<Control>("%HeroName").ZIndex > orbit.ZIndex + edgeShade.ZIndex,
                "edge shade follows the modal canvas above art and below the name rail");
            if (Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--layout-only"))
            {
                await Frames(20);
                var cardFrame = view.GetNode<RosterHeroCard>("%SelectedCard").GetNode<Control>("CardFrame").GetGlobalRect();
                Require(view.Disclosure.Groups.All(group => cardFrame.Encloses(group.GetGlobalRect())),
                    "all four groups settle inside the wider roster frame");
                await Capture("inner-frame");
                GD.Print("ROSTER_INNER_LAYOUT_OK real-open always-visible no-details-button symmetric-core mirrored-right inner-edges broad-frame");
                GetTree().Quit(); return;
            }
            var before = JsonSerializer.Serialize(run);
            foreach (var attribute in UnitCardOrbit.DisplayedAttributes.Append(CombatAttribute.StartingMana))
            {
                var fact = Fact(view, attribute);
                Require(fact.IsVisibleInTree() && !string.IsNullOrWhiteSpace(fact.GetNode<Label>("%FactValue").Text),
                    "card includes displayed zero values: " + attribute);
                Require(Card(view, view.SelectedHeroId).GetGlobalRect().Encloses(fact.GetGlobalRect()),
                    "every attribute is readable without clipping at reference viewport: " + attribute);
            }
            // Explicit nonzero spell/critical values exercise presentation without altering
            // authored definitions, equipment rules, or the persistent run.
            var fixtureEntry = app.Content.TryGet(run.Roster[0].ContentId, out var found) ? found
                : throw new InvalidOperationException("fixture hero missing");
            var fixtureBaseline = TowerAutobattler.Battle.BattleSetupFactory.Snapshot(fixtureEntry, app.Content);
            var values = Enum.GetValues<CombatAttribute>().ToDictionary(attribute => attribute,
                attribute => fixtureBaseline.AttributeDefinition!.Attributes.FirstOrDefault(value => value.Attribute == attribute)?.BaseValue ?? 0);
            values[CombatAttribute.SpellPower] = 37;
            values[CombatAttribute.CriticalChance] = .275f;
            values[CombatAttribute.CriticalDamage] = 1.85f;
            values[CombatAttribute.StartingMana] = 20;
            values[CombatAttribute.MaxMana] = 60;
            var valueModel = new UnitInformation("value-fixture", (TowerAutobattler.Content.UnitDefinition)fixtureEntry.Definition,
                fixtureBaseline, new PreparedUnitDetails(fixtureBaseline, values, values[CombatAttribute.MaxHealth], 0));
            information.Bind(valueModel); orbit.Bind(valueModel);
            await Frames(3);
            Require(orbit.Fact(CombatAttribute.MaxMana).GetNode<Label>("%FactValue").Text == "60" &&
                orbit.Fact(CombatAttribute.StartingMana).GetNode<Label>("%FactValue").Text == "20",
                "starting mana and maximum mana occupy separate rows");
            foreach (var (attribute, expected) in new[] { (CombatAttribute.SpellPower, "37"),
                         (CombatAttribute.CriticalChance, "27.5%"), (CombatAttribute.CriticalDamage, "185%") })
                Require(information.GetNode<CompactDetailFact>("%" + attribute).GetNode<Label>("%FactValue").Text == expected
                    && Fact(view, attribute).GetNode<Label>("%FactValue").Text == expected,
                    "nonzero spell power and distinct critical values agree: " + attribute);
            await Click(Selector(view, heroA));

            _step = "left selector and keyboard activation";
            await Click(Selector(view, heroB).GetNode<Control>("%Portrait"));
            Require(view.SelectedHeroId == heroB && screens.Deployment.SelectedPieceId == heroB,
                "portrait selects hero in both manager and deployment");
            await Click(Selector(view, heroA));
            await KeyPress(Key.Space);
            Require(view.SelectedHeroId == heroA && Card(view, heroA).HeroId == heroA, "focused selector activates by keyboard");
            Require(view.Disclosure.Groups.All(group => group.IsVisibleInTree()), "switching heroes preserves the visible information");
            Require(JsonSerializer.Serialize(run) == before, "hero inspection changes no persistent state");
            await Click(Item(view, blade));
            await Click(Slot(view, heroA, 0));
            Require(JsonSerializer.Serialize(run) == before, "item and slot inspection do not equip");

            _step = "inventory equips and replaces";
            var initialArmor = orbit.Fact(CombatAttribute.Armor).GetNode<Label>("%FactValue").Text;
            await Drag(Item(view, blade), Slot(view, heroA, 0));
            Require(Holds(run, heroA, blade, 0), "native inventory drag equips selected slot");
            var armorFact = orbit.Fact(CombatAttribute.Armor);
            await Click(armorFact);
            Require(view.Disclosure.IsOpen, "attributes remain visible before equipment drag");
            await Drag(Item(view, armor), Slot(view, heroA, 0));
            Require(Holds(run, heroA, armor, 0) && run.EquipmentInventory.Any(item => item.InstanceId == blade),
                "occupied slot replacement preserves old item in inventory");
            var armorValue = orbit.Fact(CombatAttribute.Armor).GetNode<Label>("%FactValue").Text;
            Require(initialArmor != armorValue && armorValue == information.GetNode<CompactDetailFact>("%Armor").GetNode<Label>("%FactValue").Text,
                "equipment refreshes the effective sheet and card from the same values");
            Require(armorFact.ExplanationText.Contains(armorValue), "live per-attribute rules refresh after replacement");
            Require(view.Disclosure.Groups.All(group => group.IsVisibleInTree()), "equipment refresh keeps all information visible");
            FirstRowFits(view);
            await Capture("replacement");

            _step = "card transfer and inventory return";
            await Drag(Slot(view, heroA, 0), Selector(view, heroB));
            Require(Holds(run, heroB, armor, 0) && view.SelectedHeroId == heroB,
                "drop on compact selector transfers item and inspects its new owner");
            await Drag(Slot(view, heroB, 0), Item(view, blade));
            Require(run.EquipmentInventory.Any(item => item.InstanceId == armor), "drop onto inventory tile unequips");
            await Drag(Item(view, armor), Slot(view, heroB, 0));
            var returnZone = view.GetNode<Control>("%RosterReturnZone");
            await Drag(Slot(view, heroB, 0), returnZone.GetGlobalRect().End - new Vector2(4, 4));
            Require(run.EquipmentInventory.Any(item => item.InstanceId == armor), "inventory blank area receives unequip");

            _step = "individual attribute and skill tooltips";
            var healthFact = Fact(view, CombatAttribute.MaxHealth);
            await Hover(healthFact);
            Require(VisibleTooltip(view).GetNode<CombatRichText>("%SingleEntry").Text.Contains("满血"), "health entry explains full-health start");
            await Capture("health-symbol-hover");
            await Click(healthFact);
            Require(information.GetNode<Control>("%Portrait").IsVisibleInTree(), "single fact never replaces portrait");
            foreach (var attribute in new[] { CombatAttribute.AttackSpeed, CombatAttribute.HealingPower })
            {
                var fact = Fact(view, attribute);
                await Hover(fact);
                Require(VisibleTooltip(view).GetNode<Label>("%Title").Text == fact.ExplanationTitle, "one fact one title");
                Require(VisibleTooltip(view).GetNode<CombatRichText>("%SingleEntry").Text == fact.ExplanationText, "full rule only for this fact");
                await Click(fact); await KeyPress(Key.Space);
                Require(fact.HasFocus(), "keyboard inspection retains its own source");
            }
            var active = view.Disclosure.Groups[2].GetChildren().OfType<Button>().First();
            await Hover(active);
            Require(!string.IsNullOrWhiteSpace(VisibleTooltip(view).GetNode<CombatRichText>("%SingleEntry").Text), "single skill has full rules");
            await Capture("complete-details");

            _step = "equipment published rules";
            await Hover(Item(view, blade));
            var itemRules = VisibleTooltip(view).GetNode<CombatRichText>("%Abilities").Text;
            var compiledBlade = app.Content.Graph.ResolveEquipment("equipment_rimebrand");
            foreach (var binding in compiledBlade.ReactiveStatusBindings)
                Require(itemRules.Contains(binding.Status.DisplayName, StringComparison.Ordinal), "compiled reactive equipment rules available");
            await Capture("equipment-hover");

            _step = "reserve scrolling and equip";
            var last = Selector(view, run.Roster[^1].InstanceId);
            var scroll = view.GetNode<ScrollContainer>("%RosterHeroScroll");
            Require(scroll.GetVScrollBar().MaxValue > scroll.GetVScrollBar().Page, "reserve list scrolls");
            await Click(last);
            Require(view.SelectedHeroId == last.HeroId, "last reserve selected through real wheel input");
            Require(_stage.Viewport.GetVisibleRect().Encloses(returnZone.GetGlobalRect()), "inventory stays visible during list scroll");
            await Drag(Item(view, blade), Slot(view, last.HeroId, 0));
            Require(Holds(run, last.HeroId, blade, 0), "reserve equipment uses same command path");
            Require(Fact(view, CombatAttribute.Armor).ExplanationText.Contains("自身与装备", StringComparison.Ordinal),
                "reserve preview states its scope instead of claiming positional bonuses");
            FirstRowFits(view);
            await Capture("reserve-scroll");

            _step = "modal keyboard and close";
            for (var i = 0; i < 6; i++)
            {
                await KeyPress(Key.Tab);
                var focus = _stage.Viewport.GuiGetFocusOwner();
                Require(focus is not null && popup.IsAncestorOf(focus), "modal Tab stays in manager");
            }
            await KeyPress(Key.Escape);
            Require(!popup.IsOpen && _stage.Viewport.GuiGetFocusOwner() == opener, "Escape closes and restores opener");
            Require(bench.Modulate == benchTint && board.GetGlobalRect() == boardRect,
                "closing inspection restores reserve presentation and original board geometry");
            await Click(opener);
            Require(view.Disclosure.Groups.All(group => group.IsVisibleInTree()), "reopening the manager restores all information immediately");
            await Click(popup.GetNode<Button>("Panel/Layout/Header/Close"));
            Require(!popup.IsOpen, "explicit close works");

            _step = "shared global manager";
            await Click(resource);
            Require(army.IsOpen, "global army entry opens");
            Require(bench.Modulate.A == 0 && resource.Modulate.A == 0, "global inspection suppresses reserve and resource HUD");
            Fullscreen(army.GetNode<Control>("%Drawer"));
            var globalView = army.GetNode<RosterLoadoutView>("%ArmyEquipmentPanel");
            Require(Selectors(globalView).Count() == run.Roster.Count && Cards(globalView).Count() == 1,
                "global army uses same selected-hero manager");
            FirstRowFits(globalView);
            await Capture("global-army");
            await Click(army.GetNode<Button>("%PageToggle"));
            Require(army.GetNode<TabContainer>("%Pages").CurrentTab == 1, "army summary remains accessible");
            await Click(army.GetNode<Button>("%PageToggle"));
            Require(globalView.IsVisibleInTree(), "summary returns to hero workbench");
            await KeyPress(Key.Escape);
            Require(!army.IsOpen && !popup.IsOpen && save.Writes > 0, "global close and isolated persistence");
            Require(bench.Modulate == benchTint && resource.Modulate.A > 0, "global close restores background HUD");
            _motion.Save("equipment-motion");
            GD.Print("ROSTER_LOADOUT_INPUT_OK selected-workbench,large-art,transparent-scene,always-visible,no-details-button,symmetric-core,15-card-entries,split-initial-max-mana,portrait-selection,keyboard,effective-equipment-values,equip,replace,selector-transfer,tile-return,blank-return,explanation-focus,skills,reserve-scroll,reserve-equip,modal-focus,global-army");
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
        const int columns = 1;
        foreach (var card in Cards(view).Take(columns))
        {
            var information = card.GetNode<WorkbenchHeroSummary>("%Information");
            var artwork = information.GetNode<Control>("%Portrait");
            var ribbon = information.GetNode<Control>("%HeroName");
            var frame = card.GetNode<Control>("CardFrame").GetGlobalRect();
            Require(artwork.GetGlobalRect().End.Y >= frame.End.Y - 24 && ribbon.GetGlobalRect().End.Y >= frame.End.Y - 24,
                "roster portrait and name finish the lower edge without an equipment strip");
            if (view.Disclosure.IsOpen)
            {
                var skills = view.Disclosure.Groups[2].GetGlobalRect();
                var equipment = view.Disclosure.Groups[3].GetGlobalRect();
                Require(skills.End.Y < frame.GetCenter().Y && equipment.End.Y < frame.GetCenter().Y &&
                    Math.Abs(skills.GetCenter().Y - equipment.GetCenter().Y) < 4 && !skills.Intersects(equipment),
                    "roster skills and equipment share one non-overlapping top row");
            }
            var share = (artwork.Size.Y - ((UnitPortrait)artwork).IllustrationTopInset - ribbon.Size.Y) / card.GetNode<Control>("CardFrame").Size.Y;
            Require(share >= .60f, $"roster art excluding name ribbon must occupy at least 3/5 of card height: {share:P1}, art={artwork.Size}, ribbon={ribbon.Size}, card={card.Size}");
            GD.Print($"HERO_ART_SHARE roster={card.HeroId} visible-height={share:P1}");
        }
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
        Require(_stage.Viewport.GetVisibleRect().Grow(2).Encloses(tooltip.GetNode<Control>("%TooltipPanel").GetGlobalRect()),
            "hover details must fit inside the viewport");
        return tooltip;
    }
    private void Fullscreen(Control panel)
    {
        var viewport = _stage.Viewport.GetVisibleRect();
        var rect = panel.GetGlobalRect();
        Require(panel.IsVisibleInTree() && viewport.Grow(2).Encloses(rect)
            && rect.Size.X >= viewport.Size.X * .95f && rect.Size.Y >= viewport.Size.Y * .9f,
            $"manager should fill viewport without clipping: viewport={viewport}, panel={rect}");
    }
    private static bool Holds(ActiveRunDto run, string hero, string item, int slot) =>
        run.Roster.Single(value => value.InstanceId == hero).Equipment.Any(value => value.InstanceId == item && value.SlotIndex == slot);
    private static IEnumerable<RosterHeroSelector> Selectors(RosterLoadoutView view) => Descendants<RosterHeroSelector>(view);
    private static RosterHeroSelector Selector(RosterLoadoutView view, string id) => Selectors(view).Single(row => row.HeroId == id);
    private static IEnumerable<RosterHeroCard> Cards(RosterLoadoutView view) => Descendants<RosterHeroCard>(view);
    private static RosterHeroCard Card(RosterLoadoutView view, string id) => Cards(view).Single(card => card.HeroId == id);
    private static EquipmentSlotButton Slot(RosterLoadoutView view, string hero, int index) => Card(view, hero).Slot(index);
    private static EquipmentSlotButton Item(RosterLoadoutView view, string id) =>
        Descendants<EquipmentSlotButton>(view.GetNode<Control>("%RosterInventory")).Single(tile => tile.InstanceId == id);

    private static CompactDetailFact Fact(RosterLoadoutView view, CombatAttribute attribute)
        => attribute is CombatAttribute.AttackDamage or CombatAttribute.SpellPower or CombatAttribute.MaxHealth
            ? Card(view, view.SelectedHeroId).GetNode<WorkbenchHeroSummary>("%Information").GetNode<CompactDetailFact>("%" + attribute)
            : Card(view, view.SelectedHeroId).Orbit.Fact(attribute);
    private async Task Click(Control target) { await Reveal(target); await ClickPoint(target.GetGlobalRect().GetCenter()); }
    private async Task ClickPoint(Vector2 point)
    {
        WarpPointer(point); await Frames(1);
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await ToSignal(GetTree().CreateTimer(.32), SceneTreeTimer.SignalName.Timeout);
        await Frames(3);
    }
    private async Task Hover(Control target)
    {
        await Reveal(target);
        var point = target.GetGlobalRect().GetCenter();
        for (var attempt = 0; attempt < 3; attempt++)
        {
        WarpPointer(point);
        await Frames(1);
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await ToSignal(GetTree().CreateTimer(.6), SceneTreeTimer.SignalName.Timeout);
        await Frames(3);
        GD.Print($"ROSTER_HOVER target={target.GetPath()} point={point} mouse={_stage.Viewport.GetMousePosition()} hovered={_stage.Viewport.GuiGetHoveredControl()?.GetPath()}");
        if (IsolatedPointer)
        {
            var hovered = _stage.Viewport.GuiGetHoveredControl();
            // Desktop mouse coordinates stay native even when this window is off-screen.
            // Check the GUI hit result of the injected event, then the actual tooltip below.
            Require(hovered == target || (hovered is not null && target.IsAncestorOf(hovered)) ||
                (target is UnitPortrait && hovered is RosterHeroCard && hovered.IsAncestorOf(target)),
                "isolated pointer did not hit the intended control");
            return;
        }
        // A native cursor update can displace the real pointer during the hover delay.
        // Retry only that observed displacement, never a missing tooltip at the correct point.
        if (_stage.Viewport.GetMousePosition().DistanceTo(point) <= 2) return;
        }
        throw new InvalidOperationException("Pointer kept moving during hover verification: " + target.GetPath());
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
        Require(_stage.Viewport.GetVisibleRect().HasPoint(start) && _stage.Viewport.GetVisibleRect().HasPoint(destination), "drag endpoints must be visible");
        WarpPointer(start); await Frames(1);
        _stage.Push(new InputEventMouseMotion { Position = start, GlobalPosition = start });
        _stage.Push(new InputEventMouseButton { Position = start, GlobalPosition = start, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        for (var i = 1; i <= 20; i++)
        {
            var point = start.Lerp(destination, i / 20f);
            WarpPointer(point);
            _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = (destination - start) / 20, ButtonMask = MouseButtonMask.Left });
            await Frames(2);
        }
        GD.Print($"ROSTER_DRAG step={_step} source={source.GetPath()} target={destination} native={_stage.Viewport.GuiIsDragging()} hover={_stage.Viewport.GuiGetHoveredControl()?.GetPath()}");
        if (source is EquipmentSlotButton)
        {
            var preview = Descendants<Control>(GetTree().Root).FirstOrDefault(node =>
                node.Name == "EquipmentDragPreview" && node.IsVisibleInTree());
            Require(preview is not null && !string.IsNullOrWhiteSpace(preview.GetNode<Label>("Layout/Name").Text),
                "native drag preview keeps the item name when the source socket is icon-only");
            // The presentation now lives in the same viewport as its input source.
            if (_step == "card transfer and inventory return" && destination.X < 1000 && destination.Y < 650)
                await Capture("equipment-drag");
        }
        // Windows cursor refresh can race a rendered frame. Reassert the target
        // motion immediately before release, through the same real input path.
        _stage.Push(new InputEventMouseMotion { Position = destination, GlobalPosition = destination, ButtonMask = MouseButtonMask.Left });
        _stage.Push(new InputEventMouseButton { Position = destination, GlobalPosition = destination, ButtonIndex = MouseButton.Left, Pressed = false });
        var until = Time.GetTicksMsec() + 280;
        while (Time.GetTicksMsec() < until) await Frames(1);
        Require(!Descendants<UiDragVisual>(_stage.Viewport).Any(), "equipment flight cleans up after release");
    }
    private async Task Reveal(Control target)
    {
        if (!target.IsVisibleInTree() && target is EquipmentSlotButton slot && slot.SlotIndex >= 0)
        {
            UnitCardOrbit? orbitOwner = null;
            for (var ancestor = target.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
                if (ancestor is UnitCardOrbit candidate) { orbitOwner = candidate; break; }
            if (orbitOwner is not null)
            {
                var owner = (Control)orbitOwner.GetParent();
                WarpPointer(owner.GetGlobalRect().GetCenter());
                _stage.Push(new InputEventMouseMotion { Position = owner.GetGlobalRect().GetCenter(), GlobalPosition = owner.GetGlobalRect().GetCenter() });
                await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout); await Frames(3);
            }
        }
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
                WarpPointer(point); await Frames(1);
                _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
                _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = direction, Pressed = true });
                _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = direction, Pressed = false });
                await Frames(2);
            }
            Require(scroll.GetGlobalRect().HasPoint(target.GetGlobalRect().GetCenter()), "target cannot be scrolled into view: " + target.GetPath());
        }
    }
    private async Task KeyPress(Key key)
    {
        _stage.Push(new InputEventKey { Keycode = key, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = key, Pressed = false });
        await ToSignal(GetTree().CreateTimer(.32), SceneTreeTimer.SignalName.Timeout);
        await Frames(3);
    }
    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            // macOS can suspend native presentation when the host is occluded.
            // Render the isolated viewport without depending on a desktop swap.
            if (IsolatedPointer) RenderingServer.ForceDraw(false);
            else await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            if (_step is "card transfer and inventory return" or "inventory equips and replaces")
                _motion.Sample(_stage.Viewport, _step);
        }
    }
    private async Task Capture(string name)
    {
        await Frames(2);
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://.godot/ui-review"));
        _stage.Viewport.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://.godot/ui-review/roster-{name}.png"));
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
