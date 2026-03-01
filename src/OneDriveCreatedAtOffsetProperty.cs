using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OwlCore.Storage;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A modifiable storage property for the creation timestamp of a OneDrive item with offset.
/// </summary>
public class OneDriveCreatedAtOffsetProperty : SimpleModifiableStorageProperty<DateTimeOffset?>, IModifiableCreatedAtOffsetProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="OneDriveCreatedAtOffsetProperty"/>.
    /// </summary>
    /// <param name="owner">The owning storable item.</param>
    /// <param name="client">The Graph service client.</param>
    /// <param name="itemId">The ID of the item.</param>
    public OneDriveCreatedAtOffsetProperty(IStorable owner, GraphServiceClient client, string itemId)
        : base(
            id: owner.Id + "/" + nameof(ICreatedAtOffset.CreatedAtOffset),
            name: nameof(ICreatedAtOffset.CreatedAtOffset),
            asyncGetter: async ct =>
            {
                var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
                var item = await client.Drives[drive!.Id].Items[itemId].GetAsync(cancellationToken: ct);
                return item?.CreatedDateTime;
            },
            asyncSetter: async (val, ct) =>
            {
                var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
                var updateItem = new DriveItem
                {
                    FileSystemInfo = new FileSystemInfo
                    {
                        CreatedDateTime = val
                    }
                };
                await client.Drives[drive!.Id].Items[itemId].PatchAsync(updateItem, cancellationToken: ct);
            })
    {
    }
}