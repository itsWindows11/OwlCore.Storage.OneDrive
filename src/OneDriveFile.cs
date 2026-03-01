using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A file implementation that interacts with a file in OneDrive.
/// </summary>
public class OneDriveFile : IFile, IChildFile, ICreatedAtOffset, ILastAccessedAtOffset, ILastModifiedAtOffset
{
    private readonly GraphServiceClient _graphClient;
    private OneDriveCreatedAtProperty? _createdAt;
    private OneDriveCreatedAtOffsetProperty? _createdAtOffset;
    private OneDriveLastAccessedAtProperty? _lastAccessedAt;
    private OneDriveLastAccessedAtOffsetProperty? _lastAccessedAtOffset;
    private OneDriveLastModifiedAtProperty? _lastModifiedAt;
    private OneDriveLastModifiedAtOffsetProperty? _lastModifiedAtOffset;

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
    public string Id => DriveItem.Id!;

    /// <inheritdoc />
    public string Name => DriveItem.Name!;

    /// <inheritdoc />
    public ICreatedAtProperty CreatedAt => _createdAt ??= new OneDriveCreatedAtProperty(this, _graphClient, DriveItem.Id!);

    /// <inheritdoc />
    public ICreatedAtOffsetProperty CreatedAtOffset => _createdAtOffset ??= new OneDriveCreatedAtOffsetProperty(this, _graphClient, DriveItem.Id!);

    /// <inheritdoc />
    public ILastAccessedAtProperty LastAccessedAt => _lastAccessedAt ??= new OneDriveLastAccessedAtProperty(this, _graphClient, DriveItem.Id!);

    /// <inheritdoc />
    public ILastAccessedAtOffsetProperty LastAccessedAtOffset => _lastAccessedAtOffset ??= new OneDriveLastAccessedAtOffsetProperty(this, _graphClient, DriveItem.Id!);

    /// <inheritdoc />
    public ILastModifiedAtProperty LastModifiedAt => _lastModifiedAt ??= new OneDriveLastModifiedAtProperty(this, _graphClient, DriveItem.Id!);

    /// <inheritdoc />
    public ILastModifiedAtOffsetProperty LastModifiedAtOffset => _lastModifiedAtOffset ??= new OneDriveLastModifiedAtOffsetProperty(this, _graphClient, DriveItem.Id!);

    /// <inheritdoc />
    public virtual async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var parent = await _graphClient.Drives[drive!.Id].Items[DriveItem.ParentReference!.Id].GetAsync(cancellationToken: cancellationToken);

        return new OneDriveFolder(_graphClient, parent!);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        if (accessMode == 0 || (int)accessMode > 3)
            throw new ArgumentOutOfRangeException(nameof(accessMode));

        var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var result = await _graphClient.Drives[drive!.Id].Items[Id].Content.GetAsync(cancellationToken: cancellationToken);

        return result!;
    }
}