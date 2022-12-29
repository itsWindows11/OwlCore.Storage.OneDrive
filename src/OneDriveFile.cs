using Microsoft.Graph;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A file implementation that interacts with a file in OneDrive.
/// </summary>
public class OneDriveFile : IFile, IAddressableFile
{
    private readonly GraphServiceClient _graphClient;
    private readonly DriveItem _driveItem;

    /// <summary>
    /// Creates a new instance of <see cref="OneDriveFile"/>.
    /// </summary>
    public OneDriveFile(GraphServiceClient graphClient, DriveItem driveItem)
    {
        _graphClient = graphClient;
        _driveItem = driveItem;
    }

    /// <inheritdoc />
    public string Id => _driveItem.Id;

    /// <inheritdoc />
    public string Name => _driveItem.Name;

    /// <inheritdoc />
    public string Path => throw new System.NotImplementedException();

    /// <inheritdoc />
    public virtual async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        var parent = await _graphClient.Drive.Items[_driveItem.ParentReference.DriveId].Request().GetAsync(cancellationToken);

        return new OneDriveFolder(_graphClient, parent);
    }

    /// <inheritdoc />
    public Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        return _graphClient.Drive.Items[_driveItem.Id].Content.Request().GetAsync(cancellationToken);
    }
}
