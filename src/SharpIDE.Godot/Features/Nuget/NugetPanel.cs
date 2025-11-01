using Godot;
using SharpIDE.Application.Features.Nuget;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;

namespace SharpIDE.Godot.Features.Nuget;

public partial class NugetPanel : Control
{
    private VBoxContainer _installedPackagesVboxContainer = null!;
    private VBoxContainer _implicitlyInstalledPackagesItemList = null!;
    private VBoxContainer _availablePackagesItemList = null!;
    
    private NugetPackageDetails _nugetPackageDetails = null!;
    
    public SharpIdeSolutionModel? Solution { get; set; }
    
    [Inject] private readonly NugetClientService _nugetClientService = null!;
    
    private readonly PackedScene _packageEntryScene = ResourceLoader.Load<PackedScene>("uid://cqc2xlt81ju8s");
    
    private IdePackageResult? _selectedPackage;

    public override void _Ready()
    {
        _installedPackagesVboxContainer = GetNode<VBoxContainer>("%InstalledPackagesVBoxContainer");
        _implicitlyInstalledPackagesItemList = GetNode<VBoxContainer>("%ImplicitlyInstalledPackagesVBoxContainer");
        _availablePackagesItemList = GetNode<VBoxContainer>("%AvailablePackagesVBoxContainer");
        _nugetPackageDetails = GetNode<NugetPackageDetails>("%NugetPackageDetails");
        _nugetPackageDetails.Visible = false;

        _ = Task.GodotRun(async () =>
        {
            await Task.Delay(300);
            var result = await _nugetClientService.GetTop100Results(Solution!.DirectoryPath);
            ;
            await this.InvokeAsync(() => _availablePackagesItemList.QueueFreeChildren());
            var scenes = result.Select(s =>
            {
                var scene = _packageEntryScene.Instantiate<PackageEntry>();
                scene.PackageResult = s;
                scene.PackageSelected += OnPackageSelected;
                return scene;
            }).ToList();
            await this.InvokeAsync(() =>
            {
                foreach (var scene in scenes)
                {
                    _availablePackagesItemList.AddChild(scene);
                }
            });
        });
    }

    private async Task OnPackageSelected(IdePackageResult packageResult)
    {
        _selectedPackage = packageResult;
        await _nugetPackageDetails.SetPackage(packageResult);
    }
}