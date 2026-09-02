using System;
using Godot;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public sealed record SettingsIntent(float MasterVolume, float DefaultBattleSpeed);

public partial class SettingsScreenController : Control
{
    public event Action<SettingsIntent>? SaveRequested;

    private HSlider _volume = null!;
    private OptionButton _speed = null!;
    private Button _save = null!;

    public override void _Ready()
    {
        _volume = GetNode<HSlider>("Center/Panel/Layout/VolumeSlider");
        _speed = GetNode<OptionButton>("Center/Panel/Layout/SpeedOption");
        _save = GetNode<Button>("Center/Panel/Layout/SaveButton");
        _save.Pressed += OnSave;
    }

    public override void _ExitTree() => _save.Pressed -= OnSave;

    public void Bind(SettingsDto settings)
    {
        _volume.Value = settings.MasterVolume;
        _speed.Selected = settings.DefaultBattleSpeed switch { >= 4 => 2, >= 2 => 1, _ => 0 };
    }

    private void OnSave() => SaveRequested?.Invoke(new SettingsIntent(
        (float)_volume.Value,
        _speed.Selected switch { 2 => 4f, 1 => 2f, _ => 1f }));
}
