﻿using System.IO;
using System.Text;
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
                var s = Encoding.UTF8.GetBytes($"Message {i}");
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
    }
    
    
}