using Godot;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Hosting;
using SharpIDE.Application.Features.Events;
using SharpIDE.Application.Features.FilePersistence;
using SharpIDE.Godot.Features.IdeSettings;
using SharpIDE.Godot.Features.SlnPicker;

namespace SharpIDE.Godot;

/// <summary>
/// Used to hold either the main IDE scene or the solution picker scene
/// </summary>
public partial class IdeWindow : Control
{
    private const string SlnPickerScenePath = "res://Features/SlnPicker/SlnPicker.tscn";
    private const string IdeRootScenePath = "res://IdeRoot.tscn";
    private PackedScene? _solutionPickerScene;
    private PackedScene? _ideRootScene;

    private IdeRoot? _ideRoot;
    private SlnPicker? _slnPicker;
    
    [Inject] private readonly IdeOpenTabsFileManager _openTabsFileManager = null!;

    public override void _Ready()
    {
        GD.Print("IdeWindow _Ready called");
        ResourceLoader.LoadThreadedRequest(SlnPickerScenePath);
        ResourceLoader.LoadThreadedRequest(IdeRootScenePath);
        MSBuildLocator.RegisterDefaults();
        GodotServiceDefaults.AddServiceDefaults();
        Singletons.AppState = AppStateLoader.LoadAppStateFromConfigFile();
        GetWindow().FocusExited += OnFocusExited;
        //GetWindow().SetMinSize(new Vector2I(1152, 648));
        Callable.From(() => PickSolution(true)).CallDeferred();
    }
    
    public override void _ExitTree()
    {
        AppStateLoader.SaveAppStateToConfigFile(Singletons.AppState);
        // GodotGlobalEvents.Instance = null!;
        // GlobalEvents.Instance = null!;
        // GC.Collect();
        // GC.WaitForPendingFinalizers();
        // GC.Collect();
        // PrintOrphanNodes();
    }
    
    // TODO: Problematic, as this is called even when the focus shifts to an embedded subwindow, such as a popup 
    private void OnFocusExited()
    {
        _ = Task.GodotRun(async () => await _openTabsFileManager.SaveAllOpenFilesAsync());
    }
    
    public void PickSolution(bool fullscreen = false)
    {
        if (_slnPicker is not null) throw new InvalidOperationException("Solution picker is already active");
        _solutionPickerScene ??= (PackedScene)ResourceLoader.LoadThreadedGet(SlnPickerScenePath);
        _slnPicker = _solutionPickerScene.Instantiate<SlnPicker>();
        if (fullscreen)
        {
            AddChild(_slnPicker);
        }
        else
        {
            var popupWindow = GetNode<Window>("Window");
            var windowSize = GetWindow().GetSize();
            popupWindow.Size = windowSize with { X = windowSize.X / 2, Y = windowSize.Y / 2 };
            popupWindow.Title = "Open Solution";
            popupWindow.AddChild(_slnPicker);
            popupWindow.Popup();
            popupWindow.CloseRequested += () =>
            {
                popupWindow.Hide();
            };
        }
        _ = Task.GodotRun(async () =>
        {
            var slnPathTask = _slnPicker.GetSelectedSolutionPath();
            _ideRootScene ??= (PackedScene)ResourceLoader.LoadThreadedGet(IdeRootScenePath);
            var ideRoot = _ideRootScene.Instantiate<IdeRoot>();
            ideRoot.IdeWindow = this;
            var slnPath = await slnPathTask;
            if (slnPath is null)
            {
                ideRoot.QueueFree();
                _slnPicker.QueueFree();
                _slnPicker = null;
                return;
            }
            ideRoot.SetSlnFilePath(slnPath);
            var recentSln = new RecentSln { FilePath = slnPath, Name = Path.GetFileName(slnPath) };
            Singletons.AppState.RecentSlns.RemoveAll(s => s == recentSln); // Move to end (most recent)
            Singletons.AppState.RecentSlns.Add(recentSln);
            
            await this.InvokeAsync(() =>
            {
                if (fullscreen is false) _slnPicker.GetParent<Window>().Hide();
                _slnPicker.GetParent().RemoveChild(_slnPicker);
                _slnPicker.QueueFree();
                _slnPicker = null;
                if (_ideRoot is not null)
                {
                    RemoveChild(_ideRoot);
                    _ideRoot.QueueFree();
                }
                else
                { 
                    GetWindow().Mode = Window.ModeEnum.Maximized;
                }
                _ideRoot = ideRoot;
                AddChild(ideRoot);
            });
        });
    }
}