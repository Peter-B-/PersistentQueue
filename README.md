# PersistentQueue

![.NET Core](https://github.com/Peter-B-/PersistentQueue/workflows/.NET%20Core/badge.svg)
![Nuget](https://img.shields.io/nuget/v/PersistentQueue)

This project provides a queue that is persisted on the local file system. It is intended as a local buffer for messages on IoT devices.

The items are persisted in [memory mapped files](https://docs.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files).

## Getting started

Install the [PersistentQueue Nuget package](https://www.nuget.org/packages/PersistentQueue) and run the following:

```c#
var configuration = new PersistentQueueConfiguration(@"d:\Temp\Queue\");
using var queue = configuration.CreateQueue();

// Enqueue 2 items
queue.Enqueue(Encoding.UTF8.GetBytes("A first item"));
queue.Enqueue(Encoding.UTF8.GetBytes("A second item"));


// Dequeue up to 10 items
// Will return immediatelly, since more than minItems are in the queue
var result = await queue.DequeueAsync(minItems: 1, maxItems: 10);

var messages =
    result.Items
        .Select(item => Encoding.UTF8.GetString(item.Span));

foreach (var message in messages)
	Console.WriteLine(message);

// Commit returned dequeued items so they will not be returned next time.
result.Commit();
```

## Concepts

IoT devices usually collect data at their own pace and have to send them over a network connection. If the connection is broken, data has
to be stored locally and sent on reconnect. It is usually important to maintain the message order.

If a lot of messages have been collected, it is usually best to send them in batches to reduce time lost in round trips.
On the other hand, if the network connection is working, messages should be sent as soon as possible, to allow for real time analytics.

This library was designed with those concepts in mind. New items are persisted on the file system, when `.Enqueue(item)` is called.

`.DequeueAsync()` will return as soon as `minItems` items are available. It returns an `IDequeueResult`:

```c#
public interface IDequeueResult
{
    IReadOnlyList<Memory<byte>> Items { get; }
    void Commit();
    void Reject();
}
```

`Items` contains the items to be consumed. If the items were passed on successfully, `.Commit()` must be called, which marks all items
of the batch as processed and deletes them from the file system. If `.Commit()` is not called on the `IDequeueResult`, subsequent calls to
`.DequeueAsync()` will return the same items again.

## Example

There is a [LinqPad](https://www.linqpad.net/) example on how to use the library at `Examples/Send to IoT Hub with PersistentQueue.linq`.

It consists of two loops running in parallel:
 - `EnqueueMessages` enqueues serialized messages into a persistent queue.
 - `SendLoop` does dequeue batches of messages, sends them to an [Azure IoT Hub](https://azure.microsoft.com/en-us/services/iot-hub/), if successful, commits them.

## Credits

The project is heavily based on and even forked from [work by larsolavk](https://github.com/larsolavk/PersistentQueue), who did most of the
heavy lifting with persistence and file access.
