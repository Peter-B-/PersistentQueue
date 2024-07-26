namespace Persistent.Queue.DataObjects;

internal readonly struct ItemRange(long headIndex, int itemCount)
{
    public readonly long HeadIndex = headIndex;
    public readonly int ItemCount = itemCount;
}
