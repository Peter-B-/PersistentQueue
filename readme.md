# PersistentQueue

![Nuget](https://img.shields.io/nuget/v/PersistentQueue)

This project provides a queue that is persisted on the local file system. It is intended as a local buffer for messages on IoT devices.

The items are persisted in [memory mapped files](https://docs.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files) accross application
and system restarts. Items are dequeued in batches and must be confirmed, before they are removed from the queue. Thereby it is
ensured that only items successfully sent to a downstream service, usually some kind of message bus, are removed from the queue.

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
to be stored locally and sent on reconnect. The messages must be persisted on hard dist, so they are available after software
restart or reboot. It is usually important to maintain the message order.

If a lot of messages have been collected, it is usually best to send them in batches to reduce time lost in round trips.
On the other hand, if the network connection is working, messages should be sent as soon as possible, to allow for real time analytics.

Batche size can be limited by message number and/or batch size.

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

### Batch size limit

The size of dequeue batches can be restricted by `PersistentQueueConfiguration.MaxDequeueBatchSize` and `MaxDequeueBatchSizeInBytes`. When
`.DequeueAsync` is called, all available items are reutned until

- the queue is empty
- `MaxDequeueBatchSize` items are returned
- `MaxDequeueBatchSizeInBytes` is reached

If `ThrowExceptionWhenItemExceedingMaxDequeueBatchSizeIsEnqueued` is `true` (default), an `InvalidOperationException` is thrown, if an item
exceeding `MaxDequeueBatchSizeInBytes` is enqueued. You can disable this option, if you want to enqueue and dequeue larger messages.

Btw: Ideas for a better name for `ThrowExceptionWhenItemExceedingMaxDequeueBatchSizeIsEnqueued` are highly welcome.

## Example

There is a [LinqPad](https://www.linqpad.net/) example on how to use the library at [Examples/Send to IoT Hub with PersistentQueue.linq](Examples/Send%20to%20IoT%20Hub%20with%20PersistentQueue.linq).

It consists of two loops running in parallel:

- `EnqueueMessages` enqueues serialized messages into a persistent queue.
- `SendLoop` does dequeue batches of messages, sends them to an [Azure IoT Hub](https://azure.microsoft.com/en-us/services/iot-hub/), if successful, commits them.

## Credits

The project is heavily based on and even forked from [work by larsolavk](https://github.com/larsolavk/PersistentQueue), who did most of the
heavy lifting with persistence and file access.
