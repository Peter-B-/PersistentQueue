using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests;

[TestFixture]
public class HasItems
{
    [Test]
    public void Empty_Enqueued()
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue();
        queue.EnqueueMany(10);

        // Act & Assert
        queue.HasItems.ShouldBeTrue();
    }

    [Test]
    public void Empty_HasItemsIsFalse()
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue();

        // Act & Assert
        queue.HasItems.ShouldBeFalse();
    }

    [Test]
    public async Task EnqueueAndDequeue10_HasItemsFalse()
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue();
        queue.EnqueueMany(10);

        // Act
        for (var i = 0; i < 5; i++)
        {
            var result = await queue.DequeueAsync(1, 2);
            result.Commit();
        }

        // Assert
        queue.HasItems.ShouldBeFalse();
    }
}
