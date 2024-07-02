using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;
using Nerdbank.Streams;
using System;
using System.IO;
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

        switch (accessMode)
        {
            case FileAccess.Read:
                return await _graphClient.Drives[_drive.Id].Items[Id].Content.GetAsync(cancellationToken: cancellationToken);
            case FileAccess.Write:
                {
                    var uploadBody = new CreateUploadSessionPostRequestBody()
                    {
                        Item = new DriveItemUploadableProperties()
                    };

                    var uploadSession = await _graphClient
                        .Drives[_drive.Id]
                        .Items[Id]
                        .CreateUploadSession
                        .PostAsync(uploadBody, cancellationToken: cancellationToken);

                    return new OneDriveWriteStream(uploadSession);
                }
            case FileAccess.ReadWrite:
                {
                    var uploadBody = new CreateUploadSessionPostRequestBody()
                    {
                        Item = new DriveItemUploadableProperties()
                    };

                    return await OpenFullDuplexStreamAsync(_drive, uploadBody, cancellationToken);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(accessMode));
        }
    }

    private async Task<Stream> OpenFullDuplexStreamAsync(Drive drive, CreateUploadSessionPostRequestBody uploadBody, CancellationToken cancellationToken)
    {
        var readStream = await _graphClient
            .Drives[drive.Id]
            .Items[Id]
            .Content
            .GetAsync(cancellationToken: cancellationToken);

        var uploadSession = await _graphClient
            .Drives[drive.Id]
            .Items[Id]
            .CreateUploadSession
            .PostAsync(uploadBody, cancellationToken: cancellationToken);

        return FullDuplexStream.Splice(readStream, new OneDriveWriteStream(uploadSession));
    }
}
