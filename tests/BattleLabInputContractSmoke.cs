using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public partial class BattleLabInputContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = Run();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private static int Run()
    {
        try
        {
            var requirements = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["scenes/ui/BattleLabScreen.tscn"] =
                ["BattleLabScreen", "PlayerLibrary", "EnemyLibrary", "Battlefield", "ModeBanner",
                 "PresetChoice", "SetPrimaryButton"],
                ["src/UI/BattleLabScreenController.cs"] =
                ["Viewport", "_Input", "InputEventMouseButton", "自由实验配置", "BattleLabPlacementPolicy"],
                ["scenes/ui/MainMenuScreen.tscn"] = ["BattleLabButton", "战斗实验室"],
                ["src/App/AppScreenHost.cs"] = ["BattleLab", "AppScreenId"],
                ["src/App/GameFlowCoordinator.cs"] =
                ["BattleLabRequested", "ShowBattleLab", "StartBattleLabBattle", "ReturnToBattleLabConfiguration"]
            };
            var missing = new List<string>();
            foreach (var (path, tokens) in requirements)
            {
                var full = ProjectSettings.GlobalizePath("res://" + path);
                var text = File.Exists(full) ? File.ReadAllText(full) : string.Empty;
                missing.AddRange(tokens.Where(token => !text.Contains(token, StringComparison.Ordinal))
                    .Select(token => path + ":" + token));
            }

            if (missing.Count > 0)
            {
                foreach (var gap in missing) GD.PrintErr("BATTLE_LAB_INPUT_RED_GAP " + gap);
                GD.PrintErr($"BATTLE_LAB_INPUT_RED_EXPECTED missing={missing.Count}");
                return 1;
            }

            GD.Print("BATTLE_LAB_INPUT_CONTRACT_OK contract=authored-source-surface " +
                     "entry=routing-token placement=input-handler-token modes=formal-free " +
                     "equipment=hero relics=team cleanup=source-contract");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("BATTLE_LAB_INPUT_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }
}
