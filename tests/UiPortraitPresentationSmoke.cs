using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.UI;

// Checks the shared portrait's opt-in presentation without a run or player save.
public partial class UiPortraitPresentationSmoke : Node
{
    public override async void _Ready()
    {
        var code = 0;
        try
        {
            var scene = GD.Load<PackedScene>("res://scenes/ui/components/UnitPortrait.tscn");
            var card = scene.Instantiate<UnitPortrait>();
            var other = scene.Instantiate<UnitPortrait>();
            var board = scene.Instantiate<UnitPortrait>();
            card.PreferIllustration = other.PreferIllustration = true;
            foreach (var portrait in new[] { card, other, board })
            {
                AddChild(portrait);
                portrait.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
                portrait.Size = new Vector2(300, 170);
            }
            var count = 0;
            var fallbacks = 0;
            var illustrations = new Dictionary<string, UnitPortraitDefinition>();
            foreach (var file in DirAccess.GetFilesAt("res://content/portraits/heroes"))
            {
                if (!file.EndsWith(".tres", StringComparison.Ordinal)) continue;
                var definition = GD.Load<UnitPortraitDefinition>("res://content/portraits/heroes/" + file);
                var frames = definition.Frames;
                var original = definition.ResolveTexture();
                card.Bind(definition);
                board.Bind(definition);
                Require(!board.IsShowingIllustration && board.IsPortraitPlaying,
                    file + ": board must still animate even when the definition contains an illustration");
                if (definition.Illustration is not null)
                {
                    count++;
                    illustrations.TryAdd(definition.Illustration.ResourcePath, definition);
                    Require(card.IsShowingIllustration && !card.IsPortraitPlaying, file + ": card displays authored art");
                    var sourceFolder = frames!.ResourcePath.GetBaseDir().GetFile();
                    Require(definition.Illustration.ResourcePath.GetFile() == sourceFolder + ".png",
                        file + ": illustration matches current animation identity, not historical labels");
                    Require(definition.CardIllustration is not null &&
                        definition.CardIllustration.ResourcePath == "res://assets/portraits/illustrations/cutouts/" + sourceFolder + ".png",
                        file + ": card derivative matches the same current character identity");
                }
                else
                {
                    fallbacks++;
                    Require(!card.IsShowingIllustration && card.IsPortraitPlaying, file + ": missing art retains animation");
                }
                Require(definition.Frames == frames && definition.ResolveTexture() == original,
                    "presentation must not change shared animation resources");
            }
            Require(count > 0 && fallbacks > 0, "exercise both authored art and missing-art fallback");
            var beast = GD.Load<UnitPortraitDefinition>("res://content/portraits/heroes/hero_hc03_iron_guard.tres");
            var archer = GD.Load<UnitPortraitDefinition>("res://content/portraits/heroes/hero_hc01_crossbow.tres");
            card.Bind(beast);
            other.Bind(archer);
            var cardWindow = card.GetNode<TextureRect>("%PortraitIllustration");
            var otherWindow = other.GetNode<TextureRect>("%PortraitIllustration");
            var firstMaterial = (ShaderMaterial)cardWindow.Material;
            var secondMaterial = (ShaderMaterial)otherWindow.Material;
            Require(firstMaterial != secondMaterial, "portrait crop parameters belong to each scene instance");
            card.Size = new Vector2(80, 110);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(firstMaterial.GetShaderParameter("focal_point").AsVector2() == beast.IllustrationFocus &&
                secondMaterial.GetShaderParameter("focal_point").AsVector2() == archer.IllustrationFocus,
                "resizing and rebinding one portrait preserve the other portrait's focal point");
            Require(firstMaterial.GetShaderParameter("portrait_zoom").AsSingle() == beast.IllustrationZoom &&
                secondMaterial.GetShaderParameter("portrait_zoom").AsSingle() == archer.IllustrationZoom,
                "instances retain each illustration's authored zoom");
            card.FullIllustration = true;
            card.StoneFrame = true;
            card.IllustrationBottomOcclusion = 30;
            card.Size = new Vector2(320, 494);
            card.Bind(beast);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(cardWindow.Size.IsEqualApprox(card.Size) && cardWindow.Position.IsEqualApprox(Vector2.Zero),
                "full illustration fills the tall card window");
            Require(card.GetNode<Control>("%PortraitFrame").Visible &&
                firstMaterial.GetShaderParameter("stone_frame").AsBool() &&
                !other.GetNode<Control>("%PortraitFrame").Visible,
                "stone opening belongs to the opting-in card, not to other portrait instances");
            Require(firstMaterial.GetShaderParameter("full_illustration").AsBool() &&
                !secondMaterial.GetShaderParameter("full_illustration").AsBool(),
                "large card framing does not change another instance's small portrait mask");
            Require(cardWindow.Texture == beast.CardIllustration && otherWindow.Texture == archer.CardIllustration,
                "large and small UI windows use the transparent figure with separate framing");
            Require(firstMaterial.GetShaderParameter("focal_point").AsVector2() == beast.CardIllustrationFocus &&
                firstMaterial.GetShaderParameter("portrait_zoom").AsSingle() == beast.CardIllustrationZoom &&
                firstMaterial.GetShaderParameter("bottom_occlusion").AsSingle() == 30,
                "card-owned framing and foreground occlusion reach its material");
            card.Size = new Vector2(500, 200);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(cardWindow.Size.IsEqualApprox(card.Size), "large artwork responds to its allocated window without a medallion width cap");
            Require(board.IsPortraitPlaying && !board.IsShowingIllustration,
                "large card resizing leaves the battlefield portrait animated");
            var item = GD.Load<Texture2D>("res://assets/ui/items/cutouts/item_field_rations.png");
            card.Bind(null, item);
            Require(!card.GetNode<Control>("%PortraitFrame").Visible &&
                !card.GetNode<Control>("%PortraitRecess").Visible,
                "unit-to-item rebind clears the unit-owned frame and recess");
            Require(!card.IsShowingIllustration && !card.IsPortraitPlaying &&
                card.GetNode<TextureRect>("%PortraitFallback").Texture == item, "unit-to-item rebind clears illustration");
            card.Bind(null);
            Require(!card.GetNode<TextureRect>("%PortraitFallback").Visible, "empty rebind clears all artwork");
            Require(cardWindow.Texture is null && !cardWindow.Visible, "empty rebind releases card illustration");
            var withoutCutout = (UnitPortraitDefinition)archer.Duplicate();
            withoutCutout.CardIllustration = null;
            card.Bind(withoutCutout);
            Require(cardWindow.Texture == archer.Illustration, "a missing derivative retains the original illustration");
            GD.Print($"UI_PORTRAIT_PRESENTATION_OK illustrated={count} animation-fallbacks={fallbacks} board=animated identity=current-frames instances=isolated full-art=resized rebind=cleared");
            card.Hide(); other.Hide(); board.Hide();
            if (Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--review"))
                await CaptureIllustrations(scene, illustrations.Values);
            if (Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--review-cards"))
                await CaptureCardArt(illustrations.Values);
        }
        catch (Exception error) { GD.PrintErr(error); code = 1; }
        GetTree().Quit(code);
    }

    // Opt-in visual fixture: uses the production shader and resource crops, with no player state.
    private async Task CaptureIllustrations(PackedScene scene, IEnumerable<UnitPortraitDefinition> definitions)
    {
        Require(DisplayServer.GetName() != "headless", "portrait review needs rendered output");
        GetWindow().Size = new Vector2I(1600, 900);
        var output = ProjectSettings.GlobalizePath("res://.godot/ui-review");
        System.IO.Directory.CreateDirectory(output);
        var page = new Control();
        AddChild(page);
        var cells = new List<Control>();
        var index = 0;
        var pageNumber = 1;
        foreach (var definition in definitions)
        {
            if (index == 12)
            {
                await CapturePage(pageNumber++);
                foreach (var cell in cells) cell.QueueFree();
                cells.Clear();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                index = 0;
            }
            var cellRoot = new Control { Position = new Vector2(index % 4 * 400, index / 4 * 300) };
            page.AddChild(cellRoot);
            cells.Add(cellRoot);
            cellRoot.AddChild(new TextureRect {
                Position = new Vector2(5, 10), Size = new Vector2(145, 220),
                Texture = definition.Illustration, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            });
            var portrait = scene.Instantiate<UnitPortrait>();
            portrait.PreferIllustration = true;
            cellRoot.AddChild(portrait);
            portrait.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            portrait.Position = new Vector2(155, 20);
            portrait.Size = new Vector2(240, 200);
            portrait.Bind(definition);
            cellRoot.AddChild(new Label {
                Position = new Vector2(15, 240),
                Text = definition.Illustration!.ResourcePath.GetFile().GetBaseName() + "\n" +
                    definition.Illustration.GetSize() + " / focus " + definition.IllustrationFocus
            });
            index++;
        }
        if (index > 0) await CapturePage(pageNumber);
        page.QueueFree();

        async Task CapturePage(int number)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var capture = GetViewport().GetTexture().GetImage();
            Require(capture.SavePng(output + $"/portrait-crops-{number}.png") == Error.Ok, "save review page");
        }
    }

    // Inspect every distinct illustration inside the actual authored stone card.
    // This rendering fixture deliberately does not claim gameplay input coverage.
    private async Task CaptureCardArt(IEnumerable<UnitPortraitDefinition> definitions)
    {
        Require(DisplayServer.GetName() != "headless", "card review needs rendered output");
        GetWindow().Size = new Vector2I(1600, 900);
        var output = ProjectSettings.GlobalizePath("res://.godot/ui-review");
        System.IO.Directory.CreateDirectory(output);
        var scene = GD.Load<PackedScene>("res://scenes/ui/components/RosterHeroCard.tscn");
        var page = new Control { Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres") };
        AddChild(page);
        var index = 0;
        var number = 1;
        foreach (var definition in definitions)
        {
            if (index == 4)
            {
                await CapturePage(number++);
                foreach (var child in page.GetChildren()) child.QueueFree();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                index = 0;
            }
            var card = scene.Instantiate<RosterHeroCard>();
            page.AddChild(card);
            card.Position = new Vector2(10 + index * 400, 40);
            card.Size = new Vector2(380, 740);
            var information = card.GetNode<WorkbenchHeroSummary>("%Information");
            information.GetNode<UnitPortrait>("%Portrait").Bind(definition);
            information.GetNode<Label>("%HeroName").Text = definition.Illustration!.ResourcePath.GetFile().GetBaseName();
            card.GetNode<Label>("%HeroRank").Text = "I";
            index++;
        }
        if (index > 0) await CapturePage(number);
        page.QueueFree();

        async Task CapturePage(int pageNumber)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var capture = GetViewport().GetTexture().GetImage();
            Require(capture.SavePng(output + $"/card-art-{pageNumber}.png") == Error.Ok, "save card review page");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
