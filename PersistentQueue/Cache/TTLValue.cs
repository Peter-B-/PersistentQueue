using System;

namespace Persistent.Queue.Cache
{
    internal class TTLValue
    {
        public DateTime LastAccessTimestamp;
        public long RefCount;
    }
}
