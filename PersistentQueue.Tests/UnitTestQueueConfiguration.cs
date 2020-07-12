namespace PersistentQueue.Tests
{
    public class UnitTestQueueConfiguration : PersistentQueueConfiguration
    {
        public UnitTestQueueConfiguration()
        {
            QueuePath = UnitTestPersistentQueue.GetTempPath();
        }
    }
}