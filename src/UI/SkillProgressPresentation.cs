using System;
using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.UI;

// Both HUD surfaces bind the same battle projection; there is no UI clock or resource cache.
public static class SkillProgressPresentation
{
    public static void Bind(ProgressBar bar, BattleSkillProgress progress)
    {
        bar.ThemeTypeVariation = "Skill" + progress.Kind;
        bar.MaxValue = Math.Max(1, progress.Maximum);
        bar.Value = progress.Current;
    }

    public static string Marker(BattleSkillProgress progress) => progress.Kind switch
    {
        SkillResourceKind.Mana => "法",
        SkillResourceKind.Grit => "怒",
        SkillResourceKind.Timer => "时",
        SkillResourceKind.Counter => "计",
        SkillResourceKind.Passive => "常",
        _ => "触"
    };

    public static string StateText(SkillProgressState state) => state switch
    {
        SkillProgressState.Ready => "待施放",
        SkillProgressState.Casting => "施放中",
        SkillProgressState.Queued => "已排队",
        SkillProgressState.Recovering => "收招中",
        SkillProgressState.Waiting => "待触发",
        SkillProgressState.Active => "生效中",
        SkillProgressState.Spent => "已用尽",
        SkillProgressState.Defeated => "已阵亡",
        SkillProgressState.Ended => "已结束",
        _ => "积累中"
    };

    public static string Summary(BattleSkillProgress progress)
    {
        var value = progress.Kind switch
        {
            SkillResourceKind.Mana or SkillResourceKind.Grit or SkillResourceKind.Counter =>
                $" {progress.Current:0.#}/{progress.Maximum:0.#}",
            SkillResourceKind.Timer => $" {Math.Max(0, progress.Maximum - progress.Current) * BattleTiming.TickSeconds:0.0} 秒",
            _ => ""
        };
        var state = progress.Kind == SkillResourceKind.Timer && progress.State == SkillProgressState.Building
            ? "计时中" : StateText(progress.State);
        return $"{progress.DisplayName} · {progress.ResourceName}{value} · {state}";
    }
}
