using Persistent.Queue.DataObjects;

namespace Persistent.Queue.Interfaces;

public interface IPersistentQueueStatisticSource
{
    QueueStatistics GetStatistics();
}
