namespace Persistent.Queue.DataObjects;

public class QueueStatistics
{
    public long QueueDataSizeEstimate { get; set; }
    public long QueueLength { get; set; }
    public long TotalEnqueuedItems { get; set; }
}
