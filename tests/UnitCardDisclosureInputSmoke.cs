using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Attributes;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;
using TowerAutobattler.Growth;

// Production cards, catalog data and real GUI input; never moves the desktop mouse.
public partial class UnitCardDisclosureInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private readonly UiDragMotionRecorder _recording = new();
    private readonly UiDragMotionRecorder _openingRecording = new();
    private const string Output = "res://.godot/ui-review/unit-card-disclosure";
    private bool _reduced;
    private RunOfferChoiceCard? _revealingCard;
    private bool _observedDirectionalReveal;
    private bool _observedShadeFade;
    public override async void _Ready()
    {
        var exit = 0;
        UiMotionBinding? motion = null;
        try
        {
            _stage = new UiInputStage(this, true);
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(Output));
            var growthOpening = OS.GetCmdlineUserArgs().Contains("--growth-opening");
            var gate = growthOpening ? await GrowthContentPackage.CreateReadyAsync(this)
                : await GamePackagePublisher.CreateReadyAsync(this,
                    GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var save = new MemorySave(growthOpening ? "null" : System.IO.File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/fixtures/first-boss-ranged-stall.json")));
            var app = new RunApplication(package.Content, save, package.Project, growthRules: package.GrowthRules);
            var host = new Control { Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres") };
            _stage.AddChild(host);
            host.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var backdrop = new TextureRect { Texture = GD.Load<Texture2D>("res://assets/ui/tavern/reference-card.png"),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, MouseFilter = Control.MouseFilterEnum.Ignore,
                SelfModulate = new Color(.36f, .34f, .32f) };
            host.AddChild(backdrop); backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            motion = new UiMotionBinding(host, () => _reduced);
            if (!OS.GetCmdlineUserArgs().Contains("--opening-only"))
            {
                var offers = new RunOfferChoiceCard[3];
                var confirmations = 0;
                for (var i = 0; i < offers.Length; i++)
                {
                    var card = GD.Load<PackedScene>("res://scenes/ui/components/RunOfferChoiceCard.tscn").Instantiate<RunOfferChoiceCard>();
                    host.AddChild(card); card.Position = new Vector2(35 + i * 510, 70); card.Size = new Vector2(500, 740);
                    var hero = app.ActiveRun!.Roster[i % app.ActiveRun.Roster.Count];
                    card.Bind(RunOfferDetailText.Card(app, hero.ContentId, "offer-" + i, "", "招募 · 3 金币", "测试夹具，不扣金币。", "", false), _ => confirmations++);
                    card.SetAvailableHeight(740); offers[i] = card;
                }
                Move(new Vector2(1580, 880));
                await Frames(5);
                var first = offers[0]; var last = offers[2];
                var art = first.GetNode<Control>("Layout/Artwork");
                var initialArt = art.GetGlobalRect();
                var initialButton = first.ConfirmButton.GetGlobalRect();
                var edgeShade = first.Orbit.GetNode<Control>("EdgeShade");
                Require(!edgeShade.IsVisibleInTree(), "idle card retains unobscured art");
                await Capture("offers-default");
                // PNG compression blocks this thread. Let that frame finish
                // before measuring a new real-time transition.
                await Frames(3);
                _revealingCard = first;
                Move(art.GetGlobalRect().GetCenter());
                await Record("hover-open", .80);
                Require(_observedDirectionalReveal, "attributes enter inward and skills enter downward");
                Require(_observedShadeFade && edgeShade.IsVisibleInTree() && edgeShade.Modulate.A == 1,
                    "hover smoothly fades in the shared edge shade");
                Require(first.Disclosure.IsOpen && !first.Disclosure.IsPinned, "hover opens a transient annex");
                Require(art.GetGlobalRect() == initialArt && art.IsVisibleInTree() && first.ConfirmButton.GetGlobalRect() == initialButton,
                    "hover never hides art or moves confirmation");
                CheckPerimeter(first, offers.Where(card => card != first).ToArray());
                Move(first.Orbit.Fact(CombatAttribute.CriticalChance).GetGlobalRect().GetCenter());
                await Record("single-attribute", .7);
                var hint = Descendants<BattleLabTooltip>(host).Single(tooltip => tooltip.Visible);
                Require(hint.GetNode<Label>("%Title").Text == "暴击率", "one hovered attribute has one own tooltip");
                Require(!hint.GetNode<CombatRichText>("%SingleEntry").Text.Contains("法力上限"), "tooltip never includes other attributes");
                var shortEntry = hint.GetNode<CombatRichText>("%SingleEntry");
                Require(shortEntry.GetContentHeight() <= shortEntry.Size.Y, "a short attribute explanation fits without clipping or scrolling");
                await Capture("attribute-hover");
                // Return to the name rail, outside the currently open tooltip's
                // pointer bridge, so the clean card is captured without an old hint.
                Move(new Vector2(art.GetGlobalRect().GetCenter().X, art.GetGlobalRect().End.Y - 28));
                await Record("return-to-card", .5);
                await Capture("offers-hover");
                await Click(first.GetNode<Button>("UnitDetailsButton"));
                Move(new Vector2(30, 30)); await Record("pinned", .30);
                Require(first.Disclosure.IsPinned && first.Disclosure.IsOpen, "clicked details remain pinned off-card");
                Move(last.GetNode<Control>("Layout/Artwork").GetGlobalRect().GetCenter()); await Record("pin-priority", .3);
                Require(first.Disclosure.IsPinned && !last.Disclosure.IsOpen, "passing another card cannot steal pinned details");
                PressKey(Key.Escape); await Record("escape-close", .30);
                Require(!first.Disclosure.IsOpen && confirmations == 0, "escape closes without action");
                Require(!edgeShade.IsVisibleInTree(), "escape clears edge shade");
                Move(last.GetNode<Control>("Layout/Artwork").GetGlobalRect().GetCenter());
                await Record("right-edge-open", .55);
                Require(last.Disclosure.IsOpen && !first.Disclosure.IsOpen, "only one annex visible");
                CheckPerimeter(last, offers.Where(card => card != last).ToArray());
                await Capture("offers-right-edge");
                var skill = last.Disclosure.Groups[2].GetChildren().OfType<Button>().First();
                Move(skill.GetGlobalRect().GetCenter()); await Record("single-skill", .7);
                hint = Descendants<BattleLabTooltip>(host).Single(tooltip => tooltip.Visible);
                Require(!string.IsNullOrWhiteSpace(hint.GetNode<CombatRichText>("%SingleEntry").Text), "own skill rules remain available");
                await Capture("skill-hover");
                Move(hint.GetNode<Control>("%TooltipPanel").GetGlobalRect().GetCenter());
                await Record("read-own-tooltip", .4);
                Require(hint.Visible && last.Disclosure.IsOpen, "pointer can enter its own tooltip without collapse");
                Move(new Vector2(30, 30)); await Record("leave-close", .5);
                Require(!last.Disclosure.IsOpen, "leaving closes transient preview");
                var button = first.GetNode<Button>("UnitDetailsButton");
                button.GrabFocus(); PressKey(Key.Enter); await Frames(1);
                _reduced = true; await Frames(3);
                Require(first.Disclosure.IsPinned && first.Disclosure.Groups.SelectMany(group => group.GetChildren().OfType<Control>()).All(item => item.Modulate.A == 1 && item.OffsetTransformScale.IsEqualApprox(Vector2.One)), "keyboard pin and live reduced motion settle");
                Require(edgeShade.IsVisibleInTree() && edgeShade.Modulate.A == 1, "reduced motion settles shade too");
                PressKey(Key.Escape); await Frames(2);
                Require(!first.Disclosure.IsOpen, "reduced close immediate");
                Require(!edgeShade.IsVisibleInTree(), "reduced close clears shade immediately");
                await Click(first.ConfirmButton);
                Require(confirmations == 1, "explicit confirmation fires exactly once");
                await Click(button); await Click(button); await Click(button);
                Require(first.Disclosure.IsPinned, "rapid close and reopen keeps the latest intent");
                host.Hide(); await Frames(2); host.Show(); await Frames(2);
                Require(!first.Disclosure.IsOpen, "owner hiding clears floating canvas and pin");
                Require(!edgeShade.IsVisibleInTree(), "owner hiding leaves no black veil");
                first.Bind(new RunOfferChoiceViewModel("opportunity", "机会", "", null, null, false,
                    "", "", "", "", "", "", "保留原有机会页", "规则", "领取", IsOpportunity: true), _ => confirmations++);
                first.SetAvailableHeight(680); await Frames(2); Move(art.GetGlobalRect().GetCenter()); await Record("non-unit", .35);
                Require(!first.Disclosure.IsOpen && first.BodyScroll.IsVisibleInTree(), "non-unit opportunities retain their normal copy layout");
                Require(first.GetGlobalRect().Encloses(first.ConfirmButton.GetGlobalRect()), "non-unit action remains within its existing card");
                await Click(first.DetailsButton);
                Require(first.DetailsExpanded && confirmations == 1, "non-unit details remain inspection only");
                await Capture("non-unit-action");
                first.ConfirmButton.Disabled = true;
                await Click(first.ConfirmButton);
                Require(confirmations == 1, "disabled action never submits");
                first.ConfirmButton.Disabled = false;
                await Click(first.ConfirmButton);
                Require(confirmations == 2, "non-unit explicit action still submits once after rebinding");
                foreach (var card in offers) card.Hide();

                var tile = GD.Load<PackedScene>("res://scenes/ui/components/HeroLibraryTile.tscn").Instantiate<HeroLibraryTile>();
                host.AddChild(tile); tile.Position = new Vector2(540, 100); tile.Size = new Vector2(500, 680);
                var selected = 0; tile.SelectionRequested += _ => selected++;
                var entry = package.Content.TryGet(app.ActiveRun!.Roster[0].ContentId, out var found) ? found : throw new InvalidOperationException("hero missing");
                tile.Bind(new HeroSelectionViewModel("opening", (UnitDefinition)entry.Definition, true, "成长规则", "实际角色数据", BattleSetupFactory.Snapshot(entry, package.Content)));
                await Frames(3); Move(tile.GetGlobalRect().GetCenter()); await Record("opening-hover", .4);
                Require(tile.Disclosure.IsOpen && selected == 0, "opening hover inspects but does not select");
                await Click(tile.GetNode<Button>("DetailsButton"));
                Require(tile.Disclosure.IsPinned && selected == 0, "opening details button does not select");
                var openingFrame = tile.GetNode<Control>("CardFrame").GetGlobalRect();
                Require(tile.GetNode<Control>("%HeroPortrait").GetGlobalRect().End.Y >= openingFrame.End.Y - 24 &&
                    tile.GetNode<Control>("%HeroName").GetGlobalRect().End.Y >= openingFrame.End.Y - 24,
                    "opening portrait and name reach the lower edge without a reserved equipment strip");
                await Capture("opening-card");
                await Click(tile);
                Require(selected == 1, "card body retains explicit selection");
                tile.Hide(); await Frames(2);
                Require(!tile.Disclosure.IsOpen, "hidden tile leaves no floating details");
            }
            app.AbandonRun();
            Require(app.BeginOpeningRecruitment(90429), "real opening draft fixture");
            var opening = GD.Load<PackedScene>("res://scenes/ui/HeroSelectScreen.tscn").Instantiate<HeroSelectScreen>();
            host.AddChild(opening); opening.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            opening.OpeningHeroToggled += id =>
            {
                Require(app.ToggleOpeningHero(id), "real opening selection command");
                opening.RefreshOpening(app.ActiveRun!.OpeningRecruitment!);
            };
            var openingConfirmations = 0;
            opening.OpeningConfirmed += () => { Require(app.ConfirmOpeningRecruitment(), "real opening confirmation"); openingConfirmations++; };
            opening.BindOpening(app);
            Move(new Vector2(800, 150)); await Record("opening-arrival", .8);
            var library = opening.GetNode<GridContainer>("%HeroLibrary");
            var choices = library.GetChildren().OfType<HeroLibraryTile>().ToArray();
            Require(library.Columns == 6 && choices.Length == 6 && choices.All(choice =>
                Math.Abs(choice.Position.Y - choices[0].Position.Y) <= 1), "all six opening candidates share one row");
            Require(opening.GetNode<PanelContainer>("%LibraryPanel").GetThemeStylebox("panel") is StyleBoxEmpty,
                "opening candidates have no enclosing backplate");
            var firstChoice = choices[0];
            var openingScroll = opening.GetNode<ScrollContainer>("%LibraryScroll");
            Require(choices.All(choice => openingScroll.GetGlobalRect().Encloses(choice.GetGlobalRect())) &&
                openingScroll.ScrollVertical == 0, "all six complete candidate cards fit without scrolling");
            Require(Math.Abs(library.GetGlobalRect().GetCenter().X - openingScroll.GetGlobalRect().GetCenter().X) <= 2,
                "the complete opening grid is centered in its available area");
            Require(choices.All(choice => choice.Size.X >= 234 && choice.Size.Y >= 351 &&
                Math.Abs(choice.Size.Y / choice.Size.X - 1.5f) < .01f &&
                !choice.Orbit.ShowEquipment && !choice.Orbit.GetNode<Control>("TopDock/Equipment").IsVisibleInTree()),
                "six cards retain the authored stone-frame ratio and exclude equipment");
            var firstSeat = opening.GetNode<OpeningHeroSeat>("%FirstSeat");
            var secondSeat = opening.GetNode<OpeningHeroSeat>("%SecondSeat");
            Require(firstSeat.Disabled && secondSeat.Disabled &&
                !opening.GetNode<Label>("%OpeningStatus").Visible, "empty seats replace permanent selection instructions");
            await Capture("opening-page-idle");
            GD.Print($"OPENING_CARD_LAYOUT scroll={openingScroll.GetGlobalRect()} offset={openingScroll.ScrollVertical} card={firstChoice.GetGlobalRect()}");
            Move(firstChoice.GetGlobalRect().GetCenter()); await Record("opening-page-hover", .5);
            Require(firstChoice.OffsetTransformScale.X > 1 &&
                firstChoice.GetNode<Control>("CardFrame").OffsetTransformScale == Vector2.One &&
                firstChoice.GetNode<Control>("%HeroPortrait").OffsetTransformScale == Vector2.One,
                "the complete card scales once, including its frame and portrait");
            Require(firstChoice.Disclosure.IsOpen && firstChoice.Disclosure.Groups.All(group => firstChoice.GetGlobalRect().Encloses(group.GetGlobalRect())),
                "all groups fit the real opening page");
            Require(firstChoice.Disclosure.Groups.Count == 3 &&
                !firstChoice.Orbit.GetNode<Control>("TopDock/Equipment").IsVisibleInTree(), "hover never reopens disabled equipment");
            Require(firstChoice.Disclosure.Groups.All(group => openingScroll.GetGlobalRect().Encloses(group.GetGlobalRect())),
                "opening focus keeps the top skills in view");
            await Capture("opening-page-hover");
            Require(Descendants<CompactDetailFact>(firstChoice.Orbit).Count() == 12,
                "opening keeps all twelve secondary values plus three permanent core facts");
            var openingArmor = firstChoice.Orbit.Fact(CombatAttribute.Armor);
            Move(openingArmor.GetGlobalTransformWithCanvas() * (openingArmor.Size * .5f));
            await Record("opening-transformed-hit", .65);
            var armorHint = Descendants<BattleLabTooltip>(host).Single(tooltip => tooltip.Visible);
            Require(armorHint.GetNode<Label>("%Title").Text == "防御" && !firstChoice.OffsetTransformVisualOnly,
                "the visibly lifted armor entry receives input at its rendered location");
            if (growthOpening)
                Require(firstChoice.Orbit.GetNode<Button>("Left/Growth").IsVisibleInTree() &&
                    firstChoice.Orbit.GetNode<Button>("Right/Traits").IsVisibleInTree(),
                    "formal growth opening keeps growth and trait contribution entries");
            var openingSkill = firstChoice.Disclosure.Groups[2].GetChildren().OfType<Button>().First();
            Require(openingSkill.Icon is not null, "actual skill retains its semantic icon");
            Move(openingSkill.GetGlobalRect().GetCenter()); await Record("opening-skill-reading", .65);
            var openingHint = Descendants<BattleLabTooltip>(host).Single(tooltip => tooltip.Visible);
            Require(!string.IsNullOrWhiteSpace(openingHint.GetNode<CombatRichText>("%SingleEntry").Text) &&
                app.ActiveRun!.OpeningRecruitment!.SelectedIds.Length == 0,
                "reading the actual skill explanation cannot select a hero");
            await Capture("opening-skill-reading");
            Move(new Vector2(800, 150)); await Record("opening-skill-leave", .5);
            await Click(firstChoice);
            Require(firstSeat.StableId == firstChoice.StableId && !firstSeat.Disabled && secondSeat.Disabled,
                "committed selection populates its portrait seat");
            Move(choices[5].GetGlobalRect().GetCenter()); await Record("opening-last-card", .5);
            Require(choices[5].Disclosure.IsOpen && choices[5].Disclosure.Groups.All(group => openingScroll.GetGlobalRect().Encloses(group.GetGlobalRect())),
                "rightmost attributes and skills remain fully visible");
            Require(choices.All(choice => !choice.HasNode("DetailsButton")) &&
                !choices[5].Disclosure.IsPinned && app.ActiveRun!.OpeningRecruitment!.SelectedIds.Length == 1,
                "opening inspection needs no visible details button and never selects or pins");
            var rightFacts = choices[5].Orbit.GetNode<Control>("Right").GetChildren().OfType<CompactDetailFact>().ToArray();
            Require(rightFacts.Length == 6 && rightFacts.All(fact =>
                fact.GetNode<Control>("%FactValue").GetGlobalRect().End.X <= fact.GetNode<Control>("%FactIcon").GetGlobalRect().Position.X),
                "right column places every value before its edge-aligned icon");
            foreach (var (attribute, caption) in new[] { (CombatAttribute.StartingMana, "初始法力"), (CombatAttribute.MaxMana, "法力上限") })
            {
                var fact = choices[5].Orbit.Fact(attribute);
                Require(!fact.GetNode<Label>("%FactValue").Text.Contains('/'), "mana is split into independent values");
                Move(fact.GetGlobalRect().GetCenter()); await Record("opening-mana-" + attribute, .6);
                var hint = Descendants<BattleLabTooltip>(host).Single(tooltip => tooltip.Visible);
                Require(hint.GetNode<Label>("%Title").Text == caption, "each mana entry explains its own value");
            }
            PressKey(Key.Escape); await Record("opening-close", .5);
            Require(!choices[5].Disclosure.IsOpen, "escape closes buttonless inspection");
            Move(new Vector2(800, 100)); await Record("opening-leave", .3);
            await Click(choices[5]);
            await Record("opening-selected-details", .35);
            Require(choices[5].Disclosure.IsOpen && choices[5].Disclosure.Groups.All(group =>
                group.IsVisibleInTree() &&
                !group.GetGlobalRect().Intersects(choices[5].GetNode<Control>("%HeroState").GetGlobalRect())),
                "selected header must not overlap: tier=" + choices[5].GetNode<Control>("%HeroState").GetGlobalRect() +
                " groups=" + string.Join(";", choices[5].Disclosure.Groups.Select(group => group.GetGlobalRect())));
            await Capture("opening-two-selected");
            Require(secondSeat.StableId == choices[5].StableId, "second committed selection fills the second seat");
            await Click(secondSeat);
            Require(!choices[5].GetNode<Panel>("CardFrame").HasThemeStyleboxOverride("panel"),
                "seat cancellation restores the original card frame");
            await Click(choices[5]);
            Require(!opening.GetNode<Button>("%ConfirmOpening").Disabled, "two choices enable confirmation");
            _reduced = true; await Frames(3);
            Require(choices.All(choice => choice.OffsetTransformScale == Vector2.One &&
                choice.OffsetTransformPosition == Vector2.Zero && choice.Modulate.A == 1),
                "live reduced motion settles the complete cards without losing selection");
            _reduced = false;
            await Click(choices[5]); await Click(choices[5]); await Click(choices[5]);
            await Click(choices[5]); await Record("opening-rapid-reselect", .3);
            Require(app.ActiveRun!.OpeningRecruitment!.SelectedIds.Length == 2 && secondSeat.StableId == choices[5].StableId,
                "rapid card input keeps seats aligned with the authoritative draft");
            await Click(opening.GetNode<Button>("%ConfirmOpening"));
            Require(openingConfirmations == 1, "opening confirms exactly once");
            GD.Print("UNIT_CARD_DISCLOSURE_OK opening-single-row no-backplate optional-equipment frame-selection mirrored-values split-mana nonoverlapping-header deselection buttonless-hover escape rightmost-selection confirmation");
        }
        catch (Exception error) { GD.PrintErr("UNIT_CARD_DISCLOSURE_FAILED " + error); exit = 1; await Capture("failure"); }
        finally
        {
            motion?.Dispose();
            if (!OS.GetCmdlineUserArgs().Contains("--opening-only")) _recording.Save("unit-card-disclosure-motion");
            _openingRecording.Save("opening-card-motion");
        }
        GetTree().Quit(exit);
    }
    private static System.Collections.Generic.IEnumerable<T> Descendants<T>(Node node) where T : Node
    {
        foreach (var child in node.GetChildren()) { if (child is T value) yield return value; foreach (var nested in Descendants<T>(child)) yield return nested; }
    }
    private void CheckPerimeter(RunOfferChoiceCard card, RunOfferChoiceCard[] neighbours)
    {
        var frame = card.GetNode<Control>("CardFrame").GetGlobalRect();
        var groups = card.Disclosure.Groups;
        Require(Math.Abs(groups[0].GetGlobalRect().Position.Y - groups[1].GetGlobalRect().Position.Y) < 1,
            "attribute columns share a reading baseline");
        var shade = card.Orbit.GetNode<Control>("EdgeShade");
        Require(shade.MouseFilter == Control.MouseFilterEnum.Ignore &&
            shade.GetChildren().OfType<Control>().All(part => part.MouseFilter == Control.MouseFilterEnum.Ignore), "shade never intercepts input");
        Require(shade.ZAsRelative && card.Orbit.ZIndex + shade.ZIndex == 1 &&
            card.GetNode<Control>("Layout/Artwork/ArtTitle").ZIndex > card.Orbit.ZIndex + shade.ZIndex,
            "shade follows its card layer above art and below the name rail");
        Require(frame.Size.X >= card.Size.X * .82f && frame.Size.X / frame.Size.Y >= .60f, "card frame is broad rather than a narrow strip");
        Require(card.ConfirmButton.GetGlobalRect().Position.Y >= frame.End.Y + 12,
            "recruitment action sits below the stone frame, separated from unit information");
        Require(groups[0].GetGlobalRect().End.X < frame.GetCenter().X && groups[1].GetGlobalRect().Position.X > frame.GetCenter().X, "attributes stay on the inner left and right sides");
        Require(groups.Count == 3 && groups[2].GetGlobalRect().End.Y < frame.GetCenter().Y,
            "candidate cards reveal attributes and top-edge skills only");
        Require(!card.Orbit.ShowEquipment && !card.Orbit.GetNode<Control>("TopDock/Equipment").IsVisibleInTree(),
            "recruitment has no unrelated empty equipment slots");
        Require(card.GetNode<Control>("Layout/Artwork/Portrait").GetGlobalRect().End.Y >= frame.End.Y - 24,
            "portrait reaches the lower frame with no reserved equipment strip");
        Require(card.GetNode<Control>("Layout/Artwork/ArtTitle").GetGlobalRect().End.Y >= frame.End.Y - 24,
            "the name rail finishes the lower edge of the card");
        Require(Descendants<CompactDetailFact>(card.Orbit).Count() == 11, "eleven secondary entries plus three permanent core values");
        Require(card.Orbit.Fact(CombatAttribute.MaxMana).GetNode<Label>("%FactValue").Text.Contains('/'), "mana combines initial and maximum");
        Require(!Descendants<CompactDetailFact>(card.Orbit).Any(fact => fact.Name.ToString().StartsWith("ManaPer") || fact.Name == "StartingMana"),
            "regeneration values and duplicate initial mana are absent from the card");
        Require(groups[2].GetChildCount() is >= 1 and <= 2, "top skills contain only actual skills, not growth or trait explanations");
        foreach (var group in groups)
        {
            Require(frame.Encloses(group.GetGlobalRect()), $"each group lands inside the stone frame: {group.Name} group={group.GetGlobalRect()} frame={frame}");
            Require(!group.GetGlobalRect().Intersects(card.ConfirmButton.GetGlobalRect()), "inner entries never cover confirmation");
            foreach (var neighbour in neighbours) Require(!group.GetGlobalRect().Intersects(neighbour.GetGlobalRect()), "perimeter never covers neighbour");
        }
        foreach (var fact in Descendants<CompactDetailFact>(card.Orbit))
        {
            Require(!fact.GetNode<Label>("%FactCaption").Visible, "only icon and value, never permanent prose");
            var icon = fact.GetNode<TextureRect>("%FactIcon");
            Require(icon.Texture is AtlasTexture && icon.Modulate == Colors.White, "painted material and native colors survive binding: " + fact.Name);
            var value = fact.GetNode<Label>("%FactValue");
            var textWidth = value.GetThemeFont("font").GetStringSize(value.Text, fontSize: value.GetThemeFontSize("font_size")).X;
            Require(fact.Size.X >= textWidth + icon.Size.X + 15,
                "icon plus complete value fit: " + fact.Name);
            foreach (var state in new[] { "normal", "hover", "pressed" })
                Require(fact.GetThemeStylebox(state) is StyleBoxEmpty, "attributes have no backing plate in " + state);
        }
    }
    private void Move(Vector2 p) => _stage.Push(new InputEventMouseMotion { Position = p, GlobalPosition = p });
    private async Task Click(Control target)
    {
        var p = target.GetGlobalRect().GetCenter(); Move(p);
        _stage.Push(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }
    private void PressKey(Key code)
    {
        _stage.Push(new InputEventKey { Keycode = code, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = code, Pressed = false });
    }
    private async Task Record(string phase, double seconds)
    {
        var until = Time.GetTicksMsec() + (ulong)(seconds * 1000);
        do
        {
            await Frames(1);
            if (phase == "hover-open" && _revealingCard?.Disclosure.IsOpen == true)
            {
                var shade = _revealingCard.Orbit.GetNode<Control>("EdgeShade");
                _observedShadeFade |= shade.Modulate.A > 0 && shade.Modulate.A < 1;
                Require(shade.OffsetTransformPosition.IsEqualApprox(Vector2.Zero) && shade.OffsetTransformScale.IsEqualApprox(Vector2.One),
                    "gradient fades without breathing or shifting its edges");
                var offsets = _revealingCard.Disclosure.Groups.Select(group => group.GetChildren().OfType<Control>().First().OffsetTransformPosition).ToArray();
                // Ignore the small Back-easing overshoot: it has the opposite
                // sign even for a wrong outward entrance.
                _observedDirectionalReveal |= offsets[0].X < -12 && offsets[1].X > 12 && offsets[2].Y < -10;
            }
            if (phase.StartsWith("opening-")) _openingRecording.Sample(_stage.Viewport, phase);
            else _recording.Sample(_stage.Viewport, phase);
        } while (Time.GetTicksMsec() < until);
    }
    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            // Keep the isolated viewport rendering when macOS occludes the host.
            Callable.From(() => RenderingServer.ForceDraw(false)).CallDeferred();
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
    private async Task Capture(string name)
    {
        await Frames(2); using var image = _stage.Viewport.GetTexture().GetImage(); image.SavePng(Output + "/" + name + ".png");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class MemorySave(string json) : IRunSaveService
    {
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => JsonSerializer.Deserialize<ActiveRunDto>(json);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) => true;
        public void DeleteActiveRun() { }
    }
}
