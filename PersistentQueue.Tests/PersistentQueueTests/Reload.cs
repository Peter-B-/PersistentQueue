using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace PersistentQueue.Tests.PersistentQueueTests
{
    [TestFixture]
    public class Reload
    {
        [TestCase(5, 15)]
        [TestCase(10, 10)]
        [TestCase(5, 15, 5)]
        [TestCase(10, 10, 5)]
        [TestCase(5, 15, 10)]
        [TestCase(10, 10, 10)]
        [TestCase(5, 15, 8)]
        [TestCase(10, 10, 8)]
        [TestCase(5, 15, 30, 64)]
        [TestCase(10, 10, 30, 64)]
        [TestCase(5, 15, 30, 128)]
        [TestCase(10, 10, 30, 128)]
        public async Task ReloadWithData(int firstDequeue, int secondDequeue, int indexItemsPerPage = 30, int dataPageSize = 32 * 10)
        {
            var config = new UnitTestQueueConfiguration
            {
                IndexItemsPerPage = indexItemsPerPage,
                DataPageSize = dataPageSize
            };
            try
            {
                using (var q1 = new Persistent.Queue.PersistentQueue(config))
                {
                    q1.EnqueueManySized(10, 32);
                    var result = await q1.DequeueAsync(1, firstDequeue);
                    result.Commit();
                }

                using (var q2 = new Persistent.Queue.PersistentQueue(config))
                {
                    q2.EnqueueManySized(10, 32);
                    var result = await q2.DequeueAsync(1, secondDequeue);
                    result.Commit();
                    q2.HasItems.ShouldBeFalse();
                }
            }
            finally
            {
                Directory.Delete(config.QueuePath, true);
            }
        }
    }
}
