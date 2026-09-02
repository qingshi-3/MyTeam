using Godot;

namespace TowerAutobattler.Battle;

public static class BattlefieldLayout
{
    public const int Width = 10;
    public const int Height = 6;
    public const int PlayerDeploymentColumns = 3;
    public static readonly Vector2 BaseCellPitch = new(88, 68);
    public static readonly Vector2I Version2HeroCell = new(0, 3);
    public static readonly Vector2I[] Version2SoldierCells =
    [
        new(1, 1), new(1, 2), new(1, 3),
        new(2, 1), new(2, 2), new(2, 3)
    ];
    public static readonly Vector2I[] PlayerDeploymentCells = BuildPlayerDeploymentCells();
    public static readonly Vector2I[] EnemyCells =
    [
        new(9, 2), new(9, 3), new(8, 1), new(8, 2),
        new(8, 3), new(8, 4), new(7, 1), new(7, 4)
    ];

    public static bool IsInBounds(Vector2I cell) =>
        cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;

    public static bool IsPlayerDeploymentCell(Vector2I cell) =>
        IsInBounds(cell) && cell.X < PlayerDeploymentColumns;

    public static int PlayerDeploymentSlot(Vector2I cell)
    {
        if (!IsPlayerDeploymentCell(cell)) return -1;
        return cell.Y * PlayerDeploymentColumns + cell.X;
    }

    private static Vector2I[] BuildPlayerDeploymentCells()
    {
        var cells = new Vector2I[PlayerDeploymentColumns * Height];
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < PlayerDeploymentColumns; x++)
            cells[y * PlayerDeploymentColumns + x] = new Vector2I(x, y);
        return cells;
    }
}
