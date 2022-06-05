using NUnit.Framework;

namespace PersistentQueue.Tests;

public class UnitTestPersistentQueue : Persistent.Queue.PersistentQueue
{
    public UnitTestPersistentQueue(bool hasMaxSize = false) :
        this(new UnitTestQueueConfiguration
        {
            MaxDequeueBatchSizeInBytes = hasMaxSize ? 16 << 10 : null
        })
    {
    }

    public UnitTestPersistentQueue(UnitTestQueueConfiguration configuration) : base(configuration)
    {
        TestContext.WriteLine("Using path " + configuration.QueuePath);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            DeleteQueue();
    }

    private void DeleteQueue()
    {
        TestContext.WriteLine("Delete " + Configuration.QueuePath);
        Directory.Delete(Configuration.QueuePath, true);
    }
}
