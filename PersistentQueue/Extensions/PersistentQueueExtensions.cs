namespace Persistent.Queue.Extensions
{
    public static class PersistentQueueExtensions
    {
        public static PersistentQueue Create(this PersistentQueueConfiguration config)
        {
            return new PersistentQueue(config);
        }
    }
}