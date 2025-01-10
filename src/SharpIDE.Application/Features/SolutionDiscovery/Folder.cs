using System.Text.Json.Serialization;

namespace SharpIDE.Application.Features.SolutionDiscovery;

public class Folder
{
	public required string Path { get; set; }
	public string Name { get; set; } = null!;
	public List<Folder> Folders { get; set; } = [];
	public List<TreeMapFile> Files { get; set; } = [];
	public bool IsPseudoFolder { get; set; }
	public int Depth { get; set; }

	[JsonIgnore]
	public bool Expanded { get; set; }
}

public class TreeMapFile
{
	public required string Path { get; set; }
	public required string Name { get; set; }
}
