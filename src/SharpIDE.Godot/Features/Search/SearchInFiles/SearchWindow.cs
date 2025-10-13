using Godot;
using SharpIDE.Application.Features.Search;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot.Features.Search;

public partial class SearchWindow : PopupPanel
{
    private LineEdit _lineEdit = null!;
    private VBoxContainer _searchResultsContainer = null!;
    public SharpIdeSolutionModel Solution { get; set; } = null!;
	private readonly PackedScene _searchResultEntryScene = ResourceLoader.Load<PackedScene>("res://Features/Search/SearchInFiles/SearchResultComponent.tscn");

    private CancellationTokenSource _cancellationTokenSource = new();
    
    public override void _Ready()
    {
        _lineEdit = GetNode<LineEdit>("%SearchLineEdit");
        _lineEdit.Text = "";
        _searchResultsContainer = GetNode<VBoxContainer>("%SearchResultsVBoxContainer");
        _searchResultsContainer.GetChildren().ToList().ForEach(s => s.QueueFree());
        _lineEdit.TextChanged += OnTextChanged;
        AboutToPopup += OnAboutToPopup;
    }

    private void OnAboutToPopup()
    {
        _lineEdit.SelectAll();
        Callable.From(_lineEdit.GrabFocus).CallDeferred();
    }

    private async void OnTextChanged(string newText)
    {
        await _cancellationTokenSource.CancelAsync();
        // TODO: Investigate allocations
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        await Task.GodotRun(() => Search(newText, token));
    }

    private async Task Search(string text, CancellationToken cancellationToken)
    {
        var result = await SearchService.FindInFiles(Solution, text, cancellationToken);
        await this.InvokeAsync(() =>
        {
            _searchResultsContainer.GetChildren().ToList().ForEach(s => s.QueueFree());
            foreach (var searchResult in result)
            {
                var resultNode = _searchResultEntryScene.Instantiate<SearchResultComponent>();
                resultNode.Result = searchResult;
                resultNode.ParentSearchWindow = this;
                _searchResultsContainer.AddChild(resultNode);
            }
        });
    }
}
