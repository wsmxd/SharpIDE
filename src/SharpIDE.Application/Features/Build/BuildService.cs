using System.Diagnostics;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using SharpIDE.Application.Features.Logging;

namespace SharpIDE.Application.Features.Build;

public enum BuildType
{
	Build,
	Rebuild,
	Clean,
	Restore
}
public class BuildService
{
	public event Func<Task> BuildStarted = () => Task.CompletedTask;
	public ChannelTextWriter BuildTextWriter { get; } = new ChannelTextWriter();
	public async Task MsBuildSolutionAsync(string solutionFilePath, BuildType buildType = BuildType.Build)
	{
		var normalOut = Console.Out;
		Console.SetOut(BuildTextWriter);
		var terminalLogger = InternalTerminalLoggerFactory.CreateLogger();
		var buildParameters = new BuildParameters
		{
			Loggers =
			[
				//new BinaryLogger { Parameters = "msbuild.binlog" },
				//new ConsoleLogger(LoggerVerbosity.Minimal) {Parameters = "FORCECONSOLECOLOR"},
				terminalLogger
				//new InMemoryLogger(LoggerVerbosity.Normal)
			],
		};
		Console.SetOut(normalOut);

		string[] targetsToBuild = buildType switch
		{
			BuildType.Build => ["Restore", "Build"],
			BuildType.Rebuild => ["Restore", "Rebuild"],
			BuildType.Clean => ["Clean"],
			BuildType.Restore => ["Restore"],
			_ => throw new ArgumentOutOfRangeException(nameof(buildType), buildType, null)
		};
		var buildRequest = new BuildRequestData(
			projectFullPath : solutionFilePath,
			globalProperties: new Dictionary<string, string?>(),
			toolsVersion: null,
			targetsToBuild: targetsToBuild,
			hostServices: null,
			flags: BuildRequestDataFlags.None);

		await Task.Run(async () =>
		{
			await BuildStarted.Invoke().ConfigureAwait(false);
			var buildCompleteTcs = new TaskCompletionSource<BuildResult>();
			BuildManager.DefaultBuildManager.BeginBuild(buildParameters);
			var buildResult2 = BuildManager.DefaultBuildManager.PendBuildRequest(buildRequest);
			var timer = Stopwatch.StartNew();
			buildResult2.ExecuteAsync((BuildSubmission test) =>
			{
				buildCompleteTcs.SetResult(test.BuildResult!);
			}, null);
			//var buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest); // This is a convenience to essentially do the same thing.
			var buildResult = await buildCompleteTcs.Task.ConfigureAwait(false);
			timer.Stop();
			BuildManager.DefaultBuildManager.EndBuild();
			Console.WriteLine($"Build result: {buildResult.OverallResult} in {timer.ElapsedMilliseconds}ms");
		}).ConfigureAwait(false);
	}
}

// To build a single project
// var solutionFile = GetNodesInSolution.ParseSolutionFileFromPath(_solutionFilePath);
// ArgumentNullException.ThrowIfNull(solutionFile);
// var projects = GetNodesInSolution.GetCSharpProjectObjectsFromSolutionFile(solutionFile);
// var projectRoot = projects.First();
// var buildRequest = new BuildRequestData(
// 	ProjectInstance.FromProjectRootElement(projectRoot, new ProjectOptions()),
// 	targetsToBuild: ["Restore", "Build"]);
