using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OwlCore.Storage;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A read-only storage property for the last accessed timestamp of a OneDrive item.
/// </summary>
public class OneDriveLastAccessedAtOffsetProperty : SimpleStorageProperty<DateTimeOffset?>, ILastAccessedAtOffsetProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="OneDriveLastAccessedAtOffsetProperty"/>.
    /// </summary>
    /// <param name="owner">The owning storable item.</param>
    /// <param name="client">The Graph service client.</param>
    /// <param name="itemId">The ID of the item.</param>
    public OneDriveLastAccessedAtOffsetProperty(IStorable owner, GraphServiceClient client, string itemId)
        : base(
            id: owner.Id + "/" + nameof(ILastAccessedAtOffset.LastAccessedAtOffset),
            name: nameof(ILastAccessedAtOffset.LastAccessedAtOffset),
            asyncGetter: async ct =>
            {
                var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
                var item = await client.Drives[drive!.Id].Items[itemId].GetAsync(cancellationToken: ct);
                return item?.FileSystemInfo?.LastAccessedDateTime;
            })
    {
    }
}