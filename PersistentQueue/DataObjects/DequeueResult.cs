using Persistent.Queue.Interfaces;

namespace Persistent.Queue.DataObjects;

internal class DequeueResult(List<Memory<byte>> data, ItemRange itemRange, Action<ItemRange> commitCallBack, Action<ItemRange> rejectCallBack)
    : IDequeueResult
{
    public IReadOnlyList<Memory<byte>> Items { get; } = data;

    public void Commit()
    {
        commitCallBack(itemRange);
    }

    public void Reject()
    {
        rejectCallBack(itemRange);
    }
}
