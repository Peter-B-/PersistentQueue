using System;
using System.Threading.Tasks;

namespace PersistentQueue.Interfaces
{
    public interface IPersistentQueue : IDisposable
    {
        bool HasItems { get; }
        void Enqueue(ReadOnlySpan<byte> itemData);
        Task<IDequeueResult> DequeueAsync(int maxElements, int minElements = 1);
    }
}