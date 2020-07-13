namespace Persistent.Queue.Cache
{
    internal class TTLValue
    {
        public long LastAccessTimestamp;
        public long RefCount;
    }
}