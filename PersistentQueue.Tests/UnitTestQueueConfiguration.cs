using System;
using System.IO;
using Persistent.Queue;

namespace PersistentQueue.Tests
{
    public class UnitTestQueueConfiguration : PersistentQueueConfiguration
    {
        public UnitTestQueueConfiguration():base(GetTempPath(), 10*1024)
        {
            IndexItemsPerPage = 200;
        }

        public static string GetTempPath()
        {
            return Path.Combine(Path.GetTempPath(), "PersistentQueue.Tests", Guid.NewGuid().ToString());
        }
    }
}