using System;
using System.IO;
using System.Linq;
using Godot;
using Microsoft.Build.Locator;
using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot;

public partial class IdeRoot : Control
{
	private Button _openSlnButton = null!;
	private FileDialog _fileDialog = null!;
	private SharpIdeCodeEdit _sharpIdeCodeEdit = null!;
	private SolutionExplorerPanel _solutionExplorerPanel = null!;
	public override void _Ready()
	{
		MSBuildLocator.RegisterDefaults();
		
		_openSlnButton = GetNode<Button>("%OpenSlnButton");
		_sharpIdeCodeEdit = GetNode<SharpIdeCodeEdit>("%SharpIdeCodeEdit");
		_fileDialog = GetNode<FileDialog>("%OpenSolutionDialog");
		_solutionExplorerPanel = GetNode<SolutionExplorerPanel>("%SolutionExplorerPanel");
		_fileDialog.FileSelected += OnFileSelected;
		_openSlnButton.Pressed += () => _fileDialog.Visible = true;
		//_fileDialog.Visible = true;
		OnFileSelected(@"C:\Users\Matthew\Documents\Git\BlazorCodeBreaker\BlazorCodeBreaker.slnx");
	}

	private async void OnFileSelected(string path)
	{
		try
		{
			GD.Print($"Selected: {path}");
			var solutionModel = await VsPersistenceMapper.GetSolutionModel(path);
			_solutionExplorerPanel.SolutionModel = solutionModel;
			_solutionExplorerPanel.RepopulateTree();
			RoslynAnalysis.StartSolutionAnalysis(path);
			var infraProject = solutionModel.AllProjects.Single(s => s.Name == "Infrastructure");
			var diFile = infraProject.Files.Single(s => s.Name == "DependencyInjection.cs");
			await _sharpIdeCodeEdit.SetSharpIdeFile(diFile);
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error loading solution: {e.Message}");
			GD.PrintErr(e.StackTrace);
		}
	}
}