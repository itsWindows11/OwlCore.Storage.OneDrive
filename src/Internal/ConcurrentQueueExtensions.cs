using System.Collections.Concurrent;
using System.Linq;

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

    internal static void EnqueueRangeItem(this ConcurrentQueue<RangeData> ranges, RangeData newRange)
    {
        var rangesList = ranges.ToList();
        ranges.Clear();

        foreach (var range in rangesList)
        {
            if (range.OverlapsOrAdjacent(newRange))
                newRange = newRange.Merge(range);
            else
                ranges.Enqueue(range);
        }

        ranges.Enqueue(newRange);
    }
}