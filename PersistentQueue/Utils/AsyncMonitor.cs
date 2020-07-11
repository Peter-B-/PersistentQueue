using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PersistentQueue.Utils
{
    public sealed class AsyncMonitor<T>
    {
        private TaskCompletionSource<T> _currentTcs;

        public AsyncMonitor()
        {
            _currentTcs = new TaskCompletionSource<T>();
        }

        public void Pulse(T data)
        {
            Interlocked.Exchange(ref _currentTcs, new TaskCompletionSource<T>()).TrySetResult(data);
        }

        public Task<T> WaitOne()
        {
            return _currentTcs.Task;
        }
    }
}