using System;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.UI;
using TowerAutobattler.Battle;

namespace TowerAutobattler.App;

// Standalone ability tools. No RunApplication or SaveService is created here.
public partial class ContentRebuildRoot : Control
{
    public event Action? ReturnRequested;
    private ContentRegistry? _content;
    private BattleScreenController _battle = null!;
    private Control _menu = null!;
    private AbilityPrototypePanel _prototype = null!;
    private BattleConfig? _prototypeBattle;
    private string _sampleId = "";
    private string _sampleTitle = "";
    internal bool ContentReady => _content is not null;

    public override async void _Ready()
    {
        _battle = GetNode<BattleScreenController>("Battle");
        _menu = GetNode<Control>("Menu");
        _prototype = GetNode<AbilityPrototypePanel>("PrototypePanel");
        _prototype.StartRequested += StartPrototype;
        _prototype.BackRequested += () => { _prototype.Hide(); _menu.Show(); };
        GetNode<Button>("%Prototype").Pressed += () => { _prototype.Bind(_content!); _menu.Hide(); _prototype.Show(); };
        _battle.ReturnToConfigurationRequested += ReturnToConfiguration;
        _battle.ResetRequested += RestartSample;
        BindSample("Punch", "hero_hc38_grit_brawler", "蓄意轰拳");
        BindSample("Charge", "enemy_ee03_trample_brute", "冲锋");
        BindSample("Fire", "enemy_ee07_furnace_lizard", "蝎子喷火");
        BindSample("Swap", "enemy_ee09_swap_envoy", "位置交换");
        GetNode<Button>("%Vfx").Pressed += OpenVfx;
        GetNode<Button>("%Environment").Pressed += OpenEnvironment;
        GetNode<Button>("%Quit").Pressed += Quit;
        GetNode<Button>("%BackToGame").Pressed += BackToGame;
        try
        {
            var content = await SkillSampleContent.LoadAsync(this);
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion()) return;
            _content = content;
            GetNode<Button>("%Prototype").Disabled = false;
            foreach (var name in new[] { "Punch", "Charge", "Fire", "Swap" })
                GetNode<Button>("%" + name).Disabled = false;
            GetNode<Label>("%Status").Text = "八羁绊能力实验与保留技能可体验";
        }
        catch (Exception exception)
        {
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion()) return;
            GD.PushError("Retained skill content failed: " + exception);
            GetNode<Label>("%Status").Text = "保留技能载入失败，请查看日志。存档未被改动。";
        }
    }

    private void BindSample(string button, string id, string title) =>
        GetNode<Button>("%" + button).Pressed += () => StartSample(id, title);

    private void StartSample(string id, string title)
    {
        if (_content is null) return;
        _prototypeBattle = null;
        _sampleId = id;
        _sampleTitle = title;
        _menu.Hide();
        _battle.Show();
        _battle.StartBattle(_content, SkillSampleContent.CreateBattle(_content, id), title + " · 技能验证");
        _battle.SetLabControlsVisible(true);
    }

    private void StartPrototype()
    {
        if (_content is null) return;
        _prototypeBattle = _prototype.CreateBattle();
        _prototype.Hide();
        _battle.Show();
        _battle.StartBattle(_content, _prototypeBattle, "八羁绊 · 能力实验");
        _battle.SetLabControlsVisible(true);
    }
    private void RestartSample()
    {
        if (_prototypeBattle is null) { StartSample(_sampleId, _sampleTitle); return; }
        _battle.StartBattle(_content!, _prototypeBattle, "八羁绊 · 能力实验");
        _battle.SetLabControlsVisible(true);
    }
    private void ReturnToConfiguration()
    {
        if (_prototypeBattle is null) { ShowMenu(); return; }
        _battle.StopBattle(); _battle.Hide(); _prototype.Show();
        _prototypeBattle = null;
    }

    private void ShowMenu()
    {
        _battle.StopBattle();
        _battle.Hide();
        _menu.Show();
    }

    private void OpenVfx() => GetTree().ChangeSceneToFile("res://scenes/app/VfxPreview.tscn");
    private void OpenEnvironment() => GetTree().ChangeSceneToFile("res://scenes/app/EnvironmentPreview.tscn");
    private void Quit() => GetTree().Quit();
    private void BackToGame()
    {
        if (ReturnRequested is { } back) back();
        else GetTree().ChangeSceneToFile("res://scenes/app/GrowthGameRoot.tscn");
    }

    public override void _ExitTree()
    {
        if (_battle is not null)
        {
            _battle.ReturnToConfigurationRequested -= ReturnToConfiguration;
            _battle.ResetRequested -= RestartSample;
        }
        _content = null;
    }
}
