using Persistent.Queue.Interfaces;

namespace Persistent.Queue.DataObjects;

internal class DequeueResult : IDequeueResult
{
    private readonly Action<ItemRange> _commitCallBack;
    private readonly ItemRange _itemRange;
    private readonly Action<ItemRange> _rejectCallBack;

    public DequeueResult(List<Memory<byte>> data, ItemRange itemRange, Action<ItemRange> commitCallBack, Action<ItemRange> rejectCallBack)
    {
        _itemRange = itemRange;
        _commitCallBack = commitCallBack;
        _rejectCallBack = rejectCallBack;
        Items = data;
    }

    public void Commit()
    {
        _commitCallBack(_itemRange);
    }

    public IReadOnlyList<Memory<byte>> Items { get; }

    public void Reject()
    {
        _rejectCallBack(_itemRange);
    }
}
