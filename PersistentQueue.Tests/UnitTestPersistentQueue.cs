using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PersistentQueue.Tests
{
    public class UnitTestPersistentQueue : PersistentQueue
    {
        public UnitTestPersistentQueue() : base(GetTempPath(), 10 << 10)
        {
        }

        public UnitTestPersistentQueue(PersistentQueueConfiguration configuration) : base(configuration)
        {
        }

        public static string GetTempPath()
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
            TestContext.WriteLine("Delete " + Configuration.QueuePath);
            Directory.Delete(Configuration.QueuePath, true);
        }

        public void Enqueue(int itemNo)
        {
            var s = Encoding.UTF8.GetBytes($"Message {itemNo}");
            Enqueue(s);
        }

        public void EnqueueMany(int count, int start = 1)
        {
            Enumerable.Range(start, count)
                .ForEach(Enqueue);
        }
    }
}