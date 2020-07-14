using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests
{
    [TestFixture]
    public class DequeueAsync
    {
        [Test]
        public async Task EmptyQueue_WaitForNextItem()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();

            // Act & Assert
            var resultTask = queue.DequeueAsync(2);
            resultTask.IsCompleted.ShouldBeFalse();

            queue.Enqueue(1);
            var result = await resultTask;

            result.Items.Count.ShouldBe(1);
        }

        [Test]
        public async Task ItemsInQueue_ReturnAll()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();
            queue.EnqueueMany(2);

            // Act
            var result = await queue.DequeueAsync(2);

            // Assert
            result.Items.Count.ShouldBe(2);
        }

        [Test]
        public void ItemsInQueue_ReturnsSynchronous()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();
            queue.EnqueueMany(2);

            // Act
            var resultTask = queue.DequeueAsync(2);

            // Assert
            resultTask.IsCompleted.ShouldBeTrue();
        }

        [Test]
        public async Task LessThanMaxItemsInQueue_ReturnsAvailable()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();
            queue.EnqueueMany(2);

            // Act
            var result = await queue.DequeueAsync(10);

            // Assert
            result.Items.Count.ShouldBe(2);
        }


        [Test]
        public void WaitForMinElements()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();

            // Act & Assert
            var resultTask = queue.DequeueAsync(12, 10);
            for (var i = 0; i < 4; i++)
            {
                queue.EnqueueMany(2);
                resultTask.IsCompleted.ShouldBeFalse();
            }

            queue.EnqueueMany(2);
            resultTask.IsCompleted.ShouldBeTrue();
        }

        [Test]
        public void WaitAndCancel()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();

            var cts = new CancellationTokenSource();

            // Act & Assert
            var resultTask = queue.DequeueAsync(12, 10, cts.Token);
            resultTask.IsCompleted.ShouldBeFalse();
            resultTask.IsCanceled.ShouldBeFalse();

            cts.Cancel();
            resultTask.IsCanceled.ShouldBeTrue();
        }

        [Test]
        public void Cancel_ShouldThrowException()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();

            var cts = new CancellationTokenSource();

            // Act & Assert
            var resultTask = queue.DequeueAsync(12, 10, cts.Token);

            cts.Cancel();
            Should.ThrowAsync<OperationCanceledException>(async () => await resultTask);
        }
    }
}
