using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A folder implementation that interacts with a folder in OneDrive.
/// </summary>
public class OneDriveFolder : IFolder, IAddressableFolder
{
    private const string EXPAND_STRING = "children";
    private readonly GraphServiceClient _graphClient;
    private readonly DriveItem _driveItem;

    /// <summary>
    /// Creates a new instance of <see cref="OneDriveFolder"/>.
    /// </summary>
    public OneDriveFolder(GraphServiceClient graphClient, DriveItem driveItem)
    {
        _graphClient = graphClient;
        _driveItem = driveItem;
    }

    /// <inheritdoc />
    public string Id => _driveItem.Id;

    /// <inheritdoc />
    public string Name => _driveItem.Name;

    /// <inheritdoc />
    public string Path => throw new NotImplementedException();

    /// <inheritdoc />
    public virtual async IAsyncEnumerable<IAddressableStorable> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var driveItem = await _graphClient.Drive.Items[Id].Request().Expand(EXPAND_STRING).GetAsync();

        foreach (var item in driveItem.Children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item.Folder is not null && type.HasFlag(StorableType.Folder))
                yield return new OneDriveFolder(_graphClient, driveItem);

            if (item.File is not null && type.HasFlag(StorableType.File))
                yield return new OneDriveFile(_graphClient, driveItem);
        }
    }

    /// <inheritdoc />
    public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        throw new NotImplementedException();
    }
}