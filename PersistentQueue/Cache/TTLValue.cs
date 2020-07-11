namespace PersistentQueue.Cache
{
    internal class TTLValue
    {
        public long LastAccessTimestamp;
        public long RefCount;
    }
}