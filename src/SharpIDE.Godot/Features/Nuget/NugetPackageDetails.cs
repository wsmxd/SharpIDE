using Godot;
using SharpIDE.Application.Features.Nuget;

namespace SharpIDE.Godot.Features.Nuget;

public partial class NugetPackageDetails : VBoxContainer
{
    private TextureRect _packageIconTextureRect = null!;
    private Label _packageNameLabel = null!;

    private IdePackageResult? _package;
    
    [Inject] private readonly NugetPackageIconCacheService _nugetPackageIconCacheService = null!;
    public override void _Ready()
    {
        _packageIconTextureRect = GetNode<TextureRect>("%PackageIconTextureRect");
        _packageNameLabel = GetNode<Label>("%PackageNameLabel");
    }
    
    public async Task SetPackage(IdePackageResult package)
    {
        _package = package;
        var iconTask = _nugetPackageIconCacheService.GetNugetPackageIcon(_package.PackageId, _package.PackageFromSources.First().PackageSearchMetadata.IconUrl);
        await this.InvokeAsync(() =>
        {
            _packageNameLabel.Text = package.PackageId;
            Visible = true;
        });
        var (iconBytes, iconFormat) = await iconTask;
        var imageTexture = ImageTextureHelper.GetImageTextureFromBytes(iconBytes, iconFormat);
        if (imageTexture is not null)
        {
            await this.InvokeAsync(() => _packageIconTextureRect.Texture = imageTexture);
        }
    }
}