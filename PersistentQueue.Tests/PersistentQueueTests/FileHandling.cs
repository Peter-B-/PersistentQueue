using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests
{
    [TestFixture]
    public class FileHandling
    {
        [Conditional("DEBUG")]
        private void PrintFiles(string path)
        {
            var files = string.Join(Environment.NewLine, Directory.GetFiles(path).Select(Path.GetFileName));

            TestContext.WriteLine("Files in " + path);
            TestContext.WriteLine(files);
        }

        private static async Task Dequeue(UnitTestPersistentQueue queue, int elements)
        {
            var result = await queue.DequeueAsync(1, elements);
            result.Items.Count.ShouldBe(elements);
            result.Commit();
        }

        [Test]
        public void DataFiles_AreCreated()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration
            {
                DataPageSize = 64
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
        public async Task DataFiles_ManyAreDeletedAfterCommit()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration
            {
                DataPageSize = 64
            };
            using var queue = new UnitTestPersistentQueue(config);


            // Act & Assert
            for (var i = 0; i < 20; i++)
                queue.Enqueue(new byte[32]);

            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(10);


            await Dequeue(queue, 10);
            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(6);
        }

        [Test]
        public async Task DataFiles_OneIsDeletedAfterCommit()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration
            {
                DataPageSize = 64
            };
            using var queue = new UnitTestPersistentQueue(config);


            // Act & Assert
            for (var i = 0; i < 20; i++)
                queue.Enqueue(new byte[32]);

            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(10);

            await Dequeue(queue, 3);
            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(9);
        }

        [Test]
        public async Task DataFiles_OneRemainsAfterAllItemsAreCommitted()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration
            {
                DataPageSize = 64
            };
            using var queue = new UnitTestPersistentQueue(config);


            // Act & Assert
            for (var i = 0; i < 20; i++)
                queue.Enqueue(new byte[32]);

            while (queue.HasItems)
                await Dequeue(queue, 2);

            Directory.GetFiles(config.GetDataPath()).Length.ShouldBe(1);
        }


        [Test]
        public void IndexFiles_AreCreated()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration
            {
                IndexItemsPerPage = 2
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
        public async Task IndexFiles_CreateAndDelete()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration
            {
                IndexItemsPerPage = 2
            };
            using var queue = new UnitTestPersistentQueue(config);


            // Act & Assert
            queue.EnqueueMany(2);
            await Dequeue(queue, 2);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(1);
            PrintFiles(config.GetIndexPath());

            queue.EnqueueMany(2);
            await Dequeue(queue, 2);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(1);
            PrintFiles(config.GetIndexPath());

            queue.EnqueueMany(2);
            await Dequeue(queue, 2);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(1);
            PrintFiles(config.GetIndexPath());
        }

        [Test]
        public async Task IndexFiles_ManyAreDeletedAfterCommit()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration
            {
                IndexItemsPerPage = 2
            };
            using var queue = new UnitTestPersistentQueue(config);


            // Act & Assert
            queue.EnqueueMany(20);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(10);

            var result = await queue.DequeueAsync(1, 10);
            result.Commit();
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(6);
        }

        [Test]
        public async Task IndexFiles_OneIsDeletedAfterCommit()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration
            {
                IndexItemsPerPage = 2
            };
            using var queue = new UnitTestPersistentQueue(config);


            // Act & Assert
            queue.EnqueueMany(20);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(10);

            await Dequeue(queue, 3);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(9);

            await Dequeue(queue, 4);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(7);
        }


        [Test]
        public async Task IndexFiles_OneRemainsAfterAllItemsAreCommitted()
        {
            // Arrange
            var config = new UnitTestQueueConfiguration
            {
                IndexItemsPerPage = 2
            };
            using var queue = new UnitTestPersistentQueue(config);


            // Act & Assert
            queue.EnqueueMany(20);
            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(10);

            while (queue.HasItems)
                await Dequeue(queue, 2);

            Directory.GetFiles(config.GetIndexPath()).Length.ShouldBe(1);
        }
    }
}
