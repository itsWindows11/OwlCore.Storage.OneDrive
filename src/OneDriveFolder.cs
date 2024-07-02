using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.Copy;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A folder implementation that interacts with a folder in OneDrive.
/// </summary>
public class OneDriveFolder :
    IModifiableFolder,
    IChildFolder,
    IMoveFrom,
    ICreateCopyOf,
    IGetItem,
    IGetItemRecursive,
    IGetRoot
{
    private readonly GraphServiceClient _graphClient;
    private Drive? _drive;
    private string? _path;

    /// <summary>
    /// Creates a new instance of <see cref="OneDriveFolder"/>.
    /// </summary>
    public OneDriveFolder(GraphServiceClient graphClient, Drive drive, DriveItem driveItem)
        : this(graphClient, driveItem)
    {
        _drive = drive;
    }

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

    /// <summary>
    /// The path to this folder.
    /// </summary>
    public string Path => _path ??= IOPath.Combine(DriveItem.ParentReference.Path, Name);

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

        _drive ??= await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var result = await _graphClient.Drives[_drive.Id].Items[Id].Children.GetAsync(cancellationToken: cancellationToken);

        foreach (var item in result.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item.Folder is not null && type.HasFlag(StorableType.Folder))
                yield return new OneDriveFolder(_graphClient, _drive, item);

            if (item.File is not null && type.HasFlag(StorableType.File))
                yield return new OneDriveFile(_graphClient, _drive, item);
        }
    }

    /// <inheritdoc />
    public Task<IStorableChild> GetItemRecursiveAsync(string id, CancellationToken cancellationToken = default) => GetItemAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<IStorableChild> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _drive ??= await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
            var driveItem = await _graphClient.Drives[_drive.Id].Items[id].GetAsync(cancellationToken: cancellationToken);

            if (driveItem?.Folder is not null)
                return new OneDriveFolder(_graphClient, _drive, driveItem);

            if (driveItem?.File is not null)
                return new OneDriveFile(_graphClient, _drive, driveItem);
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

        _drive ??= await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var parentDriveItem = await _graphClient.Drives[_drive.Id].Items[DriveItem.ParentReference.Id].GetAsync(cancellationToken: cancellationToken);

        return new OneDriveFolder(_graphClient, _drive, parentDriveItem);
    }

    /// <inheritdoc />
    public async Task<IFolder?> GetRootAsync(CancellationToken cancellationToken = default)
    {
        if (DriveItem.Root is null)
            return null;

        _drive ??= await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var rootDriveItem = await _graphClient.Drives[_drive.Id].Root.GetAsync(cancellationToken: cancellationToken);

        return new OneDriveFolder(_graphClient, _drive, rootDriveItem);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
    {
        _drive ??= await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        await _graphClient.Drives[_drive.Id].Items[item.Id].DeleteAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        _drive ??= await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);

        var folder = new DriveItem
        {
            Name = name,
            Folder = new Folder(),
            AdditionalData = new Dictionary<string, object>
            {
                {
                    "@microsoft.graph.conflictBehavior" , overwrite ? "replace" : "fail"
                },
            },
        };

        try
        {
            var createdFolder = await _graphClient.Drives[_drive.Id].Items[Id].Children.PostAsync(folder, cancellationToken: cancellationToken);
            return new OneDriveFolder(_graphClient, _drive, createdFolder);
        }
        catch
        {
            var item = await GetItemsAsync(StorableType.Folder, cancellationToken)
                .FirstOrDefaultAsync(folder => folder.Name == name && folder is IChildFolder, cancellationToken);
            return (IChildFolder?)item ?? throw new FileNotFoundException();
        }
    }

    /// <inheritdoc />
    public async Task<IChildFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        _drive ??= await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);

        var file = new DriveItem
        {
            Name = name,
            File = new FileObject(),
            AdditionalData = new Dictionary<string, object>
            {
                {
                    "@microsoft.graph.conflictBehavior" , overwrite ? "replace" : "fail"
                },
            },
        };

        try
        {
            var createdFolder = await _graphClient.Drives[_drive.Id].Items[Id].Children.PostAsync(file, cancellationToken: cancellationToken);
            return new OneDriveFile(_graphClient, _drive, createdFolder);
        }
        catch
        {
            var item = await GetItemsAsync(StorableType.Folder, cancellationToken)
                .FirstOrDefaultAsync(file => file.Name == name && file is IChildFile, cancellationToken);
            return (IChildFile?)item ?? throw new FileNotFoundException();
        }
    }

    /// <inheritdoc />
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Cannot watch OneDrive folders.");
    }

    /// <inheritdoc />
    public async Task<IChildFile> MoveFromAsync(IChildFile fileToMove, IModifiableFolder source, bool overwrite, CancellationToken cancellationToken, MoveFromDelegate fallback)
    {
        if (fileToMove is not OneDriveFile)
            await fallback(this, fileToMove, source, overwrite, cancellationToken);

        _drive ??= await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);

        var copyBody = new CopyPostRequestBody
        {
            Name = fileToMove.Name,
            ParentReference = new ItemReference
            {
                Id = Id,
            },
        };

        var newItem = await _graphClient.Drives[_drive.Id].Items[fileToMove.Id].Copy.PostAsync(
            copyBody,
            cancellationToken: cancellationToken
        );

        await DeleteAsync(fileToMove, cancellationToken);

        return new OneDriveFile(_graphClient, _drive, newItem);
    }

    /// <inheritdoc />
    public async Task<IChildFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite, CancellationToken cancellationToken, CreateCopyOfDelegate fallback)
    {
        if (fileToCopy is not OneDriveFile)
            await fallback(this, fileToCopy, overwrite, cancellationToken);

        _drive ??= await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);

        var copyBody = new CopyPostRequestBody
        {
            Name = fileToCopy.Name,
            ParentReference = new ItemReference
            {
                Id = Id,
            },
        };

        var newItem = await _graphClient.Drives[_drive.Id].Items[fileToCopy.Id].Copy.PostAsync(
            copyBody,
            cancellationToken: cancellationToken
        );

        return new OneDriveFile(_graphClient, _drive, newItem);
    }
}