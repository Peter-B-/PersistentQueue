using System;
using System.Threading;
using System.Threading.Tasks;

namespace Persistent.Queue.Interfaces
{
    public interface IPersistentQueue : IDisposable
    {
        bool HasItems { get; }
        void Enqueue(ReadOnlySpan<byte> itemData);
        Task<IDequeueResult> DequeueAsync(CancellationToken token = default);
        Task<IDequeueResult> DequeueAsync(int maxItems, int minItems, CancellationToken token = default);
    }
}
