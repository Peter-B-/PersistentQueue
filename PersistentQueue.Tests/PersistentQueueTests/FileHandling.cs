using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests
{
    [TestFixture]
    public class FileHandling
    {
        [Test]
        public void DataFiles_AreCreated()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration()
            {
                DataPageSize = 64,
            };
            using var queue = new UnitTestPersistentQueue(config);
            
            
            // Act & Assert
            queue.Enqueue(new byte[32]);
            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(1);
            queue.Enqueue(new byte[32]);
            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(1);
            queue.Enqueue(new byte[32]);
            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(2);
            queue.Enqueue(new byte[32]);
            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(2);
        }

        [Test]
        public async Task DataFiles_OneIsDeletedAfterCommit()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration()
            {
                DataPageSize = 64,
            };
            using var queue = new UnitTestPersistentQueue(config);


            // Act & Assert
            for (var i = 0; i < 20; i++)
                queue.Enqueue(new byte[32]);

            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(10);

            var result = await queue.DequeueAsync(2);
            result.Commit();
            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(9);
        }

        [Test]
        public async Task DataFiles_ManyAreDeletedAfterCommit()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration()
            {
                DataPageSize = 64,
            };
            using var queue = new UnitTestPersistentQueue(config);


            // Act & Assert
            for (var i = 0; i < 20; i++)
                queue.Enqueue(new byte[32]);

            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(10);


            var result = await queue.DequeueAsync(10);
            result.Commit();
            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(5);
        }

        
        [Test]
        public void IndexFiles_AreCreated()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration()
            {
                IndexItemsPerPage = 2,
            };
            using var queue = new UnitTestPersistentQueue(config);
            
            
            // Act & Assert
            queue.Enqueue(1);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(1);
            queue.Enqueue(1);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(1);
            queue.Enqueue(1);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(2);
            queue.Enqueue(1);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(2);
        }
        
        [Test]
        public async Task IndexFiles_OneIsDeletedAfterCommit()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration()
            {
                IndexItemsPerPage = 2
            };
            using var queue = new UnitTestPersistentQueue(config);


            // Act & Assert
            queue.EnqueueMany(20);
            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(10);


            var result = await queue.DequeueAsync(2);
            result.Commit();
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(9);

            result = await queue.DequeueAsync(4);
            result.Commit();
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(7);
        }

        [Test] public async Task IndexFiles_ManyAreDeletedAfterCommit()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration()
            {
                IndexItemsPerPage = 2
            };
            using var queue = new UnitTestPersistentQueue(config);


            // Act & Assert
            queue.EnqueueMany(20);
            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(10);

            var result = await queue.DequeueAsync(10);
            result.Commit();
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(5);
        }
    }
}