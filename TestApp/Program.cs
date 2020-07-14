using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Persistent.Queue;

namespace TestApp
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            using var q = new PersistentQueue(@"C:\temp\PersistentQueue", 10 * 1024 * 1024);

            var items = 25000;
            var threads = 5;

            var taskList = new List<Task<int>>();

            var swOuter = Stopwatch.StartNew();
            for (var i = 0; i < threads; i++)
                taskList.Add(Task.Run(() =>
                {
                    var swInner = Stopwatch.StartNew();

                    for (var j = 0; j < items; j++)
                    {
                        var s = Encoding.UTF8.GetBytes(
                            $"This is line number {j}. Just adding some more text to grow the item size");

                        q.Enqueue(s);
                    }

                    swInner.Stop();
                    Console.WriteLine("Thread {0} Enqueued {1} items in {2} ms ({3:0} items/s)",
                        Environment.CurrentManagedThreadId,
                        items,
                        swInner.ElapsedMilliseconds,
                        (double) items / swInner.ElapsedMilliseconds * 1000);

                    return items;
                }));

            Task.WaitAll(taskList.ToArray());
            swOuter.Stop();

            Console.WriteLine("Enqueued totally {0} items in {1} ms ({2:0} items/s)",
                items * threads,
                swOuter.ElapsedMilliseconds,
                (double) items * threads / swOuter.ElapsedMilliseconds * 1000);


            swOuter.Reset();
            swOuter.Start();


            var count = 0;
            while (q.HasItems)
            {
                var dequeueTask = q.DequeueAsync();
                var res = await dequeueTask;
                foreach (var memory in res.Items)
                {
                    var str = Encoding.UTF8.GetString(memory.Span);
                    count++;
                }

                res.Commit();
            }


            swOuter.Stop();
            Console.WriteLine("Read {0} items in {1} ms ({2:0} items/s)",
                count,
                swOuter.ElapsedMilliseconds,
                (double) count / swOuter.ElapsedMilliseconds * 1000);
        }
    }
}
