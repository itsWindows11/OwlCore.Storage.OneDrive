using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OwlCore.Storage;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A modifiable storage property for the last modified timestamp of a OneDrive item.
/// </summary>
public class OneDriveLastModifiedAtProperty : SimpleModifiableStorageProperty<DateTime?>, IModifiableLastModifiedAtProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="OneDriveLastModifiedAtProperty"/>.
    /// </summary>
    /// <param name="owner">The owning storable item.</param>
    /// <param name="client">The Graph service client.</param>
    /// <param name="itemId">The ID of the item.</param>
    public OneDriveLastModifiedAtProperty(IStorable owner, GraphServiceClient client, string itemId)
        : base(
            id: owner.Id + "/" + nameof(ILastModifiedAt.LastModifiedAt),
            name: nameof(ILastModifiedAt.LastModifiedAt),
            asyncGetter: async ct =>
            {
                var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
                var item = await client.Drives[drive!.Id].Items[itemId].GetAsync(cancellationToken: ct);
                return item?.LastModifiedDateTime?.LocalDateTime;
            },
            asyncSetter: async (val, ct) =>
            {
                var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
                var updateItem = new DriveItem
                {
                    FileSystemInfo = new FileSystemInfo
                    {
                        LastModifiedDateTime = val.HasValue ? new DateTimeOffset(val.Value) : null
                    }
                };
                await client.Drives[drive!.Id].Items[itemId].PatchAsync(updateItem, cancellationToken: ct);
            })
    {
    }
}