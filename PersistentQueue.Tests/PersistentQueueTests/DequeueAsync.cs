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

            result.Data.Count.ShouldBe(1);
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
            result.Data.Count.ShouldBe(2);
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
            result.Data.Count.ShouldBe(2);
        }
    }
}