using Godot;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot;

public partial class SolutionExplorerPanel : Panel
{
	public SharpIdeSolutionModel SolutionModel { get; set; } = null!;
	private Tree _tree = null!;
	public override void _Ready()
	{
		_tree = GetNode<Tree>("Tree");
		var item = _tree.CreateItem();
		item.SetText(0, "Solution Explorer");
	}
	
	public void RepopulateTree()
	{
		_tree.Clear();

		var rootItem = _tree.CreateItem();
		rootItem.SetText(0, SolutionModel.Name);

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
	}

	private void AddSlnFolderToTree(TreeItem parent, SharpIdeSolutionFolder folder)
	{
		var folderItem = _tree.CreateItem(parent);
		folderItem.SetText(0, folder.Name);

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
	}
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}