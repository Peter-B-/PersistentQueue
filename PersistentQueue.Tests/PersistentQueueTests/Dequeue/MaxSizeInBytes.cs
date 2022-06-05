using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests.Dequeue;

[TestFixture]
public class MaxSizeInBytes
{
    [TestCase(30, 8, 3)]
    [TestCase(31, 8, 3)]
    [TestCase(32, 8, 4)]
    [TestCase(33, 8, 4)]
    [TestCase(32, 2, 8)]
    public async Task LimitItemsOnSize(long maxSize, int itemSize, int expected)
    {
        //Arrange
        var config = new UnitTestQueueConfiguration
        {
            MaxDequeueBatchSizeInBytes = maxSize,
            MaxDequeueBatchSize = 8,
        };

        using var queue = new UnitTestPersistentQueue(config);

        queue.EnqueueManySized(10, itemSize);

        //Act
        var res = await queue.DequeueAsync();

        //Assert
        res.Items.Count.ShouldBe(expected);
    }

    [Test]
    public async Task EnqueueAndDequeueMany()
    {
        //Arrange
        var config = new UnitTestQueueConfiguration
        {
            MaxDequeueBatchSizeInBytes = 32,
            MaxDequeueBatchSize = 8,
            ThrowExceptionWhenItemExceedingMaxDequeueBatchSizeIsEnqueued = false
        };

        using var queue = new UnitTestPersistentQueue(config);

        var rand = new Random(1234);
        for (var i = 0; i < 20; i++)
        {
            queue.Enqueue(new byte[rand.Next(8, 64)]);
        }

        //Act
        var dequeued = 0;
        while (queue.HasItems)
        {
            var res = await queue.DequeueAsync();
            dequeued += res.Items.Count;
            res.Commit();
        }

        //Assert
        dequeued.ShouldBe(20);
    }


    [Test]
    public async Task ReturnAtLeastOne()
    {
        //Arrange
        var config = new UnitTestQueueConfiguration
        {
            MaxDequeueBatchSizeInBytes = 2,
            MaxDequeueBatchSize = 8,
            ThrowExceptionWhenItemExceedingMaxDequeueBatchSizeIsEnqueued = false
        };

        using var queue = new UnitTestPersistentQueue(config);

        queue.EnqueueManySized(10, 8);

        //Act
        var res = await queue.DequeueAsync();

        //Assert
        res.Items.Count.ShouldBe(1);
    }
}
