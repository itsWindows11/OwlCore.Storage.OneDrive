using System;

namespace OwlCore.Storage.OneDrive.Internal;

internal readonly struct RangeData(int offset, int count) : IComparable<RangeData>
{
    public int Offset => offset;

    public int Count => count;

    public int End => Offset + Count;

    public int CompareTo(RangeData other)
        => Offset.CompareTo(other.Offset);

    public bool OverlapsOrAdjacent(RangeData other)
        => Offset <= other.End && End >= other.Offset;

    public RangeData Merge(RangeData other)
    {
        var newOffset = Math.Min(Offset, other.Offset);

        return new RangeData(
            newOffset,
            Math.Max(End, other.End) - newOffset
        );
    }
}