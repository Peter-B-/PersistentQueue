using System;
using System.IO;
using NUnit.Framework;

namespace PersistentQueue.Tests
{
    public class UnitTestPersistentQueue : PersistentQueue
    {
        public UnitTestPersistentQueue() : base(GetTempPath(), 10 << 10)
        {
        }

        public UnitTestPersistentQueue(string queuePath) : base(queuePath)
        {
        }

        public UnitTestPersistentQueue(string queuePath, long pageSize) : base(queuePath, pageSize)
        {
        }

        private static string GetTempPath()
        {
            return Path.Combine(Path.GetTempPath(), "PersistentQueue.Tests", Guid.NewGuid().ToString());
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                DeleteQueue();
        }

        private void DeleteQueue()
        {
            TestContext.WriteLine("Delete " + QueuePath);
            Directory.Delete(QueuePath, true);
        }
    }
}