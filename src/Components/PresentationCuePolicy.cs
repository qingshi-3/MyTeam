namespace TowerAutobattler.Components;

public static class PresentationCuePolicy
{
    public static bool IsAction(string cue) => cue is "skill_cast" or "attack" or "hit";

    public static int Priority(string cue) => cue switch
    {
        "defeated" => 500,
        "skill_cast" => 410,
        "attack" => 400,
        "hit" => 300,
        "move" => 200,
        "idle" => 100,
        _ => 0
    };

    public static string Prefer(string current, string candidate) =>
        Priority(candidate) > Priority(current) ? candidate : current;
}
