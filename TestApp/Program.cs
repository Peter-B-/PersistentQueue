﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var q = new PersistentQueue.PersistentQueue(@"c:\temp\PersistentQueue", 10 * 1024 * 1024);
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

            taskList.Clear();

            for (var i = 0; i < threads; i++)
                taskList.Add(Task.Run(() =>
                {
                    Stream stream;
                    var read = 0;
                    var swInner = Stopwatch.StartNew();

                    do
                    {
                        using (stream = q.Dequeue())
                        {
                            if (stream != null)
                            {
                                read++;

                                using (var br = new BinaryReader(stream))
                                {
                                    var s = new string(br.ReadChars((int) stream.Length));
                                    //Console.WriteLine(s);
                                }
                            }
                        }
                    } while (stream != null);

                    swInner.Stop();
                    Console.WriteLine("Thread {0} Dequeued {1} items in {2} ms ({3:0} items/s)",
                        Environment.CurrentManagedThreadId,
                        read,
                        swInner.ElapsedMilliseconds,
                        (double) read / swInner.ElapsedMilliseconds * 1000);

                    return read;
                }));

            Task.WaitAll(taskList.ToArray());
            swOuter.Stop();

            var sum = taskList.Sum(t => t.Result);
            Console.WriteLine("Dequeued totally {0} items in {1} ms ({2:0} items/s)",
                sum,
                swOuter.ElapsedMilliseconds,
                (double) sum / swOuter.ElapsedMilliseconds * 1000);


            //    var swOuter = new Stopwatch();
            //    var swInner = new Stopwatch();
            //    swOuter.Start();
            //    for (int i = 0; i < items; i++)
            //    {
            //        using (var s = GetStream(String.Format("This is line number {0}. Just adding some more text to grow the item size", i)))
            //        {
            //            swInner.Start();
            //            q.Enqueue(s);
            //            swInner.Stop();
            //        }
            //    }
            //    swOuter.Stop();

            //    Console.WriteLine("Enqueued {0} items in {1} ms ({2:0} items/s). Inner: {3} ms ({4:0} items/s)",
            //        items,
            //        swOuter.ElapsedMilliseconds,
            //        ((double)items / swOuter.ElapsedMilliseconds) * 1000,
            //        swInner.ElapsedMilliseconds,
            //        ((double)items / swInner.ElapsedMilliseconds) * 1000);


            //    Stream stream;
            //    swOuter.Reset();
            //    swInner.Reset();
            //    swOuter.Start();
            //    items = 0;

            //    // Get n number of items from the queue
            //    //var n = 2;
            //    //for (int i = 0; i < n; i++)
            //    //{
            //    //    if (null == (stream = q.Dequeue()))
            //    //        break;

            //    //    items++;
            //    //    using (var br = new BinaryReader(stream))
            //    //    {
            //    //        var s = new string(br.ReadChars((int)stream.Length));
            //    //        Console.WriteLine(s);
            //    //    }
            //    //    stream.Dispose();
            //    //}


            //    // Get all items from queue
            //    do
            //    {
            //        swInner.Start();
            //        using (stream = q.Dequeue())
            //        {
            //            swInner.Stop();
            //            if (stream != null)
            //            {
            //                items++;

            //                using (var br = new BinaryReader(stream))
            //                {
            //                    var s = new string(br.ReadChars((int)stream.Length));
            //                    //Console.WriteLine(s);
            //                }
            //            }
            //        }
            //    }
            //    while (stream != null);

            //    Console.WriteLine("Dequeued {0} items in {1} ms ({2:0} items/s). Inner: {3} ms ({4:0} items/s)",
            //        items,
            //        swOuter.ElapsedMilliseconds,
            //        ((double)items / swOuter.ElapsedMilliseconds) * 1000,
            //        swInner.ElapsedMilliseconds,
            //        ((double)items / swInner.ElapsedMilliseconds) * 1000);

            //    //Console.ReadLine();
        }


        private static byte[] GetBytes(string str)
        {
            var bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static string GetString(byte[] bytes)
        {
            var chars = new char[bytes.Length / sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }
}