using System.IO;
using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        [Test]
        public void DequeueEmpty()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();

            // Act
            var res = queue.Dequeue();

            // Assert
            res.ShouldBeNull();
        }

        [Test]
        public void EnqueueDequeue()
        {
            // Arrange
            using var queue = new UnitTestPersistentQueue();

            // Act
            for (int i = 0; i < 5; i++)
            {
                using var s = GetStream("Message " + i);
                queue.Enqueue(s);
            }

            Stream outStream;
            var count = 0;
            while ((outStream = queue.Dequeue()) != null)
            {
                var reader = new StreamReader(outStream);
                var message = reader.ReadToEnd();
                message.ShouldStartWith("Message ");
                count++;
            }
            
            count.ShouldBe(5);
        }
        
        private static Stream GetStream(string s)
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            sw.Write(s);
            sw.Flush();
            ms.Position = 0;
            return ms;
        }

    }
    
    
}