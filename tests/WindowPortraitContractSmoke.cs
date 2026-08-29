using System;
using System.Collections.Generic;
using Godot;

public partial class WindowPortraitContractSmoke : Node
{
    public override void _Ready()
    {
        var failures = new List<string>();
        VerifyWindowBaseline(failures);
        VerifyUnitChoiceBaseline(failures);
        if (failures.Count > 0)
        {
            GD.PrintErr("WINDOW_PORTRAIT_CONTRACT_FAILED: " + string.Join(" | ", failures));
            GetTree().Quit(1);
            return;
        }

        GD.Print("WINDOW_PORTRAIT_CONTRACT_OK window=1600x900,windowed,resizable portraits=authored-unit-card");
        GetTree().Quit();
    }

    private static void VerifyWindowBaseline(List<string> failures)
    {
        ExpectProjectSetting(failures, "display/window/size/viewport_width", 1600L);
        ExpectProjectSetting(failures, "display/window/size/viewport_height", 900L);
        ExpectProjectSetting(failures, "display/window/size/window_width_override", 1600L);
        ExpectProjectSetting(failures, "display/window/size/window_height_override", 900L);
        ExpectProjectSetting(failures, "display/window/size/mode", 0L);
        ExpectProjectSetting(failures, "display/window/size/resizable", true);
        ExpectProjectSettingString(failures, "display/window/stretch/mode", "canvas_items");
        ExpectProjectSettingString(failures, "display/window/stretch/aspect", "expand");
    }

    private static void VerifyUnitChoiceBaseline(List<string> failures)
    {
        const string unitCardPath = "res://scenes/ui/components/UnitChoiceCard.tscn";
        const string heroTilePath = "res://scenes/ui/components/HeroLibraryTile.tscn";
        const string heroDetailPath = "res://scenes/ui/components/HeroDetailPanel.tscn";
        const string portraitPath = "res://scenes/ui/components/UnitPortrait.tscn";
        if (!ResourceLoader.Exists(unitCardPath) || !ResourceLoader.Exists(heroTilePath) || !ResourceLoader.Exists(heroDetailPath) || !ResourceLoader.Exists(portraitPath))
        {
            var generalCardSource = FileAccess.GetFileAsString("res://scenes/ui/components/ChoiceCard.tscn");
            var slot = generalCardSource.Contains("custom_minimum_size = Vector2(58, 58)", StringComparison.Ordinal)
                ? "58x58"
                : "unknown";
            failures.Add($"unit-specific portrait/card scenes are absent; general ChoiceCard portrait slot is {slot}");
        }
        var generalCardSourceAfter = FileAccess.GetFileAsString("res://scenes/ui/components/ChoiceCard.tscn");
        if (!generalCardSourceAfter.Contains("custom_minimum_size = Vector2(250, 112)", StringComparison.Ordinal) ||
            !generalCardSourceAfter.Contains("custom_minimum_size = Vector2(58, 58)", StringComparison.Ordinal))
            failures.Add("general ChoiceCard size or 58x58 icon contract changed");
        var unitCardSource = FileAccess.GetFileAsString(unitCardPath);
        if (!unitCardSource.Contains("custom_minimum_size = Vector2(0, 172)", StringComparison.Ordinal) ||
            !unitCardSource.Contains("custom_minimum_size = Vector2(106, 106)", StringComparison.Ordinal))
            failures.Add("UnitChoiceCard is missing the authored compact card / 106px portrait hierarchy");
        var heroTileSource = FileAccess.GetFileAsString(heroTilePath);
        var heroDetailSource = FileAccess.GetFileAsString(heroDetailPath);
        if (!heroTileSource.Contains("custom_minimum_size = Vector2(82, 82)", StringComparison.Ordinal) ||
            !heroDetailSource.Contains("custom_minimum_size = Vector2(142, 142)", StringComparison.Ordinal) ||
            !heroDetailSource.Contains("HeroAbilityPanel.tscn", StringComparison.Ordinal))
            failures.Add("hero library/detail does not author distinct compact and focused portrait hierarchies");
    }

    private static void ExpectProjectSetting(List<string> failures, string key, Variant expected)
    {
        var actual = ProjectSettings.GetSetting(key);
        if (!actual.Equals(expected)) failures.Add($"{key} expected {expected}, got {actual}");
    }

    private static void ExpectProjectSettingString(List<string> failures, string key, string expected)
    {
        var actual = ProjectSettings.GetSetting(key).AsString();
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            failures.Add($"{key} expected {expected}, got {actual}");
    }
}
