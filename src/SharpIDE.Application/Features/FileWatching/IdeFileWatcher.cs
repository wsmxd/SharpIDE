using FileWatcherEx;
using Microsoft.Extensions.FileSystemGlobbing;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Application.Features.FileWatching;

public sealed class IdeFileWatcher : IDisposable
{
	private Matcher? _matcher;
	private FileSystemWatcherEx? _fileWatcher;
	private SharpIdeSolutionModel? _solution;

	public void StartWatching(SharpIdeSolutionModel solution)
	{
		_solution = solution;

		var matcher = new Matcher();
		//matcher.AddIncludePatterns(["**/*.cs", "**/*.csproj", "**/*.sln"]);
		matcher.AddIncludePatterns(["**/*"]);
		matcher.AddExcludePatterns(["**/bin", "**/obj", "**/node_modules", "**/.vs", "**/.git", "**/.idea", "**/.vscode"]);
		_matcher = matcher;

		var fileWatcher = new FileSystemWatcherEx();
		fileWatcher.FolderPath = solution.DirectoryPath;
		//fileWatcher.Filters.AddRange(["*"]);
		fileWatcher.IncludeSubdirectories = true;
		fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
		fileWatcher.OnChanged += OnEvent;
		fileWatcher.OnCreated += OnEvent;
		fileWatcher.OnDeleted += OnEvent;
		fileWatcher.OnRenamed += OnEvent;
		fileWatcher.OnError += static (s, e) => Console.WriteLine($"FileSystemWatcher: Error - {e.GetException().Message}");

		fileWatcher.Start();
		_fileWatcher = fileWatcher;
	}

	public void StopWatching()
	{
		if (_fileWatcher is not null)
		{
			_fileWatcher.Stop();
			_fileWatcher.Dispose();
			_fileWatcher = null!;
		}
	}

	// TODO: Put events on a queue and process them in the background to avoid filling the buffer? FileSystemWatcherEx might already handle this
	private void OnEvent(object? sender, FileChangedEvent e)
	{
		var matchResult = _matcher!.Match(_solution!.DirectoryPath, e.FullPath);
		if (!matchResult.HasMatches) return;
		switch (e.ChangeType)
		{
			case ChangeType.CHANGED: HandleChanged(e.FullPath); break;
			case ChangeType.CREATED: HandleCreated(e.FullPath); break;
			case ChangeType.DELETED: HandleDeleted(e.FullPath); break;
			case ChangeType.RENAMED: HandleRenamed(e.OldFullPath, e.FullPath); break;
			default: throw new ArgumentOutOfRangeException();
		}
	}

	private void HandleRenamed(string? oldFullPath, string fullPath)
	{

		Console.WriteLine($"FileSystemWatcher: Renamed - {oldFullPath}, {fullPath}");
	}

	private void HandleDeleted(string fullPath)
	{
		Console.WriteLine($"FileSystemWatcher: Deleted - {fullPath}");
	}

	private void HandleCreated(string fullPath)
	{
		Console.WriteLine($"FileSystemWatcher: Created - {fullPath}");
	}

	// The only changed event we care about is files, not directories
	// We will naively assume that if the file name does not have an extension, it's a directory
	// This may not always be true, but it lets us avoid reading the file system to check
	// TODO: Make a note to users that they should not use files without extensions
	private void HandleChanged(string fullPath)
	{
		if (Path.HasExtension(fullPath) is false) return;
		// TODO: Handle updating the content of open files in editors
		Console.WriteLine($"FileSystemWatcher: Changed - {fullPath}");
	}

	public void Dispose()
	{
		StopWatching();
	}
}
