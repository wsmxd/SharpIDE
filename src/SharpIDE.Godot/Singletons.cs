using SharpIDE.Application.Features.Build;
using SharpIDE.Application.Features.FileWatching;
using SharpIDE.Application.Features.Run;
using SharpIDE.Godot.Features.IdeSettings;

namespace SharpIDE.Godot;

public static class Singletons
{
    public static RunService RunService { get; set; } = null!;
    public static BuildService BuildService { get; set; } = null!;
    public static IdeFileWatcher FileWatcher { get; set; } = null!;
    public static AppState AppState { get; set; } = null!;
}