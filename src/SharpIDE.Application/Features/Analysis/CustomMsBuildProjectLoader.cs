using System.Collections.Immutable;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace SharpIDE.Application.Features.Analysis;

public class CustomMsBuildProjectLoader(Workspace workspace, ImmutableDictionary<string, string>? properties = null) : MSBuildProjectLoader(workspace, properties)
{
	public async Task<ImmutableArray<ProjectInfo>> LoadProjectInfosAsync(
		List<string> projectFilePaths,
		ProjectMap? projectMap = null,
		IProgress<ProjectLoadProgress>? progress = null,
#pragma warning disable IDE0060 // TODO: decide what to do with this unusued ILogger, since we can't reliabily use it if we're sending builds out of proc
		ILogger? msbuildLogger = null,
#pragma warning restore IDE0060
		CancellationToken cancellationToken = default)
	{
		if (projectFilePaths.Count is 0)
		{
			throw new ArgumentException("At least one project file path must be specified.", nameof(projectFilePaths));
		}

		var requestedProjectOptions = DiagnosticReportingOptions.ThrowForAll;

		var reportingMode = GetReportingModeForUnrecognizedProjects();

		var discoveredProjectOptions = new DiagnosticReportingOptions(
			onPathFailure: reportingMode,
			onLoaderFailure: reportingMode);

		var buildHostProcessManager = new BuildHostProcessManager(Properties, loggerFactory: _loggerFactory);
		await using var _ = buildHostProcessManager.ConfigureAwait(false);

		var worker = new Worker(
			_solutionServices,
			_diagnosticReporter,
			_pathResolver,
			_projectFileExtensionRegistry,
			buildHostProcessManager,
			requestedProjectPaths: projectFilePaths.ToImmutableArray(),
			baseDirectory: Directory.GetCurrentDirectory(),
			projectMap,
			progress,
			requestedProjectOptions,
			discoveredProjectOptions,
			this.LoadMetadataForReferencedProjects);

		return await worker.LoadAsync(cancellationToken).ConfigureAwait(false);
	}
}
