using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.UI;

public partial class VisualHierarchyContractSmoke : Node
{
    public override async void _Ready()
    {
        var failures = new List<string>();
        foreach (var path in new[]
                 {
                     "res://scenes/ui/components/StatBlock.tscn",
                     "res://scenes/ui/components/TraitBadge.tscn",
                     "res://scenes/ui/components/ResourceCostBadge.tscn",
                     "res://scenes/ui/components/HeroAbilityPanel.tscn",
                     "res://scenes/ui/components/HeroLibraryTile.tscn",
                     "res://scenes/ui/components/HeroDetailPanel.tscn",
                     "res://src/UI/StatBlock.cs",
                     "res://src/UI/TraitBadge.cs",
                     "res://src/UI/ResourceCostBadge.cs",
                     "res://src/UI/HeroAbilityPanel.cs",
                     "res://src/UI/HeroLibraryTile.cs",
                     "res://src/UI/HeroDetailPanel.cs",
                     "res://src/UI/HeroSelectScreen.cs"
                 })
            Expect(path, failures);

        var heroScreen = Read("res://scenes/ui/HeroSelectScreen.tscn");
        Require(heroScreen, "HeroLibrary", "hero selection lacks an authored compact library", failures);
        Require(heroScreen, "HeroDetailPanel", "hero selection lacks an authored detail panel", failures);
        Require(heroScreen, "HeroLibraryTile.tscn", "hero selection does not author a library-tile template", failures);

        var heroController = Read("res://src/UI/HeroSelectScreen.cs");
        Require(heroController, "HeroSelectionViewModel", "hero selection is not bound through a typed view model", failures);
        if (heroController.Contains("_heroUnitChoiceCard", StringComparison.Ordinal))
            failures.Add("hero selection still routes through verbose HeroUnitChoiceCard rows");
        if (heroController.Contains("command.Description}", StringComparison.Ordinal) && heroController.Contains(" MP", StringComparison.Ordinal))
            failures.Add("hero screen still embeds structured command cost in hero prose");

        var unitCard = Read("res://scenes/ui/components/UnitChoiceCard.tscn");
        Require(unitCard, "TraitBadge.tscn", "recruitment lacks authored trait badges", failures);
        Require(unitCard, "StatBlock.tscn", "recruitment lacks authored stat blocks", failures);

        var hud = Read("res://scenes/ui/components/TacticalCommandHud.tscn") +
                  Read("res://scenes/ui/components/TacticalCommandSlot.tscn");
        Require(hud, "ResourceCostBadge.tscn", "command HUD lacks structured resource cost badges", failures);
        if (hud.Contains("name=\"CommandCost\" type=\"Label\"", StringComparison.Ordinal))
            failures.Add("command HUD still owns a prose cost label");
        var armyRow = Read("res://scenes/ui/components/ArmyDrawerRow.tscn");
        Require(armyRow, "ResourceCostBadge.tscn", "Army hero detail lacks structured resource cost badges", failures);
        var armyModels = Read("res://src/UI/ArmyOverviewModels.cs");
        if (!armyModels.Contains("TacticalCommands", StringComparison.Ordinal) ||
            armyModels.Contains("消耗 {command.ManaCost} MP", StringComparison.Ordinal))
            failures.Add("Army overview lacks an independent tactical-command section");

        var theme = Read("res://content/ui/RealmTheme.tres");
        foreach (var variation in new[]
                 {
                     "HealthValue", "DamageValue", "ManaValue", "ShieldValue", "HealingValue",
                     "GoldValue", "RangeValue", "DangerValue", "RiskValue", "HeroIdentity"
                 })
            Require(theme, variation, $"shared Theme lacks semantic variation {variation}", failures);

        var allPresentationSources = heroController + Read("res://src/UI/TacticalCommandHud.cs") + Read("res://src/UI/TacticalCommandSlot.cs");
        if (allPresentationSources.Contains("Regex", StringComparison.Ordinal) || allPresentationSources.Contains("hero_\"", StringComparison.Ordinal))
            failures.Add("presentation introduces prose parsing or concrete hero-id dispatch");

        await VerifyRuntimeAsync(failures);

        if (failures.Count > 0)
        {
            GD.PrintErr("VISUAL_HIERARCHY_CONTRACT_FAILED: " + string.Join(" | ", failures));
            GetTree().Quit(1);
            return;
        }

        GD.Print("VISUAL_HIERARCHY_CONTRACT_OK master-detail=responsive components=4 costs=structured palette=semantic");
        GetTree().Quit();
    }

    private static void Expect(string path, List<string> failures)
    {
        if (!ResourceLoader.Exists(path) && !FileAccess.FileExists(path)) failures.Add("missing " + path);
    }

    private static void Require(string source, string token, string failure, List<string> failures)
    {
        if (!source.Contains(token, StringComparison.Ordinal)) failures.Add(failure);
    }

    private static string Read(string path) => FileAccess.FileExists(path) ? FileAccess.GetFileAsString(path) : string.Empty;

    private async Task VerifyRuntimeAsync(List<string> failures)
    {
        var screen = GD.Load<PackedScene>("res://scenes/ui/HeroSelectScreen.tscn").Instantiate<HeroSelectScreen>();
        screen.Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres");
        AddChild(screen);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var commander = GD.Load<UnitDefinition>("res://content/definitions/heroes/hero_banner_marshal.tres");
        var regent = GD.Load<UnitDefinition>("res://content/definitions/heroes/hero_bone_regent.tres");
        var models = new[]
        {
            new HeroSelectionViewModel(commander.Id, commander, true, "军团规则甲", "规则正文甲"),
            new HeroSelectionViewModel(regent.Id, regent, false, "军团规则乙", "规则正文乙")
        };
        var chosenCount = 0;
        var chosenId = string.Empty;
        screen.HeroChosen += stableId => { chosenCount++; chosenId = stableId; };
        screen.Bind(models);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var grid = screen.GetNode<GridContainer>("%HeroLibrary");
        var tiles = grid.GetChildren().OfType<HeroLibraryTile>().ToArray();
        if (tiles.Length != 2 || screen.PreviewStableId != commander.Id)
            failures.Add("hero library did not bind typed tiles and initial preview");
        tiles.Single(tile => tile.StableId == regent.Id).EmitSignal(BaseButton.SignalName.Pressed);
        if (screen.PreviewStableId != regent.Id || chosenCount != 0)
            failures.Add("tile activation did not remain preview-only");
        var deploy = screen.GetNode<Button>("Margin/Layout/Content/HeroDetailPanel/Layout/DeployButton");
        if (!deploy.Disabled || deploy.Text != "尚未解锁")
            failures.Add("locked preview does not retain explicit disabled action state");
        screen.Preview(commander.Id);
        deploy.EmitSignal(BaseButton.SignalName.Pressed);
        if (chosenCount != 1 || chosenId != commander.Id)
            failures.Add("detail primary action did not emit the previewed stable id exactly once");
        if (screen.GetNodeOrNull<Control>("Margin/Layout/Content/HeroDetailPanel/Layout/DetailScroll/Content/HeroAbilityPanel") is not null)
            failures.Add("hero detail still presents a hero-owned tactical command");
        screen.Preview(regent.Id);
        screen.Size = new Vector2(1280, 720);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (grid.Columns != 2) failures.Add("hero library did not reduce columns at 1280 width");
        screen.Size = new Vector2(1600, 900);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (grid.Columns != 3) failures.Add("hero library did not expand columns at 1600 width");
        screen.QueueFree();
    }
}
