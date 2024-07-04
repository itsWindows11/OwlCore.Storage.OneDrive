using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A file implementation that interacts with a file in OneDrive.
/// </summary>
public class OneDriveFile : IFile, IChildFile
{
    private readonly GraphServiceClient _graphClient;
    private Drive? _drive;
    private string? _path;

    /// <summary>
    /// Creates a new instance of <see cref="OneDriveFile"/>.
    /// </summary>
    public OneDriveFile(GraphServiceClient graphClient, Drive drive, DriveItem driveItem)
        : this(graphClient, driveItem)
    {
        _drive = drive;
    }

    /// <summary>
    /// Creates a new instance of <see cref="OneDriveFile"/> with the current user's drive.
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

    /// <summary>
    /// The path to this file.
    /// </summary>
    public string Path => _path ??= IOPath.Combine(DriveItem.ParentReference.Path, Name);

    /// <inheritdoc />
    public string Name => DriveItem.Name;

    /// <inheritdoc />
    public virtual async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        _drive ??= await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var parent = await _graphClient.Drives[_drive.Id].Items[DriveItem.ParentReference.Id].GetAsync(cancellationToken: cancellationToken);

        return new OneDriveFolder(_graphClient, _drive, parent);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        _drive ??= await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);

        if (accessMode == FileAccess.Read)
        {
            var baseReadStream = await _graphClient.Drives[_drive.Id].Items[Id].Content.GetAsync(cancellationToken: cancellationToken);
            return new OneDriveFileStream(null, null, baseReadStream, DriveItem.Size.GetValueOrDefault(), accessMode);
        } else if (accessMode == FileAccess.Write || accessMode == FileAccess.ReadWrite)
        {
            var baseReadStream = await _graphClient.Drives[_drive.Id].Items[Id].Content.GetAsync(cancellationToken: cancellationToken);

            var uploadBody = new CreateUploadSessionPostRequestBody()
            {
                Item = new DriveItemUploadableProperties()
            };

            var uploadSession = await _graphClient
                .Drives[_drive.Id]
                .Items[Id]
                .CreateUploadSession
                .PostAsync(uploadBody, cancellationToken: cancellationToken);

            return new OneDriveFileStream(new HttpClient(), uploadSession, baseReadStream, DriveItem.Size.GetValueOrDefault(), accessMode);
        }

        throw new ArgumentOutOfRangeException(nameof(accessMode), "File access mode is not supported.");
    }
}
