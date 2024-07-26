using Persistent.Queue.Interfaces;
using System;

namespace Persistent.Queue.DataObjects;

internal sealed class DequeueResult(IReadOnlyList<ReadOnlyMemory<byte>> data, ItemRange itemRange, Action<ItemRange> commitCallBack, Action<ItemRange> rejectCallBack)
    : IDequeueResult
{
    public IReadOnlyList<ReadOnlyMemory<byte>> Items { get; } = data;

    public void Commit()
    {
        commitCallBack(itemRange);
    }

    public void Reject()
    {
        rejectCallBack(itemRange);
    }
}
