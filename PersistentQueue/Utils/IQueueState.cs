using System.Threading.Tasks;

namespace Persistent.Queue.Utils
{
    public interface IQueueState
    {
        long TailIndex { get; }
        Task<IQueueState> NextUpdate { get; }
    }
}