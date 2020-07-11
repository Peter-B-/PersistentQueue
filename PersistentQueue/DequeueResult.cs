using System;
using System.Collections.Generic;

namespace PersistentQueue
{
    class DequeueResult : IDequeueResult
    {
        public DequeueResult(IReadOnlyList<Memory<byte>> data)
        {
            Data = data;
        }

        public IReadOnlyList<Memory<byte>> Data { get; }
        public void Commit()
        {
            throw new NotImplementedException();
        }

        public void Reject()
        {
            throw new NotImplementedException();
        }
    }
}