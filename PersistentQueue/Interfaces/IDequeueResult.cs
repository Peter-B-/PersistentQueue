using System;
using System.Collections.Generic;

namespace PersistentQueue.Interfaces
{
    public interface IDequeueResult
    {
        IReadOnlyList<Memory<byte>> Data { get; }
        void Commit();
        void Reject();
    }
}