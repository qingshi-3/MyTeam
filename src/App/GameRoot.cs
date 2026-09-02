using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

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

    public ContentRegistry? Content => _app?.Content;
    internal GameFlowCoordinator Flow => _flow ??
        throw new System.InvalidOperationException("Game flow has not completed bootstrap.");

    public override async void _Ready()
    {
        _screens = GetNode<AppScreenHost>(ScreenHostPath);
        var gate = await GamePackagePublisher.CreateReadyAsync(this, ProjectDefinition);
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        if (gate.Package is not { } package)
        {
            ShowBootstrapFailure("项目内容校验失败", string.Join("\n", gate.Report.CoreErrors));
            return;
        }

        var registry = package.Content;
        var project = package.Project;
        SemanticIcons.Configure(project.Presentation.SemanticIcons);
        _app = new RunApplication(registry, new SaveService(SaveNamespace), project);
        _flow = new GameFlowCoordinator(() => _app, _screens, project.Presentation, () => GetTree().Quit());
        _flow.Start();
    }

    public override void _ExitTree()
    {
        _flow?.Dispose();
        _flow = null;
    }

    private void ShowBootstrapFailure(string title, string summary)
    {
        _screens.Result.Bind(title, summary);
        _screens.Show(AppScreenId.Result, null, null, null);
    }
}
