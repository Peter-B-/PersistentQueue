using System.Threading.Tasks;

namespace PersistentQueue.Utils
{
    public interface IQueueState
    {
        long TailIndex { get; }
        Task<IQueueState> NextUpdate { get; }
    }
}