using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

// Context adapter; the reusable view owns all information nodes and reading state.
public partial class PreparedUnitDetailPanel : PanelContainer
{
    public void Bind(string identity, UnitDefinition definition, UnitSnapshot snapshot,
        string context, PreparedUnitDetails? prepared = null, float healthRatio = 1) =>
        GetNode<UnitDetailView>("%UnitDetails").Bind(new UnitInformation(identity, definition, snapshot, prepared, healthRatio, context));
}
