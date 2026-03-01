using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OwlCore.Storage;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A modifiable storage property for the last modified timestamp of a OneDrive item with offset.
/// </summary>
public class OneDriveLastModifiedAtOffsetProperty : SimpleModifiableStorageProperty<DateTimeOffset?>, IModifiableLastModifiedAtOffsetProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="OneDriveLastModifiedAtOffsetProperty"/>.
    /// </summary>
    /// <param name="owner">The owning storable item.</param>
    /// <param name="client">The Graph service client.</param>
    /// <param name="itemId">The ID of the item.</param>
    public OneDriveLastModifiedAtOffsetProperty(IStorable owner, GraphServiceClient client, string itemId)
        : base(
            id: owner.Id + "/" + nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
            name: nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
            asyncGetter: async ct =>
            {
                var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
                var item = await client.Drives[drive!.Id].Items[itemId].GetAsync(cancellationToken: ct);
                return item?.LastModifiedDateTime;
            },
            asyncSetter: async (val, ct) =>
            {
                var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
                var updateItem = new DriveItem
                {
                    FileSystemInfo = new FileSystemInfo
                    {
                        LastModifiedDateTime = val
                    }
                };
                await client.Drives[drive!.Id].Items[itemId].PatchAsync(updateItem, cancellationToken: ct);
            })
    {
    }
}