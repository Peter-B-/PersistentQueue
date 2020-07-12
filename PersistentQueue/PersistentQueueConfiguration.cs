using System.IO;

namespace PersistentQueue
{
    public class PersistentQueueConfiguration
    {
        public string QueuePath { get; set; }
        public string MetaPageFolder { get; set; } = "meta";
        public string IndexPageFolder { get; set; } = "index";
        public string DataPageFolder { get; set; } = "data";

        // Index pages
        public long IndexItemsPerPage { get; set; } = 50000;
        public long DataPageSize { get; set; } = DefaultDataPageSize;

        public static long DefaultDataPageSize { get; } = 128 * 1024 * 1024;
        
        public string GetMetaPath() => Path.Combine(QueuePath, MetaPageFolder);
        public string GetIndexPath() => Path.Combine(QueuePath, IndexPageFolder);
        public string GetDataPath() => Path.Combine(QueuePath, DataPageFolder);

        public static PersistentQueueConfiguration GetDefault(string queuePath, long? dataPageSize = null)
        {
            return new PersistentQueueConfiguration
            {
                QueuePath = queuePath,
                DataPageSize = dataPageSize ?? DefaultDataPageSize
            };
        }
    }
}