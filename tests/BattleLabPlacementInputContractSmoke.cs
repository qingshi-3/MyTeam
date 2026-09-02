using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.BattleLab;
using TowerAutobattler.UI;

public partial class BattleLabPlacementInputContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private async Task<int> RunAsync()
    {
        BattleLabScreenController? screen = null;
        Control? host = null;
        try
        {
            GetWindow().Size = new Vector2I(1600, 900);
            var gate = await TestProjectFixture.PublishAsync(this);
            var package = gate.Package ?? throw new InvalidOperationException(
                "Battle Lab input package: " + string.Join(';', gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var graph = index.Package.Content.Graph;
            var traitHero = index.PlayerHeroes.FirstOrDefault(hero =>
                graph.ResolveUnitTraitContributions(hero.StableId).Length > 0) ?? index.PlayerHeroes[0];
            var traitEquipment = index.Equipment.First(item =>
                graph.ResolveEquipment(item.StableId).TraitContributions.Length > 0);
            var session = new BattleLabSession(index, Math.Max(2, package.Project.RunRules.InitialPopulation));
            var scene = GD.Load<PackedScene>("res://scenes/ui/BattleLabScreen.tscn") ??
                        throw new InvalidOperationException("BattleLabScreen scene missing");
            screen = scene.Instantiate<BattleLabScreenController>();
            host = new Control { Size = new Vector2(1600, 900) };
            AddChild(host);
            host.AddChild(screen);
            await Frame(3);
            screen.Bind(index, session);
            await Frame(3);
            Require(screen.CellCount == 60, "authored 10x6 board");

            var playerCard = Descendants<BattleLabLibraryCard>(screen)
                .First(card => card.Side == BattleLabSide.Player && card.ContentId == traitHero.StableId);
            var enemyCard = Descendants<BattleLabLibraryCard>(screen).First(card => card.Side == BattleLabSide.Enemy);
            var playerCell = Cell(screen, 0, 0);
            var enemyCell = Cell(screen, 9, 0);
            await Drag(playerCard, playerCell);
            await Drag(enemyCard, enemyCell);
            Require(session.Units.Count == 2 && session.At(new Vector2I(0, 0))?.Side == BattleLabSide.Player &&
                    session.At(new Vector2I(9, 0))?.Side == BattleLabSide.Enemy,
                $"real library drag placement: {screen.LastFeedback}; " +
                $"player={playerCard.GetGlobalRect()} enemy={enemyCard.GetGlobalRect()} " +
                $"pcell={playerCell.GetGlobalRect()} ecell={enemyCell.GetGlobalRect()}");

            var beforeInvalid = session.Freeze().CanonicalDigest;
            await Drag(playerCell, Cell(screen, 5, 0));
            Require(session.Freeze().CanonicalDigest == beforeInvalid && screen.LastFeedback.Contains("3×6", StringComparison.Ordinal),
                "real invalid formal drop is non-mutating with Chinese reason");

            await Drag(playerCard, Cell(screen, 1, 0));
            var firstId = session.At(new Vector2I(0, 0))!.InstanceId;
            var secondId = session.At(new Vector2I(1, 0))?.InstanceId ?? throw new InvalidOperationException(
                $"second real player placement failed: {screen.LastFeedback}; pop={session.CurrentPopulation}; " +
                $"players={session.Units.Count(unit => unit.Side == BattleLabSide.Player)}");
            await Drag(Cell(screen, 1, 0), Cell(screen, 0, 0));
            Require(session.At(new Vector2I(0, 0))?.InstanceId == secondId &&
                    session.At(new Vector2I(1, 0))?.InstanceId == firstId, "real occupied swap");

            var beforeCancel = session.Freeze().CanonicalDigest;
            await DragTo(Cell(screen, 0, 0), new Vector2(-20, -20));
            Require(session.Freeze().CanonicalDigest == beforeCancel &&
                    screen.LastFeedback.Contains("已取消", StringComparison.Ordinal),
                "real outside release cancels without mutation");

            var playerSearch = screen.GetNode<LineEdit>("%PlayerSearch");
            playerSearch.Text = "no-such-player-card";
            playerSearch.EmitSignal(LineEdit.SignalName.TextChanged, playerSearch.Text);
            await Frame(3);
            Require(!Descendants<BattleLabLibraryCard>(screen).Any(card => card.Side == BattleLabSide.Player),
                "player search empty fixture");
            var playerPanel = screen.GetNode<Control>("%PlayerPanel");
            await DragTo(Cell(screen, 1, 0), playerPanel.GetGlobalRect().GetCenter());
            Require(!session.Units.Any(unit => unit.InstanceId == firstId), "real recall to origin library");
            playerSearch.Text = string.Empty;
            playerSearch.EmitSignal(LineEdit.SignalName.TextChanged, playerSearch.Text);
            await Frame(3);

            await Click(screen.GetNode<Control>("%ModeToggleButton"));
            Require(session.Mode == BattleLabPlacementMode.FreeExperiment &&
                    screen.GetNode<Label>("%ModeBanner").Text.Contains("自由实验配置", StringComparison.Ordinal),
                "real free-mode switch with persistent non-colour label");
            await Drag(Cell(screen, 0, 0), Cell(screen, 6, 5));
            Require(session.At(new Vector2I(6, 5))?.Side == BattleLabSide.Player, "real free-mode cross-board move");
            Require(Cell(screen, 6, 5).Text == "▣", "selected board unit keeps persistent non-colour shape");

            await Click(Cell(screen, 9, 0));
            Require(screen.LastFeedback.Contains("已位于", StringComparison.Ordinal) &&
                    !screen.GetNode<Control>("%EquipmentBox").Visible &&
                    screen.GetNode<Label>("%EquipmentNotApplicable").Visible &&
                    screen.GetNode<Label>("%Inspector").Text.Contains("不适用", StringComparison.Ordinal),
                "enemy selection exposes no Equipment editor and explicit inapplicability");
            await Click(screen.GetNode<Control>("%DeleteSelectedButton"));
            Require(session.Units.All(unit => unit.Side != BattleLabSide.Enemy), "real explicit delete action");
            var currentEnemyCard = Descendants<BattleLabLibraryCard>(screen)
                .First(card => card.Side == BattleLabSide.Enemy);
            await Drag(currentEnemyCard, Cell(screen, 9, 0));
            Require(session.At(new Vector2I(9, 0))?.Side == BattleLabSide.Enemy,
                "enemy restored after explicit delete fixture");
            await Click(Cell(screen, 6, 5));
            Require(screen.LastFeedback.Contains("已位于", StringComparison.Ordinal),
                "real player re-selection before build editing");

            var slot = screen.GetNode<OptionButton>("%EquipmentSlot");
            var equipmentChoice = screen.GetNode<OptionButton>("%EquipmentChoice");
            var traitEquipmentIndex = Enumerable.Range(0, index.Equipment.Length)
                .First(itemIndex => index.Equipment[itemIndex].StableId == traitEquipment.StableId);
            equipmentChoice.Select(traitEquipmentIndex);
            Require(index.Rules.EquipmentSlotCapacity == 3 && slot.ItemCount == index.Rules.EquipmentSlotCapacity,
                "authored Equipment UI exposes exactly the production three slots");
            for (var slotIndex = 0; slotIndex < index.Rules.EquipmentSlotCapacity; slotIndex++)
            {
                slot.Select(slotIndex);
                await Click(screen.GetNode<Control>("%EquipButton"));
                Require(session.At(new Vector2I(6, 5))?.Equipment.Length == slotIndex + 1,
                    $"real choose-and-click Equipment slot {slotIndex + 1}");
            }
            Require(!session.Equip(secondId, 3, traitEquipment.StableId) &&
                    session.At(new Vector2I(6, 5))?.Equipment.Length == index.Rules.EquipmentSlotCapacity,
                "domain rejects slotIndex=3 without pretending a fourth UI slot exists");
            Require(screen.GetNode<Label>("%Inspector").Text.Contains("控制抗性", StringComparison.Ordinal) &&
                    screen.GetNode<Label>("%Inspector").Text.Contains("实例", StringComparison.Ordinal),
                "prepared selected-unit inspector");
            slot.Select(0);
            await Click(screen.GetNode<Control>("%RemoveEquipmentButton"));
            Require(session.At(new Vector2I(6, 5))?.Equipment.Length == index.Rules.EquipmentSlotCapacity - 1,
                "real equipment removal");
            var traitEquipmentInstanceId = session.At(new Vector2I(6, 5))!.Equipment
                .OrderBy(item => item.InstanceId, StringComparer.Ordinal).First().InstanceId;
            if (index.Relics.Length > 0)
            {
                var relicStacks = screen.GetNode<SpinBox>("%RelicStacks");
                Require(relicStacks.MaxValue == int.MaxValue,
                    "Relic SpinBox uses the technical Int32 ceiling rather than an authored maximum");
                await ReplaceText(relicStacks.GetLineEdit(), "1000");
                await Click(screen.GetNode<Control>("%SetRelicButton"));
                Require(session.Relics.Count == 1 && session.Relics.Single().Stacks == 1000,
                    "real Relic text input and Set button accept stacks above 999");
                screen.GetNode<OptionButton>("%ExistingRelicChoice").Select(0);
                var contentScroll = screen.GetNode<ScrollContainer>("Margin/Root/ContentScroll");
                var removeRelic = screen.GetNode<Button>("%RemoveRelicButton");
                await ScrollUntilVisible(contentScroll, removeRelic);
                removeRelic.GrabFocus();
                await Frame();
                await PushKey(Key.Enter);
                Require(session.Relics.Count == 0,
                    $"real team Relic removal: selected={screen.GetNode<OptionButton>("%ExistingRelicChoice").Selected} " +
                    $"feedback={screen.LastFeedback}");
                removeRelic.ReleaseFocus();
                contentScroll.ScrollVertical = 0;
                await Frame(3);
            }

            var seedBefore = session.Seed;
            screen.GetNode<LineEdit>("%Seed").Text = "not-a-64-bit-seed";
            var startButton = screen.GetNode<Button>("%StartButton");
            startButton.GrabFocus();
            await Frame();
            await PushKey(Key.Enter);
            Require(session.Seed == seedBefore && screen.LastFeedback.Contains("64 位整数", StringComparison.Ordinal),
                $"invalid seed is visibly rejected without fallback: seed={session.Seed} feedback={screen.LastFeedback}");
            screen.GetNode<LineEdit>("%Seed").Text = seedBefore.ToString();
            startButton.ReleaseFocus();

            var filter = screen.GetNode<OptionButton>("%EnemyFilter");
            var eliteFilter = Enumerable.Range(0, filter.ItemCount)
                .First(item => filter.GetItemId(item) == (int)BattleLabUnitClassification.PveElite);
            filter.Select(eliteFilter);
            filter.EmitSignal(OptionButton.SignalName.ItemSelected, eliteFilter);
            await Frame(3);
            Require(Descendants<BattleLabLibraryCard>(screen).Where(card => card.Side == BattleLabSide.Enemy)
                    .All(card => index.TryGetUnit(card.ContentId, out var unit) &&
                                 unit.Classification.HasFlag(BattleLabUnitClassification.PveElite)),
                "authored enemy filter reads additive typed flags");
            filter.Select(0);
            filter.EmitSignal(OptionButton.SignalName.ItemSelected, 0);
            await Frame(3);

            await ActivateButton(screen.GetNode<Button>("%ClearEnemyButton"));
            Require(session.Units.All(unit => unit.Side == BattleLabSide.Player), "real clear-side action");
            await Click(Cell(screen, 6, 5));
            var unreadyInspector = screen.GetNode<Label>("%Inspector").Text;
            Require(screen.GetNode<Label>("%Readiness").Text.Contains("尚不可开战", StringComparison.Ordinal) &&
                    unreadyInspector.Contains("生命", StringComparison.Ordinal) &&
                    unreadyInspector.Contains("伤害", StringComparison.Ordinal) &&
                    unreadyInspector.Contains("装备：", StringComparison.Ordinal) &&
                    unreadyInspector.Contains("单位贡献：", StringComparison.Ordinal) &&
                    unreadyInspector.Contains("装备", StringComparison.Ordinal) &&
                    unreadyInspector.Contains(traitEquipmentInstanceId, StringComparison.Ordinal) &&
                    unreadyInspector.Contains("+", StringComparison.Ordinal) &&
                    unreadyInspector.Contains("团队档位：", StringComparison.Ordinal),
                "real hero selection keeps final stats, Equipment, unit Trait sources, and team tiers while unready");
            foreach (var path in new[] { "%ClearEnemyButton", "%ClearAllButton", "%StartButton" })
                Require(Inside(screen.GetGlobalRect(), screen.GetNode<Control>(path).GetGlobalRect()),
                    $"long Trait Inspector keeps fixed action {path} inside the 1600x900 screen rect");
            await ActivateButton(screen.GetNode<Button>("%ClearAllButton"));
            Require(session.Units.Count == 0, "real clear-all action");

            var presets = new BattleLabPresetStore(screen.PresetCatalog);
            screen.Bind(index, session, presets);
            await ActivateButton(screen.GetNode<Button>("%RestoreDefaultButton"));
            Require(session.Units.Count >= 2 && !string.IsNullOrWhiteSpace(session.PrimaryHeroInstanceId),
                "real default preset restore");
            var presetChoice = screen.GetNode<OptionButton>("%PresetChoice");
            var frostIndex = Enumerable.Range(0, presetChoice.ItemCount)
                .First(itemIndex => presetChoice.GetItemText(itemIndex).Contains("冰霜", StringComparison.Ordinal));
            presetChoice.Select(frostIndex);
            await ActivateButton(screen.GetNode<Button>("%LoadPresetButton"));
            var frostConfig = new BattleLabPreparationAdapter(index).Build(session.Freeze());
            Require(frostConfig.Equipment.Instances.Length >= 2 &&
                    frostConfig.Spawns.Any(spawn => spawn.Team == 1 &&
                        spawn.Unit.AttributeDefinition?.Attributes.Any(attribute =>
                            attribute.Attribute == TowerAutobattler.Attributes.CombatAttribute.ControlResistance &&
                            attribute.BaseValue > 0) == true),
                "real frost preset load with independent Equipment and resistant target");
            await VerifyResponsiveLayout(screen, host);
            host.QueueFree();
            await Frame(2);
            GD.Print("BATTLE_LAB_PLACEMENT_INPUT_CONTRACT_OK mouse=viewport-push libraries=both " +
                     "placement=move-swap-recall invalid=nonmutating-chinese modes=formal-free clear=side-all cleanup=tree");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("BATTLE_LAB_PLACEMENT_INPUT_CONTRACT_FAILED: " + exception);
            host?.QueueFree();
            return 1;
        }
    }

    private BattleLabBoardCell Cell(BattleLabScreenController screen, int x, int y) =>
        Descendants<BattleLabBoardCell>(screen)
            .Single(cell => cell.Cell == new Vector2I(x, y));

    private static System.Collections.Generic.IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private async Task Drag(Control source, Control destination) =>
        await DragTo(source, destination.GetGlobalRect().GetCenter());

    private async Task DragTo(Control source, Vector2 destination)
    {
        var viewport = GetViewport();
        var origin = source.GetGlobalRect().GetCenter();
        viewport.PushInput(MouseButtonEvent(origin, true));
        await Frame();
        viewport.PushInput(new InputEventMouseMotion { Position = destination, GlobalPosition = destination,
            Relative = destination - origin, ButtonMask = MouseButtonMask.Left });
        await Frame();
        viewport.PushInput(MouseButtonEvent(destination, false));
        await Frame(2);
    }

    private async Task Click(Control control)
    {
        var position = control.GetGlobalRect().GetCenter();
        var viewport = GetViewport();
        var previous = viewport.GetMousePosition();
        viewport.PushInput(new InputEventMouseMotion
        {
            Position = position,
            GlobalPosition = position,
            Relative = position - previous
        });
        await Frame();
        viewport.PushInput(MouseButtonEvent(position, true));
        await Frame();
        viewport.PushInput(MouseButtonEvent(position, false));
        await Frame(2);
    }

    private async Task ReplaceText(LineEdit lineEdit, string text)
    {
        await Click(lineEdit);
        await PushKey(Key.A, 0, ctrlPressed: true);
        foreach (var character in text)
            await PushKey((Key)character, character);
        await PushKey(Key.Enter);
    }

    private async Task ActivateButton(Button button)
    {
        button.GrabFocus();
        await Frame();
        await PushKey(Key.Enter);
        button.ReleaseFocus();
        await Frame();
    }

    private async Task PushKey(Key keycode, uint unicode = 0, bool ctrlPressed = false)
    {
        GetViewport().PushInput(new InputEventKey
        {
            Keycode = keycode,
            PhysicalKeycode = keycode,
            Unicode = unicode,
            CtrlPressed = ctrlPressed,
            Pressed = true
        });
        await Frame();
        GetViewport().PushInput(new InputEventKey
        {
            Keycode = keycode,
            PhysicalKeycode = keycode,
            Unicode = unicode,
            CtrlPressed = ctrlPressed,
            Pressed = false
        });
        await Frame();
    }

    private async Task ScrollUntilVisible(ScrollContainer scroll, Control target)
    {
        for (var step = 0; step < 24 && !scroll.GetGlobalRect().HasPoint(target.GetGlobalRect().GetCenter()); step++)
        {
            var position = scroll.GetGlobalRect().GetCenter();
            GetViewport().PushInput(new InputEventMouseButton
            {
                Position = position,
                GlobalPosition = position,
                ButtonIndex = MouseButton.WheelDown,
                Pressed = true
            });
            await Frame();
        }
        Require(scroll.GetGlobalRect().HasPoint(target.GetGlobalRect().GetCenter()),
            "Relic removal button is reachable by real scroll input");
    }

    private async Task VerifyResponsiveLayout(BattleLabScreenController screen, Control host)
    {
        GetWindow().Size = new Vector2I(1280, 720);
        host.Size = new Vector2(1280, 720);
        await Frame(4);
        var screenRect = screen.GetGlobalRect();
        Require(screenRect.Size.X <= 1280.1f && screenRect.Size.Y <= 720.1f,
            "Battle Lab 1280x720 root rect");
        Require(Inside(screenRect, screen.GetNode<Control>("%StartButton").GetGlobalRect()),
            "fixed start action reachable at 1280x720");
        Require(Inside(screenRect, screen.GetNode<Control>("%ClearEnemyButton").GetGlobalRect()) &&
                Inside(screenRect, screen.GetNode<Control>("%ClearAllButton").GetGlobalRect()),
            "fixed clear actions reachable at 1280x720");
        var scroll = screen.GetNode<ScrollContainer>("Margin/Root/ContentScroll");
        var visibleRect = scroll.GetGlobalRect();
        Require(visibleRect.Intersects(screen.GetNode<Control>("%PlayerPanel").GetGlobalRect()) &&
                visibleRect.Intersects(screen.GetNode<Control>("%EnemyPanel").GetGlobalRect()) &&
                visibleRect.Intersects(Cell(screen, 0, 0).GetGlobalRect()) &&
                visibleRect.Intersects(Cell(screen, 9, 5).GetGlobalRect()),
            "libraries and complete board reachable at 1280x720");

        for (var index = 0; index < 24; index++)
        {
            var position = visibleRect.GetCenter();
            GetViewport().PushInput(new InputEventMouseButton
            {
                Position = position,
                GlobalPosition = position,
                ButtonIndex = MouseButton.WheelDown,
                Pressed = true
            });
            await Frame();
        }
        Require(scroll.ScrollVertical > 0 &&
                visibleRect.Intersects(screen.GetNode<Control>("%EquipmentBox").GetGlobalRect()) &&
                visibleRect.Intersects(screen.GetNode<Control>("%Readiness").GetGlobalRect()),
            "Inspector, Equipment, Relic/readiness build area reachable by real scroll input");

        GetWindow().Size = new Vector2I(1600, 900);
        host.Size = new Vector2(1600, 900);
        scroll.ScrollVertical = 0;
        await Frame(4);
        Require(Inside(screen.GetGlobalRect(), screen.GetNode<Control>("%StartButton").GetGlobalRect()),
            "fixed start action reachable at 1600x900");
        Require(Inside(screen.GetGlobalRect(), screen.GetNode<Control>("%ClearEnemyButton").GetGlobalRect()) &&
                Inside(screen.GetGlobalRect(), screen.GetNode<Control>("%ClearAllButton").GetGlobalRect()),
            "fixed clear actions reachable at 1600x900");
    }

    private static bool Inside(Rect2 outer, Rect2 inner) =>
        outer.HasPoint(inner.Position + Vector2.One) &&
        outer.HasPoint(inner.End - Vector2.One);

    private static InputEventMouseButton MouseButtonEvent(Vector2 position, bool pressed) => new()
    {
        Position = position,
        GlobalPosition = position,
        ButtonIndex = MouseButton.Left,
        ButtonMask = pressed ? MouseButtonMask.Left : 0,
        Pressed = pressed
    };

    private async Task Frame(int count = 1)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }
}
