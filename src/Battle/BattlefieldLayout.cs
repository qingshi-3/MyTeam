using Godot;

namespace TowerAutobattler.Battle;

public static class BattlefieldLayout
{
    public const int Width = 10;
    public const int Height = 6;
    public static readonly Vector2 CellSize = new(88, 68);
    public static readonly Vector2 Origin = new(56, 52);
    public static readonly Vector2I HeroCell = new(0, 3);
    public static readonly Vector2I[] SoldierCells =
    [
        new(1, 1), new(1, 2), new(1, 3),
        new(2, 1), new(2, 2), new(2, 3)
    ];
    public static readonly Vector2I[] EnemyCells =
    [
        new(9, 2), new(9, 3), new(8, 1), new(8, 2),
        new(8, 3), new(8, 4), new(7, 1), new(7, 4)
    ];

    public static Vector2 CellToLocal(Vector2I cell) => Origin + new Vector2(cell.X * CellSize.X, cell.Y * CellSize.Y);
}
