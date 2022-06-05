using System;
using System.IO;

namespace Persistent.Queue.Benchmarks;

public static class Commons
{
    public static string GetTempPath()
    {
        return Path.Combine(Path.GetTempPath(), "PersistentQueue.Benchmarks", Guid.NewGuid().ToString());
    }

}