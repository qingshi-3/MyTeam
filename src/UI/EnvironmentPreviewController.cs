using Godot;
using TowerAutobattler.Presentation;

namespace TowerAutobattler.UI;

// Isolated scenery viewer. Does not load a content package, run or saved game.
public partial class EnvironmentPreviewController : Control
{
    [Export] public WeatherPresentationStyle[] Styles { get; set; } = [];
    public BattleEnvironment Environment { get; private set; } = null!;
    private Button[] _choices = [];
    private Button _pause = null!;
    private CheckButton _reduced = null!;
    private Button _back = null!;

    public override void _Ready()
    {
        Environment = GetNode<BattleEnvironment>("Background");
        _choices = [GetNode<Button>("%Clear"), GetNode<Button>("%Rain"),
            GetNode<Button>("%Snow"), GetNode<Button>("%Storm")];
        _pause = GetNode<Button>("%Pause");
        _reduced = GetNode<CheckButton>("%ReducedMotion");
        _back = GetNode<Button>("%Back");
        _choices[0].Pressed += OnClear;
        _choices[1].Pressed += OnRain;
        _choices[2].Pressed += OnSnow;
        _choices[3].Pressed += OnStorm;
        _pause.Toggled += OnPause;
        _reduced.Toggled += OnReduced;
        _back.Pressed += OnBack;
        Select(0);
    }

    private void Select(int index)
    {
        if (index >= Styles.Length) return;
        Environment.SetWeatherStyle(Styles[index]);
        Environment.ResetClock();
        GetNode<Label>("%WeatherTitle").Text = Styles[index].DisplayName + "庭院";
        GetNode<Label>("%WeatherDescription").Text = Styles[index].Description;
        for (var i = 0; i < _choices.Length; i++) _choices[i].SetPressedNoSignal(i == index);
    }

    private void OnClear() => Select(0);
    private void OnRain() => Select(1);
    private void OnSnow() => Select(2);
    private void OnStorm() => Select(3);
    private void OnPause(bool paused)
    {
        Environment.SetProcess(!paused);
        _pause.Text = paused ? "继续" : "暂停";
    }
    private void OnReduced(bool reduced)
    {
        Environment.ReducedMotion = reduced;
        Environment.Advance(0);
    }
    private void OnBack() => GetTree().ChangeSceneToFile("res://scenes/app/GrowthGameRoot.tscn");

    public override void _ExitTree()
    {
        _choices[0].Pressed -= OnClear;
        _choices[1].Pressed -= OnRain;
        _choices[2].Pressed -= OnSnow;
        _choices[3].Pressed -= OnStorm;
        _pause.Toggled -= OnPause;
        _reduced.Toggled -= OnReduced;
        _back.Pressed -= OnBack;
    }
}
