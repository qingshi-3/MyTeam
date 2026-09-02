using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class SemanticPresentationContractSmoke : Node
{
    public override async void _Ready()
    {
        var failures = new List<string>();
        try
        {
            VerifyCatalogAndAssets(failures);
            VerifyAuthoredScenes(failures);
            VerifyBindingBoundaries(failures);
            await VerifyRuntimeAsync(failures);
        }
        catch (Exception exception)
        {
            failures.Add(exception.GetType().Name + ": " + exception.Message);
        }
        if (failures.Count > 0)
        {
            GD.PrintErr("SEMANTIC_PRESENTATION_CONTRACT_FAILED: " + string.Join(" | ", failures));
            GetTree().Quit(1);
            return;
        }

        GD.Print("SEMANTIC_PRESENTATION_CONTRACT_OK catalog=39 chips=authored routes=7 portraits=independent-idle tactical-points=distinct");
        GetTree().Quit();
    }

    private static void VerifyCatalogAndAssets(List<string> failures)
    {
        Expect(failures, "res://content/ui/semantic_icon_catalog.tres");
        Expect(failures, "res://scenes/ui/components/SemanticChip.tscn");
        Expect(failures, "res://src/UI/SemanticIconCatalog.cs");
        Expect(failures, "res://src/UI/SemanticChip.cs");
        Expect(failures, "res://assets/ui/icons/reach.svg");
        foreach (var name in new[] { "vanguard", "fighter", "ranged", "support", "assassin", "summoner", "artillery", "boss" })
            Expect(failures, $"res://assets/ui/icons/role-{name}.svg");
        foreach (var name in new[] { "order", "desert", "undead", "beast", "machine", "frost", "neutral", "enemy" })
            Expect(failures, $"res://assets/ui/icons/faction-{name}.svg");
        foreach (var name in new[] { "combat", "elite", "recruitment", "shop", "event", "rest", "boss" })
            Expect(failures, $"res://assets/ui/icons/tower-{name}.svg");
        var newIconSources = new[] { "reach.svg" }
            .Concat(new[] { "vanguard", "fighter", "ranged", "support", "assassin", "summoner", "artillery", "boss" }.Select(name => $"role-{name}.svg"))
            .Concat(new[] { "order", "desert", "undead", "beast", "machine", "frost", "neutral", "enemy" }.Select(name => $"faction-{name}.svg"))
            .Concat(new[] { "combat", "elite", "recruitment", "shop", "event", "rest", "boss" }.Select(name => $"tower-{name}.svg"))
            .Select(name => Read("res://assets/ui/icons/" + name)).ToArray();
        if (newIconSources.Distinct(StringComparer.Ordinal).Count() != 24 || newIconSources.Any(source => !source.Contains("#fff", StringComparison.OrdinalIgnoreCase)))
            failures.Add("new semantic SVG sources are not 24 differentiated monochrome assets");
    }

    private static void VerifyAuthoredScenes(List<string> failures)
    {
        var portrait = Read("res://scenes/ui/components/UnitPortrait.tscn");
        if (!portrait.Contains("type=\"AnimatedSprite2D\"", StringComparison.Ordinal))
            failures.Add("UnitPortrait does not author an independent AnimatedSprite2D");
        var chip = Read("res://scenes/ui/components/SemanticChip.tscn");
        if (!chip.Contains("type=\"TextureRect\"", StringComparison.Ordinal) || !chip.Contains("type=\"Label\"", StringComparison.Ordinal))
            failures.Add("SemanticChip does not author icon and Chinese text regions");
        var unitCard = Read("res://scenes/ui/components/UnitChoiceCard.tscn");
        if (!unitCard.Contains("IdentityFacts", StringComparison.Ordinal) || !unitCard.Contains("AttributeFacts", StringComparison.Ordinal))
            failures.Add("UnitChoiceCard lacks authored responsibility/trait and attribute fact groups");
        if (!unitCard.Contains("TraitBadge.tscn", StringComparison.Ordinal) || !unitCard.Contains("StatBlock.tscn", StringComparison.Ordinal))
            failures.Add("UnitChoiceCard does not use the confirmed trait/stat presentation roles");
    }

    private static void VerifyBindingBoundaries(List<string> failures)
    {
        var tower = Read("res://src/UI/TowerScreenController.cs");
        if (!tower.Contains("SemanticIconCatalog", StringComparison.Ordinal) || !tower.Contains("TowerNodeSemantic", StringComparison.Ordinal))
            failures.Add("Tower screen controller does not bind route-node semantics through the shared catalog");
        if (tower.Contains("icon: _riskIcon", StringComparison.Ordinal))
            failures.Add("tower node identity still uses risk.svg as its primary icon");
        var heroes = Read("res://src/UI/HeroSelectScreen.cs");
        if (heroes.Contains("? $\"生命 {definition.MaxHealth", StringComparison.Ordinal))
            failures.Add("hero selection footer still duplicates health and damage");
        var portrait = Read("res://src/UI/UnitPortrait.cs");
        if (!portrait.Contains("AnimatedSprite2D", StringComparison.Ordinal) || !portrait.Contains("IsVisibleInTree", StringComparison.Ordinal))
            failures.Add("UnitPortrait lacks independent idle playback and visibility pause handling");
    }

    private async System.Threading.Tasks.Task VerifyRuntimeAsync(List<string> failures)
    {
        var catalog = GD.Load<SemanticIconCatalog>(SemanticIcons.CatalogPath);
        var report = catalog.Validate();
        if (report.HasCoreErrors || catalog.Entries.Count != 39)
            failures.Add($"semantic catalog validation/count failed: entries={catalog.Entries.Count}, errors={string.Join(',', report.CoreErrors)}");
        if (!catalog.TryResolve(SemanticIconKeys.TacticalPoint, out var tacticalPoint) ||
            tacticalPoint.Icon is null || tacticalPoint.PresentationRole != "TacticalPointValue")
            failures.Add("tactical points do not own a distinct semantic icon and presentation role");
        VerifyCategory(catalog, "role", new[] { "vanguard", "fighter", "ranged", "support", "assassin", "summoner", "artillery", "boss" }, failures);
        VerifyCategory(catalog, "faction", new[] { "order", "desert", "undead", "beast", "machine", "frost", "neutral", "enemy" }, failures);
        VerifyCategory(catalog, "tower", new[] { "combat", "elite", "recruitment", "shop", "event", "rest", "boss" }, failures);
        var invalid = new SemanticIconCatalog
        {
            Entries =
            [
                new SemanticIconEntry { Key = new StringName(), Icon = null },
                new SemanticIconEntry { Key = "health", Icon = catalog.ResolveIcon(SemanticIconKeys.Health) },
                new SemanticIconEntry { Key = "health", Icon = catalog.ResolveIcon(SemanticIconKeys.Health) }
            ]
        };
        if (invalid.Validate().CoreErrors.Count < 3) failures.Add("catalog does not reject blank, missing-texture, and duplicate entries");
        var incomplete = new SemanticIconCatalog();
        foreach (var entry in catalog.Entries)
            if (entry.Key != SemanticIconKeys.Reach) incomplete.Entries.Add(entry);
        if (!incomplete.Validate().CoreErrors.Any(error => error.Contains("missing required key 'reach'", StringComparison.Ordinal)))
            failures.Add("catalog validation accepts a missing confirmed semantic");

        var unitCard = GD.Load<PackedScene>("res://scenes/ui/components/UnitChoiceCard.tscn").Instantiate<UnitChoiceCard>();
        unitCard.Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres");
        AddChild(unitCard);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var abyss = GD.Load<UnitDefinition>("res://content/definitions/soldiers/soldier_abyss_crawler.tres");
        unitCard.Bind(abyss.Id, abyss, catalog.ResolveIcon(SemanticIconKeys.Melee), abyss.Description, "加入军团");
        var badges = unitCard.FindChildren("*", "", true, false).OfType<TraitBadge>().ToArray();
        var stats = unitCard.FindChildren("*", "", true, false).OfType<StatBlock>().ToArray();
        var keys = badges.Select(badge => badge.SemanticKey.ToString()).Concat(stats.Select(stat => stat.SemanticKey.ToString())).ToArray();
        if (!keys.Contains("faction.undead") || !keys.Contains("faction.beast") || keys.Count(key => key == "health") != 1 || keys.Count(key => key == "damage") != 1)
            failures.Add("unit card lost multi-trait semantics or duplicated core stats");
        foreach (var badge in badges)
        {
            var icon = badge.GetNode<TextureRect>("%TraitIcon");
            var label = badge.GetNode<Label>("%TraitLabel");
            if (icon.Texture is null || !icon.Modulate.IsEqualApprox(label.GetThemeColor("font_color")))
                failures.Add($"trait badge '{badge.SemanticKey}' did not resolve and tint its catalog icon");
        }
        foreach (var stat in stats)
        {
            var icon = stat.GetNode<TextureRect>("%StatIcon");
            var value = stat.GetNode<Label>("%StatValue");
            if (icon.Texture is null || !icon.Modulate.IsEqualApprox(value.GetThemeColor("font_color")))
                failures.Add($"stat block '{stat.SemanticKey}' did not resolve and tint its catalog icon");
        }
        var chosenCount = 0;
        var chosenId = string.Empty;
        unitCard.ConnectChosen(id => { chosenCount++; chosenId = id; });
        unitCard.EmitSignal(BaseButton.SignalName.Pressed);
        if (chosenCount != 1 || chosenId != abyss.Id) failures.Add("typed unit choice activation changed");

        foreach (var scenePath in new[]
                 {
                     "res://scenes/ui/components/ChoiceCard.tscn",
                     "res://scenes/ui/components/DeploymentUnitCard.tscn",
                     "res://scenes/ui/components/ArmyDrawerRow.tscn",
                     "res://scenes/ui/components/BattleReportLeaderboardRow.tscn",
                     "res://scenes/ui/components/BattleReportUnitDetail.tscn",
                     "res://scenes/ui/components/SelectedUnitPanel.tscn"
                 })
        {
            var node = GD.Load<PackedScene>(scenePath).Instantiate();
            AddChild(node);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            foreach (var chip in node.FindChildren("*", "", true, false).OfType<SemanticChip>())
                if (chip.Catalog.GetInstanceId() != catalog.GetInstanceId()) failures.Add($"{scenePath} does not share the catalog resource identity");
            foreach (var badge in node.FindChildren("*", "", true, false).OfType<TraitBadge>())
                if (badge.Catalog.GetInstanceId() != catalog.GetInstanceId()) failures.Add($"{scenePath} trait badge does not share the catalog resource identity");
            foreach (var stat in node.FindChildren("*", "", true, false).OfType<StatBlock>())
                if (stat.Catalog.GetInstanceId() != catalog.GetInstanceId()) failures.Add($"{scenePath} stat block does not share the catalog resource identity");
            node.QueueFree();
        }

        await VerifyPortraitPlaybackAsync(failures);
        unitCard.QueueFree();
    }

    private static void VerifyCategory(SemanticIconCatalog catalog, string prefix, IEnumerable<string> names, List<string> failures)
    {
        var paths = names.Select(name => catalog.ResolveIcon($"{prefix}.{name}")?.ResourcePath ?? string.Empty).ToArray();
        if (paths.Any(string.IsNullOrWhiteSpace) || paths.Distinct(StringComparer.Ordinal).Count() != paths.Length)
            failures.Add($"{prefix} semantics do not resolve to differentiated icons");
    }

    private async System.Threading.Tasks.Task VerifyPortraitPlaybackAsync(List<string> failures)
    {
        var definition = GD.Load<UnitPortraitDefinition>("res://content/portraits/heroes/hero_banner_marshal.tres");
        if (definition.Frames is null || definition.Frames.GetFrameCount(definition.AnimationName) < 2)
        {
            failures.Add("portrait playback probe lacks a multi-frame idle source");
            return;
        }
        var host = new Control { CustomMinimumSize = new Vector2(180, 180), Size = new Vector2(180, 180) };
        var portrait = GD.Load<PackedScene>("res://scenes/ui/components/UnitPortrait.tscn").Instantiate<UnitPortrait>();
        host.AddChild(portrait);
        AddChild(host);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        portrait.Bind(definition, null);
        var sprite = portrait.GetNode<AnimatedSprite2D>("%PortraitSprite");
        var sourceCount = definition.Frames.GetFrameCount(definition.AnimationName);
        var startFrame = sprite.Frame;
        await ToSignal(GetTree().CreateTimer(.35), SceneTreeTimer.SignalName.Timeout);
        if (!sprite.IsPlaying() || (sprite.Frame == startFrame && sprite.FrameProgress <= .01f))
            failures.Add("visible portrait idle did not advance");
        host.Visible = false;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var pausedFrame = sprite.Frame;
        var pausedProgress = sprite.FrameProgress;
        await ToSignal(GetTree().CreateTimer(.25), SceneTreeTimer.SignalName.Timeout);
        if (sprite.IsPlaying() || sprite.Frame != pausedFrame || Math.Abs(sprite.FrameProgress - pausedProgress) > .001f)
            failures.Add("hidden portrait did not pause at its current frame");
        host.Visible = true;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!sprite.IsPlaying()) failures.Add("shown portrait did not resume");
        if (!ReferenceEquals(sprite.SpriteFrames, definition.Frames) || definition.Frames.GetFrameCount(definition.AnimationName) != sourceCount)
            failures.Add("UI portrait duplicated or mutated shared SpriteFrames");
        host.QueueFree();
    }

    private static void Expect(List<string> failures, string path)
    {
        if (!ResourceLoader.Exists(path) && !FileAccess.FileExists(path)) failures.Add($"missing {path}");
    }

    private static string Read(string path) => FileAccess.FileExists(path) ? FileAccess.GetFileAsString(path) : string.Empty;
}
