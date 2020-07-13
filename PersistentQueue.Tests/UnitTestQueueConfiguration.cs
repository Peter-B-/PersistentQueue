using Persistent.Queue;

namespace PersistentQueue.Tests
{
    public class UnitTestQueueConfiguration : PersistentQueueConfiguration
    {
        public UnitTestQueueConfiguration()
        {
            QueuePath = UnitTestPersistentQueue.GetTempPath();
            DataPageSize = 10 * 1024;
            IndexItemsPerPage = 200;
        }
    }
}