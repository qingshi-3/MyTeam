using System;
using Godot;
using TowerAutobattler.BattleLab;

namespace TowerAutobattler.UI;

public partial class BattleLabLibraryCard : Button
{
    public event Action<string, BattleLabSide>? DragRequested;
    public string ContentId { get; private set; } = string.Empty;
    public BattleLabSide Side { get; private set; }

    public void Bind(string contentId, string displayName, BattleLabSide side, string classification)
    {
        ContentId = contentId ?? string.Empty;
        Side = side;
        Text = $"{(side == BattleLabSide.Player ? "◆" : "⚔")} {displayName}\n{classification}";
        TooltipText = $"{displayName}\n{contentId}";
        Disabled = string.IsNullOrWhiteSpace(ContentId);
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (!Disabled && inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            DragRequested?.Invoke(ContentId, Side);
            AcceptEvent();
        }
    }

    public override void _ExitTree()
    {
        DragRequested = null;
        ContentId = string.Empty;
    }
}
