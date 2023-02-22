using Microsoft.Graph;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A file implementation that interacts with a file in OneDrive.
/// </summary>
public class OneDriveFile : IFile, IChildFile
{
    private readonly GraphServiceClient _graphClient;

    /// <summary>
    /// Creates a new instance of <see cref="OneDriveFile"/>.
    /// </summary>
    public OneDriveFile(GraphServiceClient graphClient, DriveItem driveItem)
    {
        _graphClient = graphClient;
        DriveItem = driveItem;
    }

    /// <summary>
    /// The graph item that was provided as the backing implementation for this file.
    /// </summary>
    public DriveItem DriveItem { get; }

    /// <inheritdoc />
    public string Id => DriveItem.Id;

    /// <inheritdoc />
    public string Name => DriveItem.Name;

    /// <inheritdoc />
    public virtual async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        var parent = await _graphClient.Drive.Items[DriveItem.ParentReference.DriveId].Request().GetAsync(cancellationToken);

        return new OneDriveFolder(_graphClient, parent);
    }

    /// <inheritdoc />
    public Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        return _graphClient.Drive.Items[DriveItem.Id].Content.Request().GetAsync(cancellationToken);
    }
}
