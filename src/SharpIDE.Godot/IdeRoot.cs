using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Microsoft.Build.Locator;
using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;
using SharpIDE.Godot.Features.Run;

namespace SharpIDE.Godot;

public partial class IdeRoot : Control
{
	private Button _openSlnButton = null!;
	private FileDialog _fileDialog = null!;
	private SharpIdeCodeEdit _sharpIdeCodeEdit = null!;
	private SolutionExplorerPanel _solutionExplorerPanel = null!;
	private RunPanel _runPanel = null!;
	private Button _runMenuButton = null!;
	private Popup _runMenuPopup = null!;
	
	private readonly PackedScene _runMenuItemScene = ResourceLoader.Load<PackedScene>("res://Features/Run/RunMenuItem.tscn");
	public override void _Ready()
	{
		MSBuildLocator.RegisterDefaults();
		
		_openSlnButton = GetNode<Button>("%OpenSlnButton");
		_runMenuPopup = GetNode<Popup>("%RunMenuPopup");
		_runMenuButton = GetNode<Button>("%RunMenuButton");
		_runMenuButton.Pressed += () =>
		{
			var popupMenuPosition = _runMenuButton.GlobalPosition;
			const int buttonHeight = 44;
			_runMenuPopup.Position = new Vector2I((int)popupMenuPosition.X, (int)popupMenuPosition.Y + buttonHeight);
			_runMenuPopup.Popup();
		};
		
		_sharpIdeCodeEdit = GetNode<SharpIdeCodeEdit>("%SharpIdeCodeEdit");
		_fileDialog = GetNode<FileDialog>("%OpenSolutionDialog");
		_solutionExplorerPanel = GetNode<SolutionExplorerPanel>("%SolutionExplorerPanel");
		_fileDialog.FileSelected += OnFileSelected;
		_runPanel = GetNode<RunPanel>("%RunPanel");
		_openSlnButton.Pressed += () => _fileDialog.Visible = true;
		//_fileDialog.Visible = true;
		OnFileSelected(@"C:\Users\Matthew\Documents\Git\BlazorCodeBreaker\BlazorCodeBreaker.slnx");
	}

	private void OnFileSelected(string path)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				GD.Print($"Selected: {path}");
				var solutionModel = await VsPersistenceMapper.GetSolutionModel(path);
				_solutionExplorerPanel.SolutionModel = solutionModel;
				Callable.From(_solutionExplorerPanel.RepopulateTree).CallDeferred();
				RoslynAnalysis.StartSolutionAnalysis(path);
				
				var tasks = solutionModel.AllProjects.Select(p => p.MsBuildEvaluationProjectTask).ToList();
				await Task.WhenAll(tasks).ConfigureAwait(false);
				var runnableProjects = solutionModel.AllProjects.Where(p => p.IsRunnable).ToList();
				await this.InvokeAsync(() =>
				{
					var runMenuPopupVbox = _runMenuPopup.GetNode<VBoxContainer>("VBoxContainer");
					foreach (var project in runnableProjects)
					{
						var runMenuItem = _runMenuItemScene.Instantiate<RunMenuItem>();
						runMenuItem.Project = project;
						runMenuPopupVbox.AddChild(runMenuItem);
					}
					_runMenuButton.Disabled = false;
				});
				
				var infraProject = solutionModel.AllProjects.Single(s => s.Name == "Infrastructure");
				var diFile = infraProject.Files.Single(s => s.Name == "DependencyInjection.cs");
				await this.InvokeAsync(async () => await _sharpIdeCodeEdit.SetSharpIdeFile(diFile));
				
				//var runnableProject = solutionModel.AllProjects.First(s => s.IsRunnable);
				//await this.InvokeAsync(() => _runPanel.NewRunStarted(runnableProject));
			}
			catch (Exception e)
			{
				GD.PrintErr($"Error loading solution: {e.Message}");
				GD.PrintErr(e.StackTrace);
			}
		});
	}
}