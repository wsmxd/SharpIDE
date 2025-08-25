using System.Threading.Tasks;
using Godot;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot.Features.Run;

public partial class RunMenuItem : HBoxContainer
{
    public SharpIdeProjectModel Project { get; set; } = null!;
    private Label _label = null!;
    private Button _runButton = null!;
    private Button _stopButton = null!;
    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        _label.Text = Project.Name;
        _runButton = GetNode<Button>("RunButton");
        _runButton.Pressed += OnRunButtonPressed;
        _stopButton = GetNode<Button>("StopButton");
        _stopButton.Pressed += OnStopButtonPressed;
        Project.ProjectStartedRunning += OnProjectStartedRunning;
    }

    private async Task OnProjectStartedRunning()
    {
        await this.InvokeAsync(() =>
        {
            _runButton.Visible = false;
            _stopButton.Visible = true;
        });
    }

    private async void OnStopButtonPressed()
    {
        await Singletons.RunService.CancelRunningProject(Project);
        _stopButton.Visible = false;
        _runButton.Visible = true;
    }

    private async void OnRunButtonPressed()
    {
        await Singletons.RunService.RunProject(Project).ConfigureAwait(false);
    }
}