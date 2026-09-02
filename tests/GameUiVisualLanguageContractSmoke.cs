using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.UI;

public partial class GameUiVisualLanguageContractSmoke : Node
{
    public override async void _Ready()
    {
        var failures = new List<string>();
        try
        {
            VerifyAuthoredLanguage(failures);
            await VerifyDeploymentRuntime(failures);
        }
        catch (Exception exception)
        {
            failures.Add(exception.GetType().Name + ": " + exception.Message);
        }

        if (failures.Count > 0)
        {
            GD.PrintErr("GAME_UI_VISUAL_LANGUAGE_CONTRACT_FAILED: " + string.Join(" | ", failures));
            GetTree().Quit(1);
            return;
        }

        GD.Print("GAME_UI_VISUAL_LANGUAGE_CONTRACT_OK deployment=portrait+semantic states=9 board-markers=icons army=resource-strip items=distinct battle=status-strip prose=layered");
        GetTree().Quit();
    }

    private static void VerifyAuthoredLanguage(List<string> failures)
    {
        var theme = Read("res://content/ui/RealmTheme.tres");
        foreach (var role in new[]
                 {
                     "DeploymentCellSelected", "DeploymentCellLegal", "DeploymentCellIllegal",
                     "DeploymentCellSwap", "DeploymentCellDrag", "DeploymentCellSuccess",
                     "DeploymentCellFailure", "FeedbackSuccess", "FeedbackFailure", "ItemChoiceButton"
                 })
            Require(theme, role, "RealmTheme lacks authored interaction role " + role, failures);

        var cell = Read("res://scenes/ui/components/DeploymentCell.tscn");
        foreach (var node in new[] { "UnitPortrait", "HeroBadge", "RoleBadge", "ReachBadge" })
            Require(cell, node, "deployment cell lacks authored " + node, failures);
        if (cell.Contains("可部署", StringComparison.Ordinal) || cell.Contains("1-1", StringComparison.Ordinal))
            failures.Add("deployment cell retains persistent instructional/coordinate copy");

        var marker = Read("res://scenes/ui/components/DeploymentMarker.tscn");
        Require(marker, "type=\"TextureRect\"", "deployment marker is not icon-authored", failures);
        if (marker.Contains("type=\"Label\"", StringComparison.Ordinal))
            failures.Add("deployment marker still embeds board prose");

        var readability = Read("res://scenes/components/UnitReadabilityComponent.tscn");
        if (readability.Contains("type=\"Label\"", StringComparison.Ordinal) ||
            readability.Contains("★ 英雄", StringComparison.Ordinal) || readability.Contains("text = \"近\"", StringComparison.Ordinal))
            failures.Add("battleboard identity still uses persistent hero/near/far words");

        Expect("res://scenes/ui/components/ArmyResourceStrip.tscn", failures);
        Expect("res://scenes/ui/components/ItemChoiceCard.tscn", failures);
        Expect("res://scenes/ui/components/BattleStatusStrip.tscn", failures);
        Expect("res://scenes/ui/components/OutcomeActionButton.tscn", failures);
        var army = Read("res://scenes/ui/components/ArmyOverview.tscn");
        Require(army, "ArmyResourceStrip.tscn", "Army overview still lacks a semantic resource strip", failures);
        if (army.Contains("英雄 100% · 部署", StringComparison.Ordinal))
            failures.Add("Army overview retains a sentence-style summary");
        var reward = Read("res://src/UI/RewardScreenController.cs");
        var shopController = Read("res://src/UI/ShopScreenController.cs");
        var flow = Read("res://src/App/GameFlowCoordinator.cs");
        if (!reward.Contains("Template: itemTemplate", StringComparison.Ordinal) ||
            !shopController.Contains("Template: itemTemplate", StringComparison.Ordinal) ||
            flow.Split("_presentation.ItemChoiceCard", StringSplitOptions.None).Length - 1 < 2)
            failures.Add("reward/shop do not bind the distinct authored item card template");
        var itemCard = Read("res://scenes/ui/components/ItemChoiceCard.tscn");
        var railStart = itemCard.IndexOf("[node name=\"RarityRail\"", StringComparison.Ordinal);
        var railEnd = itemCard.IndexOf("[node name=\"Margin\"", StringComparison.Ordinal);
        if (railStart < 0 || railEnd <= railStart || itemCard[railStart..railEnd].Contains("anchor_right = 1.0", StringComparison.Ordinal))
            failures.Add("item rarity rail expands across the whole card instead of remaining a narrow authored edge");
        using (var itemDirectory = DirAccess.Open("res://content/definitions/items"))
        {
            var definitions = itemDirectory?.GetFiles().Where(name => name.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
                .Select(name => GD.Load<ItemDefinition>("res://content/definitions/items/" + name)).ToArray() ?? [];
            var relics = definitions.Where(definition => definition?.ProductKind == ItemProductKind.Relic).ToArray();
            var relicIconPaths = relics.Select(definition => definition?.Icon?.ResourcePath ?? string.Empty).ToArray();
            var equipment = definitions.Where(definition => definition?.ProductKind == ItemProductKind.Equipment).ToArray();
            if (relics.Length != 12 || relicIconPaths.Any(string.IsNullOrWhiteSpace) ||
                relicIconPaths.Distinct(StringComparer.Ordinal).Count() != relics.Length)
                failures.Add("Relic definitions do not provide 12 differentiated semantic identities");
            if (equipment.Length != 2 || equipment.Any(definition => definition?.Icon is null ||
                    string.IsNullOrWhiteSpace(definition.DisplayName) || string.IsNullOrWhiteSpace(definition.Description)))
                failures.Add("both Equipment definitions must provide authored semantic identities");
        }
        var battle = Read("res://scenes/ui/BattleScreen.tscn");
        Require(battle, "BattleStatusStrip.tscn", "battle HUD remains a sentence-style status label", failures);
        var deployment = Read("res://scenes/ui/DeploymentScreen.tscn");
        if (deployment.Contains("部署区：", StringComparison.Ordinal) || deployment.Contains("选择英雄或士兵，再选择", StringComparison.Ordinal))
            failures.Add("deployment screen retains persistent instructional prose");
        var shop = Read("res://scenes/ui/ShopScreen.tscn");
        if (shop.Contains("物品是独立场景", StringComparison.Ordinal))
            failures.Add("shop exposes implementation explanation as persistent player copy");
        var eventScreen = Read("res://scenes/ui/EventScreen.tscn");
        var restScreen = Read("res://scenes/ui/RestScreen.tscn");
        if (!eventScreen.Contains("OutcomeActionButton.tscn", StringComparison.Ordinal) ||
            eventScreen.Contains("+18 金币", StringComparison.Ordinal) || eventScreen.Contains("英雄 -25%", StringComparison.Ordinal))
            failures.Add("event choices are not authored semantic outcome clusters");
        if (!restScreen.Contains("OutcomeActionButton.tscn", StringComparison.Ordinal) || restScreen.Contains("+8 金币", StringComparison.Ordinal))
            failures.Add("rest choices are not authored semantic outcome clusters");
        if (Read("res://scenes/ui/HeroSelectScreen.tscn").Contains("英雄决定军团规则", StringComparison.Ordinal))
            failures.Add("hero selection repeats master-detail information as persistent prose");
    }

    private async Task VerifyDeploymentRuntime(List<string> failures)
    {
        var host = new Control { Size = new Vector2(240, 160) };
        AddChild(host);
        var cell = GD.Load<PackedScene>("res://scenes/ui/components/DeploymentCell.tscn").Instantiate<DeploymentCell>();
        cell.Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres");
        host.AddChild(cell);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var hero = GD.Load<UnitDefinition>("res://content/definitions/heroes/hero_banner_marshal.tres");
        cell.Bind(new Vector2I(0, 0), "hero", hero.DisplayName, true, true, true, FloorCellPreview.Normal,
            hero.Portrait, hero.Role, hero.AttackRange, true);
        if (cell.Text.Length > 0 || cell.ThemeTypeVariation != "DeploymentCellSelected" ||
            !cell.GetNode<UnitPortrait>("%UnitPortrait").Visible || !cell.GetNode<TextureRect>("%HeroBadge").Visible ||
            cell.GetNode<TextureRect>("%RoleBadge").Texture is null || cell.GetNode<TextureRect>("%ReachBadge").Texture is null)
            failures.Add("occupied deployment cell did not bind portrait, hero redundancy, responsibility, reach, and selected state");
        cell.Bind(new Vector2I(1, 1), string.Empty, string.Empty, false, false, true, FloorCellPreview.Normal,
            hasSelection: true);
        if (cell.Text.Length > 0 || cell.GetNode<UnitPortrait>("%UnitPortrait").Visible || cell.ThemeTypeVariation != "DeploymentCellLegal")
            failures.Add("empty legal deployment cell did not remain copy-free with an explicit legal state");
        host.QueueFree();
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
}
