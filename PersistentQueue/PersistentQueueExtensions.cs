namespace Persistent.Queue;

public static class PersistentQueueExtensions
{
    public static PersistentQueue CreateQueue(this PersistentQueueConfiguration config) => new PersistentQueue(config);
}