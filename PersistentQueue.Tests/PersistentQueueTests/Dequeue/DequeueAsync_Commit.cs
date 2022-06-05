using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests.Dequeue;

[TestFixture]
public class DequeueAsync_Commit
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task NoCommit_SameElementsAreReturnedAgain(bool hasMaxSize)
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue(hasMaxSize);
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

    [TestCase(true)]
    [TestCase(false)]
    public async Task WithCommit_NewElementsAreReturned(bool hasMaxSize)
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue(hasMaxSize);
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
