using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OwlCore.Storage;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A modifiable storage property for the creation timestamp of a OneDrive item.
/// </summary>
public class OneDriveCreatedAtProperty : SimpleModifiableStorageProperty<DateTime?>, IModifiableCreatedAtProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="OneDriveCreatedAtProperty"/>.
    /// </summary>
    /// <param name="owner">The owning storable item.</param>
    /// <param name="client">The Graph service client.</param>
    /// <param name="itemId">The ID of the item.</param>
    public OneDriveCreatedAtProperty(IStorable owner, GraphServiceClient client, string itemId)
        : base(
            id: owner.Id + "/" + nameof(ICreatedAt.CreatedAt),
            name: nameof(ICreatedAt.CreatedAt),
            asyncGetter: async ct =>
            {
                var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
                var item = await client.Drives[drive!.Id].Items[itemId].GetAsync(cancellationToken: ct);
                return item?.CreatedDateTime?.LocalDateTime;
            },
            asyncSetter: async (val, ct) =>
            {
                // CreatedDateTime is read-only on BaseItem but can be set via FileSystemInfo for preservation purposes during migration
                // However, the interface expects us to try and set it.
                // Graph API prioritizes FileSystemInfo for setting timestamps.
                var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
                var updateItem = new DriveItem
                {
                    FileSystemInfo = new FileSystemInfo
                    {
                        CreatedDateTime = val.HasValue ? new DateTimeOffset(val.Value) : null
                    }
                };
                await client.Drives[drive!.Id].Items[itemId].PatchAsync(updateItem, cancellationToken: ct);
            })
    {
    }
}