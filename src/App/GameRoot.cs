using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;
using TowerAutobattler.Audio;

namespace TowerAutobattler.App;

// Project composition root: atomically publish the complete compiled game package,
// construct the Run facade, then delegate typed screen flow to the coordinator.
public partial class GameRoot : Control
{
    [Export] public string SaveNamespace { get; set; } = string.Empty;
    [Export] public GameProjectDefinition? ProjectDefinition { get; set; }
    [Export] public NodePath ScreenHostPath { get; set; } = "Screens";

    private RunApplication? _app;
    private GameFlowCoordinator? _flow;
    private AppScreenHost _screens = null!;
    private UiFeedbackBinding? _uiFeedback;
    private UiMotionBinding? _uiMotion;
    private bool _bootstrapping;
    private ContentRebuildRoot? _abilityPrototype;

    public ContentRegistry? Content => _app?.Content;
    internal GameFlowCoordinator Flow => _flow ??
        throw new System.InvalidOperationException("Game flow has not completed bootstrap.");

    protected virtual System.Threading.Tasks.Task<GamePackagePublicationResult> PublishPackageAsync() =>
        GamePackagePublisher.CreateReadyAsync(this, ProjectDefinition);

    public override async void _Ready()
    {
        _screens = GetNode<AppScreenHost>(ScreenHostPath);
        var audio = GetNode<FeedbackAudio>("UiAudio");
        _screens.BindJourneySound(audio.RequestUi);
        _uiFeedback = new UiFeedbackBinding(this, audio, _screens.MainMenu);
        _uiMotion = new UiMotionBinding(this, () => _app?.Settings.ReduceUiMotion ?? false);
        await BootstrapAsync();
    }

    private bool CanFinishBootstrap => GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion();

    private async Task BootstrapAsync()
    {
        if (_bootstrapping) return;
        _bootstrapping = true;
        try
        {
            ClearFlowBindings();
            _app = null;
            _screens.Visible = false;
            var gate = await PublishPackageAsync();
            if (!CanFinishBootstrap) return;
            if (gate.Package is not { } package)
            {
                GD.PushWarning("Game bootstrap content rejected: " + string.Join("; ", gate.Report.CoreErrors));
                ShowBootstrapFailure("项目内容校验失败", "游戏内容未能完整载入，存档未被重置。请修复内容后重试启动。");
                return;
            }

            var registry = package.Content;
            var project = package.Project;
            SemanticIcons.Configure(project.Presentation.SemanticIcons);
            _app = new RunApplication(package, new SaveService(SaveNamespace), SaveNamespace);
            var audio = GetNode<FeedbackAudio>("UiAudio");
            _flow = new GameFlowCoordinator(() => _app, _screens, project.Presentation, Quit, audio.RequestUi);
            _flow.Start();
            _screens.MainMenu.ExperienceSliceRequested += OpenExperienceSlice;
            _screens.MainMenu.VfxPreviewRequested += OpenVfx;
            _screens.MainMenu.EnvironmentPreviewRequested += OpenEnvironment;
            _screens.MainMenu.AbilityPrototypeRequested += OpenAbilityPrototype;
            _screens.MainMenu.GrowthJourneyRequested += OpenGrowthJourney;
            _screens.MainMenu.BindJourney(_app.GrowthRules is not null);
            _screens.Visible = true;
        }
        catch (Exception exception)
        {
            if (!CanFinishBootstrap) return;
            GD.PushWarning("Game bootstrap failed: " + exception);
            ShowBootstrapFailure("启动未完成", exception is InvalidDataException
                ? "保存数据无法读取，原文件已保留。请修复存档或恢复备份后重试启动。"
                : "游戏初始化失败，存档未被重置。请检查日志，排除问题后重试启动。");
        }
        finally { _bootstrapping = false; }
    }

    public override void _ExitTree()
    {
        if (_abilityPrototype is not null)
            _abilityPrototype.ReturnRequested -= CloseAbilityPrototype;
        _uiMotion?.Dispose();
        _uiMotion = null;
        _uiFeedback?.Dispose();
        _uiFeedback = null;
        ClearFlowBindings();
        _app = null;
    }

    private void ClearFlowBindings()
    {
        if (_screens is not null)
        {
            _screens.MainMenu.ExperienceSliceRequested -= OpenExperienceSlice;
            _screens.MainMenu.VfxPreviewRequested -= OpenVfx;
            _screens.MainMenu.EnvironmentPreviewRequested -= OpenEnvironment;
            _screens.MainMenu.AbilityPrototypeRequested -= OpenAbilityPrototype;
            _screens.MainMenu.GrowthJourneyRequested -= OpenGrowthJourney;
            _screens.Result.NewRunRequested -= RetryBootstrap;
            _screens.Result.MenuRequested -= Quit;
        }
        _flow?.Dispose();
        _flow = null;
    }

    private void OpenExperienceSlice() => GetTree().ChangeSceneToFile("res://scenes/app/ExperienceSlice.tscn");
    private void OpenVfx() => GetTree().ChangeSceneToFile("res://scenes/app/VfxPreview.tscn");
    private void OpenEnvironment() => GetTree().ChangeSceneToFile("res://scenes/app/EnvironmentPreview.tscn");
    private void OpenAbilityPrototype()
    {
        if (_abilityPrototype is not null) return;
        _abilityPrototype = GD.Load<PackedScene>("res://scenes/app/ContentRebuildRoot.tscn").Instantiate<ContentRebuildRoot>();
        _abilityPrototype.ReturnRequested += CloseAbilityPrototype;
        _screens.Hide();
        AddChild(_abilityPrototype);
    }

    private void CloseAbilityPrototype()
    {
        if (_abilityPrototype is null) return;
        _abilityPrototype.ReturnRequested -= CloseAbilityPrototype;
        _abilityPrototype.QueueFree();
        _abilityPrototype = null;
        _screens.Show();
    }
    private void OpenGrowthJourney() => GetTree().ChangeSceneToFile(_app?.GrowthRules is null
        ? "res://scenes/app/GrowthGameRoot.tscn" : "res://scenes/app/GameRoot.tscn");

    private void ShowBootstrapFailure(string title, string summary)
    {
        ClearFlowBindings();
        _app = null;
        _screens.Result.Bind(title, summary, "重试启动", "退出游戏");
        _screens.Result.NewRunRequested += RetryBootstrap;
        _screens.Result.MenuRequested += Quit;
        _screens.Show(AppScreenId.Result, null, null, null);
        _screens.Visible = true;
    }

    private async void RetryBootstrap() => await BootstrapAsync();
    private void Quit() => GetTree().Quit();
}
