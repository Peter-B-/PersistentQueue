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
        public void NewDataFilesAreCreated()
        {
            // Arrange
            var config = new PersistentQueueConfiguration()
            {
                QueuePath = UnitTestPersistentQueue.GetTempPath(),
                DataPageSize = 64,
            };
            var queue = new UnitTestPersistentQueue(config);
            
            
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
    }
}