namespace Persistent.Queue.Utils;

public sealed class QueueState : IQueueState
{
    private readonly TaskCompletionSource<IQueueState> _updateTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public QueueState(long tailIndex)
    {
        TailIndex = tailIndex;
    }

    public Task<IQueueState> NextUpdate => _updateTcs.Task;

    public long TailIndex { get; }

    public void Update(IQueueState newState)
    {
        _updateTcs.TrySetResult(newState);
    }
}
