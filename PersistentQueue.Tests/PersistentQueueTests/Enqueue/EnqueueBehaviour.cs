using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests.Enqueue;

[TestFixture]
public class EnqueueBehaviour
{
    [Test]
    public static Task EnqueueNull() => EnqueueEmpty(null);

    [Test]
    public static Task EnqueueEmpty() => EnqueueEmpty([]);

    public static async Task EnqueueEmpty(byte[]? data)
    {
        // Arrange
        using var queue = new UnitTestPersistentQueue();

        // Act
        queue.Enqueue(data);

        // Assert
        var res = await queue.DequeueAsync();
        res.Items.Count.ShouldBe(1);
        res.Items[0].Length.ShouldBe(0);
        res.Commit();
    }
}
