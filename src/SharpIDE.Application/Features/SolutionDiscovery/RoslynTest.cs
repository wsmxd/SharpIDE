using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.MSBuild;

namespace SharpIDE.Application.Features.SolutionDiscovery;

public static class RoslynTest
{
	public static async Task Analyse(string solutionFilePath)
	{


		var workspace = MSBuildWorkspace.Create();
		var timer = Stopwatch.StartNew();
		var solution = await workspace.OpenSolutionAsync(solutionFilePath);
		timer.Stop();
		Console.WriteLine($"Solution loaded in {timer.ElapsedMilliseconds}ms");
		Console.WriteLine();

		// foreach (var project in solution.Projects)
		// {
		// 	Console.WriteLine($"Project: {project.Name}");
		// 	foreach (var document in project.Documents)
		// 	{
		// 		Console.WriteLine($"Document: {document.Name}");
		// 		var syntaxTree = await document.GetSyntaxTreeAsync();
		// 		var root = await syntaxTree!.GetRootAsync();
		// 		var classifiedSpans = await Classifier.GetClassifiedSpansAsync(document, root.FullSpan);
		// 		foreach (var span in classifiedSpans)
		// 		{
		// 			var classifiedSpan = root.GetText().GetSubText(span.TextSpan);
		// 			Console.WriteLine($"{span.TextSpan}: {span.ClassificationType}");
		// 			Console.WriteLine(classifiedSpan);
		// 		}
		// 	}
		// }

	}
}
