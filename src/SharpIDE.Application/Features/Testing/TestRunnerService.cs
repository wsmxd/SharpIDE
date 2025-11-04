using SharpIDE.Application.Features.Evaluation;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;
using SharpIDE.Application.Features.Testing.Client;
using SharpIDE.Application.Features.Testing.Client.Dtos;

namespace SharpIDE.Application.Features.Testing;

public class TestRunnerService
{
	public async Task<List<TestNode>> DiscoverTests(SharpIdeSolutionModel solutionModel)
	{
		var testProjects = solutionModel.AllProjects.Where(p => p.IsMtpTestProject).ToList();
		List<TestNode> allDiscoveredTestNodes = [];
		foreach (var testProject in testProjects)
		{
			using var client = await GetInitialisedClientAsync(testProject);
			List<TestNodeUpdate> testNodeUpdates = [];
			var discoveryResponse = await client.DiscoverTestsAsync(Guid.NewGuid(), node =>
			{
				testNodeUpdates.AddRange(node);
				return Task.CompletedTask;
			});
			await discoveryResponse.WaitCompletionAsync();

			await client.ExitAsync();
			allDiscoveredTestNodes.AddRange(testNodeUpdates.Select(tn => tn.Node));
		}

		return allDiscoveredTestNodes;
	}
	
	// Assumes it has already been built
	public async Task RunTestsAsync(SharpIdeProjectModel project)
	{
		using var client = await GetInitialisedClientAsync(project);
		List<TestNodeUpdate> testNodeUpdates = [];
		var discoveryResponse = await client.DiscoverTestsAsync(Guid.NewGuid(), node =>
		{
			testNodeUpdates.AddRange(node);
			return Task.CompletedTask;
		});
		await discoveryResponse.WaitCompletionAsync();

		Console.WriteLine($"Discovery finished: {testNodeUpdates.Count} tests discovered");
		Console.WriteLine(string.Join(Environment.NewLine, testNodeUpdates.Select(n => n.Node.DisplayName)));

		List <TestNodeUpdate> runResults = [];
		ResponseListener runRequest = await client.RunTestsAsync(Guid.NewGuid(), testNodeUpdates.Select(x => x.Node).ToArray(), node =>
		{
			runResults.AddRange(node);
			return Task.CompletedTask;
		});
		await runRequest.WaitCompletionAsync();


		var passedCount = runResults.Where(tn => tn.Node.ExecutionState == ExecutionStates.Passed).Count();
		var failedCount = runResults.Where(tn => tn.Node.ExecutionState == ExecutionStates.Failed).Count();
		var skippedCount = runResults.Where(tn => tn.Node.ExecutionState == ExecutionStates.Skipped).Count();

		Console.WriteLine($"Passed: {passedCount}; Skipped: {skippedCount}; Failed: {failedCount};");
		await client.ExitAsync();
	}

	private async Task<TestingPlatformClient> GetInitialisedClientAsync(SharpIdeProjectModel project)
	{
		var outputDllPath = ProjectEvaluation.GetOutputDllFullPath(project);
		var outputExecutablePath = 0 switch
		{
			_ when OperatingSystem.IsWindows() => outputDllPath!.Replace(".dll", ".exe"),
			_ when OperatingSystem.IsLinux() => outputDllPath!.Replace(".dll", ""),
			_ when OperatingSystem.IsMacOS() => outputDllPath!.Replace(".dll", ""),
			_ => throw new PlatformNotSupportedException("Unsupported OS for running tests.")
		};

		var client = await TestingPlatformClientFactory.StartAsServerAndConnectToTheClientAsync(outputExecutablePath);
		await client.InitializeAsync();
		return client;
	}
}
