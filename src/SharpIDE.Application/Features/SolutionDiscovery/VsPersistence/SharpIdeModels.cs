using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Microsoft.CodeAnalysis;
using ObservableCollections;
using SharpIDE.Application.Features.Evaluation;
using Project = Microsoft.Build.Evaluation.Project;

namespace SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

public interface ISharpIdeNode;

public interface IExpandableSharpIdeNode
{
	public bool Expanded { get; set; }
}
public interface IChildSharpIdeNode
{
	public IExpandableSharpIdeNode Parent { get; set; }

	// TODO: Profile/redesign
	public SharpIdeProjectModel? GetNearestProjectNode()
	{
		var current = this;
		while (current is not SharpIdeProjectModel && current?.Parent is not null)
		{
			current = current.Parent as IChildSharpIdeNode;
		}
		return current as SharpIdeProjectModel;
	}
}

public class SharpIdeSolutionModel : ISharpIdeNode, IExpandableSharpIdeNode
{
	public required string Name { get; set; }
	public required string FilePath { get; set; }
	public required string DirectoryPath { get; set; }
	public required List<SharpIdeProjectModel> Projects { get; set; }
	public required List<SharpIdeSolutionFolder> Folders { get; set; }
	public required HashSet<SharpIdeProjectModel> AllProjects { get; set; }
	public required HashSet<SharpIdeFile> AllFiles { get; set; }
	public bool Expanded { get; set; }

	[SetsRequiredMembers]
	internal SharpIdeSolutionModel(string solutionFilePath, IntermediateSolutionModel intermediateModel)
	{
		var solutionName = Path.GetFileName(solutionFilePath);
		var allProjects = new ConcurrentBag<SharpIdeProjectModel>();
		var allFiles = new ConcurrentBag<SharpIdeFile>();
		Name = solutionName;
		FilePath = solutionFilePath;
		DirectoryPath = Path.GetDirectoryName(solutionFilePath)!;
		Projects = intermediateModel.Projects.Select(s => new SharpIdeProjectModel(s, allProjects, allFiles, this)).ToList();
		Folders = intermediateModel.SolutionFolders.Select(s => new SharpIdeSolutionFolder(s, allProjects, allFiles, this)).ToList();
		AllProjects = allProjects.ToHashSet();
		AllFiles = allFiles.ToHashSet();
	}
}
public class SharpIdeSolutionFolder : ISharpIdeNode, IExpandableSharpIdeNode, IChildSharpIdeNode
{
	public required string Name { get; set; }
	public required List<SharpIdeSolutionFolder> Folders { get; set; }
	public required List<SharpIdeProjectModel> Projects { get; set; }
	public required List<SharpIdeFile> Files { get; set; }
	public bool Expanded { get; set; }
	public required IExpandableSharpIdeNode Parent { get; set; }

	[SetsRequiredMembers]
	internal SharpIdeSolutionFolder(IntermediateSlnFolderModel intermediateModel, ConcurrentBag<SharpIdeProjectModel> allProjects, ConcurrentBag<SharpIdeFile> allFiles, IExpandableSharpIdeNode parent)
	{
		Name = intermediateModel.Model.Name;
		Parent = parent;
		Files = intermediateModel.Files.Select(s => new SharpIdeFile(s.FullPath, s.Name, this, allFiles)).ToList();
		Folders = intermediateModel.Folders.Select(x => new SharpIdeSolutionFolder(x, allProjects, allFiles, this)).ToList();
		Projects = intermediateModel.Projects.Select(x => new SharpIdeProjectModel(x, allProjects, allFiles, this)).ToList();
	}
}
public class SharpIdeProjectModel : ISharpIdeNode, IExpandableSharpIdeNode, IChildSharpIdeNode
{
	public required string Name { get; set; }
	public required string FilePath { get; set; }
	public required List<SharpIdeFolder> Folders { get; set; }
	public required List<SharpIdeFile> Files { get; set; }
	public bool Expanded { get; set; }
	public required IExpandableSharpIdeNode Parent { get; set; }
	public bool Running { get; set; }
	public CancellationTokenSource? RunningCancellationTokenSource { get; set; }
	public required Task<Project> MsBuildEvaluationProjectTask { get; set; }

	[SetsRequiredMembers]
	internal SharpIdeProjectModel(IntermediateProjectModel projectModel, ConcurrentBag<SharpIdeProjectModel> allProjects, ConcurrentBag<SharpIdeFile> allFiles, IExpandableSharpIdeNode parent)
	{
		Parent = parent;
		Name = projectModel.Model.ActualDisplayName;
		FilePath = projectModel.FullFilePath;
		Files = TreeMapperV2.GetFiles(projectModel.FullFilePath, this, allFiles);
		Folders = TreeMapperV2.GetSubFolders(projectModel.FullFilePath, this, allFiles);
		MsBuildEvaluationProjectTask = ProjectEvaluation.GetProject(projectModel.FullFilePath);
		allProjects.Add(this);
	}

	public Project MsBuildEvaluationProject => MsBuildEvaluationProjectTask.IsCompletedSuccessfully
		? MsBuildEvaluationProjectTask.Result
		: throw new InvalidOperationException("Do not attempt to access the MsBuildEvaluationProject before it has been loaded");

	public bool IsRunnable => IsBlazorProject || MsBuildEvaluationProject.GetPropertyValue("OutputType") is "Exe" or "WinExe";
	public bool IsBlazorProject => MsBuildEvaluationProject.Xml.Sdk is "Microsoft.NET.Sdk.BlazorWebAssembly";
	public string BlazorDevServerVersion => MsBuildEvaluationProject.Items.Single(s => s.ItemType is "PackageReference" && s.EvaluatedInclude is "Microsoft.AspNetCore.Components.WebAssembly.DevServer").GetMetadataValue("Version");
	public bool OpenInRunPanel { get; set; }
	public Channel<byte[]>? RunningOutputChannel { get; set; }

	public event Func<Task> ProjectStartedRunning = () => Task.CompletedTask;
	public void InvokeProjectStartedRunning() => ProjectStartedRunning.Invoke();

	public event Func<Task> ProjectStoppedRunning = () => Task.CompletedTask;
	public void InvokeProjectStoppedRunning() => ProjectStoppedRunning.Invoke();

	public ObservableHashSet<Diagnostic> Diagnostics { get; internal set; } = [];
}
