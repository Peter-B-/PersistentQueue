using System.Text;

namespace PersistentQueue.Tests;

public static class QueueExtensions
{
    public static void Enqueue(this Persistent.Queue.PersistentQueue queue, int itemNo)
    {
        var s = Encoding.UTF8.GetBytes($"Message {itemNo}");
        queue.Enqueue(s);
    }

    public static void EnqueueMany(this Persistent.Queue.PersistentQueue queue, int count, int start = 1)
    {
        Enumerable.Range(start, count)
            .ForEach(queue.Enqueue);
    }

    public static void EnqueueManySized(this Persistent.Queue.PersistentQueue queue, int count, int byteSize)
    {
        Enumerable.Range(1, count)
            .ForEach(_ => queue.Enqueue(new byte[byteSize]));
    }
}
