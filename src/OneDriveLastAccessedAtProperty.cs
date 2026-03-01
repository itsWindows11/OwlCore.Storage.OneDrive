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
public class OneDriveLastAccessedAtProperty : SimpleStorageProperty<DateTime?>, ILastAccessedAtProperty
{
    /// <summary>
    /// Creates a new instance of <see cref="OneDriveLastAccessedAtProperty"/>.
    /// </summary>
    /// <param name="owner">The owning storable item.</param>
    /// <param name="client">The Graph service client.</param>
    /// <param name="itemId">The ID of the item.</param>
    public OneDriveLastAccessedAtProperty(IStorable owner, GraphServiceClient client, string itemId)
        : base(
            id: owner.Id + "/" + nameof(ILastAccessedAt.LastAccessedAt),
            name: nameof(ILastAccessedAt.LastAccessedAt),
            asyncGetter: async ct =>
            {
                var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
                var item = await client.Drives[drive!.Id].Items[itemId].GetAsync(cancellationToken: ct);
                return item?.FileSystemInfo?.LastAccessedDateTime?.LocalDateTime;
            })
    {
    }
}