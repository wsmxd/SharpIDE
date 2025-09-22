using Ardalis.GuardClauses;
using Godot;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot.Features.SolutionExplorer;

public partial class SolutionExplorerPanel : MarginContainer
{
	[Export]
	public Texture2D CsharpFileIcon { get; set; } = null!;
	[Export]
	public Texture2D FolderIcon { get; set; } = null!;
	[Export]
	public Texture2D SlnFolderIcon { get; set; } = null!;
	[Export]
	public Texture2D CsprojIcon { get; set; } = null!;
	[Export]
	public Texture2D SlnIcon { get; set; } = null!;
	
	public SharpIdeSolutionModel SolutionModel { get; set; } = null!;
	private Tree _tree = null!;
	public override void _Ready()
	{
		_tree = GetNode<Tree>("Tree");
		_tree.ItemMouseSelected += TreeOnItemMouseSelected;
		GodotGlobalEvents.FileExternallySelected += OnFileExternallySelected;
	}

	private void TreeOnItemMouseSelected(Vector2 mousePosition, long mouseButtonIndex)
	{
		var selected = _tree.GetSelected();
		if (selected is null) return;
		var sharpIdeFileContainer = selected.GetMetadata(0).As<SharpIdeFileGodotContainer?>();
		if (sharpIdeFileContainer is null) return;
		var sharpIdeFile = sharpIdeFileContainer.File;
		Guard.Against.Null(sharpIdeFile, nameof(sharpIdeFile));
		GodotGlobalEvents.InvokeFileSelected(sharpIdeFile);
	}
	
	private async Task OnFileExternallySelected(SharpIdeFile file)
	{
		GodotGlobalEvents.InvokeFileSelected(file);
		var item = FindItemRecursive(_tree.GetRoot(), file);
		if (item is not null)
		{
			await this.InvokeAsync(() =>
			{
				item.UncollapseTree();
				_tree.SetSelected(item, 0);
				_tree.ScrollToItem(item, true);
			});
		}
	}
	
	private static TreeItem? FindItemRecursive(TreeItem item, SharpIdeFile file)
	{
		var metadata = item.GetMetadata(0);
		if (metadata.As<SharpIdeFileGodotContainer?>()?.File == file)
			return item;

		var child = item.GetFirstChild();
		while (child != null)
		{
			var result = FindItemRecursive(child, file);
			if (result != null)
				return result;

			child = child.GetNext();
		}

		return null;
	}

	public void RepopulateTree()
	{
		_tree.Clear();

		var rootItem = _tree.CreateItem();
		rootItem.SetText(0, SolutionModel.Name);
		rootItem.SetIcon(0, SlnIcon);

		// Add projects directly under solution
		foreach (var project in SolutionModel.Projects)
		{
			AddProjectToTree(rootItem, project);
		}

		// Add folders under solution
		foreach (var folder in SolutionModel.Folders)
		{
			AddSlnFolderToTree(rootItem, folder);
		}
		rootItem.SetCollapsedRecursive(true);
		rootItem.Collapsed = false;
	}

	private void AddSlnFolderToTree(TreeItem parent, SharpIdeSolutionFolder folder)
	{
		var folderItem = _tree.CreateItem(parent);
		folderItem.SetText(0, folder.Name);
		folderItem.SetIcon(0, SlnFolderIcon);

		foreach (var project in folder.Projects)
		{
			AddProjectToTree(folderItem, project);
		}

		foreach (var subFolder in folder.Folders)
		{
			AddSlnFolderToTree(folderItem, subFolder); // recursion
		}

		foreach (var sharpIdeFile in folder.Files)
		{
			AddFileToTree(folderItem, sharpIdeFile);
		}
	}

	private void AddProjectToTree(TreeItem parent, SharpIdeProjectModel project)
	{
		var projectItem = _tree.CreateItem(parent);
		projectItem.SetText(0, project.Name);
		projectItem.SetIcon(0, CsprojIcon);

		foreach (var sharpIdeFolder in project.Folders)
		{
			AddFoldertoTree(projectItem, sharpIdeFolder);
		}

		foreach (var file in project.Files)
		{
			AddFileToTree(projectItem, file);
		}
	}

	private void AddFoldertoTree(TreeItem projectItem, SharpIdeFolder sharpIdeFolder)
	{
		var folderItem = _tree.CreateItem(projectItem);
		folderItem.SetText(0, sharpIdeFolder.Name);
		folderItem.SetIcon(0, FolderIcon);

		foreach (var subFolder in sharpIdeFolder.Folders)
		{
			AddFoldertoTree(folderItem, subFolder); // recursion
		}

		foreach (var file in sharpIdeFolder.Files)
		{
			AddFileToTree(folderItem, file);
		}
	}

	private void AddFileToTree(TreeItem parent, SharpIdeFile file)
	{
		var fileItem = _tree.CreateItem(parent);
		fileItem.SetText(0, file.Name);
		fileItem.SetIcon(0, CsharpFileIcon);
		var container = new SharpIdeFileGodotContainer { File = file };
		// TODO: Handle ObjectDB instances leaked at exit
		fileItem.SetMetadata(0, container);
	}
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}