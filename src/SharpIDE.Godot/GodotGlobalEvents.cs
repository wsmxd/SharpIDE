using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.Events;
using SharpIDE.Application.Features.SolutionDiscovery;

namespace SharpIDE.Godot;

public static class GodotGlobalEvents
{
    public static event Func<BottomPanelType, Task> BottomPanelTabExternallySelected = _ => Task.CompletedTask;
    public static void InvokeBottomPanelTabExternallySelected(BottomPanelType type) => BottomPanelTabExternallySelected.InvokeParallelFireAndForget(type);
    
    public static event Func<BottomPanelType?, Task> BottomPanelTabSelected = _ => Task.CompletedTask;
    public static void InvokeBottomPanelTabSelected(BottomPanelType? type) => BottomPanelTabSelected.InvokeParallelFireAndForget(type);
    
    public static event Func<bool, Task> BottomPanelVisibilityChangeRequested = _ => Task.CompletedTask;
    public static void InvokeBottomPanelVisibilityChangeRequested(bool show) => BottomPanelVisibilityChangeRequested.InvokeParallelFireAndForget(show);
    
    public static event Func<SharpIdeFile, SharpIdeFileLinePosition?, Task> FileSelected = (_, _) => Task.CompletedTask;
    public static void InvokeFileSelected(SharpIdeFile file, SharpIdeFileLinePosition? fileLinePosition = null) => FileSelected.InvokeParallelFireAndForget(file, fileLinePosition);
    public static async Task InvokeFileSelectedAndWait(SharpIdeFile file, SharpIdeFileLinePosition? fileLinePosition) => await FileSelected.InvokeParallelAsync(file, fileLinePosition);
    public static event Func<SharpIdeFile, SharpIdeFileLinePosition?, Task> FileExternallySelected = (_, _) => Task.CompletedTask;
    public static void InvokeFileExternallySelected(SharpIdeFile file, SharpIdeFileLinePosition? fileLinePosition = null) => FileExternallySelected.InvokeParallelFireAndForget(file, fileLinePosition);
    public static async Task InvokeFileExternallySelectedAndWait(SharpIdeFile file, SharpIdeFileLinePosition? fileLinePosition = null) => await FileExternallySelected.InvokeParallelAsync(file, fileLinePosition);
    
}

public enum BottomPanelType
{
    Run,
    Debug,
    Build,
    Problems,
    IdeDiagnostics
}