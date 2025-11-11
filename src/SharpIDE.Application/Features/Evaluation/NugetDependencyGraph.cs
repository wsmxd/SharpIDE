using NuGet.Packaging.Core;
using NuGet.ProjectModel;

namespace SharpIDE.Application.Features.Evaluation;

public class Dependent
{
	public required string PackageName { get; set; }
	public required PackageDependency PackageDependency { get; set; }
}
public static class NugetDependencyGraph
{
	internal static Dictionary<string, List<Dependent>> GetPackageDependencyMap(LockFile assetsFile)
	{
		var parentMap = new Dictionary<string, List<Dependent>>(StringComparer.OrdinalIgnoreCase);

		var target = assetsFile.Targets.SingleOrDefault(s => s.RuntimeIdentifier is null);
		if (target == null) return parentMap;

		var packageLibraries = target.Libraries.ToList();

		foreach (var library in packageLibraries)
		{
			var dependencies = library.Dependencies;
			foreach (var packageDependency in dependencies)
			{
				var mapEntry = parentMap!.GetValueOrDefault(packageDependency.Id, []);
				var dependent = new Dependent
				{
					PackageName = library.Name!,
					PackageDependency = packageDependency
				};
				mapEntry.Add(dependent);
				parentMap[packageDependency.Id] = mapEntry;
			}
		}

		return parentMap;
	}
}
