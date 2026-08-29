using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;

namespace TowerAutobattler.UI;

public partial class DeploymentBoard : Control
{
    public event Action<int>? SlotSelected;
    public event Action<string, int>? UnitDropped;

    private readonly List<DeploymentCell> _cells = [];
    private readonly List<Control> _markers = [];
    private PackedScene _cellScene = null!;
    private PackedScene _markerScene = null!;
    private IBattleFloorRuleRuntime? _floorRule;

    public override void _Ready()
    {
        _cellScene = GD.Load<PackedScene>("res://scenes/ui/components/DeploymentCell.tscn");
        _markerScene = GD.Load<PackedScene>("res://scenes/ui/components/DeploymentMarker.tscn");
        for (var slot = 0; slot < BattlefieldLayout.SoldierCells.Length; slot++)
        {
            var cell = _cellScene.Instantiate<DeploymentCell>();
            AddChild(cell);
            cell.Position = BattlefieldLayout.CellToLocal(BattlefieldLayout.SoldierCells[slot]) - new Vector2(39, 27);
            cell.SlotSelected += OnSlotSelected;
            cell.UnitDropped += OnUnitDropped;
            _cells.Add(cell);
        }
    }

    public override void _ExitTree()
    {
        foreach (var cell in _cells)
        {
            cell.SlotSelected -= OnSlotSelected;
            cell.UnitDropped -= OnUnitDropped;
        }
    }

    public void Bind(BattleConfig config, IReadOnlyList<DeploymentUnitViewModel> roster, IReadOnlyList<string> deployment, string selectedId)
    {
        _floorRule = config.FloorRule;
        QueueRedraw();
        for (var slot = 0; slot < _cells.Count; slot++)
        {
            var instanceId = deployment[slot];
            var unit = roster.FirstOrDefault(entry => entry.InstanceId == instanceId);
            _cells[slot].Bind(slot, instanceId, unit?.DisplayName ?? string.Empty, instanceId == selectedId);
        }
        ClearMarkers();
        var hero = config.Spawns.First(spawn => spawn.Team == 0 && spawn.Unit.IsHero);
        AddMarker(hero.Cell, $"★英雄\n{hero.Unit.DisplayName}", new Color(1f, .78f, .24f, 1f));
        foreach (var enemy in config.Spawns.Where(spawn => spawn.Team == 1))
            AddMarker(enemy.Cell, $"敌\n{enemy.Unit.DisplayName}", new Color(1f, .48f, .48f, 1f));
    }

    public override void _Draw()
    {
        for (var y = 0; y < BattlefieldLayout.Height; y++)
        for (var x = 0; x < BattlefieldLayout.Width; x++)
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
            var topLeft = BattlefieldLayout.CellToLocal(cell) - BattlefieldLayout.CellSize * .46f;
            var rect = new Rect2(topLeft, BattlefieldLayout.CellSize * .92f);
            DrawRect(rect, color, true);
            DrawRect(rect, new Color(1, 1, 1, .1f), false, 1f);
        }
    }

    private void AddMarker(Vector2I cell, string text, Color color)
    {
        var marker = _markerScene.Instantiate<Label>();
        AddChild(marker);
        marker.Position = BattlefieldLayout.CellToLocal(cell) - new Vector2(39, 27);
        marker.Text = text;
        marker.Modulate = color;
        _markers.Add(marker);
    }

    private void ClearMarkers()
    {
        foreach (var marker in _markers) { RemoveChild(marker); marker.Free(); }
        _markers.Clear();
    }

    private void OnSlotSelected(int slot) => SlotSelected?.Invoke(slot);
    private void OnUnitDropped(string instanceId, int slot) => UnitDropped?.Invoke(instanceId, slot);
}
