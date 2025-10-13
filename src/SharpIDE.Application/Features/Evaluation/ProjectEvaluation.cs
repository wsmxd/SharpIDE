using Ardalis.GuardClauses;
using Microsoft.Build.Evaluation;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.Evaluation;

public static class ProjectEvaluation
{
	private static readonly ProjectCollection _projectCollection = ProjectCollection.GlobalProjectCollection;
	public static async Task<Project> GetProject(string projectFilePath)
	{
		using var _ = SharpIdeOtel.Source.StartActivity($"{nameof(ProjectEvaluation)}.{nameof(GetProject)}");
		Guard.Against.Null(projectFilePath, nameof(projectFilePath));

		await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

		var project = _projectCollection.LoadProject(projectFilePath);
		//Console.WriteLine($"ProjectEvaluation: loaded {project.FullPath}");
		return project;
	}

	public static async Task ReloadProject(string projectFilePath)
	{
		using var _ = SharpIdeOtel.Source.StartActivity($"{nameof(ProjectEvaluation)}.{nameof(ReloadProject)}");
		Guard.Against.Null(projectFilePath, nameof(projectFilePath));

		var project = _projectCollection.GetLoadedProjects(projectFilePath).Single();
		var projectRootElement = project.Xml;
		projectRootElement.Reload();
		project.ReevaluateIfNecessary();
	}

	public static string? GetOutputDllFullPath(SharpIdeProjectModel projectModel)
	{
		var project = _projectCollection.GetLoadedProjects(projectModel.FilePath).Single();
		var targetPath = project.GetPropertyValue("TargetPath");
		Guard.Against.NullOrWhiteSpace(targetPath, nameof(targetPath));
		return targetPath;
	}
}
