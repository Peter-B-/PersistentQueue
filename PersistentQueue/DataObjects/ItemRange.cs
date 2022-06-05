namespace Persistent.Queue.DataObjects;

internal readonly struct ItemRange
{
    public readonly long HeadIndex;
    public readonly int ItemCount;

    public ItemRange(long headIndex, int itemCount)
    {
        HeadIndex = headIndex;
        ItemCount = itemCount;
    }
}
