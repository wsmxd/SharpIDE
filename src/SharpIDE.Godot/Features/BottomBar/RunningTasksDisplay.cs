using System.Diagnostics;
using Godot;
using SharpIDE.Application.Features.Analysis;
using SharpIDE.Godot.Features.ActivityListener;

namespace SharpIDE.Godot.Features.BottomBar;

public partial class RunningTasksDisplay : HBoxContainer
{
    [Inject] private readonly ActivityMonitor _activityMonitor = null!;
    
    private bool _isSolutionLoading;
    private bool _isSolutionDiagnosticsBeingRetrieved;

    private Label _solutionLoadingLabel = null!;
    private Label _solutionDiagnosticsLabel = null!;
    
    public override void _Ready()
    {
        _solutionLoadingLabel = GetNode<Label>("SolutionLoadingLabel");
        _solutionDiagnosticsLabel = GetNode<Label>("SolutionDiagnosticsLabel");
        Visible = false;
        _activityMonitor.ActivityStarted.Subscribe(OnActivityStarted);
        _activityMonitor.ActivityStopped.Subscribe(OnActivityStopped);
    }

    public override void _ExitTree()
    {
        _activityMonitor.ActivityStarted.Unsubscribe(OnActivityStarted);
    }

    private async Task OnActivityStarted(Activity activity) => await OnActivityChanged(activity, true);
    private async Task OnActivityStopped(Activity activity) => await OnActivityChanged(activity, false);
    private async Task OnActivityChanged(Activity activity, bool isOccurring)
    {
        if (activity.DisplayName == $"{nameof(RoslynAnalysis)}.{nameof(RoslynAnalysis.UpdateSolutionDiagnostics)}")
        {
            _isSolutionDiagnosticsBeingRetrieved = isOccurring;
        }
        else if (activity.DisplayName == "OpenSolution")
        {
            _isSolutionLoading = isOccurring;
        }
        else
        {
            return;
        }
        
        var visible = _isSolutionDiagnosticsBeingRetrieved || _isSolutionLoading;
        await this.InvokeAsync(() =>
        {
            _solutionLoadingLabel.Visible = _isSolutionLoading;
            _solutionDiagnosticsLabel.Visible = _isSolutionDiagnosticsBeingRetrieved;
            Visible = visible;
        });
    }
}