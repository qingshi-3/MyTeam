using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class RealmThemeContractSmoke : Node
{
    private const string ThemePath = "res://content/ui/RealmTheme.tres";

    private static readonly string[] Screens =
    [
        "MainMenuScreen", "HeroSelectScreen", "TowerScreen", "RecruitmentScreen",
        "DeploymentScreen", "BattleScreen", "BattleReportScreen", "RewardScreen",
        "ShopScreen", "EventScreen", "RestScreen", "ResultScreen", "SettingsScreen"
    ];

    public override void _Ready()
    {
        var failures = new List<string>();
        try
        {
            VerifyTheme(failures);
            VerifyRootAndScreens(failures);
            VerifyComponentsAndRuntime(failures);
            VerifyRemovedOwnership(failures);
        }
        catch (Exception exception)
        {
            failures.Add(exception.GetType().Name + ": " + exception.Message);
        }

        if (failures.Count > 0)
        {
            GD.PrintErr("REALM_THEME_CONTRACT_FAILED: " + string.Join(" | ", failures));
            GetTree().Quit(1);
            return;
        }

        GD.Print("REALM_THEME_CONTRACT_OK authority=local screens=13 core=donor semantics=preserved background=flat cleanup=complete");
        GetTree().Quit();
    }

    private static void VerifyTheme(List<string> failures)
    {
        Expect(ThemePath, failures);
        if (!ResourceLoader.Exists(ThemePath)) return;
        var source = Read(ThemePath);
        var theme = GD.Load<Theme>(ThemePath);
        if (theme is null)
        {
            failures.Add("project-local RealmTheme does not load as Theme");
            return;
        }

        foreach (var role in new[]
                 {
                     "TopBarPanel", "ContextSidebarPanel", "ModalPanel", "NotificationPanel",
                     "TitleLabel", "SectionTitleLabel", "PrimaryButton", "SecondaryButton",
                     "DangerButton", "CompactButton"
                 })
            if (!theme.GetTypeList().Contains(role)) failures.Add($"Realm donor role missing: {role}");

        foreach (var role in new[]
                 {
                     "HealthValue", "DamageValue", "ManaValue", "ShieldValue", "HealingValue",
                     "GoldValue", "RangeValue", "DangerValue", "RiskValue", "HeroIdentity",
                     "PlayerLabel", "EnemyLabel", "SecondaryLabel", "ReportTabButton",
                     "ReportPlayerSummarySurface", "ReportEnemySummarySurface", "ReportUnitCardSurface",
                     "ReportHeroCardSurface", "ReportDefeatedCardSurface", "ReportPrimaryMetricSurface",
                     "ReportHealthBar", "ReportContributionBar", "GridCellButton", "SelectedButton",
                     "BadgePanel", "ManaCostSurface", "GoldCostSurface"
                 })
            if (!theme.GetTypeList().Contains(role)) failures.Add($"project semantic/control role missing: {role}");

        foreach (var donorProof in new[]
                 {
                     "StyleBox_panel", "StyleBox_top_bar", "StyleBox_context_sidebar", "StyleBox_modal",
                     "StyleBox_notification", "StyleBox_button_normal", "StyleBox_primary_normal",
                     "StyleBox_danger_normal", "StyleBox_compact_normal",
                     "bg_color = Color(0.035, 0.052, 0.066, 0.94)",
                     "border_color = Color(0.52, 0.43, 0.22, 1)"
                 })
            Require(source, donorProof, $"Realm donor core proof missing: {donorProof}", failures);

        if (source.Contains("StyleBoxTexture", StringComparison.Ordinal) ||
            source.Contains("ExtResource", StringComparison.Ordinal) ||
            source.Contains("Texture2D", StringComparison.Ordinal))
            failures.Add("RealmTheme is not self-contained StyleBoxFlat presentation");

        foreach (var controlProof in new[]
                 {
                     "VScrollBar/styles/scroll", "VScrollBar/styles/grabber",
                     "HSlider/styles/slider", "HSlider/styles/grabber_area",
                     "ReportHealthBar/styles/background", "ReportHealthBar/styles/fill",
                     "ReportContributionBar/styles/background", "ReportContributionBar/styles/fill"
                 })
            Require(source, controlProof, $"RealmTheme missing control treatment: {controlProof}", failures);
    }

    private static void VerifyRootAndScreens(List<string> failures)
    {
        var root = Read("res://scenes/app/GameRoot.tscn");
        Require(root, ThemePath, "GameRoot does not assign the local RealmTheme", failures);
        Require(root, "[node name=\"Background\" type=\"ColorRect\"", "GameRoot background is not an authored ColorRect", failures);
        Require(root, "color = Color(", "GameRoot flat background has no authored color", failures);
        if (root.Contains("TextureRect", StringComparison.Ordinal) || root.Contains("texture_repeat", StringComparison.Ordinal))
            failures.Add("GameRoot still owns a textured/tiled background");

        foreach (var name in Screens)
        {
            var path = $"res://scenes/ui/{name}.tscn";
            Expect(path, failures);
            var packed = ResourceLoader.Exists(path) ? GD.Load<PackedScene>(path) : null;
            if (packed is null) failures.Add($"screen does not statically load: {name}");
            var source = Read(path);
            foreach (var role in OldVisualRoles())
                if (source.Contains(role, StringComparison.Ordinal)) failures.Add($"{name} retains removed visual role {role}");
        }
    }

    private static void VerifyComponentsAndRuntime(List<string> failures)
    {
        var mappings = new Dictionary<string, string>
        {
            ["ChoiceCard.tscn"] = "SecondaryButton",
            ["UnitChoiceCard.tscn"] = "SecondaryButton",
            ["HeroLibraryTile.tscn"] = "CompactButton",
            ["DeploymentUnitCard.tscn"] = "SecondaryButton",
            ["DeploymentCell.tscn"] = "GridCellButton",
            ["TacticalCommandHud.tscn"] = "ContextSidebarPanel",
            ["SelectedUnitPanel.tscn"] = "ContextSidebarPanel",
            ["TraitBadge.tscn"] = "BadgePanel",
            ["ResourceCostBadge.tscn"] = "ManaCostSurface"
        };
        foreach (var (file, role) in mappings)
        {
            var path = "res://scenes/ui/components/" + file;
            var source = Read(path);
            Require(source, $"theme_type_variation = &\"{role}\"", $"{file} is not mapped to Realm role {role}", failures);
            foreach (var oldRole in OldVisualRoles())
                if (source.Contains(oldRole, StringComparison.Ordinal)) failures.Add($"{file} retains removed visual role {oldRole}");
        }

        var tile = Read("res://src/UI/HeroLibraryTile.cs");
        Require(tile, "previewed ? \"SelectedButton\" : \"CompactButton\"", "HeroLibraryTile does not restore Realm state roles", failures);

        foreach (var path in EnumerateFiles("res://src", ".cs"))
        {
            var source = Read(path);
            foreach (var forbidden in new[] { "new Theme(", "new StyleBox", "new StyleBoxTexture", "Theme.New(" })
                if (source.Contains(forbidden, StringComparison.Ordinal))
                    failures.Add($"runtime presentation resource construction in {path}: {forbidden}");
        }
    }

    private static void VerifyRemovedOwnership(List<string> failures)
    {
        foreach (var path in new[]
                 {
                     "res://assets/ui/" + "pixel_" + "fantasy",
                     "res://content/ui/" + "pixel_" + "fantasy",
                     "res://content/ui/game_" + "theme.tres",
                     "res://scenes/ui/dev/Pixel" + "FantasyUiPreview.tscn",
                     "res://src/UI/Dev/Pixel" + "FantasyUiPreview.cs",
                     "res://tools/process_" + "pixel_fantasy_ui.py",
                     "res://tools/process_" + "pixel_fantasy_live.py"
                 })
            if (FileAccess.FileExists(path) || DirAccess.DirExistsAbsolute(ProjectSettings.GlobalizePath(path)))
                failures.Add("confirmed obsolete ownership remains: " + path);

        var forbiddenPath = "res://" + "assets/ui/" + "pixel_" + "fantasy";
        var forbiddenContent = "res://" + "content/ui/" + "pixel_" + "fantasy";
        var externalDonor = "D:" + "\\" + "godot" + "\\" + "realm";
        foreach (var root in new[] { "res://content", "res://scenes", "res://src", "res://tests", "res://system-design", "res://docs" })
        foreach (var path in EnumerateFiles(root, null))
        {
            if (path.EndsWith("RealmThemeContractSmoke.cs", StringComparison.Ordinal)) continue;
            var source = Read(path);
            if (source.Contains(forbiddenPath, StringComparison.OrdinalIgnoreCase) ||
                source.Contains(forbiddenContent, StringComparison.OrdinalIgnoreCase) ||
                source.Contains(externalDonor, StringComparison.OrdinalIgnoreCase))
                failures.Add("removed/external resource reference remains in " + path);
            foreach (var role in OldVisualRoles())
                if (source.Contains(role, StringComparison.Ordinal)) failures.Add($"removed visual role {role} remains in {path}");
        }
    }

    private static IEnumerable<string> OldVisualRoles()
    {
        yield return "Live" + "Screen";
        yield return "Live" + "General";
        yield return "Live" + "Compact";
        yield return "Live" + "Horizontal";
        yield return "Live" + "Card";
        yield return "Live" + "Slot";
        yield return "Live" + "Badge";
        yield return "Live" + "Title";
        yield return "Quiet" + "Structural";
        yield return "Quiet" + "Section";
        yield return "Quiet" + "Row";
        yield return "Quiet" + "Micro";
        yield return "Quiet" + "Choice";
        yield return "Quiet" + "Roster";
        yield return "Quiet" + "Identity";
        yield return "Tactical" + "Cell";
        yield return "Primary" + "Decision";
        yield return "Secondary" + "Action";
        yield return "Stat" + "Surface";
        yield return "Trait" + "Surface";
        yield return "Ability" + "Surface";
    }

    private static IEnumerable<string> EnumerateFiles(string directoryPath, string? suffix)
    {
        using var directory = DirAccess.Open(directoryPath);
        if (directory is null) yield break;
        directory.ListDirBegin();
        while (true)
        {
            var name = directory.GetNext();
            if (string.IsNullOrEmpty(name)) break;
            if (name is "." or "..") continue;
            var path = directoryPath.TrimEnd('/') + "/" + name;
            if (directory.CurrentIsDir())
            {
                foreach (var nested in EnumerateFiles(path, suffix)) yield return nested;
            }
            else if (suffix is null || name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                yield return path;
            }
        }
        directory.ListDirEnd();
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
