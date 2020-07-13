using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Persistent.Queue.Benchmarks
{
    public class DequeueBenchmark
    {
        private PersistentQueue _queue;

        [Params(1000)]
        public int EnqueueCount { get; set; }

        [Params(1, 100)]
        public int BatchSize { get; set; }

        [Params(1 *1024, 10 * 1024)]
        public int ItemSize { get; set; }

        [Params(100 * 1024, 10 * 1024 * 1024)]
        public long? DataPageSize { get; set; }

        [IterationSetup]
        public void Setup()
        {
            var config = new PersistentQueueConfiguration(Commons.GetTempPath(), DataPageSize);
            _queue = config.CreateQueue();

            var data = new byte[ItemSize];
            for (var i = 0; i < EnqueueCount; i++)
                _queue.Enqueue(data);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _queue?.Dispose();
        }

        [Benchmark]
        public async Task Dequeue()
        {
            while (_queue.HasItems)
            {
                var result = await _queue.DequeueAsync(BatchSize);
                result.Commit();
            }
        }
    }
}
