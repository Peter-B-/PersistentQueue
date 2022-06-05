using System;
using System.Collections.Generic;

namespace Persistent.Queue.Interfaces;

public interface IDequeueResult
{
    IReadOnlyList<Memory<byte>> Items { get; }
    void Commit();
    void Reject();
}