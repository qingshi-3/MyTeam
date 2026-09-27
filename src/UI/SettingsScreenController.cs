using System;
using Godot;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public sealed record SettingsIntent(float MasterVolume, float DefaultBattleSpeed, bool ReduceUiMotion = false);

public partial class SettingsScreenController : Control
{
    public event Action<SettingsIntent>? SaveRequested;

    private HSlider _volume = null!;
    private Label _volumeValue = null!;
    private OptionButton _speed = null!;
    private Button _save = null!;
    private CheckButton _reduceMotion = null!;

    public override void _Ready()
    {
        _volume = GetNode<HSlider>("Center/Panel/Layout/VolumeSlider");
        _volumeValue = GetNode<Label>("Center/Panel/Layout/VolumeHeader/VolumeValue");
        _speed = GetNode<OptionButton>("Center/Panel/Layout/SpeedOption");
        _save = GetNode<Button>("Center/Panel/Layout/SaveButton");
        _reduceMotion = GetNode<CheckButton>("Center/Panel/Layout/ReduceMotion");
        _save.Pressed += OnSave;
        _volume.ValueChanged += RefreshVolumeValue;
        _reduceMotion.Toggled += RefreshReduceMotionText;
    }

    public override void _ExitTree()
    {
        _save.Pressed -= OnSave;
        _volume.ValueChanged -= RefreshVolumeValue;
        _reduceMotion.Toggled -= RefreshReduceMotionText;
    }

    public void Bind(SettingsDto settings)
    {
        _volume.Value = settings.MasterVolume;
        RefreshVolumeValue(_volume.Value);
        _reduceMotion.SetPressedNoSignal(settings.ReduceUiMotion);
        RefreshReduceMotionText(settings.ReduceUiMotion);
        _speed.Selected = settings.DefaultBattleSpeed switch { >= 4 => 2, >= 2 => 1, _ => 0 };
    }

    private void OnSave() => SaveRequested?.Invoke(new SettingsIntent(
        (float)_volume.Value,
        _speed.Selected switch { 2 => 4f, 1 => 2f, _ => 1f },
        _reduceMotion.ButtonPressed));

    private void RefreshVolumeValue(double value) => _volumeValue.Text = $"{value:P0}";
    private void RefreshReduceMotionText(bool enabled) =>
        _reduceMotion.Text = $"减弱界面动效：{(enabled ? "开" : "关")}";
}
