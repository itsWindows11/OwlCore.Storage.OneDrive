using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A folder implementation that interacts with a folder in OneDrive.
/// </summary>
public class OneDriveFolder : IChildFolder, IGetItem, IGetItemRecursive, IGetRoot
{
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

        var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var result = await _graphClient.Drives[drive.Id].Items[Id].Children.GetAsync(cancellationToken: cancellationToken);

        foreach (var item in result.Value)
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
            var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
            var driveItem = await _graphClient.Drives[drive.Id].Items[id].GetAsync(cancellationToken: cancellationToken);

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

        var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var parentDriveItem = await _graphClient.Drives[drive.Id].Items[DriveItem.ParentReference.Id].GetAsync(cancellationToken: cancellationToken);

        return new OneDriveFolder(_graphClient, parentDriveItem);
    }

    /// <inheritdoc />
    public async Task<IFolder?> GetRootAsync(CancellationToken cancellationToken = default)
    {
        if (DriveItem.Root is null)
            return null;

        var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var rootDriveItem = await _graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);

        return new OneDriveFolder(_graphClient, rootDriveItem);
    }
}