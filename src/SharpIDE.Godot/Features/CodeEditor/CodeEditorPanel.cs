using Ardalis.GuardClauses;
using Godot;
using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.Debugging;
using SharpIDE.Application.Features.Events;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot.Features.CodeEditor;

public partial class CodeEditorPanel : MarginContainer
{
    [Export]
    public Texture2D CsFileTexture { get; set; } = null!;
    public SharpIdeSolutionModel Solution { get; set; } = null!;
    private PackedScene _sharpIdeCodeEditScene = GD.Load<PackedScene>("res://Features/CodeEditor/SharpIdeCodeEdit.tscn");
    private TabContainer _tabContainer = null!;
	private ExecutionStopInfo? _debuggerExecutionStopInfo;
    
    public override void _Ready()
    {
        _tabContainer = GetNode<TabContainer>("TabContainer");
        _tabContainer.RemoveChildAndQueueFree(_tabContainer.GetChild(0)); // Remove the default tab
        _tabContainer.TabClicked += OnTabClicked;
        var tabBar = _tabContainer.GetTabBar();
        tabBar.TabCloseDisplayPolicy = TabBar.CloseButtonDisplayPolicy.ShowAlways;
        tabBar.TabClosePressed += OnTabClosePressed;
		GlobalEvents.Instance.DebuggerExecutionStopped += OnDebuggerExecutionStopped;
    }
    
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputStringNames.StepOver))
        {
            SendDebuggerStepOver();
        }
    }

    private void OnTabClicked(long tab)
    {
        var sharpIdeFile = _tabContainer.GetChild<SharpIdeCodeEdit>((int)tab).SharpIdeFile;
        GodotGlobalEvents.Instance.InvokeFileExternallySelected(sharpIdeFile);
    }

    private void OnTabClosePressed(long tabIndex)
    {
        var tab = _tabContainer.GetChild<Control>((int)tabIndex);
        _tabContainer.RemoveChild(tab);
        tab.QueueFree();
    }

    public async Task SetSharpIdeFile(SharpIdeFile file, SharpIdeFileLinePosition? fileLinePosition)
    {
		await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        var existingTab = await this.InvokeAsync(() => _tabContainer.GetChildren().OfType<SharpIdeCodeEdit>().FirstOrDefault(t => t.SharpIdeFile == file));
        if (existingTab is not null)
        {
            var existingTabIndex = existingTab.GetIndex();
            await this.InvokeAsync(() =>
            {
                _tabContainer.CurrentTab = existingTabIndex;
                if (fileLinePosition is not null) existingTab.SetFileLinePosition(fileLinePosition.Value);
            });
            return;
        }
        var newTab = _sharpIdeCodeEditScene.Instantiate<SharpIdeCodeEdit>();
        newTab.Solution = Solution;
        await this.InvokeAsync(() =>
        {
            _tabContainer.AddChild(newTab);
            var newTabIndex = _tabContainer.GetTabCount() - 1;
            _tabContainer.SetTabIcon(newTabIndex, CsFileTexture);
            _tabContainer.SetTabTitle(newTabIndex, file.Name);
            _tabContainer.SetTabTooltip(newTabIndex, file.Path);
            _tabContainer.CurrentTab = newTabIndex;
        });
        await newTab.SetSharpIdeFile(file);
        if (fileLinePosition is not null) await this.InvokeAsync(() => newTab.SetFileLinePosition(fileLinePosition.Value));
    }
    
    private async Task OnDebuggerExecutionStopped(ExecutionStopInfo executionStopInfo)
    {
        Guard.Against.Null(Solution, nameof(Solution));
        
        var currentSharpIdeFile = await this.InvokeAsync<SharpIdeFile>(() => _tabContainer.GetChild<SharpIdeCodeEdit>(_tabContainer.CurrentTab).SharpIdeFile);
        
        if (executionStopInfo.FilePath != currentSharpIdeFile?.Path)
        {
            var file = Solution.AllFiles.Single(s => s.Path == executionStopInfo.FilePath);
            await GodotGlobalEvents.Instance.InvokeFileExternallySelectedAndWait(file).ConfigureAwait(false);
        }
        var lineInt = executionStopInfo.Line - 1; // Debugging is 1-indexed, Godot is 0-indexed
        Guard.Against.Negative(lineInt, nameof(lineInt));
        _debuggerExecutionStopInfo = executionStopInfo;
        
        await this.InvokeAsync(() =>
        {
            var focusedTab = _tabContainer.GetChild<SharpIdeCodeEdit>(_tabContainer.CurrentTab);
            focusedTab.SetLineBackgroundColor(lineInt, new Color("665001"));
            focusedTab.SetLineAsExecuting(lineInt, true);
        });
    }
    
    private void SendDebuggerStepOver()
    {
        if (_debuggerExecutionStopInfo is null) return; // ie not currently stopped
        var godotLine = _debuggerExecutionStopInfo.Line - 1;
        var focusedTab = _tabContainer.GetChild<SharpIdeCodeEdit>(_tabContainer.CurrentTab);
        focusedTab.SetLineAsExecuting(godotLine, false);
        focusedTab.SetLineColour(godotLine);
        var threadId = _debuggerExecutionStopInfo.ThreadId;
        _debuggerExecutionStopInfo = null;
        _ = Task.GodotRun(async () =>
        {
            await Singletons.RunService.SendDebuggerStepOver(threadId);
        });
    }
}