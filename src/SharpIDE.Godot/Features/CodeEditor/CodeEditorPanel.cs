using Ardalis.GuardClauses;
using Godot;
using R3;
using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.Debugging;
using SharpIDE.Application.Features.Events;
using SharpIDE.Application.Features.Run;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;
using SharpIDE.Godot.Features.IdeSettings;

namespace SharpIDE.Godot.Features.CodeEditor;

public partial class CodeEditorPanel : MarginContainer
{
    [Export]
    public Texture2D CsFileTexture { get; set; } = null!;
    public SharpIdeSolutionModel Solution { get; set; } = null!;
    private PackedScene _sharpIdeCodeEditScene = GD.Load<PackedScene>("res://Features/CodeEditor/SharpIdeCodeEdit.tscn");
    private TabContainer _tabContainer = null!;
	private ExecutionStopInfo? _debuggerExecutionStopInfo;
    
    [Inject] private readonly RunService _runService = null!;
    public override void _Ready()
    {
        _tabContainer = GetNode<TabContainer>("TabContainer");
        _tabContainer.RemoveChildAndQueueFree(_tabContainer.GetChild(0)); // Remove the default tab
        _tabContainer.TabClicked += OnTabClicked;
        var tabBar = _tabContainer.GetTabBar();
        tabBar.TabCloseDisplayPolicy = TabBar.CloseButtonDisplayPolicy.ShowAlways;
        tabBar.TabClosePressed += OnTabClosePressed;
		GlobalEvents.Instance.DebuggerExecutionStopped.Subscribe(OnDebuggerExecutionStopped);
    }

    public override void _ExitTree()
    {
        var selectedTabIndex = _tabContainer.CurrentTab;
        var thisSolution = Singletons.AppState.RecentSlns.Single(s => s.FilePath == Solution.FilePath);
        thisSolution.IdeSolutionState.OpenTabs = _tabContainer.GetChildren().OfType<SharpIdeCodeEdit>()
            .Select((t, index) => new OpenTab
            {
                FilePath = t.SharpIdeFile.Path,
                CaretLine = t.GetCaretLine(),
                CaretColumn = t.GetCaretColumn(),
                IsSelected = index == selectedTabIndex
            })
            .ToList();
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
        var sharpIdeCodeEdit = _tabContainer.GetChild<SharpIdeCodeEdit>((int)tab);
        var sharpIdeFile = sharpIdeCodeEdit.SharpIdeFile;
        var caretLinePosition = new SharpIdeFileLinePosition(sharpIdeCodeEdit.GetCaretLine(), sharpIdeCodeEdit.GetCaretColumn());
        GodotGlobalEvents.Instance.FileExternallySelected.InvokeParallelFireAndForget(sharpIdeFile, caretLinePosition);
    }

    private void OnTabClosePressed(long tabIndex)
    {
        var tab = _tabContainer.GetChild<Control>((int)tabIndex);
        var previousSibling = _tabContainer.GetChildOrNull<SharpIdeCodeEdit>((int)tabIndex - 1);
        if (previousSibling is not null)
        {
            var sharpIdeFile = previousSibling.SharpIdeFile;
            var caretLinePosition = new SharpIdeFileLinePosition(previousSibling.GetCaretLine(), previousSibling.GetCaretColumn());
            // This isn't actually necessary - closing a tab automatically selects the previous tab, however we need to do it to select the file in sln explorer, record navigation event etc
            GodotGlobalEvents.Instance.FileExternallySelected.InvokeParallelFireAndForget(sharpIdeFile, caretLinePosition);
        }
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
            
            file.IsDirty.Skip(1).SubscribeOnThreadPool().SubscribeAwait(async (isDirty, ct) =>
            {
                //GD.Print($"File dirty state changed: {file.Path} is now {(isDirty ? "dirty" : "clean")}");
                await this.InvokeAsync(() =>
                {
                    var tabIndex = newTab.GetIndex();
                    var title = file.Name + (isDirty ? " (*)" : "");
                    _tabContainer.SetTabTitle(tabIndex, title);
                });
            }).AddTo(newTab); // needs to be on ui thread
        });
        
        await newTab.SetSharpIdeFile(file, fileLinePosition);
    }
    
    private async Task OnDebuggerExecutionStopped(ExecutionStopInfo executionStopInfo)
    {
        Guard.Against.Null(Solution, nameof(Solution));
        
        var currentSharpIdeFile = await this.InvokeAsync<SharpIdeFile>(() => _tabContainer.GetChild<SharpIdeCodeEdit>(_tabContainer.CurrentTab).SharpIdeFile);
        
        if (executionStopInfo.FilePath != currentSharpIdeFile?.Path)
        {
            var file = Solution.AllFiles.Single(s => s.Path == executionStopInfo.FilePath);
            await GodotGlobalEvents.Instance.FileExternallySelected.InvokeParallelAsync(file, null).ConfigureAwait(false);
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
            await _runService.SendDebuggerStepOver(threadId);
        });
    }
}