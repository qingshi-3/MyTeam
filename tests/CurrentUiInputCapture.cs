using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.UI;

// Runs the current production screens. Every navigation, selection and scroll goes
// through engine input; reading controls below is observation, not a UI shortcut.
public partial class CurrentUiInputCapture : Node
{
    private const string OutputPath = "res://.godot/ui-review";
    private const string PoisonId = "hero_hc08_poison_keeper";
    private const string CrossbowId = "hero_hc01_crossbow";
    private string _step = "boot";
    private readonly List<string> _captures = [];

    public override async void _Ready()
    {
        var code = 0;
        GameRoot? game = null;
        try
        {
            Require(DisplayServer.GetName() != "headless", "Rendered capture requires a display (do not pass --headless).");
            GetWindow().Size = new Vector2I(1600, 900);
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(OutputPath));
            game = GD.Load<PackedScene>(ProjectSettings.GetSetting("application/run/main_scene").AsString()).Instantiate<GameRoot>();
            game.SaveNamespace = $"tests/current-ui-capture/{Guid.NewGuid():N}";
            AddChild(game);
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            await Until(() => game.Content is not null && screens.MainMenu.IsVisibleInTree(), "production main menu", 180);
            await Frames(4);

            if (OS.GetCmdlineUserArgs().Contains("--footprints"))
                await InspectDeploymentFootprints(screens);
            else if (OS.GetCmdlineUserArgs().Contains("--tooltip-icons"))
                await InspectTooltipIcons(screens);
            else
            {
            Step("hero selection: real main-menu button");
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "hero selection opened");
            var heroScreen = screens.HeroSelection;
            var tile = Descendants<HeroLibraryTile>(heroScreen).Single(candidate => candidate.StableId == PoisonId);
            await RevealByWheel(heroScreen.GetNode<ScrollContainer>("%LibraryScroll"), tile);
            await Click(tile);
            Require(heroScreen.PreviewStableId == PoisonId, "HC08 click selects current production hero");
            var heroDetail = heroScreen.GetNode<HeroDetailPanel>("%HeroDetailPanel");
            var information = heroDetail.GetNode<UnitDetailView>("%UnitDetails");
            var heroScroll = information.GetNode<ScrollContainer>("%DetailsScroll");
            Require(information.GetNode<UnitVitals>("%Vitals").GetNode<Label>("%ManaValue").Text.Contains("法力", StringComparison.Ordinal), "hero attributes include mana");
            Require(Descendants<RichTextLabel>(information.GetNode<Control>("%SkillList")).Any(copy => copy.Text.Contains("棘毒", StringComparison.Ordinal)), "hero preview uses current NE01 skills");
            InViewport(heroScreen.GetNode<Control>("Margin/Layout/BackButton"), "hero back");
            InViewport(heroDetail.GetNode<Control>("%DeployButton"), "hero deploy");
            InViewport(heroScroll, "hero detail scroll area");
            await Capture("hero-hc08-top");
            Step("hero selection: keyboard focus and full detail scroll");
            for (var index = 0; index < 4; index++)
            {
                await KeyPress(Key.Tab);
                var focus = GetViewport().GuiGetFocusOwner();
                Require(focus is not null && focus.IsVisibleInTree(), "Tab has a visible focus owner");
                GD.Print($"CURRENT_UI_FOCUS {focus!.GetPath()} rect={focus.GetGlobalRect()}");
            }
            await ScrollEnd(heroScroll, true);
            Require(BottomVisible(information.GetNode<Control>("%SkillList"), heroScroll), "hero skill text bottom reachable by wheel");
            await Capture("hero-hc08-bottom");
            await Click(heroScreen.GetNode<Button>("Margin/Layout/BackButton"));
            await Until(() => screens.MainMenu.IsVisibleInTree(), "return to main menu");

            Step("battle laboratory: library selection and independent side panels");
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/BattleLabButton"));
            await Until(() => screens.BattleLab.IsVisibleInTree(), "battle laboratory opened");
            var lab = screens.BattleLab;
            await Frames(4);
            await SelectLibrary(lab, "HC01", CrossbowId);
            CheckLabBounds(lab);
            Require(lab.GetNode<RichTextLabel>("%Inspector").Text.Length > 0, "crossbow skill summary exists");
            await Capture("lab-hc01-top");
            await SelectLibrary(lab, "HC08", PoisonId);
            Require(lab.GetNode<RichTextLabel>("%Inspector").Text.Contains("棘毒", StringComparison.Ordinal), "library HC08 details use current NE01 skill");
            Require(lab.GetNode<RichTextLabel>("%InspectorStats").Text.Length > 0, "library unit has readable attribute details");
            CheckLabBounds(lab);
            await Capture("lab-hc08-top");
            Step("battle laboratory: delayed library hover");
            await MovePointer(new Vector2(800, 85));
            var hoverCard = Descendants<BattleLabLibraryCard>(lab).Single(candidate => candidate.Side == BattleLabSide.Player && candidate.ContentId == PoisonId);
            await MovePointer(hoverCard.GetGlobalRect().GetCenter());
            await ToSignal(GetTree().CreateTimer(.32), SceneTreeTimer.SignalName.Timeout);
            await Frames(2);
            var tooltip = Descendants<BattleLabTooltip>(lab).SingleOrDefault(candidate => candidate.Visible);
            Require(tooltip is not null, "library hover opens its authored tooltip after the delay");
            var tooltipPanel = tooltip!.GetNode<Control>("%TooltipPanel");
            InViewport(tooltipPanel, "HC08 hover card");
            Require(tooltip.GetNode<Label>("%Title").Text.Contains("棘毒", StringComparison.Ordinal), "hover shows the selected production hero title");
            Require(tooltip.GetNode<RichTextLabel>("%Abilities").Text.Length > 0, "hover has nonempty skill information");
            await Capture("lab-hc08-hover");
            Step("battle laboratory: independent side panel clicks");
            var unitPage = Descendants<ScrollContainer>(lab).Single(node => node.Name == "UnitPage");
            await ScrollEnd(unitPage, true);
            await Click(lab.GetNode<Button>("%ToggleLibraryButton"));
            Require(!Descendants<Control>(lab).Single(node => node.Name == "LibraryPane").Visible, "left panel collapses");
            await Click(lab.GetNode<Button>("%ToggleDetailsButton"));
            Require(!Descendants<Control>(lab).Single(node => node.Name == "DetailPane").Visible, "right panel collapses");
            CheckLabBounds(lab);
            await Capture("lab-both-collapsed");
            await Click(lab.GetNode<Button>("%ToggleLibraryButton"));
            await Click(lab.GetNode<Button>("%ToggleDetailsButton"));
            await Frames(3);
            CheckLabBounds(lab);

            Step("battle laboratory: preset modal through real text input and list click");
            await Click(lab.GetNode<Button>("%OpenPresetsButton"));
            await ReplaceText(lab.GetNode<Control>("%PresetPanel").GetNode<LineEdit>("%PresetSearch"), "NE01");
            var presetList = lab.GetNode<Control>("%PresetPanel").GetNode<ItemList>("%PresetChoice");
            Require(presetList.ItemCount > 0, "NE01 preset search returned a row");
            await ClickPoint(presetList.GlobalPosition + presetList.GetItemRect(0).GetCenter());
            InViewport(lab.GetNode<Control>("%PresetPanel").GetNode<Control>("%ClosePresetsButton"), "preset close");
            InViewport(lab.GetNode<Control>("%PresetPanel").GetNode<Control>("%LoadPresetButton"), "preset load");
            InViewport(lab.GetNode<Control>("%PresetPanel").GetNode<Control>("%SavePresetButton"), "preset save");
            await Capture("lab-preset-modal");
            await Click(lab.GetNode<Control>("%PresetPanel").GetNode<Button>("%ClosePresetsButton"));
            Require(!lab.GetNode<Control>("%PresetPanel").Visible, "preset close button closes modal");
            await Click(lab.GetNode<Button>("%OpenPresetsButton"));
            await Click(lab.GetNode<Control>("%PresetPanel").GetNode<Button>("%LoadPresetButton"));
            await Until(() => !lab.GetNode<Control>("%PresetPanel").Visible, "preset loaded and modal closed");
            await Click(lab.GetNode<Button>("%TeamDetailsButton"));
            CheckLabBounds(lab);
            await Capture("lab-team");

            Step("battle laboratory: placed unit inspection and battle launch");
            var poisonCell = Descendants<BattleLabBoardCell>(lab).Single(cell => cell.Cell == new Vector2I(2, 2));
            Require(!string.IsNullOrEmpty(poisonCell.InstanceId), "NE01 board contains its selected hero");
            await Click(poisonCell);
            await Frames(3);
            var start = lab.GetNode<Button>("%StartButton");
            Require(!start.Disabled, "loaded preset allows battle launch");
            await Click(start);
            await Until(() => screens.Battle.IsVisibleInTree(), "battle opened");
            var battle = screens.Battle;
            await Until(() => battle.ReadRuntimeUnits().Length > 0, "battle runtime units ready");
            await Click(battle.GetNode<Button>("%PauseButton"));
            await Frames(3);
            var selected = battle.ReadRuntimeUnits().First(unit => unit.Team == 0 && unit.DisplayName.Contains("棘毒", StringComparison.Ordinal));
            var presenter = Descendants<UnitContentRoot>(battle).Single(unit => unit.RuntimeId == selected.RuntimeId);
            await ClickPoint(presenter.GlobalPosition);
            var dock = battle.GetNode<BattleInspectorDock>("%BattleInspectorDock");
            var details = dock.Details;
            Require(details.IsVisibleInTree(), "real battle unit click opens its details");
            InViewport(dock, "battle right inspector");
            InViewport(battle.GetNode<Control>("%PauseButton"), "battle pause");
            InViewport(battle.GetNode<Control>("%ReturnConfigurationButton"), "battle return configuration");
            Require(details.GetNode<Button>("%StatAttack").Text.Any(char.IsDigit), "battle unit attack value visible");
            await Capture("battle-unit-top");

            Step("battle unit: click attributes and skill descriptions, scroll to end");
            var detailScroll = details.GetNode<ScrollContainer>("%Scroll");
            await Click(details.GetNode<Button>("%StatAttack"));
            await Frames(4);
            Require(details.GetNode<RichTextLabel>("%FactDetail").Text.Contains("攻击", StringComparison.Ordinal), "attribute detail opens by click");
            await RevealByWheel(detailScroll, details.GetNode<Button>("%UnitAbility"));
            await Click(details.GetNode<Button>("%UnitAbility"));
            await Frames(4);
            Require(details.GetNode<RichTextLabel>("%FactDetail").Text.Length > 0, "active skill details opened");
            await Capture("battle-skill-detail");
            await ScrollEnd(detailScroll, true);
            Require(BottomVisible(details.GetNode<Control>("%FactDetail"), detailScroll), "battle expanded skill text bottom reachable");
            await Capture("battle-skill-bottom");
            await Click(dock.GetNode<Button>("%StatisticsToggle"));
            Require(dock.WantsStatistics, "statistics rail opens statistics page");
            await Capture("battle-statistics");
            await Click(dock.GetNode<Button>("%CloseInspector"));
            Require(!dock.WantsStatistics, "statistics close button collapses inspector");
            await Capture("battle-statistics-collapsed");
            GD.Print($"CURRENT_UI_INPUT_CAPTURE_OK size=1600x900 captures={_captures.Count} path={OutputPath} input=mouse,key,wheel save=isolated no-user-presets-written");
            }
        }
        catch (Exception exception)
        {
            code = 1;
            GD.PrintErr($"CURRENT_UI_INPUT_CAPTURE_FAILED step={_step}: {exception}");
            if (DisplayServer.GetName() != "headless") await Capture("failure");
        }
        finally { game?.QueueFree(); }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(code);
    }

    private async Task InspectDeploymentFootprints(AppScreenHost screens)
    {
        Step("footprints: large bodies use authored radius through real placement input");
        await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/BattleLabButton"));
        var lab = screens.BattleLab;
        await Until(() => lab.IsVisibleInTree(), "laboratory open");
        await Click(lab.GetNode<Button>("%SettingsDetailsButton"));
        await Click(lab.GetNode<Button>("%ClearAllButton"));
        await KeyPress(Key.Escape);
        await Click(lab.GetNode<Button>("%ToggleLibraryButton"));
        await ReplaceText(lab.GetNode<LineEdit>("%UnitSearch"), "EB02");
        var card = Descendants<BattleLabLibraryCard>(lab).Single(item => item.Side == BattleLabSide.Player);
        var footprint = lab.GetNode<DeploymentFootprintLayer>("%DeploymentFootprints");
        BattleLabBoardCell Cell(int x, int y) => Descendants<BattleLabBoardCell>(lab).Single(cell => cell.Cell == new Vector2I(x, y));
        async Task BeginDrag(Control source, Control destination)
        {
            var start = source.GetGlobalRect().GetCenter();
            var end = destination.GetGlobalRect().GetCenter();
            await MovePointer(start);
            Input.ParseInputEvent(new InputEventMouseButton { Position = start, GlobalPosition = start,
                ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
            for (var i = 1; i <= 8; i++)
            {
                var point = start.Lerp(end, i / 8f);
                Input.WarpMouse(point);
                Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point,
                    Relative = (end - start) / 8, ButtonMask = MouseButtonMask.Left });
                await Frames(1);
            }
            await Frames(2);
        }
        async Task Release(Control target)
        {
            var point = target.GetGlobalRect().GetCenter();
            Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = false });
            await Frames(4);
        }
        await BeginDrag(card, Cell(7, 3));
        Require(footprint.DragBody is { Radius: 1.2f, Cell: { X: 7, Y: 3 } } && footprint.DragAllowed,
            "drag preview carries the real 2.4-cell diameter and legal anchor");
        await Capture("footprint-valid-drag");
        await Release(Cell(7, 3));
        Require(lab.CurrentSnapshot!.Units.Length == 1 && footprint.Bodies.Single().Radius == 1.2f && footprint.DragBody is null,
            "placed large body remains visible and transient preview clears");
        await Capture("footprint-placed");
        await BeginDrag(card, Cell(8, 3));
        Require(!footprint.DragAllowed, "overlapping full bodies show an invalid footprint");
        await Capture("footprint-overlap");
        await Release(Cell(8, 3));
        Require(lab.CurrentSnapshot.Units.Length == 1 && footprint.DragBody is null, "invalid overlap adds no unit");
        await BeginDrag(Cell(7, 3), Cell(9, 3));
        Require(!footprint.DragAllowed, "body beyond board edge is rejected");
        await Capture("footprint-edge");
        await Release(Cell(9, 3));
        Require(lab.CurrentSnapshot.Units.Single().Cell == new Vector2I(7, 3), "invalid move preserves original anchor");
        await BeginDrag(Cell(7, 3), Cell(4, 2));
        await KeyPress(Key.Escape);
        Require(footprint.DragBody is null, "Escape cancels footprint even with the library open");
        await Release(Cell(4, 2));
        Require(lab.CurrentSnapshot.Units.Single().Cell == new Vector2I(7, 3), "cancelled move changes no configuration");
        await BeginDrag(Cell(7, 3), Cell(4, 2));
        await Release(Cell(4, 2));
        Require(footprint.Bodies.Single().Cell == new Vector2I(4, 2), "successful move relocates footprint");
        await Click(lab.GetNode<Button>("%UndoButton"));
        Require(footprint.Bodies.Single().Cell == new Vector2I(7, 3), "undo restores footprint with configuration");
        if (!lab.GetNode<Control>("%LibraryPane").IsVisibleInTree())
            await Click(lab.GetNode<Button>("%ToggleLibraryButton"));
        await ReplaceText(lab.GetNode<LineEdit>("%UnitSearch"), "soldier_dummy_static");
        card = Descendants<BattleLabLibraryCard>(lab).Single(item => item.Side == BattleLabSide.Player);
        await BeginDrag(card, Cell(4, 2));
        Require(footprint.DragBody is { Radius: <= .5f } && footprint.DragAllowed, "normal unit keeps a single-cell preview");
        await Release(Cell(4, 2));
        Require(footprint.Bodies.Count == 1, "ordinary bodies do not retain large-body overlays");

        Step("footprints: shared campaign board and enlarged enemy preview");
        lab.Hide();
        var board = GD.Load<PackedScene>("res://scenes/ui/components/DeploymentBoard.tscn").Instantiate<DeploymentBoard>();
        AddChild(board);
        board.Position = new Vector2(150, 170); board.Size = new Vector2(1280, 610);
        var definition = GD.Load<UnitDefinition>("res://content/definitions/enemies/enemy_eb02_shell_matriarch.tres");
        board.Bind(new BattleConfig { FloorRule = new ClearFloorRuleRuntime("preview", "", ""),
            HeroRule = new HeroRuleSnapshot(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, "") }, [], "", [
            new EnemyDeploymentViewModel("large", definition.DisplayName, new Vector2I(7, 3),
                definition.Role, definition.AttackRange, true, definition.Portrait, definition.BodyRadius)]);
        await Frames(3);
        var enemy = Descendants<EnemyDeploymentPreview>(board).Single();
        Require(Math.Abs(enemy.Size.X / board.CurrentProjection.CellPitch.X - 2.4f) < .01f &&
            Math.Abs(enemy.Size.Y / board.CurrentProjection.CellPitch.Y - 2.4f) < .01f,
            "campaign preview spans the authored body diameter on both projected axes");
        var selected = ""; board.EnemySelected += id => selected = id;
        await ClickPoint(enemy.GetGlobalRect().GetCenter() + new Vector2(board.CurrentProjection.CellPitch.X * .8f, 0));
        Require(selected == "large", "the enlarged enemy can be inspected outside its anchor cell");
        await KeyPress(Key.Tab);
        await Capture("footprint-campaign");
        board.QueueFree();
        GD.Print("DEPLOYMENT_FOOTPRINT_INPUT_OK real-drag placed overlap edge cancel move undo normal-unit campaign-body enemy-click isolated-save");
    }

    private async Task InspectTooltipIcons(AppScreenHost screens)
    {
        Step("tooltip icons: production laboratory");
        await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/BattleLabButton"));
        await Until(() => screens.BattleLab.IsVisibleInTree(), "laboratory open");
        var lab = screens.BattleLab;
        await Click(lab.GetNode<Button>("%ToggleLibraryButton"));
        await ReplaceText(lab.GetNode<LineEdit>("%UnitSearch"), "HC08");
        var card = Descendants<BattleLabLibraryCard>(lab).Single(item => item.ContentId == PoisonId && item.Side == BattleLabSide.Player);
        await MovePointer(card.GetGlobalRect().GetCenter());
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        await Frames(3);
        var tooltip = Descendants<BattleLabTooltip>(card).Single(item => item.Visible);
        CheckIconBounds(tooltip);
        Require(tooltip.GetNode<TooltipStatGrid>("%IconStats").GetChildren().OfType<SemanticChip>()
            .Any(chip => chip.SemanticKey == SemanticIconKeys.Mana && chip.DisplayText == "20/60"), "production starting/max mana presented");
        await Capture("tooltip-icons-library");
        await Click(card);
        await MovePointer(new Vector2(800, 80));
        await KeyPress(Key.Tab, false);
        await KeyPress(Key.Tab, false, true);
        Require(GetViewport().GuiGetFocusOwner() == card, "Tab and Shift+Tab restore card focus");
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        Require(tooltip.Visible, "keyboard focus opens same attribute tooltip");
        CheckIconBounds(tooltip);
        await Capture("tooltip-icons-keyboard");

        Step("tooltip icons: drag to battlefield");
        var cell = Descendants<BattleLabBoardCell>(lab).First(item => item.Cell.X == 2 && item.Cell.Y == 2);
        var start = card.GetGlobalRect().GetCenter();
        var end = cell.GetGlobalRect().GetCenter();
        await MovePointer(start);
        Input.ParseInputEvent(new InputEventMouseButton { Position = start, GlobalPosition = start, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        for (var i = 1; i <= 8; i++)
        {
            var point = start.Lerp(end, i / 8f);
            Input.WarpMouse(point);
            Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = (end - start) / 8, ButtonMask = MouseButtonMask.Left });
            await Frames(1);
        }
        Input.ParseInputEvent(new InputEventMouseMotion { Position = end, GlobalPosition = end, ButtonMask = MouseButtonMask.Left });
        Input.ParseInputEvent(new InputEventMouseButton { Position = end, GlobalPosition = end, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(4);
        Require(!string.IsNullOrEmpty(cell.InstanceId), "drag placed a real unit");
        await MovePointer(new Vector2(800, 80));
        await MovePointer(cell.GetGlobalRect().GetCenter());
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        var placedTooltip = Descendants<BattleLabTooltip>(cell).Single(item => item.Visible);
        CheckIconBounds(placedTooltip);
        await Capture("tooltip-icons-placed");

        Step("tooltip icons: no-mana unit");
        await ReplaceText(lab.GetNode<LineEdit>("%UnitSearch"), "soldier_dummy_static");
        var dummy = Descendants<BattleLabLibraryCard>(lab).Single(item => item.Side == BattleLabSide.Player);
        await MovePointer(dummy.GetGlobalRect().GetCenter());
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        var dummyTooltip = Descendants<BattleLabTooltip>(dummy).Single(item => item.Visible);
        CheckIconBounds(dummyTooltip);
        Require(!dummyTooltip.GetNode<TooltipStatGrid>("%IconStats").GetChildren().OfType<SemanticChip>()
            .Any(chip => chip.Visible && chip.SemanticKey == SemanticIconKeys.Mana), "no stale mana on a no-mana unit");
        await Capture("tooltip-icons-no-mana");
        await InspectKeywordColors(lab);
        GD.Print("TOOLTIP_ICONS_INPUT_OK mouse-hover,keyboard-focus,drag,prepared-values,no-mana,rendered-bounds isolated-save-namespace");
    }

    private async Task InspectKeywordColors(BattleLabScreenController lab)
    {
        Step("keyword icons: white shield and numeric ownership");
        var vocabulary = GD.Load<CombatKeywordCatalog>("res://content/ui/combat_keywords.tres");
        var terms = vocabulary.Terms.ToArray();
        foreach (var term in terms)
            Require(SemanticIcons.Catalog.ResolveIcon(term.SemanticIcon) is not null, "keyword icon resolves: " + term.Word);
        string Mark(string text) => CombatTextMarkup.Line(text, terms, 16, new HashSet<CombatKeyword>());
        void Colored(string input, string value, string word)
        {
            var tint = terms.Single(term => term.Word == word).Tint.ToHtml(false);
            Require(Mark(input).Contains($"[color=#{tint}]{value}[/color]", StringComparison.Ordinal), $"{input}: {value} belongs to {word}");
        }
        Colored("攻击 200 防御 40", "200", "攻击");
        Colored("攻击 200 防御 40", "40", "防御");
        Colored("法强 80，造成 100 伤害", "80", "法强");
        Colored("法强 80，造成 100 伤害", "100", "伤害");
        Colored("造成 100 真实伤害", "100", "真实伤害");
        Colored("成功闪避恢复 20 生命", "20", "生命");
        Colored("攻速 +0.25 和 25% 吸血", "+0.25", "攻速");
        Colored("攻速 +0.25 和 25% 吸血", "25%", "吸血");
        Colored("对周围 1.5 格敌人施加 3 层棘毒", "3", "棘毒");
        Colored("每 2 秒获得 20 护盾", "20", "护盾");
        Colored("冰冻 0.8 秒", "0.8", "冰冻");
        Colored("棘毒：持续 12 秒", "12", "棘毒");
        Colored("生命从 60% 及以上跌至 50% 以下", "60%", "生命");
        Colored("生命从 60% 及以上跌至 50% 以下", "50%", "生命");
        Colored("生命低于 20%", "20%", "生命");
        Colored("每点护盾提供 0.15% 暴击率，最多额外 60%", "60%", "暴击");
        Require(Mark("对周围 1.5 格敌人施加 3 层棘毒").Contains("[color=#f4eee0]1.5[/color]", StringComparison.Ordinal), "unrelated radius does not inherit poison color");
        Require(Mark("每 2 秒获得 20 护盾").Contains("[color=#f4eee0]2[/color]", StringComparison.Ordinal), "trigger interval does not inherit shield color");

        await ReplaceText(lab.GetNode<LineEdit>("%UnitSearch"), "HC13");
        var card = Descendants<BattleLabLibraryCard>(lab).Single(item => item.Side == BattleLabSide.Player);
        await MovePointer(card.GetGlobalRect().GetCenter());
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        var tooltip = Descendants<BattleLabTooltip>(card).Single(item => item.Visible);
        var body = tooltip.GetNode<RichTextLabel>("%Abilities");
        Require(body.Text.Contains("[color=#ffffff]120[/color]", StringComparison.Ordinal), "real shield skill amount is white");
        Require(!body.GetParsedText().Contains("[img", StringComparison.Ordinal), "inline images parsed by Godot");
        CheckIconBounds(tooltip);
        await Capture("keyword-shield-white");

        // Explicit presentation fixture: no content, simulation or authored resource is changed.
        card.SetInspection(new BattleLabTooltipInfo("词条图标示例 · 非单位数值", Stats:
            string.Join("\n", terms.Select(term => term.Word + " 25"))));
        await MovePointer(new Vector2(800, 80));
        await MovePointer(card.GetGlobalRect().GetCenter());
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        await Frames(3);
        InViewport(tooltip.GetNode<Control>("%TooltipPanel"), "keyword legend");
        await Capture("keyword-icons-palette");
        var rich = tooltip.GetNode<CombatRichText>("%Stats");
        rich.Text = "[color=red]攻击 200[/color]";
        Require(rich.GetParsedText().Contains("[color=red]", StringComparison.Ordinal), "caller markup remains literal");
        rich.ShowGlossary = true;
        rich.Text = "被动 · 乘乱追击";
        Require(!rich.GetParsedText().Contains("词条释义", StringComparison.Ordinal), "skill name does not acquire an unrelated pursuit definition");
        await ReplaceText(lab.GetNode<LineEdit>("%UnitSearch"), "HC38");
        var longCard = Descendants<BattleLabLibraryCard>(lab).Single(item => item.Side == BattleLabSide.Player);
        await MovePointer(longCard.GetGlobalRect().GetCenter());
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        await Frames(3);
        var longTooltip = Descendants<BattleLabTooltip>(longCard).Single(item => item.Visible);
        CheckIconBounds(longTooltip);
        await Capture("keyword-long-skill");
        GD.Print("KEYWORD_ICONS_OK all-icons-loaded,matching-value-colors,unrelated-values-neutral,white-shield,escaped-copy,skill-title-distinction");
    }

    private void CheckIconBounds(BattleLabTooltip tooltip)
    {
        var panel = tooltip.GetNode<Control>("%TooltipPanel");
        InViewport(panel, "attribute tooltip");
        foreach (var chip in tooltip.GetNode<TooltipStatGrid>("%IconStats").GetChildren().OfType<SemanticChip>().Where(chip => chip.Visible))
            Require(chip.ResolvedIcon is not null && panel.GetGlobalRect().Encloses(chip.GetGlobalRect()), "loaded attribute icon fits tooltip");
    }

    private async Task SelectLibrary(BattleLabScreenController lab, string search, string id)
    {
        await ReplaceText(lab.GetNode<LineEdit>("%UnitSearch"), search);
        var card = Descendants<BattleLabLibraryCard>(lab).Single(candidate => candidate.Side == BattleLabSide.Player && candidate.ContentId == id);
        await Click(card);
        await Frames(3);
    }

    private void CheckLabBounds(BattleLabScreenController lab)
    {
        InViewport(lab.GetNode<Control>("%BackButton"), "lab back");
        InViewport(lab.GetNode<Control>("%StartButton"), "lab start");
        InViewport(lab.GetNode<Control>("%ToggleLibraryButton"), "lab left toggle");
        InViewport(lab.GetNode<Control>("%ToggleDetailsButton"), "lab right toggle");
        foreach (var name in new[] { "LibraryPane", "DetailPane", "BoardCenter" })
        {
            var panel = Descendants<Control>(lab).Single(node => node.Name == name);
            if (panel.IsVisibleInTree()) InViewport(panel, "lab " + name);
        }
    }

    private void InViewport(Control control, string label)
    {
        var rect = control.GetGlobalRect();
        Require(control.IsVisibleInTree() && GetViewport().GetVisibleRect().Grow(2).Encloses(rect), $"{label} must fit viewport: {rect}");
        GD.Print($"CURRENT_UI_BOUNDS {label}={rect}");
    }

    private static bool BottomVisible(Control target, ScrollContainer scroll) =>
        target.GetGlobalRect().End.Y <= scroll.GetGlobalRect().End.Y + 3;

    private async Task RevealByWheel(ScrollContainer scroll, Control target)
    {
        for (var count = 0; count < 60; count++)
        {
            var area = scroll.GetGlobalRect().Grow(-3);
            if (area.HasPoint(target.GetGlobalRect().GetCenter())) return;
            await Wheel(scroll, target.GetGlobalRect().GetCenter().Y > area.End.Y);
        }
        throw new InvalidOperationException($"Cannot reveal {target.GetPath()} using wheel: target={target.GetGlobalRect()} scroll={scroll.GetGlobalRect()}");
    }

    private async Task ScrollEnd(ScrollContainer scroll, bool down)
    {
        for (var count = 0; count < 60; count++)
        {
            var bar = scroll.GetVScrollBar();
            if (down ? bar.Value >= bar.MaxValue - bar.Page - 1 : bar.Value <= bar.MinValue + 1) return;
            var before = bar.Value;
            await Wheel(scroll, down);
            Require(Math.Abs(bar.Value - before) > .1, $"wheel must move {scroll.GetPath()} (value={bar.Value}, max={bar.MaxValue}, page={bar.Page})");
        }
        throw new InvalidOperationException("Scroll end unreachable: " + scroll.GetPath());
    }

    private async Task Wheel(ScrollContainer scroll, bool down)
    {
        var point = scroll.GetGlobalRect().GetCenter();
        await MovePointer(point);
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = down ? MouseButton.WheelDown : MouseButton.WheelUp, Pressed = true, Factor = 3 });
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = down ? MouseButton.WheelDown : MouseButton.WheelUp, Pressed = false });
        await Frames(2);
    }

    private async Task ReplaceText(LineEdit edit, string text)
    {
        await Click(edit);
        await KeyPress(Key.A, true);
        await KeyPress(Key.Backspace);
        foreach (var character in text)
        {
            Input.ParseInputEvent(new InputEventKey { Unicode = character, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Unicode = character, Pressed = false });
        }
        await Frames(3);
        Require(edit.Text == text, $"real text input expected {text}, got {edit.Text}");
    }

    private async Task KeyPress(Key key, bool ctrl = false, bool shift = false)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, CtrlPressed = ctrl, ShiftPressed = shift, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, CtrlPressed = ctrl, ShiftPressed = shift, Pressed = false });
        await Frames(2);
    }

    private async Task Click(Control control)
    {
        Require(control.IsVisibleInTree(), "click target hidden: " + control.GetPath());
        var point = control.GetGlobalRect().GetCenter();
        Require(GetViewport().GetVisibleRect().HasPoint(point), "click target outside viewport: " + control.GetPath());
        var path = control.GetPath().ToString();
        var presses = 0;
        var button = control as BaseButton;
        void OnPressed() => presses++;
        if (button is not null) button.Pressed += OnPressed;
        try
        {
            await ClickPoint(point);
            GD.Print($"CURRENT_UI_CLICK target={path} point={point} engine-mouse={GetViewport().GetMousePosition()} pressed-events={presses} focused={GetViewport().GuiGetFocusOwner()?.GetPath()}");
        }
        finally
        {
            if (button is not null && GodotObject.IsInstanceValid(button)) button.Pressed -= OnPressed;
        }
    }

    private async Task ClickPoint(Vector2 point)
    {
        await MovePointer(point);
        // Keep the release in the same engine frame as the press: a native pointer
        // refresh between them must not silently turn a click into a pointer exit.
        Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }

    private async Task MovePointer(Vector2 point)
    {
        // Synchronize the engine's native pointer with the injected viewport input;
        // the application's mouse-enter/exit and hover timers then use one position.
        Input.WarpMouse(point);
        await Frames(1);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(1);
    }

    private async Task Capture(string name)
    {
        await Frames(2);
        var path = $"{OutputPath}/after-{name}.png";
        var error = GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath(path));
        Require(error == Error.Ok, "save screenshot " + path + ": " + error);
        _captures.Add(path);
        GD.Print("CURRENT_UI_CAPTURE " + ProjectSettings.GlobalizePath(path));
    }

    private async Task Until(Func<bool> ready, string label, int frames = 60)
    {
        for (var index = 0; index < frames; index++)
        {
            if (ready()) return;
            await Frames(1);
        }
        Require(false, "timed out: " + label);
    }

    private async Task Frames(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }

    private void Step(string value) { _step = value; GD.Print("CURRENT_UI_STEP " + value); }
    private static IEnumerable<T> Descendants<T>(Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
