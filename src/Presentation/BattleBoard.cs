using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Presentation;

public partial class BattleBoard : Control
{
    private IBattleFloorRuleRuntime? _floorRule;

    public void Bind(IBattleFloorRuleRuntime floorRule)
    {
        _floorRule = floorRule;
        QueueRedraw();
    }

    public Vector2 CellToLocal(Vector2I cell) => BattlefieldLayout.CellToLocal(cell);

    public override void _Draw()
    {
        for (var y = 0; y < BattleSimulation.Height; y++)
        for (var x = 0; x < BattleSimulation.Width; x++)
        {
            var cell = new Vector2I(x, y);
            var preview = _floorRule?.GetCellPreview(cell) ?? FloorCellPreview.Normal;
            var color = preview switch
            {
                FloorCellPreview.Blocked => new Color("243043"),
                FloorCellPreview.Hazard => new Color("6e2f2f"),
                FloorCellPreview.Objective => new Color("285b57"),
                _ => (x + y) % 2 == 0 ? new Color("30384d") : new Color("293146")
            };
            var topLeft = BattlefieldLayout.Origin + new Vector2(x * BattlefieldLayout.CellSize.X, y * BattlefieldLayout.CellSize.Y) - BattlefieldLayout.CellSize * .46f;
            var rect = new Rect2(topLeft, BattlefieldLayout.CellSize * .92f);
            DrawRect(rect, color, true);
            DrawRect(rect, new Color(1, 1, 1, .08f), false, 1f);
        }
    }
}
