using BenchmarkDotNet.Attributes;

namespace Persistent.Queue.Benchmarks;

public class EnqueueBenchmark
{
    private PersistentQueue? _queue;

    [Params(100, 1000)]
    public int EnqueueCount { get; set; }

    [Params(1 *1024, 10 * 1024)]
    public int ItemSize { get; set; }

    [Params(100 * 1024, 10 * 1024 * 1024)]
    public long? DataPageSize { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var config = new PersistentQueueConfiguration(Commons.GetTempPath(), DataPageSize);
        _queue = config.CreateQueue();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _queue?.Dispose();
    }

    [Benchmark]
    public void Enqueue()
    {
        if (_queue == null) throw new ArgumentNullException(nameof(_queue));

        var data = new byte[ItemSize];
        for (var i = 0; i < EnqueueCount; i++)
            _queue.Enqueue(data);
    }
}
