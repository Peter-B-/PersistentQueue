using BenchmarkDotNet.Running;

namespace Persistent.Queue.Benchmarks
{
    public class BenchmarkProgram
    {
        public static void Main()
        {
            //var summary1 = BenchmarkRunner.Run<EnqueueBenchmark>();
            var summary2 = BenchmarkRunner.Run<DequeueBenchmark>();
        }
    }
}
