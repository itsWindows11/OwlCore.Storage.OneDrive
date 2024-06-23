using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;
using Nerdbank.Streams;
using System.IO;
using System.Net.Http;
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
        var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var parent = await _graphClient.Drives[drive.Id].Items[DriveItem.ParentReference.Id].GetAsync(cancellationToken: cancellationToken);

        return new OneDriveFolder(_graphClient, parent);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var readStream = await _graphClient.Drives[drive.Id].Items[Id].Content.GetAsync(cancellationToken: cancellationToken);
        
        var uploadBody = new CreateUploadSessionPostRequestBody()
        {
            Item = new DriveItemUploadableProperties()
        };

        var uploadSession = await _graphClient.Drives[drive.Id].Items[Id].CreateUploadSession.PostAsync(uploadBody, cancellationToken: cancellationToken);
        
        return FullDuplexStream.Splice(readStream, new OneDriveWriteStream(uploadSession));
    }
}
