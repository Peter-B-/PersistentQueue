using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using NUnit.Framework;
using Persistent.Queue.DataObjects;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests
{
    public class Statistics
    {
        [Test]
        public void Enqueue10()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();
            queue.EnqueueMany(10);

            // Act
            var statistics = queue.GetStatistics();

            // Assert
            var expectedDataSize =
                Enumerable.Range(1, 10)
                    .Select(itemNo => $"Message {itemNo}")
                    .Select(Encoding.UTF8.GetBytes)
                    .Select(bytes => bytes.LongLength)
                    .Sum();
    
            
            statistics.ShouldDeepEqual(new QueueStatistics()
            {
                QueueLength = 10,
                QueueDataSizeEstimate = expectedDataSize,
                TotalEnqueuedItems = 10
            });
        }

        [Test]
        public void Empty()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();

            // Act
            var statistics = queue.GetStatistics();

            // Assert
            statistics.ShouldDeepEqual(new QueueStatistics()
            {
                QueueLength = 0,
                QueueDataSizeEstimate = 0,
                TotalEnqueuedItems = 0
            });
        }

        [Test]
        public async Task EnqueueAndDequeue10()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();
            queue.EnqueueMany(10);

            for (var i = 0; i < 5; i++)
            {
                var result = await queue.DequeueAsync(1, 2);
                result.Commit();
            }

            // Act
            var statistics = queue.GetStatistics();

            // Assert
            statistics.ShouldDeepEqual(new QueueStatistics()
            {
                QueueLength = 0,
                QueueDataSizeEstimate = 0,
                TotalEnqueuedItems = 10
            });
        }
    }
}