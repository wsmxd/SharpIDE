using Godot;

namespace SharpIDE.Godot.Features.LeftSideBar;

public partial class LeftSideBar : Panel
{
    private Button _slnExplorerButton = null!;
    // These are in a ButtonGroup, which handles mutual exclusivity of being toggled on
    private Button _problemsButton = null!;
    private Button _runButton = null!;
    private Button _buildButton = null!;
    private Button _debugButton = null!;
    private Button _ideDiagnosticsButton = null!;
    
    public override void _Ready()
    {
        _slnExplorerButton = GetNode<Button>("%SlnExplorerButton");
        _problemsButton = GetNode<Button>("%ProblemsButton");
        _runButton = GetNode<Button>("%RunButton");
        _buildButton = GetNode<Button>("%BuildButton");
        _debugButton = GetNode<Button>("%DebugButton");
        _ideDiagnosticsButton = GetNode<Button>("%IdeDiagnosticsButton");
        
        _problemsButton.Toggled += toggledOn => GodotGlobalEvents.InvokeBottomPanelTabSelected(toggledOn ? BottomPanelType.Problems : null);
        _runButton.Toggled += toggledOn => GodotGlobalEvents.InvokeBottomPanelTabSelected(toggledOn ? BottomPanelType.Run : null);
        _buildButton.Toggled += toggledOn => GodotGlobalEvents.InvokeBottomPanelTabSelected(toggledOn ? BottomPanelType.Build : null);
        _debugButton.Toggled += toggledOn => GodotGlobalEvents.InvokeBottomPanelTabSelected(toggledOn ? BottomPanelType.Debug : null);
        _ideDiagnosticsButton.Toggled += toggledOn => GodotGlobalEvents.InvokeBottomPanelTabSelected(toggledOn ? BottomPanelType.IdeDiagnostics : null);
        GodotGlobalEvents.BottomPanelTabExternallySelected += OnBottomPanelTabExternallySelected;
    }

    private async Task OnBottomPanelTabExternallySelected(BottomPanelType arg)
    {
        await this.InvokeAsync(() =>
        {
            switch (arg)
            {
                case BottomPanelType.Run: _runButton.ButtonPressed = true; break;
                case BottomPanelType.Debug: _debugButton.ButtonPressed = true; break;
                case BottomPanelType.Build: _buildButton.ButtonPressed = true; break;
                case BottomPanelType.Problems: _problemsButton.ButtonPressed = true; break;
                case BottomPanelType.IdeDiagnostics: _ideDiagnosticsButton.ButtonPressed = true; break;
                default: throw new ArgumentOutOfRangeException(nameof(arg), arg, null);
            }
        });
    }
}