using GDExtensionBindgen;
using Godot;

namespace SharpIDE.Godot.Features.Build;

public partial class BuildPanel : Control
{
    private Terminal _terminal = null!;
	private Task _writeTask = Task.CompletedTask;
    public override void _Ready()
    {
        _terminal = new Terminal(GetNode<Control>("%Terminal"));
        Singletons.BuildService.BuildStarted += OnBuildStarted;
    }

    private async Task OnBuildStarted()
    {
        if (_writeTask.IsCompleted is not true)
        {
            // If the write task is already running, just clear the terminal - we reuse the channel for the build output 🤷‍♂️
            await this.InvokeAsync(() => _terminal.Clear());
            return;
        }
        _writeTask = GodotTask.Run(async () =>
        {
            await this.InvokeAsync(() => _terminal.Clear());
            await foreach (var str in Singletons.BuildService.BuildTextWriter.ConsoleChannel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await this.InvokeAsync(() => _terminal.Write(str));
            }
        });
    }
}