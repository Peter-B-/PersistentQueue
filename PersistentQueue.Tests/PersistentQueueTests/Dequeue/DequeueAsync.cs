using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests.Dequeue;

[TestFixture]
public class DequeueAsync
{
    [TestCase(true)]
    [TestCase(false)]
    public void Cancel_ShouldThrowException(bool hasMaxSize)
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue(hasMaxSize);

        var cts = new CancellationTokenSource();

        // Act & Assert
        var resultTask = queue.DequeueAsync(10, 12, cts.Token);

        cts.Cancel();
        Should.ThrowAsync<OperationCanceledException>(async () => await resultTask);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task EmptyQueue_WaitForNextItem(bool hasMaxSize)
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue(hasMaxSize);

        // Act & Assert
        var resultTask = queue.DequeueAsync(1, 2);
        resultTask.IsCompleted.ShouldBeFalse();

        queue.Enqueue(1);
        var result = await resultTask;

        result.Items.Count.ShouldBe(1);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ItemsInQueue_ReturnAll(bool hasMaxSize)
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue(hasMaxSize);
        queue.EnqueueMany(2);

        // Act
        var result = await queue.DequeueAsync(1, 2);

        // Assert
        result.Items.Count.ShouldBe(2);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ItemsInQueue_ReturnsSynchronous(bool hasMaxSize)
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue(hasMaxSize);
        queue.EnqueueMany(2);

        // Act
        var resultTask = queue.DequeueAsync(1, 2);

        // Assert
        resultTask.IsCompleted.ShouldBeTrue();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task LessThanMaxItemsInQueue_ReturnsAvailable(bool hasMaxSize)
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue(hasMaxSize);
        queue.EnqueueMany(2);

        // Act
        var result = await queue.DequeueAsync(1, 10);

        // Assert
        result.Items.Count.ShouldBe(2);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void WaitAndCancel(bool hasMaxSize)
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue(hasMaxSize);

        var cts = new CancellationTokenSource();

        // Act & Assert
        var resultTask = queue.DequeueAsync(10, 12, cts.Token);
        resultTask.IsCompleted.ShouldBeFalse();
        resultTask.IsCanceled.ShouldBeFalse();

        cts.Cancel();
        resultTask.IsCanceled.ShouldBeTrue();
    }


    [TestCase(true)]
    [TestCase(false)]
    public async Task WaitForMinElements(bool hasMaxSize)
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue(hasMaxSize);

        // Act & Assert
        var resultTask = queue.DequeueAsync(10, 12);
        for (var i = 0; i < 4; i++)
        {
            queue.EnqueueMany(2);
            resultTask.IsCompleted.ShouldBeFalse();
        }

        queue.EnqueueMany(2);
        await resultTask;
    }
}
