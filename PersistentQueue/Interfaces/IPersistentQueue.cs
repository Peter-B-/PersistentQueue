namespace Persistent.Queue.Interfaces;

public interface IPersistentQueue : IDisposable
{
    bool HasItems { get; }
    Task<IDequeueResult> DequeueAsync(CancellationToken token = default);
    Task<IDequeueResult> DequeueAsync(int minItems, int maxItems, CancellationToken token = default);
    void Enqueue(ReadOnlySpan<byte> itemData);
}
