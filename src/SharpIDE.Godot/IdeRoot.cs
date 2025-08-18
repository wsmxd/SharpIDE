using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Godot;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Host.Mef;
using SharpIDE.Application.Features.Analysis;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot;

public partial class IdeRoot : Control
{
	private Button _openSlnButton = null!;
	private FileDialog _fileDialog = null!;
	public override void _Ready()
	{
		MSBuildLocator.RegisterDefaults();
		
		_openSlnButton = GetNode<Button>("%OpenSlnButton");
		_fileDialog = GetNode<FileDialog>("%OpenSolutionDialog");
		_fileDialog.FileSelected += OnFileSelected;
		//_fileDialog.Visible = true;
	}

	private async void OnFileSelected(string path)
	{
		try
		{
			GD.Print($"Selected: {path}");
			var solutionModel = await VsPersistenceMapper.GetSolutionModel(path);
			RoslynAnalysis.StartSolutionAnalysis(path);
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error loading solution: {e.Message}");
			GD.PrintErr(e.StackTrace);
		}
	}
}