using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests.Enqueue;

[TestFixture]
public class TaskBehaviour
{
    [Test]
    public async Task DequeueMustNotContinueOnEnqueueThread()
    {
        //Arrange
        using var queue = new UnitTestPersistentQueue();

        var enqueueThread = Environment.CurrentManagedThreadId;

        //Act
        var dequeueTask = GetThreadIdAfterDequeue(queue);

        queue.EnqueueMany(1);

        var dequeueThread = await dequeueTask;

        //Assert
        dequeueThread.ShouldNotBe(enqueueThread);
    }

    private static async Task<int> GetThreadIdAfterDequeue(UnitTestPersistentQueue queue)
    {
        var res = await queue.DequeueAsync(1, 10);
        res.Commit();
        return Environment.CurrentManagedThreadId;
    }
}
