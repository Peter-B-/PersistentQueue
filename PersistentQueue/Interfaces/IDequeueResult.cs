namespace Persistent.Queue.Interfaces;

public interface IDequeueResult
{
    IReadOnlyList<ReadOnlyMemory<byte>> Items { get; }
    void Commit();
    void Reject();
}
