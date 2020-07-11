using System;
using System.Collections.Generic;

namespace PersistentQueue
{
    public interface IDequeueResult
    {
        IReadOnlyList<Memory<byte>> Data { get; }
        void Commit();
        void Reject();
    }
}