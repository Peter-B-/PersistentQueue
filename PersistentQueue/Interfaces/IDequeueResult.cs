using System;
using System.Collections.Generic;

namespace Persistent.Queue.Interfaces
{
    public interface IDequeueResult
    {
        IReadOnlyList<Memory<byte>> Data { get; }
        void Commit();
        void Reject();
    }
}