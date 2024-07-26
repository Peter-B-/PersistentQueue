namespace Persistent.Queue.Utils;

public sealed class QueueState(long tailIndex) : IQueueState
{
    private readonly TaskCompletionSource<IQueueState> _updateTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<IQueueState> NextUpdate => _updateTcs.Task;

    public long TailIndex { get; } = tailIndex;

    public void Update(IQueueState newState)
    {
        _updateTcs.TrySetResult(newState);
    }
}
