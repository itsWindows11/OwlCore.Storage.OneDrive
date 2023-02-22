using Microsoft.Graph;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A folder implementation that interacts with a folder in OneDrive.
/// </summary>
public class OneDriveFolder : IChildFolder, IFastGetItem, IFastGetItemRecursive
{
    private const string EXPAND_STRING = "children";
    private readonly GraphServiceClient _graphClient;

    /// <summary>
    /// Creates a new instance of <see cref="OneDriveFolder"/>.
    /// </summary>
    public OneDriveFolder(GraphServiceClient graphClient, DriveItem driveItem)
    {
        _graphClient = graphClient;
        DriveItem = driveItem;
    }

    /// <inheritdoc />
    public string Id => DriveItem.Id;

    /// <inheritdoc />
    public string Name => DriveItem.Name;

    /// <summary>
    /// The graph item that was provided as the backing implementation for this file.
    /// </summary>
    public DriveItem DriveItem { get; }

    /// <inheritdoc />
    public virtual async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var driveItem = await _graphClient.Drive.Items[Id].Request().Expand(EXPAND_STRING).GetAsync(cancellationToken);

        foreach (var item in driveItem.Children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item.Folder is not null && type.HasFlag(StorableType.Folder))
                yield return new OneDriveFolder(_graphClient, item);

            if (item.File is not null && type.HasFlag(StorableType.File))
                yield return new OneDriveFile(_graphClient, item);
        }
    }

    /// <inheritdoc />
    public Task<IStorableChild> GetItemRecursiveAsync(string id, CancellationToken cancellationToken = default) => GetItemAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<IStorableChild> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var driveItem = await _graphClient.Drive.Items[id].Request().GetAsync(cancellationToken);

            if (driveItem?.Folder is not null)
                return new OneDriveFolder(_graphClient, driveItem);

            if (driveItem?.File is not null)
                return new OneDriveFile(_graphClient, driveItem);
        }
        catch
        {
            // ignored
        }

        throw new FileNotFoundException();
    }

    /// <inheritdoc />
    public virtual async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        if (DriveItem.ParentReference is null)
            return null;

        var parentDriveItem = await _graphClient.Drive.Items[DriveItem.ParentReference.Id].Request().Expand(EXPAND_STRING).GetAsync(cancellationToken);

        return new OneDriveFolder(_graphClient, parentDriveItem);
    }
}