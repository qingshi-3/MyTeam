using Godot;

namespace TowerAutobattler.Vfx;

// Preview-only examples of the external facts normally supplied by combat.
// They never modify definitions or feed back into simulation.
public static class VfxPreviewSample
{
    public static bool Moving(string id) => id is "projectile" or "piercing_arrow" or "acid_spit" or "mechanical_hook" or "returning_blade" or "trample_rush";
    public static bool Phased(string id) => id is "cone_breath" or "ground_fissure" or "rock_raise" or "position_swap" or "shell_break" or "claw_swipe";
    public static bool SelfCentered(VfxDefinition d) => d.AtSource || d.StableId is "cleave" or "piercing_thrust" or "whirlwind" or "taunt_call";
    public static bool ShowTarget(VfxDefinition d) => !SelfCentered(d) && d.StableId is not "rock_raise" and not "teleport";
    public static bool Directional(VfxDefinition d) => d.StretchBetween || Moving(d.StableId) || Phased(d.StableId) && d.StableId is not "shell_break" ||
        d.FlightDuration > 0 || d.StableId is "life_drain" or "arc_lightning" or "teleport" or "piercing_beam" or "piercing_beam_charge" or "piercing_arrow_charge" or "grit_charge" or "grit_punch" or "trample_warning";
    public static float Radius(VfxDefinition d) => d.UsesRadius ? d.ReferenceRadius : d.Ground && d.StableId != "duel_mark" ? d.ReferenceRadius : 0;
    public static float Distance(VfxDefinition d) => d.StableId is "claw_swipe" or "piercing_thrust" ? 1.7f : d.StableId == "rock_raise" ? 2.2f : 3;
    public static float Flight(string id) => id == "mechanical_hook" ? .2f : .7f;
    public static float EndAt(VfxDefinition d, float flight) => d.StableId switch
    {
        "mechanical_hook" => flight + .18f,
        "returning_blade" => flight * 2,
        "projectile" or "piercing_arrow" or "acid_spit" or "trample_rush" => flight,
        "shell_break" or "claw_swipe" or "ground_fissure" or "position_swap" => 1.1f,
        "cone_breath" or "rock_raise" => 2.1f,
        _ => float.PositiveInfinity
    };

    public static (Vector2 Source, Vector2 Target, Vector2 SourceUnit, Vector2 TargetUnit) Geometry(
        VfxDefinition d, Vector2 direction, float distance, float age, float flight)
    {
        var a = -direction * distance / 2;
        var b = direction * distance / 2;
        if (SelfCentered(d)) { a = Vector2.Zero; b = direction * distance; }
        else if (!Directional(d)) { a = -direction * distance; b = Vector2.Zero; }
        var sourceUnit = a; var targetUnit = b;
        if (d.StableId == "teleport" && age >= d.CastDuration) sourceUnit = b;
        float u = Mathf.Clamp(age / Mathf.Max(.05f, flight), 0, 1);
        if (d.StableId == "returning_blade") u = Mathf.Clamp(1 - Mathf.Abs(age / Mathf.Max(.05f, flight) - 1), 0, 1);
        if (d.StableId == "mechanical_hook" && age > flight) u = 1 - Mathf.Clamp((age - flight) / .18f, 0, 1);
        if (Moving(d.StableId))
        {
            if (d.StableId == "trample_rush") sourceUnit = a = a.Lerp(b, u);
            else b = a.Lerp(b, u);
        }
        if (SelfCentered(d)) b = a;
        return (a, b, sourceUnit, targetUnit);
    }
}
