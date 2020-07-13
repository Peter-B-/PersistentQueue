using System.IO;

namespace Persistent.Queue
{
    public class PersistentQueueConfiguration
    {
        public PersistentQueueConfiguration(string queuePath, long? dataPageSize = null)
        {
            QueuePath = queuePath;
            DataPageSize = dataPageSize ?? DefaultDataPageSize;
        }

        public string QueuePath { get; }
        public string MetaPageFolder { get; set; } = "meta";
        public string IndexPageFolder { get; set; } = "index";
        public string DataPageFolder { get; set; } = "data";

        // Index pages
        public long IndexItemsPerPage { get; set; } = 50000;
        public long DataPageSize { get; set; }

        public static long DefaultDataPageSize { get; } = 128 * 1024 * 1024;

        public string GetMetaPath() => Path.Combine(QueuePath, MetaPageFolder);
        public string GetIndexPath() => Path.Combine(QueuePath, IndexPageFolder);
        public string GetDataPath() => Path.Combine(QueuePath, DataPageFolder);
    }
}
