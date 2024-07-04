using System.Collections.Concurrent;

namespace OwlCore.Storage.OneDrive.Internal;

internal static class ConcurrentQueueExtensions
{
    internal static void Clear<T>(this ConcurrentQueue<T> queue)
    {
        while (!queue.IsEmpty)
        {
            queue.TryDequeue(out _);
        }
    }
}