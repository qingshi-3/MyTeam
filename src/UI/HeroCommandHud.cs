using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class HeroCommandHud : PanelContainer
{
    public event Action? UseRequested;

    private Label _name = null!;
    private Label _effect = null!;
    private ResourceCostBadge _manaCost = null!;
    private ResourceCostBadge _goldCost = null!;
    private Label _manaText = null!;
    private Label _failure = null!;
    private Button _use = null!;
    private ColorRect[] _segments = [];

    public override void _Ready()
    {
        _name = GetNode<Label>("%CommandName");
        _effect = GetNode<Label>("%CommandEffect");
        _manaCost = GetNode<ResourceCostBadge>("%ManaCostBadge");
        _goldCost = GetNode<ResourceCostBadge>("%GoldCostBadge");
        _manaText = GetNode<Label>("%ManaText");
        _failure = GetNode<Label>("%FailureReason");
        _use = GetNode<Button>("%CommandButton");
        _segments =
        [
            GetNode<ColorRect>("%ManaSegment0"),
            GetNode<ColorRect>("%ManaSegment1"),
            GetNode<ColorRect>("%ManaSegment2")
        ];
        _use.Pressed += OnUsePressed;
    }

    public override void _ExitTree() => _use.Pressed -= OnUsePressed;

    public void Bind(string commandName, string effect, int currentMana, int maxMana, int manaCost, int goldCost, string failureReason, bool battleRunning)
    {
        _name.Text = commandName;
        _effect.Text = effect;
        _manaText.Text = $"MP {currentMana}/{maxMana}";
        _manaCost.BindMana(manaCost);
        _goldCost.BindGold(goldCost);
        _failure.Text = failureReason;
        _failure.Visible = !string.IsNullOrWhiteSpace(failureReason);
        for (var index = 0; index < _segments.Length; index++)
        {
            _segments[index].Visible = index < maxMana;
            _segments[index].Color = index < currentMana ? new Color("3c8dff") : new Color("1b2b46");
        }
        // Resource failures are player-facing command results, so an active battle must allow the attempt.
        _use.Disabled = !battleRunning;
        _use.Text = "发动指令";
        TooltipText = effect;
    }

    private void OnUsePressed() => UseRequested?.Invoke();
}
