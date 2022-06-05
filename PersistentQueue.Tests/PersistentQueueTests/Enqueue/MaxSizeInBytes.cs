using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests.Enqueue;

[TestFixture]
public class MaxSizeInBytes
{
    [TestCase(true)]
    [TestCase(false)]
    public void AllowSmallItem(bool throwWhenExceeding)
    {
        //Arrange
        var config = new UnitTestQueueConfiguration
        {
            MaxDequeueBatchSizeInByte = 32,
            ThrowExceptionWhenItemExceedingMaxDequeueBatchSizeIsEnqueued = throwWhenExceeding
        };
        using var queue = new UnitTestPersistentQueue(config);

        //Act
        queue.EnqueueSized(32);

        //Assert
        queue.HasItems.ShouldBeTrue();
    }

    [Test]
    public void AllowLargeItem()
    {
        //Arrange
        var config = new UnitTestQueueConfiguration
        {
            MaxDequeueBatchSizeInByte = 32,
            ThrowExceptionWhenItemExceedingMaxDequeueBatchSizeIsEnqueued = false
        };
        using var queue = new UnitTestPersistentQueue(config);

        //Act
        queue.EnqueueSized(33);

        //Assert
        queue.HasItems.ShouldBeTrue();
    }

    [Test]
    public void ThrowOnLargeItem()
    {
        //Arrange
        var config = new UnitTestQueueConfiguration
        {
            MaxDequeueBatchSizeInByte = 32,
            ThrowExceptionWhenItemExceedingMaxDequeueBatchSizeIsEnqueued = true
        };
        using var queue = new UnitTestPersistentQueue(config);

        //Act
        Should.Throw<InvalidOperationException>(() => queue.EnqueueSized(33));

        //Assert
        queue.HasItems.ShouldBeFalse();
    }
}
