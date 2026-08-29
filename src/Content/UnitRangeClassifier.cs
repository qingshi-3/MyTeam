namespace TowerAutobattler.Content;

public enum UnitReachClass
{
    Near,
    Ranged
}

public static class UnitRangeClassifier
{
    public const float RangedThreshold = 3f;

    public static UnitReachClass Classify(float attackRange) =>
        attackRange > RangedThreshold ? UnitReachClass.Ranged : UnitReachClass.Near;

    public static string Describe(float attackRange) =>
        Classify(attackRange) == UnitReachClass.Ranged ? "远程" : "近战";

    public static string Marker(float attackRange) =>
        Classify(attackRange) == UnitReachClass.Ranged ? "远" : "近";
}
