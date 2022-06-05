namespace Persistent.Queue.Utils;

public interface IQueueState
{
    Task<IQueueState> NextUpdate { get; }
    long TailIndex { get; }
}
