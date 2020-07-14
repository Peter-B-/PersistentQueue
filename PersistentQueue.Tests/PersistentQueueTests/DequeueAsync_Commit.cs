using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests
{
    [TestFixture]
    public class DequeueAsync_Commit
    {
        [Test]
        public async Task NoCommit_SameElementsAreReturnedAgain()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();
            queue.EnqueueMany(2);

            // Act
            var results1 = await queue.DequeueAsync(1, 2);
            var results2 = await queue.DequeueAsync(1, 2);
            var results3 = await queue.DequeueAsync(1, 2);

            // Assert
            results1.Items.Count.ShouldBe(2);
            results2.Items.Count.ShouldBe(2);
            results3.Items.Count.ShouldBe(2);

            CollectionAssert.AreEqual(results1.Items[0].ToArray(), results3.Items[0].ToArray());
        }

        [Test]
        public async Task WithCommit_NewElementsAreReturned()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();
            queue.EnqueueMany(10);

            // Act
            var results1 = await queue.DequeueAsync(1, 2);
            results1.Commit();

            var results2 = await queue.DequeueAsync(1, 2);

            // Assert
            results1.Items.Count.ShouldBe(2);
            results2.Items.Count.ShouldBe(2);

            CollectionAssert.AreNotEqual(results1.Items[0].ToArray(), results2.Items[0].ToArray());
        }
    }
}
