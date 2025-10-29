using Godot;
using R3;
using SharpIDE.Application.Features.NavigationHistory;

namespace SharpIDE.Godot.Features.Navigation;

public partial class ForwardBackwardButtonContainer : HBoxContainer
{
    private Button _backwardButton = null!;
    private Button _forwardButton = null!;
    
    [Inject] private readonly IdeNavigationHistoryService _navigationHistoryService = null!;

    public override void _Ready()
    {
        _backwardButton = GetNode<Button>("BackwardButton");
        _forwardButton = GetNode<Button>("ForwardButton");
        _backwardButton.Pressed += OnBackwardButtonPressed;
        _forwardButton.Pressed += OnForwardButtonPressed;
        Observable.EveryValueChanged(_navigationHistoryService, navigationHistoryService => navigationHistoryService.Current)
            .Where(s => s is not null)
            .Subscribe(s =>
            {
                _backwardButton.Disabled = !_navigationHistoryService.CanGoBack;
                _forwardButton.Disabled = !_navigationHistoryService.CanGoForward;
            }).AddTo(this);
    }
    
    private void OnBackwardButtonPressed()
    {
        _navigationHistoryService.GoBack();
    }

    private void OnForwardButtonPressed()
    {
        _navigationHistoryService.GoForward();
    }
}